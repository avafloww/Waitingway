using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Waitingway.Dalamud.Network;
using Waitingway.Common.Protocol.Serverbound;
using Waitingway.Dalamud.Structs;

namespace Waitingway.Dalamud;

public class Plugin : IDalamudPlugin
{
    public string Name => "Waitingway";

    [PluginService]
    [RequiredVersion("1.0")]
    internal DalamudPluginInterface PluginInterface { get; init; }

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
    internal GameGui GameGui { get; init; }

    private readonly Configuration _config;

    private ulong _selectedCharacterId;
    private ushort _selectedDataCenter;
    private ushort _selectedWorld;
    private DateTime? _currentLoginStartTime;

    private Hook<OnLobbyErrorCode> _lobbyStatusHook;
    private Hook<OnSelectYesNoEvent> _selectYesNoHook;

    internal readonly PluginUi Ui;
    internal readonly WaitingwayClient Client;

    private delegate IntPtr OnLobbyErrorCode(IntPtr a1, IntPtr a2);

    private delegate IntPtr OnSelectYesNoEvent(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr data);

    [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
#pragma warning disable CS8618
#pragma warning disable CS8602
    public Plugin()
    {
        _config = PluginInterface!.GetPluginConfig() as Configuration ?? new Configuration();
        _config.Initialize(PluginInterface);
        _config.Save(); // save immediately in case we generated a new client ID
        PluginLog.Log($"Waitingway Client ID: {_config.ClientId}");

        Ui = new PluginUi(this);

        Client = new WaitingwayClient(this, _config.RemoteServer, _config.ClientId, PluginInterface.UiLanguage);
        PluginInterface.LanguageChanged += Client.LanguageChanged;

        var lobbyStatusAddr = SigScanner!.ScanText(
            "48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 8B 41 10 48 8D 7A 10");
        PluginLog.Log($"Found LobbyStatus address: {lobbyStatusAddr.ToInt64():X}");
        _lobbyStatusHook =
            new Hook<OnLobbyErrorCode>(lobbyStatusAddr, LobbyStatusUpdateDetour);
        _lobbyStatusHook.Enable();

        var selectYesNoEventAddr =
            SigScanner!.ScanText("40 ?? 55 57 48 81 ?? ?? ?? ?? ?? 48 8B ?? ?? ?? ?? ?? 48 33 ?? ?? 89 ?? ?? ?? 0F B7");
        PluginLog.Log($"Found SelectYesNoReceiveEvent address: {selectYesNoEventAddr.ToInt64():X}");
        _selectYesNoHook = new Hook<OnSelectYesNoEvent>(selectYesNoEventAddr, SelectYesNoEventDetour);
        // don't enable here, we only enable it while the SelectYesno we want to watch is on screen

        ClientState.Login += HandleLogin;
        ClientState.Logout += HandleLogout;

        Framework.Update += OnFrameworkUpdate;
    }
#pragma warning restore CS8602
#pragma warning restore CS8618

    public bool InLoginQueue()
    {
        return !ClientState.IsLoggedIn && _currentLoginStartTime != null;
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
        }

        var yesno = GameGui.GetAddonByName("SelectYesno", 1);

        // hook onto yesno dialogs for both exit queue and character select confirmation
        if (yesno != IntPtr.Zero && !_selectYesNoHook.IsEnabled)
        {
            _selectYesNoHook.Enable();
        }
        else if (yesno == IntPtr.Zero && _selectYesNoHook.IsEnabled)
        {
            _selectYesNoHook.Disable();
        }

        // are we currently in a login attempt?
        if (_currentLoginStartTime != null)
        {
            var addon = GameGui.GetAddonByName("SelectOk", 1);
            if (addon != IntPtr.Zero)
            {
                // if the "exit queue?" dialog is on screen, attach to that instead
                if (yesno != IntPtr.Zero)
                {
                    addon = yesno;
                }

                Ui.LoginQueueWindow.SetDrawPos((AtkUnitBase*) addon);
            }
            else
            {
                if (_selectYesNoHook.IsEnabled)
                {
                    _selectYesNoHook.Disable();
                }
            }
        }
    }

    private void StartLoginAttempt()
    {
        _currentLoginStartTime = DateTime.Now;
        Client.Send(new LoginQueueEnter(_config.ClientId, _selectedCharacterId, _config.ClientSalt,
            _selectedDataCenter, _selectedWorld));
    }

    private unsafe IntPtr LobbyStatusUpdateDetour(IntPtr a1, IntPtr a2)
    {
        try
        {
            var lobbyStatus = (LobbyStatusUpdate*) a2.ToPointer();
            if (lobbyStatus->statusCode == LobbyStatusCode.WorldFull)
            {
                PluginLog.Log(
                    $"LobbyStatusUpdate: waiting in queue, queue length = {lobbyStatus->queueLength}");
                if (lobbyStatus->queueLength < 0)
                {
                    // "Character not properly logged off", skip
                    PluginLog.LogWarning(
                        "LobbyStatusUpdate: invalid queue length (character still logged in?), not sending update to server");
                }
                else
                {
                    Client.Send(new QueueStatusUpdate
                    {
                        QueuePosition = (uint) lobbyStatus->queueLength
                    });
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Log($"Exception in LobbyStatusUpdateDetour: {ex}");
        }

        return _lobbyStatusHook.Original(a1, a2);
    }

    private IntPtr SelectYesNoEventDetour(IntPtr atkUnit, ushort eventType, int which, IntPtr source,
        IntPtr data)
    {
        try
        {
            if (eventType == 0x19 && which == 0) // EventType.Change, Yes button
            {
                if (_currentLoginStartTime != null)
                {
                    // leaving login queue
                    _currentLoginStartTime = null;
                    PluginLog.Log("Sending QueueExit due to user cancellation of login queue");
                    Client.Send(new QueueExit {Reason = QueueExit.QueueExitReason.UserCancellation});
                }
                else
                {
                    // logging in with a character, assuming CharaSelect is up...
                    if (GameGui.GetAddonByName("CharaSelect", 1) != IntPtr.Zero)
                    {
                        StartLoginAttempt();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Exception in SelectYesNoEventDetour: {ex}");
        }

        return _selectYesNoHook.Original(atkUnit, eventType, which, source, data);
    }

    private void HandleLogin(object? sender, EventArgs eventArgs)
    {
        PluginLog.Debug($"HandleLogin: sender = {sender}, eventArgs = {eventArgs}");
        if (_currentLoginStartTime != null)
        {
            Client.Send(new QueueExit {Reason = QueueExit.QueueExitReason.Success});
            var duration = DateTime.Now - _currentLoginStartTime;
            PluginLog.Log($"Login queue took {duration}");
            _currentLoginStartTime = null;
        }

        // we don't want to hook yesno dialogs ingame
        if (_selectYesNoHook.IsEnabled)
        {
            _selectYesNoHook.Disable();
        }
    }

    private void HandleLogout(object? sender, EventArgs eventArgs)
    {
        PluginLog.Debug($"HandleLogout: sender = {sender}, eventArgs = {eventArgs}");
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

        PluginInterface.LanguageChanged -= Client.LanguageChanged;
        Client.DisposeAsync();

        Ui.Dispose();

        _selectYesNoHook?.Dispose();
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