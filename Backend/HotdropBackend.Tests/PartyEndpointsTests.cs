using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using Xunit;

namespace HotdropBackend.Tests;

public class PartyEndpointsTests : IClassFixture<HotdropWebApplicationFactory>
{
    private readonly HotdropWebApplicationFactory _factory;

    public PartyEndpointsTests(HotdropWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Party_Create_ReturnsPartyWithCode_AndLeaderAsMember()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/party/create", new { });

        // Assert
        response.EnsureSuccessStatusCode();
        var party = await response.Content.ReadFromJsonAsync<PartyResponse>();
        Assert.NotNull(party);
        Assert.NotNull(party.Code);
        Assert.Equal(8, party.Code.Length);
        Assert.Equal(playerId.ToString(), party.LeaderId);
        Assert.Contains(playerId.ToString(), party.MemberIds);
        // Ready state is explicit per player (spec section 14: "all members
        // must be ready") — creating a party doesn't auto-ready the leader.
        Assert.DoesNotContain(playerId.ToString(), party.ReadyIds);
    }

    [Fact]
    public async Task Post_Party_Join_AddsMember_UpToThreePlayers()
    {
        // Arrange
        var leaderId = Guid.NewGuid();
        var leaderClient = _factory.CreateClientWithPlayerId(leaderId);
        var createResponse = await leaderClient.PostAsJsonAsync("/api/party/create", new { });
        var party = await createResponse.Content.ReadFromJsonAsync<PartyResponse>();

        // Act - first joiner (player 2)
        var player2Id = Guid.NewGuid();
        var player2Client = _factory.CreateClientWithPlayerId(player2Id);
        var joinResponse1 = await player2Client.PostAsJsonAsync($"/api/party/{party.Code}/join", new { });

        // Assert - first join succeeds
        joinResponse1.EnsureSuccessStatusCode();
        var joinedParty1 = await joinResponse1.Content.ReadFromJsonAsync<PartyResponse>();
        Assert.Equal(2, joinedParty1.MemberIds.Count);
        Assert.Contains(player2Id.ToString(), joinedParty1.MemberIds);

        // Act - second joiner (player 3)
        var player3Id = Guid.NewGuid();
        var player3Client = _factory.CreateClientWithPlayerId(player3Id);
        var joinResponse2 = await player3Client.PostAsJsonAsync($"/api/party/{party.Code}/join", new { });

        // Assert - second join succeeds (now 3 players)
        joinResponse2.EnsureSuccessStatusCode();
        var joinedParty2 = await joinResponse2.Content.ReadFromJsonAsync<PartyResponse>();
        Assert.Equal(3, joinedParty2.MemberIds.Count);
        Assert.Contains(player3Id.ToString(), joinedParty2.MemberIds);

        // Act - third joiner (player 4) - should fail with 400 (party full)
        var player4Id = Guid.NewGuid();
        var player4Client = _factory.CreateClientWithPlayerId(player4Id);
        var joinResponse3 = await player4Client.PostAsJsonAsync($"/api/party/{party.Code}/join", new { });

        // Assert - third join fails
        Assert.Equal(HttpStatusCode.BadRequest, joinResponse3.StatusCode);
        var error = await joinResponse3.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("party_full", error?.Error);
    }

    [Fact]
    public async Task Post_Party_Leave_RemovesMember_AndReadyStatus()
    {
        // Arrange
        var leaderId = Guid.NewGuid();
        var leaderClient = _factory.CreateClientWithPlayerId(leaderId);
        var createResponse = await leaderClient.PostAsJsonAsync("/api/party/create", new { });
        var party = await createResponse.Content.ReadFromJsonAsync<PartyResponse>();

        var player2Id = Guid.NewGuid();
        var player2Client = _factory.CreateClientWithPlayerId(player2Id);
        await player2Client.PostAsJsonAsync($"/api/party/{party.Code}/join", new { });

        // Act - player 2 leaves
        var leaveResponse = await player2Client.PostAsJsonAsync($"/api/party/{party.Code}/leave", new { });

        // Assert
        leaveResponse.EnsureSuccessStatusCode();
        var updatedParty = await leaveResponse.Content.ReadFromJsonAsync<PartyResponse>();
        Assert.Single(updatedParty.MemberIds);
        Assert.Equal(leaderId.ToString(), updatedParty.MemberIds[0]);
        Assert.DoesNotContain(player2Id.ToString(), updatedParty.MemberIds);
        Assert.DoesNotContain(player2Id.ToString(), updatedParty.ReadyIds);
    }

    [Fact]
    public async Task Post_Party_Ready_TogglesReadyStatus()
    {
        // Arrange
        var leaderId = Guid.NewGuid();
        var leaderClient = _factory.CreateClientWithPlayerId(leaderId);
        var createResponse = await leaderClient.PostAsJsonAsync("/api/party/create", new { });
        var party = await createResponse.Content.ReadFromJsonAsync<PartyResponse>();

        var player2Id = Guid.NewGuid();
        var player2Client = _factory.CreateClientWithPlayerId(player2Id);
        await player2Client.PostAsJsonAsync($"/api/party/{party.Code}/join", new { });

        // Act - player 2 sets ready = true
        var readyResponse = await player2Client.PostAsJsonAsync($"/api/party/{party.Code}/ready", true);

        // Assert
        readyResponse.EnsureSuccessStatusCode();
        var updatedParty = await readyResponse.Content.ReadFromJsonAsync<PartyResponse>();
        Assert.Contains(player2Id.ToString(), updatedParty.ReadyIds);

        // Act - player 2 sets ready = false
        var notReadyResponse = await player2Client.PostAsJsonAsync($"/api/party/{party.Code}/ready", false);

        // Assert
        notReadyResponse.EnsureSuccessStatusCode();
        var notReadyParty = await notReadyResponse.Content.ReadFromJsonAsync<PartyResponse>();
        Assert.DoesNotContain(player2Id.ToString(), notReadyParty.ReadyIds);
    }

    [Fact]
    public async Task Party_Endpoints_RequireAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act & Assert - all party endpoints should return 401
        var createResponse = await client.PostAsJsonAsync("/api/party/create", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);

        var joinResponse = await client.PostAsJsonAsync("/api/party/ABC12345/join", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, joinResponse.StatusCode);

        var leaveResponse = await client.PostAsJsonAsync("/api/party/ABC12345/leave", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, leaveResponse.StatusCode);

        var readyResponse = await client.PostAsJsonAsync("/api/party/ABC12345/ready", true);
        Assert.Equal(HttpStatusCode.Unauthorized, readyResponse.StatusCode);
    }

    [Fact]
    public async Task Post_Party_Join_NonExistentCode_Returns404()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/party/NOTEXIST/join", new { });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Party_Leave_NonExistentCode_Returns404()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/party/NOTEXIST/leave", new { });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Party_Ready_NonExistentCode_Returns404()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/party/NOTEXIST/ready", true);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record PartyResponse(string Code, string LeaderId, List<string> MemberIds, List<string> ReadyIds);
    private record ErrorResponse(string Error);
}


