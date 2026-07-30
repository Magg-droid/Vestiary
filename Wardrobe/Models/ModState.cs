using System;
using System.Collections.Generic;

namespace Wardrobe.Models;

/// <summary>
/// Captured mod state for a single design. Stored per-design in Configuration.
/// </summary>
public class ModState
{
    /// <summary>The design this state belongs to.</summary>
    public Guid DesignId { get; set; }

    /// <summary>Penumbra collection name assigned when captured.</summary>
    public string? PenumbraCollection { get; set; }

    /// <summary>Raw Glamourer state JSON captured at snapshot time.</summary>
    public string? GlamourerStateJson { get; set; }

    /// <summary>List of mod paths that were active.</summary>
    public List<string> ActiveMods { get; set; } = new();

    /// <summary>UTC timestamp of the capture.</summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}
