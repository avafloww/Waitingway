using System;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Waitingway.Dalamud.Network;
using Waitingway.Common.Protocol.Serverbound;

namespace Waitingway.Dalamud;

public class Plugin : IDalamudPlugin
{
    public string Name => "Waitingway";

    [PluginService]
    [RequiredVersion("1.0")]
    private DalamudPluginInterface PluginInterface { get; init; }

    [PluginService]
    [RequiredVersion("1.0")]
    private ClientState ClientState { get; init; }

    [PluginService]
    [RequiredVersion("1.0")]
    private SigScanner SigScanner { get; init; }

    [PluginService]
    [RequiredVersion("1.0")]
    private Framework Framework { get; init; }

    [PluginService]
    [RequiredVersion("1.0")]
    private GameGui GameGui { get; init; }

    private readonly Configuration _config;

    private ulong _selectedCharacterId;
    private ushort _selectedDataCenter;
    private ushort _selectedWorld;
    private DateTime? _currentLoginStartTime;

    private Hook<OnLobbyErrorCode> _lobbyStatusHook;

    private WaitingwayClient _client;

    private delegate IntPtr OnLobbyErrorCode(IntPtr a1, IntPtr a2);

    [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
    public Plugin()
    {
        _config = PluginInterface!.GetPluginConfig() as Configuration ?? new Configuration();
        _config.Initialize(PluginInterface);
        PluginLog.Log($"Waitingway Client ID: {_config.ClientId}");

        _client = new WaitingwayClient(_config.RemoteServer, _config.ClientId);

        var lobbyStatusAddr = SigScanner!.ScanText(
            "48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 8B 41 10 48 8D 7A 10");
        PluginLog.Log($"Found LobbyStatus address: {lobbyStatusAddr.ToInt64():X}");
        _lobbyStatusHook =
            new Hook<OnLobbyErrorCode>(lobbyStatusAddr, LobbyStatusUpdateDetour);
        _lobbyStatusHook.Enable();

        ClientState.Login += HandleLogin;
        ClientState.Logout += HandleLogout;

        Framework.Update += OnFrameworkUpdate;
    }

    private unsafe void OnFrameworkUpdate(Framework framework)
    {
        if (ClientState.IsLoggedIn)
        {
            // don't unnecessarily cycle once the player is actually logged in
            return;
        }

        var agentLobby = AgentLobby.Instance();
        if (agentLobby->SelectedCharacterId > 0 && agentLobby->SelectedCharacterId != _selectedCharacterId)
        {
            // reset current login attempt
            _currentLoginStartTime = null;

            _selectedCharacterId = agentLobby->SelectedCharacterId;
            _selectedDataCenter = agentLobby->DataCenter;
            _selectedWorld = *(ushort*) ((byte*) agentLobby + 0x824);
            // todo: once Dalamud includes the latest clientstructs
            // _selectedWorld = agentLobby->WorldId;

            PluginLog.Log(
                $"Selected character: {_selectedCharacterId:X} on data center {_selectedDataCenter}");
        }

        // are we currently in a login attempt?
        if (_currentLoginStartTime == null)
        {
            // when charaSelectList initially disappears, and we have a 1007 status, we can assume we have started a login attempt
            if (_selectedCharacterId > 0 && GameGui.GetAddonByName("_CharaSelectListMenu", 1) == IntPtr.Zero)
            {
                _currentLoginStartTime = DateTime.Now;
                _client.Send(new LoginQueueEnter(_config.ClientId, _selectedCharacterId, _selectedDataCenter, _selectedWorld));
                PluginLog.Log(
                    $"Login attempt started at {_currentLoginStartTime} with character {_selectedCharacterId:X} on data center {_selectedDataCenter} and world {_selectedWorld}");
            }
        }
    }

    private unsafe IntPtr LobbyStatusUpdateDetour(IntPtr a1, IntPtr a2)
    {
        var worldNameAddon = GameGui.GetAddonByName("_CharaSelectListMenu", 1);
        PluginLog.Log($"worldNameAddon: {worldNameAddon.ToInt64():X}");
        var charaId = AgentLobby.Instance()->SelectedCharacterId;
        PluginLog.Log($"characterId: {charaId:X}");
        var lobbyStatus = (LobbyStatusUpdate*) a2.ToPointer();
        if (lobbyStatus->statusCode == (uint) LobbyStatusCode.WorldFull)
        {
            PluginLog.Log(
                $"LobbyStatusUpdate: status = {lobbyStatus->statusCode}, queue length = {lobbyStatus->queueLength}");
            _client.Send(new QueueStatusUpdate
            {
                QueuePosition = lobbyStatus->queueLength
            });
        }

        return _lobbyStatusHook.Original(a1, a2);
    }

    private void HandleLogin(object sender, EventArgs eventArgs)
    {
        PluginLog.Log($"HandleLogin: sender = {sender}, eventArgs = {eventArgs}");
        if (_currentLoginStartTime != null)
        {
            var duration = DateTime.Now - _currentLoginStartTime;
            PluginLog.Log($"Login queue took {duration}");
            _currentLoginStartTime = null;
        }
    }

    private void HandleLogout(object sender, EventArgs eventArgs)
    {
        PluginLog.Log($"HandleLogout: sender = {sender}, eventArgs = {eventArgs}");
        // just in case...
        _currentLoginStartTime = null;
    }

    #region IDisposable Support

    public virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _lobbyStatusHook?.Disable();
        _lobbyStatusHook?.Dispose();

        PluginInterface.SavePluginConfig(_config);

        ClientState.Login -= HandleLogin;
        ClientState.Logout -= HandleLogout;

        Framework.Update -= OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}