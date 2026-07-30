using System;
using System.Collections.Generic;
using System.Linq;
using Wardrobe.Models;

namespace Wardrobe.Services;

public class DesignMetadataService
{
    private readonly Configuration configuration;
    private readonly GlamourerService glamourerService;

    public DesignMetadataService(Configuration configuration, GlamourerService glamourerService)
    {
        this.configuration = configuration;
        this.glamourerService = glamourerService;
    }

    /// <summary>
    /// Get metadata for a specific design, or null if not found.
    /// </summary>
    public DesignMetadata? GetMetadata(Guid designId)
    {
        return configuration.DesignMetadata.FirstOrDefault(dm => dm.DesignId == designId);
    }

    /// <summary>
    /// Create or update metadata for a design.
    /// </summary>
    public void UpsertMetadata(Guid designId, string nickname = "", string customImagePath = "")
    {
        var existing = GetMetadata(designId);
        if (existing != null)
        {
            existing.Nickname = nickname;
            existing.CustomImagePath = customImagePath;
        }
        else
        {
            var metadata = new DesignMetadata(designId, nickname, customImagePath);
            configuration.DesignMetadata.Add(metadata);
        }
        configuration.Save();
    }

    /// <summary>
    /// Delete metadata for a design.
    /// </summary>
    public void DeleteMetadata(Guid designId)
    {
        var metadata = GetMetadata(designId);
        if (metadata != null)
        {
            configuration.DesignMetadata.Remove(metadata);
            configuration.Save();
        }
    }

    /// <summary>
    /// Set nickname without touching the custom image path.
    /// </summary>
    public void SetNickname(Guid designId, string nickname)
    {
        var existing = GetMetadata(designId);
        UpsertMetadata(designId, nickname: nickname, customImagePath: existing?.CustomImagePath ?? "");
    }

    /// <summary>
    /// Set custom image path without touching the nickname.
    /// </summary>
    public void SetCustomImage(Guid designId, string path)
    {
        var existing = GetMetadata(designId);
        UpsertMetadata(designId, nickname: existing?.Nickname ?? "", customImagePath: path);
    }

    /// <summary>
    /// Get display name for a design: returns Nickname if set, otherwise Glamourer's DisplayName.
    /// </summary>
    public string GetDisplayName(Guid designId)
    {
        var metadata = GetMetadata(designId);
        if (!string.IsNullOrEmpty(metadata?.Nickname))
        {
            return metadata.Nickname;
        }

        // Fallback to Glamourer's DisplayName
        var designs = glamourerService.GetDesignList();
        if (designs.TryGetValue(designId, out var design))
        {
            return design.DisplayName;
        }

        return "Unknown Design";
    }
}
