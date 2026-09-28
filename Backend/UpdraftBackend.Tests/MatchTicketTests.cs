using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using UpdraftBackend.Endpoints;
using Xunit;

namespace UpdraftBackend.Tests;

public class MatchTicketTests : IClassFixture<UpdraftWebApplicationFactory>
{
    readonly UpdraftWebApplicationFactory _factory;
    public MatchTicketTests(UpdraftWebApplicationFactory factory) => _factory = factory;

    async Task<(Guid player, Guid match)> SeedMatched(bool allocated = true) => await SeedMatched(_factory, allocated);

    static async Task<(Guid player, Guid match)> SeedMatched(UpdraftWebApplicationFactory factory, bool allocated)
    {
        var player = Guid.NewGuid();
        var match = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        db.PlayerAccounts.Add(new PlayerAccount { Id = player, Alias = "SwiftOtter-1234" });
        db.MatchmakingTickets.Add(new MatchmakingTicket { Id = Guid.NewGuid(), PlayerId = player, Status = "matched", MatchId = match, Team = "B" });
        if (allocated) db.MatchAllocations.Add(new MatchAllocation { MatchId = match, ServerId = "srv-seed" });
        await db.SaveChangesAsync();
        return (player, match);
    }

    [Fact]
    public async Task NoTicket_UntilAServerClaimsTheMatch_ThenClaimIsExclusive()
    {
        using var factory = new UpdraftWebApplicationFactory();
        var (player, match) = await SeedMatched(factory, allocated: false);
        var client = factory.CreateClientWithPlayerId(player);

        var waiting = await client.GetFromJsonAsync<JsonElement>("/api/matchmaking/ticket");
        Assert.Equal(JsonValueKind.Null, waiting.GetProperty("ticket").ValueKind);
        Assert.False(waiting.GetProperty("serverReady").GetBoolean());

        var server = factory.CreateGameServerClient();
        var claim = await server.PostAsync("/api/matches/claim", null);
        claim.EnsureSuccessStatusCode();
        var claimed = await claim.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(match, claimed.GetProperty("matchId").GetGuid());
        var players = claimed.GetProperty("players").EnumerateArray().ToList();
        Assert.Single(players);
        Assert.Equal("B", players[0].GetProperty("team").GetString());
        Assert.Equal("SwiftOtter-1234", players[0].GetProperty("alias").GetString());

        // Nothing else to host: a second server gets 204, never the same match.
        var second = await server.PostAsync("/api/matches/claim", null);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var ready = await client.GetFromJsonAsync<JsonElement>("/api/matchmaking/ticket");
        Assert.True(ready.GetProperty("serverReady").GetBoolean());
        Assert.False(string.IsNullOrEmpty(ready.GetProperty("ticket").GetString()));
    }

    [Fact]
    public async Task Claim_RespectsConcurrentMatchCap_AndPlayersCannotClaim()
    {
        using var factory = new UpdraftWebApplicationFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            for (int i = 0; i < 4; i++) db.MatchAllocations.Add(new MatchAllocation { MatchId = Guid.NewGuid(), ServerId = $"busy-{i}" });
            await db.SaveChangesAsync();
        }
        var (player, _) = await SeedMatched(factory, allocated: false);
        var atCap = await factory.CreateGameServerClient().PostAsync("/api/matches/claim", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, atCap.StatusCode);
        var denied = await factory.CreateClientWithPlayerId(player).PostAsync("/api/matches/claim", null);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Claim_ExpiresCrashedAllocationWithoutInventingAResult()
    {
        using var factory = new UpdraftWebApplicationFactory();
        var (player, match) = await SeedMatched(factory, allocated: false);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.MatchAllocations.Add(new MatchAllocation
            {
                MatchId = match,
                ServerId = "crashed-server",
                ClaimedAt = DateTimeOffset.UtcNow - MatchEndpoints.AllocationTimeout - TimeSpan.FromSeconds(1),
            });
            await db.SaveChangesAsync();
        }

        var response = await factory.CreateGameServerClient().PostAsync("/api/matches/claim", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            var allocation = await db.MatchAllocations.SingleAsync(a => a.MatchId == match);
            var ticket = await db.MatchmakingTickets.SingleAsync(t => t.PlayerId == player);
            var account = await db.PlayerAccounts.SingleAsync(a => a.Id == player);
            Assert.NotNull(allocation.CompletedAt);
            Assert.Equal("cancelled", ticket.Status);
            Assert.False(await db.MatchParticipations.AnyAsync(p => p.MatchId == match));
            Assert.False(await db.CreditLedgerEntries.AnyAsync(e => e.PlayerId == player));
            Assert.Equal(0, account.ProfileXp);
            Assert.Equal(1000, account.HiddenMatchmakingRating);
        }
    }

    [Fact]
    public async Task MatchedPlayer_GetsTicket_ServerValidatesIt()
    {
        var (player, match) = await SeedMatched();
        var response = await _factory.CreateClientWithPlayerId(player).GetFromJsonAsync<JsonElement>("/api/matchmaking/ticket");
        string ticket = response.GetProperty("ticket").GetString()!;
        Assert.False(string.IsNullOrEmpty(ticket));
        Assert.Equal(match.ToString("N"), response.GetProperty("session").GetString());

        var validation = await _factory.CreateGameServerClient().PostAsJsonAsync("/api/matches/validate-ticket", new { MatchId = match, Ticket = ticket });
        validation.EnsureSuccessStatusCode();
        var body = await validation.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(player, body.GetProperty("playerId").GetGuid());
        Assert.Equal("B", body.GetProperty("team").GetString());
        Assert.Equal("SwiftOtter-1234", body.GetProperty("alias").GetString());
    }

    [Fact]
    public async Task Ticket_ForAnotherMatch_OrTampered_IsRejected()
    {
        var (player, _) = await SeedMatched();
        var response = await _factory.CreateClientWithPlayerId(player).GetFromJsonAsync<JsonElement>("/api/matchmaking/ticket");
        string ticket = response.GetProperty("ticket").GetString()!;
        var server = _factory.CreateGameServerClient();

        var wrongMatch = await server.PostAsJsonAsync("/api/matches/validate-ticket", new { MatchId = Guid.NewGuid(), Ticket = ticket });
        Assert.Equal(HttpStatusCode.Forbidden, wrongMatch.StatusCode);
        var tampered = await server.PostAsJsonAsync("/api/matches/validate-ticket", new { MatchId = Guid.NewGuid(), Ticket = ticket[..^4] + "AAAA" });
        Assert.Equal(HttpStatusCode.Unauthorized, tampered.StatusCode);
    }

    [Fact]
    public async Task Players_CannotValidateTickets()
    {
        var (player, match) = await SeedMatched();
        var denied = await _factory.CreateClientWithPlayerId(player).PostAsJsonAsync("/api/matches/validate-ticket", new { MatchId = match, Ticket = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task ServerToken_RequiresConfiguredKey()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var noKey = await client.PostAsJsonAsync("/api/auth/server-token", new { ServerId = "srv-1" });
        Assert.Equal(HttpStatusCode.Unauthorized, noKey.StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/server-token") { Content = JsonContent.Create(new { ServerId = "srv-1" }) };
        request.Headers.Add("X-Server-Key", UpdraftWebApplicationFactory.TestServerKey);
        var ok = await client.SendAsync(request);
        ok.EnsureSuccessStatusCode();
        var body = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }
}
