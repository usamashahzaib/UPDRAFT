using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;
using Xunit;

namespace HotdropBackend.Tests;

public class AnalyticsEndpointsTests : IClassFixture<HotdropWebApplicationFactory>
{
    readonly HotdropWebApplicationFactory _factory;
    public AnalyticsEndpointsTests(HotdropWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ClientEvent_UsesAuthenticatedPlayer_AndRejectsServerFacts()
    {
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);
        var request = Event("tutorial_completed");

        var response = await client.PostAsJsonAsync("/api/analytics/events", request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
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
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
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
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        Assert.False(await verifyDb.AnalyticsEvents.AnyAsync(e => e.EventName == "tutorial_started" && e.Timestamp < cutoff));
        Assert.True(await verifyDb.AnalyticsDailyAggregates.AnyAsync(a => a.EventName == "tutorial_started" && a.EventCount >= 1));
    }

    [Fact]
    public async Task ClientBatch_PersistsPseudonymousId_AndClientTimestamp()
    {
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);
        // Exactly the line shape the Unity Telemetry queue writes (see Telemetry.cs).
        string Line(string name, string timestamp) => $$$"""
            {"eventName":"{{{name}}}","schemaVersion":1,"source":"client","buildVersion":"0.1","balanceVersion":"v1.0-spec",
             "pseudonymousId":"tester-abc","matchId":"","region":"offline","deviceTier":"low","partySize":1,"hasBots":true,
             "cohort":"study_pads_on","timestamp":"{{{timestamp}}}","payload":{}}
            """;
        var body = new StringContent($"[{Line("first_open", "2026-09-20T08:30:00Z")}]", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/analytics/events/batch", body);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, result.GetProperty("accepted").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var stored = await db.AnalyticsEvents.SingleAsync(e => e.PlayerId == playerId && e.EventName == "first_open");
        Assert.Equal("tester-abc", stored.PseudonymousId);
        Assert.Equal(DateTimeOffset.Parse("2026-09-20T08:30:00Z"), stored.ClientTimestamp!.Value);
    }

    [Fact]
    public async Task ServerEvents_StoreNoClientIdentity()
    {
        var server = _factory.CreateGameServerClient();
        var response = await server.PostAsJsonAsync("/api/analytics/server-events", Event("match_completed"));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var stored = await db.AnalyticsEvents.Where(e => e.Source == "server" && e.EventName == "match_completed")
            .OrderByDescending(e => e.Id).FirstAsync();
        Assert.Equal(string.Empty, stored.PseudonymousId);
        Assert.Null(stored.ClientTimestamp);
    }

    [Fact]
    public async Task PresenceRollup_CreatesOneRowPerUserDay_AndIsIdempotent()
    {
        // Yesterday 12:00 UTC: always in the past, never near a UTC date boundary.
        var eventTime = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-1).AddHours(12);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.AnalyticsEvents.AddRange(
                SeedEvent("presence-p1", eventTime, clientTimestamp: eventTime.AddMinutes(-3)),
                SeedEvent("presence-p1", eventTime.AddMinutes(1)),
                SeedEvent("presence-p2", eventTime.AddMinutes(2)));
            await db.SaveChangesAsync();
        }

