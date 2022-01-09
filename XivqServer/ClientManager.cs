namespace XIVq.Server;

public class ClientManager
{
    private readonly Object _lock = new();

    // client id -> connection id
    private readonly Dictionary<string, string> _clientToConnection = new();

    // connection id -> client id
    private readonly Dictionary<string, string> _connectionToClient = new();

    private readonly Dictionary<string, Client> _clients = new();

    public int Count
    {
        get => _clientToConnection.Count;
    }

    public void Add(string connectionId, Client client)
    {
        lock (_lock)
        {
            string? existingConnection;
            if (_clientToConnection.TryGetValue(client.Id, out existingConnection))
            {
                // todo: disconnect existing client existingConnection
                _clientToConnection.Remove(client.Id);
            }

            _clientToConnection[client.Id] = connectionId;
            _connectionToClient[connectionId] = client.Id;
            _clients[client.Id] = client;
        }
    }

    public void Remove(string connectionId)
    {
        lock (_lock)
        {
            string? clientId;
            if (!_connectionToClient.TryGetValue(connectionId, out clientId))
            {
                return;
            }

            _clients.Remove(clientId);
            _clientToConnection.Remove(clientId);
            _connectionToClient.Remove(connectionId);
        }
    }

    public Client GetClient(string id)
    {
        lock (_lock)
        {
            return _clients[id];
        }
    }

    public Client GetClientForConnection(string connectionId)
    {
        lock (_lock)
        {
            return _clients[_connectionToClient[connectionId]];
        }
    }

    public string GetClientIdForConnection(string connectionId)
    {
        lock (_lock)
        {
            return _connectionToClient[connectionId];
        }
    }

    public string GetConnectionIdForClient(string clientId)
    {
        lock (_lock)
        {
            return _clientToConnection[clientId];
        }
    }
}