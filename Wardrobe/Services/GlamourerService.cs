using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;

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
}