using Microsoft.EntityFrameworkCore;
using System.Data;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;

namespace HotdropBackend.Endpoints;

public static class MatchmakingEndpoints
{
    // Spec section 14: one public queue, one initial region. Composition
    // (party-aware grouping, rating-window widening, bot-fill) happens
    // out-of-band in MatchmakingWorker/MatchmakingComposer; these endpoints
    // only read/write ticket rows.
    public static void MapMatchmakingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matchmaking").RequireAuthorization();

        group.MapPost("/enqueue", async (HttpContext ctx, HotdropDbContext db,
            Microsoft.Extensions.Options.IOptionsMonitor<FeatureFlags> features, CancellationToken cancellationToken) =>
        {
            if (!features.CurrentValue.MatchmakingEnabled)
                return Results.Json(new { error = "matchmaking_disabled" }, statusCode: StatusCodes.Status503ServiceUnavailable);
            var playerId = PlayerId(ctx);
            var account = await db.PlayerAccounts.FindAsync([playerId], cancellationToken);
            if (account is null) return Results.NotFound();

            var membership = await db.PartyMembers.SingleOrDefaultAsync(m => m.PlayerId == playerId, cancellationToken);
            Guid? partyId = membership?.PartyId;
            if (partyId is not null)
            {
                var party = await db.Parties.FindAsync([partyId.Value], cancellationToken);
                if (party is null) return Results.Conflict(new { error = "party_unavailable" });
                if (party.LeaderId != playerId) return Results.Forbid();
                bool everyoneReady = await db.PartyMembers
                    .Where(m => m.PartyId == partyId)
                    .AllAsync(m => m.IsReady, cancellationToken);
                if (!everyoneReady) return Results.Conflict(new { error = "party_not_ready" });
            }
            List<Guid> playerIds = partyId is null
                ? [playerId]
                : await db.PartyMembers.Where(m => m.PartyId == partyId).Select(m => m.PlayerId).ToListAsync(cancellationToken);

            bool alreadyQueued = await db.MatchmakingTickets
                .Where(t => playerIds.Contains(t.PlayerId) && t.Status == "searching")
                .AnyAsync(cancellationToken);
            if (alreadyQueued) return Results.Conflict(new { error = "already_queued" });

            var now = DateTimeOffset.UtcNow;
            var accounts = await db.PlayerAccounts.Where(a => playerIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);
            foreach (var id in playerIds)
            {
                db.MatchmakingTickets.Add(new MatchmakingTicket
                {
                    Id = Guid.NewGuid(),
                    PlayerId = id,
                    PartyId = partyId,
                    Rating = accounts.TryGetValue(id, out var a) ? a.HiddenMatchmakingRating : 1000,
                    EnqueuedAt = now,
                    UpdatedAt = now,
                    Status = "searching",
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { queued = true, playerId = playerId.ToString() });
        });

        group.MapPost("/cancel", async (HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            // Serialize cancellation against the allocator's claim so a match cannot be
            // claimed in the gap between checking for a lease and withdrawing its tickets.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var playerId = PlayerId(ctx);
            var ticket = await db.MatchmakingTickets
                .Where(t => t.PlayerId == playerId && (t.Status == "searching" || t.Status == "matched"))
                .OrderByDescending(t => t.EnqueuedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (ticket is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { cancelled = true });
            }

            // Once a dedicated server claims a reservation it may already be constructing
            // the session. Keep the client in the join flow instead of orphaning that lease.
            if (ticket.Status == "matched" && ticket.MatchId is { } matchId &&
                await db.MatchAllocations.AnyAsync(a => a.MatchId == matchId && a.CompletedAt == null, cancellationToken))
                return Results.Conflict(new { error = "reservation_starting" });

            var partyTickets = ticket.PartyId is null
                ? new List<MatchmakingTicket> { ticket }
                : await db.MatchmakingTickets.Where(t => t.PartyId == ticket.PartyId &&
                    (t.Status == "searching" || t.Status == "matched") && t.MatchId == ticket.MatchId).ToListAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            foreach (var t in partyTickets) { t.Status = "cancelled"; t.UpdatedAt = now; }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { cancelled = true });
        });

        group.MapGet("/status", async (HttpContext ctx, HotdropDbContext db, AllocatorOptions allocator, CancellationToken cancellationToken) =>
        {
            var playerId = PlayerId(ctx);
            var ticket = await db.MatchmakingTickets
                .Where(t => t.PlayerId == playerId)
                .OrderByDescending(t => t.EnqueuedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (ticket is null) return Results.Ok(new { state = "idle", elapsedSeconds = 0, region = allocator.FusionRegion });

            var state = ticket.Status switch { "matched" => "matched", "cancelled" => "idle", _ => "searching" };
            // QA-014: party members follow the leader by polling this endpoint, so a "matched"
            // ticket from a match that already ended (or never got a server) must read as idle.
            if (state == "matched")
            {
                bool finished = ticket.MatchId is null || await db.MatchAllocations
                    .AnyAsync(a => a.MatchId == ticket.MatchId && a.CompletedAt != null, cancellationToken);
                bool stale = DateTimeOffset.UtcNow - ticket.UpdatedAt > TimeSpan.FromMinutes(10) &&
                    !await db.MatchAllocations.AnyAsync(a => a.MatchId == ticket.MatchId && a.CompletedAt == null, cancellationToken);
                if (finished || stale) return Results.Ok(new { state = "idle", elapsedSeconds = 0, region = allocator.FusionRegion });
            }
            var elapsed = ticket.Status == "searching" ? (int)(DateTimeOffset.UtcNow - ticket.EnqueuedAt).TotalSeconds : 0;
            // QA-029: the search screen shows which region the player is queued in.
            return Results.Ok(new { state, elapsedSeconds = elapsed, matchId = ticket.MatchId, region = allocator.FusionRegion });
        });

        group.MapGet("/ticket", async (HttpContext ctx, HotdropDbContext db, MatchTicketService tickets,
            AllocatorOptions allocator, CancellationToken cancellationToken) =>
        {
            var playerId = PlayerId(ctx);
            var ticket = await db.MatchmakingTickets
                .Where(t => t.PlayerId == playerId && t.Status == "matched")
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (ticket is null) return Results.Ok(new { ticket = (string?)null, note = "no reservation yet" });

            // The Fusion session name is the match id; it exists once a pooled dedicated
            // server has claimed the match (POST /api/matches/claim). Until then the client keeps polling.
            bool serverReady = await db.MatchAllocations.AnyAsync(a => a.MatchId == ticket.MatchId && a.CompletedAt == null, cancellationToken);
            if (!serverReady)
                return Results.Ok(new { ticket = (string?)null, matchId = ticket.MatchId, serverReady, note = "waiting_for_server" });
            var (joinTicket, expiresAt) = tickets.IssueJoinTicket(playerId, ticket.MatchId!.Value, ticket.Team ?? "A", ticket.HasBots);
            return Results.Ok(new
            {
                ticket = joinTicket,
                expiresAt,
                matchId = ticket.MatchId,
                session = ticket.MatchId!.Value.ToString("N"),
                region = allocator.FusionRegion,
                team = ticket.Team,
                hasBots = ticket.HasBots,
                serverReady,
                note = "matched",
            });
        });
    }

    static Guid PlayerId(HttpContext ctx) => Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
}



