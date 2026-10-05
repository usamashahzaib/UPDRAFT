using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;

namespace HotdropBackend.Services;

/// <summary>Spec sections 13 and 18. Mirrors the Unity MatchmakingConfig/ProgressionConfig defaults.</summary>
public sealed class ProgressionOptions
{
    public int EloK { get; set; } = 24;
    public int XpPerEligibleMatch { get; set; } = 120;
    public int XpWinBonus { get; set; } = 30;
    public int XpDrawBonus { get; set; } = 15;
    public int XpPerLevel { get; set; } = 400;
    public int MaxLevel { get; set; } = 20;
    public int CreditsPerEligibleMatch { get; set; } = 20;
    public int CreditsWinBonus { get; set; } = 5;
    public int CreditsPerLevelUp { get; set; } = 20;
    public string[] StarterCosmetics { get; set; } = ["body_standard", "wrap_standard", "badge_rookie"];
    public int UnlockAfterEligibleMatches { get; set; } = 3;
    public string MatchCountUnlock { get; set; } = "body_teal_stripe";
    // Amendment 1: one permanent badge after the first eligible public match, win or loss.
    public string FirstMatchBadge { get; set; } = "badge_first_flight";
    public Dictionary<int, string> LevelUnlocks { get; set; } = new()
    {
        [3] = "wrap_hazard", [6] = "badge_pathfinder", [10] = "body_rooftop", [15] = "wrap_updraft", [20] = "badge_summit",
    };

    public int LevelForXp(int xp) => Math.Min(MaxLevel, 1 + xp / XpPerLevel);
}

/// <param name="Slot">QA-025: the server-assigned in-match slot shown to other players (optional for older servers).</param>
public sealed record ParticipantResult(Guid PlayerId, bool Won, bool Eligible, bool HadBotSubstitution, string? Team = null, int? Slot = null,
    bool IsHuman = true, AuthoritativeProgressFacts? ProgressFacts = null);

public sealed record SubmitResultRequest(bool Draw, List<ParticipantResult> Participants, bool ServerFailed = false, bool HasBots = false);

public sealed record PlayerResultSummary(Guid PlayerId, int XpGained, int CreditsGained, int Level, int? RatingDelta, IReadOnlyList<string> Unlocked);

/// <summary>
/// Applies an authoritative match result exactly once per (match, player).
/// Rating uses team-average Elo and is skipped for bot-filled, bot-substituted
/// and server-failed matches. Rewards go through the credit ledger, whose
/// unique (player, reason, source) index rejects a replayed grant.
/// </summary>
public sealed class MatchResultProcessor(ProgressionOptions options, ContractsMasteryService contractsMastery)
{
    public async Task<List<PlayerResultSummary>> ApplyAsync(Guid matchId, SubmitResultRequest request, HotdropDbContext db, bool contractsMasteryEnabled, CancellationToken cancellationToken)
    {
        var ids = request.Participants.Select(p => p.PlayerId).Distinct().ToList();
        var recorded = (await db.MatchParticipations
            .Where(m => m.MatchId == matchId && ids.Contains(m.PlayerId))
            .Select(m => m.PlayerId).ToListAsync(cancellationToken)).ToHashSet();
        var fresh = request.Participants.Where(p => !recorded.Contains(p.PlayerId)).DistinctBy(p => p.PlayerId).ToList();
        if (fresh.Count == 0) return [];

        var accounts = await db.PlayerAccounts.Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);
        var ratingDeltas = ComputeRatingDeltas(request, accounts);
        var summaries = new List<PlayerResultSummary>();
        var now = DateTimeOffset.UtcNow;

