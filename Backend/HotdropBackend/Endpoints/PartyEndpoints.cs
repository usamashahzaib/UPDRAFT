using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;

namespace HotdropBackend.Endpoints;

public static class PartyEndpoints
{
    public static void MapPartyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/party").RequireAuthorization();

        group.MapPost("/create", async (HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            var playerId = PlayerId(ctx);
            var existingMembership = await db.PartyMembers.SingleOrDefaultAsync(m => m.PlayerId == playerId, cancellationToken);
            if (existingMembership is not null)
            {
                var existingParty = await db.Parties.SingleAsync(p => p.Id == existingMembership.PartyId, cancellationToken);
                return Results.Conflict(new { error = "already_in_party", code = existingParty.Code });
            }

            var party = new Party { Id = Guid.NewGuid(), Code = await GenerateUniqueInviteCode(db, cancellationToken), LeaderId = playerId };
            db.Parties.Add(party);
            db.PartyMembers.Add(new PartyMember { PartyId = party.Id, PlayerId = playerId });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(await ToResponse(party, db, cancellationToken));
        });

        group.MapPost("/{code}/join", async (string code, HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            var party = await FindParty(code, db, cancellationToken);
            if (party is null) return Results.NotFound();

            var playerId = PlayerId(ctx);
            var existingMembership = await db.PartyMembers.SingleOrDefaultAsync(m => m.PlayerId == playerId, cancellationToken);
            if (existingMembership is not null)
            {
                if (existingMembership.PartyId == party.Id) return Results.Ok(await ToResponse(party, db, cancellationToken));
                return Results.Conflict(new { error = "already_in_party" });
            }

            if (await db.PartyMembers.CountAsync(m => m.PartyId == party.Id, cancellationToken) >= 3)
                return Results.BadRequest(new { error = "party_full" });

            db.PartyMembers.Add(new PartyMember { PartyId = party.Id, PlayerId = playerId });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(await ToResponse(party, db, cancellationToken));
        });

        group.MapPost("/{code}/leave", async (string code, HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            var party = await FindParty(code, db, cancellationToken);
            if (party is null) return Results.NotFound();

            var playerId = PlayerId(ctx);
            var member = await db.PartyMembers.SingleOrDefaultAsync(m => m.PartyId == party.Id && m.PlayerId == playerId, cancellationToken);
            if (member is null) return Results.Forbid();

            db.PartyMembers.Remove(member);
            await db.SaveChangesAsync(cancellationToken);
            var remaining = (await db.PartyMembers.Where(m => m.PartyId == party.Id).ToListAsync(cancellationToken))
                .OrderBy(m => m.JoinedAt).ToList();
            if (remaining.Count == 0)
            {
                db.Parties.Remove(party);
                await db.SaveChangesAsync(cancellationToken);
                return Results.Ok(new PartyResponse(code.ToUpperInvariant(), playerId.ToString(), [], [], []));
            }

            if (party.LeaderId == playerId)
            {
                party.LeaderId = remaining[0].PlayerId;
                await db.SaveChangesAsync(cancellationToken);
            }
            return Results.Ok(await ToResponse(party, db, cancellationToken));
        });

        group.MapPost("/{code}/ready", async (string code, [Microsoft.AspNetCore.Mvc.FromBody] bool ready,
            HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            var party = await FindParty(code, db, cancellationToken);
            if (party is null) return Results.NotFound();
            var playerId = PlayerId(ctx);
            var member = await db.PartyMembers.SingleOrDefaultAsync(m => m.PartyId == party.Id && m.PlayerId == playerId, cancellationToken);
            if (member is null) return Results.Forbid();
            member.IsReady = ready;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(await ToResponse(party, db, cancellationToken));
        });

        group.MapGet("/current", async (HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            var playerId = PlayerId(ctx);
            var membership = await db.PartyMembers.SingleOrDefaultAsync(m => m.PlayerId == playerId, cancellationToken);
            if (membership is null) return Results.NotFound();
            var party = await db.Parties.SingleAsync(p => p.Id == membership.PartyId, cancellationToken);
            return Results.Ok(await ToResponse(party, db, cancellationToken));
        });
    }

    static Guid PlayerId(HttpContext ctx) => Guid.Parse(ctx.User.FindFirst("player_id")!.Value);

    static Task<Party?> FindParty(string code, HotdropDbContext db, CancellationToken cancellationToken) =>
        db.Parties.SingleOrDefaultAsync(p => p.Code == code.ToUpper(), cancellationToken);

    static async Task<PartyResponse> ToResponse(Party party, HotdropDbContext db, CancellationToken cancellationToken)
    {
        var members = (await db.PartyMembers.Where(m => m.PartyId == party.Id).ToListAsync(cancellationToken))
            .OrderBy(m => m.JoinedAt).ToList();
        var ids = members.Select(m => m.PlayerId).ToList();
        var aliases = await db.PlayerAccounts.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Alias, cancellationToken);
        return new PartyResponse(
            party.Code,
            party.LeaderId.ToString(),
            members.Select(m => m.PlayerId.ToString()).ToList(),
            members.Where(m => m.IsReady).Select(m => m.PlayerId.ToString()).ToList(),
            members.Select(m => new PartyMemberResponse(m.PlayerId.ToString(), aliases.GetValueOrDefault(m.PlayerId, "Player"), m.IsReady)).ToList());
    }

    static async Task<string> GenerateUniqueInviteCode(HotdropDbContext db, CancellationToken cancellationToken)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var bytes = RandomNumberGenerator.GetBytes(8);
            var code = new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
            if (!await db.Parties.AnyAsync(p => p.Code == code, cancellationToken)) return code;
        }
        throw new InvalidOperationException("Could not allocate a unique party code.");
    }

    public sealed record PartyMemberResponse(string PlayerId, string Alias, bool IsReady);
    public sealed record PartyResponse(string Code, string LeaderId, List<string> MemberIds, List<string> ReadyIds, List<PartyMemberResponse> Members);
}



