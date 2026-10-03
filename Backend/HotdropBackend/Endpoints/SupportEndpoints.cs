using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;

namespace HotdropBackend.Endpoints;

public static class SupportEndpoints
{
    // Spec section 23 "Player support" + section 31: in-app account-deletion
    // request and a public web path for the same (required by Google Play
    // policy for apps with account creation).
    public static void MapSupportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/support");

        group.MapPost("/report", async (SubmitReportRequest request, HttpContext ctx, HotdropDbContext db) =>
        {
            var reporterId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            string reason = request.Reason?.Trim() ?? string.Empty;
            string? alias = string.IsNullOrWhiteSpace(request.ReportedAlias) ? null : request.ReportedAlias.Trim();
            // Unity's JsonUtility cannot emit null: "" means "no id" and a negative slot means "no slot".
            int? slot = request.ReportedSlot is >= 0 ? request.ReportedSlot : null;
            if (!TryParseOptionalGuid(request.ReportedPlayerId, out var targetGuid) || !TryParseOptionalGuid(request.MatchId, out var matchId))
                return Results.BadRequest(new { error = "valid_report_target_and_reason_required" });
            bool byGuid = targetGuid is not null;
            bool byMatchIdentity = !byGuid && matchId is not null && (alias is not null || slot is not null);
            if ((!byGuid && !byMatchIdentity) || reason.Length is < 3 or > 500 ||
                alias is { Length: > 64 } || slot is > 63)
                return Results.BadRequest(new { error = "valid_report_target_and_reason_required" });

            Guid reportedId;
            if (byGuid)
            {
                reportedId = targetGuid!.Value;
                bool targetExists = await db.PlayerAccounts.AnyAsync(a => a.Id == reportedId);
                if (!targetExists) return Results.NotFound(new { error = "reported_player_not_found" });
            }
            else
            {
                // QA-025: the client identifies the target by what it shows in-app
                // (alias / slot in a specific match). Only someone who was in that
                // match may resolve its roster.
                var resolved = await ResolveMatchParticipantAsync(db, matchId!.Value, reporterId, alias, slot);
                if (resolved.Error is not null) return resolved.Error;
                reportedId = resolved.PlayerId;
            }
            if (reportedId == reporterId) return Results.BadRequest(new { error = "cannot_report_self" });
            db.PlayerReports.Add(new PlayerReport
            {
                Id = Guid.NewGuid(),
                ReportingPlayerId = reporterId,
                ReportedPlayerId = reportedId,
                Reason = reason,
                MatchId = matchId,
                ReportedAlias = alias,
                ReportedSlot = slot,
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { submitted = true });
        }).RequireAuthorization();

        group.MapPost("/account-deletion", async (HttpContext ctx, HotdropDbContext db, IEmailSender emailSender,
            SupportOptions support, ILoggerFactory loggers) =>
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

            // QA-028: a single-use, expiring link goes to the address; only its hash is
            // stored. A repeat request rotates the link (older links stop working). The
            // email and the page are identical whether or not any account uses this
            // address, so neither reveals account existence.
            var pending = await db.AccountDeletionRequests.FirstOrDefaultAsync(r =>
                r.ContactEmail == email && r.Status == AccountDeletionService.PendingVerification);
            if (pending is null)
            {
                pending = new AccountDeletionRequest
                {
                    Id = Guid.NewGuid(),
                    ContactName = name,
                    ContactEmail = email,
                    Status = AccountDeletionService.PendingVerification,
                };
                db.AccountDeletionRequests.Add(pending);
            }
            string token = AccountDeletionService.NewToken();
            pending.VerificationTokenHash = AccountDeletionService.Hash(token);
            pending.VerificationExpiresAt = DateTimeOffset.UtcNow.AddHours(Math.Clamp(support.DeletionLinkLifetimeHours, 1, 168));
            await db.SaveChangesAsync();

            string baseUrl = string.IsNullOrWhiteSpace(support.PublicBaseUrl)
                ? $"{ctx.Request.Scheme}://{ctx.Request.Host}"
                : support.PublicBaseUrl.TrimEnd('/');
            string link = $"{baseUrl}/api/support/deletion/verify?token={Uri.EscapeDataString(token)}";
            try
            {
                await emailSender.SendAsync(new EmailMessage(email, "Confirm your Project HOTDROP account deletion",
                    "We received a request to delete the Project HOTDROP account associated with this email address.\n\n" +
                    $"To confirm, open this link within {support.DeletionLinkLifetimeHours} hours:\n{link}\n\n" +
                    "The link works once. If you did not make this request, ignore this email and nothing will change.\n"),
                    ctx.RequestAborted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The request is stored; the player can resubmit to get a fresh link.
                loggers.CreateLogger("HotdropBackend.Support").LogError(ex, "Failed to send account-deletion verification email");
            }
            return Results.Content(
                "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>Request received</title>" +
                "<body><main><h1>Request received</h1><p>Check your email for the next verification step.</p></main></body></html>",
                "text/html");
        }).AllowAnonymous().RequireRateLimiting("auth");

        // QA-028: the emailed link. GET only renders a confirmation form (mail
        // scanners and link previews prefetch GETs, which must never delete an
        // account); POST performs the same deletion as the in-app endpoint.
        group.MapGet("/deletion/verify", async (string? token, HotdropDbContext db, AccountDeletionService deletion,
            CancellationToken cancellationToken) =>
        {
            if (!await deletion.IsUsableTokenAsync(token ?? string.Empty, db, cancellationToken))
                return InvalidLinkPage();
            string encoded = System.Net.WebUtility.HtmlEncode(token!);
            return Results.Content(
                "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>Confirm account deletion</title>" +
                "<body><main><h1>Confirm account deletion</h1>" +
                "<p>Deleting removes the Project HOTDROP account associated with this email address, including profile, progression, inventory and credits. This cannot be undone.</p>" +
                "<form method=\"post\" action=\"/api/support/deletion/verify\">" +
                $"<input type=\"hidden\" name=\"token\" value=\"{encoded}\">" +
                "<button type=\"submit\">Delete my account</button></form></main></body></html>",
                "text/html");
        }).AllowAnonymous().RequireRateLimiting("auth");

        group.MapPost("/deletion/verify", async (HttpContext ctx, HotdropDbContext db, AccountDeletionService deletion,
            CancellationToken cancellationToken) =>
        {
            string? token = ctx.Request.Query["token"].ToString();
            if (string.IsNullOrEmpty(token) && ctx.Request.HasFormContentType)
                token = (await ctx.Request.ReadFormAsync(cancellationToken))["token"].ToString();
            if (!await deletion.CompleteVerifiedRequestAsync(token ?? string.Empty, db, cancellationToken))
                return InvalidLinkPage();
            return Results.Content(
                "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>Deletion complete</title>" +
                "<body><main><h1>Deletion request complete</h1>" +
                "<p>Your request has been processed. If a Project HOTDROP account was associated with this email address, it and its player data have been deleted. Purchase records may be retained where required by law.</p>" +
                "</main></body></html>",
                "text/html");
        }).AllowAnonymous().DisableAntiforgery().RequireRateLimiting("auth");

        // QA-028: lets a player register the contact email a later web deletion
        // request will be matched against (stored only as a hash).
        group.MapPut("/contact-email", async (ContactEmailRequest request, HttpContext ctx, HotdropDbContext db,
            CancellationToken cancellationToken) =>
        {
            string email = request.Email?.Trim() ?? string.Empty;
            if (!AccountDeletionService.IsValidEmail(email))
                return Results.BadRequest(new { error = "valid_email_required" });
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            var account = await db.PlayerAccounts.FindAsync([playerId], cancellationToken);
            if (account is null) return Results.NotFound();
            account.ContactEmailHash = AccountDeletionService.HashEmail(email);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { saved = true });
        }).RequireAuthorization();

