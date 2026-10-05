namespace HotdropBackend.Domain;

// Spec section 23: minimum persistent records.

public class PlayerAccount
{
    public Guid Id { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string? LinkedPlatformIdentity { get; set; } // e.g. Google Play Games player ID, verified server-side
    public int ProfileXp { get; set; }
    public int Level { get; set; } = 1;
    public int HiddenMatchmakingRating { get; set; } = 1000;
    // Amendment 3: independent validated appearance slots + rig. Body selection is free.
    // The legacy single value is kept as a compatibility adapter until old clients retire.
    public string? EquippedCosmeticId { get; set; }
    public string? BodyStyleId { get; set; }
    public string? WeaponWrapId { get; set; }
    public string? BadgeId { get; set; }
    public string? BodyRigId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsGuest { get; set; } = true;

    // QA-028: SHA-256 of the normalized contact email the player chose in-app, so a
    // verified public web deletion request can be matched to the account. The
    // plaintext address is never stored on the account.
    public string? ContactEmailHash { get; set; }
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

    // QA-025: what other players saw in-match (alias snapshot at result time and
    // the server-assigned slot), so a report made from the results/scoreboard UI
    // can be resolved to the account without the client ever exposing a GUID.
    public string? Alias { get; set; }
    public int? Slot { get; set; }

    // QA-017: exactly what the result granted, so the client's results screen can
    // show authoritative rewards from a receipt (0/empty when ineligible).
    public int XpGained { get; set; }
    public int CreditsGained { get; set; }
    public string? UnlockedCsv { get; set; }
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
    // QA-025: moderation context — the in-match alias/slot the reporter picked, when reported that way.
    public string? ReportedAlias { get; set; }
    public int? ReportedSlot { get; set; }
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

    // QA-028: single-use email verification for the public web path. Only the
    // SHA-256 of the token is stored; it is cleared once used.
    public string? VerificationTokenHash { get; set; }
    public DateTimeOffset? VerificationExpiresAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
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
    // QA-027: client envelope dimensions (see Unity Telemetry.cs).
    public bool BotSubstituted { get; set; }
    public int BotCount { get; set; }
    public bool IncentivizedTester { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string PayloadJson { get; set; } = "{}";

    // Spec section 164: the pseudonymous ID travels with every event. Retention
    // (D1/D7/D30), crash-free-per-user and per-user monetization KPIs are only
    // computable if ingestion persists it; it is pseudonymous, never an account
    // or device identifier.
    public string PseudonymousId { get; set; } = string.Empty;

    // Event time as reported by the client. Ingestion stamps Timestamp (server
    // receive time); offline-collected events must keep their real event time,
    // so ClientTimestamp preserves the client clock for later analysis.
    public DateTimeOffset? ClientTimestamp { get; set; }
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
/// One row per pseudonymous player per UTC day with any analysable activity.
/// Aggregated (no event detail) so it can outlive the 30-day raw window and
/// support retention windows up to the 180-day aggregate retention; presence
/// rows are pseudonymous and carry no payload content.
/// </summary>
public class AnalyticsDailyPresence
{
    public long Id { get; set; }
    public DateOnly EventDate { get; set; }
    public string PseudonymousId { get; set; } = string.Empty;
    public string Cohort { get; set; } = string.Empty;
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// One row per pseudonymous player per UTC day with the amendment's session
/// definitions applied (see Services.SessionRules): how many playable sessions the
/// player had that day, how many of them were same-day return visits, and how many
/// were voluntary rather than incentive/task-driven. Aggregated like
/// AnalyticsDailyPresence so the measure survives the 30-day raw window.
/// </summary>
public class AnalyticsDailySession
{
    public long Id { get; set; }
    public DateOnly EventDate { get; set; }
    public string PseudonymousId { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int SameDayReturns { get; set; }
    public int VoluntarySessions { get; set; }
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

/// <summary>Server-assigned, versioned contract instance. Template IDs are reusable; instance IDs are not.</summary>
public class ContractGoalInstance
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string CatalogVersion { get; set; } = "v1";
    public string TemplateId { get; set; } = string.Empty;
    public string Status { get; set; } = "paused"; // active, paused, completed
    public int Progress { get; set; }
    public int Target { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>One non-purchasable Flight Seal per completed goal instance.</summary>
public class FlightSealGrant
{
    public long Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid GoalInstanceId { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Permanent, server-derived counters. Weapon is ARC, ZIP or BRICK for weapon familiarity.</summary>
public class MasteryProgress
{
    public Guid PlayerId { get; set; }
    public string TrackId { get; set; } = string.Empty;
    public string WeaponId { get; set; } = string.Empty;
    public int Progress { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

