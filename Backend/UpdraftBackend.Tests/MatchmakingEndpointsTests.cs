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
    public async Task Get_Matchmaking_Status_ReturnsSearching()
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
        Assert.Equal("searching", result.State);
        Assert.Equal(0, result.ElapsedSeconds);
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
    private record StatusResponse(string State, int ElapsedSeconds);
    private record TicketResponse(string? Ticket, string Note);
}