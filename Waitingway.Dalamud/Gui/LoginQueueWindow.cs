using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace Waitingway.Dalamud.Gui;

internal class LoginQueueWindow
{
    private readonly PluginUi ui;
    private int _posX = 0;
    private int _posY = 0;
    private int _width = 400;

    public LoginQueueWindow(PluginUi ui)
    {
        this.ui = ui;
    }

    public void Draw()
    {
        if (!ui.Plugin.InLoginQueue())
        {
            return;
        }

        ImGui.Begin("Waitingway", ImGuiWindowFlags.NoNav
                                  | ImGuiWindowFlags.NoNavFocus
                                  | ImGuiWindowFlags.NoNavInputs
                                  | ImGuiWindowFlags.NoResize
                                  | ImGuiWindowFlags.NoScrollbar
                                  | ImGuiWindowFlags.NoSavedSettings
                                  | ImGuiWindowFlags.NoFocusOnAppearing
                                  | ImGuiWindowFlags.NoDocking
                                  | ImGuiWindowFlags.NoCollapse
                                  | ImGuiWindowFlags.NoMove
                                  | ImGuiWindowFlags.NoMouseInputs
        );
        ImGui.SetWindowPos(new Vector2(_posX, _posY));
        ImGui.SetWindowSize(new Vector2(_width, 0));

        ImGui.Text("asdf");
        ImGui.End();
    }

    public unsafe void SetDrawPos(AtkUnitBase* loginWindow)
    {
        if (loginWindow == null || loginWindow->RootNode == null)
        {
            return;
        }
        
        _posX = loginWindow->X;
        _posY = loginWindow->Y + loginWindow->RootNode->Height - 10;
        _width = loginWindow->RootNode->Width;
    }
}