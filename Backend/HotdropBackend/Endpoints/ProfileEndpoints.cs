using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;

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
            if (account is null) return Results.NotFound(new { error = "profile_not_found" });
            BackfillLegacySlot(account);
            return Results.Ok(new ProfileResponse(
                account.Id, account.Alias, account.ProfileXp, account.Level,
                // Adapter: old clients keep reading the single field, which is the body style.
                account.BodyStyleId ?? account.EquippedCosmeticId,
                account.LinkedPlatformIdentity, account.IsGuest,
                account.BodyStyleId ?? account.EquippedCosmeticId,
                account.WeaponWrapId, account.BadgeId, account.BodyRigId));
        });

        group.MapGet("/wallet", async (HttpContext ctx, HotdropDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var credits = await db.CreditLedgerEntries.Where(e => e.PlayerId == playerId).SumAsync(e => (long?)e.ChangeAmount) ?? 0L;
            return Results.Ok(new { credits });
        });

        // Only expose facts recorded for this player. Rating and any unrecorded gameplay
        // telemetry deliberately stay out of the product profile.
        group.MapGet("/stats", async (HttpContext ctx, HotdropDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var matches = db.MatchParticipations.Where(m => m.PlayerId == playerId && m.Eligible);
            return Results.Ok(new
            {
                eligibleMatches = await matches.CountAsync(),
                wins = await matches.CountAsync(m => m.Won),
                draws = await matches.CountAsync(m => m.Draw),
            });
        });

        group.MapGet("/inventory", async (HttpContext ctx, HotdropDbContext db, ProgressionOptions progression) =>
        {
            var playerId = RequireOwnerId(ctx);
            var items = await db.InventoryItems.Where(i => i.PlayerId == playerId).ToListAsync();
            var wrapUnlocks = (progression.LevelUnlocks ?? new Dictionary<int, string>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value.StartsWith("wrap_", StringComparison.Ordinal))
                .OrderBy(x => x.Key)
                .Select(x => new { level = x.Key, cosmeticId = x.Value })
                .ToArray();
            return Results.Ok(new { items, wrapUnlocks });
        });

        // Amendment 3: equip validates category and ownership per slot and writes only that
        // slot, so a body style, weapon wrap and badge can be worn at the same time. A body
        // rig is an allowed catalog choice and never requires ownership. Unknown ids are
        // rejected instead of silently landing in one catch-all slot.
        group.MapPost("/equip/{cosmeticId}", async (string cosmeticId, HttpContext ctx, HotdropDbContext db) =>
        {
            var playerId = RequireOwnerId(ctx);
            var slot = SlotOf(cosmeticId);
            if (slot is null) return Results.BadRequest(new { error = "unknown_cosmetic_category" });

            var account = await db.PlayerAccounts.FindAsync(playerId);
            if (account is null) return Results.NotFound(new { error = "profile_not_found" });

            if (slot != CosmeticSlot.BodyRig)
            {
                int currentRigMask = string.Equals(account.BodyRigId, "rig_female", StringComparison.Ordinal) ?
                    (int)BodyRigVariantMask.Female : (int)BodyRigVariantMask.Male;
                var owns = await db.InventoryItems.AnyAsync(i => i.PlayerId == playerId && i.CosmeticId == cosmeticId &&
                    (slot != CosmeticSlot.BodyStyle || (i.SupportedBodyRigMask & currentRigMask) != 0));
                if (!owns)
                    return Results.Json(new { error = "cosmetic_not_owned" }, statusCode: StatusCodes.Status403Forbidden);
            }
            switch (slot)
            {
                case CosmeticSlot.BodyStyle:
                    account.BodyStyleId = cosmeticId;
                    // Backward-compatible adapter: old clients only understand one value.
                    account.EquippedCosmeticId = cosmeticId;
                    break;
                case CosmeticSlot.WeaponWrap: account.WeaponWrapId = cosmeticId; break;
                case CosmeticSlot.Badge: account.BadgeId = cosmeticId; break;
                case CosmeticSlot.BodyRig: account.BodyRigId = cosmeticId; break;
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { equipped = cosmeticId, category = slot.Value.ToString() });
        });
    }

    enum CosmeticSlot { BodyStyle, WeaponWrap, Badge, BodyRig }

    static CosmeticSlot? SlotOf(string cosmeticId)
    {
        if (string.IsNullOrWhiteSpace(cosmeticId)) return null;
        string id = cosmeticId.ToLowerInvariant();
        // Both catalog naming conventions: "body_teal_stripe" and "signal_body".
        if (id.StartsWith("body_") || id.EndsWith("_body")) return CosmeticSlot.BodyStyle;
        if (id.StartsWith("wrap_") || id.EndsWith("_wrap")) return CosmeticSlot.WeaponWrap;
        if (id.StartsWith("badge_") || id.EndsWith("_badge")) return CosmeticSlot.Badge;
        if (id == "rig_male" || id == "rig_female") return CosmeticSlot.BodyRig;
        return null;
    }

    /// <summary>Accounts written before Amendment 3 keep their one value as the body style.</summary>
    static void BackfillLegacySlot(PlayerAccount account)
    {
        if (string.IsNullOrEmpty(account.BodyStyleId) && !string.IsNullOrEmpty(account.EquippedCosmeticId))
            account.BodyStyleId = account.EquippedCosmeticId;
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
        string? EquippedCosmeticId, string? LinkedPlatformIdentity, bool IsGuest,
        string? BodyStyleId = null, string? WeaponWrapId = null, string? BadgeId = null, string? BodyRigId = null);
}