        var service = new AnalyticsRetentionService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AnalyticsRetentionService>.Instance);
        await service.RunOnce(CancellationToken.None);
        await service.RunOnce(CancellationToken.None); // rerun must not duplicate user-days

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var date = DateOnly.FromDateTime(eventTime.UtcDateTime);
        var rows = await verifyDb.AnalyticsDailyPresence.Where(p => p.EventDate == date).ToListAsync();
        Assert.Equal(2, rows.Count);

        var p1 = rows.Single(r => r.PseudonymousId == "presence-p1");
        Assert.Equal("study_pads_on", p1.Cohort);
        Assert.Equal(eventTime.AddMinutes(-3), p1.FirstSeenAt);
    }

    [Fact]
    public async Task RetentionJob_DropsAbandonedDeletionRequests_ButKeepsLiveOnes()
    {
        Guid abandoned = Guid.NewGuid(), live = Guid.NewGuid();
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.AccountDeletionRequests.Add(new AccountDeletionRequest
            {
                Id = abandoned, ContactName = "Old", ContactEmail = "old@example.test",
                Status = AccountDeletionService.PendingVerification, VerificationTokenHash = "x1",
                VerificationExpiresAt = DateTimeOffset.UtcNow.AddDays(-8),
            });
            db.AccountDeletionRequests.Add(new AccountDeletionRequest
            {
                Id = live, ContactName = "New", ContactEmail = "new@example.test",
                Status = AccountDeletionService.PendingVerification, VerificationTokenHash = "x2",
                VerificationExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
            });
            await db.SaveChangesAsync();
        }

        var service = new AnalyticsRetentionService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AnalyticsRetentionService>.Instance);
        await service.RunOnce(CancellationToken.None);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        Assert.Null(await verifyDb.AccountDeletionRequests.FindAsync(abandoned));
        Assert.NotNull(await verifyDb.AccountDeletionRequests.FindAsync(live));
    }

    [Fact]
    public async Task PresenceRollup_FoldsLateArrivingOfflineEvents_AfterWatermark()
    {
        var service = new AnalyticsRetentionService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AnalyticsRetentionService>.Instance);
        await service.RunOnce(CancellationToken.None); // advances the watermark to now

        // An offline-collected event arrives after the watermark with an old client clock.
        var eventTime = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-2).AddHours(9);
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.AnalyticsEvents.Add(SeedEvent("presence-late", eventTime.AddMinutes(5), clientTimestamp: eventTime));
            await db.SaveChangesAsync();
        }

        await service.RunOnce(CancellationToken.None);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var row = await verifyDb.AnalyticsDailyPresence.SingleAsync(p =>
            p.PseudonymousId == "presence-late" && p.EventDate == DateOnly.FromDateTime(eventTime.UtcDateTime));
        Assert.Equal(eventTime, row.FirstSeenAt);
    }

    [Fact]
    public async Task PresenceRollup_SurvivesRawWindowExpiry()
    {
        var oldTime = DateTimeOffset.UtcNow.AddDays(-31);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.AnalyticsEvents.Add(SeedEvent("presence-expired", oldTime, clientTimestamp: oldTime));
            await db.SaveChangesAsync();
        }

        var service = new AnalyticsRetentionService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AnalyticsRetentionService>.Instance);
        await service.RunOnce(CancellationToken.None);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        Assert.False(await verifyDb.AnalyticsEvents.AnyAsync(e => e.PseudonymousId == "presence-expired" && e.Timestamp < cutoff));
        var presence = await verifyDb.AnalyticsDailyPresence.Where(p => p.PseudonymousId == "presence-expired")
            .OrderByDescending(p => p.Id).FirstAsync();
        Assert.Equal(DateOnly.FromDateTime(oldTime.UtcDateTime), presence.EventDate);
    }

    [Fact]
    public async Task SessionRollup_CountsSessionsAndSameDayReturns_AndIsIdempotent()
    {
        // One player, one UTC day: two playings separated by more than 30 minutes,
        // plus a five-minute background switch that must not become a return.
        // This fixture is shared, so the session tests use their own UTC day to stay
        // clear of the presence-rollup tests that count rows per date.
        var noon = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-3).AddHours(12);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.AnalyticsEvents.AddRange(
                SeedEvent("session-p1", noon.AddMinutes(0), "tutorial_step", noon),
                SeedEvent("session-p1", noon.AddMinutes(5), "tutorial_step", noon.AddMinutes(5)),
                SeedEvent("session-p1", noon.AddMinutes(70), "online_match_completed", noon.AddMinutes(70)));
            await db.SaveChangesAsync();
        }

        var service = new AnalyticsRetentionService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AnalyticsRetentionService>.Instance);
        await service.RunOnce(CancellationToken.None);
        await service.RunOnce(CancellationToken.None); // rerun must not double-count

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var row = await verifyDb.AnalyticsDailySessions.SingleAsync(s =>
            s.PseudonymousId == "session-p1" && s.EventDate == DateOnly.FromDateTime(noon.UtcDateTime));
        Assert.Equal(2, row.Sessions);
        Assert.Equal(1, row.SameDayReturns);
        Assert.Equal(2, row.VoluntarySessions);
    }

    [Fact]
    public async Task SessionRollup_ExcludesNonGameplayEventsAndIncentivizedSessions()
    {
        var noon = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-3).AddHours(12);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var startup = SeedEvent("session-p2", noon.AddMinutes(0), "first_open", noon);
            var error = SeedEvent("session-p2", noon.AddMinutes(40), "client_error", noon.AddMinutes(40));
            error.IncentivizedTester = true;
            db.AnalyticsEvents.AddRange(startup, error);
            await db.SaveChangesAsync();
        }

        var service = new AnalyticsRetentionService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AnalyticsRetentionService>.Instance);
        await service.RunOnce(CancellationToken.None);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        // Startup and crash reports are not gameplay, so this player has no session row at all.
        Assert.False(await verifyDb.AnalyticsDailySessions.AnyAsync(s => s.PseudonymousId == "session-p2"));
    }

    static AnalyticsEvent SeedEvent(string pseudonymousId, DateTimeOffset timestamp, string eventName = "tutorial_step", DateTimeOffset? clientTimestamp = null) => new()
    {
        EventName = eventName,
        SchemaVersion = 1,
        Source = "client",
        BuildVersion = "test",
        BalanceVersion = "test",
        Region = "test",
        DeviceTier = "low",
        PartySize = 1,
        Cohort = "study_pads_on",
        PseudonymousId = pseudonymousId,
        Timestamp = timestamp,
        ClientTimestamp = clientTimestamp,
    };

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
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
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




