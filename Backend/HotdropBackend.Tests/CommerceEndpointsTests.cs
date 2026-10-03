using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using Xunit;

namespace HotdropBackend.Tests;

public class CommerceEndpointsTests : IClassFixture<HotdropWebApplicationFactory>
{
    private readonly HotdropWebApplicationFactory _factory;

    public CommerceEndpointsTests(HotdropWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Commerce_VerifyPurchase_IdempotentForSamePurchaseToken()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);
        var request = new VerifyPurchaseRequest(ProductId: "signal", PurchaseToken: "purchase-token-123");

        // Act - first verification
        var response1 = await client.PostAsJsonAsync("/api/commerce/verify-purchase", request);
        response1.EnsureSuccessStatusCode();
        var result1 = await response1.Content.ReadFromJsonAsync<VerifyPurchaseResponse>();
        Assert.Equal("pending_verification", result1?.Status);
        Assert.False(result1?.AlreadyProcessed);

        // Act - second verification with same token
        var response2 = await client.PostAsJsonAsync("/api/commerce/verify-purchase", request);
        response2.EnsureSuccessStatusCode();
        var result2 = await response2.Content.ReadFromJsonAsync<VerifyPurchaseResponse>();
        Assert.Equal("pending_verification", result2?.Status);
        Assert.True(result2?.AlreadyProcessed);

        // Assert - only one PurchaseTransaction exists
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var transactions = await db.PurchaseTransactions
                .Where(t => t.GooglePurchaseToken == "purchase-token-123")
                .ToListAsync();
            Assert.Single(transactions);
            Assert.Equal(playerId, transactions[0].PlayerId);
            Assert.Equal("signal", transactions[0].ProductId);
        }
    }

    [Fact]
    public async Task Post_Commerce_VerifyPurchase_DifferentTokensCreateSeparateTransactions()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act - first token
        var request1 = new VerifyPurchaseRequest(ProductId: "signal", PurchaseToken: "token-1");
        var response1 = await client.PostAsJsonAsync("/api/commerce/verify-purchase", request1);
        response1.EnsureSuccessStatusCode();

        // Act - second token
        var request2 = new VerifyPurchaseRequest(ProductId: "foundry", PurchaseToken: "token-2");
        var response2 = await client.PostAsJsonAsync("/api/commerce/verify-purchase", request2);
        response2.EnsureSuccessStatusCode();

        // Assert - two separate transactions
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var transactions = await db.PurchaseTransactions
                .Where(t => t.PlayerId == playerId)
                .ToListAsync();
            Assert.Equal(2, transactions.Count);
            Assert.Contains(transactions, t => t.GooglePurchaseToken == "token-1" && t.ProductId == "signal");
            Assert.Contains(transactions, t => t.GooglePurchaseToken == "token-2" && t.ProductId == "foundry");
        }
    }

    [Fact]
    public async Task Get_Commerce_Catalog_ReturnsDisabledState()
    {
        // Arrange
        var client = _factory.CreateClientWithPlayerId(Guid.NewGuid());

        // Act
        var response = await client.GetAsync("/api/commerce/catalog");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CatalogResponse>();

        // Assert
        Assert.False(result?.Enabled);
        Assert.NotNull(result?.Bundles);
        Assert.Equal(3, result.Bundles.Length);
        Assert.Contains("signal", result.Bundles);
        Assert.Contains("foundry", result.Bundles);
        Assert.Contains("skyline", result.Bundles);
    }

    [Fact]
    public async Task Post_Commerce_Restore_ReturnsOwnedItems()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, IsGuest = true });
            db.InventoryItems.Add(new InventoryItem
            {
                PlayerId = playerId,
                CosmeticId = "signal_body",
                AcquiredVia = "starter"
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/commerce/restore", new { });
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<InventoryItemResponse>>();

        // Assert
        Assert.NotNull(items);
        Assert.Single(items);
        Assert.Equal("signal_body", items[0].CosmeticId);
    }

    [Fact]
    public async Task Commerce_Endpoints_RequireAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act & Assert
        var catalogResponse = await client.GetAsync("/api/commerce/catalog");
        Assert.Equal(HttpStatusCode.Unauthorized, catalogResponse.StatusCode);

        var verifyResponse = await client.PostAsJsonAsync("/api/commerce/verify-purchase", 
            new VerifyPurchaseRequest("signal", "token"));
        Assert.Equal(HttpStatusCode.Unauthorized, verifyResponse.StatusCode);

        var restoreResponse = await client.PostAsJsonAsync("/api/commerce/restore", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, restoreResponse.StatusCode);
    }

    private record VerifyPurchaseRequest(string ProductId, string PurchaseToken);
    private record VerifyPurchaseResponse(string Status, bool AlreadyProcessed);
    private record CatalogResponse(bool Enabled, string[] Bundles);
    private record InventoryItemResponse(string CosmeticId, DateTimeOffset AcquiredAt, string AcquiredVia);
}



