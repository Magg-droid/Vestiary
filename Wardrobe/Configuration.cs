using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using Wardrobe.Models;

namespace Wardrobe;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool ApplyEquipmentOnly { get; set; }
    public bool ShowHidden { get; set; }
    public string ThemeName { get; set; } = "Rose Gold";
    public List<Guid> HiddenDesignIds { get; set; } = new();
    public List<Collection> Collections { get; set; } = new();
    public List<DesignMetadata> DesignMetadata { get; set; } = new();

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
