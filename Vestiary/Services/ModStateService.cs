using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Vestiary.Models;

namespace Vestiary.Services;

public class ModStateService
{
    private readonly Configuration configuration;
    private readonly PenumbraService penumbra;
    private readonly GlamourerService glamourer;
    private readonly string snapshotsPath;
    private List<ModSnapshot> snapshots = new();

    public ModStateService(Configuration configuration, PenumbraService penumbra, GlamourerService glamourer, string pluginConfigDir)
    {
        this.configuration = configuration;
        this.penumbra = penumbra;
        this.glamourer = glamourer;
        snapshotsPath = Path.Combine(pluginConfigDir, "mod-snapshots.json");
        LoadSnapshots();
    }

    public void ImportSnapshots(List<ModSnapshot>? incoming)
    {
        snapshots = incoming ?? new();
        SaveSnapshots();
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
        var allMods = penumbra.GetAllModChangedItems();
        var allSettings = penumbra.GetAllModSettings(collection.Value.Id);

        var changedItemsByDir = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in allMods)
            changedItemsByDir[m.ModDirectory] = m.ChangedItems;

        var snapshot = new ModSnapshot
        {
            DesignId = designId,
            ItemNames = itemNames.ToList()
        };
        int enabled = 0, disabled = 0;

        foreach (var (dir, modName) in modList)
        {
            if (!changedItemsByDir.TryGetValue(dir, out var changedItems)) continue;
            if (!changedItems.Keys.Any(key => itemNames.Contains(key))) continue;

            var modEnabled = false;
            int modPriority = 0;
            Dictionary<string, List<string>> modSettings = new();
            if (allSettings != null && allSettings.TryGetValue(dir, out var s))
            {
                modEnabled = s.Enabled;
                modPriority = s.Priority;
                modSettings = s.Settings;
            }
            if (modEnabled) enabled++; else disabled++;

            snapshot.Mods.Add(new ModEntry
            {
                DirName = dir,
                ModName = modName,
                Enabled = modEnabled,
                Priority = modPriority,
                Settings = modSettings
            });
        }

        snapshots.RemoveAll(s => s.DesignId == designId);
        snapshots.Add(snapshot);
        SaveSnapshots();

        Plugin.Log.Information($"[SaveMods] 💾 {snapshot.Mods.Count} mods saved ({enabled} enabled, {disabled} disabled) @ {collection.Value.Name} ({sw.ElapsedMilliseconds}ms)");
    }

    public bool HasSnapshot(Guid designId) =>
        snapshots.Any(s => s.DesignId == designId);

    public ModSnapshot? GetSnapshot(Guid designId) =>
        snapshots.FirstOrDefault(s => s.DesignId == designId);

    public void ClearSnapshot(Guid designId)
    {
        snapshots.RemoveAll(s => s.DesignId == designId);
        SaveSnapshots();
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
        int desiredOn = 0, desiredOff = 0;
        bool snapshotChanged = false;

        if (snapshot.ItemNames.Count > 0)
        {
            var snapshotMods = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in snapshot.Mods)
                snapshotMods[m.DirName] = m;

            var itemNames = new HashSet<string>(snapshot.ItemNames, StringComparer.OrdinalIgnoreCase);

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
                    if (entry.Enabled) desiredOn++; else desiredOff++;
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
                    else if (ec == 3) { snapshot.Mods.Remove(entry); snapshotChanged = true; Plugin.Log.Information($"[SaveMods]   🗑 Removed missing mod [{dir}]"); }
                    else errors++;
                }
                else
                {
                    var ec = penumbra.TrySetMod(collection.Value.Id, dir, false, modName);
                    if (ec == 0)
                    {
                        disabled++;
                        desiredOff++;
                        Plugin.Log.Information($"[SaveMods]   🆕 Disabled new mod [{dir}]");
                    }
                    else if (ec == 1) unchanged++;
                    else errors++;
                }
            }
        }
        else
        {
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
                else if (ec == 3) { snapshot.Mods.Remove(mod); snapshotChanged = true; Plugin.Log.Information($"[SaveMods]   🗑 Removed missing mod [{mod.DirName}]"); }
                else errors++;
            }
        }

        if (snapshotChanged)
            SaveSnapshots();

        Plugin.Log.Information($"[SaveMods] 🔄 Restored — {desiredOn} on, {desiredOff} off, {unchanged} unchanged, {errors} errors @ {collection.Value.Name} ({sw.ElapsedMilliseconds}ms)");
    }

    private void LoadSnapshots()
    {
        try
        {
            if (File.Exists(snapshotsPath))
            {
                var json = File.ReadAllText(snapshotsPath);
                snapshots = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ModSnapshot>>(json) ?? new();
                return;
            }

            // One-time extraction from old Vestiary.json (users upgrading from before split)
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher", "pluginConfigs", "Vestiary.json");

            if (File.Exists(configPath))
            {
                var configJson = File.ReadAllText(configPath);
                var legacy = Newtonsoft.Json.JsonConvert.DeserializeObject<LegacyConfig>(configJson);
                if (legacy?.ModSnapshots?.Count > 0)
                {
                    snapshots = legacy.ModSnapshots;
                    SaveSnapshots();
                    configuration.Save(); // re-save Vestiary.json without ModSnapshots
                    Plugin.Log.Information($"[ModState] Extracted {snapshots.Count} snapshots to dedicated file");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[ModState] Failed to load snapshots");
        }
    }

    private void SaveSnapshots()
    {
        try
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
                Formatting = Newtonsoft.Json.Formatting.None
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshots, settings);
            File.WriteAllText(snapshotsPath, json);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[ModState] Failed to save snapshots");
        }
    }

    /// <summary>Minimal DTO for extracting ModSnapshots from old Vestiary.json.</summary>
    [Serializable]
    private class LegacyConfig
    {
        public List<ModSnapshot>? ModSnapshots { get; set; }
    }
}
