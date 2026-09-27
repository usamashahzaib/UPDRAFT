using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Endpoints;

public static class SupportEndpoints
{
    // Spec section 23 "Player support" + section 31: in-app account-deletion
    // request and a public web path for the same (required by Google Play
    // policy for apps with account creation).
    public static void MapSupportEndpoints(this IEndpointRouteBuilder app)
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
            var playerClaim = ctx.User.FindFirst("player_id")?.Value;
            if (Guid.TryParse(playerClaim, out var playerId))
            {
                db.AccountDeletionRequests.Add(new AccountDeletionRequest
                {
                    Id = Guid.NewGuid(),
                    PlayerId = playerId,
                    Status = "requested",
                });
                await db.SaveChangesAsync();
                return Results.Ok(new { requested = true });
            }

            if (!ctx.Request.HasFormContentType)
                return Results.Unauthorized();

            var form = await ctx.Request.ReadFormAsync();
            string name = form["name"].ToString().Trim();
            string email = form["email"].ToString().Trim();
            if (name.Length is < 1 or > 120 || email.Length is < 3 or > 254 ||
                !System.Net.Mail.MailAddress.TryCreate(email, out _))
            {
                return Results.BadRequest(new { error = "valid_name_and_email_required" });
            }

            bool alreadyPending = await db.AccountDeletionRequests.AnyAsync(r =>
                r.ContactEmail == email && r.Status == "pending_verification");
            if (!alreadyPending)
            {
                db.AccountDeletionRequests.Add(new AccountDeletionRequest
                {
                    Id = Guid.NewGuid(),
                    ContactName = name,
                    ContactEmail = email,
                    Status = "pending_verification",
                });
            }
            await db.SaveChangesAsync();
            return Results.Content(
                "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>Request received</title>" +
                "<body><main><h1>Request received</h1><p>Check your email for the next verification step.</p></main></body></html>",
                "text/html");
        }).AllowAnonymous();

        // Public, unauthenticated path per Play policy — a player must be able
        // to request deletion without the app installed.
        group.MapGet("/account-deletion", () =>
        {
            string path = Path.Combine(AppContext.BaseDirectory, "account-deletion.html");
            return File.Exists(path)
                ? Results.File(path, "text/html")
                : Results.Problem("Account-deletion page is unavailable.", statusCode: 503);
        }).AllowAnonymous();
    }

    public record SubmitReportRequest(Guid ReportedPlayerId, string Reason, Guid? MatchId);
}
