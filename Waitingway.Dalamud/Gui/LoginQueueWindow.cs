using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace Waitingway.Dalamud.Gui;

internal class LoginQueueWindow
{
    private readonly PluginUi _ui;
    private float _posX;
    private float _posY;
    private float _width;
    private bool _foundPositions;

    public LoginQueueWindow(PluginUi ui)
    {
        _ui = ui;
    }

    public void Draw()
    {
        if (!_ui.Plugin.InLoginQueue()
            || _ui.Plugin.GameGui.GetAddonByName("CharaSelect", 1) == IntPtr.Zero
            || !_foundPositions)
        {
            return;
        }

        ImGuiHelpers.ForceNextWindowMainViewport();
        if (ImGui.Begin("Waitingway", ImGuiWindowFlags.NoNav
                                      | ImGuiWindowFlags.NoNavFocus
                                      | ImGuiWindowFlags.NoNavInputs
                                      | ImGuiWindowFlags.NoResize
                                      | ImGuiWindowFlags.NoScrollbar
                                      | ImGuiWindowFlags.NoSavedSettings
                                      | ImGuiWindowFlags.NoFocusOnAppearing
                                      | ImGuiWindowFlags.NoDocking
                                      | ImGuiWindowFlags.NoCollapse
                                      | ImGuiWindowFlags.NoMove
                                      | ImGuiWindowFlags.NoTitleBar
            ))
        {
            ImGui.SetWindowPos(new Vector2(_posX, _posY));
            ImGui.SetWindowSize(new Vector2(_width, 0));

            _ui.DrawQueueText();

            ImGui.Separator();
            ImGui.Text($"Waitingway version {_ui.Plugin.Version} (alpha)");
            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Globe))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://waitingway.com",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    PluginLog.LogError($"Failed to open website: {ex}");
                }
            }

            ImGui.SameLine();
            // todo: replace with Discord icon once fixed in Dalamud
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Comments))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://discord.waitingway.com",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    PluginLog.LogError($"Failed to open website: {ex}");
                }
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
            {
                _ui.ConfigWindow.ToggleVisible();
            }

            ImGui.TextColored(ImGuiColors.DalamudGrey,
                "Please report any bugs on the support Discord.");
            ImGui.End();
        }
    }

    public unsafe void SetDrawPos(AtkUnitBase* loginWindow)
    {
        if (loginWindow == null || loginWindow->RootNode == null)
        {
            return;
        }

        _posX = loginWindow->X;
        _posY = loginWindow->Y
                + loginWindow->RootNode->Height * loginWindow->RootNode->ScaleY
                - 10 * loginWindow->RootNode->ScaleY;
        _width = loginWindow->RootNode->Width * loginWindow->RootNode->ScaleX;
        _foundPositions = true;
    }
}