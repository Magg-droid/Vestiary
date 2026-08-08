using System;
using System.Collections.Generic;
using System.Linq;

namespace Wardrobe.Services;

public static class RandomSelectionHelper
{
    /// <summary>
    /// Picks a random design from the pool while avoiding the immediate previous design when possible.
    /// </summary>
    public static bool TryPickDesign<T>(Dictionary<Guid, T> pool, ref Guid lastPickedDesignId, out Guid pickedDesignId)
    {
        pickedDesignId = Guid.Empty;
        if (pool.Count == 0)
            return false;

        if (pool.Count == 1)
        {
            pickedDesignId = pool.Keys.First();
            lastPickedDesignId = pickedDesignId;
            return true;
        }

        bool hasPreviousInPool = pool.ContainsKey(lastPickedDesignId);
        int eligibleCount = hasPreviousInPool ? pool.Count - 1 : pool.Count;
        if (eligibleCount <= 0)
        {
            pickedDesignId = pool.Keys.First();
            lastPickedDesignId = pickedDesignId;
            return true;
        }

        int targetIndex = Random.Shared.Next(eligibleCount);
        int currentIndex = 0;
        foreach (var designId in pool.Keys)
        {
            if (hasPreviousInPool && designId == lastPickedDesignId)
                continue;

            if (currentIndex == targetIndex)
            {
                pickedDesignId = designId;
                lastPickedDesignId = pickedDesignId;
                return true;
            }

            currentIndex++;
        }

        // Defensive fallback in case of unexpected enumeration mismatch.
        pickedDesignId = pool.Keys.First();
        lastPickedDesignId = pickedDesignId;
        return true;
    }
}