# Mod Snapshot — Analysis & Observations

> Feature branch: `feature/quickshot`  
> Renamed from "Quickshot" to "Mod Snapshot" for clarity.

---

## 1. What It Is

A feature that captures the full mod environment of an outfit — both Glamourer state **and** Penumbra mod resolution — so applying a design later also restores the exact mod setup it was created with.

**The problem:** Applying a Glamourer design changes gear appearance but doesn't touch Penumbra. If "Summer Casual" was created while using a body replacer and certain hair mods, re-applying it later won't restore those mods. You have to manually switch Penumbra collections.

---

## 2. What Glamourer Already Does (Manual Mod Association)

Glamourer has a **Mod Association** feature — but it's entirely **manual**. The user must:

1. Open a design in Glamourer
2. Manually trigger "Associate Mods"
3. Glamourer queries Penumbra: *"What mods affect these equipment items?"*
4. Glamourer stores the associated mods in the design state (`Mods[]` array)

The user has to remember to do this for every design. Most don't.

### Glamourer State JSON Structure (after manual association)

```json
{
  "Equipment": {
    "Head":  { "ItemId": 12345, "Stain": 0 },
    "Body":  { "ItemId": 23456, "Stain": 5 },
    "Hands": { "ItemId": 34567, "Stain": 0 },
    "Legs":  { "ItemId": 45678, "Stain": 5 },
    "Feet":  { "ItemId": 56789, "Stain": 0 }
  },
  "Customize": { ... },
  "Mods": [
    { "Name": "Bikini Armor Mod",   "Directory": "bikini_armor_v2", "Enabled": true  },
    { "Name": "Hair Texture HD",    "Directory": "hair_hd_mod",     "Enabled": true  },
    { "Name": "Conflicting Mod",    "Directory": "other_body_mod",  "Remove": true   }
  ]
}
```

`Remove: true` means Glamourer explicitly disables that mod on apply (conflict resolution).

---

## 3. How Penumbra Exposes Mod Data (Investigation Results)

We traced Glamourer's mod association code and Penumbra's IPC implementation. Here's what actually works:

### Two Different Data Sources

| Source | API | What it returns |
|---|---|---|
| **Mod metadata** (raw) | `Penumbra.GetChangedItems(dirName, modName)` | Mod's own declared changed items — **NOT filtered by enabled state** |
| **Mod metadata** (bulk) | `Penumbra.GetChangedItemAdapterList()` | ALL mods with ALL their changed items — **NOT filtered** |
| **Collection cache** (resolved) | `Penumbra.CheckCurrentChangedItemFunc(gamePath)` | Only mods that WIN resolution — **enabled only** ❌ |

**Key finding:** `GetChangedItems` and `GetChangedItemAdapterList` query the mod's own metadata (`mod.ChangedItems`) — this is the mod's declaration of what files it modifies, part of its `.pmp`/config. It does **not** change based on whether the mod is enabled or disabled in a collection.

`CheckCurrentChangedItemFunc` queries the **collection's resolved cache** (`CollectionCache.ResolvedFiles`) which only contains winning mods — so it **does** filter to enabled only.

### Why Disabled Mods Matter

If a user has 3 body mods in Penumbra for the Hempen Camise but only 1 is enabled, applying the design later should:

- ✅ Enable the one that was enabled
- ❌ Leave the other two disabled (exactly as they were)
- ❌ NOT let Penumbra's default resolution pick a different winner

### Summary: What Wardrobe Adds

| | Glamourer Manual Association | Wardrobe Mod Snapshot |
|---|---|---|
| Trigger | User must remember to do it | One-click lock icon |
| Enabled mods | ✅ Captures | ✅ Captures |
| Disabled mods | ❌ Misses them | ✅ Captures — full resolution |
| Per-equipment-item precision | ❓ Manual per-mod selection | ✅ Automatic per-item filtering |

---

## 4. What We Actually Need

For each equipment slot in the design, capture the complete Penumbra mod resolution:

