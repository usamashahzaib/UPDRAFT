using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using UpdraftBackend.Services;
using Xunit;

namespace UpdraftBackend.Tests;

// Each test owns its database: the composer reads every searching ticket, so a
// shared fixture would let one test's leftover tickets join another test's match.
public class MatchmakingComposerTests
{
    static async Task<List<MatchmakingTicket>> Run(Action<UpdraftDbContext, DateTimeOffset> seed, MatchmakingOptions? options = null)
    {
        using var factory = new UpdraftWebApplicationFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            seed(db, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
        await new MatchmakingComposer(factory.Services.GetRequiredService<IServiceScopeFactory>(), options ?? new MatchmakingOptions())
            .RunOnceAsync(CancellationToken.None);
        using var verify = factory.Services.CreateScope();
        return await verify.ServiceProvider.GetRequiredService<UpdraftDbContext>().MatchmakingTickets.AsNoTracking().ToListAsync();
    }

    static void Add(UpdraftDbContext db, DateTimeOffset at, int rating = 1000, Guid? party = null) =>
        db.MatchmakingTickets.Add(new MatchmakingTicket { Id = Guid.NewGuid(), PlayerId = Guid.NewGuid(), PartyId = party, Rating = rating, EnqueuedAt = at, UpdatedAt = at });

    static void AddParty(UpdraftDbContext db, DateTimeOffset at, int size, int rating = 1000)
    {
        var party = Guid.NewGuid();
        for (int i = 0; i < size; i++) Add(db, at, rating, party);
    }

    [Fact]
    public async Task SixSolos_ComposeBalancedThreeVsThree()
    {
        int[] ratings = [1060, 1040, 1000, 990, 950, 920];
        var tickets = await Run((db, now) => { foreach (var r in ratings) Add(db, now, r); });

        Assert.All(tickets, t => Assert.Equal("matched", t.Status));
        Assert.Single(tickets.Select(t => t.MatchId).Distinct());
        Assert.Equal(3, tickets.Count(t => t.Team == "A"));
        Assert.Equal(3, tickets.Count(t => t.Team == "B"));
        Assert.All(tickets, t => Assert.False(t.HasBots));
        double avgA = tickets.Where(t => t.Team == "A").Average(t => t.Rating);
        double avgB = tickets.Where(t => t.Team == "B").Average(t => t.Rating);
        Assert.True(Math.Abs(avgA - avgB) <= 30, $"team averages {avgA} vs {avgB}");
    }

    [Fact]
    public async Task TwoFullParties_ComposeThreeVsThree_EachPartyOnOwnTeam()
    {
        var tickets = await Run((db, now) => { AddParty(db, now, 3); AddParty(db, now.AddSeconds(1), 3); });

        Assert.All(tickets, t => Assert.Equal("matched", t.Status));
        foreach (var party in tickets.GroupBy(t => t.PartyId))
            Assert.Single(party.Select(t => t.Team).Distinct());
    }

    [Fact]
    public async Task FullParty_IsNeverMatchedAgainstSolos()
    {
        var tickets = await Run((db, now) => { AddParty(db, now, 3); for (int i = 0; i < 3; i++) Add(db, now.AddSeconds(1)); });
        Assert.All(tickets, t => Assert.Equal("searching", t.Status));
    }

    [Fact]
    public async Task DuoPlusSolo_MirroredOnBothTeams()
    {
        var tickets = await Run((db, now) => { AddParty(db, now, 2); AddParty(db, now.AddSeconds(1), 2); Add(db, now.AddSeconds(2)); Add(db, now.AddSeconds(3)); });

        Assert.All(tickets, t => Assert.Equal("matched", t.Status));
        foreach (var team in tickets.GroupBy(t => t.Team))
        {
            Assert.Equal(3, team.Count());
            Assert.Equal(2, team.Count(t => t.PartyId != null));
        }
    }

    [Fact]
    public async Task RatingGapBeyondInitialWindow_DoesNotMatchImmediately()
    {
        var tickets = await Run((db, now) => { Add(db, now, 1000); for (int i = 0; i < 5; i++) Add(db, now, 1300); });
        Assert.All(tickets, t => Assert.Equal("searching", t.Status));
    }

    [Fact]
    public async Task RatingWindow_WidensWithWait_UpToCap()
    {
        var options = new MatchmakingOptions();
        Assert.Equal(150, options.RatingWindow(0));
        Assert.Equal(150, options.RatingWindow(20));
        Assert.Equal(200, options.RatingWindow(30));
        Assert.Equal(250, options.RatingWindow(40));
        Assert.Equal(350, options.RatingWindow(300));

        // Oldest waited 40s -> window 250, so a 1200 group now fits with a 1000 one.
        var tickets = await Run((db, now) => { Add(db, now.AddSeconds(-40), 1000); for (int i = 0; i < 5; i++) Add(db, now, 1200); });
        Assert.All(tickets, t => Assert.Equal("matched", t.Status));
    }

    [Fact]
    public async Task LoneGroup_PastBotFillWait_IsMatchedWithBots()
    {
        var tickets = await Run((db, now) => Add(db, now.AddSeconds(-46)));
        var ticket = Assert.Single(tickets);
        Assert.Equal("matched", ticket.Status);
        Assert.True(ticket.HasBots);
    }

    [Fact]
    public async Task FewerThanSix_BelowBotFillWait_StaysSearching()
    {
        var tickets = await Run((db, now) => { for (int i = 0; i < 3; i++) Add(db, now); });
        Assert.All(tickets, t => Assert.Equal("searching", t.Status));
    }
}
