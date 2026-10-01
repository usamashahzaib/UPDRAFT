using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using Xunit;

namespace UpdraftBackend.Tests;

/// <summary>QA-017: authoritative per-player reward receipt, and idempotent result re-submission.</summary>
public class MatchReceiptTests : IClassFixture<UpdraftWebApplicationFactory>
{
    readonly UpdraftWebApplicationFactory _factory;

    public MatchReceiptTests(UpdraftWebApplicationFactory factory) => _factory = factory;

    async Task<(Guid a, Guid b)> SeedPlayersAsync(int profileXpA = 0)
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        db.PlayerAccounts.AddRange(
            new PlayerAccount { Id = a, Alias = "RcA-" + a.ToString("N")[..8], ProfileXp = profileXpA, Level = 1 + profileXpA / 400 },
            new PlayerAccount { Id = b, Alias = "RcB-" + b.ToString("N")[..8] });
        await db.SaveChangesAsync();
        return (a, b);
    }

    static object Result(Guid a, Guid b, bool aEligible = true) => new
    {
        Draw = false,
        Participants = new object[]
        {
            new { PlayerId = a, Won = true, Eligible = aEligible, HadBotSubstitution = false, Team = "A", Slot = 0 },
            new { PlayerId = b, Won = false, Eligible = true, HadBotSubstitution = false, Team = "B", Slot = 1 },
        },
    };

    async Task<JsonElement> ReceiptAsync(Guid player, Guid matchId)
    {
        var response = await _factory.CreateClientWithPlayerId(player).GetAsync($"/api/matches/{matchId}/receipt");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Receipt_BeforeResult_IsNotRecorded_WithExactShape()
    {
        var (a, _) = await SeedPlayersAsync();
        var receipt = await ReceiptAsync(a, Guid.NewGuid());

        Assert.Equal(
            new[] { "recorded", "eligible", "xpGained", "creditsGained", "level", "unlocked" },
            receipt.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.False(receipt.GetProperty("recorded").GetBoolean());
        Assert.False(receipt.GetProperty("eligible").GetBoolean());
        Assert.Equal(0, receipt.GetProperty("xpGained").GetInt32());
        Assert.Equal(0, receipt.GetProperty("creditsGained").GetInt32());
        Assert.Equal(1, receipt.GetProperty("level").GetInt32());
        Assert.Equal(0, receipt.GetProperty("unlocked").GetArrayLength());
    }

    [Fact]
    public async Task Receipt_AfterResult_MatchesWhatWasGranted()
    {
        var (a, _) = await SeedPlayersAsync(profileXpA: 700); // a win crosses into level 3 (unlocks wrap_hazard)
        var matchId = Guid.NewGuid();
        var submit = await _factory.CreateGameServerClient().PostAsJsonAsync($"/api/matches/{matchId}/result", Result(a, Guid.NewGuid()));
        submit.EnsureSuccessStatusCode();
        var summary = (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("players").EnumerateArray()
            .Single(p => p.GetProperty("playerId").GetGuid() == a);

        var receipt = await ReceiptAsync(a, matchId);

        Assert.True(receipt.GetProperty("recorded").GetBoolean());
        Assert.True(receipt.GetProperty("eligible").GetBoolean());
        Assert.Equal(summary.GetProperty("xpGained").GetInt32(), receipt.GetProperty("xpGained").GetInt32());
        Assert.Equal(summary.GetProperty("creditsGained").GetInt32(), receipt.GetProperty("creditsGained").GetInt32());
        Assert.Equal(3, receipt.GetProperty("level").GetInt32());
        Assert.Contains("wrap_hazard", receipt.GetProperty("unlocked").EnumerateArray().Select(u => u.GetString()));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        int ledger = await db.CreditLedgerEntries.Where(e => e.PlayerId == a).SumAsync(e => e.ChangeAmount);
        Assert.Equal(ledger, receipt.GetProperty("creditsGained").GetInt32());
    }

    [Fact]
    public async Task Receipt_ForIneligibleParticipant_IsRecordedWithNoRewards()
    {
        var (a, b) = await SeedPlayersAsync();
        var matchId = Guid.NewGuid();
        (await _factory.CreateGameServerClient().PostAsJsonAsync($"/api/matches/{matchId}/result", Result(a, b, aEligible: false))).EnsureSuccessStatusCode();

        var receipt = await ReceiptAsync(a, matchId);
        Assert.True(receipt.GetProperty("recorded").GetBoolean());
        Assert.False(receipt.GetProperty("eligible").GetBoolean());
        Assert.Equal(0, receipt.GetProperty("xpGained").GetInt32());
        Assert.Equal(0, receipt.GetProperty("creditsGained").GetInt32());
        Assert.Equal(0, receipt.GetProperty("unlocked").GetArrayLength());
    }

    [Fact]
    public async Task Receipt_OnlyEverShowsTheCallersOwnParticipation()
    {
        var (a, b) = await SeedPlayersAsync();
        var (outsider, _) = await SeedPlayersAsync();
        var matchId = Guid.NewGuid();
        (await _factory.CreateGameServerClient().PostAsJsonAsync($"/api/matches/{matchId}/result", Result(a, b))).EnsureSuccessStatusCode();

        var outsiderReceipt = await ReceiptAsync(outsider, matchId);
        Assert.False(outsiderReceipt.GetProperty("recorded").GetBoolean());
        Assert.Equal(0, outsiderReceipt.GetProperty("xpGained").GetInt32());

        var anon = await _factory.CreateUnauthenticatedClient().GetAsync($"/api/matches/{matchId}/receipt");
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);
    }

    [Fact]
    public async Task ResubmittingSameResult_Returns200_AndGrantsExactlyOnce()
    {
        var (a, b) = await SeedPlayersAsync();
        var matchId = Guid.NewGuid();
        var server = _factory.CreateGameServerClient();

        var first = await server.PostAsJsonAsync($"/api/matches/{matchId}/result", Result(a, b));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var receiptAfterFirst = await ReceiptAsync(a, matchId);
        (int xp, int ledger, int items, int rows) Snapshot()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            return (db.PlayerAccounts.Single(p => p.Id == a).ProfileXp,
                db.CreditLedgerEntries.Where(e => e.PlayerId == a).Sum(e => e.ChangeAmount),
                db.InventoryItems.Count(i => i.PlayerId == a),
                db.MatchParticipations.Count(m => m.MatchId == matchId));
        }
        var before = Snapshot();

        // The dedicated server's durable outbox retries the identical submission.
        var second = await server.PostAsJsonAsync($"/api/matches/{matchId}/result", Result(a, b));

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(before, Snapshot());
        Assert.Equal(2, before.rows);
        var receiptAfterSecond = await ReceiptAsync(a, matchId);
        Assert.Equal(receiptAfterFirst.GetRawText(), receiptAfterSecond.GetRawText());
    }
}
