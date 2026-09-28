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

        // Linking, refreshing, and revoking all act on an existing session
        // (spec section 23: "ownership from validated session only") — unlike
        // /guest, which is the entry point before any session exists. A
        // genuinely separate group is required here: calling
        // RequireAuthorization() on `group` itself would retroactively apply
        // to /guest too, since route group conventions apply to every endpoint
        // mapped on that builder regardless of registration order.
        var authenticatedGroup = app.MapGroup("/api/auth").RequireAuthorization();

        authenticatedGroup.MapPost("/link-google-play", (LinkGooglePlayRequest request) =>
        {
            // TODO(external dependency): verify request.ServerAuthCode against Google's
            // Play Games Services server-side identity API before trusting the identity.
            // Never accept a caller-supplied platform identity unverified.
            return Results.StatusCode(StatusCodes.Status501NotImplemented);
        });

        authenticatedGroup.MapPost("/revoke", async (HttpContext ctx, UpdraftDbContext db,
            AuthTokenService tokens, CancellationToken cancellationToken) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            await tokens.RevokePlayerAsync(playerId, db, cancellationToken);
            return Results.Ok(new { revoked = true });
        });
    }

    public record LinkGooglePlayRequest(string ServerAuthCode, string? VerifiedPlatformId);
    public record RefreshSessionRequest(string RefreshToken);
    public record ServerTokenRequest(string ServerId);
}
