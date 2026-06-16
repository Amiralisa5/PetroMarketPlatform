namespace PetroMarketPlatform.API.Services;

/// <summary>
/// Server-authoritative deadline enforcement (BR-8). Periodically closes competitions whose bidding
/// window has elapsed. In production this maps to a BullMQ delayed job per competition (§8); here a
/// lightweight polling loop achieves the same exactly-once close via the service's guarded update.
/// </summary>
public class CompetitionCloserService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<CompetitionCloserService> _log;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    public CompetitionCloserService(IServiceProvider sp, ILogger<CompetitionCloserService> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<ICompetitionService>();
                var closed = await svc.CloseDueCompetitionsAsync(stoppingToken);
                if (closed > 0) _log.LogInformation("Auto-closed {Count} competition(s) past deadline", closed);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CompetitionCloserService tick failed");
            }
            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}
