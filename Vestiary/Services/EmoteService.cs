using System;
using System.Collections.Generic;
using System.Linq;
using Vestiary;
using Vestiary.Models;

namespace Vestiary.Services;

public class EmoteService
{
    private readonly Configuration configuration;
    private readonly PenumbraService penumbra;

    public EmoteService(Configuration configuration, PenumbraService penumbra)
    {
        this.configuration = configuration;
        this.penumbra = penumbra;
        EnsureCollectionsInitialized();
    }

    public List<EmoteCard> GetCards() => configuration.EmoteCards;

    public List<EmoteCard> GetCardsByCollection(Guid collectionId)
    {
        if (collectionId == Guid.Empty)
            return configuration.EmoteCards;

        return configuration.EmoteCards
            .Where(c => c.CollectionId == collectionId)
            .ToList();
    }

    public List<EmoteCollection> GetCollections() =>
        configuration.EmoteCollections
            .OrderBy(c => c.Order)
            .ToList();

    public Guid GetDefaultCollectionId()
    {
        EnsureCollectionsInitialized();
        return configuration.EmoteCollections.OrderBy(c => c.Order).First().Id;
    }

    public EmoteCollection? CreateCollection(string name)
    {
        EnsureCollectionsInitialized();
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (configuration.EmoteCollections.Any(c =>
                string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            return null;

        var collection = new EmoteCollection(trimmed, configuration.EmoteCollections.Count);
        configuration.EmoteCollections.Add(collection);
        configuration.Save();
        return collection;
    }

    public bool RenameCollection(Guid collectionId, string name)
    {
        EnsureCollectionsInitialized();
        var collection = configuration.EmoteCollections.FirstOrDefault(c => c.Id == collectionId);
        if (collection == null)
            return false;

        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return false;

        bool duplicate = configuration.EmoteCollections.Any(c =>
            c.Id != collectionId && string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
            return false;

        collection.Name = trimmed;
        configuration.Save();
        return true;
    }

    public bool DeleteCollection(Guid collectionId)
    {
        EnsureCollectionsInitialized();
        if (configuration.EmoteCollections.Count <= 1)
            return false;

        var collection = configuration.EmoteCollections.FirstOrDefault(c => c.Id == collectionId);
        if (collection == null)
            return false;

        var fallback = configuration.EmoteCollections
            .Where(c => c.Id != collectionId)
            .OrderBy(c => c.Order)
            .First();

        foreach (var card in configuration.EmoteCards.Where(c => c.CollectionId == collectionId))
            card.CollectionId = fallback.Id;

        configuration.EmoteCollections.Remove(collection);
        NormalizeCollectionOrder();
        configuration.Save();
        return true;
    }

    public bool SetCardCollection(Guid cardId, Guid collectionId)
    {
        EnsureCollectionsInitialized();
        var card = configuration.EmoteCards.FirstOrDefault(c => c.Id == cardId);
        if (card == null)
            return false;
        if (!configuration.EmoteCollections.Any(c => c.Id == collectionId))
            return false;

        card.CollectionId = collectionId;
        configuration.Save();
        return true;
    }

    public EmoteCard CreateCard(string name, string emoteName, Guid collectionId = default)
    {
        EnsureCollectionsInitialized();
        if (collectionId == Guid.Empty || !configuration.EmoteCollections.Any(c => c.Id == collectionId))
            collectionId = GetDefaultCollectionId();

        var card = new EmoteCard { Name = name, EmoteName = emoteName, CollectionId = collectionId };
        configuration.EmoteCards.Add(card);
        configuration.Save();
        return card;
    }

    public void DeleteCard(Guid id)
    {
        configuration.EmoteCards.RemoveAll(c => c.Id == id);
        configuration.Save();
    }

    public void UpdateCard(Guid id, string name, string? thumbnailPath)
    {
        var card = configuration.EmoteCards.FirstOrDefault(c => c.Id == id);
        if (card == null) return;
        card.Name = name;
        card.ThumbnailPath = thumbnailPath;
        configuration.Save();
    }

    public void CaptureState(Guid cardId)
    {
        var card = configuration.EmoteCards.FirstOrDefault(c => c.Id == cardId);
        if (card == null || string.IsNullOrEmpty(card.EmoteName)) return;

        var collection = penumbra.GetPlayerCollection();
        if (collection == null) return;

        // Find mods that touch this emote
        var emoteKey1 = $"Emote: {card.EmoteName}";
        var emoteKey2 = $"Action: {card.EmoteName}";
        var allMods = penumbra.GetAllModChangedItems();
        var changedByDir = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in allMods)
            changedByDir[m.ModDirectory] = m.ChangedItems;

        var allSettings = penumbra.GetAllModSettings(collection.Value.Id);
        var modList = penumbra.GetModList();

        card.Mods.Clear();
        int enabled = 0, disabled = 0;

        foreach (var (dir, modName) in modList)
        {
            if (!changedByDir.TryGetValue(dir, out var changed)) continue;
            if (!changed.Keys.Contains(emoteKey1) && !changed.Keys.Contains(emoteKey2)) continue;

            int modPriority = 0;
            Dictionary<string, List<string>> modSettings = new();
            if (allSettings != null && allSettings.TryGetValue(dir, out var s))
            {
                modPriority = s.Priority;
                modSettings = s.Settings;
            }

            bool isCurrent = (allSettings != null && allSettings.TryGetValue(dir, out var curr) && curr.Enabled);
            if (isCurrent) enabled++; else disabled++;

            card.Mods.Add(new ModEntry
            {
                DirName = dir,
                ModName = modName,
                Enabled = isCurrent,
                Priority = modPriority,
                Settings = modSettings
            });
        }

        configuration.Save();
        Plugin.Log.Information($"[Emotes] 💾 {card.Name} — {card.Mods.Count} mods ({enabled} on, {disabled} off)");
    }

    public void RestoreState(Guid cardId)
    {
        var card = configuration.EmoteCards.FirstOrDefault(c => c.Id == cardId);
        if (card == null || card.Mods.Count == 0) return;

        var collection = penumbra.GetPlayerCollection();
        if (collection == null) return;

        int enabled = 0, disabled = 0, unchanged = 0, errors = 0;

        foreach (var mod in card.Mods)
        {
            var ec = penumbra.TrySetMod(collection.Value.Id, mod.DirName, mod.Enabled, mod.ModName);
            if (ec == 0) { if (mod.Enabled) enabled++; else disabled++; }
            else if (ec == 1) unchanged++;
            else { errors++; continue; }

            // Always apply settings (even if enabled state didn't change)
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

        Plugin.Log.Information($"[Emotes] 🎭 {card.Name} — {enabled} on, {disabled} off, {unchanged} unchanged, {errors} errors");
    }

    public bool HasState(Guid cardId) =>
        configuration.EmoteCards.Any(c => c.Id == cardId && c.Mods.Count > 0);

    private void EnsureCollectionsInitialized()
    {
        configuration.EmoteCollections ??= new();

        bool changed = false;
        if (configuration.EmoteCollections.Count == 0)
        {
            configuration.EmoteCollections.Add(new EmoteCollection(Strings.EmoteDefaultCollectionName, 0));
            changed = true;
        }

        NormalizeCollectionOrder();
        var defaultCollectionId = configuration.EmoteCollections.OrderBy(c => c.Order).First().Id;
        foreach (var card in configuration.EmoteCards.Where(c => c.CollectionId == Guid.Empty))
        {
            card.CollectionId = defaultCollectionId;
            changed = true;
        }

        if (changed)
            configuration.Save();
    }

    private void NormalizeCollectionOrder()
    {
        var sorted = configuration.EmoteCollections.OrderBy(c => c.Order).ToList();
        for (int i = 0; i < sorted.Count; i++)
            sorted[i].Order = i;
    }
}
