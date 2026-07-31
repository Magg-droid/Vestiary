using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

namespace Wardrobe.Services;

public class PenumbraService
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog log;
    private readonly IDataManager dataManager;

    private readonly ICallGateSubscriber<IReadOnlyList<(string ModDirectory, IReadOnlyDictionary<string, object?> ChangedItems)>>
        getChangedItemAdapterListSubscriber;

    private readonly ICallGateSubscriber<Dictionary<string, string>>
        getModListSubscriber;

    private readonly ICallGateSubscriber<Guid, string, string, bool, (int, (bool, int, Dictionary<string, List<string>>, bool)?)>
        getCurrentModSettingsSubscriber;

    private readonly ICallGateSubscriber<string, string, Dictionary<string, object?>>
        getChangedItemsSubscriber;

    private readonly ICallGateSubscriber<int, (bool, bool, (Guid Id, string Name))>
        getCollectionForObjectSubscriber;

    private readonly ICallGateSubscriber<byte, (Guid Id, string Name)?>
        getCollectionSubscriber;

    // Restore IPCs
    private readonly ICallGateSubscriber<Guid, string, string, bool, int>
        trySetModSubscriber;

    private readonly ICallGateSubscriber<Guid, string, string, int, int>
        trySetModPrioritySubscriber;

    private readonly ICallGateSubscriber<Guid, string, string, string, string, int>
        trySetModSettingSubscriber;

    private readonly ICallGateSubscriber<Guid, string, string, string, IReadOnlyList<string>, int>
        trySetModSettingsSubscriber;

    public PenumbraService(IDalamudPluginInterface pluginInterface, IPluginLog log, IDataManager dataManager)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
        this.dataManager = dataManager;

        getChangedItemAdapterListSubscriber = pluginInterface
            .GetIpcSubscriber<IReadOnlyList<(string ModDirectory, IReadOnlyDictionary<string, object?> ChangedItems)>>(
                "Penumbra.GetChangedItemAdapterList");

        getModListSubscriber = pluginInterface
            .GetIpcSubscriber<Dictionary<string, string>>("Penumbra.GetModList");

        getCurrentModSettingsSubscriber = pluginInterface
            .GetIpcSubscriber<Guid, string, string, bool, (int, (bool, int, Dictionary<string, List<string>>, bool)?)>(
                "Penumbra.GetCurrentModSettings.V5");

        getChangedItemsSubscriber = pluginInterface
            .GetIpcSubscriber<string, string, Dictionary<string, object?>>("Penumbra.GetChangedItems.V5");

        getCollectionForObjectSubscriber = pluginInterface
            .GetIpcSubscriber<int, (bool, bool, (Guid Id, string Name))>("Penumbra.GetCollectionForObject.V5");

        getCollectionSubscriber = pluginInterface
            .GetIpcSubscriber<byte, (Guid Id, string Name)?>("Penumbra.GetCollection");

        trySetModSubscriber = pluginInterface
            .GetIpcSubscriber<Guid, string, string, bool, int>("Penumbra.TrySetMod.V5");

        trySetModPrioritySubscriber = pluginInterface
            .GetIpcSubscriber<Guid, string, string, int, int>("Penumbra.TrySetModPriority.V5");

        trySetModSettingSubscriber = pluginInterface
            .GetIpcSubscriber<Guid, string, string, string, string, int>("Penumbra.TrySetModSetting.V5");

        trySetModSettingsSubscriber = pluginInterface
            .GetIpcSubscriber<Guid, string, string, string, IReadOnlyList<string>, int>("Penumbra.TrySetModSettings.V5");
    }

    public bool IsAvailable()
    {
        try { var _ = pluginInterface.GetIpcSubscriber<(int, int)>("Penumbra.ApiVersions").InvokeFunc(); return true; }
        catch { return false; }
    }

    public Dictionary<string, string> GetModList()
    {
        try { return getModListSubscriber.InvokeFunc(); }
        catch (Exception ex) { log.Error($"[ModSnapshot] GetModList failed: {ex.Message}"); return new(); }
    }

    public IReadOnlyList<(string ModDirectory, IReadOnlyDictionary<string, object?> ChangedItems)> GetAllModChangedItems()
    {
        try { return getChangedItemAdapterListSubscriber.InvokeFunc(); }
        catch (Exception ex) { log.Error($"[ModSnapshot] GetChangedItemAdapterList failed: {ex.Message}"); return Array.Empty<(string, IReadOnlyDictionary<string, object?>)>(); }
    }

    public Dictionary<string, object?> GetModChangedItems(string directoryName, string modName)
    {
        try { return getChangedItemsSubscriber.InvokeFunc(directoryName, modName); }
        catch (Exception ex) { log.Error($"[ModSnapshot] GetChangedItems failed for [{directoryName}]: {ex.Message}"); return new(); }
    }

    public (bool Enabled, int Priority, Dictionary<string, List<string>> Settings)? GetModSettings(
        Guid collectionId, string directoryName, string modName)
    {
        try
        {
            var (ec, result) = getCurrentModSettingsSubscriber.InvokeFunc(collectionId, directoryName, modName, false);
            if (ec != 0 || result == null) return null;
            return (result.Value.Item1, result.Value.Item2, result.Value.Item3);
        }
        catch (Exception ex) { log.Error($"[ModSnapshot] GetCurrentModSettings failed for [{directoryName}]: {ex.Message}"); return null; }
    }

    public (Guid Id, string Name)? GetPlayerCollection()
    {
        try
        {
            var (valid, _, (id, name)) = getCollectionForObjectSubscriber.InvokeFunc(0);
            return valid ? (id, name) : null;
        }
        catch (Exception ex) { log.Error($"[ModSnapshot] GetCollectionForObject failed: {ex.Message}"); return null; }
    }

    // ── Restore ─────────────────────────────────────────────────────

    public int TrySetMod(Guid collectionId, string dirName, bool enabled, string modName = "")
    {
        try { return trySetModSubscriber.InvokeFunc(collectionId, dirName, modName, enabled); }
        catch (Exception ex) { log.Error($"[ModSnapshot] TrySetMod failed for [{dirName}]: {ex.Message}"); return -1; }
    }

    public int TrySetModPriority(Guid collectionId, string dirName, int priority, string modName = "")
    {
        try { return trySetModPrioritySubscriber.InvokeFunc(collectionId, dirName, modName, priority); }
        catch (Exception ex) { log.Error($"[ModSnapshot] TrySetModPriority failed for [{dirName}]: {ex.Message}"); return -1; }
    }

    public int TrySetModSetting(Guid collectionId, string dirName, string optionGroup, string optionValue, string modName = "")
    {
        try { return trySetModSettingSubscriber.InvokeFunc(collectionId, dirName, modName, optionGroup, optionValue); }
        catch (Exception ex) { log.Error($"[ModSnapshot] TrySetModSetting failed for [{dirName}]: {ex.Message}"); return -1; }
    }

    public int TrySetModSettings(Guid collectionId, string dirName, string optionGroup, IReadOnlyList<string> optionValues, string modName = "")
    {
        try { return trySetModSettingsSubscriber.InvokeFunc(collectionId, dirName, modName, optionGroup, optionValues); }
        catch (Exception ex) { log.Error($"[ModSnapshot] TrySetModSettings failed for [{dirName}]: {ex.Message}"); return -1; }
    }

    private const uint MaxValidItemId = 1000000;

    /// <summary>
    /// Convert equipment ItemIds to item names using game data.
    /// Skips invalid IDs and "The Emperor's New" items.
    /// </summary>
    public HashSet<string> GetDesignItemNames(Dictionary<string, uint> equipment)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var itemSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Item>();
        foreach (var (_, itemId) in equipment)
        {
            if (itemId == 0 || itemId > MaxValidItemId) continue;
            var name = itemSheet.GetRow(itemId).Name.ToString();
            if (string.IsNullOrEmpty(name)) continue;
            if (name.StartsWith("The Emperor's New", StringComparison.OrdinalIgnoreCase)) continue;
            names.Add(name);
        }
        return names;
    }

    // ── Console log (for debugging) ──────────────────────────────────

    public void LogModsForDesign(Guid designId, GlamourerService glamourer)
    {
        if (!IsAvailable()) { log.Information("[ModSnapshot] Penumbra is not available."); return; }
        var collection = GetPlayerCollection();
        if (collection == null) { log.Information("[ModSnapshot] No Penumbra collection assigned to player."); return; }

        var equipment = glamourer.GetDesignEquipment(designId);
        var itemNames = GetDesignItemNames(equipment);

        log.Information($"[ModSnapshot] ========================================");
        log.Information($"[ModSnapshot] Design: {designId}");
        log.Information($"[ModSnapshot] Items: {string.Join(" | ", itemNames)}");
        log.Information($"[ModSnapshot] Collection: {collection.Value.Name}");
        log.Information($"[ModSnapshot] ========================================");

        var modList = GetModList();
        int enabledCount = 0, disabledCount = 0, matchingCount = 0;

        foreach (var (dir, modName) in modList)
        {
            var changedItems = GetModChangedItems(dir, modName);
            if (!changedItems.Keys.Any(key => itemNames.Contains(key))) continue;

            matchingCount++;
            var settings = GetModSettings(collection.Value.Id, dir, modName);
            bool enabled = settings?.Enabled ?? false;
            if (enabled) enabledCount++; else disabledCount++;
            log.Information($"[ModSnapshot]   {(enabled ? "✅" : "❌")}  [{dir}]  \"{modName}\"");
        }

        log.Information($"[ModSnapshot] ========================================");
        log.Information($"[ModSnapshot] Matching: {matchingCount} ({enabledCount} enabled, {disabledCount} disabled)");
        log.Information($"[ModSnapshot] ========================================");
    }
}
