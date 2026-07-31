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

        var eqOnly = configuration.ApplyEquipmentOnly;
        if (ImGui.Checkbox(Strings.SettingsApplyEquipmentOnly, ref eqOnly))
        {
            configuration.ApplyEquipmentOnly = eqOnly;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Strings.SettingsApplyEquipmentTooltip);

        var showHidden = configuration.ShowHidden;
        if (ImGui.Checkbox(Strings.SettingsShowHidden, ref showHidden))
        {
            configuration.ShowHidden = showHidden;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Strings.SettingsShowHiddenTooltip);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(ThemeManager.Current.TextHeading, Strings.SettingsThemeHeading);
        ImGui.Spacing();

        int selectedTheme = configuration.ThemeName switch
        {
            "Ocean" => 1,
            "Midnight Purple" => 2,
            "Forest" => 3,
            _ => 0,
        };
        if (ImGui.RadioButton(Strings.SettingsThemeClassic, ref selectedTheme, 0))
            SetTheme("Classic");
        if (ImGui.RadioButton(Strings.SettingsThemeOcean, ref selectedTheme, 1))
            SetTheme("Ocean");
        if (ImGui.RadioButton(Strings.SettingsThemePurple, ref selectedTheme, 2))
            SetTheme("Midnight Purple");
        if (ImGui.RadioButton(Strings.SettingsThemeForest, ref selectedTheme, 3))
            SetTheme("Forest");
    }

    private void SetTheme(string name)
    {
        configuration.ThemeName = name;
        configuration.Save();
        ThemeManager.SetTheme(name);
    }
}
