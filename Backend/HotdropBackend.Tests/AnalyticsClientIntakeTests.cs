using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using Xunit;

namespace HotdropBackend.Tests;

/// <summary>QA-027: the intake accepts every event name and envelope field the Unity client emits.</summary>
public class AnalyticsClientIntakeTests : IClassFixture<HotdropWebApplicationFactory>
{
    readonly HotdropWebApplicationFactory _factory;

    public AnalyticsClientIntakeTests(HotdropWebApplicationFactory factory) => _factory = factory;

    // Every Telemetry.Track("...") name in ProjectHotdrop/Assets/Scripts (as of QA-027).
    public static TheoryData<string> UnityClientEventNames => new()
    {
        "matchmaking_started", "match_joining", "online_match_joined", "online_match_completed",
        "online_failed", "match_disconnect", "match_reconnected", "client_error", "startup_budget",
        "first_open", "results_play_again_clicked", "study_cohort_set", "study_incentivized_set",
        "study_participant_started", "tutorial_started", "tutorial_step", "tutorial_completed",
        "weapon_selected",
    };

    static string Line(string name, string pseudonymousId, bool botSubstituted = false, int botCount = 0,
        bool incentivized = false, string region = "offline", int partySize = 1, string cohort = "default") => $$$"""
        {"eventName":"{{{name}}}","schemaVersion":1,"source":"client","buildVersion":"0.1","balanceVersion":"v1.0-spec",
         "pseudonymousId":"{{{pseudonymousId}}}","matchId":"","region":"{{{region}}}","deviceTier":"mid","partySize":{{{partySize}}},
         "hasBots":{{{(botCount > 0 ? "true" : "false")}}},"botSubstituted":{{{(botSubstituted ? "true" : "false")}}},"botCount":{{{botCount}}},
         "incentivizedTester":{{{(incentivized ? "true" : "false")}}},"cohort":"{{{cohort}}}","timestamp":"2026-09-29T10:00:00Z","payload":{}}
        """;

    async Task<JsonElement> PostBatchAsync(Guid playerId, params string[] lines)
    {
        var client = _factory.CreateClientWithPlayerId(playerId);
        var body = new StringContent("[" + string.Join(",", lines) + "]", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/analytics/events/batch", body);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Theory]
    [MemberData(nameof(UnityClientEventNames))]
    public async Task Batch_AcceptsEveryUnityClientEventName(string eventName)
    {
        var result = await PostBatchAsync(Guid.NewGuid(), Line(eventName, "p-" + eventName));
        Assert.Equal(1, result.GetProperty("accepted").GetInt32());
        Assert.Equal(0, result.GetProperty("rejected").GetInt32());
    }

    [Theory]
    [MemberData(nameof(UnityClientEventNames))]
    public async Task SingleEvent_AcceptsEveryUnityClientEventName(string eventName)
    {
        var client = _factory.CreateClientWithPlayerId(Guid.NewGuid());
        var response = await client.PostAsJsonAsync("/api/analytics/events", new
        {
            EventName = eventName, SchemaVersion = 1, BuildVersion = "0.1", BalanceVersion = "v1.0-spec",
            Region = "offline", DeviceTier = "mid", PartySize = 1, HasBots = false, Cohort = "default",
            Payload = new { },
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Theory]
    [InlineData("totally_unknown_event")]
    [InlineData("match_completed")] // server-owned fact: never accepted from a client
    [InlineData("MATCHMAKING_STARTED")] // names are exact
    public async Task UnknownOrServerOwnedName_IsRejected(string eventName)
    {
        var result = await PostBatchAsync(Guid.NewGuid(), Line(eventName, "rej-" + eventName));
        Assert.Equal(0, result.GetProperty("accepted").GetInt32());
        Assert.Equal(1, result.GetProperty("rejected").GetInt32());

        var client = _factory.CreateClientWithPlayerId(Guid.NewGuid());
        var single = await client.PostAsJsonAsync("/api/analytics/events", new
        {
            EventName = eventName, SchemaVersion = 1, BuildVersion = "0.1", BalanceVersion = "v1.0-spec",
            Region = "offline", DeviceTier = "mid", PartySize = 1, HasBots = false, Cohort = "default", Payload = new { },
        });
        Assert.Equal(HttpStatusCode.BadRequest, single.StatusCode);
    }

    [Fact]
    public async Task EnvelopeFields_RoundTripIntoStorage()
    {
        var playerId = Guid.NewGuid();
        var result = await PostBatchAsync(playerId,
            Line("online_match_completed", "pseudo-roundtrip", botSubstituted: true, botCount: 2, incentivized: true,
                region: "eu", partySize: 3, cohort: "study_pads_on"));
        Assert.Equal(1, result.GetProperty("accepted").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var stored = await db.AnalyticsEvents.SingleAsync(e => e.PlayerId == playerId);
        Assert.Equal("online_match_completed", stored.EventName);
        Assert.Equal("eu", stored.Region);
        Assert.Equal(3, stored.PartySize);
        Assert.True(stored.HasBots);
        Assert.True(stored.BotSubstituted);
        Assert.Equal(2, stored.BotCount);
        Assert.True(stored.IncentivizedTester);
        Assert.Equal("study_pads_on", stored.Cohort);
        Assert.Equal("pseudo-roundtrip", stored.PseudonymousId);
    }

    [Fact]
    public async Task EnvelopeWithoutNewFields_StillAccepted_WithDefaults()
    {
        var playerId = Guid.NewGuid();
        const string legacy = """
            {"eventName":"first_open","schemaVersion":1,"source":"client","buildVersion":"0.1","balanceVersion":"v1.0-spec",
             "pseudonymousId":"legacy","matchId":"","region":"offline","deviceTier":"low","partySize":1,"hasBots":false,
             "cohort":"default","timestamp":"2026-09-29T10:00:00Z","payload":{}}
            """;
        await PostBatchAsync(playerId, legacy);

        using var scope = _factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<HotdropDbContext>().AnalyticsEvents.SingleAsync(e => e.PlayerId == playerId);
        Assert.False(stored.BotSubstituted);
        Assert.Equal(0, stored.BotCount);
        Assert.False(stored.IncentivizedTester);
    }

    [Fact]
    public async Task ImpossibleBotCount_IsRejected()
    {
        var result = await PostBatchAsync(Guid.NewGuid(), Line("online_match_joined", "bots", botCount: 99));
        Assert.Equal(1, result.GetProperty("rejected").GetInt32());
    }
}




