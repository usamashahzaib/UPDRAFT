using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;

namespace HotdropBackend.Services;

/// <summary>"Support" configuration for the public web deletion path.</summary>
public sealed class SupportOptions
{
    /// <summary>Publisher-owned public origin used in emailed links. Production requires HTTPS.</summary>
    public string? PublicBaseUrl { get; set; }
    public int DeletionLinkLifetimeHours { get; set; } = 24;
    public int EffectiveDeletionLinkLifetimeHours => Math.Clamp(DeletionLinkLifetimeHours, 1, 168);

    public void Validate(Microsoft.Extensions.Hosting.IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;

        if (!Uri.TryCreate(PublicBaseUrl?.Trim(), UriKind.Absolute, out var publicUri) ||
            publicUri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(publicUri.Host) ||
            !string.IsNullOrEmpty(publicUri.UserInfo) || !string.IsNullOrEmpty(publicUri.Query) ||
            !string.IsNullOrEmpty(publicUri.Fragment) || publicUri.AbsolutePath != "/")
        {
            throw new InvalidOperationException(
                "Production requires Support:PublicBaseUrl to be a publisher-controlled HTTPS origin without a path, credentials, query, or fragment.");
        }
    }
}

/// <summary>
/// One account-deletion code path shared by the in-app DELETE /api/support/account
/// and the verified public web request (QA-028).
/// </summary>
public sealed class AccountDeletionService
{
    public const string PendingVerification = "pending_verification";

    /// <summary>Deletes the account and its player data; purchase records are retained. False when no such account.</summary>
    public async Task<bool> DeleteAccountAsync(Guid playerId, HotdropDbContext db, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await DeleteAccountCoreAsync(playerId, db, cancellationToken)) return false;
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    async Task<bool> DeleteAccountCoreAsync(Guid playerId, HotdropDbContext db, CancellationToken cancellationToken)
    {
        var account = await db.PlayerAccounts.FindAsync([playerId], cancellationToken);
        if (account is null) return false;

        var membership = await db.PartyMembers.SingleOrDefaultAsync(m => m.PlayerId == playerId, cancellationToken);
        if (membership is not null)
        {
            var party = await db.Parties.FindAsync([membership.PartyId], cancellationToken);
            db.PartyMembers.Remove(membership);
            await db.SaveChangesAsync(cancellationToken);
            var remaining = await db.PartyMembers.Where(m => m.PartyId == membership.PartyId).ToListAsync(cancellationToken);
            if (party is not null && remaining.Count == 0) db.Parties.Remove(party);
            else if (party is not null && party.LeaderId == playerId) party.LeaderId = remaining.OrderBy(m => m.JoinedAt).First().PlayerId;
        }

        db.PlayerSessions.RemoveRange(db.PlayerSessions.Where(s => s.PlayerId == playerId));
        db.InventoryItems.RemoveRange(db.InventoryItems.Where(i => i.PlayerId == playerId));
        db.CreditLedgerEntries.RemoveRange(db.CreditLedgerEntries.Where(e => e.PlayerId == playerId));
        db.MatchmakingTickets.RemoveRange(db.MatchmakingTickets.Where(t => t.PlayerId == playerId));
        db.MatchParticipations.RemoveRange(db.MatchParticipations.Where(m => m.PlayerId == playerId));
        db.ContractGoalInstances.RemoveRange(db.ContractGoalInstances.Where(g => g.PlayerId == playerId));
        db.FlightSealGrants.RemoveRange(db.FlightSealGrants.Where(g => g.PlayerId == playerId));
        db.MasteryProgresses.RemoveRange(db.MasteryProgresses.Where(m => m.PlayerId == playerId));
        db.PlayerReports.RemoveRange(db.PlayerReports.Where(r => r.ReportingPlayerId == playerId || r.ReportedPlayerId == playerId));
        db.AnalyticsEvents.RemoveRange(db.AnalyticsEvents.Where(e => e.PlayerId == playerId));
        db.PlayerAccounts.Remove(account);

        var now = DateTimeOffset.UtcNow;
        var requests = await db.AccountDeletionRequests.Where(r => r.PlayerId == playerId).ToListAsync(cancellationToken);
        foreach (var request in requests)
        {
            request.Status = "completed";
            request.CompletedAt ??= now;
        }
        if (requests.Count == 0)
            db.AccountDeletionRequests.Add(new AccountDeletionRequest { Id = Guid.NewGuid(), PlayerId = playerId, Status = "completed", CompletedAt = now });

        // Purchase records are deliberately retained for accounting/legal obligations.
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static string NewToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string HashEmail(string email) => Hash("contact-email:" + NormalizeEmail(email));

    public static bool IsValidEmail(string email) =>
        email.Length is >= 3 and <= 254 && System.Net.Mail.MailAddress.TryCreate(email, out var parsed) && parsed.Address == email;

    /// <summary>
    /// Completes a verified web request: deletes every account whose contact email
    /// matches (possibly none) and marks the request completed. The caller's response
    /// must not depend on how many accounts matched.
    /// </summary>
    public async Task<bool> CompleteVerifiedRequestAsync(string token, HotdropDbContext db, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256) return false;
        string hash = Hash(token);
        var now = DateTimeOffset.UtcNow;
        var candidate = await db.AccountDeletionRequests.AsNoTracking()
            .Where(r => r.VerificationTokenHash == hash && r.Status == PendingVerification &&
                        r.VerificationExpiresAt != null && r.VerificationExpiresAt > now)
            .Select(r => new { r.Id, r.ContactEmail })
            .SingleOrDefaultAsync(cancellationToken);
        if (candidate is null) return false;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // The conditional update is the single-use claim. Concurrent confirmations
        // race on one row; only the request that still matches the unconsumed token
        // and expiry can proceed. Keep it in the same transaction as the deletion so
        // a database failure does not consume the player's only valid link.
        int claimed = await db.AccountDeletionRequests
            .Where(r => r.Id == candidate.Id && r.VerificationTokenHash == hash &&
                        r.Status == PendingVerification && r.VerificationExpiresAt != null &&
                        r.VerificationExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.VerificationTokenHash, (string?)null)
                .SetProperty(r => r.VerificationExpiresAt, (DateTimeOffset?)null)
                .SetProperty(r => r.Status, "processing"), cancellationToken);
        if (claimed != 1) return false;

        var request = await db.AccountDeletionRequests.SingleAsync(r => r.Id == candidate.Id, cancellationToken);
        string emailHash = HashEmail(candidate.ContactEmail ?? string.Empty);
        var accountIds = await db.PlayerAccounts.Where(a => a.ContactEmailHash == emailHash)
            .Select(a => a.Id).ToListAsync(cancellationToken);
        if (accountIds.Count > 0) request.PlayerId = accountIds[0];

        // The request is kept only as an anonymous audit record: once acted on, the
        // plaintext name/email it was matched by must not outlive the account itself.
        request.ContactEmail = null;
        request.ContactName = null;
        request.Status = "completed";
        request.CompletedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        foreach (var id in accountIds) await DeleteAccountCoreAsync(id, db, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> IsUsableTokenAsync(string token, HotdropDbContext db, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256) return false;
        string hash = Hash(token);
        var now = DateTimeOffset.UtcNow;
        var request = await db.AccountDeletionRequests.AsNoTracking().SingleOrDefaultAsync(r =>
            r.VerificationTokenHash == hash && r.Status == PendingVerification, cancellationToken);
        return request is { VerificationExpiresAt: not null } && request.VerificationExpiresAt > now;
    }

    static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}



