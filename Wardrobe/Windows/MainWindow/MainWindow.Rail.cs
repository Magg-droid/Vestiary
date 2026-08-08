using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Wardrobe.Windows;

public partial class MainWindow
{
    private const float RailWidth = 150f;

    private void DrawRail()
    {
        ImGui.BeginChild("##Rail", new Vector2(RailWidth, -1), false,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        var dl = ImGui.GetWindowDrawList();
        var railStart = ImGui.GetCursorScreenPos();
        var railW = ImGui.GetContentRegionAvail().X;

        // Rail background
        dl.AddRectFilled(
            railStart,
            railStart + new Vector2(railW, ImGui.GetWindowHeight()),
            ImGui.GetColorU32(ThemeManager.Current.RailBg));

        ImGui.Spacing();
        ImGui.Spacing();

        // Glamour
        DrawRailItem(Strings.RailGlamour, _currentView == 0, () => _currentView = 0);

        ImGui.Spacing();

        // Emotes (only if enabled)
        if (plugin.Configuration.EnableEmotes)
        {
            DrawRailItem(Strings.RailEmotes, _currentView == 1, () => _currentView = 1);
            ImGui.Spacing();
        }

        // Roulette (only if enabled)
        if (plugin.Configuration.EnableGlamourRoulette)
        {
            DrawRailItem(Strings.RailRoulette, _currentView == 2, () => _currentView = 2);
            ImGui.Spacing();
        }

        ImGui.Spacing();

        // Divider before settings
        var div2Start = ImGui.GetCursorScreenPos();
        dl.AddLine(
            new Vector2(div2Start.X + 12f, div2Start.Y),
            new Vector2(div2Start.X + railW - 12f, div2Start.Y),
            ImGui.GetColorU32(ThemeManager.Current.RailDivider), 1f);

        ImGui.Spacing();
        ImGui.Spacing();

        // Settings
        DrawRailItem(Strings.Settings, false, () => plugin.ToggleConfigUi());

        ImGui.Spacing();

        // Help
        DrawRailItem(Strings.RailHelp, false, () => plugin.GuideWin.Toggle());

        ImGui.Spacing();

        // Divider before minimize
        var div3Start = ImGui.GetCursorScreenPos();
        dl.AddLine(
            new Vector2(div3Start.X + 12f, div3Start.Y),
            new Vector2(div3Start.X + railW - 12f, div3Start.Y),
            ImGui.GetColorU32(ThemeManager.Current.RailDivider), 1f);

        ImGui.Spacing();
        ImGui.Spacing();

        // Minimize / Expand
        bool minimized = plugin.Configuration.IsMinimized;
        DrawRailItem(
            minimized ? Strings.RailExpand : Strings.RailMinimize,
            false,
            () =>
            {
                plugin.Configuration.IsMinimized = !plugin.Configuration.IsMinimized;
                plugin.Configuration.Save();
            });

        ImGui.EndChild();
    }

    private void DrawRailItem(string label, bool active, Action onClick)
    {
        float itemW = ImGui.GetContentRegionAvail().X - 16f;
        float itemH = 30f;
        float rounding = 8f;

        ImGui.SetCursorPosX(8f);
        var min = ImGui.GetCursorScreenPos();
        var max = min + new Vector2(itemW, itemH);
        var dl = ImGui.GetWindowDrawList();

        bool hovered = ImGui.IsMouseHoveringRect(min, max);

        uint bg = active
            ? ImGui.GetColorU32(ThemeManager.Current.RailItemBgActive)
            : hovered
                ? ImGui.GetColorU32(ThemeManager.Current.RailItemBgHovered)
                : 0;

        if (bg != 0)
            dl.AddRectFilled(min, max, bg, rounding);

        uint textCol = ImGui.GetColorU32(
            active || hovered ? ThemeManager.Current.RailTextActive : ThemeManager.Current.RailTextIdle);

        var textSize = ImGui.CalcTextSize(label);
        dl.AddText(
            new Vector2(min.X + 12f, min.Y + (itemH - textSize.Y) / 2f),
            textCol, label);

        ImGui.InvisibleButton($"##rail_{label}", new Vector2(itemW, itemH));
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            onClick();
        if (hovered)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
    }
}
