namespace UpdraftBackend.Domain;

// Spec section 23: minimum persistent records.

public class PlayerAccount
{
    public Guid Id { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string? LinkedPlatformIdentity { get; set; } // e.g. Google Play Games player ID, verified server-side
    public int ProfileXp { get; set; }
    public int Level { get; set; } = 1;
    public int HiddenMatchmakingRating { get; set; } = 1000;
    public string? EquippedCosmeticId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsGuest { get; set; } = true;
}

public class PlayerSession
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public class Party
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid LeaderId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PartyMember
{
    public Guid PartyId { get; set; }
    public Guid PlayerId { get; set; }
    public bool IsReady { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Currency ledger entry. A unique constraint on (Reason, SourceId) must
/// prevent the same source event from granting a reward twice (spec section 23/28).
/// </summary>
public class CreditLedgerEntry
{
    public long Id { get; set; }
    public Guid PlayerId { get; set; }
    public int ChangeAmount { get; set; }
    public string Reason { get; set; } = string.Empty; // e.g. "match_completion", "win_bonus", "level_up"
    public string SourceId { get; set; } = string.Empty; // match id or transaction id — uniqueness key with Reason
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class InventoryItem
{
    public long Id { get; set; }
    public Guid PlayerId { get; set; }
    public string CosmeticId { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAt { get; set; } = DateTimeOffset.UtcNow;
    public string AcquiredVia { get; set; } = string.Empty; // "starter", "unlock", "purchase:<txn_id>"
}

public class MatchParticipation
{
    public Guid MatchId { get; set; }
    public Guid PlayerId { get; set; }
    public bool Won { get; set; }
    public bool Draw { get; set; }
    public bool Eligible { get; set; } // 75% presence, no AFK removal, present at result
    public bool HadBotSubstitution { get; set; }
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PurchaseTransaction
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string ProductId { get; set; } = string.Empty; // "signal", "foundry", "skyline"
    public string GooglePurchaseToken { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, verified, acknowledged, refunded, revoked
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PlayerReport
{
    public Guid Id { get; set; }
    public Guid ReportingPlayerId { get; set; }
    public Guid ReportedPlayerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? MatchId { get; set; }
    public string Status { get; set; } = "submitted"; // submitted, reviewed, actioned, dismissed
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AccountDeletionRequest
{
    public Guid Id { get; set; }
    public Guid? PlayerId { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string Status { get; set; } = "requested"; // pending_verification, requested, completed
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AnalyticsEvent
{
    public long Id { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string Source { get; set; } = string.Empty;
    public Guid? PlayerId { get; set; }
    public Guid? MatchId { get; set; }
    public string BuildVersion { get; set; } = string.Empty;
    public string BalanceVersion { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string DeviceTier { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public bool HasBots { get; set; }
    public string Cohort { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public class AnalyticsDailyAggregate
{
    public long Id { get; set; }
    public DateOnly EventDate { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string DeviceTier { get; set; } = string.Empty;
    public long EventCount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Spec section 14: one persistent queue entry per player. Party members share
/// PartyId so the composer keeps them on the same team and never splits a party.
/// </summary>
public class MatchmakingTicket
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid? PartyId { get; set; }
    public string Region { get; set; } = "default";
    public int Rating { get; set; }
    public DateTimeOffset EnqueuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "searching"; // searching, matched, cancelled
    public Guid? MatchId { get; set; }
    public string? Team { get; set; } // "A" or "B" once matched
    public bool HasBots { get; set; }
}

/// <summary>
/// Spec sections 20/29: a composed match is hosted by exactly one dedicated server.
/// Servers in the pool claim matches; MatchId is the key, so a claim is atomic.
/// </summary>
public class MatchAllocation
{
    public Guid MatchId { get; set; }
    public string ServerId { get; set; } = "";
    public DateTimeOffset ClaimedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
