using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        if (!configuration.EnableSaveMods) return;
        var sw = Stopwatch.StartNew();
        var collection = penumbra.GetPlayerCollection();
        if (collection == null) return;

        var equipment = glamourer.GetDesignEquipment(designId);
        var itemNames = penumbra.GetDesignItemNames(equipment);
        if (itemNames.Count == 0) return;

        var modList = penumbra.GetModList();
        var snapshot = new ModSnapshot
        {
            DesignId = designId,
            ItemNames = itemNames.ToList()
        };
        int enabled = 0, disabled = 0;

        foreach (var (dir, modName) in modList)
        {
            var changedItems = penumbra.GetModChangedItems(dir, modName);
            if (!changedItems.Keys.Any(key => itemNames.Contains(key))) continue;

            var settings = penumbra.GetModSettings(collection.Value.Id, dir, modName);
            var modEnabled = settings?.Enabled ?? false;
            if (modEnabled) enabled++; else disabled++;

            snapshot.Mods.Add(new ModEntry
            {
                DirName = dir,
                ModName = modName,
                Enabled = modEnabled,
                Priority = settings?.Priority ?? 0,
                Settings = settings?.Settings ?? new()
            });
        }

        configuration.ModSnapshots ??= new();
        configuration.ModSnapshots.RemoveAll(s => s.DesignId == designId);
        configuration.ModSnapshots.Add(snapshot);
        configuration.Save();

        Plugin.Log.Information($"[SaveMods] 💾 {snapshot.Mods.Count} mods saved ({enabled} enabled, {disabled} disabled) ({sw.ElapsedMilliseconds}ms)");
    }

    public bool HasSnapshot(Guid designId) =>
        configuration.ModSnapshots?.Any(s => s.DesignId == designId) ?? false;

    public ModSnapshot? GetSnapshot(Guid designId) =>
        configuration.ModSnapshots?.FirstOrDefault(s => s.DesignId == designId);

    public void ClearSnapshot(Guid designId)
    {
        configuration.ModSnapshots?.RemoveAll(s => s.DesignId == designId);
        configuration.Save();
        Plugin.Log.Information("[SaveMods] Mods cleared.");
    }

    public void RestoreState(Guid designId)
    {
        if (!configuration.EnableSaveMods) return;
        var snapshot = GetSnapshot(designId);
        if (snapshot == null) return;

        var sw = Stopwatch.StartNew();

        var collection = penumbra.GetPlayerCollection();
        if (collection == null) return;

        int enabled = 0, disabled = 0, unchanged = 0, errors = 0;

        if (snapshot.ItemNames.Count > 0)
        {
            var snapshotMods = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in snapshot.Mods)
                snapshotMods[m.DirName] = m;

            var itemNames = new HashSet<string>(snapshot.ItemNames, StringComparer.OrdinalIgnoreCase);

            // Use bulk IPC for changed items
            var allMods = penumbra.GetAllModChangedItems();
            var matchingMods = allMods
                .Where(m => m.ChangedItems.Keys.Any(key => itemNames.Contains(key)))
                .Select(m => m.ModDirectory)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var modList = penumbra.GetModList();

            foreach (var (dir, modName) in modList)
            {
                if (!matchingMods.Contains(dir)) continue;

                if (snapshotMods.TryGetValue(dir, out var entry))
                {
                    var ec = penumbra.TrySetMod(collection.Value.Id, dir, entry.Enabled, modName);
                    if (ec == 0)
                    {
                        if (entry.Enabled) enabled++; else disabled++;
                        if (entry.Enabled)
                        {
                            penumbra.TrySetModPriority(collection.Value.Id, dir, entry.Priority, modName);
                            foreach (var (group, values) in entry.Settings)
                            {
                                if (values.Count == 1)
                                    penumbra.TrySetModSetting(collection.Value.Id, dir, group, values[0], modName);
                                else if (values.Count > 1)
                                    penumbra.TrySetModSettings(collection.Value.Id, dir, group, values, modName);
                            }
                        }
                    }
                    else if (ec == 1) unchanged++;
                    else errors++;
                }
                else
                {
                    var ec = penumbra.TrySetMod(collection.Value.Id, dir, false, modName);
                    if (ec == 0)
                    {
                        disabled++;
                        Plugin.Log.Information($"[SaveMods]   🆕 Disabled new mod [{dir}]");
                    }
                    else if (ec == 1) unchanged++;
                    else errors++;
                }
            }
        }
        else
        {
            // Legacy: restore from snapshot mods directly
            foreach (var mod in snapshot.Mods)
            {
                var ec = penumbra.TrySetMod(collection.Value.Id, mod.DirName, mod.Enabled, mod.ModName);
                if (ec == 0)
                {
                    if (mod.Enabled) enabled++; else disabled++;
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
                else if (ec == 1) unchanged++;
                else errors++;
            }
        }

        Plugin.Log.Information($"[SaveMods] 🔄 Restored — {enabled} enabled, {disabled} disabled, {unchanged} unchanged, {errors} errors ({sw.ElapsedMilliseconds}ms)");
    }
}
