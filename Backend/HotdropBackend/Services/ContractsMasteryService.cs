using HotdropBackend.Data;
using HotdropBackend.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotdropBackend.Services;

/// <summary>
/// The server-owned v1 catalog. It intentionally has no reward sets: no cosmetic
/// set may be advertised until its three licensed catalog assets are shipped.
/// </summary>
public static class GoalsCatalog
{
    public const string Version = "v1";
    public static readonly IReadOnlyDictionary<string, int> Contracts = new Dictionary<string, int>
    {
        ["finish_together"] = 2,
        ["hold_the_relay"] = 45,
        ["build_a_team_route"] = 2,
    };
    public static readonly int[] MasteryThresholds = [60, 300, 1200];
    public static readonly int[] RouteThresholds = [3, 12, 40];
    public static readonly int[] WeaponThresholds = [5, 20, 60];
    public static readonly string[] Weapons = ["ARC", "ZIP", "BRICK"];
}

/// <summary>Bounded facts emitted by the authenticated match server, never a client telemetry upload.</summary>
public sealed record AuthoritativeProgressFacts(
    int ValidUncontestedOccupationSeconds = 0,
    IReadOnlyList<PadTeammateUse>? HumanTeammatePadUses = null,
    string? MajorityEquippedWeapon = null);

public sealed record PadTeammateUse(Guid PadOwnerPlayerId, Guid TeammatePlayerId, bool TeammateIsHuman);

public sealed class ContractsMasteryService
{
    public async Task ApplyMatchAsync(Guid matchId, SubmitResultRequest request, IReadOnlyList<ParticipantResult> fresh,
        HotdropDbContext db, int maxUncontestedOccupationSecondsPerMatch, CancellationToken cancellationToken)
    {
        var players = request.Participants.ToDictionary(p => p.PlayerId);
        var now = DateTimeOffset.UtcNow;
        foreach (var participant in fresh)
        {
            if (!participant.Eligible || request.ServerFailed) continue;
            var facts = participant.ProgressFacts;
            if (facts is null) continue;
            int occupation = Math.Clamp(facts.ValidUncontestedOccupationSeconds, 0, maxUncontestedOccupationSecondsPerMatch);
            var qualifyingUses = (facts.HumanTeammatePadUses ?? [])
                .Take(64)
                .Where(u => u.PadOwnerPlayerId == participant.PlayerId && u.TeammatePlayerId != participant.PlayerId && u.TeammateIsHuman
                    && players.TryGetValue(u.TeammatePlayerId, out var teammate) && teammate.Eligible && teammate.IsHuman)
                .Select(u => u.TeammatePlayerId).Distinct().Count();

            await IncrementMastery(db, participant.PlayerId, "relay_specialist", "", occupation, now, cancellationToken);
            await IncrementMastery(db, participant.PlayerId, "route_builder", "", qualifyingUses, now, cancellationToken);
            if (GoalsCatalog.Weapons.Contains(facts.MajorityEquippedWeapon ?? "", StringComparer.Ordinal))
                await IncrementMastery(db, participant.PlayerId, "weapon_familiarity", facts.MajorityEquippedWeapon!, 1, now, cancellationToken);

            var active = await db.ContractGoalInstances.SingleOrDefaultAsync(g => g.PlayerId == participant.PlayerId && g.Status == "active", cancellationToken);
            if (active is null) continue;
            int delta = active.TemplateId switch
            {
                "finish_together" => 1,
                "hold_the_relay" => occupation,
                "build_a_team_route" => qualifyingUses,
                _ => 0,
            };
            if (delta <= 0) continue;
            active.Progress = Math.Min(active.Target, active.Progress + delta);
            if (active.Progress < active.Target) continue;
            active.Status = "completed";
            active.CompletedAt = now;
            // The unique instance and source keys make a retried receipt unable to mint a second seal.
            db.FlightSealGrants.Add(new FlightSealGrant { PlayerId = participant.PlayerId, GoalInstanceId = active.Id, SourceKey = $"contract:{active.Id}", GrantedAt = now });
        }
    }

    static async Task IncrementMastery(HotdropDbContext db, Guid playerId, string track, string weapon, int amount,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (amount <= 0) return;
        var row = await db.MasteryProgresses.FindAsync([playerId, track, weapon], cancellationToken);
        if (row is null) db.MasteryProgresses.Add(new MasteryProgress { PlayerId = playerId, TrackId = track, WeaponId = weapon, Progress = amount, UpdatedAt = now });
        else { row.Progress += amount; row.UpdatedAt = now; }
    }

    public async Task<ContractGoalInstance?> AssignAsync(Guid playerId, string templateId, HotdropDbContext db, CancellationToken cancellationToken)
    {
        if (!GoalsCatalog.Contracts.TryGetValue(templateId, out var target)) return null;
        int eligible = await db.MatchParticipations.CountAsync(m => m.PlayerId == playerId && m.Eligible, cancellationToken);
        if (eligible < 2) return null;
        var active = await db.ContractGoalInstances.SingleOrDefaultAsync(g => g.PlayerId == playerId && g.Status == "active", cancellationToken);
        if (active is not null && active.TemplateId == templateId) return active;
        if (active is not null)
        {
            active.Status = "paused";
            // Flush the filtered unique-index transition before another instance is
            // activated. SQLite and PostgreSQL may otherwise insert/update in an
            // order that briefly creates two active rows for the same player.
            await db.SaveChangesAsync(cancellationToken);
        }
        // Choosing a paused template resumes its same server-generated instance;
        // only a completed template gets a fresh instance on a later assignment.
        var paused = await db.ContractGoalInstances
            .Where(g => g.PlayerId == playerId && g.TemplateId == templateId && g.CatalogVersion == GoalsCatalog.Version && g.Status == "paused")
            .OrderByDescending(g => g.AssignedAt).FirstOrDefaultAsync(cancellationToken);
        if (paused is not null)
        {
            paused.Status = "active";
            await db.SaveChangesAsync(cancellationToken);
            return paused;
        }
        var instance = new ContractGoalInstance { Id = Guid.NewGuid(), PlayerId = playerId, CatalogVersion = GoalsCatalog.Version, TemplateId = templateId, Target = target, Status = "active" };
        db.ContractGoalInstances.Add(instance);
        await db.SaveChangesAsync(cancellationToken);
        return instance;
    }
}
