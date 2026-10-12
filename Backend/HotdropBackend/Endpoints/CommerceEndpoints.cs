using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;

namespace HotdropBackend.Endpoints;

public static class CommerceEndpoints
{
    static readonly string[] CatalogProducts = ["signal", "foundry", "skyline"];

    // Spec section 20/28: verify on backend, grant each entitlement exactly
    // once, handle pending transactions, restore, refunds. Payment stays
    // disabled (CatalogConfig.shopEnabled = false) until this full lifecycle
    // passes testing — this scaffold intentionally does not enable it.
    public static void MapCommerceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/commerce").RequireAuthorization();

        group.MapGet("/catalog", () => Results.Ok(new
        {
            enabled = false, // flips true only after section 26's gameplay validation gate passes
            bundles = CatalogProducts,
            supportedBodyRigMask = (int)BodyRigVariantMask.All,
        }));

        group.MapPost("/verify-purchase", async (VerifyPurchaseRequest? request, HttpContext ctx, HotdropDbContext db,
            Microsoft.Extensions.Options.IOptionsMonitor<HotdropBackend.Services.FeatureFlags> features) =>
        {
            if (!features.CurrentValue.PurchasesEnabled)
                return Results.Json(new { error = "purchases_disabled" }, statusCode: StatusCodes.Status503ServiceUnavailable);
            if (request is null || string.IsNullOrWhiteSpace(request.PurchaseToken) ||
                string.IsNullOrWhiteSpace(request.ProductId) ||
                !CatalogProducts.Contains(request.ProductId, StringComparer.Ordinal))
                return Results.BadRequest(new { error = "invalid_purchase_request" });

            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            string? linkedPlatformIdentity = await db.PlayerAccounts.AsNoTracking()
                .Where(account => account.Id == playerId)
                .Select(account => account.LinkedPlatformIdentity)
                .SingleOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(linkedPlatformIdentity))
                return Results.Json(new { error = "linked_google_identity_required" },
                    statusCode: StatusCodes.Status403Forbidden);

            var existing = await db.PurchaseTransactions
                .FirstOrDefaultAsync(t => t.GooglePurchaseToken == request.PurchaseToken);
            if (existing is not null)
                return ExistingPurchaseResult(existing, playerId, request.ProductId);

            // External dependency: Google Play Developer API verification is not
            // configured. Keep the transaction pending; never trust client product
            // or purchase-token claims and never grant an entitlement here.
            var txn = new PurchaseTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                ProductId = request.ProductId,
                GooglePurchaseToken = request.PurchaseToken,
                Status = "pending_verification",
            };
            db.PurchaseTransactions.Add(txn);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique GooglePurchaseToken index is the concurrency arbiter.
                // If another request committed this token after our initial lookup,
                // return the same idempotent result instead of leaking a 500 to the
                // retrying client. Other database failures still propagate.
                db.ChangeTracker.Clear();
                var raced = await db.PurchaseTransactions.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.GooglePurchaseToken == request.PurchaseToken);
                if (raced is null) throw;
                return ExistingPurchaseResult(raced, playerId, request.ProductId);
            }

            return Results.Ok(new { status = txn.Status });
        });

        group.MapPost("/restore", async (HttpContext ctx, HotdropDbContext db) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            var owned = await db.InventoryItems.Where(i => i.PlayerId == playerId).ToListAsync();
            return Results.Ok(owned);
        });
    }

    static IResult ExistingPurchaseResult(PurchaseTransaction transaction, Guid playerId, string productId)
    {
        // A Google purchase token is globally unique. Never reveal another
        // account's transaction state or treat a token submitted for another
        // catalog item as an idempotent retry.
        if (transaction.PlayerId != playerId ||
            !string.Equals(transaction.ProductId, productId, StringComparison.Ordinal))
            return Results.Conflict(new { error = "purchase_token_conflict" });

        return Results.Ok(new { status = transaction.Status, alreadyProcessed = true });
    }

    public record VerifyPurchaseRequest(string ProductId, string PurchaseToken);
}




