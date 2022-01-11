using System;
using Dalamud.Interface.Colors;
using ImGuiNET;
using Waitingway.Common.Protocol;
using Waitingway.Dalamud.Gui;

namespace Waitingway.Dalamud;

internal class PluginUi : IDisposable
{
    internal readonly Plugin Plugin;
    internal readonly LoginQueueWindow LoginQueueWindow;
    internal GuiText[]? QueueText;

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

    internal void SetStatusText(string status)
    {
        QueueText = new[]
        {
            new GuiText
            {
                Color = GuiText.GuiTextColor.White,
                Text = status
            }
        };
    }

    internal void DrawQueueText()
    {
        if (QueueText == null)
        {
            return;
        }

        foreach (var text in QueueText)
        {
            ImGui.TextColored(text.ColorAsVec(), text.Text);
        }
    }
}