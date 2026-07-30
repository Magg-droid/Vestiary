using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Wardrobe.Services;

/// <summary>
/// IPC bridge to Penumbra. Handles mod collection queries.
/// </summary>
public class PenumbraService
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog log;

    public PenumbraService(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
    }

    /// <summary>
    /// Returns true if Penumbra is currently loaded and accepting IPC calls.
    /// </summary>
    public bool IsAvailable()
    {
        try
        {
            // TODO: Check if Penumbra IPC is available
            // var penumbra = pluginInterface.GetIpcSubscriber<...>("Penumbra.XXX");
            return false;
        }
        catch (Exception ex)
        {
            log.Error($"Penumbra IPC check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the Penumbra collection assigned to the player character.
    /// Returns null if Penumbra is not available or no collection is assigned.
    /// </summary>
    public string? GetCollectionForPlayer()
    {
        // TODO: Call Penumbra IPC to get current collection
        // var getCollection = pluginInterface.GetIpcSubscriber<Func<string?>>("Penumbra.GetCollectionForObject");
        // return getCollection();
        return null;
    }

    /// <summary>
    /// Gets the list of mods (paths) active in the given collection.
    /// </summary>
    public List<string> GetModsInCollection(string collectionName)
    {
        // TODO: Call Penumbra IPC to get mods in collection
        return new List<string>();
    }

    /// <summary>
    /// Sets the Penumbra collection for the player character.
    /// </summary>
    public void SetCollectionForPlayer(string collectionName)
    {
        // TODO: Call Penumbra IPC to set collection
    }
}
