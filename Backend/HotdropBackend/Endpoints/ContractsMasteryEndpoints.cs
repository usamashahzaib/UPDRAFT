using HotdropBackend.Data;
using HotdropBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotdropBackend.Endpoints;

/// <summary>Read and selection APIs are feature-gated separately from match submission.</summary>
public static class ContractsMasteryEndpoints
{
    public static void MapContractsMasteryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goals").RequireAuthorization();
        group.MapGet("/", async (HttpContext ctx, HotdropDbContext db, IOptionsMonitor<FeatureFlags> flags, CancellationToken ct) =>
        {
            if (!TryPlayer(ctx, out var playerId)) return Results.Forbid();
            if (!flags.CurrentValue.ContractsMasteryEnabled) return Results.NotFound();
            int eligible = await db.MatchParticipations.CountAsync(m => m.PlayerId == playerId && m.Eligible, ct);
            var goals = await db.ContractGoalInstances.Where(g => g.PlayerId == playerId).OrderByDescending(g => g.AssignedAt).ToListAsync(ct);
            var mastery = await db.MasteryProgresses.Where(m => m.PlayerId == playerId).ToListAsync(ct);
            int seals = await db.FlightSealGrants.CountAsync(s => s.PlayerId == playerId, ct);
            return Results.Ok(new { catalogVersion = GoalsCatalog.Version, unlocked = eligible >= 2, eligiblePublicMatches = eligible,
                templates = GoalsCatalog.Contracts.Select(x => new { id = x.Key, target = x.Value }), goals, seals,
                rewardSets = Array.Empty<object>(), rewardCatalogComplete = false, mastery,
                relayThresholds = GoalsCatalog.MasteryThresholds, routeThresholds = GoalsCatalog.RouteThresholds, weaponThresholds = GoalsCatalog.WeaponThresholds });
        });
        group.MapPost("/contracts/{templateId}", async (string templateId, HttpContext ctx, HotdropDbContext db, ContractsMasteryService service, IOptionsMonitor<FeatureFlags> flags, CancellationToken ct) =>
        {
            if (!TryPlayer(ctx, out var playerId)) return Results.Forbid();
            if (!flags.CurrentValue.ContractsMasteryEnabled) return Results.NotFound();
            var goal = await service.AssignAsync(playerId, templateId, db, ct);
            return goal is null ? Results.Conflict(new { error = "contracts_locked_or_invalid" }) : Results.Ok(goal);
        });
    }

    static bool TryPlayer(HttpContext ctx, out Guid playerId) => Guid.TryParse(ctx.User.FindFirst("player_id")?.Value, out playerId);
}
