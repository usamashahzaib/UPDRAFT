using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Endpoints;

public static class SupportEndpoints
{
    // Spec section 23 "Player support" + section 31: in-app account-deletion
    // request and a public web path for the same (required by Google Play
    // policy for apps with account creation).
    public static void MapSupportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/support");

        group.MapPost("/report", async (SubmitReportRequest request, HttpContext ctx, UpdraftDbContext db) =>
        {
            var reporterId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            db.PlayerReports.Add(new PlayerReport
            {
                Id = Guid.NewGuid(),
                ReportingPlayerId = reporterId,
                ReportedPlayerId = request.ReportedPlayerId,
                Reason = request.Reason,
                MatchId = request.MatchId,
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { submitted = true });
        }).RequireAuthorization();

        group.MapPost("/account-deletion", async (HttpContext ctx, UpdraftDbContext db) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            db.AccountDeletionRequests.Add(new AccountDeletionRequest { Id = Guid.NewGuid(), PlayerId = playerId });
            await db.SaveChangesAsync();
            return Results.Ok(new { requested = true });
        }).RequireAuthorization();

        // Public, unauthenticated path per Play policy — a player must be able
        // to request deletion without the app installed.
        group.MapGet("/account-deletion", () =>
            Results.Content("<html><body>Account deletion request form placeholder.</body></html>", "text/html"));
    }

    public record SubmitReportRequest(Guid ReportedPlayerId, string Reason, Guid? MatchId);
}
