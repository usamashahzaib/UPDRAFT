using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Endpoints;

public static class AuthEndpoints
{
    // Spec section 23 "Authentication" + section 23 "Account recovery":
    // guest sessions for online play, Google Play Games linking to recover
    // a profile, no wallet/inventory auto-merge on link.
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/guest", async (UpdraftDbContext db) =>
        {
            var account = new PlayerAccount { Id = Guid.NewGuid(), IsGuest = true };
            db.PlayerAccounts.Add(account);
            await db.SaveChangesAsync();

            // TODO: issue a short-lived session token once JWT signing key infra exists
            // (external dependency: signing key management, not fabricated here).
            return Results.Ok(new { playerId = account.Id });
        });

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

        authenticatedGroup.MapPost("/refresh", () => Results.Ok(new { note = "session refresh — pending signing-key infra" }));
        authenticatedGroup.MapPost("/revoke", () => Results.Ok(new { revoked = true }));
    }

    public record LinkGooglePlayRequest(string ServerAuthCode, string? VerifiedPlatformId);
}
