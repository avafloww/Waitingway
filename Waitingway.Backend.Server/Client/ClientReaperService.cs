namespace Waitingway.Backend.Server.Client;

public class ClientReaperService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;

    public ClientReaperService(IServiceProvider services)
    {
        _serviceProvider = services;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();

        _timer = new Timer(
            ReapDisconnectedClients,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1)
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();

        return Task.CompletedTask;
    }

    private void ReapDisconnectedClients(object? state)
    {
        using var scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ClientManager>().ReapDisconnectedClients();
    }
}