        group.MapDelete("/contact-email", async (HttpContext ctx, HotdropDbContext db, CancellationToken cancellationToken) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            var account = await db.PlayerAccounts.FindAsync([playerId], cancellationToken);
            if (account is null) return Results.NotFound();
            account.ContactEmailHash = null;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { removed = true });
        }).RequireAuthorization();

        group.MapDelete("/account", async (HttpContext ctx, HotdropDbContext db, AccountDeletionService deletion,
            CancellationToken cancellationToken) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            if (!await deletion.DeleteAccountAsync(playerId, db, cancellationToken)) return Results.NotFound();
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

    /// <summary>
    /// Resolves an in-match alias and/or slot to an account using the match roster:
    /// recorded participations (alias snapshot + slot, after the result) and matched
    /// matchmaking tickets (current alias, while the match is still running).
    /// </summary>
    static async Task<(Guid PlayerId, IResult? Error)> ResolveMatchParticipantAsync(
        HotdropDbContext db, Guid matchId, Guid reporterId, string? alias, int? slot)
    {
        var participations = await db.MatchParticipations.Where(p => p.MatchId == matchId)
            .Select(p => new { p.PlayerId, p.Alias, p.Slot }).ToListAsync();
        var ticketPlayers = await db.MatchmakingTickets.Where(t => t.MatchId == matchId && t.Status == "matched")
            .Select(t => t.PlayerId).ToListAsync();

        bool reporterInMatch = participations.Any(p => p.PlayerId == reporterId) || ticketPlayers.Contains(reporterId);
        if (!reporterInMatch)
            return (Guid.Empty, Results.Json(new { error = "reporter_not_in_match" }, statusCode: StatusCodes.Status403Forbidden));

        var rosterIds = participations.Select(p => p.PlayerId).Union(ticketPlayers).ToList();
        var currentAliases = await db.PlayerAccounts.Where(a => rosterIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Alias);
        var roster = rosterIds.Select(id =>
        {
            var p = participations.FirstOrDefault(x => x.PlayerId == id);
            return new
            {
                PlayerId = id,
                Aliases = new[] { p?.Alias, currentAliases.GetValueOrDefault(id) }.Where(a => !string.IsNullOrEmpty(a)).ToList(),
                Slot = p?.Slot,
            };
        }).Where(r => currentAliases.ContainsKey(r.PlayerId)); // deleted accounts cannot be reported

        var matches = roster
            .Where(r => slot is null || r.Slot == slot)
            .Where(r => alias is null || r.Aliases.Any(a => string.Equals(a, alias, StringComparison.OrdinalIgnoreCase)))
            .Select(r => r.PlayerId).Distinct().ToList();
        return matches.Count == 1
            ? (matches[0], null)
            : (Guid.Empty, Results.NotFound(new { error = "reported_player_not_found" }));
    }

    /// <summary>
    /// Report by GUID (<paramref name="ReportedPlayerId"/>) or, QA-025, by
    /// <paramref name="MatchId"/> plus the in-match <paramref name="ReportedAlias"/>
    /// and/or <paramref name="ReportedSlot"/>. A GUID takes precedence when present.
    /// </summary>
    public record SubmitReportRequest(string? ReportedPlayerId, string Reason, string? MatchId,
        string? ReportedAlias = null, int? ReportedSlot = null);

    public record ContactEmailRequest(string? Email);

    // Same page for unknown, used and expired links.
    static IResult InvalidLinkPage() => Results.Content(
        "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>Link invalid</title>" +
        "<body><main><h1>This link is invalid or has expired</h1>" +
        "<p>Deletion links work once and expire. Submit a new request from the account-deletion page to get a fresh link.</p></main></body></html>",
        "text/html", System.Text.Encoding.UTF8, StatusCodes.Status400BadRequest);

    /// <summary>Null/blank → absent (true, null); a GUID → (true, value); anything else → false.</summary>
    static bool TryParseOptionalGuid(string? value, out Guid? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Guid.TryParse(value, out var g)) return false;
        if (g != Guid.Empty) parsed = g;
        return true;
    }
}




