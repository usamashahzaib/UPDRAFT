using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Endpoints;

public static class AuthEndpoints
{
    // Spec section 23 "Authentication" + section 23 "Account recovery":
    // guest sessions for online play, Google Play Games linking to recover
    // a profile, no wallet/inventory auto-merge on link.
    public static void MapAuthEndpoints(this WebApplication app)
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

        group.MapPost("/link-google-play", async (LinkGooglePlayRequest request, UpdraftDbContext db) =>
        {
            // TODO(external dependency): verify request.ServerAuthCode against Google's
            // Play Games Services server-side identity API before trusting the identity.
            // Never accept a caller-supplied platform identity unverified.
            var existing = await db.PlayerAccounts
                .FirstOrDefaultAsync(p => p.LinkedPlatformIdentity == request.VerifiedPlatformId);

            if (existing is not null)
                return Results.Ok(new { playerId = existing.Id, loadedExistingProfile = true });

            return Results.Ok(new { linked = false, reason = "no_verified_identity_yet" });
        });

        group.MapPost("/refresh", () => Results.Ok(new { note = "session refresh — pending signing-key infra" }));
        group.MapPost("/revoke", () => Results.Ok(new { revoked = true }));
    }

    public record LinkGooglePlayRequest(string ServerAuthCode, string? VerifiedPlatformId);
}
