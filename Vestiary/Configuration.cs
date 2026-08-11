using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using Vestiary.Models;

namespace Vestiary;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool ApplyEquipmentOnly { get; set; }
    public bool ShowHidden { get; set; }
    public bool EnableSaveMods { get; set; } = false;
    public bool EnableEmotes { get; set; } = false;
    public bool EnableGlamourRoulette { get; set; } = false;
    public bool RouletteActive { get; set; } = false;
    public int RouletteIntervalMinutes { get; set; } = 15;
    public bool RouletteExcludeFavorites { get; set; } = true;
    public List<Guid> RouletteCollectionIds { get; set; } = new();
    public bool IsMinimized { get; set; } = false;
    public string ThemeName { get; set; } = "Classic";
    public List<Guid> HiddenDesignIds { get; set; } = new();
    public List<EmoteCard> EmoteCards { get; set; } = new();
    public List<EmoteCollection> EmoteCollections { get; set; } = new();
    public List<Guid> FavoriteDesignIds { get; set; } = new();
    public List<Collection> Collections { get; set; } = new();
    public List<DesignMetadata> DesignMetadata { get; set; } = new();

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
