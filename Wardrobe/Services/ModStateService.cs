using System;
using System.Linq;
using Dalamud.Plugin.Services;
using Wardrobe.Models;

namespace Wardrobe.Services;

public class ModStateService
{
    private readonly Configuration configuration;
    private readonly PenumbraService penumbra;
    private readonly GlamourerService glamourer;

    public ModStateService(Configuration configuration, PenumbraService penumbra, GlamourerService glamourer)
    {
        this.configuration = configuration;
        this.penumbra = penumbra;
        this.glamourer = glamourer;
    }

    public void CaptureState(Guid designId)
    {
        var collection = penumbra.GetPlayerCollection();
        if (collection == null) return;

        var equipment = glamourer.GetDesignEquipment(designId);
        var itemNames = penumbra.GetDesignItemNames(equipment);
        if (itemNames.Count == 0) return;

        var modList = penumbra.GetModList();
        var snapshot = new ModSnapshot { DesignId = designId };

        foreach (var (dir, modName) in modList)
        {
            var changedItems = penumbra.GetModChangedItems(dir, modName);
            if (!changedItems.Keys.Any(key => itemNames.Contains(key))) continue;

            var settings = penumbra.GetModSettings(collection.Value.Id, dir, modName);
            snapshot.Mods.Add(new ModEntry
            {
                DirName = dir,
                ModName = modName,
                Enabled = settings?.Enabled ?? false,
                Priority = settings?.Priority ?? 0,
                Settings = settings?.Settings ?? new()
            });
        }

        configuration.ModSnapshots ??= new();
        configuration.ModSnapshots.RemoveAll(s => s.DesignId == designId);
        configuration.ModSnapshots.Add(snapshot);
        configuration.Save();
    }

    public bool HasSnapshot(Guid designId) =>
        configuration.ModSnapshots?.Any(s => s.DesignId == designId) ?? false;

    public ModSnapshot? GetSnapshot(Guid designId) =>
        configuration.ModSnapshots?.FirstOrDefault(s => s.DesignId == designId);

    /// <summary>
    /// Restores saved mod state for a design. Called after Glamourer apply.
    /// </summary>
    public void RestoreState(Guid designId, IPluginLog log)
    {
        var snapshot = GetSnapshot(designId);
        if (snapshot == null) return;

        var collection = penumbra.GetPlayerCollection();
        if (collection == null) return;

        log.Information($"[ModSnapshot] Restoring {snapshot.Mods.Count} mods...");

        int enabled = 0, disabled = 0, unchanged = 0, errors = 0;
        foreach (var mod in snapshot.Mods)
        {
            var ec = penumbra.TrySetMod(collection.Value.Id, mod.DirName, mod.Enabled, mod.ModName);
            if (ec == 0)
            {
                if (mod.Enabled) { enabled++; log.Information($"[ModSnapshot]   ✅ Enabled  [{mod.DirName}]"); }
                else           { disabled++; log.Information($"[ModSnapshot]   ❌ Disabled [{mod.DirName}]"); }

                if (mod.Enabled)
                {
                    penumbra.TrySetModPriority(collection.Value.Id, mod.DirName, mod.Priority, mod.ModName);
                    foreach (var (group, values) in mod.Settings)
                    {
                        if (values.Count == 1)
                            penumbra.TrySetModSetting(collection.Value.Id, mod.DirName, group, values[0], mod.ModName);
                        else if (values.Count > 1)
                            penumbra.TrySetModSettings(collection.Value.Id, mod.DirName, group, values, mod.ModName);
                    }
                }
            }
            else if (ec == 1)
                unchanged++;
            else
            {
                errors++;
                log.Warning($"[ModSnapshot]   ⚠ Failed [{mod.DirName}]: ec={ec}");
            }
        }

        log.Information($"[ModSnapshot] Done: {enabled} enabled, {disabled} disabled, {unchanged} unchanged, {errors} errors");
    }
}
