namespace Waitingway.Server;

public class ClientManager
{
    private static readonly Object Lock = new();

    // client id -> connection id
    private static readonly Dictionary<string, string> ClientToConnection = new();

    // connection id -> client id
    private static readonly Dictionary<string, string> ConnectionToClient = new();

    private static readonly Dictionary<string, Client> Clients = new();

    public int ActiveCount
    {
        get => ClientToConnection.Count;
    }

    public int TotalCount
    {
        get => Clients.Count;
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

            ClientToConnection[client.Id] = connectionId;
            ConnectionToClient[connectionId] = client.Id;
            Clients[client.Id] = client;
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
}