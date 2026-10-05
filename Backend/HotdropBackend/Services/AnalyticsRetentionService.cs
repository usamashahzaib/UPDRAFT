using Microsoft.EntityFrameworkCore;
using HotdropBackend.Data;
using HotdropBackend.Domain;

namespace HotdropBackend.Services;

public sealed class AnalyticsRetentionService(IServiceScopeFactory scopeFactory, ILogger<AnalyticsRetentionService> logger)
    : BackgroundService
{
    // Everything with Timestamp older than this UTC watermark has already been
    // folded into AnalyticsDailyPresence. The first run folds everything stored
    // so far; afterwards the watermark only advances after a successful commit,
    // making the rollup idempotent: a crashed run reprocesses the same window,
    // and events ingested between runs are picked up on the next one.
    DateTimeOffset lastPresenceRollup = DateTimeOffset.MinValue;
    bool presenceWatermarkInitialized;

    // Amendment (measurement section): a session ends after 30 minutes with no
    // gameplay activity. Only events that show the player actually playing count;
    // startup, crash reports and study bookkeeping do not keep a session alive.
    static readonly HashSet<string> GameplayActivityEvents =
    [
        "tutorial_started", "tutorial_step", "tutorial_completed",
        "queue_started", "matchmaking_started", "match_joining", "online_match_joined", "online_match_completed",
        "offline_challenge_started", "offline_challenge_completed",
        "match_started", "match_completed", "pad_placed", "pad_used", "pad_destroyed",
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnce(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Analytics retention job failed."); }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    public async Task RunOnce(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var now = DateTimeOffset.UtcNow;
        var rawCutoff = now.AddDays(-30);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Presence and sessions must fold in before expired raw rows are deleted,
        // inside the same transaction, so no user-day can vanish between the two steps.
        await RollUpDailyPresence(db, cancellationToken);
        await RollUpDailySessions(db, cancellationToken);

        var expiredRaw = await db.AnalyticsEvents.Where(e => e.Timestamp < rawCutoff).ToListAsync(cancellationToken);

        foreach (var group in expiredRaw.GroupBy(e => new { Date = DateOnly.FromDateTime(e.Timestamp.UtcDateTime), e.EventName, e.Region, e.DeviceTier }))
        {
            var aggregate = await db.AnalyticsDailyAggregates.SingleOrDefaultAsync(a =>
                a.EventDate == group.Key.Date && a.EventName == group.Key.EventName &&
                a.Region == group.Key.Region && a.DeviceTier == group.Key.DeviceTier, cancellationToken);
            if (aggregate is null)
            {
                db.AnalyticsDailyAggregates.Add(new AnalyticsDailyAggregate
                {
                    EventDate = group.Key.Date,
                    EventName = group.Key.EventName,
                    Region = group.Key.Region,
                    DeviceTier = group.Key.DeviceTier,
                    EventCount = group.LongCount(),
                    ExpiresAt = new DateTimeOffset(group.Key.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(180),
                });
            }
            else aggregate.EventCount += group.LongCount();
        }

        db.AnalyticsEvents.RemoveRange(expiredRaw);
        var expiredAggregates = await db.AnalyticsDailyAggregates.Where(a => a.ExpiresAt <= now).ToListAsync(cancellationToken);
        db.AnalyticsDailyAggregates.RemoveRange(expiredAggregates);
        var expiredPresence = await db.AnalyticsDailyPresence.Where(p => p.ExpiresAt <= now).ToListAsync(cancellationToken);
        db.AnalyticsDailyPresence.RemoveRange(expiredPresence);
        var expiredSessions = await db.AnalyticsDailySessions.Where(s => s.ExpiresAt <= now).ToListAsync(cancellationToken);
        db.AnalyticsDailySessions.RemoveRange(expiredSessions);
        // Web deletion requests nobody verified hold a plaintext name/email and did nothing;
        // drop them a week after their link expired (completed requests are already scrubbed).
        var abandonedCutoff = now.AddDays(-7);
        var abandonedDeletionRequests = await db.AccountDeletionRequests
            .Where(r => r.Status == AccountDeletionService.PendingVerification && r.VerificationExpiresAt < abandonedCutoff)
            .ToListAsync(cancellationToken);
        db.AccountDeletionRequests.RemoveRange(abandonedDeletionRequests);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        lastPresenceRollup = now;
        presenceWatermarkInitialized = true;
    }

    // One AnalyticsDailyPresence row per pseudonymous player per UTC day with
    // analysable activity (spec §164/§167: retention cohorts are anchored on
    // pseudonymous IDs, not account IDs, so the rollup must survive the 30-day
    // raw window while staying inside the 180-day aggregate retention policy).
    async Task RollUpDailyPresence(HotdropDbContext db, CancellationToken cancellationToken)
    {
        var cutoff = presenceWatermarkInitialized ? lastPresenceRollup : DateTimeOffset.UtcNow;
        var stale = await db.AnalyticsEvents
            .Where(e => e.Timestamp < cutoff)
            .ToListAsync(cancellationToken);
        if (stale.Count == 0) return;

        foreach (var group in stale
            .Where(e => !string.IsNullOrEmpty(e.PseudonymousId))
            .GroupBy(e => new
            {
                Date = DateOnly.FromDateTime((e.ClientTimestamp ?? e.Timestamp).UtcDateTime),
                e.PseudonymousId,
            }))
        {
            var day = group.Key.Date;
            var firstSeen = group.Min(e => e.ClientTimestamp ?? e.Timestamp);
            // Last-write-wins cohort for the day; ordered by event time so the
            // result does not depend on ingestion order.
            var cohort = group.OrderBy(e => e.Timestamp).Last().Cohort;

            var presence = await db.AnalyticsDailyPresence.SingleOrDefaultAsync(p =>
                p.EventDate == day && p.PseudonymousId == group.Key.PseudonymousId, cancellationToken);
            if (presence is null)
            {
                db.AnalyticsDailyPresence.Add(new AnalyticsDailyPresence
                {
                    EventDate = day,
                    PseudonymousId = group.Key.PseudonymousId,
                    Cohort = cohort,
                    FirstSeenAt = firstSeen,
                    ExpiresAt = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(180),
                });
            }
            else
            {
                if (firstSeen < presence.FirstSeenAt) presence.FirstSeenAt = firstSeen;
                presence.Cohort = cohort;
            }
        }
    }

    // One AnalyticsDailySession row per pseudonymous player per UTC day, using the
    // amendment's definitions in SessionRules: a session ends after 30 minutes of
    // no gameplay activity, and a same-day return is a new session at least 30
    // minutes after the previous one ended on the same UTC day. Sessions are
    // attributed to the day they started; a return is credited to the day the
    // player came back. Recomputed from the raw events still inside the 30-day
    // window and written last-write-wins, so reruns and a crashed run converge.
    //
    // Known boundary: raw rows expire after 30 days, so a session whose events have
    // all aged out keeps the daily row written while they were still present rather
    // than being silently re-attributed. Only days that still have raw activity are
    // recomputed, so an aged day is never overwritten with a partial count.
    async Task RollUpDailySessions(HotdropDbContext db, CancellationToken cancellationToken)
    {
        var cutoff = presenceWatermarkInitialized ? lastPresenceRollup : DateTimeOffset.UtcNow;
        var affected = await db.AnalyticsEvents
            .Where(e => e.Timestamp < cutoff && e.PseudonymousId != "" && GameplayActivityEvents.Contains(e.EventName))
            .Select(e => e.PseudonymousId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (affected.Count == 0) return;

        foreach (var pseudonymousId in affected)
        {
            // Full remaining history for this player: sessions are a gap property, so
            // splitting them across rollup windows would invent extra sessions.
            var activity = await db.AnalyticsEvents
                .Where(e => e.PseudonymousId == pseudonymousId && GameplayActivityEvents.Contains(e.EventName))
                .Select(e => new { At = e.ClientTimestamp ?? e.Timestamp, e.IncentivizedTester })
                .ToListAsync(cancellationToken);
            if (activity.Count == 0) continue;

            // Sessions are built from every gameplay event, including incentivized
            // ones: dropping them first would invent extra sessions at the boundary.
            // Voluntariness is then judged per session, so an incentivized tester
            // never contributes a voluntary session.
            var sessions = SessionRules.BuildSessions(activity.Select(a => a.At));

            var perDay = sessions
                .Select((session, index) => new
                {
                    Day = DateOnly.FromDateTime(session.StartedAt.UtcDateTime),
                    Session = session,
                    // The first session of a player cannot be a return: there is no prior one.
                    IsReturn = index > 0 && SessionRules.IsSameDayReturn(sessions[index - 1].EndedAt, session.StartedAt),
                    Voluntary = !activity.Any(a => a.IncentivizedTester && a.At >= session.StartedAt && a.At <= session.EndedAt),
                })
                .GroupBy(x => x.Day);

            foreach (var day in perDay)
            {
                var existing = await db.AnalyticsDailySessions.SingleOrDefaultAsync(s =>
                    s.EventDate == day.Key && s.PseudonymousId == pseudonymousId, cancellationToken);
                if (existing is null)
                {
                    db.AnalyticsDailySessions.Add(new AnalyticsDailySession
                    {
                        EventDate = day.Key,
                        PseudonymousId = pseudonymousId,
                        Sessions = day.Count(),
                        SameDayReturns = day.Count(x => x.IsReturn),
                        VoluntarySessions = day.Count(x => x.Voluntary),
                        ExpiresAt = new DateTimeOffset(day.Key.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(180),
                    });
                }
                else
                {
                    existing.Sessions = day.Count();
                    existing.SameDayReturns = day.Count(x => x.IsReturn);
                    existing.VoluntarySessions = day.Count(x => x.Voluntary);
                }
            }
        }
    }
}



