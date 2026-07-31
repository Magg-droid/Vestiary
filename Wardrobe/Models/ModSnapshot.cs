using System;
using System.Collections.Generic;

namespace Wardrobe.Models;

[Serializable]
public class ModSnapshot
{
    public Guid DesignId { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public List<string> ItemNames { get; set; } = new();
    public List<ModEntry> Mods { get; set; } = new();
}

[Serializable]
public class ModEntry
{
    public string DirName { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int Priority { get; set; }
    public Dictionary<string, List<string>> Settings { get; set; } = new();
}
