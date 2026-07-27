using System;
using System.Collections.Generic;
using System.Linq;
using Wardrobe.Models;

namespace Wardrobe.Services;

public class CollectionService
{
    private readonly Configuration configuration;
    private readonly GlamourerService glamourerService;

    public CollectionService(Configuration configuration, GlamourerService glamourerService)
    {
        this.configuration = configuration;
        this.glamourerService = glamourerService;
    }

    /// <summary>
    /// Get all collections.
    /// </summary>
    public List<Collection> GetCollections()
    {
        return configuration.Collections;
    }

    /// <summary>
    /// Create a new collection.
    /// </summary>
    public Collection CreateCollection(string name, List<string> folderPaths)
    {
        var collection = new Collection(name, folderPaths, configuration.Collections.Count);
        configuration.Collections.Add(collection);
        configuration.Save();
        return collection;
    }

    /// <summary>
    /// Update an existing collection.
    /// </summary>
    public bool UpdateCollection(Guid id, string name, List<string> folderPaths)
    {
        var collection = configuration.Collections.FirstOrDefault(c => c.Id == id);
        if (collection == null)
            return false;

        collection.Name = name;
        collection.FolderPaths = folderPaths ?? new();
        configuration.Save();
        return true;
    }

    /// <summary>
    /// Delete a collection by ID.
    /// </summary>
    public bool DeleteCollection(Guid id)
    {
        var collection = configuration.Collections.FirstOrDefault(c => c.Id == id);
        if (collection == null)
            return false;

        configuration.Collections.Remove(collection);
        configuration.Save();
        return true;
    }

    /// <summary>
    /// Swap the order of two collections by their indices in the sorted list.
    /// </summary>
    public void SwapOrder(int indexA, int indexB)
    {
        var sorted = configuration.Collections.OrderBy(c => c.Order).ToList();
        if (indexA < 0 || indexA >= sorted.Count || indexB < 0 || indexB >= sorted.Count)
            return;

        // Swap in the underlying list
        var a = sorted[indexA];
        var b = sorted[indexB];
        (a.Order, b.Order) = (b.Order, a.Order);
        configuration.Save();
    }

    /// <summary>
    /// Get all designs that match the collection's folder paths.
    /// - If the collection has paths: returns designs matching any of those paths (prefix matching)
    /// - If the collection has NO paths: returns designs not in any other collection ("Uncategorized")
    /// </summary>
    public Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> GetDesignsByCollection(Guid collectionId)
    {
        var collection = configuration.Collections.FirstOrDefault(c => c.Id == collectionId);
        if (collection == null)
            return new();

        var allDesigns = glamourerService.GetDesignList();

        // If collection has no paths, return designs with no folder (root-level designs)
        if (collection.FolderPaths == null || collection.FolderPaths.Count == 0)
        {
            var filtered = allDesigns
                .Where(kvp => !kvp.Value.FullPath.Contains("/"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return filtered;
        }

        // Otherwise, return designs that match any path in this collection
        var result = allDesigns
            .Where(kvp => collection.FolderPaths.Any(path => 
                kvp.Value.FullPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return result;
    }
}
