using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;
using Xunit;

namespace HotdropBackend.Tests;

public class MatchResultProgressionTests : IClassFixture<HotdropWebApplicationFactory>
{
    readonly HotdropWebApplicationFactory _factory;
    public MatchResultProgressionTests(HotdropWebApplicationFactory factory) => _factory = factory;

    async Task<List<Guid>> SeedPlayers(int count, int rating = 1000, int xp = 0)
    {
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        foreach (var id in ids)
            db.PlayerAccounts.Add(new PlayerAccount { Id = id, HiddenMatchmakingRating = rating, ProfileXp = xp, Level = 1 + xp / 400 });
        await db.SaveChangesAsync();
        return ids;
    }

    static SubmitResultRequest ThreeVsThree(List<Guid> ids, bool teamAWins, bool draw = false, bool hasBots = false) => new(
        draw,
        ids.Select((id, i) => new ParticipantResult(id, !draw && (i < 3) == teamAWins, true, false, i < 3 ? "A" : "B")).ToList(),
        HasBots: hasBots);

    async Task Submit(Guid matchId, SubmitResultRequest request) =>
        (await _factory.CreateGameServerClient().PostAsJsonAsync($"/api/matches/{matchId}/result", request)).EnsureSuccessStatusCode();

    async Task<(PlayerAccount account, int credits)> Load(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var account = await db.PlayerAccounts.AsNoTracking().SingleAsync(a => a.Id == id);
        int credits = await db.CreditLedgerEntries.Where(e => e.PlayerId == id).SumAsync(e => e.ChangeAmount);
        return (account, credits);
    }

    [Fact]
    public async Task EvenMatch_WinnersGainTwelve_LosersLoseTwelve()
    {
        var ids = await SeedPlayers(6);
        await Submit(Guid.NewGuid(), ThreeVsThree(ids, teamAWins: true));

        Assert.Equal(1012, (await Load(ids[0])).account.HiddenMatchmakingRating);
        Assert.Equal(988, (await Load(ids[5])).account.HiddenMatchmakingRating);
    }

    [Fact]
    public async Task BotFilledMatch_DoesNotChangeRating_ButStillRewards()
    {
        var ids = await SeedPlayers(6);
        await Submit(Guid.NewGuid(), ThreeVsThree(ids, teamAWins: true, hasBots: true));

        var (account, credits) = await Load(ids[0]);
        Assert.Equal(1000, account.HiddenMatchmakingRating);
        Assert.Equal(150, account.ProfileXp);
        Assert.Equal(25, credits);
    }

    [Fact]
    public async Task WinDrawLoss_GrantSpecifiedXpAndCredits()
    {
        var ids = await SeedPlayers(6);
        await Submit(Guid.NewGuid(), ThreeVsThree(ids, teamAWins: true));
        Assert.Equal((150, 25), ((await Load(ids[0])).account.ProfileXp, (await Load(ids[0])).credits));
        Assert.Equal((120, 20), ((await Load(ids[4])).account.ProfileXp, (await Load(ids[4])).credits));

        var drawIds = await SeedPlayers(6);
        await Submit(Guid.NewGuid(), ThreeVsThree(drawIds, teamAWins: false, draw: true));
        Assert.Equal((135, 20), ((await Load(drawIds[1])).account.ProfileXp, (await Load(drawIds[1])).credits));
    }

    [Fact]
    public async Task LevelUp_GrantsCreditsAndLevelUnlock_Once()
    {
        var ids = await SeedPlayers(6, xp: 790); // level 2, 10 XP short of level 3
        var matchId = Guid.NewGuid();
        await Submit(matchId, ThreeVsThree(ids, teamAWins: true));
        await Submit(matchId, ThreeVsThree(ids, teamAWins: true)); // duplicate result

        var (account, credits) = await Load(ids[0]);
        Assert.Equal(3, account.Level);
        Assert.Equal(940, account.ProfileXp);
        Assert.Equal(45, credits);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        Assert.Single(await db.InventoryItems.Where(i => i.PlayerId == ids[0] && i.CosmeticId == "wrap_hazard").ToListAsync());
    }

    [Fact]
    public async Task LevelCapsAtTwenty()
    {
        var options = new ProgressionOptions();
        Assert.Equal(1, options.LevelForXp(0));
        Assert.Equal(2, options.LevelForXp(400));
        Assert.Equal(20, options.LevelForXp(7600));
        Assert.Equal(20, options.LevelForXp(1_000_000));
    }

    [Fact]
    public async Task ThirdEligibleMatch_UnlocksMatchCountCosmetic()
    {
        var ids = await SeedPlayers(6);
        for (int i = 0; i < 3; i++) await Submit(Guid.NewGuid(), ThreeVsThree(ids, teamAWins: i % 2 == 0));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        Assert.True(await db.InventoryItems.AnyAsync(i => i.PlayerId == ids[2] && i.CosmeticId == "body_teal_stripe"));
    }

    [Fact]
    public async Task ServerFailedMatch_IsIneligibleAndUnrated()
    {
        var ids = await SeedPlayers(6);
        var request = ThreeVsThree(ids, teamAWins: true) with { ServerFailed = true };
        await Submit(Guid.NewGuid(), request);

        var (account, credits) = await Load(ids[0]);
        Assert.Equal(1000, account.HiddenMatchmakingRating);
        Assert.Equal(0, account.ProfileXp);
        Assert.Equal(0, credits);
    }

    [Fact]
    public async Task GuestAccount_GetsAliasAndStarterSet()
    {
        var response = await _factory.CreateUnauthenticatedClient().PostAsync("/api/auth/guest", null);
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<IssuedSession>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var account = await db.PlayerAccounts.SingleAsync(a => a.Id == session!.PlayerId);
        Assert.Matches(@"^[A-Z][a-z]+[A-Z][a-z]+-\d{4}$", account.Alias);
        Assert.Equal(3, await db.InventoryItems.CountAsync(i => i.PlayerId == account.Id && i.AcquiredVia == "starter"));
    }
}




