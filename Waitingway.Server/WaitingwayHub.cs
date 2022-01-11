using Microsoft.AspNetCore.SignalR;
using Waitingway.Common.Protocol;
using Waitingway.Common.Protocol.Clientbound;
using Waitingway.Common.Protocol.Serverbound;
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
        // todo: persist client for X time until removed with a sweep
        _manager.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task ClientHello(ClientHello packet)
    {
        _logger.LogInformation("connection {} identified as client {}", Context.ConnectionId, packet.ClientId);
        _manager.Add(Context.ConnectionId, new Client {Id = packet.ClientId});
        await Send(new ServerHello());
    }

    public void ClientGoodbye(ClientGoodbye packet)
    {
        _manager.Remove(Context.ConnectionId);
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
            SessionType = QueueSession.Type.Login
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

    public void QueueStatusUpdate(QueueStatusUpdate packet)
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
        _db.SaveChanges();
        
        client.Queue.QueuePosition = packet.QueuePosition;
    }

    private async Task Send(IPacket packet)
    {
        await Clients.Caller.SendAsync(packet.GetType().Name, packet);
    }
}