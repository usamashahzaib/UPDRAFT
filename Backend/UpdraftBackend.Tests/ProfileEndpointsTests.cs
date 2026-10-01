using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using Xunit;

namespace UpdraftBackend.Tests;

public class ProfileEndpointsTests : IClassFixture<UpdraftWebApplicationFactory>
{
    private readonly UpdraftWebApplicationFactory _factory;

    public ProfileEndpointsTests(UpdraftWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Profile_ReturnsAccount()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount
            {
                Id = playerId,
                IsGuest = true,
                ProfileXp = 500,
                Level = 2,
                HiddenMatchmakingRating = 1200
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.GetAsync("/api/profile");
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<PlayerAccountResponse>();

        // Assert
        Assert.NotNull(account);
        Assert.Equal(playerId, account.Id);
        Assert.Equal(500, account.ProfileXp);
        Assert.Equal(2, account.Level);
        Assert.True(account.IsGuest);
        Assert.DoesNotContain("hiddenMatchmakingRating", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_Profile_NotFound_Returns404()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.GetAsync("/api/profile");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Profile_Inventory_ReturnsOwnedItems()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            db.InventoryItems.AddRange(
                new InventoryItem { PlayerId = playerId, CosmeticId = "signal_body", AcquiredVia = "starter" },
                new InventoryItem { PlayerId = playerId, CosmeticId = "foundry_badge", AcquiredVia = "unlock" }
            );
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.GetAsync("/api/profile/inventory");
        response.EnsureSuccessStatusCode();
        var inventory = await response.Content.ReadFromJsonAsync<InventoryResponse>();

        // Assert
        Assert.NotNull(inventory);
        Assert.Equal(2, inventory.Items.Count);
        Assert.Contains(inventory.Items, i => i.CosmeticId == "signal_body" && i.AcquiredVia == "starter");
        Assert.Contains(inventory.Items, i => i.CosmeticId == "foundry_badge" && i.AcquiredVia == "unlock");
    }

    [Fact]
    public async Task Post_Profile_Equip_OwnedCosmetic_Succeeds()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            db.InventoryItems.Add(new InventoryItem { PlayerId = playerId, CosmeticId = "signal_body", AcquiredVia = "starter" });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/profile/equip/signal_body", new { });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EquipResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("signal_body", result.Equipped);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        Assert.Equal("signal_body", (await verificationDb.PlayerAccounts.FindAsync(playerId))?.EquippedCosmeticId);
    }

    [Fact]
    public async Task Post_Profile_Equip_UnownedCosmetic_Returns403()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            // No inventory item added
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/profile/equip/foundry_badge", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Profile_Endpoints_RequireAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act & Assert
        var profileResponse = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, profileResponse.StatusCode);

        var inventoryResponse = await client.GetAsync("/api/profile/inventory");
        Assert.Equal(HttpStatusCode.Unauthorized, inventoryResponse.StatusCode);

        var equipResponse = await client.PostAsJsonAsync("/api/profile/equip/signal_body", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, equipResponse.StatusCode);
    }

    private record PlayerAccountResponse(Guid Id, string Alias, int ProfileXp, int Level, string? EquippedCosmeticId, string? LinkedPlatformIdentity, bool IsGuest);
    private record InventoryItemResponse(string CosmeticId, DateTimeOffset AcquiredAt, string AcquiredVia);
    private record InventoryResponse(List<InventoryItemResponse> Items);
    private record EquipResponse(string Equipped);
}
