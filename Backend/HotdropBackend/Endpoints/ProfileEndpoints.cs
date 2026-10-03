using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;

namespace HotdropBackend.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile").RequireAuthorization();

        group.MapGet("/", async (HttpContext ctx, HotdropDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var account = await db.PlayerAccounts.FindAsync(playerId);
            return account is null ? Results.NotFound() : Results.Ok(new ProfileResponse(
                account.Id, account.Alias, account.ProfileXp, account.Level,
                account.EquippedCosmeticId, account.LinkedPlatformIdentity, account.IsGuest));
        });

        group.MapGet("/wallet", async (HttpContext ctx, HotdropDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var credits = await db.CreditLedgerEntries.Where(e => e.PlayerId == playerId).SumAsync(e => (int?)e.ChangeAmount) ?? 0;
            return Results.Ok(new { credits });
        });

        group.MapGet("/inventory", async (HttpContext ctx, HotdropDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var items = await db.InventoryItems.Where(i => i.PlayerId == playerId).ToListAsync();
            return Results.Ok(new { items });
        });

        group.MapPost("/equip/{cosmeticId}", async (string cosmeticId, HttpContext ctx, HotdropDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var owns = await db.InventoryItems.AnyAsync(i => i.PlayerId == playerId && i.CosmeticId == cosmeticId);
            if (!owns) return Results.Forbid();

            var account = await db.PlayerAccounts.FindAsync(playerId);
            if (account is null) return Results.NotFound();
            account.EquippedCosmeticId = cosmeticId;
            await db.SaveChangesAsync();
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

    public sealed record ProfileResponse(Guid Id, string Alias, int ProfileXp, int Level,
        string? EquippedCosmeticId, string? LinkedPlatformIdentity, bool IsGuest);
}



