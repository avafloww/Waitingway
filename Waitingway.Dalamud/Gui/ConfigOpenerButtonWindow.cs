using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;

namespace Waitingway.Dalamud.Gui;

internal class ConfigButtonOpenerWindow : IDisposable
{
    private readonly PluginUi _ui;
    private readonly TextureWrap _icon;
    private float _posX;
    private float _posY;
    private float _width = 36;
    private float _height = 36;
    private ushort _worldBtnTargetWidth;
    private int _removedWidth;
    private bool _foundPositions;

    public ConfigButtonOpenerWindow(PluginUi ui)
    {
        _ui = ui;

        var assemblyLocation = _ui.Plugin.PluginInterface.AssemblyLocation.DirectoryName!;
        var iconPath = Path.Combine(assemblyLocation, @"Assets\Settings.png");
        _icon = _ui.Plugin.PluginInterface.UiBuilder.LoadImage(iconPath);
    }

    public void Draw()
    {
        if (!ShouldDraw())
        {
            return;
        }

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.SetNextWindowBgAlpha(.9f);

        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
        ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0, 0, 0, 1));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 1));
        ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0, 0, 0, 1));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 1));

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.Begin("###WaitingwayConfigMenuOpener", ImGuiWindowFlags.AlwaysAutoResize
                                                     | ImGuiWindowFlags.NoBackground
                                                     | ImGuiWindowFlags.NoDecoration
                                                     | ImGuiWindowFlags.NoMove
                                                     | ImGuiWindowFlags.NoScrollbar
                                                     | ImGuiWindowFlags.NoResize
                                                     | ImGuiWindowFlags.NoSavedSettings
        );

        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor(8);

        ImGui.SetWindowPos(new Vector2(_posX, _posY));

        ImGui.Image(_icon.ImGuiHandle, new Vector2(_width, _height));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Open Waitingway Settings");
        }

        if (ImGui.IsItemClicked())
        {
            _ui.ConfigWindow.ToggleVisible();
        }

        ImGui.End();
    }

    public unsafe void SetDrawParams(AtkUnitBase* charaSelectList)
    {
        if (charaSelectList == null || charaSelectList->RootNode == null || !charaSelectList->IsVisible)
        {
            return;
        }

        var root = charaSelectList->RootNode;
        var backupBtn = charaSelectList->UldManager.SearchNodeById(6);
        var worldBtn = charaSelectList->UldManager.SearchNodeById(4);

        // PluginLog.Log($"HasAdjustedNativeUi: {HasAdjustedNativeUi()}; worldBtn->Width: {worldBtn->Width}; _worldBtnTargetWidth: {_worldBtnTargetWidth}");
        if (!HasAdjustedNativeUi() || worldBtn->Width != _worldBtnTargetWidth)
        {
            // move the buttons to make a little more room
            var newCharaBtn = charaSelectList->UldManager.SearchNodeById(5);

            _removedWidth = (int) worldBtn->X;
            _worldBtnTargetWidth = (ushort) (worldBtn->Width - _removedWidth);

            AdjustWidth(worldBtn, -_removedWidth);
            AdjustX(newCharaBtn, -_removedWidth);
            AdjustX(backupBtn, -_removedWidth);
        }

        _posX = root->X + (backupBtn->X + backupBtn->Width - 4) * root->ScaleX;
        _posY = root->Y + (backupBtn->Y - 2) * root->ScaleY;
        _width = backupBtn->Width * root->ScaleX + 4;
        _height = backupBtn->Height * root->ScaleY + 4;

        _foundPositions = true;
    }

    private bool ShouldDraw()
    {
        return _ui.Plugin.GameGui.GetAddonByName("_CharaSelectListMenu", 1) != IntPtr.Zero && _foundPositions;
    }

    private bool HasAdjustedNativeUi()
    {
        return _removedWidth != 0;
    }

    public unsafe void Dispose()
    {
        if (HasAdjustedNativeUi())
        {
            var addon = _ui.Plugin.GameGui.GetAddonByName("_CharaSelectListMenu", 1);
            if (addon != IntPtr.Zero)
            {
                var charaSelectList = (AtkUnitBase*) addon;

                var worldBtn = charaSelectList->UldManager.SearchNodeById(4);
                var newCharaBtn = charaSelectList->UldManager.SearchNodeById(5);
                var backupBtn = charaSelectList->UldManager.SearchNodeById(6);

                AdjustWidth(worldBtn, _removedWidth);
                AdjustX(newCharaBtn, _removedWidth);
                AdjustX(backupBtn, _removedWidth);
            }

            _removedWidth = 0;
        }

        _icon.Dispose();
    }

    private unsafe void AdjustWidth(AtkResNode* node, int delta)
    {
        node->SetWidth((ushort) (node->Width + delta));

        var btn = (AtkComponentButton*) node->GetComponent();
        btn->ButtonBGNode->SetWidth((ushort) (btn->ButtonBGNode->Width + delta));
        btn->ButtonTextNode->AtkResNode.SetPositionFloat(btn->ButtonTextNode->AtkResNode.X + (short) (delta / 2),
            btn->ButtonTextNode->AtkResNode.Y);
    }

    private unsafe void AdjustX(AtkResNode* node, int delta)
    {
        node->SetPositionFloat(node->X + delta, node->Y);
    }
}