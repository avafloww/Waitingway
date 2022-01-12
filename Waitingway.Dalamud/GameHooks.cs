using System;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using Siggingway;
using Waitingway.Common.Protocol.Serverbound;
using Waitingway.Dalamud.Network;
using Waitingway.Dalamud.Structs;

namespace Waitingway.Dalamud;

public class GameHooks : IDisposable
{
    [Signature(
        "48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 8B 41 10 48 8D 7A 10",
        DetourName = nameof(LobbyStatusUpdateDetour))]
    private Hook<OnLobbyErrorCode> LobbyStatusHook { get; init; } = null!;

    [Signature("40 ?? 55 57 48 81 ?? ?? ?? ?? ?? 48 8B ?? ?? ?? ?? ?? 48 33 ?? ?? 89 ?? ?? ?? 0F B7",
        DetourName = nameof(SelectYesNoEventDetour))]
    private Hook<OnSelectYesNoEvent> SelectYesNoHook { get; init; } = null!;

    private delegate IntPtr OnLobbyErrorCode(IntPtr a1, IntPtr a2);

    private delegate IntPtr OnSelectYesNoEvent(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr data);

    public bool IsDisposed { get; private set; }

    private WaitingwayClient Client => Plugin.Client;
    private Plugin Plugin { get; }

    public GameHooks(Plugin plugin, SigScanner sigScanner)
    {
        Plugin = plugin;
        Siggingway.Siggingway.Initialise(sigScanner, this);
        
        LobbyStatusHook.Enable();
        // don't enable yesno here, we only enable it while the SelectYesno we want to watch is on screen
    }

    internal void ToggleSelectYesNoHook(bool state)
    {
        if (!state && SelectYesNoHook.IsEnabled)
        {
            SelectYesNoHook.Disable();
        } else if (state && !SelectYesNoHook.IsEnabled)
        {
            SelectYesNoHook.Enable();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException("GameHooks");
        }

        if (disposing)
        {
            LobbyStatusHook.Dispose();
            SelectYesNoHook.Dispose();
        }

        IsDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal unsafe IntPtr LobbyStatusUpdateDetour(IntPtr a1, IntPtr a2)
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

        return LobbyStatusHook.Original(a1, a2);
    }

    internal IntPtr SelectYesNoEventDetour(IntPtr atkUnit, ushort eventType, int which, IntPtr source,
        IntPtr data)
    {
        try
        {
            if (eventType == 0x19 && which == 0) // EventType.Change, Yes button
            {
                if (Client.InQueue)
                {
                    PluginLog.Log("Sending QueueExit due to user cancellation of login queue");
                    Client.ExitQueue(QueueExit.QueueExitReason.UserCancellation);
                }
                else if (Plugin.GameGui.GetAddonByName("CharaSelect", 1) != IntPtr.Zero)
                {
                    // logging in with a character, assuming CharaSelect is up...
                    Client.EnterLoginQueue();
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Exception in SelectYesNoEventDetour: {ex}");
        }

        return SelectYesNoHook.Original(atkUnit, eventType, which, source, data);
    }
}