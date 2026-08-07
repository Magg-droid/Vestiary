using System;
using System.Collections.Generic;

namespace Wardrobe.Models;

[Serializable]
public class EmoteCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmoteName { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public List<ModEntry> Mods { get; set; } = new();
}
