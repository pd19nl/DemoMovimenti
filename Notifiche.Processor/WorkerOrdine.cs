namespace Notifiche.Processor;

public class WorkerOrdine : BackgroundService
{
    private readonly ILogger<WorkerOrdine> _logger;

    public WorkerOrdine(ILogger<WorkerOrdine> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
