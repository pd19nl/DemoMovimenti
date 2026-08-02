namespace Pagamenti.Processor.Domains;

public class WorkerPagamenti : BackgroundService
{
    private readonly ILogger<WorkerPagamenti> _logger;

    public WorkerPagamenti(ILogger<WorkerPagamenti> logger)
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
