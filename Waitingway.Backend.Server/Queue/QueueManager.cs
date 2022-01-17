using System.Diagnostics;
using StackExchange.Redis;
using Waitingway.Backend.Database;
using Waitingway.Backend.Database.Models;
using Waitingway.Backend.Database.Queue;
using Waitingway.Protocol.Serverbound;

namespace Waitingway.Backend.Server.Queue;

public class QueueManager
{
    private static readonly object Lock = new();

    private static readonly Dictionary<string, ClientQueue> ClientQueues = new();

    private readonly ILogger<QueueManager> _logger;
    private readonly WaitingwayContext _db;
    private readonly ConnectionMultiplexer _redis;

    public QueueManager(ILogger<QueueManager> logger, WaitingwayContext db, ConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = db;
        _redis = redis;
    }

    public void RemoveClient(string clientId)
    {
        lock (Lock)
        {
            ClientQueues.Remove(clientId);
        }
    }

    public ClientQueue? TryGetQueue(Client.Client client)
    {
        Debug.Assert(client != null);
        lock (Lock)
        {
            return ClientQueues.ContainsKey(client.Id) ? ClientQueues[client.Id] : null;
        }
    }

    public ClientQueue GetQueue(Client.Client client)
    {
        Debug.Assert(client != null);
        lock (Lock)
        {
            return ClientQueues[client.Id];
        }
    }

    public bool IsInQueue(Client.Client client)
    {
        Debug.Assert(client != null);
        lock (Lock)
        {
            return ClientQueues.ContainsKey(client.Id);
        }
    }

    internal void DoEnterQueue(Client.Client client, ClientQueue queue, bool allowOverwrite = false)
    {
        Debug.Assert(client != null);
        Debug.Assert(queue != null);
        lock (Lock)
        {
            if (ClientQueues.ContainsKey(client.Id) && !allowOverwrite)
            {
                throw new InvalidOperationException("Client is already in queue");
            }

            ClientQueues[client.Id] = queue;
        }
    }

    public void ResumeOrEnterLoginQueue(Client.Client client, LoginQueueEnter packet)
    {
        Debug.Assert(client != null);
        Debug.Assert(packet != null);

        ClientQueue? oldQueue;
        lock (Lock)
        {
            oldQueue = TryGetQueue(client);
            if (oldQueue != null)
            {
                if (oldQueue.DbSession.ClientSessionId == packet.SessionId
                    || oldQueue.DbSession.World == packet.WorldId
                    || oldQueue.DbSession.DataCenter == packet.DatacenterId)
                {
                    // resume existing session
                    _logger.LogInformation(
                        "[{}] resuming login queue with DbSessionId = {} (no parameters have changed)",
                        client.Id, oldQueue.DbSession.Id);
                    oldQueue.LastUpdateReceived = DateTime.UtcNow;
                    return;
                }
            }
        }

        if (oldQueue != null)
        {
            _logger.LogInformation("[{}] abandoning previous queue as parameters have changed", client.Id);

            var endData = new QueueSessionData
            {
                Session = oldQueue.DbSession,
                Type = QueueSessionData.DataType.End,
                // assume user cancellation
                EndReason = QueueSessionData.QueueEndReason.UserCancellation,
                Time = DateTime.UtcNow
            };

            _db.QueueSessions.Attach(endData.Session);
            _db.QueueSessionData.Add(endData);
            _db.SaveChanges();
        }

        EnterLoginQueue(client, packet);
    }

    public void EnterLoginQueue(Client.Client client, LoginQueueEnter packet)
    {
        Debug.Assert(client != null);
        Debug.Assert(packet != null);

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

        var queue = new ClientQueue
        {
            ClientId = Guid.Parse(client.Id),
            QueuePosition = 0,
            DbSession = session
        };
        DoEnterQueue(client, queue);

        var rc = _redis.GetDatabase();
        var json = queue.ToJson();
        rc.StringSet($"client:{client.Id}:queue", json);
        rc.Publish("queue:enter", json);

        _logger.LogInformation("[{}] entered login queue (DbSessionId = {})", client.Id, queue.DbSession.Id);
    }

    public void ExitQueue(Client.Client client,
        QueueSessionData.QueueEndReason reason = QueueSessionData.QueueEndReason.Unknown)
    {
        Debug.Assert(client != null);

        ClientQueue queue;
        lock (Lock)
        {
            queue = GetQueue(client);
            RemoveClient(client.Id);
        }

        var sessionData = new QueueSessionData
        {
            Session = queue.DbSession,
            Type = QueueSessionData.DataType.End,
            EndReason = reason,
            Time = DateTime.UtcNow
        };

        _db.QueueSessions.Attach(sessionData.Session);
        _db.QueueSessionData.Add(sessionData);
        _db.SaveChanges();

        var rc = _redis.GetDatabase();
        rc.KeyDelete($"client:{client.Id}:queue");
        rc.Publish("queue:exit", queue.ToJson());

        _logger.LogInformation("[{}] left queue (DbSessionId = {})", client.Id, queue.DbSession.Id);
    }

    public void RecordPositionUpdate(Client.Client client, uint queuePosition)
    {
        Debug.Assert(client != null);

        var queue = GetQueue(client);
        var sessionData = new QueueSessionData
        {
            Session = queue.DbSession,
            Type = QueueSessionData.DataType.Update,
            QueuePosition = queuePosition,
            Time = DateTime.UtcNow
        };

        _db.QueueSessions.Attach(sessionData.Session);
        _db.QueueSessionData.Add(sessionData);
        _db.SaveChanges();

        var rc = _redis.GetDatabase();
        var json = queue.ToJson();
        rc.StringSet($"client:{client.Id}:queue", json);
        rc.Publish("queue:update", json);
    }
}