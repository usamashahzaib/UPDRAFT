using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using Xunit;

namespace HotdropBackend.Tests;

public class SupportEndpointsTests : IClassFixture<HotdropWebApplicationFactory>
{
    private readonly HotdropWebApplicationFactory _factory;

    public SupportEndpointsTests(HotdropWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Support_Report_CreatesReport()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var reportedId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.AddRange(
                new PlayerAccount { Id = reporterId, IsGuest = true },
                new PlayerAccount { Id = reportedId, IsGuest = true }
            );
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(reporterId);
        var request = new SubmitReportRequest(reportedId, "cheating", matchId);

        // Act
        var response = await client.PostAsJsonAsync("/api/support/report", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReportResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Submitted);

        // Verify in database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var report = await db.PlayerReports.FirstOrDefaultAsync(r => r.ReportingPlayerId == reporterId && r.ReportedPlayerId == reportedId);
            Assert.NotNull(report);
            Assert.Equal("cheating", report.Reason);
            Assert.Equal(matchId, report.MatchId);
            Assert.Equal("submitted", report.Status);
        }
    }

    [Fact]
    public async Task Post_Support_Report_RequiresAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();
        var request = new SubmitReportRequest(Guid.NewGuid(), "cheating", null);

        // Act
        var response = await client.PostAsJsonAsync("/api/support/report", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Support_AccountDeletion_CreatesRequest()
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

        // Act
        var response = await client.PostAsJsonAsync("/api/support/account-deletion", new { });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DeletionResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Requested);

        // Verify in database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            var request = await db.AccountDeletionRequests.FirstOrDefaultAsync(r => r.PlayerId == playerId);
            Assert.NotNull(request);
            Assert.Equal("requested", request.Status);
        }
    }

    [Fact]
    public async Task Post_Support_AccountDeletion_RequiresAuthorization()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/support/account-deletion", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Support_AccountDeletion_PublicEndpoint_ReturnsHtml()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/support/account-deletion");

        // Assert - public endpoint should work without auth
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Delete your Project HOTDROP account", html);
        Assert.Contains("action=\"/api/support/account-deletion\"", html);
    }

    [Fact]
    public async Task Post_Support_AccountDeletion_PublicForm_CreatesPendingVerificationRequest()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/support/account-deletion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "Example Player",
            ["email"] = "player@example.test",
        }));

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var request = await db.AccountDeletionRequests.SingleAsync(r => r.ContactEmail == "player@example.test");
        Assert.Null(request.PlayerId);
        Assert.Equal("pending_verification", request.Status);
    }

    [Fact]
    public async Task Delete_Support_Account_RemovesPlayerData_ButRetainsPurchaseRecord()
    {
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId });
            db.InventoryItems.Add(new InventoryItem { PlayerId = playerId, CosmeticId = "starter", AcquiredVia = "starter" });
            db.CreditLedgerEntries.Add(new CreditLedgerEntry { PlayerId = playerId, ChangeAmount = 10, Reason = "test", SourceId = Guid.NewGuid().ToString() });
            db.PurchaseTransactions.Add(new PurchaseTransaction { Id = Guid.NewGuid(), PlayerId = playerId, ProductId = "signal", GooglePurchaseToken = Guid.NewGuid().ToString() });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientWithPlayerId(playerId);
        var response = await client.DeleteAsync("/api/support/account");
        response.EnsureSuccessStatusCode();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        Assert.Null(await verifyDb.PlayerAccounts.FindAsync(playerId));
        Assert.False(await verifyDb.InventoryItems.AnyAsync(i => i.PlayerId == playerId));
        Assert.False(await verifyDb.CreditLedgerEntries.AnyAsync(i => i.PlayerId == playerId));
        Assert.True(await verifyDb.PurchaseTransactions.AnyAsync(i => i.PlayerId == playerId));
        Assert.Equal("completed", (await verifyDb.AccountDeletionRequests.SingleAsync(r => r.PlayerId == playerId)).Status);
    }

    private record SubmitReportRequest(Guid ReportedPlayerId, string Reason, Guid? MatchId);
    private record ReportResponse(bool Submitted);
    private record DeletionResponse(bool Requested);
}




