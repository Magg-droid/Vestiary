using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Wardrobe.Services;

public class GlamourerService
{
    private readonly ICallGateSubscriber<Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>> designListSubscriber;
    private readonly ICallGateSubscriber<Guid, string?> designBase64Subscriber;

    public GlamourerService(IDalamudPluginInterface pluginInterface)
    {
        designListSubscriber = pluginInterface.GetIpcSubscriber<Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>>(
            "Glamourer.GetDesignListExtended");

        designBase64Subscriber = pluginInterface.GetIpcSubscriber<Guid, string?>(
            "Glamourer.GetDesignBase64");
    }

    public Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> GetDesignList()
    {
        return designListSubscriber.InvokeFunc();
    }

    public string? GetDesignBase64(Guid designId)
    {
        return designBase64Subscriber.InvokeFunc(designId);
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
}