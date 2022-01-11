using System;
using Waitingway.Dalamud.Gui;

namespace Waitingway.Dalamud;

internal class PluginUi : IDisposable
{
    internal readonly Plugin Plugin;
    internal readonly LoginQueueWindow LoginQueueWindow;

    public PluginUi(Plugin plugin)
    {
        Plugin = plugin;

        LoginQueueWindow = new LoginQueueWindow(this);
        
        Plugin.PluginInterface.UiBuilder.Draw += Draw;
    }

    private void Draw()
    {
        LoginQueueWindow.Draw();
    }

    public void Dispose()
    {
        Plugin.PluginInterface.UiBuilder.Draw -= Draw;
    }
}