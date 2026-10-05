using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using Xunit;

namespace HotdropBackend.Tests;

public class ProfileEndpointsTests : IClassFixture<HotdropWebApplicationFactory>
{
    private readonly HotdropWebApplicationFactory _factory;

    public ProfileEndpointsTests(HotdropWebApplicationFactory factory)
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
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
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
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
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
    public async Task Get_Profile_Stats_ReturnsOnlyEligibleMatchFacts()
    {
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId });
            db.MatchParticipations.AddRange(
                new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = playerId, Eligible = true, Won = true },
                new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = playerId, Eligible = true, Draw = true },
                new MatchParticipation { MatchId = Guid.NewGuid(), PlayerId = playerId, Eligible = false, Won = true });
            await db.SaveChangesAsync();
        }

        var response = await _factory.CreateClientWithPlayerId(playerId).GetAsync("/api/profile/stats");
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<ProfileStatsResponse>();
        Assert.NotNull(stats);
        Assert.Equal(2, stats.EligibleMatches);
        Assert.Equal(1, stats.Wins);
        Assert.Equal(1, stats.Draws);
    }

    [Fact]
    public async Task Post_Profile_Equip_OwnedCosmetic_Succeeds()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
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
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        Assert.Equal("signal_body", (await verificationDb.PlayerAccounts.FindAsync(playerId))?.EquippedCosmeticId);
    }

    [Fact]
    public async Task Post_Profile_Equip_UnownedCosmetic_Returns403()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
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
    public async Task Equip_WritesEachSlotIndependently_AndRigNeedsNoOwnership()
    {
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            db.InventoryItems.AddRange(
                new InventoryItem { PlayerId = playerId, CosmeticId = "body_teal_stripe", AcquiredVia = "unlock" },
                new InventoryItem { PlayerId = playerId, CosmeticId = "wrap_hazard", AcquiredVia = "unlock" },
                new InventoryItem { PlayerId = playerId, CosmeticId = "badge_summit", AcquiredVia = "unlock" });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);
        foreach (var id in new[] { "body_teal_stripe", "wrap_hazard", "badge_summit" })
            (await client.PostAsJsonAsync("/api/profile/equip/" + id, new { })).EnsureSuccessStatusCode();
        // Amendment 3: body selection is a catalog choice and never requires earning it.
        (await client.PostAsJsonAsync("/api/profile/equip/rig_female", new { })).EnsureSuccessStatusCode();

        // Amendment 3: an id outside every known category is rejected, not silently stored.
        var unknown = await client.PostAsJsonAsync("/api/profile/equip/not_a_cosmetic", new { });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var account = await db2.PlayerAccounts.FindAsync(playerId);
        Assert.NotNull(account);
        Assert.Equal("body_teal_stripe", account.BodyStyleId);
        Assert.Equal("wrap_hazard", account.WeaponWrapId);
        Assert.Equal("badge_summit", account.BadgeId);
        Assert.Equal("rig_female", account.BodyRigId);
        // Backward-compatible adapter: old clients still read a single body style.
        Assert.Equal("body_teal_stripe", account.EquippedCosmeticId);

        var profile = await (await client.GetAsync("/api/profile/")).Content
            .ReadFromJsonAsync<SlotProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("body_teal_stripe", profile.BodyStyleId);
        Assert.Equal("wrap_hazard", profile.WeaponWrapId);
        Assert.Equal("badge_summit", profile.BadgeId);
        Assert.Equal("rig_female", profile.BodyRigId);
    }

    [Fact]
    public async Task Get_Profile_BackfillsLegacySingleValueIntoBodyStyleSlot()
    {
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, EquippedCosmeticId = "body_rooftop" });
            await db.SaveChangesAsync();
        }

        var profile = await (await _factory.CreateClientWithPlayerId(playerId).GetAsync("/api/profile/"))
            .Content.ReadFromJsonAsync<SlotProfileResponse>();

        Assert.NotNull(profile);
        Assert.Equal("body_rooftop", profile.BodyStyleId);
        Assert.Equal("body_rooftop", profile.EquippedCosmeticId);
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
    private record SlotProfileResponse(Guid Id, string Alias, int ProfileXp, int Level,
        string? EquippedCosmeticId, string? LinkedPlatformIdentity, bool IsGuest,
        string? BodyStyleId, string? WeaponWrapId, string? BadgeId, string? BodyRigId);
    private record ProfileStatsResponse(int EligibleMatches, int Wins, int Draws);
}




