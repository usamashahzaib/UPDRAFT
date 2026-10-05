using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;
using Xunit;

namespace HotdropBackend.Tests;

/// <summary>
/// C04: the dedicated server is the only producer of progress facts. These tests use a
/// factory with the contracts/mastery feature flag ON (its real staging configuration) and
/// post the exact PascalCase JSON the Unity server's JsonUtility serialises, proving the
/// authenticated result path turns bounded facts into contracts/mastery progress exactly once.
/// </summary>
public sealed class ContractsProgressFactsFactory : HotdropWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Features:ContractsMasteryEnabled", "true");
    }
}

public class ProgressFactsContractTests : IClassFixture<ContractsProgressFactsFactory>
{
    readonly ContractsProgressFactsFactory factory;
    public ProgressFactsContractTests(ContractsProgressFactsFactory factory) => this.factory = factory;

    [Fact]
    public async Task ServerProgressFacts_AdvanceMasteryAndContract_ExactlyOnce()
    {
        var owner = Guid.NewGuid();
        var teammate = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.AddRange(new PlayerAccount { Id = owner }, new PlayerAccount { Id = teammate });
            // Contracts unlock at two eligible public matches; seed them for the owner.
            db.MatchParticipations.AddRange(
                new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = owner, Eligible = true },
                new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = owner, Eligible = true });
            await db.SaveChangesAsync();
            var service = scope.ServiceProvider.GetRequiredService<ContractsMasteryService>();
            Assert.NotNull(await service.AssignAsync(owner, "build_a_team_route", db, CancellationToken.None));
        }

        var payload = $$"""
        {
          "Draw": false,
          "Participants": [
            { "PlayerId": "{{owner}}", "Won": true, "Eligible": true, "HadBotSubstitution": false, "Team": "A", "Slot": 0,
              "ProgressFacts": { "ValidUncontestedOccupationSeconds": 12, "MajorityEquippedWeapon": "ARC",
                "HumanTeammatePadUses": [ { "PadOwnerPlayerId": "{{owner}}", "TeammatePlayerId": "{{teammate}}", "TeammateIsHuman": true } ] } },
            { "PlayerId": "{{teammate}}", "Won": true, "Eligible": true, "HadBotSubstitution": false, "Team": "A", "Slot": 1 }
          ],
          "ServerFailed": false,
          "HasBots": false
        }
        """;
        var server = factory.CreateGameServerClient();
        async Task<HttpResponseMessage> Post() =>
            await server.PostAsync($"/api/matches/{matchId}/result", new StringContent(payload, Encoding.UTF8, "application/json"));

        var first = await Post();
        first.EnsureSuccessStatusCode();
        await AssertProgress(owner, occupationSeconds: 12, routeUses: 1, arc: 1);

        // The dedicated server's durable outbox retries the identical submission; progress must not double.
        (await Post()).EnsureSuccessStatusCode();
        await AssertProgress(owner, occupationSeconds: 12, routeUses: 1, arc: 1);
    }

    [Fact]
    public async Task IneligibleParticipantFacts_DoNotAdvanceProgress()
    {
        var owner = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = owner });
            await db.SaveChangesAsync();
        }

        var payload = $$"""
        {
          "Draw": false,
          "Participants": [
            { "PlayerId": "{{owner}}", "Won": true, "Eligible": false, "HadBotSubstitution": true, "Team": "A", "Slot": 0,
              "ProgressFacts": { "ValidUncontestedOccupationSeconds": 90, "MajorityEquippedWeapon": "ZIP" } }
          ],
          "ServerFailed": false,
          "HasBots": false
        }
        """;
        var response = await factory.CreateGameServerClient()
            .PostAsync($"/api/matches/{matchId}/result", new StringContent(payload, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        Assert.False(await verifyDb.MasteryProgresses.AnyAsync(m => m.PlayerId == owner));
        Assert.False(await verifyDb.ContractGoalInstances.AnyAsync(g => g.PlayerId == owner));
    }

    async Task AssertProgress(Guid owner, int occupationSeconds, int routeUses, int arc)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        Assert.Equal(occupationSeconds, (await db.MasteryProgresses.FindAsync([owner, "relay_specialist", ""]))!.Progress);
        Assert.Equal(arc, (await db.MasteryProgresses.FindAsync([owner, "weapon_familiarity", "ARC"]))!.Progress);
        Assert.Equal(routeUses, (await db.ContractGoalInstances.SingleAsync(g => g.PlayerId == owner && g.TemplateId == "build_a_team_route")).Progress);
    }
}