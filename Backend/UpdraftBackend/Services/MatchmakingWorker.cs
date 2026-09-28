namespace UpdraftBackend.Services;

public sealed class MatchmakingWorker(MatchmakingComposer composer, MatchmakingOptions options, ILogger<MatchmakingWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await composer.RunOnceAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Matchmaking composition pass failed."); }
            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken);
        }
    }
}
