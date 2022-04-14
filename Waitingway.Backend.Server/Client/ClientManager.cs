using StackExchange.Redis;
using Waitingway.Backend.Database;
using Waitingway.Backend.Database.Queue;
using Waitingway.Backend.Server.Queue;

namespace Waitingway.Backend.Server.Client;

public class ClientManager
{
    private static readonly object Lock = new();

    // client id -> connection id
    private static readonly Dictionary<string, string> ClientToConnection = new();

    // connection id -> client id
    private static readonly Dictionary<string, string> ConnectionToClient = new();

    private static readonly Dictionary<string, Client> Clients = new();
    private static readonly Dictionary<string, DisconnectedClient> DisconnectedClients = new();

    private readonly ILogger<ClientManager> _logger;
    private readonly WaitingwayContext _db;
    private readonly ConnectionMultiplexer _redis;
    private readonly QueueManager _queueManager;

    public int ActiveCount
    {
        get => ClientToConnection.Count;
    }

    public int TotalCount
    {
        get => Clients.Count;
    }

    public ClientManager(ILogger<ClientManager> logger, WaitingwayContext db, ConnectionMultiplexer redis,
        QueueManager queueManager)
    {
        _logger = logger;
        _db = db;
        _redis = redis;
        _queueManager = queueManager;
    }

    public void Restore()
    {
        _logger.LogInformation("Restoring queue sessions from Redis...");
        var rc = _redis.GetDatabase();
        var count = 0;

        foreach (var clientId in rc.SetMembers("clients:queued"))
        {
            try
            {
                var jsonData = rc.StringGet($"client:${clientId}:queue");

                if (jsonData == RedisValue.Null || jsonData == RedisValue.EmptyString)
                {
                    _logger.LogWarning("Queue data key for client {} was missing or empty, skipping", clientId);

                    continue;
                }

                var queue = ClientQueue.FromJson(jsonData);
                var client = new Client
                {
                    Id = queue!.DbSession.ClientId.ToString(),
                    PluginVersion = queue.DbSession.PluginVersion,
                    GameVersion = queue.DbSession.GameVersion
                };

                _logger.LogInformation("restoring queue session for client: {}", client.Id);

                lock (Lock)
                {
                    if (DisconnectedClients.ContainsKey(client.Id))
                    {
                        _logger.LogWarning("duplicate queue session for client {}, using the newer one", client.Id);
                        var old = DisconnectedClients[client.Id];

                        if (_queueManager.TryGetQueue(old.Client)?.LastUpdateReceived < queue.LastUpdateReceived)
                        {
                            DisconnectedClients.Remove(client.Id);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    DisconnectedClients.Add(client.Id, new DisconnectedClient {Client = client});
                    _queueManager.DoEnterQueue(client, queue, true);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while restoring queue session for client {}", clientId);
            }
        }

        _logger.LogInformation("{} queue sessions restored from Redis", count);
    }

    internal void ReapDisconnectedClients()
    {
        lock (Lock)
        {
            foreach (var dc in DisconnectedClients.Values.Where(dc =>
                         DateTime.UtcNow.Subtract(dc.DisconnectedAt).TotalMinutes >= 15))
            {
                _logger.LogInformation("[{}] reaping disconnected client", dc.Client.Id);
                RemoveClient(dc.Client.Id);
            }
        }
    }

    public void Add(string connectionId, Client client)
    {
        lock (Lock)
        {
            string? existingConnection;

            if (ClientToConnection.TryGetValue(client.Id, out existingConnection))
            {
                // todo: disconnect existing client existingConnection
                ClientToConnection.Remove(client.Id);
            }

            DisconnectedClient? dc;

            if (DisconnectedClients.TryGetValue(client.Id, out dc))
            {
                // this client is reconnecting, use their previous instance (but cleanup if needed)
                DisconnectedClients.Remove(dc.Client.Id);
                _logger.LogInformation("[{}] client has reconnected", dc.Client.Id);

                Clients[client.Id] = dc.Client;
            }
            else
            {
                Clients[client.Id] = client;
            }

            ClientToConnection[client.Id] = connectionId;
            ConnectionToClient[connectionId] = client.Id;
        }
    }

    public void Disconnect(string connectionId, Client client)
    {
        if (!_queueManager.IsInQueue(client))
        {
            // client was not in queue, remove them immediately
            RemoveConnection(connectionId);
        }
        else
        {
            // non-graceful disconnect, add to DisconnectedClients first
            lock (Lock)
            {
                _logger.LogInformation("[{}] client disconnected while in queue, waiting to remove", client.Id);
                DisconnectedClients.Add(client.Id, new DisconnectedClient {Client = client});
                RemoveConnection(connectionId);
            }
        }
    }

    public void RemoveConnection(string connectionId)
    {
        lock (Lock)
        {
            string? clientId;

            if (!ConnectionToClient.TryGetValue(connectionId, out clientId))
            {
                return;
            }

            ConnectionToClient.Remove(connectionId);
            RemoveClient(clientId);
        }
    }

    public void RemoveClient(string clientId)
    {
        lock (Lock)
        {
            string? connectionId;

            if (ClientToConnection.TryGetValue(clientId, out connectionId))
            {
                ConnectionToClient.Remove(connectionId);
            }

            _queueManager.RemoveClient(clientId);

            Clients.Remove(clientId);
            ClientToConnection.Remove(clientId);
            DisconnectedClients.Remove(clientId);
        }
    }

    public Client GetClient(string id)
    {
        lock (Lock)
        {
            return Clients[id];
        }
    }

    public Client GetClientForConnection(string connectionId)
    {
        lock (Lock)
        {
            return Clients[ConnectionToClient[connectionId]];
        }
    }

    public string GetClientIdForConnection(string connectionId)
    {
        lock (Lock)
        {
            return ConnectionToClient[connectionId];
        }
    }

    public string GetConnectionIdForClient(string clientId)
    {
        lock (Lock)
        {
            return ClientToConnection[clientId];
        }
    }

    private class DisconnectedClient
    {
        public Client Client { get; init; }
        public DateTime DisconnectedAt { get; } = DateTime.UtcNow;
    }
}
