using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Vestiary.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private bool migrationDone;

    public ConfigWindow(Plugin plugin) : base("Vestiary Settings##VestiaryConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 260),
            MaximumSize = new Vector2(900, 2000),
        };

        Size = new Vector2(360, 380);
        SizeCondition = ImGuiCond.Appearing;

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (plugin.IsCameraActive) return;

        ImGui.BeginChild("##SettingsScroll", Vector2.Zero, false, ImGuiWindowFlags.AlwaysVerticalScrollbar);

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

        var enableSaveMods = configuration.EnableSaveMods;
        if (ImGui.Checkbox(Strings.SettingsEnableSaveMods, ref enableSaveMods))
        {
            configuration.EnableSaveMods = enableSaveMods;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Strings.SettingsEnableSaveModsTooltip);

        var enableEmotes = configuration.EnableEmotes;
        if (ImGui.Checkbox(Strings.SettingsEnableEmotes, ref enableEmotes))
        {
            configuration.EnableEmotes = enableEmotes;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Strings.SettingsEnableEmotesTooltip);

        var enableRoulette = configuration.EnableGlamourRoulette;
        if (ImGui.Checkbox(Strings.SettingsEnableGlamourRoulette, ref enableRoulette))
        {
            configuration.EnableGlamourRoulette = enableRoulette;
            if (!enableRoulette && configuration.RouletteActive)
            {
                configuration.RouletteActive = false;
            }
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Strings.SettingsEnableGlamourRouletteTooltip);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(ThemeManager.Current.TextHeading, Strings.SettingsThemeHeading);
        ImGui.Spacing();

        int selectedTheme = configuration.ThemeName switch
        {
            "Midnight Purple" => 1,
            "Champagne" => 2,
            "Rose" => 3,
            _ => 0,
        };
        if (ImGui.RadioButton(Strings.SettingsThemeOcean, ref selectedTheme, 0))
            SetTheme("Ocean");
        if (ImGui.RadioButton(Strings.SettingsThemePurple, ref selectedTheme, 1))
            SetTheme("Midnight Purple");
        if (ImGui.RadioButton(Strings.SettingsThemeChampagne, ref selectedTheme, 2))
            SetTheme("Champagne");
        if (ImGui.RadioButton(Strings.SettingsThemeRose, ref selectedTheme, 3))
            SetTheme("Rose");

        if (!migrationDone && plugin.UtilityService.CanMigrateFromWardrobe)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(ThemeManager.Current.TextHeading, Strings.SettingsMigrationHeading);
            ImGui.Spacing();
            ImGui.PushTextWrapPos();
            ImGui.TextColored(ThemeManager.Current.TextMuted, Strings.SettingsMigrationDescription);
            ImGui.PopTextWrapPos();
            ImGui.Spacing();

            if (ImGui.Button(Strings.SettingsMigrationButton, new Vector2(ImGui.GetContentRegionAvail().X, 0)))
            {
                plugin.UtilityService.MigrateFromWardrobe(configuration, force: true);
                migrationDone = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Strings.SettingsMigrationTooltip);
        }

        if (migrationDone)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextColored(ThemeManager.Current.TextSuccess, Strings.SettingsMigrationSuccess);
        }

        ImGui.EndChild();
    }

    private void SetTheme(string name)
    {
        configuration.ThemeName = name;
        configuration.Save();
        ThemeManager.SetTheme(name);
    }
}
