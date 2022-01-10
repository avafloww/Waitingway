using System;
using System.Threading.Tasks;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Waitingway.Common.Protocol;
using Waitingway.Common.Protocol.Clientbound;
using Waitingway.Common.Protocol.Serverbound;

namespace Waitingway.Dalamud.Network;

public class WaitingwayClient : IAsyncDisposable
{
    private readonly string _clientId;
    private readonly HubConnection _connection;

    public WaitingwayClient(string serverUrl, string clientId)
    {
        _clientId = clientId;
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        Task.Run(async () =>
        {
            PluginLog.Log($"Attempting to connect to remote server at {serverUrl}.");
            await _connection.StartAsync();
            PluginLog.Log("Connected to server.");
            await SendHello();

            // register the reconnect handler in case we get disconnected
            _connection.Reconnected += OnReconnect;
        });
    }

    private void RegisterHandlers()
    {
        // this kinda sucks, can we do better with attributes + reflection or something?
        _connection.On<ServerHello>(nameof(ServerHello), HandleServerHello);
        _connection.On<ServerGoodbye>(nameof(ServerGoodbye), HandleServerGoodbye);
        _connection.On<QueueStatusEstimate>(nameof(QueueStatusEstimate), HandleQueueStatusEstimate);
    }

    private void HandleServerHello(ServerHello packet)
    {
#if DEBUG
        PluginLog.LogDebug("Received ServerHello packet");
#endif
    }

    private void HandleServerGoodbye(ServerGoodbye packet)
    {
#if DEBUG
        PluginLog.LogDebug("Received ServerGoodbye packet");
#endif
    }

    private void HandleQueueStatusEstimate(QueueStatusEstimate packet)
    {
#if DEBUG
        PluginLog.LogDebug("Received QueueStatusEstimate packet");
#endif
    }

    private async Task OnReconnect(string _)
    {
        PluginLog.Log("Reconnected to server.");
        await SendHello();
    }

    private async Task SendHello()
    {
        await Send(new ClientHello
        {
            ProtocolVersion = 1,
            ClientId = _clientId,
            PluginVersion = "1.2.3.4"
        });
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _connection.DisposeAsync();
    }

    public async Task Send(IPacket packet)
    {
        try
        {
#if DEBUG
            PluginLog.LogDebug($"Sending {packet.GetType().Name} packet");
#endif
            await _connection.InvokeAsync(packet.GetType().Name, packet);
        }
        catch (Exception ex)
        {
            PluginLog.Log($"Error while sending packet to server: {ex}");
        }
    }
}