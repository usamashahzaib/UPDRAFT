using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;

namespace HotdropBackend.Endpoints;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches").RequireAuthorization();

        // Spec section 10: a player may rejoin the same avatar until match end.
        // The caller uses this authenticated check before requesting a fresh,
        // short-lived join ticket; the dedicated server restores its reserved slot.
        group.MapPost("/{matchId}/reconnect", async (Guid matchId, HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            bool reserved = await db.MatchmakingTickets.AnyAsync(t => t.MatchId == matchId && t.PlayerId == playerId && t.Status == "matched", cancellationToken);
            bool finished = await db.MatchParticipations.AnyAsync(m => m.MatchId == matchId, cancellationToken);
            if (!reserved) return Results.Forbid();
            return Results.Ok(new { matchId, reconnectAllowed = !finished });
        });

        // QA-017: authoritative reward receipt for the caller's own participation.
        // recorded=false (still 200) until the server's result lands, so the client
        // can poll and show "reward pending" instead of guessing.
        group.MapGet("/{matchId}/receipt", async (Guid matchId, HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(ctx.User.FindFirst("player_id")?.Value, out var playerId)) return Results.Forbid();
            var row = await db.MatchParticipations.AsNoTracking()
                .SingleOrDefaultAsync(m => m.MatchId == matchId && m.PlayerId == playerId, cancellationToken);
            int level = await db.PlayerAccounts.Where(a => a.Id == playerId).Select(a => (int?)a.Level).SingleOrDefaultAsync(cancellationToken) ?? 1;
            return Results.Ok(new MatchReceipt(
                Recorded: row is not null,
                Eligible: row?.Eligible ?? false,
                XpGained: row?.XpGained ?? 0,
                CreditsGained: row?.CreditsGained ?? 0,
                Level: level,
                Unlocked: string.IsNullOrEmpty(row?.UnlockedCsv) ? [] : row.UnlockedCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)));
        });

        // Dedicated server admits a client only after the backend confirms its join ticket.
        group.MapPost("/validate-ticket", async (ValidateTicketRequest request, MatchTicketService tickets, HotdropDbContext db,
            CancellationToken cancellationToken) =>
        {
            var principal = tickets.ValidateJoinTicket(request.Ticket);
            if (principal is null) return Results.Unauthorized();
            var playerId = Guid.Parse(principal.FindFirst("player_id")!.Value);
            var matchId = Guid.Parse(principal.FindFirst("match_id")!.Value);
            if (request.MatchId != matchId) return Results.Forbid();
            bool reserved = await db.MatchmakingTickets.AnyAsync(t => t.MatchId == matchId && t.PlayerId == playerId && t.Status == "matched", cancellationToken);
            if (!reserved) return Results.Forbid();
            var account = await db.PlayerAccounts.FindAsync([playerId], cancellationToken);
            return Results.Ok(new { playerId, matchId, team = principal.FindFirst("team")!.Value, alias = account?.Alias });
        }).RequireAuthorization(policy => policy.RequireClaim("service", "game-server"));

        // Spec sections 20/29: an idle dedicated server claims the oldest composed match
        // that has no server yet. Capped by Allocator:MaxConcurrentMatches (cost control).
        group.MapPost("/claim", async (HttpContext ctx, HotdropDbContext db, AllocatorOptions allocator, CancellationToken cancellationToken) =>
        {
            string? serverId = ctx.User.FindFirst("server_id")?.Value;
            if (string.IsNullOrEmpty(serverId)) return Results.Forbid();
            var staleBefore = DateTimeOffset.UtcNow - AllocationTimeout;
            var now = DateTimeOffset.UtcNow;
            var stale = await db.MatchAllocations
                .Where(a => a.CompletedAt == null && a.ClaimedAt <= staleBefore)
                .ToListAsync(cancellationToken);
            if (stale.Count > 0)
            {
                // A server that disappeared cannot produce a result. Retire its lease and
                // cancel the reservation without creating a participation, rating or reward.
                var staleIds = stale.Select(a => a.MatchId).ToList();
                var reserved = await db.MatchmakingTickets
                    .Where(t => t.MatchId != null && staleIds.Contains(t.MatchId.Value) && t.Status == "matched")
                    .ToListAsync(cancellationToken);
                foreach (var allocation in stale) allocation.CompletedAt = now;
                foreach (var ticket in reserved)
                {
                    ticket.Status = "cancelled";
                    ticket.UpdatedAt = now;
                }
                await db.SaveChangesAsync(cancellationToken);
            }

            var active = await db.MatchAllocations.Where(a => a.CompletedAt == null).ToListAsync(cancellationToken);
            if (active.Count(a => a.ClaimedAt > staleBefore) >= allocator.MaxConcurrentMatches)
                return Results.Json(new { error = "capacity" }, statusCode: StatusCodes.Status503ServiceUnavailable);

            var allocated = await db.MatchAllocations.Select(a => a.MatchId).ToListAsync(cancellationToken);
            var candidates = await db.MatchmakingTickets
                .Where(t => t.Status == "matched" && t.MatchId != null)
                .ToListAsync(cancellationToken);
            foreach (var match in candidates.Where(t => !allocated.Contains(t.MatchId!.Value))
                         .GroupBy(t => t.MatchId!.Value).OrderBy(g => g.Min(t => t.UpdatedAt)))
            {
                db.MatchAllocations.Add(new MatchAllocation { MatchId = match.Key, ServerId = serverId });
                try { await db.SaveChangesAsync(cancellationToken); }
                catch (DbUpdateException)
                {
                    // Another server claimed it first; try the next one.
                    db.ChangeTracker.Clear();
                    continue;
                }
                var ids = match.Select(t => t.PlayerId).ToList();
                // QA-024: each player's equipped cosmetic travels with the claim so the match server
                // can show every player's chosen identity to everyone (presentation only).
                var accounts = await db.PlayerAccounts.Where(p => ids.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => new { p.Alias, p.EquippedCosmeticId }, cancellationToken);
                return Results.Ok(new
                {
                    matchId = match.Key,
                    session = match.Key.ToString("N"),
                    region = allocator.FusionRegion,
                    hasBots = match.Any(t => t.HasBots),
                    players = match.Select(t => new
                    {
                        playerId = t.PlayerId, team = t.Team,
                        alias = accounts.GetValueOrDefault(t.PlayerId)?.Alias,
                        cosmetic = accounts.GetValueOrDefault(t.PlayerId)?.EquippedCosmeticId,
                    }),
                });
            }
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireClaim("service", "game-server"));

        // Spec section 21: results come only from the authenticated game server.
        group.MapPost("/{matchId}/result", async (Guid matchId, SubmitResultRequest request, HotdropDbContext db,
            MatchResultProcessor processor, CancellationToken cancellationToken) =>
        {
            if (request.Participants is null || request.Participants.Count is 0 or > MatchmakingOptions.MatchSize)
                return Results.BadRequest(new { error = "invalid_participants" });
            var summaries = await processor.ApplyAsync(matchId, request, db, cancellationToken);
            var allocation = await db.MatchAllocations.FindAsync([matchId], cancellationToken);
            if (allocation is { CompletedAt: null })
            {
                allocation.CompletedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
            return Results.Ok(new { recorded = true, players = summaries });
        }).RequireAuthorization(policy => policy.RequireClaim("service", "game-server"));
    }

    /// <summary>An allocation with no result after this long no longer counts toward capacity (crashed server).</summary>
    public static readonly TimeSpan AllocationTimeout = TimeSpan.FromMinutes(15);

    public sealed record ValidateTicketRequest(Guid MatchId, string Ticket);

    /// <summary>Serialized camelCase: {recorded, eligible, xpGained, creditsGained, level, unlocked}.</summary>
    public sealed record MatchReceipt(bool Recorded, bool Eligible, int XpGained, int CreditsGained, int Level, string[] Unlocked);
}



