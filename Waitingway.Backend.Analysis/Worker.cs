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

        while (!stoppingToken.IsCancellationRequested)
        {
            await scope.ServiceProvider.GetRequiredService<AnalysisService>().AnalyseAll();
            await Task.Delay(15000, stoppingToken);
        }
    }
}