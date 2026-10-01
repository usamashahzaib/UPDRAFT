using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using UpdraftBackend.Services;
using Xunit;

namespace UpdraftBackend.Tests;

public sealed class CapturingEmailSender : IEmailSender
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public EmailMessage LastTo(string to) => Sent.Last(m => m.To == to);
}

public sealed class EmailCapturingWebApplicationFactory : UpdraftWebApplicationFactory
{
    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Support:PublicBaseUrl", "https://updraft.test");
        // Every TestServer request shares one "IP"; lift the per-IP auth limit for this fixture only.
        builder.UseSetting("RateLimits:AuthPermitPerMinute", "1000");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }
}

/// <summary>QA-028: the public web deletion request is verified by email and then completed.</summary>
public class WebAccountDeletionTests : IClassFixture<EmailCapturingWebApplicationFactory>
{
    readonly EmailCapturingWebApplicationFactory _factory;

    public WebAccountDeletionTests(EmailCapturingWebApplicationFactory factory) => _factory = factory;

    static string NewEmail() => $"player-{Guid.NewGuid():N}@example.test";

    async Task<HttpResponseMessage> RequestDeletionAsync(string email) =>
        await _factory.CreateUnauthenticatedClient().PostAsync("/api/support/account-deletion",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["name"] = "Example Player", ["email"] = email }));

    string TokenFromLastEmail(string email)
    {
        var message = _factory.Emails.LastTo(email);
        var match = Regex.Match(message.TextBody, @"https://updraft\.test/api/support/deletion/verify\?token=([A-Za-z0-9_\-%]+)");
        Assert.True(match.Success, "verification link missing from email");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    async Task<HttpResponseMessage> ConfirmAsync(string token) =>
        await _factory.CreateUnauthenticatedClient().PostAsync("/api/support/deletion/verify",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token }));

    async Task<Guid> CreateAccountWithContactEmailAsync(string email)
    {
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, Alias = "Web-" + playerId.ToString("N")[..8] });
            db.InventoryItems.Add(new InventoryItem { PlayerId = playerId, CosmeticId = "body_standard", AcquiredVia = "starter" });
            db.CreditLedgerEntries.Add(new CreditLedgerEntry { PlayerId = playerId, ChangeAmount = 20, Reason = "test", SourceId = "seed" });
            db.PurchaseTransactions.Add(new PurchaseTransaction { Id = Guid.NewGuid(), PlayerId = playerId, ProductId = "signal", GooglePurchaseToken = Guid.NewGuid().ToString() });
            await db.SaveChangesAsync();
        }
        var set = await _factory.CreateClientWithPlayerId(playerId).PutAsJsonAsync("/api/support/contact-email", new { email = email.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        return playerId;
    }

    [Fact]
    public async Task Request_SendsSingleUseLink_AndStoresOnlyTokenHash()
    {
        string email = NewEmail();
        var response = await RequestDeletionAsync(email);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string token = TokenFromLastEmail(email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var request = await db.AccountDeletionRequests.SingleAsync(r => r.ContactEmail == email);
        Assert.Equal("pending_verification", request.Status);
        Assert.NotNull(request.VerificationTokenHash);
        Assert.NotEqual(token, request.VerificationTokenHash);
        Assert.Equal(AccountDeletionService.Hash(token), request.VerificationTokenHash);
        Assert.True(request.VerificationExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task VerifyGet_OnlyRendersConfirmation_AndDeletesNothing()
    {
        string email = NewEmail();
        var playerId = await CreateAccountWithContactEmailAsync(email);
        await RequestDeletionAsync(email);
        string token = TokenFromLastEmail(email);

        var page = await _factory.CreateUnauthenticatedClient().GetAsync($"/api/support/deletion/verify?token={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("method=\"post\"", await page.Content.ReadAsStringAsync());
        using var scope = _factory.Services.CreateScope();
        Assert.NotNull(await scope.ServiceProvider.GetRequiredService<UpdraftDbContext>().PlayerAccounts.FindAsync(playerId));
    }

    [Fact]
    public async Task VerifyPost_WithValidToken_PerformsInAppDeletion_AndCompletesRequest()
    {
        string email = NewEmail();
        var playerId = await CreateAccountWithContactEmailAsync(email);
        await RequestDeletionAsync(email);

        var response = await ConfirmAsync(TokenFromLastEmail(email));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        Assert.Null(await db.PlayerAccounts.FindAsync(playerId));
        Assert.False(await db.InventoryItems.AnyAsync(i => i.PlayerId == playerId));
        Assert.False(await db.CreditLedgerEntries.AnyAsync(e => e.PlayerId == playerId));
        Assert.True(await db.PurchaseTransactions.AnyAsync(p => p.PlayerId == playerId)); // retained, as in-app
        var request = await db.AccountDeletionRequests.SingleAsync(r => r.PlayerId == playerId);
        Assert.Equal("completed", request.Status);
        Assert.Null(request.ContactEmail);
        Assert.NotNull(request.CompletedAt);
        Assert.Null(request.VerificationTokenHash);
    }

    [Fact]
    public async Task VerifyPost_TokenInQueryString_AlsoWorks()
    {
        string email = NewEmail();
        var playerId = await CreateAccountWithContactEmailAsync(email);
        await RequestDeletionAsync(email);
        string token = TokenFromLastEmail(email);

        var response = await _factory.CreateUnauthenticatedClient()
            .PostAsync($"/api/support/deletion/verify?token={Uri.EscapeDataString(token)}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<UpdraftDbContext>().PlayerAccounts.FindAsync(playerId));
    }

    [Fact]
    public async Task UsedToken_IsRejected()
    {
        string email = NewEmail();
        await CreateAccountWithContactEmailAsync(email);
        await RequestDeletionAsync(email);
        string token = TokenFromLastEmail(email);
        Assert.Equal(HttpStatusCode.OK, (await ConfirmAsync(token)).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync(token)).StatusCode);
        var get = await _factory.CreateUnauthenticatedClient().GetAsync($"/api/support/deletion/verify?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.BadRequest, get.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected_AndAccountSurvives()
    {
        string email = NewEmail();
        var playerId = await CreateAccountWithContactEmailAsync(email);
        await RequestDeletionAsync(email);
        string token = TokenFromLastEmail(email);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            var request = await db.AccountDeletionRequests.SingleAsync(r => r.ContactEmail == email);
            request.VerificationExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync(token)).StatusCode);
        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        Assert.NotNull(await vdb.PlayerAccounts.FindAsync(playerId));
        Assert.Equal("pending_verification", (await vdb.AccountDeletionRequests.SingleAsync(r => r.ContactEmail == email)).Status);
    }

    [Fact]
    public async Task UnknownToken_IsRejected()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync("not-a-real-token")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync("")).StatusCode);
    }

    [Fact]
    public async Task RepeatRequest_RotatesLink_OldLinkStopsWorking()
    {
        string email = NewEmail();
        var playerId = await CreateAccountWithContactEmailAsync(email);
        await RequestDeletionAsync(email);
        string first = TokenFromLastEmail(email);
        await RequestDeletionAsync(email);
        string second = TokenFromLastEmail(email);
        Assert.NotEqual(first, second);

        Guid requestId;
        using (var before = _factory.Services.CreateScope())
        {
            var pending = await before.ServiceProvider.GetRequiredService<UpdraftDbContext>()
                .AccountDeletionRequests.Where(r => r.ContactEmail == email).ToListAsync();
            Assert.Single(pending); // the repeat request rotated the link on the same record
            requestId = pending[0].Id;
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync(first)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ConfirmAsync(second)).StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        Assert.Null(await db.PlayerAccounts.FindAsync(playerId));
        var completed = await db.AccountDeletionRequests.SingleAsync(r => r.Id == requestId);
        Assert.Equal("completed", completed.Status);
        // Only an anonymous audit record remains: no plaintext contact details outlive the account.
        Assert.Null(completed.ContactEmail);
        Assert.Null(completed.ContactName);
    }

    [Fact]
    public async Task Responses_DoNotRevealWhetherEmailHasAnAccount()
    {
        string withAccount = NewEmail();
        string withoutAccount = NewEmail();
        await CreateAccountWithContactEmailAsync(withAccount);

        var a = await RequestDeletionAsync(withAccount);
        var b = await RequestDeletionAsync(withoutAccount);
        Assert.Equal(a.StatusCode, b.StatusCode);
        Assert.Equal(await a.Content.ReadAsStringAsync(), await b.Content.ReadAsStringAsync());

        // Both addresses get a link, with identical wording apart from the token.
        string Strip(string body) => Regex.Replace(body, @"token=\S+", "token=X");
        Assert.Equal(Strip(_factory.Emails.LastTo(withAccount).TextBody), Strip(_factory.Emails.LastTo(withoutAccount).TextBody));

        Guid withoutAccountRequestId;
        using (var before = _factory.Services.CreateScope())
            withoutAccountRequestId = (await before.ServiceProvider.GetRequiredService<UpdraftDbContext>()
                .AccountDeletionRequests.SingleAsync(r => r.ContactEmail == withoutAccount)).Id;

        var confirmA = await ConfirmAsync(TokenFromLastEmail(withAccount));
        var confirmB = await ConfirmAsync(TokenFromLastEmail(withoutAccount));
        Assert.Equal(confirmA.StatusCode, confirmB.StatusCode);
        Assert.Equal(await confirmA.Content.ReadAsStringAsync(), await confirmB.Content.ReadAsStringAsync());
        using var scope = _factory.Services.CreateScope();
        Assert.Equal("completed", (await scope.ServiceProvider.GetRequiredService<UpdraftDbContext>()
            .AccountDeletionRequests.SingleAsync(r => r.Id == withoutAccountRequestId)).Status);
    }

    [Fact]
    public async Task ContactEmail_RequiresValidAddress_AndAuth()
    {
        var playerId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = playerId, Alias = "Mail-" + playerId.ToString("N")[..8] });
            await db.SaveChangesAsync();
        }
        var bad = await _factory.CreateClientWithPlayerId(playerId).PutAsJsonAsync("/api/support/contact-email", new { email = "nope" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var anon = await _factory.CreateUnauthenticatedClient().PutAsJsonAsync("/api/support/contact-email", new { email = "a@example.test" });
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        var ok = await _factory.CreateClientWithPlayerId(playerId).PutAsJsonAsync("/api/support/contact-email", new { email = "Mixed@Example.Test" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        using var verify = _factory.Services.CreateScope();
        var account = await verify.ServiceProvider.GetRequiredService<UpdraftDbContext>().PlayerAccounts.FindAsync(playerId);
        Assert.Equal(AccountDeletionService.HashEmail("mixed@example.test"), account!.ContactEmailHash);
        Assert.DoesNotContain("@", account.ContactEmailHash);
    }

    [Fact]
    public async Task OutboxSender_WritesMessageToLocalDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "updraft-outbox-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sender = new OutboxEmailSender(new EmailOptions { OutboxDirectory = dir, From = "support@updraft.test" },
                NullLogger<OutboxEmailSender>.Instance);
            await sender.SendAsync(new EmailMessage("someone@example.test", "Subject line", "Body with link"), CancellationToken.None);

            var file = Assert.Single(Directory.GetFiles(dir, "*.eml"));
            string text = await File.ReadAllTextAsync(file);
            Assert.Contains("To: someone@example.test", text);
            Assert.Contains("Subject: Subject line", text);
            Assert.Contains("Body with link", text);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
