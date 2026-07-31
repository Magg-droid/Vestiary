using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Wardrobe.Services;

public class GlamourerService
{
    private readonly ICallGateSubscriber<Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>> designListSubscriber;
    private readonly ICallGateSubscriber<Guid, string?> designBase64Subscriber;
    private readonly ICallGateSubscriber<Guid, JObject?> designJObjectSubscriber;
    private readonly ICallGateSubscriber<Guid, int, uint, ulong, int> applyDesignSubscriber;
    private readonly ICallGateSubscriber<Guid, int> deleteDesignSubscriber;
    private readonly IPluginLog log;

    public GlamourerService(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog)
    {
        log = pluginLog;
        
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

    public Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> GetDesignList()
    {
        return designListSubscriber.InvokeFunc();
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
            
            if (result == 0)
            {
                var applyType = equipmentOnly ? "(equipment only)" : "(full design)";
                log.Information($"Successfully applied design: {designId} {applyType}");
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