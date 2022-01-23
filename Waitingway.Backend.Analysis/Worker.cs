namespace Waitingway.Backend.Analysis;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _provider;

    public Worker(ILogger<Worker> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AnalysisService>().Analyse();
        
        // while (!stoppingToken.IsCancellationRequested)
        // {
            // _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            // await Task.Delay(1000, stoppingToken);
        // }
    }
}