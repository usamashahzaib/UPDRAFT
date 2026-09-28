using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using UpdraftBackend.Services;
using Xunit;

namespace UpdraftBackend.Tests;

public class AnalyticsEndpointsTests : IClassFixture<UpdraftWebApplicationFactory>
{
    readonly UpdraftWebApplicationFactory _factory;
    public AnalyticsEndpointsTests(UpdraftWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ClientEvent_UsesAuthenticatedPlayer_AndRejectsServerFacts()
    {
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);
        var request = Event("tutorial_completed");

        var response = await client.PostAsJsonAsync("/api/analytics/events", request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var stored = await db.AnalyticsEvents.SingleAsync(e => e.PlayerId == playerId && e.EventName == "tutorial_completed");
        Assert.Equal("client", stored.Source);

        var forbiddenFact = await client.PostAsJsonAsync("/api/analytics/events", Event("match_completed"));
        Assert.Equal(HttpStatusCode.BadRequest, forbiddenFact.StatusCode);
    }

    [Fact]
    public async Task ServerEvent_RequiresGameServerClaim()
    {
        var player = _factory.CreateClientWithPlayerId(Guid.NewGuid());
        var denied = await player.PostAsJsonAsync("/api/analytics/server-events", Event("match_completed"));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var server = _factory.CreateGameServerClient();
        var accepted = await server.PostAsJsonAsync("/api/analytics/server-events", Event("match_completed"));
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
    }

    [Fact]
    public async Task Retention_AggregatesAndDeletesExpiredRawEvents()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                EventName = "tutorial_started", SchemaVersion = 1, Source = "client",
                BuildVersion = "test", BalanceVersion = "test", Region = "test",
                DeviceTier = "low", PartySize = 1, Timestamp = DateTimeOffset.UtcNow.AddDays(-31),
            });
            await db.SaveChangesAsync();
        }

        var service = new AnalyticsRetentionService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AnalyticsRetentionService>.Instance);
        await service.RunOnce(CancellationToken.None);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        Assert.False(await verifyDb.AnalyticsEvents.AnyAsync(e => e.EventName == "tutorial_started" && e.Timestamp < cutoff));
        Assert.True(await verifyDb.AnalyticsDailyAggregates.AnyAsync(a => a.EventName == "tutorial_started" && a.EventCount >= 1));
    }

    [Fact]
    public async Task ClientBatch_AcceptsQueueFormat_AndDropsServerOwnedFacts()
    {
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);
        // Exactly the line shape the Unity Telemetry queue writes.
        string Line(string name) => $$$"""
            {"eventName":"{{{name}}}","schemaVersion":1,"source":"client","buildVersion":"0.1","balanceVersion":"v1.0-spec",
             "pseudonymousId":"abc","matchId":"","region":"offline","deviceTier":"low","partySize":1,"hasBots":true,
             "cohort":"study_pads_on","timestamp":"2026-09-28T10:00:00Z","payload":{"step":"PlacePad","index":3}}
            """;
        var body = new StringContent($"[{Line("tutorial_step")},{Line("match_completed")}]", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/analytics/events/batch", body);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, result.GetProperty("accepted").GetInt32());
        Assert.Equal(1, result.GetProperty("rejected").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var stored = await db.AnalyticsEvents.SingleAsync(e => e.PlayerId == playerId);
        Assert.Equal("tutorial_step", stored.EventName);
        Assert.Equal("study_pads_on", stored.Cohort);
    }

    [Fact]
    public async Task OutdatedClientProtocol_GetsUpdateRequired()
    {
        var client = _factory.CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Client-Protocol", "0");
        var response = await client.GetAsync("/health/live");
        Assert.Equal((HttpStatusCode)426, response.StatusCode);
    }

    static object Event(string name) => new
    {
        EventName = name, SchemaVersion = 1, BuildVersion = "test", BalanceVersion = "test",
        Region = "test", DeviceTier = "low", PartySize = 1, HasBots = false,
        Cohort = "test", MatchId = (Guid?)null, PlayerId = (Guid?)null,
        Payload = JsonDocument.Parse("{}").RootElement,
    };
}
