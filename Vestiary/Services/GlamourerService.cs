using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vestiary.Services;

public class GlamourerService
{
    private readonly ICallGateSubscriber<Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>> designListSubscriber;
    private readonly ICallGateSubscriber<Guid, string?> designBase64Subscriber;
    private readonly ICallGateSubscriber<Guid, JObject?> designJObjectSubscriber;
    private readonly ICallGateSubscriber<Guid, int, uint, ulong, int> applyDesignSubscriber;
    private readonly ICallGateSubscriber<Guid, int> deleteDesignSubscriber;
    private readonly IPluginLog log;
    private readonly Configuration configuration;

    private Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>? cachedDesignList;
    private DateTime cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan DesignListCacheTtl = TimeSpan.FromSeconds(2);

    private readonly Dictionary<Guid, DateTime> designDateCache = new();

    public GlamourerService(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, Configuration configuration)
    {
        log = pluginLog;
        this.configuration = configuration;
        
        designListSubscriber = pluginInterface.GetIpcSubscriber<Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>>(
            "Glamourer.GetDesignListExtended");

        designBase64Subscriber = pluginInterface.GetIpcSubscriber<Guid, string?>(
            "Glamourer.GetDesignBase64");

        designJObjectSubscriber = pluginInterface.GetIpcSubscriber<Guid, JObject?>(
            "Glamourer.GetDesignJObject");
        
        applyDesignSubscriber = pluginInterface.GetIpcSubscriber<Guid, int, uint, ulong, int>(
            "Glamourer.ApplyDesign");
        
        deleteDesignSubscriber = pluginInterface.GetIpcSubscriber<Guid, int>(
            "Glamourer.DeleteDesign");
    }

    /// <summary>
    /// Get the full design list from Glamourer. Results are cached for 2 seconds
    /// to avoid redundant IPC calls every frame. The cache is invalidated immediately
    /// when a design is applied or deleted.
    /// </summary>
    public Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> GetDesignList()
    {
        if (cachedDesignList != null && DateTime.UtcNow < cacheExpiry)
            return cachedDesignList;

        var previous = cachedDesignList;
        cachedDesignList = designListSubscriber.InvokeFunc();
        cacheExpiry = DateTime.UtcNow + DesignListCacheTtl;

        // Designs were added or removed: drop cached dates so newly-created
        // designs get fresh timestamps and deleted ones don't linger in memory.
        if (previous != null && !KeysMatch(previous, cachedDesignList))
            designDateCache.Clear();

        return cachedDesignList;
    }

    private static bool KeysMatch<T>(Dictionary<Guid, T> a, Dictionary<Guid, T> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var key in a.Keys)
        {
            if (!b.ContainsKey(key))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Force the next GetDesignList call to fetch fresh data from Glamourer.
    /// </summary>
    private void InvalidateDesignListCache()
    {
        cachedDesignList = null;
        cacheExpiry = DateTime.MinValue;
        designDateCache.Clear();
    }

    public string? GetDesignBase64(Guid designId)
    {
        return designBase64Subscriber.InvokeFunc(designId);
    }

    /// <summary>
    /// Get a design as a parsed JObject from Glamourer.
    /// </summary>
    public JObject? GetDesignJObject(Guid designId)
    {
        try
        {
            return designJObjectSubscriber.InvokeFunc(designId);
        }
        catch (Exception ex)
        {
            log.Error($"[ModSnapshot] GetDesignJObject failed for {designId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get the last updated date for a design. Falls back to the creation date
    /// if no last-updated timestamp is available. Dates are fetched once per
    /// design and cached until a design is applied, deleted, or the design list
    /// changes (new/removed designs).
    /// </summary>
    public DateTime GetDesignLastEdit(Guid designId)
    {
        if (!designDateCache.TryGetValue(designId, out var date))
        {
            date = ReadLastEditFromDesign(designId);
            designDateCache[designId] = date;
        }

        return date;
    }

    private DateTime ReadLastEditFromDesign(Guid designId)
    {
        var design = GetDesignJObject(designId);
        if (design == null)
            return DateTime.MinValue;

        var lastEdit = ParseDesignDate(design["LastEdit"]);
        if (lastEdit != DateTime.MinValue)
            return lastEdit;

        return ParseDesignDate(design["CreationDate"]);
    }

    private static DateTime ParseDesignDate(JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return DateTime.MinValue;

        if (token.Type == JTokenType.Date)
            return token.ToObject<DateTime>();

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            var value = token.ToObject<long>();
            return value > 100_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime
                : DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;
        }

        if (token.Type == JTokenType.String)
        {
            var text = token.ToString();
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                return parsed;

            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
            {
                return unix > 100_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(unix).UtcDateTime
                    : DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            }
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// Extract equipment ItemIds from a Glamourer design JObject.
    /// Returns slot name → ItemId for non-empty slots.
    /// </summary>
    public Dictionary<string, uint> GetDesignEquipment(Guid designId)
    {
        var result = new Dictionary<string, uint>();
        var design = GetDesignJObject(designId);
        if (design == null)
            return result;

        var equipment = design["Equipment"] as JObject;
        if (equipment == null)
            return result;

        foreach (var prop in equipment.Properties())
        {
            var itemId = prop.Value["ItemId"]?.ToObject<uint>() ?? 0;
            if (itemId > 0)
                result[prop.Name] = itemId;
        }

        return result;
    }

    public List<string> GetUniqueFolderPaths()
    {
        var designs = GetDesignList();
        var paths = designs.Values
            .Select(d => d.FullPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path)
            .ToList();

        return paths;
    }

    /// <summary>
    /// Apply a design to the player character using Glamourer IPC.
    /// </summary>
    /// <param name="designId">The GUID of the design to apply</param>
    /// <param name="equipmentOnly">If true, apply only equipment (not customization). Default is false (apply full design)</param>
    /// <returns>Status code: 0=Success, 1=DesignNotFound, 2=ActorNotFound, 3=InvalidKey</returns>
    public int ApplyDesign(Guid designId, bool equipmentOnly = false)
    {
        try
        {
            // Flags: 0x01=Once, 0x02=Equipment, 0x04=Customization
            // Full design: Once | Equipment | Customization = 0x07
            // Equipment only: Once | Equipment = 0x03
            ulong designFlags = equipmentOnly ? 0x03uL : 0x07uL;
            
            // Apply to player (object index 0), key=0 (no locking)
            int result = applyDesignSubscriber.InvokeFunc(designId, 0, 0, designFlags);
            InvalidateDesignListCache();
            
            if (result == 0)
            {
                var applyType = equipmentOnly ? "(equipment only)" : "(full design)";
                log.Information($"Successfully applied design: {designId} {applyType}");

                configuration.LastAppliedAt[designId] = DateTime.UtcNow;
                configuration.Save();
            }
            else
            {
                log.Warning($"Failed to apply design {designId}. Status code: {result}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Error applying design {designId}");
            return -1; // Error status
        }
    }

    /// <summary>
    /// Delete a design from Glamourer using IPC.
    /// </summary>
    /// <param name="designId">The GUID of the design to delete</param>
    /// <returns>Status code: 0=Success, non-zero=Failure</returns>
    public int DeleteDesign(Guid designId)
    {
        try
        {
            int result = deleteDesignSubscriber.InvokeFunc(designId);
            InvalidateDesignListCache();
            
            if (result == 0)
            {
                log.Information($"Successfully deleted design: {designId}");
            }
            else
            {
                log.Warning($"Failed to delete design {designId}. Status code: {result}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Error deleting design {designId}");
            return -1; // Error status
        }
    }
}