using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Endpoints;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches").RequireAuthorization();

        group.MapPost("/{matchId}/reconnect", (Guid matchId, HttpContext ctx) =>
        {
            // TODO: hand back a rejoin ticket for the same avatar in the dedicated
            // server session — requires the Fusion game-server allocator to exist.
            return Results.Ok(new { matchId, reconnectAllowed = true });
        });

        // Spec section 23: "Submit authoritative result — server only." This must
        // be callable only by the dedicated game-server process, authenticated with
        // its own service credential, never by a player client.
        group.MapPost("/{matchId}/result", async (Guid matchId, SubmitResultRequest request, UpdraftDbContext db) =>
        {
            // TODO: authenticate caller as the game server (service credential), not a player session.
            foreach (var p in request.Participants)
            {
                bool alreadyRecorded = await db.MatchParticipations
                    .AnyAsync(m => m.MatchId == matchId && m.PlayerId == p.PlayerId);
                if (alreadyRecorded) continue; // duplicate result submission must not duplicate rewards

                db.MatchParticipations.Add(new MatchParticipation
                {
                    MatchId = matchId,
                    PlayerId = p.PlayerId,
                    Won = p.Won,
                    Draw = request.Draw,
                    Eligible = p.Eligible,
                    HadBotSubstitution = p.HadBotSubstitution,
                });
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { recorded = true });
        }).RequireAuthorization(policy => policy.RequireClaim("service", "game-server"));
    }

    public record ParticipantResult(Guid PlayerId, bool Won, bool Eligible, bool HadBotSubstitution);
    public record SubmitResultRequest(bool Draw, List<ParticipantResult> Participants);
}
