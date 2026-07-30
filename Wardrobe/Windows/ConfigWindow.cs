using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Wardrobe.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("Wardrobe Settings##WardrobeConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(300, 270);
        SizeCondition = ImGuiCond.Appearing;

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (plugin.IsCameraActive) return;

        // ── Apply Equipment Only ──
        var eqOnly = configuration.ApplyEquipmentOnly;
        if (ImGui.Checkbox("Apply Equipment Only", ref eqOnly))
        {
            configuration.ApplyEquipmentOnly = eqOnly;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("When enabled, design apply will only change gear, not character appearance");

        // ── Show Hidden ──
        var showHidden = configuration.ShowHidden;
        if (ImGui.Checkbox("Show Hidden", ref showHidden))
        {
            configuration.ShowHidden = showHidden;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Show hidden designs instead of visible ones");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── Theme ──
        ImGui.TextColored(ThemeManager.Current.TextHeading, "Theme");
        ImGui.Spacing();

        int selectedTheme = configuration.ThemeName switch
        {
            "Ocean" => 1,
            "Midnight Purple" => 2,
            "Forest" => 3,
            _ => 0,
        };
        if (ImGui.RadioButton("Classic", ref selectedTheme, 0))
        {
            configuration.ThemeName = "Classic";
            configuration.Save();
            ThemeManager.SetTheme("Classic");
        }
        if (ImGui.RadioButton("Ocean", ref selectedTheme, 1))
        {
            configuration.ThemeName = "Ocean";
            configuration.Save();
            ThemeManager.SetTheme("Ocean");
        }
        if (ImGui.RadioButton("Midnight Purple", ref selectedTheme, 2))
        {
            configuration.ThemeName = "Midnight Purple";
            configuration.Save();
            ThemeManager.SetTheme("Midnight Purple");
        }
        if (ImGui.RadioButton("Forest", ref selectedTheme, 3))
        {
            configuration.ThemeName = "Forest";
            configuration.Save();
            ThemeManager.SetTheme("Forest");
        }
    }
}
