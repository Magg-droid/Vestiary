using System;
using System.Collections.Generic;
using System.Linq;
using Wardrobe.Models;

namespace Wardrobe.Services;

/// <summary>
/// Captures and restores mod state (Penumbra + Glamourer) per design.
/// Called by the lock icon on design cards.
/// </summary>
public class ModStateService
{
    private readonly Configuration configuration;
    private readonly PenumbraService penumbraService;
    private readonly GlamourerService glamourerService;

    public ModStateService(
        Configuration configuration,
        PenumbraService penumbraService,
        GlamourerService glamourerService)
    {
        this.configuration = configuration;
        this.penumbraService = penumbraService;
        this.glamourerService = glamourerService;
    }

    /// <summary>
    /// Captures the current mod state for a design and stores it in configuration.
    /// Called when the user clicks the lock icon.
    /// </summary>
    public void CaptureState(Guid designId)
    {
        var state = new ModState { DesignId = designId };

        // TODO: Capture Penumbra collection
        // state.PenumbraCollection = penumbraService.GetCollectionForPlayer();
        // state.ActiveMods = penumbraService.GetModsInCollection(state.PenumbraCollection);

        // TODO: Capture Glamourer state
        // state.GlamourerStateJson = glamourerService.GetDesignState(designId);

        // Save
        var existing = configuration.ModStates?.FirstOrDefault(m => m.DesignId == designId);
        if (existing != null)
            configuration.ModStates!.Remove(existing);
        configuration.ModStates ??= new List<ModState>();
        configuration.ModStates.Add(state);
        configuration.Save();
    }

    /// <summary>
    /// Restores the saved mod state for a design.
    /// Called when the user applies a design that has a captured state.
    /// </summary>
    public void RestoreState(Guid designId)
    {
        var state = GetState(designId);
        if (state == null)
            return;

        // TODO: Restore Penumbra collection
        // if (!string.IsNullOrEmpty(state.PenumbraCollection))
        //     penumbraService.SetCollectionForPlayer(state.PenumbraCollection);

        // TODO: Restore Glamourer state
        // if (!string.IsNullOrEmpty(state.GlamourerStateJson))
        //     glamourerService.ApplyDesignState(designId, state.GlamourerStateJson);
    }

    /// <summary>
    /// Checks if a design has captured mod state.
    /// </summary>
    public bool HasState(Guid designId) =>
        configuration.ModStates?.Any(m => m.DesignId == designId) ?? false;

    /// <summary>
    /// Gets the captured state for a design, or null.
    /// </summary>
    public ModState? GetState(Guid designId) =>
        configuration.ModStates?.FirstOrDefault(m => m.DesignId == designId);
}
