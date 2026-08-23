namespace TflQuery;

public partial class Poller(Func<CancellationToken, Task> pollAction, TimeSpan pollInterval, ILogger<Poller> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogPollerStartedWithPollInterval(pollInterval);
        using var timer = new PeriodicTimer(pollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                LogPollerExecutedAtTime(DateTimeOffset.Now);
                await pollAction(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "An exception occurred while executing poll action");
            }
        }
    }

    [LoggerMessage(LogLevel.Debug, "Poller executed at {Time}")]
    partial void LogPollerExecutedAtTime(DateTimeOffset time);

    [LoggerMessage(LogLevel.Information, "Poller started with {PollInterval}")]
    partial void LogPollerStartedWithPollInterval(TimeSpan pollInterval);
}