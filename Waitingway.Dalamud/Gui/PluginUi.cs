using System;
using ImGuiNET;
using Waitingway.Protocol;

namespace Waitingway.Dalamud.Gui;

internal class PluginUi : IDisposable
{
    internal readonly Plugin Plugin;
    internal readonly LoginQueueWindow LoginQueueWindow;
    internal readonly ConfigButtonOpenerWindow ConfigButtonOpenerWindow;
    internal readonly ConfigWindow ConfigWindow;
    internal GuiText[]? QueueText;

    public PluginUi(Plugin plugin)
    {
        Plugin = plugin;

        LoginQueueWindow = new LoginQueueWindow(this);
        ConfigButtonOpenerWindow = new ConfigButtonOpenerWindow(this);
        ConfigWindow = new ConfigWindow(this);

        SetStatusText("Waiting for server...", GuiText.GuiTextColor.White);
        Plugin.PluginInterface.UiBuilder.Draw += Draw;
    }

    private void Draw()
    {
        LoginQueueWindow.Draw();
        ConfigButtonOpenerWindow.Draw();
        ConfigWindow.Draw();
    }

    public void Dispose()
    {
        Plugin.PluginInterface.UiBuilder.Draw -= Draw;
        ConfigButtonOpenerWindow.Dispose();
    }

    internal void SetStatusText(string status, GuiText.GuiTextColor color = GuiText.GuiTextColor.Red)
    {
        QueueText = new[]
        {
            new GuiText
            {
                Color = color,
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