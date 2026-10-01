using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using UpdraftBackend.Services;
using Xunit;

namespace UpdraftBackend.Tests;

/// <summary>Accepts "code:&lt;platformId&gt;" as a verified server auth code; anything else fails verification.</summary>
public sealed class FakeGooglePlayGamesVerifier : IGooglePlayGamesIdentityVerifier
{
    public Task<string?> VerifyPlayerIdAsync(string serverAuthCode, CancellationToken cancellationToken) =>
        Task.FromResult(serverAuthCode.StartsWith("code:", StringComparison.Ordinal) ? serverAuthCode["code:".Length..] : null);
}

public sealed class PlatformIdentityWebApplicationFactory : UpdraftWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("GooglePlayGames:ServerClientId", "test-client-id");
        builder.UseSetting("GooglePlayGames:ServerClientSecret", "test-client-secret");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGooglePlayGamesIdentityVerifier>();
            services.AddSingleton<IGooglePlayGamesIdentityVerifier, FakeGooglePlayGamesVerifier>();
        });
    }
}

/// <summary>QA-026: sign back in to an existing account by its linked platform identity after reinstall.</summary>
public class PlatformSignInTests : IClassFixture<PlatformIdentityWebApplicationFactory>
{
    readonly PlatformIdentityWebApplicationFactory _factory;

    public PlatformSignInTests(PlatformIdentityWebApplicationFactory factory) => _factory = factory;

    async Task<Guid> CreateGuestAsync()
    {
        var response = await _factory.CreateUnauthenticatedClient().PostAsJsonAsync("/api/auth/guest", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionResponse>())!.PlayerId;
    }

    async Task<int> AccountCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UpdraftDbContext>().PlayerAccounts.CountAsync();
    }

    [Fact]
    public async Task SignIn_WithLinkedIdentity_ReturnsSessionForSameAccount_AndCreatesNothing()
    {
        var accountA = await CreateGuestAsync();
        string platformId = "gpg-" + Guid.NewGuid().ToString("N");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.CreditLedgerEntries.Add(new CreditLedgerEntry { PlayerId = accountA, ChangeAmount = 45, Reason = "test", SourceId = "seed" });
            await db.SaveChangesAsync();
        }
        var link = await _factory.CreateClientWithPlayerId(accountA)
            .PostAsJsonAsync("/api/auth/link-google-play", new { ServerAuthCode = "code:" + platformId });
        link.EnsureSuccessStatusCode();
        int accountsBefore = await AccountCountAsync();

        // Fresh install: no session, no player header.
        var response = await _factory.CreateUnauthenticatedClient()
            .PostAsJsonAsync("/api/auth/platform-signin", new { serverAuthCode = "code:" + platformId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(session);
        Assert.Equal(accountA, session.PlayerId);
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(session.RefreshToken));
        Assert.Equal(accountsBefore, await AccountCountAsync());

        // The recovered session is a real one: its refresh token rotates for account A.
        var refreshed = await _factory.CreateUnauthenticatedClient().PostAsJsonAsync("/api/auth/refresh", new { session.RefreshToken });
        Assert.Equal(accountA, (await refreshed.Content.ReadFromJsonAsync<SessionResponse>())!.PlayerId);

        // Inventory and balance are the account's own (starter set + seeded credits), untouched.
        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        Assert.True(await vdb.InventoryItems.AnyAsync(i => i.PlayerId == accountA));
        Assert.Equal(45, await vdb.CreditLedgerEntries.Where(e => e.PlayerId == accountA).SumAsync(e => e.ChangeAmount));
    }

    [Fact]
    public async Task SignIn_WithUnlinkedIdentity_IsNotFound_AndCreatesNoAccount()
    {
        int before = await AccountCountAsync();
        var response = await _factory.CreateUnauthenticatedClient()
            .PostAsJsonAsync("/api/auth/platform-signin", new { serverAuthCode = "code:never-linked-" + Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(before, await AccountCountAsync());
    }

    [Fact]
    public async Task SignIn_AfterLinkRevoked_IsNotFound()
    {
        var account = await CreateGuestAsync();
        string platformId = "gpg-" + Guid.NewGuid().ToString("N");
        var client = _factory.CreateClientWithPlayerId(account);
        (await client.PostAsJsonAsync("/api/auth/link-google-play", new { ServerAuthCode = "code:" + platformId })).EnsureSuccessStatusCode();
        (await client.DeleteAsync("/api/auth/google-play")).EnsureSuccessStatusCode();

        var response = await _factory.CreateUnauthenticatedClient()
            .PostAsJsonAsync("/api/auth/platform-signin", new { serverAuthCode = "code:" + platformId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithUnverifiableCode_IsUnauthorized()
    {
        var response = await _factory.CreateUnauthenticatedClient()
            .PostAsJsonAsync("/api/auth/platform-signin", new { serverAuthCode = "forged" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithMissingCode_IsBadRequest()
    {
        var response = await _factory.CreateUnauthenticatedClient()
            .PostAsJsonAsync("/api/auth/platform-signin", new { serverAuthCode = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WhenPlayGamesNotConfigured_FailsClosed()
    {
        using var unconfigured = new UpdraftWebApplicationFactory();
        var response = await unconfigured.CreateUnauthenticatedClient()
            .PostAsJsonAsync("/api/auth/platform-signin", new { serverAuthCode = "code:anything" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    record SessionResponse(Guid PlayerId, string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAt);
}
