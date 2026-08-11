using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace Vestiary.Services;

/// <summary>
/// Caches textures loaded from file paths to avoid reloading every frame.
/// Uses LRU eviction: when the cache exceeds <see cref="MaxCacheSize"/>, the least
/// recently accessed texture is evicted to keep GPU memory bounded.
/// </summary>
public class TextureCache : IDisposable
{
    private const int MaxCacheSize = 100;

    private readonly ITextureProvider textureProvider;
    private readonly Dictionary<string, Entry> cache = new();

    private struct Entry
    {
        public ISharedImmediateTexture Texture;
        public long LastAccessTick;
    }

    public TextureCache(ITextureProvider textureProvider)
    {
        this.textureProvider = textureProvider;
    }

    /// <summary>
    /// Get a cached texture or load it from file if not cached.
    /// Each call bumps the access timestamp so actively-displayed textures stay resident.
    /// </summary>
    public ISharedImmediateTexture? GetOrLoadTexture(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        if (cache.TryGetValue(filePath, out var entry))
        {
            entry.LastAccessTick = DateTime.UtcNow.Ticks;
            cache[filePath] = entry;
            return entry.Texture;
        }

        try
        {
            var textureFile = new FileInfo(filePath);
            var texture = textureProvider.GetFromFile(textureFile);

            if (texture != null)
            {
                EvictIfNeeded();
                cache[filePath] = new Entry
                {
                    Texture = texture,
                    LastAccessTick = DateTime.UtcNow.Ticks
                };
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
        cache.Remove(filePath);
    }

    /// <summary>
    /// Clear all cached textures.
    /// </summary>
    public void ClearAll()
    {
        cache.Clear();
    }

    public void Dispose()
    {
        cache.Clear();
    }

    private void EvictIfNeeded()
    {
        if (cache.Count < MaxCacheSize)
            return;

        // Remove the entry with the oldest access timestamp
        var oldest = cache.MinBy(kv => kv.Value.LastAccessTick);
        cache.Remove(oldest.Key);
    }
}
