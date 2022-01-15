namespace Waitingway.Server.Client;

public class ClientManager
{
    private static readonly Object Lock = new();

    // client id -> connection id
    private static readonly Dictionary<string, string> ClientToConnection = new();

    // connection id -> client id
    private static readonly Dictionary<string, string> ConnectionToClient = new();

    private static readonly Dictionary<string, Client> Clients = new();
    private static readonly Dictionary<string, DisconnectedClient> DisconnectedClients = new();

    private readonly ILogger<ClientManager> _logger;
    private readonly WaitingwayContext _db;

    public int ActiveCount
    {
        get => ClientToConnection.Count;
    }

    public int TotalCount
    {
        get => Clients.Count;
    }

    public ClientManager(ILogger<ClientManager> logger, WaitingwayContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Restore()
    {
        _logger.LogInformation("queued session restore");
        await foreach (var raqs in _db.RecentlyActiveQueueSessions.AsAsyncEnumerable())
        {
            var client = raqs.ToClient();
            _logger.LogInformation("restoring queue session for client: {}", client.Id);
            lock (Lock)
            {
                if (DisconnectedClients.ContainsKey(client.Id))
                {
                    _logger.LogWarning("duplicate queue session for client {}, using the newer one");
                    var old = DisconnectedClients[client.Id];
                    if (old.Client.Queue?.LastUpdateReceived < client.Queue?.LastUpdateReceived)
                    {
                        DisconnectedClients.Remove(client.Id);
                    }
                    else
                    {
                        continue;
                    }
                }

                DisconnectedClients.Add(client.Id, new DisconnectedClient {Client = client});
            }
        }
    }

    internal void ReapDisconnectedClients()
    {
        lock (Lock)
        {
            foreach (var dc in DisconnectedClients.Values.Where(dc =>
                         DateTime.UtcNow.Subtract(dc.DisconnectedAt).TotalMinutes >= 15))
            {
                _logger.LogInformation("[{}] reaping disconnected client", dc.Client.Id);
                DisconnectedClients.Remove(dc.Client.Id);
                Clients.Remove(dc.Client.Id);
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
        if (!client.InQueue)
        {
            // client was not in queue, remove them immediately
            Remove(connectionId);
        }
        else
        {
            // non-graceful disconnect, add to DisconnectedClients first
            lock (Lock)
            {
                _logger.LogInformation("[{}] client disconnected while in queue, waiting to remove", client.Id);
                DisconnectedClients.Add(client.Id, new DisconnectedClient {Client = client});
                Remove(connectionId);
            }
        }
    }

    public void Remove(string connectionId)
    {
        lock (Lock)
        {
            string? clientId;
            if (!ConnectionToClient.TryGetValue(connectionId, out clientId))
            {
                return;
            }

            Clients.Remove(clientId);
            ClientToConnection.Remove(clientId);
            ConnectionToClient.Remove(connectionId);
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