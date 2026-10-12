using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;

namespace HotdropBackend.Services;

/// <summary>Spec section 13 matchmaking tunables, bound from the "Matchmaking" configuration section.</summary>
public sealed class MatchmakingOptions
{
    public const int TeamSize = 3;
    public const int MatchSize = TeamSize * 2;

    public int RatingWindowBase { get; set; } = 150;
    public int RatingWindowExpandAfterSeconds { get; set; } = 20;
    public int RatingWindowExpandStep { get; set; } = 50;
    public int RatingWindowExpandIntervalSeconds { get; set; } = 10;
    public int RatingWindowMax { get; set; } = 350;
    public int BotFillWaitSeconds { get; set; } = 45;
    public int PollIntervalSeconds { get; set; } = 2;

    public void Validate()
    {
        if (RatingWindowBase < 0 || RatingWindowExpandAfterSeconds < 0 ||
            RatingWindowExpandStep <= 0 || RatingWindowExpandIntervalSeconds <= 0 ||
            RatingWindowMax <= RatingWindowBase)
            throw new InvalidOperationException("Matchmaking rating-window settings must use a nonnegative base and delay, positive expansion step and interval, and a maximum above the base.");
        if (BotFillWaitSeconds <= 0)
            throw new InvalidOperationException("Matchmaking:BotFillWaitSeconds must be positive; zero would fill every new queue immediately.");
        if (PollIntervalSeconds <= 0)
            throw new InvalidOperationException("Matchmaking:PollIntervalSeconds must be positive.");
    }

    public int RatingWindow(double waitSeconds)
    {
        if (waitSeconds <= RatingWindowExpandAfterSeconds) return RatingWindowBase;
        long steps = (long)((waitSeconds - RatingWindowExpandAfterSeconds) / RatingWindowExpandIntervalSeconds);
        long remainingWindow = (long)RatingWindowMax - RatingWindowBase;
        long stepsToCap = (remainingWindow + RatingWindowExpandStep - 1L) / RatingWindowExpandStep;
        if (steps >= stepsToCap) return RatingWindowMax;
        return RatingWindowBase + (int)(steps * RatingWindowExpandStep);
    }
}

/// <summary>
/// Composes searching tickets into 3v3 matches. A group is a solo player or a
/// whole party and is never split. Only mirrored compositions are allowed
/// (3 vs 3, duo+solo vs duo+solo, solos vs solos) so a full party is never
/// matched against solo players. The oldest group is always part of the next
/// match so newer groups cannot starve it; after BotFillWaitSeconds it is
/// seated with as many other waiting groups as the team size allows and the
/// remaining slots are filled with labeled bots, so one 3v3 still starts
/// instead of one match per waiting player.
/// </summary>
public sealed class MatchmakingComposer(IServiceScopeFactory scopeFactory, MatchmakingOptions options)
{
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var now = DateTimeOffset.UtcNow;

        var tickets = await db.MatchmakingTickets.Where(t => t.Status == "searching").ToListAsync(cancellationToken);
        if (tickets.Count == 0) return 0;

        var remaining = tickets
            .GroupBy(t => t.PartyId?.ToString() ?? t.PlayerId.ToString())
            .Select(g => new TicketGroup(g.ToList(), g.Min(t => t.EnqueuedAt), (int)Math.Round(g.Average(t => t.Rating))))
            .Where(g => g.Size <= MatchmakingOptions.TeamSize)
            .OrderBy(g => g.EnqueuedAt)
            .ToList();

        int composed = 0;
        while (remaining.Count > 0)
        {
            var oldest = remaining[0];
            double waited = (now - oldest.EnqueuedAt).TotalSeconds;
            int window = options.RatingWindow(waited);
            var pool = remaining.Skip(1).Where(g => Math.Abs(g.Rating - oldest.Rating) <= window).ToList();

            if (TryCompose(oldest, pool, out var teamA, out var teamB))
            {
                var matchId = Guid.NewGuid();
                Assign(teamA, matchId, "A", hasBots: false, now);
                Assign(teamB, matchId, "B", hasBots: false, now);
                foreach (var g in teamA.Concat(teamB)) remaining.Remove(g);
                composed++;
                continue;
            }

            if (waited >= options.BotFillWaitSeconds)
            {
                var (fillA, fillB) = BotFillTeams(oldest, pool);
                var matchId = Guid.NewGuid();
                foreach (var g in fillA) Assign([g], matchId, "A", hasBots: true, now);
                foreach (var g in fillB) Assign([g], matchId, "B", hasBots: true, now);
                foreach (var g in fillA.Concat(fillB)) remaining.Remove(g);
                composed++;
                continue;
            }

            break;
        }

