using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
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
    private readonly WaitingwayContext _db;
    private readonly ConnectionMultiplexer _redis;
    private readonly QueueManager _queueManager;

    public WaitingwayHub(ILogger<WaitingwayHub> logger, ClientManager clientManager, WaitingwayContext db,
        ConnectionMultiplexer redis, QueueManager queueManager)
    {
        _logger = logger;
        _clientManager = clientManager;
        _db = db;
        _redis = redis;
        _queueManager = queueManager;
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

        _logger.LogInformation("connection {} identified as client {}", Context.ConnectionId, packet.ClientId);
        _clientManager.Add(Context.ConnectionId,
            new Client.Client {Id = packet.ClientId, PluginVersion = packet.PluginVersion});
        await Send(new ServerHello());
    }

    public void ClientGoodbye(ClientGoodbye packet)
    {
        // todo: to be removed in a future protocol version, because we can't reliably send shit on game shutdown
    }

    public void ClientLanguageChange(ClientLanguageChange packet)
    {
        // todo: implement this
    }

    public void LoginQueueEnter(LoginQueueEnter packet)
    {
        var client = _clientManager.GetClientForConnection(Context.ConnectionId);
        _logger.LogDebug("[{}] LoginQueueEnter: {}", client.Id, packet);

        _queueManager.ResumeOrEnterLoginQueue(client, packet);
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

        // Send updated information to the client
        await Send(new QueueStatusEstimate
        {
            EstimatedTime = TimeSpan.Zero,
            LocalisedMessages = new[]
            {
                new GuiText
                {
                    Color = GuiText.GuiTextColor.Yellow,
                    Text = $"Your last logged queue position: {packet.QueuePosition}"
                },
                new GuiText
                {
                    Color = GuiText.GuiTextColor.Yellow,
                    Text = "Estimated wait time: unknown"
                },
                new GuiText
                {
                    Text =
                        "Estimated wait times are not yet available.\nThis feature is in progress - stay tuned!"
                }
            }
        });
    }

    private async Task Send(IPacket packet)
    {
        await Clients.Caller.SendAsync(packet.GetType().Name, packet);
    }
}