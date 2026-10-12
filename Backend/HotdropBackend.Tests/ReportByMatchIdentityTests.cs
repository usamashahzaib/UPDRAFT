using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using Xunit;

namespace HotdropBackend.Tests;

/// <summary>QA-025: players report what they can see in-app (match + alias/slot), not a raw GUID.</summary>
public class ReportByMatchIdentityTests : IClassFixture<HotdropWebApplicationFactory>
{
    readonly HotdropWebApplicationFactory _factory;

    public ReportByMatchIdentityTests(HotdropWebApplicationFactory factory) => _factory = factory;

    static string NewAlias(string prefix) => $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    async Task<(Guid reporter, Guid target, string targetAlias, Guid outsider)> SeedAccountsAsync()
    {
        var reporter = Guid.NewGuid();
        var target = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        string targetAlias = NewAlias("Kite");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        db.PlayerAccounts.AddRange(
            new PlayerAccount { Id = reporter, Alias = NewAlias("Rep") },
            new PlayerAccount { Id = target, Alias = targetAlias },
            new PlayerAccount { Id = outsider, Alias = NewAlias("Out") });
        await db.SaveChangesAsync();
        return (reporter, target, targetAlias, outsider);
    }

    async Task SeedMatchedTicketsAsync(Guid matchId, params (Guid player, string team)[] players)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        if (!await db.MatchAllocations.AnyAsync(allocation => allocation.MatchId == matchId))
            db.MatchAllocations.Add(new MatchAllocation { MatchId = matchId, ServerId = HotdropWebApplicationFactory.TestServerId });
        foreach (var (player, team) in players)
            db.MatchmakingTickets.Add(new MatchmakingTicket { Id = Guid.NewGuid(), PlayerId = player, MatchId = matchId, Status = "matched", Team = team });
        await db.SaveChangesAsync();
    }

    async Task<PlayerReport> SingleReportAsync(Guid reporter)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        return await db.PlayerReports.SingleAsync(r => r.ReportingPlayerId == reporter);
    }

    [Fact]
    public async Task Report_BySlot_AfterResult_ResolvesToParticipantAccount()
    {
        var (reporter, target, targetAlias, _) = await SeedAccountsAsync();
        var matchId = Guid.NewGuid();
        await SeedMatchedTicketsAsync(matchId, (reporter, "A"), (target, "B"));
        var server = _factory.CreateGameServerClient();
        var result = await server.PostAsJsonAsync($"/api/matches/{matchId}/result", new
        {
            Draw = false,
            Participants = new object[]
            {
                new { PlayerId = reporter, Won = true, Eligible = true, HadBotSubstitution = false, Team = "A", Slot = 0 },
                new { PlayerId = target, Won = false, Eligible = true, HadBotSubstitution = false, Team = "B", Slot = 3 },
            },
        });
        result.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var row = await db.MatchParticipations.SingleAsync(p => p.MatchId == matchId && p.PlayerId == target);
            Assert.Equal(3, row.Slot);
            Assert.Equal(targetAlias, row.Alias);
        }

        var client = _factory.CreateClientWithPlayerId(reporter);
        var response = await client.PostAsJsonAsync("/api/support/report", new
        {
            reportedPlayerId = "", reason = "griefing teammates", matchId = matchId.ToString(), reportedAlias = "", reportedSlot = 3,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await SingleReportAsync(reporter);
        Assert.Equal(target, report.ReportedPlayerId);
        Assert.Equal(matchId, report.MatchId);
        Assert.Equal(3, report.ReportedSlot);
    }

    [Fact]
    public async Task Report_ByAlias_DuringLiveMatch_ResolvesFromMatchedTickets()
    {
        var (reporter, target, targetAlias, _) = await SeedAccountsAsync();
        var matchId = Guid.NewGuid();
        await SeedMatchedTicketsAsync(matchId, (reporter, "A"), (target, "B"));

        var client = _factory.CreateClientWithPlayerId(reporter);
        var response = await client.PostAsJsonAsync("/api/support/report", new
        {
            reason = "offensive name", matchId, reportedAlias = targetAlias.ToUpperInvariant(), reportedSlot = -1,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await SingleReportAsync(reporter);
        Assert.Equal(target, report.ReportedPlayerId);
        Assert.Equal(targetAlias.ToUpperInvariant(), report.ReportedAlias);
        Assert.Null(report.ReportedSlot);
    }

    [Fact]
    public async Task Report_ByAlias_RejectsReporterWhoWasNotInMatch()
    {
        var (reporter, target, targetAlias, outsider) = await SeedAccountsAsync();
        var matchId = Guid.NewGuid();
        await SeedMatchedTicketsAsync(matchId, (reporter, "A"), (target, "B"));

        var client = _factory.CreateClientWithPlayerId(outsider);
        var response = await client.PostAsJsonAsync("/api/support/report", new { reason = "cheating", matchId, reportedAlias = targetAlias });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        Assert.False(await db.PlayerReports.AnyAsync(r => r.ReportingPlayerId == outsider));
    }

    [Fact]
    public async Task Report_ByAlias_UnknownAliasInMatch_IsNotFound()
    {
        var (reporter, target, _, outsider) = await SeedAccountsAsync();
        var matchId = Guid.NewGuid();
        await SeedMatchedTicketsAsync(matchId, (reporter, "A"), (target, "B"));
        string outsiderAlias;
        using (var scope = _factory.Services.CreateScope())
            outsiderAlias = (await scope.ServiceProvider.GetRequiredService<HotdropDbContext>().PlayerAccounts.FindAsync(outsider))!.Alias;

        var client = _factory.CreateClientWithPlayerId(reporter);
        // An alias that exists, but not in this match, must not resolve.
        var response = await client.PostAsJsonAsync("/api/support/report", new { reason = "cheating", matchId, reportedAlias = outsiderAlias });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Report_ByOwnSlot_IsRejectedAsSelfReport()
    {
        var (reporter, target, _, _) = await SeedAccountsAsync();
        var matchId = Guid.NewGuid();
        await SeedMatchedTicketsAsync(matchId, (reporter, "A"), (target, "B"));
        var server = _factory.CreateGameServerClient();
        (await server.PostAsJsonAsync($"/api/matches/{matchId}/result", new
        {
            Draw = true,
            Participants = new object[]
            {
                new { PlayerId = reporter, Won = false, Eligible = true, HadBotSubstitution = false, Team = "A", Slot = 1 },
                new { PlayerId = target, Won = false, Eligible = true, HadBotSubstitution = false, Team = "B", Slot = 2 },
            },
        })).EnsureSuccessStatusCode();

        var client = _factory.CreateClientWithPlayerId(reporter);
        var response = await client.PostAsJsonAsync("/api/support/report", new { reason = "cheating", matchId, reportedSlot = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Report_ByPrefixedBotAlias_StillResolvesToTheHumanWhoLeft()
    {
        // C19: a client that submits the name it actually saw — "[BOT] Kite-…" — after the
        // player's slot was taken over by a labeled bot must still resolve to that human.
        var (reporter, target, targetAlias, _) = await SeedAccountsAsync();
        var matchId = Guid.NewGuid();
        await SeedMatchedTicketsAsync(matchId, (reporter, "A"), (target, "B"));
        var server = _factory.CreateGameServerClient();
        (await server.PostAsJsonAsync($"/api/matches/{matchId}/result", new
        {
            Draw = false,
            Participants = new object[]
            {
                new { PlayerId = reporter, Won = true, Eligible = true, HadBotSubstitution = false, Team = "A", Slot = 0 },
                new { PlayerId = target, Won = false, Eligible = true, HadBotSubstitution = true, Team = "B", Slot = 4 },
            },
        })).EnsureSuccessStatusCode();

        var client = _factory.CreateClientWithPlayerId(reporter);
        var response = await client.PostAsJsonAsync("/api/support/report", new
        {
            reason = "griefing", matchId = matchId.ToString(), reportedAlias = "[BOT] " + targetAlias,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await SingleReportAsync(reporter);
        Assert.Equal(target, report.ReportedPlayerId);
    }

    [Fact]
    public async Task Report_ByPrefixedBotAlias_DoesNotResolveABotFilledSeatToAnotherPlayer()
    {
        // The prefix must not become a wildcard: an unrelated player's alias still fails.
        var (reporter, target, targetAlias, outsider) = await SeedAccountsAsync();
        var matchId = Guid.NewGuid();
        await SeedMatchedTicketsAsync(matchId, (reporter, "A"), (target, "B"));
        string outsiderAlias;
        using (var scope = _factory.Services.CreateScope())
            outsiderAlias = (await scope.ServiceProvider.GetRequiredService<HotdropDbContext>().PlayerAccounts.FindAsync(outsider))!.Alias;
        Assert.NotEqual(targetAlias, outsiderAlias);

        var client = _factory.CreateClientWithPlayerId(reporter);
        var response = await client.PostAsJsonAsync("/api/support/report", new
        {
            reason = "cheating", matchId = matchId.ToString(), reportedAlias = "[BOT] " + outsiderAlias,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("{\"reason\":\"cheating\"}")] // no target at all
    [InlineData("{\"reason\":\"cheating\",\"reportedAlias\":\"Someone\"}")] // alias without match
    [InlineData("{\"reason\":\"cheating\",\"reportedPlayerId\":\"not-a-guid\"}")]
    [InlineData("{\"reason\":\"no\",\"matchId\":\"6f1c1c62-0000-4000-8000-000000000001\",\"reportedSlot\":2}")] // reason too short
    [InlineData("{\"reason\":\"cheating\",\"matchId\":\"6f1c1c62-0000-4000-8000-000000000001\",\"reportedSlot\":99}")]
    public async Task Report_InvalidTargetOrReason_IsBadRequest(string json)
    {
        var client = _factory.CreateClientWithPlayerId(Guid.NewGuid());
        var response = await client.PostAsync("/api/support/report", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Report_ByGuid_StillWorks_WithEmptyMatchIdFromJsonUtility()
    {
        var (reporter, target, _, _) = await SeedAccountsAsync();
        var client = _factory.CreateClientWithPlayerId(reporter);
        var response = await client.PostAsJsonAsync("/api/support/report", new { reportedPlayerId = target.ToString(), reason = "cheating", matchId = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await SingleReportAsync(reporter);
        Assert.Equal(target, report.ReportedPlayerId);
        Assert.Null(report.MatchId);
    }
}




