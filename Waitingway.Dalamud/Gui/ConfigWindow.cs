using System;
using System.Diagnostics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using ImGuiNET;
using Waitingway.Protocol.Serverbound;

namespace Waitingway.Dalamud.Gui;

internal class ConfigWindow
{
    private readonly PluginUi _ui;
    public bool IsVisible;
    private Configuration Config => _ui.Plugin.Config;
    private bool _pageOpenFailed = false;

    public ConfigWindow(PluginUi ui)
    {
        _ui = ui;
    }

    public void ToggleVisible()
    {
        IsVisible = !IsVisible;
    }

    public void Draw()
    {
        if (!IsVisible)
        {
            return;
        }

        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(600, 0), ImGuiCond.FirstUseEver);
        ImGui.Begin("Waitingway Settings", ref IsVisible, ImGuiWindowFlags.NoCollapse);

        if (ImGui.BeginTabBar("##tabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                DrawGeneralTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Advanced"))
            {
                DrawAdvancedTab();
                ImGui.EndTabItem();
            }

#if DEBUG
            if (ImGui.BeginTabItem("Debug"))
            {
                DrawDebugTab();
                ImGui.EndTabItem();
            }
#endif

            ImGui.EndTabBar();
        }

        ImGui.Separator();

        if (ImGui.Button("Save"))
        {
            _ui.Plugin.Config.Save();
        }

        ImGui.SameLine();

        if (ImGui.Button("Save and Close"))
        {
            _ui.Plugin.Config.Save();
            IsVisible = false;
        }

        ImGui.SameLine();

        if (ImGui.Button("Close"))
        {
            IsVisible = false;
        }

        ImGui.End();
    }

    private void StartTab()
    {
        ImGui.BeginChild("scrolling", ImGuiHelpers.ScaledVector2(0, 300), true, ImGuiWindowFlags.HorizontalScrollbar);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(1, 5));
    }

    private void EndTab()
    {
        ImGui.PopStyleVar();
        ImGui.EndChild();
    }

    private void DrawGeneralTab()
    {
        StartTab();

        ImGui.Text(
            "If you link your Discord account, you can receive notifications about your queue.\n" +
            "Click the button below to link your account.\n" +
            "A browser window will open to complete the process.\n" +
            "\n" +
            "Note: if you have already linked your account, you do not need to do this again."
        );

        if (_pageOpenFailed)
        {
            ImGui.TextColored(
                ImGuiColors.DalamudRed,
                "Failed to open your browser. Please check the logs and report this error."
            );
        }

        if (ImGui.Button("Link Discord Account"))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"https://discord.waitingway.com/link/{Config.ClientId}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Failed to open browser to link Discord");
            }
        }

        EndTab();
    }

    private void DrawAdvancedTab()
    {
        StartTab();

        ImGui.TextColored(ImGuiColors.DalamudGrey,
            "You should not change these settings unless you know what you're doing.");

        ImGui.Separator();

        ImGui.Text("Client ID (click to copy): ");
        ImGui.SameLine();
        ImGuiHelpers.ClickToCopyText(Config.ClientId);

        ImGui.Text("Remote Server Address:");
        ImGui.InputText("##remote_server", ref Config.RemoteServer, 256);
        ImGui.SameLine();
        if (ImGui.Button("Reset"))
        {
            Config.RemoteServer = Configuration.DefaultRemoteServer;
        }

        ImGui.TextColored(ImGuiColors.DalamudGrey,
            "Changes to this setting will not take effect until the next restart.");

        EndTab();
    }

#if DEBUG
    private void DrawDebugTab()
    {
        StartTab();

        ImGui.Checkbox("ForceShow LoginQueueWindow", ref _ui.LoginQueueWindow.ForceShow);
        ImGui.Checkbox("Throw exceptions in hooks to test exception handling", ref _ui.Plugin.Hooks.ThrowInHooks);

        var client = _ui.Plugin.Client;
        if (client.RemoteUrl == Configuration.DefaultRemoteServer)
        {
            ImGui.Text("Server-related debug options are unavailable while pointing to the production server.");
        }
        else
        {
            if (ImGui.Button("Enter Login Queue"))
            {
                client.EnterLoginQueue();
            }

            if (ImGui.Button("Exit Queue"))
            {
                client.ExitQueue(QueueExit.QueueExitReason.UserCancellation);
            }

            if (ImGui.Button("Reset Login Queue"))
            {
                client.ResetLoginQueue(1234, 1234, 1234);
            }
        }

        EndTab();
    }
#endif
}