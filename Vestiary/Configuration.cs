using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using Vestiary.Models;

namespace Vestiary;

[Serializable]
public class Configuration : IPluginConfiguration
{
    private const long DebounceTicks = 2 * TimeSpan.TicksPerSecond;

    private bool dirty;
    private long lastSaveTick;
    private long lastChangeTick;

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
    public string ThemeName { get; set; } = "Ocean";
    public List<Guid> HiddenDesignIds { get; set; } = new();
    public List<EmoteCard> EmoteCards { get; set; } = new();
    public List<EmoteCollection> EmoteCollections { get; set; } = new();
    public List<Guid> FavoriteDesignIds { get; set; } = new();
    public List<Collection> Collections { get; set; } = new();
    public List<DesignMetadata> DesignMetadata { get; set; } = new();

    /// <summary>
    /// Mark configuration as needing a save. If it's been more than 2 seconds since
    /// the last write, saves immediately. Otherwise defers to <see cref="FlushIfNeeded"/>.
    /// </summary>
    public void Save()
    {
        var now = DateTime.UtcNow.Ticks;

        if (now - lastSaveTick > DebounceTicks)
        {
            WriteToDisk(now);
        }
        else
        {
            dirty = true;
            lastChangeTick = now;
        }
    }

    /// <summary>
    /// Call every frame. If a deferred save is pending and 2 seconds have passed
    /// since the last change, writes to disk.
    /// </summary>
    public void FlushIfNeeded()
    {
        if (!dirty)
            return;

        var now = DateTime.UtcNow.Ticks;
        if (now - lastChangeTick >= DebounceTicks)
            WriteToDisk(now);
    }

    /// <summary>
    /// Force an immediate write if there are pending changes.
    /// Call on plugin shutdown to prevent data loss.
    /// </summary>
    public void FlushNow()
    {
        if (dirty)
            WriteToDisk(DateTime.UtcNow.Ticks);
    }

    private void WriteToDisk(long now)
    {
        dirty = false;
        lastSaveTick = now;
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
