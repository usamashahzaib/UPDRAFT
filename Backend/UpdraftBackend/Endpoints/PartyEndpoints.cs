using System.Security.Cryptography;

namespace UpdraftBackend.Endpoints;

public static class PartyEndpoints
{
    // Spec section 14: party of up to 3, joined via 8-character invite code,
    // no contact upload, leader starts matchmaking, all members must be ready,
    // party survives results and requeueing.
    //
    // In-memory placeholder store — real implementation needs a shared cache
    // (e.g. the same Postgres instance or Redis) so any backend instance can
    // serve any party. Not wired up yet; documented as a TODO, not faked.
    static readonly Dictionary<string, PartyState> Parties = new();

    public static void MapPartyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/party").RequireAuthorization();

        group.MapPost("/create", (HttpContext ctx) =>
        {
            var code = GenerateInviteCode();
            var leaderId = ctx.User.FindFirst("player_id")!.Value;
            Parties[code] = new PartyState(code, leaderId);
            return Results.Ok(Parties[code]);
        });

        group.MapPost("/{code}/join", (string code, HttpContext ctx) =>
        {
            if (!Parties.TryGetValue(code, out var party)) return Results.NotFound();
            if (party.MemberIds.Count >= 3) return Results.BadRequest(new { error = "party_full" });

            var playerId = ctx.User.FindFirst("player_id")!.Value;
            party.MemberIds.Add(playerId);
            return Results.Ok(party);
        });

        group.MapPost("/{code}/leave", (string code, HttpContext ctx) =>
        {
            if (!Parties.TryGetValue(code, out var party)) return Results.NotFound();
            var playerId = ctx.User.FindFirst("player_id")!.Value;
            party.MemberIds.Remove(playerId);
            party.ReadyIds.Remove(playerId);
            return Results.Ok(party);
        });

        // A bare `bool ready` parameter binds from the query string, not a JSON
        // body — no realistic client sends it that way. Bind explicitly from body.
        group.MapPost("/{code}/ready", (string code, [Microsoft.AspNetCore.Mvc.FromBody] bool ready, HttpContext ctx) =>
        {
            if (!Parties.TryGetValue(code, out var party)) return Results.NotFound();
            var playerId = ctx.User.FindFirst("player_id")!.Value;
            if (ready) party.ReadyIds.Add(playerId); else party.ReadyIds.Remove(playerId);
            return Results.Ok(party);
        });
    }

    static string GenerateInviteCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous chars
        var bytes = RandomNumberGenerator.GetBytes(8);
        return new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
    }

    public class PartyState
    {
        public string Code { get; }
        public string LeaderId { get; }
        public List<string> MemberIds { get; } = new();
        public HashSet<string> ReadyIds { get; } = new();

        public PartyState(string code, string leaderId)
        {
            Code = code;
            LeaderId = leaderId;
            MemberIds.Add(leaderId);
        }
    }
}
