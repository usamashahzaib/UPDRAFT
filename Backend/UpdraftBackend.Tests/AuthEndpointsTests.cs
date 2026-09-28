using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using Xunit;

namespace UpdraftBackend.Tests;

public class AuthEndpointsTests : IClassFixture<UpdraftWebApplicationFactory>
{
    private readonly UpdraftWebApplicationFactory _factory;

    public AuthEndpointsTests(UpdraftWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Auth_Guest_CreatesPlayerAccount_AndReturnsPlayerId()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/guest", new { });

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GuestResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.PlayerId);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.True(result.AccessExpiresAt > DateTimeOffset.UtcNow);

        // Verify account was actually created in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var account = await db.PlayerAccounts.FindAsync(result.PlayerId);
        Assert.NotNull(account);
        Assert.True(account.IsGuest);
    }

    [Fact]
    public async Task Post_Auth_Guest_CreatesDistinctAccounts_OnMultipleCalls()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response1 = await client.PostAsJsonAsync("/api/auth/guest", new { });
        var response2 = await client.PostAsJsonAsync("/api/auth/guest", new { });

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        var result1 = await response1.Content.ReadFromJsonAsync<GuestResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<GuestResponse>();

        Assert.NotEqual(result1.PlayerId, result2.PlayerId);
    }

    [Fact]
    public async Task Post_Auth_LinkGooglePlay_RequiresAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/link-google-play", 
            new { ServerAuthCode = "test", VerifiedPlatformId = "test" });

        // Assert - should be unauthorized since no test auth header
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Auth_LinkGooglePlay_DoesNotTrustCallerSuppliedPlatformId_WhenCredentialsAreAbsent()
    {
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, Alias = "TestFox-4821", IsGuest = true });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);
        var response = await client.PostAsJsonAsync("/api/auth/link-google-play",
            new { ServerAuthCode = "unverified-code", VerifiedPlatformId = "attacker-selected-id" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var account = await verifyDb.PlayerAccounts.FindAsync(playerId);
        Assert.Null(account!.LinkedPlatformIdentity);
        Assert.True(account.IsGuest);
    }

    [Fact]
    public async Task Post_Auth_Refresh_RotatesRefreshToken_AndRejectsReuse()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var guestResponse = await client.PostAsJsonAsync("/api/auth/guest", new { });
        var guest = await guestResponse.Content.ReadFromJsonAsync<GuestResponse>();
        Assert.NotNull(guest);

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { guest.RefreshToken });

        response.EnsureSuccessStatusCode();
        var refreshed = await response.Content.ReadFromJsonAsync<GuestResponse>();
        Assert.NotNull(refreshed);
        Assert.Equal(guest.PlayerId, refreshed.PlayerId);
        Assert.NotEqual(guest.RefreshToken, refreshed.RefreshToken);

        var reused = await client.PostAsJsonAsync("/api/auth/refresh", new { guest.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task Post_Auth_Revoke_RequiresAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/revoke", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record GuestResponse(Guid PlayerId, string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAt);
}
