using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotdropBackend.Tests;

public class ContractsMasteryTests : IClassFixture<HotdropWebApplicationFactory>
{
    readonly HotdropWebApplicationFactory factory;
    public ContractsMasteryTests(HotdropWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Contracts_AreLockedUntilTwoEligiblePublicReceipts()
    {
        var player = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        db.PlayerAccounts.Add(new PlayerAccount { Id = player });
        db.MatchParticipations.Add(new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = player, Eligible = true });
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<ContractsMasteryService>();
        Assert.Null(await service.AssignAsync(player, "finish_together", db, CancellationToken.None));
        db.MatchParticipations.Add(new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = player, Eligible = true });
        await db.SaveChangesAsync();
        var goal = await service.AssignAsync(player, "finish_together", db, CancellationToken.None);
        Assert.NotNull(goal);
        Assert.Equal("active", goal!.Status);
        Assert.Equal(2, goal.Target);
    }

    [Fact]
    public async Task PausedContractAndBotOrInvalidUses_DoNotAdvance_WhileValidHumanUseDoes()
    {
        var owner = Guid.NewGuid(); var human = Guid.NewGuid(); var bot = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        db.PlayerAccounts.AddRange(new PlayerAccount { Id = owner }, new PlayerAccount { Id = human }, new PlayerAccount { Id = bot });
        for (var i = 0; i < 2; i++) db.MatchParticipations.Add(new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = owner, Eligible = true });
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<ContractsMasteryService>();
        var goal = (await service.AssignAsync(owner, "build_a_team_route", db, CancellationToken.None))!;
        goal.Status = "paused";
        await db.SaveChangesAsync();
        var invalid = new ParticipantResult(owner, false, false, false, ProgressFacts: new AuthoritativeProgressFacts(200,
            [new PadTeammateUse(owner, owner, true), new PadTeammateUse(owner, bot, false)], "NOT_A_WEAPON"));
        await service.ApplyMatchAsync(Guid.NewGuid(), new SubmitResultRequest(false, [invalid]), [invalid], db, CancellationToken.None);
        await db.SaveChangesAsync();
        Assert.Equal(0, (await db.ContractGoalInstances.FindAsync(goal.Id))!.Progress);
        Assert.False(await db.MasteryProgresses.AnyAsync(m => m.PlayerId == owner && m.TrackId == "route_builder"));

        goal.Status = "active";
        await db.SaveChangesAsync();
        var valid = invalid with { Eligible = true, ProgressFacts = new AuthoritativeProgressFacts(10, [new PadTeammateUse(owner, human, true)], "ARC") };
        var teammate = new ParticipantResult(human, false, true, false, IsHuman: true);
        await service.ApplyMatchAsync(Guid.NewGuid(), new SubmitResultRequest(false, [valid, teammate]), [valid, teammate], db, CancellationToken.None);
        await db.SaveChangesAsync();
        Assert.Equal(1, (await db.ContractGoalInstances.FindAsync(goal.Id))!.Progress);
        Assert.Equal(10, (await db.MasteryProgresses.FindAsync([owner, "relay_specialist", ""]))!.Progress);
        Assert.Equal(1, (await db.MasteryProgresses.FindAsync([owner, "weapon_familiarity", "ARC"]))!.Progress);
    }

    [Fact]
    public async Task Switching_ResumesPausedInstance_AndCompletionMintsOneSeal()
    {
        var player = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        db.PlayerAccounts.Add(new PlayerAccount { Id = player });
        db.MatchParticipations.AddRange(
            new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = player, Eligible = true },
            new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = player, Eligible = true });
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<ContractsMasteryService>();
        var first = (await service.AssignAsync(player, "finish_together", db, CancellationToken.None))!;
        first.Progress = 1;
        await service.AssignAsync(player, "hold_the_relay", db, CancellationToken.None);
        var resumed = (await service.AssignAsync(player, "finish_together", db, CancellationToken.None))!;
        Assert.Equal(first.Id, resumed.Id);
        Assert.Equal(1, resumed.Progress);
        var participant = new ParticipantResult(player, false, true, false, ProgressFacts: new AuthoritativeProgressFacts());
        await service.ApplyMatchAsync(Guid.NewGuid(), new SubmitResultRequest(false, [participant]), [participant], db, CancellationToken.None);
        await db.SaveChangesAsync();
        Assert.Equal("completed", resumed.Status);
        Assert.Single(await db.FlightSealGrants.Where(s => s.GoalInstanceId == resumed.Id).ToListAsync());
    }
}
