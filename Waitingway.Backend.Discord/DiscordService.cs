using CacheTower;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Waitingway.Backend.Database.Models;
using Waitingway.Backend.Database.Queue;
using Waitingway.Backend.Discord.Queue;

namespace Waitingway.Backend.Discord;

public class DiscordService
{
    private const string MessageSuffix = "You will receive a DM from me when your queue has ended.";
    private readonly ILogger<DiscordService> _logger;
    private readonly DiscordSocketClient _client;
    private readonly DiscordConfig _config;
    private readonly ICacheStack<DiscordQueueMessage> _messageCache;

    public DiscordService(ILogger<DiscordService> logger, DiscordSocketClient client, DiscordConfig config,
        ICacheStack<DiscordQueueMessage> messageCache)
    {
        _logger = logger;
        _client = client;
        _config = config;
        _messageCache = messageCache;
    }

    internal async Task JoinUserToGuild(ulong userId, string accessToken)
    {
        await _client.Rest.AddGuildUserAsync(_config.GuildId, userId, accessToken);
    }

    private async Task SendLinkedLogMessage(DiscordLinkInfo linkInfo, IUser user)
    {
        var channel = await _client.GetChannelAsync(_config.LogChannel);
        if (channel is IMessageChannel msgChannel)
        {
            var embed = new EmbedBuilder
            {
                Title = "Client linked",
                Description = $"User: {user.Mention}\nClient ID: {linkInfo.ClientId}",
                Color = Color.Blue,
                Timestamp = DateTimeOffset.UtcNow
            };

            await msgChannel.SendMessageAsync(embed: embed.Build());
        }
    }

    internal async Task SendLinkedMessage(DiscordLinkInfo linkInfo)
    {
        var embed = new EmbedBuilder
        {
            Title = "Client linked",
            Description =
                "Your FFXIV client has been linked to your Discord account.\nYou will receive queue notification DMs from me for significant queues (queues starting at 100 or more) when you use Waitingway.",
            Color = Color.Green,
            Timestamp = DateTimeOffset.UtcNow
        };

        var user = await _client.GetUserAsync(linkInfo.DiscordUserId);
        await user.SendMessageAsync(embed: embed.Build());

        await SendLinkedLogMessage(linkInfo, user);
    }

    internal async Task SendQueueUpdateMessage(ulong userId, ClientQueue queue)
    {
        var embed = new EmbedBuilder
        {
            Title = $":clock3: {queue.DbSession.SessionType} Queue Status",
            Description =
                $"**Position:** {queue.QueuePosition}\n\n{MessageSuffix}",
            Color = Color.Blue,
            Timestamp = queue.LastUpdateReceived,
            Footer = new EmbedFooterBuilder
            {
                Text = "Last updated"
            }
        };

        await UpdateOrSendMessage(userId, embed.Build());
    }

    internal async Task SendQueueExitMessage(ulong userId, ClientQueue queue)
    {
        var color = queue.EndReason switch
        {
            QueueSessionData.QueueEndReason.Success => Color.Green,
            QueueSessionData.QueueEndReason.Error => Color.Red,
            QueueSessionData.QueueEndReason.UserCancellation => Color.Orange,
            _ => Color.LightGrey
        };

        var info = queue.EndReason switch
        {
            QueueSessionData.QueueEndReason.Success => "Have fun!",
            QueueSessionData.QueueEndReason.Error =>
                $"**Position:** {queue.QueuePosition}\nLooks like your queue ended in error... sorry!",
            QueueSessionData.QueueEndReason.UserCancellation =>
                $"**Position:** {queue.QueuePosition}\nYou cancelled your queue.",
            _ => "I have no idea what happened here..."
        };

        var icon = queue.EndReason == QueueSessionData.QueueEndReason.Success ? ":tada:" : ":x:";

        var embed = new EmbedBuilder
        {
            Title = $"{icon} {queue.DbSession.SessionType} Queue Ended",
            Description = info,
            Color = color,
            Timestamp = queue.LastUpdateReceived
        };

        try
        {
            var user = await UpdateOrSendMessage(userId, embed.Build());

            // send a second message to trigger notification
            if (user != null)
            {
                await user.SendMessageAsync("Your queue has ended! Thanks for using Waitingway!");
            }
        }
        finally
        {
            await InvalidateMessageCache(userId);
        }
    }

    private async Task<IUser?> UpdateOrSendMessage(ulong userId, Embed embed)
    {
        var user = await _client.GetUserAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Could not find Discord user with ID {}", userId);
            return null;
        }

        var previous = await _messageCache.GetAsync<DiscordQueueMessage>($"discord:{userId}:queueMessage");

        if (previous != null && previous.Value?.PreviousMessageId != 0)
        {
            var channel = await user.CreateDMChannelAsync();
            if (channel != null && await channel.GetMessageAsync(previous.Value!) is IUserMessage userMessage)
            {
                await userMessage.ModifyAsync(props => props.Embed = embed);
                return user;
            }
        }

        var message = await user.SendMessageAsync(embed: embed);
        if (message == null)
        {
            _logger.LogWarning("Failed to send message to user {}", user.Id);
            return user;
        }

        await _messageCache.SetAsync(
            $"discord:{userId}:queueMessage",
            new CacheEntry<DiscordQueueMessage>(new DiscordQueueMessage(message.Id), TimeSpan.FromMinutes(15))
        );

        return user;
    }

    private async Task InvalidateMessageCache(ulong userId)
    {
        await _messageCache.EvictAsync($"discord:{userId}:queueMessage");
    }
}