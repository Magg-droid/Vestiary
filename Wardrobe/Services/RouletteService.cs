using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Wardrobe.Models;

namespace Wardrobe.Services;

public class RouletteService : IDisposable
{
    private readonly Configuration configuration;
    private readonly GlamourerService glamourer;
    private readonly ModStateService modState;
    private readonly CollectionService collectionService;
    private readonly HiddenDesignService hiddenDesignService;
    private readonly IFramework framework;

    private DateTime lastTriggerUtc = DateTime.MinValue;
    private Guid lastRouletteDesignId = Guid.Empty;

    public RouletteService(
        Configuration configuration,
        GlamourerService glamourer,
        ModStateService modState,
        CollectionService collectionService,
        HiddenDesignService hiddenDesignService,
        IFramework framework)
    {
        this.configuration = configuration;
        this.glamourer = glamourer;
        this.modState = modState;
        this.collectionService = collectionService;
        this.hiddenDesignService = hiddenDesignService;
        this.framework = framework;

        this.framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        framework.Update -= OnFrameworkUpdate;
    }

    public bool IsActive => configuration.EnableGlamourRoulette && configuration.RouletteActive;

    public DateTime NextTriggerTime
    {
        get
        {
            if (!IsActive || lastTriggerUtc == DateTime.MinValue)
                return DateTime.MaxValue;

            var interval = TimeSpan.FromMinutes(Math.Max(1, configuration.RouletteIntervalMinutes));
            return lastTriggerUtc + interval;
        }
    }

    public TimeSpan RemainingTime
    {
        get
        {
            if (!IsActive || lastTriggerUtc == DateTime.MinValue)
                return TimeSpan.Zero;

            var next = NextTriggerTime;
            var now = DateTime.UtcNow;
            if (next <= now)
                return TimeSpan.Zero;

            return next - now;
        }
    }

    public void StartRoulette()
    {
        if (!configuration.EnableGlamourRoulette) return;

        configuration.RouletteActive = true;
        configuration.Save();

        TriggerRandomPick(manualTrigger: false);
    }

    public void StopRoulette()
    {
        configuration.RouletteActive = false;
        configuration.Save();
    }

    public void ToggleRoulette()
    {
        if (configuration.RouletteActive)
            StopRoulette();
        else
            StartRoulette();
    }

    public bool TriggerRandomPick(bool manualTrigger = false)
    {
        lastTriggerUtc = DateTime.UtcNow;

        var eligiblePool = GetRouletteDesignPool();
        if (eligiblePool.Count == 0)
        {
            Plugin.Log.Warning("[Roulette] No visible designs available in roulette pool.");
            return false;
        }

        if (!RandomSelectionHelper.TryPickDesign(eligiblePool, ref lastRouletteDesignId, out var designId))
        {
            Plugin.Log.Warning("[Roulette] Failed to pick a design from roulette pool.");
            return false;
        }

        try
        {
            glamourer.ApplyDesign(designId, configuration.ApplyEquipmentOnly);
            modState.RestoreState(designId);
            Plugin.Log.Information($"[Roulette] Automatically applied design {designId} (Manual: {manualTrigger}).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[Roulette] Error applying roulette design.");
            return false;
        }
    }

    public Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> GetRouletteDesignPool()
    {
        var collections = collectionService.GetCollections();
        if (collections.Count == 0)
            return new();

        var selectedIds = configuration.RouletteCollectionIds ?? new();
        List<Collection> targetCollections;

        if (selectedIds.Count > 0)
        {
            targetCollections = collections.Where(c => selectedIds.Contains(c.Id)).ToList();
        }
        else
        {
            targetCollections = collections;
        }

        if (configuration.RouletteExcludeFavorites)
        {
            targetCollections = targetCollections
                .Where(c => !string.Equals(c.Name, "Favorites", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var merged = new Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>();
        foreach (var col in targetCollections)
        {
            foreach (var entry in collectionService.GetDesignsByCollection(col.Id))
            {
                if (!merged.ContainsKey(entry.Key))
                    merged.Add(entry.Key, entry.Value);
            }
        }

        // Roulette always excludes hidden designs so every consumer sees the same eligible pool.
        return hiddenDesignService.GetVisibleDesigns(merged);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!IsActive) return;

        if (lastTriggerUtc == DateTime.MinValue)
        {
            lastTriggerUtc = DateTime.UtcNow;
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, configuration.RouletteIntervalMinutes));
        if (DateTime.UtcNow >= lastTriggerUtc + interval)
        {
            TriggerRandomPick(manualTrigger: false);
        }
    }
}