| Equipment Slot | ItemId  | All Mods Affecting This Item | State       |
|----------------|---------|------------------------------|-------------|
| Body           | 23456   | Bikini Armor Mod             | ✅ Enabled  |
| Body           | 23456   | Other Body Mod               | ❌ Disabled |
| Body           | 23456   | Vanilla Upscale              | ❌ Disabled |
| Head           | 12345   | Hair Texture HD              | ✅ Enabled  |

This means we need to go **directly to Penumbra**, not just rely on Glamourer's association.

---

## 5. Proposed Architecture

### Capture Flow

```
User clicks 🔒 on a design card
        │
        ▼
Step 1: Get player's current Penumbra collection
        Penumbra.GetCollectionForObject(0)
        → (bool valid, bool individual, (Guid id, string name))
        │
        ├─ invalid → show "No Penumbra collection active" tooltip, abort
        │
        ▼
Step 2: Get ALL mods with their changed items (bulk, NOT enabled-filtered)
        Penumbra.GetChangedItemAdapterList()
        → List<(string dirName, Dictionary<string gamePath, object?> changedItems)>
        │
        ▼
Step 3: Get design equipment ItemIds → game paths
        Glamourer.GetDesignJObject(designId) → parse Equipment
        Convert ItemId → game path (via Penumbra.GameData.GamePaths)
        │
        ▼
Step 4: Filter — which mods touch any of the design's equipment paths?
        For each mod, check if changedItems keys overlap with equipment paths
        → Finds BOTH enabled AND disabled mods
        │
        ▼
Step 5: Get each matching mod's current state from collection
        Penumbra.GetCurrentModSettings(collectionId, dirName, modName)
        → (bool Enabled, int Priority, Dictionary<string, List<string>> Settings)
        │
        ▼
Step 6: Store ModSnapshot in config
        config.ModSnapshots[designId] = { ItemId → [{DirName, ModName, Enabled, Priority, Settings}, ...] }
        │
        ▼
Step 7: Update lock icon — 🔒 → 🔒🟡 (gold = snapshot exists)
```

### Restore Flow

```
User applies design (double-click or Apply button)
        │
        ▼
Step 1: Glamourer.ApplyDesign(designId) — existing flow
        │
        ▼
Step 2: config.ModSnapshots.TryGetValue(designId, out snapshot)?
        │
        ├─ No → done (no mod state captured for this design)
        │
        └─ Yes → continue
                │
                ▼
Step 3: Get player's current Penumbra collection
        Penumbra.GetCollectionForObject(0)
        → (bool valid, bool individual, (Guid id, string name))
                │
                ├─ invalid → done (no collection to modify)
                │
                └─ valid → restore each mod
                        │
                        ▼
Step 4: For each mod in snapshot:
        ┌──────────────────────────────────────────────┐
        │ Penumbra.TrySetMod(collectionId, dirName,   │
        │     enabled, modName)                       │
        │                                              │
        │ if enabled:                                  │
        │   Penumbra.TrySetModPriority(collectionId,  │
        │       dirName, priority, modName)           │
        │   For each option group:                    │
        │     TrySetModSetting (single value) or      │
        │     TrySetModSettings (multi-select)        │
        └──────────────────────────────────────────────┘
                │
                ▼
Step 5: Done — Penumbra mod state exactly matches snapshot
```

**Key design decisions:**
- **Only restore if snapshot exists.** No snapshot = no-op. We never touch Penumbra unnecessarily.
- **Use permanent settings** (`TrySetMod`, not temporary). This modifies the collection directly — same as Glamourer's own "Apply" button in Mod Associations.
- **Disabled mods are explicitly set to disabled.** Prevents Penumbra's default resolution from accidentally enabling them.
- **Mods NOT in the snapshot are left alone.** We only touch mods that affect the design's equipment.

### Data Model

Wardrobe persists the mod snapshot in its own config:

```csharp
public class ModSnapshot
{
    public Guid DesignId { get; set; }
    public DateTime CapturedAt { get; set; }

    // Key: ItemId, Value: list of mod states for that item
    public Dictionary<uint, List<ModEntry>> EquipmentMods { get; set; } = new();
}

public class ModEntry
{
    public string DirName { get; set; } = string.Empty;   // Penumbra mod directory
    public string ModName { get; set; } = string.Empty;   // Human-readable name
    public bool Enabled { get; set; }                      // Current state in Penumbra
    public int Priority { get; set; }                      // Mod priority
    public Dictionary<string, List<string>> Settings { get; set; } = new(); // Option settings
}
```

