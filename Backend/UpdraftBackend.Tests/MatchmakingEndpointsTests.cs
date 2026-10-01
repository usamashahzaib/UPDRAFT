using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using Xunit;

namespace UpdraftBackend.Tests;

public class MatchmakingEndpointsTests : IClassFixture<UpdraftWebApplicationFactory>
{
    private readonly UpdraftWebApplicationFactory _factory;

    public MatchmakingEndpointsTests(UpdraftWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Matchmaking_Enqueue_ReturnsQueued()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/matchmaking/enqueue", new { });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EnqueueResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Queued);
        Assert.Equal(playerId.ToString(), result.PlayerId);
    }

    [Fact]
    public async Task Post_Matchmaking_Cancel_ReturnsCancelled()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/matchmaking/cancel", new { });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CancelResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Cancelled);
    }

    [Fact]
    public async Task Get_Matchmaking_Status_ReturnsIdleWhenNeverQueued()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.GetAsync("/api/matchmaking/status");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StatusResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("idle", result.State);
        Assert.Equal(0, result.ElapsedSeconds);
    }

    [Fact]
    public async Task Get_Matchmaking_Status_ReturnsSearchingAfterEnqueue()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClientWithPlayerId(playerId);
        (await client.PostAsJsonAsync("/api/matchmaking/enqueue", new { })).EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/api/matchmaking/status");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StatusResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("searching", result.State);
        Assert.False(string.IsNullOrEmpty(result.Region), "QA-029: the search screen shows the queue region");
    }

    [Fact]
    public async Task Enqueue_Twice_ReturnsConflict()
    {
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClientWithPlayerId(playerId);
        (await client.PostAsJsonAsync("/api/matchmaking/enqueue", new { })).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/matchmaking/enqueue", new { });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task PartyQueue_RequiresLeaderAndEveryMemberReady()
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var leader = _factory.CreateClientWithPlayerId(leaderId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.AddRange(new PlayerAccount { Id = leaderId }, new PlayerAccount { Id = memberId });
            await db.SaveChangesAsync();
        }

        var created = await leader.PostAsJsonAsync("/api/party/create", new { });
        created.EnsureSuccessStatusCode();
        var party = await created.Content.ReadFromJsonAsync<PartyResponse>();
        Assert.NotNull(party);
        var member = _factory.CreateClientWithPlayerId(memberId);
        (await member.PostAsJsonAsync($"/api/party/{party.Code}/join", new { })).EnsureSuccessStatusCode();

        var unready = await leader.PostAsJsonAsync("/api/matchmaking/enqueue", new { });
        Assert.Equal(HttpStatusCode.Conflict, unready.StatusCode);

        (await leader.PostAsJsonAsync($"/api/party/{party.Code}/ready", true)).EnsureSuccessStatusCode();
        (await member.PostAsJsonAsync($"/api/party/{party.Code}/ready", true)).EnsureSuccessStatusCode();
        var queued = await leader.PostAsJsonAsync("/api/matchmaking/enqueue", new { });
        queued.EnsureSuccessStatusCode();
        var nonLeaderQueue = await member.PostAsJsonAsync("/api/matchmaking/enqueue", new { });
        Assert.Equal(HttpStatusCode.Forbidden, nonLeaderQueue.StatusCode);

        // QA-014: the member never enqueues, yet sees the leader's queue on their own status —
        // this is what the member client polls to follow the leader into the match.
        var memberStatus = await (await member.GetAsync("/api/matchmaking/status")).Content.ReadFromJsonAsync<StatusResponse>();
        Assert.Equal("searching", memberStatus!.State);
    }

    [Fact]
    public async Task Status_MatchedTicketForFinishedMatch_ReadsIdle()
    {
        var playerId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId });
            db.MatchmakingTickets.Add(new MatchmakingTicket
            {
                Id = Guid.NewGuid(), PlayerId = playerId, Status = "matched", MatchId = matchId,
                EnqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-5), UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-4),
            });
            db.MatchAllocations.Add(new MatchAllocation { MatchId = matchId, ServerId = "s1", CompletedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClientWithPlayerId(playerId);
        var result = await (await client.GetAsync("/api/matchmaking/status")).Content.ReadFromJsonAsync<StatusResponse>();
        Assert.Equal("idle", result!.State);
    }

    [Fact]
    public async Task Cancel_StopsFurtherComposition()
    {
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClientWithPlayerId(playerId);
        (await client.PostAsJsonAsync("/api/matchmaking/enqueue", new { })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/matchmaking/cancel", new { })).EnsureSuccessStatusCode();

        var status = await client.GetAsync("/api/matchmaking/status");
        var result = await status.Content.ReadFromJsonAsync<StatusResponse>();
        Assert.Equal("idle", result!.State);
    }

    [Fact]
    public async Task Cancel_MatchedReservationWithoutAllocation_CancelsTicket()
    {
        var playerId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            db.MatchmakingTickets.Add(new MatchmakingTicket
            {
                Id = Guid.NewGuid(), PlayerId = playerId, Status = "matched", MatchId = matchId,
                EnqueuedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);
        var cancel = await client.PostAsJsonAsync("/api/matchmaking/cancel", new { });
        cancel.EnsureSuccessStatusCode();
        var status = await (await client.GetAsync("/api/matchmaking/status")).Content.ReadFromJsonAsync<StatusResponse>();
        Assert.Equal("idle", status!.State);
    }

    [Fact]
    public async Task Cancel_MatchedReservationWithActiveAllocation_ReturnsConflictAndPreservesReservation()
    {
        var playerId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            db.MatchmakingTickets.Add(new MatchmakingTicket
            {
                Id = Guid.NewGuid(), PlayerId = playerId, Status = "matched", MatchId = matchId,
                EnqueuedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            });
            db.MatchAllocations.Add(new MatchAllocation { MatchId = matchId, ServerId = "qa-server" });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);
        var cancel = await client.PostAsJsonAsync("/api/matchmaking/cancel", new { });
        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
        Assert.Contains("reservation_starting", await cancel.Content.ReadAsStringAsync());
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var ticket = await verifyDb.MatchmakingTickets.SingleAsync(t => t.PlayerId == playerId);
        Assert.Equal("matched", ticket.Status);
        Assert.True(await verifyDb.MatchAllocations.AnyAsync(a => a.MatchId == matchId && a.CompletedAt == null));
    }

    [Fact]
    public async Task Get_Matchmaking_Ticket_ReturnsNullWhenNoReservation()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.GetAsync("/api/matchmaking/ticket");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TicketResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Ticket);
        Assert.Equal("no reservation yet", result.Note);
    }

    [Fact]
    public async Task Matchmaking_Endpoints_RequireAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act & Assert
        var enqueueResponse = await client.PostAsJsonAsync("/api/matchmaking/enqueue", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, enqueueResponse.StatusCode);

        var cancelResponse = await client.PostAsJsonAsync("/api/matchmaking/cancel", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, cancelResponse.StatusCode);

        var statusResponse = await client.GetAsync("/api/matchmaking/status");
        Assert.Equal(HttpStatusCode.Unauthorized, statusResponse.StatusCode);

        var ticketResponse = await client.GetAsync("/api/matchmaking/ticket");
        Assert.Equal(HttpStatusCode.Unauthorized, ticketResponse.StatusCode);
    }

    private record EnqueueResponse(bool Queued, string PlayerId);
    private record CancelResponse(bool Cancelled);
    private record StatusResponse(string State, int ElapsedSeconds, string? Region = null);
    private record TicketResponse(string? Ticket, string Note);
    private record PartyResponse(string Code, string LeaderId, List<string> MemberIds, List<string> ReadyIds, List<PartyMemberResponse> Members);
    private record PartyMemberResponse(string PlayerId, string Alias, bool IsReady);
}
