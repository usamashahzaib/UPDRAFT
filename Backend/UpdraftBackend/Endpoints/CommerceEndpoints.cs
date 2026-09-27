using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Endpoints;

public static class CommerceEndpoints
{
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
            bundles = new[] { "signal", "foundry", "skyline" },
        }));

        group.MapPost("/verify-purchase", async (VerifyPurchaseRequest request, HttpContext ctx, UpdraftDbContext db) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);

            var existing = await db.PurchaseTransactions
                .FirstOrDefaultAsync(t => t.GooglePurchaseToken == request.PurchaseToken);
            if (existing is not null)
                return Results.Ok(new { status = existing.Status, alreadyProcessed = true });

            // TODO(external dependency): call Google Play Developer API to verify
            // request.PurchaseToken against request.ProductId before marking verified.
            // Never mark a purchase verified without that round trip.
            var txn = new PurchaseTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                ProductId = request.ProductId,
                GooglePurchaseToken = request.PurchaseToken,
                Status = "pending_verification",
            };
            db.PurchaseTransactions.Add(txn);
            await db.SaveChangesAsync();

            return Results.Ok(new { status = txn.Status });
        });

        group.MapPost("/restore", async (HttpContext ctx, UpdraftDbContext db) =>
        {
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            var owned = await db.InventoryItems.Where(i => i.PlayerId == playerId).ToListAsync();
            return Results.Ok(owned);
        });
    }

    public record VerifyPurchaseRequest(string ProductId, string PurchaseToken);
}