Stored in `Configuration.ModSnapshots` keyed by design GUID.

---

## 6. Penumbra IPC Requirements

### Capture IPCs (already exist, verified)

| IPC Method | Purpose | Source |
|---|---|---|
| `Penumbra.GetChangedItemAdapterList()` | Get ALL mods with ALL changed items (bulk) — **not enabled-filtered** | `ModsApi` |
| `Penumbra.GetCurrentModSettings(col, dir, name)` | Get enabled state + priority + settings for a mod | `ModSettingsApi` |
| `Penumbra.GetCollectionForObject(0)` | Get the collection assigned to the player | `CollectionApi` |

### Restore IPCs (already exist, verified)

| IPC Method | Purpose |
|---|---|
| `Penumbra.TrySetMod(col, dir, enabled, name)` | Enable or disable a mod in a collection |
| `Penumbra.TrySetModPriority(col, dir, priority, name)` | Set mod priority order |
| `Penumbra.TrySetModSetting(col, dir, group, value, name)` | Set a single-select option |
| `Penumbra.TrySetModSettings(col, dir, group, values, name)` | Set a multi-select option |

### Still To Investigate

| Item | Notes |
|---|---|
| ItemId → game path conversion | `Penumbra.GameData.GamePaths` likely has helpers for this |
| Exact game path format for equipment | e.g. `chara/equipment/e{ItemId}/model/c{race}{gender}_...` |

---

## 7. Current Branch State (`feature/quickshot`)

### What's Done

| File | Status |
|---|---|
| `Models/ModState.cs` | ✅ Model exists (but needs redesign per above) |
| `Services/ModStateService.cs` | ⚠️ Skeleton — `CaptureState()` / `RestoreState()` with TODOs |
| `Services/PenumbraService.cs` | ⚠️ Skeleton — IPC methods with TODOs, all return null |
| Lock icon on design cards | ✅ Rendered as 4th icon below clipboard |
| Lock icon click handler | ⚠️ Has `/* TODO: quickshot feature */` |
| `Configuration.ModStates` | ✅ Added to config |

### What Was Removed (Collateral?)

| Item | Notes |
|---|---|
| `FavoriteService.cs` | Deleted — lock icon took the star's visual spot |
| Star icons (empty/filled) | Deleted |
| Search bar + `FilterBySearch()` | Removed from header row |
| `FavoriteDesignIds` in config | Replaced by `ModStates` |

> **⚠️ These removals should be reverted before merging.** Search and favorites are independent features.

### What's Missing

| Task | Notes |
|---|---|
| Penumbra IPC wiring | IPCs identified — `GetChangedItemAdapterList`, `GetCurrentModSettings`, `TrySetMod*` |
| ItemId → game path conversion | Need `Penumbra.GameData.GamePaths` investigation |
| Lock icon visual feedback | Gold tint when snapshot exists, tooltip changes |
| Restore hook in apply flow | After Glamourer apply, restore Penumbra state |
| `ModState` → `ModSnapshot` model redesign | Store per-item mod states, not collection name |

---

## 8. Design Decisions

### 8.1 Collection vs Per-Item Approach

| Approach | Pro | Con |
|---|---|---|
| **Store Penumbra collection name** | Simple, single IPC call | Coarse — if user changed collection contents, restore is wrong |
| **Store per-item mod states** | Precise, independent of collection changes | More IPC calls, more complex model |

**Decision:** Per-item approach. The whole point is precision — capturing both enabled and disabled mods per equipment slot.

### 8.2 Manual vs Automatic Capture

| Approach | Pro | Con |
|---|---|---|
| **Manual** (lock icon click) | User controls exactly what's captured | Extra step, forgettable |
| **Automatic** (on design apply) | Zero friction | Would capture the state you're APPLYING, not the state you CREATED it in |
| **Hybrid** — manual lock + "auto-snapshot on first apply" opt-in | Best of both | More settings UI |

