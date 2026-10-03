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

        // Presence must fold in before expired raw rows are deleted, inside the
        // same transaction, so no user-day can vanish between the two steps.
        await RollUpDailyPresence(db, cancellationToken);

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
}



