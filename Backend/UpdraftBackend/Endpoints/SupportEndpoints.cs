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

        group.MapDelete("/account", async (HttpContext ctx, UpdraftDbContext db, CancellationToken cancellationToken) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            var account = await db.PlayerAccounts.FindAsync([playerId], cancellationToken);
            if (account is null) return Results.NotFound();

            var membership = await db.PartyMembers.SingleOrDefaultAsync(m => m.PlayerId == playerId, cancellationToken);
            if (membership is not null)
            {
                var party = await db.Parties.FindAsync([membership.PartyId], cancellationToken);
                db.PartyMembers.Remove(membership);
                await db.SaveChangesAsync(cancellationToken);
                var remaining = await db.PartyMembers.Where(m => m.PartyId == membership.PartyId).ToListAsync(cancellationToken);
                if (party is not null && remaining.Count == 0) db.Parties.Remove(party);
                else if (party is not null && party.LeaderId == playerId) party.LeaderId = remaining.OrderBy(m => m.JoinedAt).First().PlayerId;
            }

            db.PlayerSessions.RemoveRange(db.PlayerSessions.Where(s => s.PlayerId == playerId));
            db.InventoryItems.RemoveRange(db.InventoryItems.Where(i => i.PlayerId == playerId));
            db.CreditLedgerEntries.RemoveRange(db.CreditLedgerEntries.Where(e => e.PlayerId == playerId));
            db.MatchParticipations.RemoveRange(db.MatchParticipations.Where(m => m.PlayerId == playerId));
            db.PlayerReports.RemoveRange(db.PlayerReports.Where(r => r.ReportingPlayerId == playerId || r.ReportedPlayerId == playerId));
            db.AnalyticsEvents.RemoveRange(db.AnalyticsEvents.Where(e => e.PlayerId == playerId));
            db.PlayerAccounts.Remove(account);

            var requests = await db.AccountDeletionRequests.Where(r => r.PlayerId == playerId).ToListAsync(cancellationToken);
            foreach (var request in requests) request.Status = "completed";
            if (requests.Count == 0)
                db.AccountDeletionRequests.Add(new AccountDeletionRequest { Id = Guid.NewGuid(), PlayerId = playerId, Status = "completed" });

            // Purchase records are deliberately retained for accounting/legal obligations.
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { deleted = true, retainedPurchaseRecords = true });
        }).RequireAuthorization();

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
