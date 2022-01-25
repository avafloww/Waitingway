using StackExchange.Redis;
using Waitingway.Backend.Database.Queue;
using Waitingway.Backend.Server.Client;

namespace Waitingway.Backend.Server.Queue;

public class QueueListenerService : IHostedService
{
    private readonly ILogger<QueueListenerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConnectionMultiplexer _redis;

    private ISubscriber? _subscriber;

    public QueueListenerService(ILogger<QueueListenerService> logger, IServiceProvider serviceProvider,
        ConnectionMultiplexer redis)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _redis = redis;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _subscriber = _redis.GetSubscriber();
        await _subscriber.SubscribeAsync("queue:estimate", OnMessage);
        _logger.LogInformation("Started Queue Listener Service");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var unsub = _subscriber?.UnsubscribeAllAsync();
        if (unsub != null)
        {
            await unsub;
        }

        _subscriber = null;
        _logger.LogInformation("Stopped Queue Listener Service");
    }

    private void OnMessage(RedisChannel eventType, RedisValue value)
    {
        var queue = QueueEstimate.FromJson(value);
        if (queue == null)
        {
            _logger.LogError("Deserialized QueueEstimate is null? Value is: {}", value);
            return;
        }

        Task.Run(() => UpdateClientEstimate(queue));
    }

    private async Task UpdateClientEstimate(QueueEstimate estimate)
    {
        _logger.LogDebug("Updating queue estimate for client {}", estimate.ClientId);
        await using var scope = _serviceProvider.CreateAsyncScope();
        var clientManager = scope.ServiceProvider.GetRequiredService<ClientManager>();
        var client = clientManager.GetClient(estimate.ClientId.ToString());

        var queueManager = scope.ServiceProvider.GetRequiredService<QueueManager>();
        var queue = queueManager.GetQueue(client);

        queue.TimeEstimate = estimate.HasEstimate ? estimate.Estimate : null;

        var hub = scope.ServiceProvider.GetRequiredService<WaitingwayHub>();
        await hub.UpdateClient(client);
    }
}