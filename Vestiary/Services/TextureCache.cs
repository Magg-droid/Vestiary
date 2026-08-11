using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace Vestiary.Services;

/// <summary>
/// Caches textures loaded from file paths to avoid reloading every frame.
/// </summary>
public class TextureCache : IDisposable
{
    private readonly ITextureProvider textureProvider;
    private readonly Dictionary<string, ISharedImmediateTexture> textureCache = new();

    public TextureCache(ITextureProvider textureProvider)
    {
        this.textureProvider = textureProvider;
    }

    /// <summary>
    /// Get a cached texture or load it from file if not cached.
    /// </summary>
    public ISharedImmediateTexture? GetOrLoadTexture(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        if (textureCache.TryGetValue(filePath, out var cachedTexture))
            return cachedTexture;

        try
        {
            // Load texture from file using TextureProvider
            var textureFile = new FileInfo(filePath);
            var texture = textureProvider.GetFromFile(textureFile);
            
            if (texture != null)
            {
                textureCache[filePath] = texture;
                return texture;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, $"Failed to load texture from {filePath}");
        }

        return null;
    }

    /// <summary>
    /// Remove a texture from cache (e.g., when file is deleted).
    /// </summary>
    public void InvalidateTexture(string filePath)
    {
        if (textureCache.TryGetValue(filePath, out var texture))
        {
            textureCache.Remove(filePath);
        }
    }

    /// <summary>
    /// Clear all cached textures.
    /// </summary>
    public void ClearAll()
    {
        textureCache.Clear();
    }

    public void Dispose()
    {
        textureCache.Clear();
    }
}
