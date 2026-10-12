using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using Xunit;

namespace HotdropBackend.Tests;

public class MatchEndpointsTests : IClassFixture<HotdropWebApplicationFactory>
{
    private readonly HotdropWebApplicationFactory _factory;

    public MatchEndpointsTests(HotdropWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Matches_Result_DuplicateSubmission_DoesNotCreateDuplicateParticipation()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            // Add the player account first (FK requirement)
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateGameServerClient();
        var request = new SubmitResultRequest(
            Draw: false,
            Participants: new List<ParticipantResult>
            {
                new ParticipantResult(playerId, Won: true, Eligible: true, HadBotSubstitution: false)
            });
        await _factory.SeedAllocatedMatchAsync(matchId, playerId);

        // Act - first submission
        var response1 = await client.PostAsJsonAsync($"/api/matches/{matchId}/result", request);
        response1.EnsureSuccessStatusCode();

        // Act - second submission (duplicate)
        var response2 = await client.PostAsJsonAsync($"/api/matches/{matchId}/result", request);
        response2.EnsureSuccessStatusCode();

        // Assert - only one MatchParticipation row exists
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var count = await db.MatchParticipations
                .CountAsync(m => m.MatchId == matchId && m.PlayerId == playerId);
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public async Task Post_Matches_Result_MultiplePlayers_AllRecordedOnce()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();
        var player3Id = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.AddRange(
                new PlayerAccount { Id = player1Id, IsGuest = true },
                new PlayerAccount { Id = player2Id, IsGuest = true },
                new PlayerAccount { Id = player3Id, IsGuest = true }
            );
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateGameServerClient();
        var request = new SubmitResultRequest(
            Draw: false,
            Participants: new List<ParticipantResult>
            {
                new ParticipantResult(player1Id, Won: true, Eligible: true, HadBotSubstitution: false),
                new ParticipantResult(player2Id, Won: false, Eligible: true, HadBotSubstitution: false),
                new ParticipantResult(player3Id, Won: false, Eligible: true, HadBotSubstitution: false)
            });
        await _factory.SeedAllocatedMatchAsync(matchId, player1Id, player2Id, player3Id);

        // Act - submit result
        var response = await client.PostAsJsonAsync($"/api/matches/{matchId}/result", request);
        response.EnsureSuccessStatusCode();

        // Act - submit again (should be idempotent)
        var response2 = await client.PostAsJsonAsync($"/api/matches/{matchId}/result", request);
        response2.EnsureSuccessStatusCode();

        // Assert - each player has exactly one participation record
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var participations = await db.MatchParticipations
                .Where(m => m.MatchId == matchId)
                .ToListAsync();
            Assert.Equal(3, participations.Count);
            Assert.Contains(participations, p => p.PlayerId == player1Id && p.Won == true);
            Assert.Contains(participations, p => p.PlayerId == player2Id && p.Won == false);
            Assert.Contains(participations, p => p.PlayerId == player3Id && p.Won == false);
        }
    }

    [Fact]
    public async Task Post_Matches_Result_DrawFlag_RecordedCorrectly()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateGameServerClient();
        var request = new SubmitResultRequest(
            Draw: true,
            Participants: new List<ParticipantResult>
            {
                new ParticipantResult(playerId, Won: false, Eligible: true, HadBotSubstitution: false)
            });
        await _factory.SeedAllocatedMatchAsync(matchId, playerId);

        // Act
        var response = await client.PostAsJsonAsync($"/api/matches/{matchId}/result", request);
        response.EnsureSuccessStatusCode();

        // Assert
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var participation = await db.MatchParticipations
                .FirstAsync(m => m.MatchId == matchId && m.PlayerId == playerId);
            Assert.True(participation.Draw);
            Assert.False(participation.Won);
        }
    }

    [Fact]
    public async Task Post_Matches_Reconnect_RequiresAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();
        var matchId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync($"/api/matches/{matchId}/reconnect", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Matches_Result_RequiresAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();
        var matchId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync($"/api/matches/{matchId}/result", 
            new SubmitResultRequest(false, new List<ParticipantResult>()));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Matches_Result_RejectsPlayerSession()
    {
        var client = _factory.CreateClientWithPlayerId(Guid.NewGuid());
        var response = await client.PostAsJsonAsync($"/api/matches/{Guid.NewGuid()}/result",
            new SubmitResultRequest(false, new List<ParticipantResult>()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private record ParticipantResult(Guid PlayerId, bool Won, bool Eligible, bool HadBotSubstitution);
    private record SubmitResultRequest(bool Draw, List<ParticipantResult> Participants);
}




