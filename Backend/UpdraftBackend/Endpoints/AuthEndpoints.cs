using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using UpdraftBackend.Services;

namespace UpdraftBackend.Endpoints;

public static class AuthEndpoints
{
    // Spec section 23 "Authentication" + section 23 "Account recovery":
    // guest sessions for online play, Google Play Games linking to recover
    // a profile, no wallet/inventory auto-merge on link.
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/guest", async (UpdraftDbContext db, AuthTokenService tokens, MatchResultProcessor progression,
            CancellationToken cancellationToken) =>
        {
            var account = new PlayerAccount { Id = Guid.NewGuid(), IsGuest = true, Alias = AliasGenerator.Next() };
            db.PlayerAccounts.Add(account);
            await progression.GrantStarterSetAsync(account.Id, db, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            var session = await tokens.IssueAsync(account.Id, db, cancellationToken);
            return Results.Ok(session);
        }).RequireRateLimiting("auth");

        group.MapPost("/refresh", async (RefreshSessionRequest request, UpdraftDbContext db,
            AuthTokenService tokens, CancellationToken cancellationToken) =>
        {
            var session = await tokens.RefreshAsync(request.RefreshToken, db, cancellationToken);
            return session is null ? Results.Unauthorized() : Results.Ok(session);
        }).RequireRateLimiting("auth");

        // Dedicated game servers exchange their configured secret for a short-lived
        // service token. Fails closed when no secret is configured.
        group.MapPost("/server-token", (HttpContext ctx, MatchTicketService tickets, ServerTokenRequest request) =>
        {
            if (!tickets.ServerAuthConfigured)
                return Results.Json(new { error = "server_auth_not_configured" }, statusCode: StatusCodes.Status503ServiceUnavailable);
            if (!tickets.IsValidServerKey(ctx.Request.Headers["X-Server-Key"].ToString())) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.ServerId) || request.ServerId.Length > 64) return Results.BadRequest();
            return Results.Ok(new { token = tickets.IssueServerToken(request.ServerId), expiresInSeconds = (int)MatchTicketService.ServerTokenLifetime.TotalSeconds });
        }).RequireRateLimiting("auth");

        // QA-026 account recovery: after a reinstall the client has no session.
        // It exchanges a fresh Play Games server auth code for a session on the
        // account already linked to that identity. Never creates an account and
        // never merges wallets; an unlinked (or revoked) identity is simply not found,
        // and the client should fall back to /guest.
        group.MapPost("/platform-signin", async (PlatformSignInRequest request, UpdraftDbContext db,
            IGooglePlayGamesIdentityVerifier verifier, Microsoft.Extensions.Options.IOptions<GooglePlayGamesOptions> options,
            AuthTokenService tokens, CancellationToken cancellationToken) =>
        {
            if (!options.Value.IsConfigured)
                return Results.Json(new { error = "google_play_games_not_configured" }, statusCode: StatusCodes.Status503ServiceUnavailable);
            if (string.IsNullOrWhiteSpace(request.ServerAuthCode) || request.ServerAuthCode.Length > 4096)
                return Results.BadRequest(new { error = "server_auth_code_required" });

            string? platformId = await verifier.VerifyPlayerIdAsync(request.ServerAuthCode, cancellationToken);
            if (platformId is null)
                return Results.Json(new { error = "google_identity_verification_failed" }, statusCode: StatusCodes.Status401Unauthorized);

            var account = await db.PlayerAccounts.SingleOrDefaultAsync(a => a.LinkedPlatformIdentity == platformId, cancellationToken);
            if (account is null) return Results.NotFound(new { error = "no_linked_account" });

            var session = await tokens.IssueAsync(account.Id, db, cancellationToken);
            return Results.Ok(session);
        }).RequireRateLimiting("auth");

        // Linking, refreshing, and revoking all act on an existing session
        // (spec section 23: "ownership from validated session only") — unlike
        // /guest, which is the entry point before any session exists. A
        // genuinely separate group is required here: calling
        // RequireAuthorization() on `group` itself would retroactively apply
        // to /guest too, since route group conventions apply to every endpoint
        // mapped on that builder regardless of registration order.
        var authenticatedGroup = app.MapGroup("/api/auth").RequireAuthorization();

        authenticatedGroup.MapPost("/link-google-play", async (LinkGooglePlayRequest request, HttpContext ctx,
            UpdraftDbContext db, IGooglePlayGamesIdentityVerifier verifier, Microsoft.Extensions.Options.IOptions<GooglePlayGamesOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (!options.Value.IsConfigured)
                return Results.Json(new { error = "google_play_games_not_configured" }, statusCode: StatusCodes.Status503ServiceUnavailable);
            if (string.IsNullOrWhiteSpace(request.ServerAuthCode) || request.ServerAuthCode.Length > 4096)
                return Results.BadRequest(new { error = "server_auth_code_required" });

            string? platformId = await verifier.VerifyPlayerIdAsync(request.ServerAuthCode, cancellationToken);
            if (platformId is null)
                return Results.Json(new { error = "google_identity_verification_failed" }, statusCode: StatusCodes.Status401Unauthorized);

            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            var account = await db.PlayerAccounts.FindAsync([playerId], cancellationToken);
            if (account is null) return Results.NotFound();
            if (account.LinkedPlatformIdentity is not null && account.LinkedPlatformIdentity != platformId)
                return Results.Conflict(new { error = "account_already_linked" });
            bool ownedByOther = await db.PlayerAccounts.AnyAsync(a => a.Id != playerId && a.LinkedPlatformIdentity == platformId, cancellationToken);
            if (ownedByOther) return Results.Conflict(new { error = "google_identity_already_linked" });

            account.LinkedPlatformIdentity = platformId;
            account.IsGuest = false;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { linked = true });
        });

        authenticatedGroup.MapDelete("/google-play", async (HttpContext ctx, UpdraftDbContext db, CancellationToken cancellationToken) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            var account = await db.PlayerAccounts.FindAsync([playerId], cancellationToken);
            if (account is null) return Results.NotFound();
            account.LinkedPlatformIdentity = null;
            account.IsGuest = true;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { revoked = true });
        });

        authenticatedGroup.MapPost("/revoke", async (HttpContext ctx, UpdraftDbContext db,
            AuthTokenService tokens, CancellationToken cancellationToken) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            await tokens.RevokePlayerAsync(playerId, db, cancellationToken);
            return Results.Ok(new { revoked = true });
        });
    }

    public record LinkGooglePlayRequest(string ServerAuthCode);
    public record PlatformSignInRequest(string ServerAuthCode);
    public record RefreshSessionRequest(string RefreshToken);
    public record ServerTokenRequest(string ServerId);
}
