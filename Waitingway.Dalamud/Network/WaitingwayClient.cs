using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Waitingway.Protocol;
using Waitingway.Protocol.Clientbound;
using Waitingway.Protocol.Serverbound;

namespace Waitingway.Dalamud.Network;

public class WaitingwayClient : IDisposable
{
    private readonly Plugin _plugin;
    private readonly string _clientId;
    private readonly HubConnection _connection;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private string _language;

    public string RemoteUrl { get; init; }

    public bool IsDisposed { get; private set; }

    public bool Connected => _connection.State == HubConnectionState.Connected;
    private ServerGoodbye? _goodbye;

    public ulong CharacterId { get; private set; }
    public ushort DataCenterId { get; private set; }
    public ushort WorldId { get; private set; }
    public DateTime? QueueEntryTime { get; private set; }
    public QueueType QueueType { get; private set; } = QueueType.None;
    public int QueuePosition { get; private set; } = -1;
    public TimeSpan? QueueEstimate { get; private set; }

    public bool InQueue => QueueEntryTime != null;

    public WaitingwayClient(Plugin plugin, string serverUrl, string clientId, string language)
    {
        _plugin = plugin;
        _clientId = clientId;

        PluginLog.Log($"Remote server: {serverUrl}");
        RemoteUrl = serverUrl;
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect(new InfiniteRetryPolicy())
            .Build();
        _language = language;

        RegisterHandlers();

        plugin.PluginInterface.LanguageChanged += LanguageChanged;

        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(() => EstablishInitialConnection(_cancellationTokenSource.Token));

        // register the reconnect handler in case we get disconnected
        _connection.Reconnected += OnReconnect;
        _connection.Closed += OnDisconnect;
    }

    private async Task EstablishInitialConnection(CancellationToken cancellationToken)
    {
        PluginLog.Log("Attempting to connect to remote server.");

        while (true)
        {
            try
            {
                await _connection.StartAsync(cancellationToken);
                PluginLog.Log("Connected to server.");
                await SendHello();
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                await Task.Delay(5000, cancellationToken);
            }
        }
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
        CheckDisposed();
#if DEBUG
        PluginLog.LogDebug("Received ServerHello packet");
#endif
    }

    private void HandleServerGoodbye(ServerGoodbye packet)
    {
        CheckDisposed();
#if DEBUG
        PluginLog.LogDebug("Received ServerGoodbye packet");
#endif
        _goodbye = packet;
        if (packet.Message != null)
        {
            _plugin.Ui.SetStatusText(packet.Message);
        }
    }

    private void HandleQueueStatusEstimate(QueueStatusEstimate packet)
    {
        CheckDisposed();
#if DEBUG
        PluginLog.LogDebug("Received QueueStatusEstimate packet");
#endif
        _plugin.Ui.QueueText = packet.LocalisedMessages;
        _plugin.Hooks.NativeUiQueueDelta = packet.NativeUiQueueDelta;
        QueueEstimate = packet.EstimatedTime;
    }

    private async Task OnReconnect(string? _)
    {
        CheckDisposed();
        PluginLog.Log("Reconnected to server.");
        await SendHello();
    }

    private Task OnDisconnect(Exception? ex)
    {
        CheckDisposed();
        PluginLog.Log($"Disconnected from server. {ex}");

        if (_goodbye == null)
        {
            _plugin.Ui.SetStatusText(
                "Disconnected from server unexpectedly.\nCheck Dalamud logs for more information.");
        }
        else
        {
            Dispose();
        }

        // reset this now that we've actually disconnected
        _goodbye = null;

        return Task.CompletedTask;
    }

    private async Task SendHello()
    {
        CheckDisposed();
        await SendAsync(new ClientHello
        {
            ProtocolVersion = WaitingwayProtocol.Version,
            ClientId = _clientId,
            PluginVersion = _plugin.Version,
            Language = _language
        });
    }

    private void LanguageChanged(string newLanguage)
    {
        CheckDisposed();
        _language = newLanguage;
        Send(new ClientLanguageChange {Language = newLanguage});
    }

    internal void TryExitQueue(QueueExit.QueueExitReason reason)
    {
        if (InQueue)
        {
            ExitQueue(reason);
        }
    }

    internal void ExitQueue(QueueExit.QueueExitReason reason)
    {
        Task.Run(() => DoExitQueue(reason));
    }

    private async Task DoExitQueue(QueueExit.QueueExitReason reason)
    {
        CheckDisposed();
        Debug.Assert(QueueEntryTime != null);
        QueueEntryTime = null;
        QueueType = QueueType.None;
        await SendAsync(new QueueExit {Reason = reason});
    }

    internal void EnterLoginQueue()
    {
        Task.Run(DoEnterLoginQueue);
    }

    private async Task DoEnterLoginQueue()
    {
        CheckDisposed();
        Debug.Assert(QueueEntryTime == null);
        QueueType = QueueType.Login;
        QueueEntryTime = DateTime.Now;
        await SendAsync(new LoginQueueEnter(
            _plugin.Config.ClientId,
            CharacterId,
            _plugin.Config.ClientSalt,
            DataCenterId,
            WorldId
        ));
    }

    // todo: this function is kinda jank but chara select logic needs it, fix this eventually
    internal void ResetLoginQueue(ulong characterId, ushort dataCenterId, ushort worldId)
    {
        CheckDisposed();

        ResetQueue();
        CharacterId = characterId;
        DataCenterId = dataCenterId;
        WorldId = worldId;
    }

    private void ResetQueue()
    {
        QueueEntryTime = null;
        QueueType = QueueType.None;
        QueuePosition = -1;
        QueueEstimate = null;
    }

    internal void UpdateQueuePosition(int position)
    {
        Task.Run(() => DoUpdateQueuePosition(position));
    }

    private async Task DoUpdateQueuePosition(int position)
    {
        QueuePosition = position;
        if (position >= 0)
        {
            await SendAsync(new QueueStatusUpdate {QueuePosition = (uint) position});
        }
    }

    private void CheckDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(
                "WaitingwayClient has been disposed (did the server forcibly disconnect your client?)");
        }
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        ResetQueue();

        _plugin.PluginInterface.LanguageChanged -= LanguageChanged;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        _connection.DisposeAsync();
    }

    public void Send(IPacket packet)
    {
        Task.Run(() => SendAsync(packet));
    }

    public async Task SendAsync(IPacket packet)
    {
        CheckDisposed();

        if (!Connected)
        {
            PluginLog.Warning($"Not connected to server, skipping send of {packet.GetType().Name} packet");
            return;
        }

        try
        {
#if DEBUG
            PluginLog.LogDebug($"Sending {packet.GetType().Name} packet");
#endif
            await _connection.InvokeAsync(packet.GetType().Name, packet);
        }
        catch (TaskCanceledException ex)
        {
            PluginLog.Warning($"SendAsync for {packet.GetType().Name} was cancelled, backing away");
        }
        catch (Exception ex)
        {
            PluginLog.Log($"Error while sending packet to server: {ex}");
        }
    }
}