        if (composed > 0) await db.SaveChangesAsync(cancellationToken);
        return composed;
    }

    /// <summary>
    /// Bot fill keeps as many waiting humans in the lobby as the 3v3 allows, so a
    /// four-player queue becomes 2v2 humans plus two bots rather than four separate
    /// one-human-versus-five-bots matches. Groups are never split and the empty
    /// seats are left to the match server, which spawns labeled bots for them.
    /// </summary>
    static (List<TicketGroup> TeamA, List<TicketGroup> TeamB) BotFillTeams(TicketGroup oldest, List<TicketGroup> pool)
    {
        var teamA = new List<TicketGroup> { oldest };
        var teamB = new List<TicketGroup>();
        // Nearest rating first, then longest waiting: the strongest available humans play.
        foreach (var g in pool.OrderBy(g => Math.Abs(g.Rating - oldest.Rating)).ThenBy(g => g.EnqueuedAt))
        {
            int freeA = MatchmakingOptions.TeamSize - teamA.Sum(x => x.Size);
            int freeB = MatchmakingOptions.TeamSize - teamB.Sum(x => x.Size);
            if (freeA <= 0 && freeB <= 0) break;
            if (freeA >= freeB ? freeA >= g.Size : freeB >= g.Size) (freeA >= freeB ? teamA : teamB).Add(g);
        }
        return (teamA, teamB);
    }

    static void Assign(IEnumerable<TicketGroup> groups, Guid matchId, string team, bool hasBots, DateTimeOffset now)
    {
        foreach (var ticket in groups.SelectMany(g => g.Tickets))
        {
            ticket.Status = "matched";
            ticket.MatchId = matchId;
            ticket.Team = team;
            ticket.HasBots = hasBots;
            ticket.UpdatedAt = now;
        }
    }

    static bool TryCompose(TicketGroup oldest, List<TicketGroup> pool, out List<TicketGroup> teamA, out List<TicketGroup> teamB)
    {
        teamA = []; teamB = [];
        var ones = pool.Where(g => g.Size == 1).ToList();
        var twos = pool.Where(g => g.Size == 2).ToList();
        var threes = pool.Where(g => g.Size == 3).ToList();

        switch (oldest.Size)
        {
            case 3 when threes.Count >= 1:
                teamA = [oldest]; teamB = [threes[0]];
                return true;
            case 2 when twos.Count >= 1 && ones.Count >= 2:
                return BalancedDuoSolo([oldest, twos[0]], [ones[0], ones[1]], out teamA, out teamB);
            case 1 when ones.Count >= 5:
                return BalancedSolos([oldest, .. ones.Take(5)], out teamA, out teamB);
            case 1 when twos.Count >= 2 && ones.Count >= 1:
                return BalancedDuoSolo([twos[0], twos[1]], [oldest, ones[0]], out teamA, out teamB);
            default:
                return false;
        }
    }

    // Pair the stronger duo with the weaker solo to keep team averages close.
    static bool BalancedDuoSolo(List<TicketGroup> duos, List<TicketGroup> solos, out List<TicketGroup> teamA, out List<TicketGroup> teamB)
    {
        var d = duos.OrderByDescending(g => g.Rating).ToList();
        var s = solos.OrderBy(g => g.Rating).ToList();
        teamA = [d[0], s[0]]; teamB = [d[1], s[1]];
        return true;
    }

    // Snake draft by rating: A B B A A B.
    static bool BalancedSolos(List<TicketGroup> solos, out List<TicketGroup> teamA, out List<TicketGroup> teamB)
    {
        var sorted = solos.OrderByDescending(g => g.Rating).ToList();
        int[] pattern = [0, 1, 1, 0, 0, 1];
        teamA = []; teamB = [];
        for (int i = 0; i < sorted.Count; i++) (pattern[i] == 0 ? teamA : teamB).Add(sorted[i]);
        return true;
    }

    sealed class TicketGroup(List<MatchmakingTicket> tickets, DateTimeOffset enqueuedAt, int rating)
    {
        public List<MatchmakingTicket> Tickets { get; } = tickets;
        public DateTimeOffset EnqueuedAt { get; } = enqueuedAt;
        public int Rating { get; } = rating;
        public int Size => Tickets.Count;
    }
}



