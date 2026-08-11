using System;
using System.Collections.Generic;
using System.Linq;

namespace Vestiary.Services;

/// <summary>
/// Manages favorite designs. Favorites auto-appear in the "Favorites" collection.
/// </summary>
public class FavoriteService
{
    private readonly Configuration configuration;
    private readonly CollectionService collectionService;

    public FavoriteService(Configuration configuration, CollectionService collectionService)
    {
        this.configuration = configuration;
        this.collectionService = collectionService;
    }

    public bool IsFavorite(Guid designId) =>
        configuration.FavoriteDesignIds.Contains(designId);

    public void Toggle(Guid designId)
    {
        if (IsFavorite(designId))
        {
            configuration.FavoriteDesignIds.Remove(designId);
            // Remove tab when no favourites left
            if (configuration.FavoriteDesignIds.Count == 0)
            {
                var fav = configuration.Collections.FirstOrDefault(c => c.Name == "Favorites");
                if (fav != null)
                {
                    configuration.Collections.Remove(fav);
                }
            }
        }
        else
        {
            GetFavoritesCollectionId(); // ensure collection exists on first favourite
            configuration.FavoriteDesignIds.Add(designId);
        }
        configuration.Save();
    }

    public Dictionary<Guid, T> GetFavorites<T>(Dictionary<Guid, T> allDesigns) =>
        allDesigns.Where(d => IsFavorite(d.Key)).ToDictionary(d => d.Key, d => d.Value);

    /// <summary>
    /// Gets all favorited designs across all collections.
    /// </summary>
    public Dictionary<Guid, (string, string, uint, bool)> GetFavoritesFromAllCollections(
        Func<Guid, Dictionary<Guid, (string, string, uint, bool)>> getDesignsForCollection)
    {
        var all = new Dictionary<Guid, (string, string, uint, bool)>();
        foreach (var col in configuration.Collections.Where(c => c.Name != "Favorites"))
            foreach (var d in getDesignsForCollection(col.Id))
                all[d.Key] = d.Value;
        return GetFavorites(all);
    }

    /// <summary>
    /// Gets or creates the "Favorites" collection. Returns its ID.
    /// </summary>
    public Guid GetFavoritesCollectionId()
    {
        var existing = configuration.Collections.FirstOrDefault(c => c.Name == "Favorites");
        if (existing != null)
            return existing.Id;

        var fav = new Models.Collection
        {
            Id = Guid.NewGuid(),
            Name = "Favorites",
            FolderPaths = new List<string>(),
            Order = int.MaxValue // always last
        };
        configuration.Collections.Add(fav);
        configuration.Save();
        return fav.Id;
    }
}