**Decision:** Start with manual lock icon. It's already built.

### 8.3 Restore: Only If Snapshot Exists

When applying a design, Wardrobe checks if a ModSnapshot exists. If no snapshot → no Penumbra calls. We never touch Penumbra unnecessarily. Only designs the user has explicitly locked get mod state restored.

### 8.4 Permanent vs Temporary Settings

| Approach | Pro | Con |
|---|---|---|
| **Permanent** (`TrySetMod`) | Modifies collection directly, persists | Can't auto-revert |
| **Temporary** (`SetTemporaryModSettings`) | Can revert, keyed | More complex, needs key management |

**Decision:** Use permanent settings for v1. Same behavior as Glamourer's Mod Association "Apply" button. Simpler, no key management needed.

### 8.5 Disabled Mods: Explicitly Set to Disabled

When a snapshot has a disabled mod, we call `TrySetMod(collectionId, dirName, enabled: false)`. This prevents Penumbra's default resolution from accidentally enabling it.

### 8.6 Mods NOT in Snapshot: Leave Alone

We only touch mods that affect the design's equipment. Other mods in the collection are unaffected.

### 8.7 What If Mods Were Removed Since Snapshot?

- Silently skip missing mods
- Show a toast: "2 mods from snapshot no longer exist"

**Decision:** Silently skip in v1, add toast later.

### 8.8 One Snapshot, or History?

Single snapshot per design, overwrites on re-lock.

**Decision:** Single snapshot for v1.

---

## 9. Next Steps

1. **Investigate ItemId → game path conversion** — Penumbra.GameData.GamePaths utilities
2. **Wire `Penumbra.GetChangedItemAdapterList()`** — bulk mod changed items query (replaces skeleton PenumbraService methods)
3. **Wire `Penumbra.GetCurrentModSettings()`** — per-mod state query
4. **Wire restore IPCs** — `TrySetMod`, `TrySetModPriority`, `TrySetModSetting(s)`
5. **Implement `ModSnapshot` model** — replace skeleton `ModState` on feature branch
6. **Implement `ModStateService.CaptureState()`** — run the capture flow
7. **Implement `ModStateService.RestoreState()`** — run the restore flow (called after Glamourer apply)
8. **Revert unintentional removals** — favorites, search, inline strings from feature branch
9. **Wire the lock icon handler** — calls CaptureState, updates icon color
10. **Add visual feedback** — gold lock = snapshot exists, tooltip changes

---

## 10. Key IPC Endpoints (Reference)

### Glamourer (already used)

```
Glamourer.GetDesignListExtended  → Dict<Guid, (DisplayName, FullPath, DisplayColor, ShownInQdb)>
Glamourer.GetDesignBase64(Guid)  → string? (base64+gzip JSON of design)
Glamourer.GetDesignJObject(Guid) → JObject? (parsed design JSON — includes Equipment ItemIds)
Glamourer.ApplyDesign(Guid, int, uint, ulong) → int  (status code)
Glamourer.DeleteDesign(Guid)     → int  (status code)
Glamourer.GetState(0, 0)         → (int, JObject?)  (current player state with Equipment + Mods[])
```

### Penumbra — Capture

```
Penumbra.GetCollectionForObject(0)              → (bool valid, bool individual, (Guid id, string name))
Penumbra.GetChangedItemAdapterList()            → List<(string dir, IReadOnlyDict<string, object?> changedItems)>
Penumbra.GetCurrentModSettings(col, dir, name)  → (bool enabled, int priority, Dict<string, List<string>> settings)
```

### Penumbra — Restore

```
Penumbra.TrySetMod(col, dir, bool enabled, name)       → PenumbraApiEc
Penumbra.TrySetModPriority(col, dir, int priority, name)→ PenumbraApiEc
Penumbra.TrySetModSetting(col, dir, group, value, name) → PenumbraApiEc
Penumbra.TrySetModSettings(col, dir, group, values, name) → PenumbraApiEc
```

---

*Last updated: Analysis phase. No implementation decisions finalized.*