        foreach (var p in fresh)
        {
            bool eligible = p.Eligible && !request.ServerFailed;
            var participation = new MatchParticipation
            {
                MatchId = matchId, PlayerId = p.PlayerId, Won = p.Won && !request.Draw, Draw = request.Draw,
                Eligible = eligible, HadBotSubstitution = p.HadBotSubstitution, CompletedAt = now,
                Alias = accounts.TryGetValue(p.PlayerId, out var named) ? named.Alias : null,
                Slot = p.Slot is >= 0 and < 64 ? p.Slot : null,
            };
            db.MatchParticipations.Add(participation);

            if (!accounts.TryGetValue(p.PlayerId, out var account)) continue;

            int? delta = ratingDeltas.TryGetValue(p.PlayerId, out var d) ? d : null;
            if (delta is int change) account.HiddenMatchmakingRating += change;
            if (!eligible)
            {
                summaries.Add(new PlayerResultSummary(p.PlayerId, 0, 0, account.Level, delta, []));
                continue;
            }

            string source = matchId.ToString();
            int xp = options.XpPerEligibleMatch + (request.Draw ? options.XpDrawBonus : p.Won ? options.XpWinBonus : 0);
            int credits = options.CreditsPerEligibleMatch;
            AddLedger(db, p.PlayerId, options.CreditsPerEligibleMatch, "match_completion", source, now);
            if (p.Won && !request.Draw)
            {
                credits += options.CreditsWinBonus;
                AddLedger(db, p.PlayerId, options.CreditsWinBonus, "win_bonus", source, now);
            }

            int oldLevel = options.LevelForXp(account.ProfileXp);
            account.ProfileXp += xp;
            int newLevel = options.LevelForXp(account.ProfileXp);
            account.Level = newLevel;

            var unlocked = new List<string>();
            for (int level = oldLevel + 1; level <= newLevel; level++)
            {
                credits += options.CreditsPerLevelUp;
                AddLedger(db, p.PlayerId, options.CreditsPerLevelUp, "level_up", $"level:{level}", now);
                if (options.LevelUnlocks.TryGetValue(level, out var cosmetic)) unlocked.Add(cosmetic);
            }

            int priorEligible = await db.MatchParticipations.CountAsync(m => m.PlayerId == p.PlayerId && m.Eligible, cancellationToken);
            if (priorEligible + 1 == options.UnlockAfterEligibleMatches) unlocked.Add(options.MatchCountUnlock);
            // Amendment 1: first eligible public completion (win or loss) grants the badge once.
            if (priorEligible + 1 == 1 && !string.IsNullOrEmpty(options.FirstMatchBadge))
                unlocked.Add(options.FirstMatchBadge);
            foreach (var cosmetic in unlocked) await GrantAsync(db, p.PlayerId, cosmetic, "unlock", cancellationToken);

            participation.XpGained = xp;
            participation.CreditsGained = credits;
            participation.UnlockedCsv = unlocked.Count > 0 ? string.Join(',', unlocked) : null;
            summaries.Add(new PlayerResultSummary(p.PlayerId, xp, credits, newLevel, delta, unlocked));
        }

        if (contractsMasteryEnabled) await contractsMastery.ApplyMatchAsync(matchId, request, fresh, db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return summaries;
    }

    public async Task GrantStarterSetAsync(Guid playerId, HotdropDbContext db, CancellationToken cancellationToken)
    {
        foreach (var cosmetic in options.StarterCosmetics) await GrantAsync(db, playerId, cosmetic, "starter", cancellationToken);
    }

    Dictionary<Guid, int> ComputeRatingDeltas(SubmitResultRequest request, Dictionary<Guid, PlayerAccount> accounts)
    {
        var deltas = new Dictionary<Guid, int>();
        if (request.ServerFailed || request.HasBots || request.Participants.Any(p => p.HadBotSubstitution)) return deltas;
        var teams = request.Participants.Where(p => p.Team != null && accounts.ContainsKey(p.PlayerId)).GroupBy(p => p.Team!).ToList();
        if (teams.Count != 2) return deltas;

        double Avg(IGrouping<string, ParticipantResult> t) => t.Average(p => accounts[p.PlayerId].HiddenMatchmakingRating);
        foreach (var team in teams)
        {
            var other = teams.First(t => t.Key != team.Key);
            double expected = 1.0 / (1.0 + Math.Pow(10, (Avg(other) - Avg(team)) / 400.0));
            double score = request.Draw ? 0.5 : team.Any(p => p.Won) ? 1.0 : 0.0;
            int change = (int)Math.Round(options.EloK * (score - expected));
            foreach (var p in team) deltas[p.PlayerId] = change;
        }
        return deltas;
    }

    static void AddLedger(HotdropDbContext db, Guid playerId, int amount, string reason, string source, DateTimeOffset at) =>
        db.CreditLedgerEntries.Add(new CreditLedgerEntry { PlayerId = playerId, ChangeAmount = amount, Reason = reason, SourceId = source, CreatedAt = at });

    static async Task GrantAsync(HotdropDbContext db, Guid playerId, string cosmetic, string via, CancellationToken cancellationToken)
    {
        bool owned = await db.InventoryItems.AnyAsync(i => i.PlayerId == playerId && i.CosmeticId == cosmetic, cancellationToken)
            || db.InventoryItems.Local.Any(i => i.PlayerId == playerId && i.CosmeticId == cosmetic);
        if (!owned) db.InventoryItems.Add(new InventoryItem { PlayerId = playerId, CosmeticId = cosmetic, AcquiredVia = via });
    }
}



