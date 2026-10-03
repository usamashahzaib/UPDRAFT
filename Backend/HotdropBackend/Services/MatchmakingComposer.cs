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

    public int RatingWindow(double waitSeconds)
    {
        if (waitSeconds <= RatingWindowExpandAfterSeconds) return RatingWindowBase;
        int steps = (int)((waitSeconds - RatingWindowExpandAfterSeconds) / RatingWindowExpandIntervalSeconds);
        return Math.Min(RatingWindowMax, RatingWindowBase + steps * RatingWindowExpandStep);
    }
}

/// <summary>
/// Composes searching tickets into 3v3 matches. A group is a solo player or a
/// whole party and is never split. Only mirrored compositions are allowed
/// (3 vs 3, duo+solo vs duo+solo, solos vs solos) so a full party is never
/// matched against solo players. The oldest group is always part of the next
/// match so newer groups cannot starve it; after BotFillWaitSeconds it is
/// matched with labeled bots instead.
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
                Assign([oldest], Guid.NewGuid(), "A", hasBots: true, now);
                remaining.RemoveAt(0);
                composed++;
                continue;
            }

            break;
        }

        if (composed > 0) await db.SaveChangesAsync(cancellationToken);
        return composed;
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



