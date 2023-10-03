using System;
using Waitingway.Dalamud.Network;
using Waitingway.Protocol.Serverbound;

namespace Waitingway.Dalamud.Handler;

internal class LoginQueueHandler : IDisposable
{
    private Plugin Plugin { get; }
    private WaitingwayClient Client => Plugin.Client;

    public LoginQueueHandler(Plugin plugin)
    {
        Plugin = plugin;
        plugin.ClientState.Login += HandleLogin;
    }

    private void HandleLogin()
    {
        Plugin.PluginLog.Debug($"HandleLogin");
        if (Client.InQueue)
        {
            var duration = DateTime.Now - Client.QueueEntryTime;
            Plugin.PluginLog.Information($"Login queue took {duration}");
            Client.ExitQueue(QueueExit.QueueExitReason.Success);
        }
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        Plugin.ClientState.Login -= HandleLogin;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}