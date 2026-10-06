namespace HotdropBackend.Services;

public sealed class MatchmakingWorker(MatchmakingComposer composer, MatchmakingOptions options,
    Microsoft.Extensions.Options.IOptionsMonitor<FeatureFlags> features,
    Microsoft.Extensions.Options.IOptionsMonitor<ServiceAvailabilityOptions> availability,
    ILogger<MatchmakingWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Leave searching tickets intact during a pause; allocation resumes when
                // the operator reopens the queue.
                if (features.CurrentValue.MatchmakingEnabled && !availability.CurrentValue.MaintenanceEnabled)
                    await composer.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) { logger.LogError(ex, "Matchmaking composition pass failed."); }
            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken);
        }
    }
}

