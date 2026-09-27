using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;

namespace UpdraftBackend.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/profile").RequireAuthorization();

        group.MapGet("/", async (HttpContext ctx, UpdraftDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var account = await db.PlayerAccounts.FindAsync(playerId);
            return account is null ? Results.NotFound() : Results.Ok(account);
        });

        group.MapGet("/inventory", async (HttpContext ctx, UpdraftDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var items = await db.InventoryItems.Where(i => i.PlayerId == playerId).ToListAsync();
            return Results.Ok(items);
        });

        group.MapPost("/equip/{cosmeticId}", async (string cosmeticId, HttpContext ctx, UpdraftDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var owns = await db.InventoryItems.AnyAsync(i => i.PlayerId == playerId && i.CosmeticId == cosmeticId);
            if (!owns) return Results.Forbid();

            // TODO: persist equipped-appearance state once the Appearance data model is added.
            return Results.Ok(new { equipped = cosmeticId });
        });
    }

    /// <summary>
    /// Spec section 23: "All authenticated requests must derive account ownership
    /// from the validated session, not a freely supplied player_id."
    /// </summary>
    static Guid RequireOwnerId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("player_id")?.Value
            ?? throw new UnauthorizedAccessException("No validated session player_id claim.");
        return Guid.Parse(claim);
    }
}
