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

        Size = new Vector2(280, 100);
        SizeCondition = ImGuiCond.Once;

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (plugin.IsCameraActive) return;

        var eqOnly = configuration.ApplyEquipmentOnly;
        if (ImGui.Checkbox("Apply Equipment Only", ref eqOnly))
        {
            configuration.ApplyEquipmentOnly = eqOnly;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("When enabled, design apply will only change gear, not character appearance");
    }
}
