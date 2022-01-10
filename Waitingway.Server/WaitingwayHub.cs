using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Waitingway.Common.Protocol;
using Waitingway.Common.Protocol.Clientbound;
using Waitingway.Common.Protocol.Serverbound;

namespace Waitingway.Server;

public class WaitingwayHub : Hub
{
    private readonly ILogger<WaitingwayHub> _logger;
    private readonly ClientManager _manager;

    public WaitingwayHub(ILogger<WaitingwayHub> logger, ClientManager manager)
    {
        _logger = logger;
        _manager = manager;
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

        client.Queue = new ClientQueue {QueuePosition = 0};
    }

    public void QueueExit(QueueExit packet)
    {
        var client = _manager.GetClientForConnection(Context.ConnectionId);
        Debug.Assert(client.Queue != null, "client.Queue != null");
        client.Queue = null;
    }

    public void QueueStatusUpdate(QueueStatusUpdate packet)
    {
        var client = _manager.GetClientForConnection(Context.ConnectionId);
        Debug.Assert(client.Queue != null, "client.Queue != null");
        client.Queue.QueuePosition = packet.QueuePosition;
    }

    private async Task Send(IPacket packet)
    {
        await Clients.Caller.SendAsync(packet.GetType().Name, packet);
    }
}