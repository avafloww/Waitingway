using CacheTower;
using StackExchange.Redis;
using Waitingway.Backend.Database;
using Waitingway.Backend.Database.Models;
using Waitingway.Backend.Database.Queue;

namespace Waitingway.Backend.Discord.Queue;

public class QueueListenerService : IHostedService
{
    private readonly ILogger<QueueListenerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordService _discordService;
    private readonly ConnectionMultiplexer _redis;
    private readonly ICacheStack<DiscordLinkInfo> _discordLinkCache;

    private ISubscriber? _subscriber;

    public QueueListenerService(ILogger<QueueListenerService> logger, IServiceProvider serviceProvider,
        DiscordService discordService, ConnectionMultiplexer redis, ICacheStack<DiscordLinkInfo> discordLinkCache)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _discordService = discordService;
        _redis = redis;
        _discordLinkCache = discordLinkCache;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _subscriber = _redis.GetSubscriber();
        await _subscriber.SubscribeAsync("queue:enter", OnMessage);
        await _subscriber.SubscribeAsync("queue:exit", OnMessage);
        await _subscriber.SubscribeAsync("queue:update", OnMessage);
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
        if (eventType != QueueEventType.Enter && eventType != QueueEventType.Exit && eventType != QueueEventType.Update)
        {
            _logger.LogError("Received invalid event type {}", eventType);
            return;
        }

        var queue = ClientQueue.FromJson(value);
        if (queue == null)
        {
            _logger.LogError("Deserialized ClientQueue is null? Value is: {}", value);
            return;
        }

        Task.Run(() => OnEvent(eventType, queue));
    }

    private async Task OnEvent(string eventType, ClientQueue queue)
    {
        var user = await GetUserForClient(queue.ClientId);
        if (user == null)
        {
            return;
        }

        switch (eventType)
        {
            case QueueEventType.Enter:
                await _discordService.SendQueueEntryMessage(user.DiscordUserId, queue);
                break;
            case QueueEventType.Update:
                await _discordService.SendQueueUpdateMessage(user.DiscordUserId, queue);
                break;
            case QueueEventType.Exit:
                await _discordService.SendQueueExitMessage(user.DiscordUserId, queue);
                break;
        }
    }

    private async Task<DiscordLinkInfo?> GetUserForClient(Guid clientId)
    {
        return await _discordLinkCache.GetOrSetAsync<DiscordLinkInfo?>($"client:{clientId}:discord", async _ =>
        {
            using (var scope = _serviceProvider.CreateAsyncScope())
            {
                var link = await scope.ServiceProvider.GetRequiredService<WaitingwayContext>().DiscordLinkInfo
                    .FindAsync(clientId);
                if (link == null)
                {
                    _logger.LogTrace("No Discord link info found for client {}", clientId);
                    return null!;
                }

                return link;
            }
        }, new CacheSettings(TimeSpan.FromHours(2)));
    }

    // private async Task<DiscordQueueMessage> GetPreviousQueueMessage(ClientQueue queue)
    // {
    //     try
    //     {
    //         var rc = _redis.GetDatabase();
    //         if (rc == null)
    //         {
    //             _logger.LogError("Failed to obtain a Redis client!");
    //             return 0;
    //         }
    //     
    //         var value = await rc.StringGetAsync($"client:{clientId}:queue:discordMessageId");
    //         return value.IsNullOrEmpty ? 0 : ulong.Parse(value);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error while getting previous message ID for client {}", clientId);
    //         return 0;
    //     }
    // }
}