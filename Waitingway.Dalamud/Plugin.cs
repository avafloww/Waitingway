﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Waitingway.Dalamud.Network;
using Waitingway.Dalamud.Gui;
using Waitingway.Dalamud.Handler;
using Waitingway.Protocol.Serverbound;
using Dalamud.Plugin.Services;

namespace Waitingway.Dalamud;

public class Plugin : IDalamudPlugin
{
    public string Name => "Waitingway";
    public string Version { get; }

    [PluginService]
    internal DalamudPluginInterface PluginInterface { get; init; }

    [PluginService]
    internal IClientState ClientState { get; init; }

    [PluginService]
    internal IDataManager DataManager { get; init; }

    [PluginService]
    internal IFramework Framework { get; init; }

    [PluginService]
    internal IGameGui GameGui { get; init; }

    [PluginService]
    internal IGameInteropProvider GameInteropProvider { get; init; }

    [PluginService]
    internal IPluginLog PluginLog { get; init; }

    internal Configuration Config { get; }
    internal PluginUi Ui { get; }
    internal WaitingwayClient Client { get; }

    internal GameHooks Hooks { get; }
    private LoginQueueHandler LoginQueueHandler { get; }
    private IpcSystem IpcSystem { get; }

#pragma warning disable CS8618
#pragma warning disable CS8602
    [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
    public Plugin()
    {
        // 4-component versions need to go die in a fire
        var vers = typeof(Plugin).Assembly.GetName().Version?.ToString();
        Version = vers == null ? "unknown" : string.Join(".", vers.Split(".").SkipLast(1));

        Config = PluginInterface!.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(PluginInterface);
        Config.Save(); // save immediately in case we generated a new client ID
        PluginLog.Information($"Waitingway Client ID: {Config.ClientId}");

        Hooks = new GameHooks(this);
        Client = new WaitingwayClient(this, DataManager.GameData.Repositories["ffxiv"].Version, Config.RemoteServer, Config.ClientId);
        IpcSystem = new IpcSystem(this);

        Ui = new PluginUi(this);
        PluginInterface.UiBuilder.OpenConfigUi += Ui.ConfigWindow.ToggleVisible;

        // queue handlers
        LoginQueueHandler = new LoginQueueHandler(this);

        ClientState.Logout += HandleLogout;

        Framework.Update += OnFrameworkUpdate;
    }
#pragma warning restore CS8602
#pragma warning restore CS8618

    public bool InLoginQueue()
    {
        return !ClientState.IsLoggedIn && Client.InQueue;
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        if (ClientState.IsLoggedIn)
        {
            // don't unnecessarily cycle once the player is actually logged in
            return;
        }

        var agentLobby = AgentLobby.Instance();

        if (agentLobby->SelectedCharacterContentId > 0 && agentLobby->SelectedCharacterContentId != Client.CharacterId)
        {
            // reset current login attempt
            Client.ResetLoginQueue(
                agentLobby->SelectedCharacterContentId,
                agentLobby->DataCenter,
                agentLobby->WorldId
            );
        }

        // are we currently in a login attempt?
        if (Client.InQueue)
        {
            var addon = GameGui.GetAddonByName("SelectOk", 1);

            if (addon != IntPtr.Zero)
            {
                // if the "exit queue?" dialog is on screen, attach to that instead
                var yesno = GameGui.GetAddonByName("SelectYesno", 1);

                if (yesno != IntPtr.Zero)
                {
                    addon = yesno;
                }

                Ui.LoginQueueWindow.SetDrawPos((AtkUnitBase*) addon);
            }
        }

        var charaSelectList = GameGui.GetAddonByName("_CharaSelectListMenu", 1);

        if (charaSelectList != IntPtr.Zero)
        {
            Ui.ConfigButtonOpenerWindow.SetDrawParams((AtkUnitBase*) charaSelectList);
        }
    }

    private void HandleLogout()
    {
        PluginLog.Debug($"HandleLogout");
        // just in case... cancel any outstanding queues
        Client.TryExitQueue(QueueExit.QueueExitReason.UserCancellation);
    }

    #region IDisposable Support

    public virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        LoginQueueHandler.Dispose();
        IpcSystem.Dispose();

        Client.Dispose();

        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.OpenConfigUi -= Ui.ConfigWindow.ToggleVisible;
        Ui.Dispose();

        Hooks.Dispose();

        PluginInterface.SavePluginConfig(Config);

        ClientState.Logout -= HandleLogout;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
