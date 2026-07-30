using System;
using System.Collections.Generic;
using System.Linq;

namespace Wardrobe.Services;

/// <summary>
/// Manages hidden design state. Modifies only Wardrobe configuration — never touches Glamourer.
/// </summary>
public class HiddenDesignService
{
    private readonly Configuration configuration;

    public HiddenDesignService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public bool IsHidden(Guid designId) =>
        configuration.HiddenDesignIds.Contains(designId);

    public void HideDesign(Guid designId)
    {
        if (!configuration.HiddenDesignIds.Contains(designId))
        {
            configuration.HiddenDesignIds.Add(designId);
            configuration.Save();
        }
    }

    public void ShowDesign(Guid designId)
    {
        if (configuration.HiddenDesignIds.Remove(designId))
            configuration.Save();
    }

    public void ToggleHidden(Guid designId)
    {
        if (IsHidden(designId))
            ShowDesign(designId);
        else
            HideDesign(designId);
    }

    public IReadOnlyList<Guid> GetHiddenDesignIds() =>
        configuration.HiddenDesignIds;

    public Dictionary<Guid, T> GetVisibleDesigns<T>(Dictionary<Guid, T> allDesigns) =>
        allDesigns
            .Where(d => !configuration.HiddenDesignIds.Contains(d.Key))
            .ToDictionary(d => d.Key, d => d.Value);

    public Dictionary<Guid, T> GetHiddenDesigns<T>(Dictionary<Guid, T> allDesigns) =>
        allDesigns
            .Where(d => configuration.HiddenDesignIds.Contains(d.Key))
            .ToDictionary(d => d.Key, d => d.Value);

    public bool ShowHidden
    {
        get => configuration.ShowHidden;
        set
        {
            configuration.ShowHidden = value;
            configuration.Save();
        }
    }
}
