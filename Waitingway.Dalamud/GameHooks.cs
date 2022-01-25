using System;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Siggingway;
using Waitingway.Dalamud.Network;
using Waitingway.Dalamud.Structs;
using Waitingway.Protocol.Serverbound;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Waitingway.Dalamud;

public unsafe class GameHooks : IDisposable
{
    [Signature(
        "48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 8B 41 10 48 8D 7A 10",
        DetourName = nameof(LobbyStatusUpdateDetour))]
    private Hook<OnLobbyErrorCode> LobbyStatusHook { get; init; } = null!;

    [Signature("40 55 56 57 41 54 41 55 48 8D AC 24 ?? ?? ?? ??", DetourName = nameof(AgentLobbyVf0Detour))]
    private Hook<AgentLobbyVf0> AgentLobbyVf0Hook { get; init; } = null!;

    private delegate IntPtr OnLobbyErrorCode(IntPtr a1, IntPtr a2);

    private delegate IntPtr AgentLobbyVf0(AgentLobby* agent, IntPtr a2, AtkValue* value, IntPtr a4, int action);

    public bool IsDisposed { get; private set; }

    private WaitingwayClient Client => Plugin.Client;
    private Plugin Plugin { get; }

#if DEBUG
    public bool ThrowInHooks = false;
#endif

    public GameHooks(Plugin plugin, SigScanner sigScanner)
    {
        Plugin = plugin;
        Siggingway.Siggingway.Initialise(sigScanner, this);

        LobbyStatusHook.Enable();
        AgentLobbyVf0Hook.Enable();
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
            AgentLobbyVf0Hook.Dispose();
        }

        IsDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal IntPtr LobbyStatusUpdateDetour(IntPtr a1, IntPtr a2)
    {
        try
        {
#if DEBUG
            if (ThrowInHooks)
            {
                throw new Exception("ThrowInHooks test: LobbyStatusUpdateDetour");
            }
#endif

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
                    Client.UpdateQueuePosition(lobbyStatus->queueLength);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Log($"Exception in LobbyStatusUpdateDetour: {ex}");
        }

        return LobbyStatusHook.Original(a1, a2);
    }

    internal IntPtr AgentLobbyVf0Detour(AgentLobby* agent, IntPtr a2, AtkValue* value, IntPtr a4, int action)
    {
        // as of 6.05: action is 0x03 immediately after confirming login, and 0x22 immediately after confirming login queue cancellation
        // value = 0: SelectYesNo "Yes" selected

        try
        {
#if DEBUG
            if (ThrowInHooks)
            {
                throw new Exception("ThrowInHooks test: AgentLobbyVf0Detour");
            }
#endif

            // don't crash the game if something funky is going on
            if (value == null)
            {
                PluginLog.LogWarning("AgentLobbyVf0Detour: AtkValue ptr is null?");
                return AgentLobbyVf0Hook.Original(agent, a2, value, a4, action);
            }

            if (value->Type != ValueType.Int && value->Type != ValueType.UInt)
            {
                PluginLog.LogWarning($"AgentLobbyVf0Detour: AtkValue type was unexpected ({value->Type})?");
                return AgentLobbyVf0Hook.Original(agent, a2, value, a4, action);
            }

            if (value->Int == 0x00)
            {
                if (action == 0x03) // confirm login
                {
                    Client.EnterLoginQueue();
                }
                else if (action == 0x22) // cancel login queue
                {
                    PluginLog.Log("Sending QueueExit due to user cancellation of login queue");
                    Client.ExitQueue(QueueExit.QueueExitReason.UserCancellation);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.LogError($"Exception in AgentLobbyVf0Detour: {ex}");
        }

        return AgentLobbyVf0Hook.Original(agent, a2, value, a4, action);
    }
}