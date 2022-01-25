using Microsoft.AspNetCore.SignalR;
using Waitingway.Backend.Database;
using Waitingway.Backend.Database.Models;
using Waitingway.Backend.Server.Client;
using Waitingway.Backend.Server.Queue;
using Waitingway.Protocol;
using Waitingway.Protocol.Clientbound;
using Waitingway.Protocol.Serverbound;

namespace Waitingway.Backend.Server;

public class WaitingwayHub : Hub
{
    private readonly ILogger<WaitingwayHub> _logger;
    private readonly ClientManager _clientManager;
    private readonly QueueManager _queueManager;
    private readonly WaitingwayContext _db;

    public WaitingwayHub(ILogger<WaitingwayHub> logger, ClientManager clientManager, QueueManager queueManager,
        WaitingwayContext db)
    {
        _logger = logger;
        _clientManager = clientManager;
        _queueManager = queueManager;
        _db = db;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("connection {} opened", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("connection {} closed", Context.ConnectionId);
        try
        {
            var client = _clientManager.GetClientForConnection(Context.ConnectionId);
            _clientManager.Disconnect(Context.ConnectionId, client);
        }
        catch (Exception)
        {
            // just remove now, if they were never properly connected
            _clientManager.RemoveConnection(Context.ConnectionId);
        }

        return Task.CompletedTask;
    }

    public async Task ClientHello(ClientHello packet)
    {
        if (packet.ProtocolVersion != WaitingwayProtocol.Version)
        {
            _logger.LogInformation("disconnecting connection {}: invalid protocol version received {}",
                Context.ConnectionId, packet.ProtocolVersion);
            await Send(new ServerGoodbye
            {
                Message = "Your version of Waitingway is outdated. Please update to the latest version."
            });

            Context.Abort();
            return;
        }

        if (!Guid.TryParse(packet.ClientId, out _))
        {
            _logger.LogInformation("disconnecting connection {}: invalid client id received {}",
                Context.ConnectionId, packet.ClientId);
            await Send(new ServerGoodbye
            {
                Message = "Your configuration is corrupt. Please delete your Waitingway configuration file."
            });

            Context.Abort();
            return;
        }

        _logger.LogInformation("connection {} identified as client {}", Context.ConnectionId, packet.ClientId);
        var client = new Client.Client {Id = packet.ClientId, PluginVersion = packet.PluginVersion};
        _clientManager.Add(Context.ConnectionId, client);

        await Send(new ServerHello());
        RefreshDiscordLinkStatus(client);
    }

    private void RefreshDiscordLinkStatus(Client.Client client)
    {
        try
        {
            var discordLink = _db.DiscordLinkInfo.Find(Guid.Parse(client.Id));
            client.DiscordLinked = discordLink != null;
            _logger.LogDebug("[{}] RefreshDiscordLinkStatus: linked = {}", client.Id, client.DiscordLinked);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{}] RefreshDiscordLinkStatus: error", client.Id);
        }
    }

    public void ClientGoodbye(ClientGoodbye packet)
    {
        // todo: to be removed in a future protocol version, because we can't reliably send shit on game shutdown
    }

    public void ClientLanguageChange(ClientLanguageChange packet)
    {
        // todo: implement this
    }

    public async void LoginQueueEnter(LoginQueueEnter packet)
    {
        var client = _clientManager.GetClientForConnection(Context.ConnectionId);
        _logger.LogDebug("[{}] LoginQueueEnter: {}", client.Id, packet);

        _queueManager.ResumeOrEnterLoginQueue(client, packet);

        RefreshDiscordLinkStatus(client);
        await UpdateClient(client);
    }

    public void QueueExit(QueueExit packet)
    {
        var client = _clientManager.GetClientForConnection(Context.ConnectionId);
        _logger.LogDebug("[{}] QueueExit: {}", client.Id, packet);

        _queueManager.ExitQueue(client, (QueueSessionData.QueueEndReason) packet.Reason);
    }

    public async Task QueueStatusUpdate(QueueStatusUpdate packet)
    {
        var client = _clientManager.GetClientForConnection(Context.ConnectionId);
        _logger.LogDebug("[{}] QueueStatusUpdate: {}", client.Id, packet);

        _queueManager.RecordPositionUpdate(client, packet.QueuePosition);

        await UpdateClient(client);
    }

    public async Task UpdateClient(Client.Client client)
    {
        if (!_queueManager.IsInQueue(client))
        {
            return;
        }

        var queue = _queueManager.GetQueue(client);

        var messages = new List<GuiText>
        {
            new()
            {
                Color = GuiText.GuiTextColor.Yellow,
                Text = $"Your last logged queue position: {_queueManager.GetQueue(client).QueuePosition}"
            }
        };

        if (queue.TimeEstimate == null)
        {
            messages.Add(new()
            {
                Color = GuiText.GuiTextColor.Yellow,
                Text = "Estimated wait time: calculating..."
            });
        }
        else
        {
            var roundedEstimate = queue.TimeEstimate?.Seconds >= 30
                ? new TimeSpan(queue.TimeEstimate?.Hours ?? 0, queue.TimeEstimate?.Minutes + 1 ?? 0, 0)
                : new TimeSpan(queue.TimeEstimate?.Hours ?? 0, queue.TimeEstimate?.Minutes ?? 0, 0);

            var formatted = roundedEstimate.TotalSeconds < 1
                ? "unknown"
                : $"about {roundedEstimate.TotalMinutes} minute{(roundedEstimate.TotalMinutes > 1 ? "s" : "")}";

            messages.Add(new()
            {
                Color = GuiText.GuiTextColor.Yellow,
                Text = $"Estimated wait time: {formatted}"
            });
        }

        if (!client.DiscordLinked)
        {
            messages.Add(new()
            {
                Color = GuiText.GuiTextColor.Green,
                Text = "Do you want queue notifications via Discord?\nClick the Settings icon to get started!"
            });
        }

        if (!client.IsLatestVersion)
        {
            messages.Add(new()
            {
                Color = GuiText.GuiTextColor.Yellow,
                Text = "There is a new Waitingway version available.\nPlease update as soon as possible."
            });
        }

        // Send updated information to the client
        await Send(new QueueStatusEstimate
        {
            EstimatedTime = TimeSpan.Zero,
            LocalisedMessages = messages.ToArray()
        });
    }

    private async Task Send(IPacket packet)
    {
        await Clients.Caller.SendAsync(packet.GetType().Name, packet);
    }
}