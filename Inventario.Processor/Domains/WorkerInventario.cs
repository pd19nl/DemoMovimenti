namespace Inventario.Processor.Domains;

public class WorkerInventario : BackgroundService
{
    private readonly ILogger<WorkerInventario> _logger;

    public WorkerInventario(ILogger<WorkerInventario> logger)
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
