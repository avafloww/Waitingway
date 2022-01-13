using Microsoft.AspNetCore.SignalR;
using Waitingway.Protocol;
using Waitingway.Protocol.Clientbound;
using Waitingway.Protocol.Serverbound;
using Waitingway.Server.Client;
using Waitingway.Server.Models;

namespace Waitingway.Server;

public class WaitingwayHub : Hub
{
    private readonly ILogger<WaitingwayHub> _logger;
    private readonly ClientManager _manager;
    private readonly WaitingwayContext _db;

    public WaitingwayHub(ILogger<WaitingwayHub> logger, ClientManager manager, WaitingwayContext db)
    {
        _logger = logger;
        _manager = manager;
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
            var client = _manager.GetClientForConnection(Context.ConnectionId);
            _manager.Disconnect(Context.ConnectionId, client);
        }
        catch (Exception)
        {
            // just remove now, if they were never properly connected
            _manager.Remove(Context.ConnectionId);
        }

        return Task.CompletedTask;
    }

    public async Task ClientHello(ClientHello packet)
    {
        _logger.LogInformation("connection {} identified as client {}", Context.ConnectionId, packet.ClientId);
        _manager.Add(Context.ConnectionId, new Client.Client {Id = packet.ClientId, PluginVersion = packet.PluginVersion, Active = true});
        await Send(new ServerHello());
    }

    public void ClientGoodbye(ClientGoodbye packet)
    {
        try
        {
            var client = _manager.GetClientForConnection(Context.ConnectionId);

            // mark the client as having cleanly disconnected
            client.Active = false;
        }
        catch (Exception)
        {
            // they were never properly connected to start with, so just abort the connection
        }

        Context.Abort();
    }

    public void LoginQueueEnter(LoginQueueEnter packet)
    {
        var client = _manager.GetClientForConnection(Context.ConnectionId);
        if (client.InQueue)
        {
            // todo: special behavior for resume
        }

        _logger.LogDebug("[{}] LoginQueueEnter: {}", client.Id, packet);

        var session = new QueueSession
        {
            ClientId = Guid.Parse(client.Id),
            ClientSessionId = packet.SessionId,
            DataCenter = packet.DatacenterId,
            World = packet.WorldId,
            SessionType = QueueSession.Type.Login,
            PluginVersion = client.PluginVersion
        };

        var sessionData = new QueueSessionData
        {
            Session = session,
            Type = QueueSessionData.DataType.Start,
            Time = DateTime.UtcNow
        };
        
        _db.QueueSessionData.Add(sessionData);
        _db.SaveChanges();

        client.Queue = new ClientQueue
        {
            QueuePosition = 0,
            DbSession = session
        };
        
        _logger.LogInformation("[{}] entered login queue (DbSessionId = {})", client.Id, client.Queue.DbSession.Id);
    }

    public void QueueExit(QueueExit packet)
    {
        var client = _manager.GetClientForConnection(Context.ConnectionId);
        if (client.Queue == null)
        {
            throw new HubException("Client is not actively in a queue");
        }
        
        _logger.LogDebug("[{}] QueueExit: {}", client.Id, packet);

        var sessionData = new QueueSessionData
        {
            Session = client.Queue.DbSession,
            Type = QueueSessionData.DataType.End,
            EndReason = packet.Reason,
            Time = DateTime.UtcNow
        };

        _db.QueueSessions.Attach(sessionData.Session);
        _db.QueueSessionData.Add(sessionData);
        _db.SaveChanges();
        
        _logger.LogInformation("[{}] left queue (DbSessionId = {})", client.Id, client.Queue.DbSession.Id);

        client.Queue = null;
    }

    public async Task QueueStatusUpdate(QueueStatusUpdate packet)
    {
        var client = _manager.GetClientForConnection(Context.ConnectionId);
        if (client.Queue == null)
        {
            throw new HubException("Client is not actively in a queue");
        }
        
        _logger.LogDebug("[{}] QueueStatusUpdate: {}", client.Id, packet);

        var sessionData = new QueueSessionData
        {
            Session = client.Queue.DbSession,
            Type = QueueSessionData.DataType.Update,
            QueuePosition = packet.QueuePosition,
            Time = DateTime.UtcNow
        };
        
        _db.QueueSessions.Attach(sessionData.Session);
        _db.QueueSessionData.Add(sessionData);
        await _db.SaveChangesAsync();
        
        client.Queue.QueuePosition = packet.QueuePosition;

        await Send(new QueueStatusEstimate
        {
            EstimatedTime = TimeSpan.Zero,
            LocalisedMessages = new[] {
                new GuiText
                {
                    Color = GuiText.GuiTextColor.Yellow,
                    Text = $"Your last logged queue position: {packet.QueuePosition}"
                },
                new GuiText {
                    Color = GuiText.GuiTextColor.Yellow,
                    Text = "Estimated wait time: unknown"
                },
                new GuiText
                {
                    Text = "Estimated wait times are not yet available.\nWe're working on adding this feature, stay tuned!"
                }
            }
        });
    }

    private async Task Send(IPacket packet)
    {
        await Clients.Caller.SendAsync(packet.GetType().Name, packet);
    }
}