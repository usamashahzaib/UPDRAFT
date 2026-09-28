using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Services;

public sealed class AnalyticsRetentionService(IServiceScopeFactory scopeFactory, ILogger<AnalyticsRetentionService> logger)
    : BackgroundService
{
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
        var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var now = DateTimeOffset.UtcNow;
        var rawCutoff = now.AddDays(-30);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
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
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
