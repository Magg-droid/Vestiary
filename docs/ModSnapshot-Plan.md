# Mod Snapshot — Implementation Plan

> Reference: `docs/ModSnapshot.md` (analysis & architecture)  
> Starting branch: `feature/quickshot`

---

## Philosophy

One small step at a time. Each step must be testable in-game before moving on. Log everything to console so we can verify without building UI.

---

## Step 0 — Branch Setup & Cleanup

**Goal:** Clean starting point. Revert collateral damage from the feature branch.

- [ ] `git checkout feature/quickshot`
- [ ] Revert removals:
  - Restore `FavoriteService.cs` and star icons (they were deleted to make room for lock icon — put them back)
  - Restore search bar + `FilterBySearch()` in `MainWindow.HeaderRow.cs`
  - Restore `FavoriteDesignIds` in `Configuration.cs` (keep `ModStates` too)
  - Restore all the hardcoded strings that got inlined back to `Strings.cs`
- [ ] Verify the lock icon still renders as the 4th icon below clipboard
- [ ] Build & test: plugin loads, gallery works, favorites work, search works, lock icon visible

**Stop and verify:** Everything before quickshot still works. The lock icon is present but does nothing.

---

## Step 1 — Wire Penumbra `GetChangedItemAdapterList`

**Goal:** Add a working Penumbra IPC subscriber for the bulk changed-items query.

- [ ] In `PenumbraService.cs`, add subscriber for `Penumbra.GetChangedItemAdapterList`  
  Label: `"Penumbra.GetChangedItemAdapterList"`  
  Returns: `IReadOnlyList<(string ModDirectory, IReadOnlyDictionary<string, object?> ChangedItems)>`
- [ ] Add a public method `GetAllModChangedItems()` that invokes it
- [ ] Handle Penumbra not available (return empty list)

**Stop and verify:** Call `GetAllModChangedItems()` from somewhere (e.g. a debug button or on plugin load) and log the count of mods and their changed item counts. Does it return ALL mods (not just enabled)?

---

## Step 2 — Wire Penumbra `GetCurrentModSettings`

**Goal:** Query a mod's enabled/disabled state from the player's current collection.

- [ ] In `PenumbraService.cs`, add subscriber for `Penumbra.GetCurrentModSettings`  
  Takes: `(Guid collectionId, string directoryName, string modName)`  
  Returns: `(PenumbraApiEc, (bool Enabled, int Priority, Dictionary<string, List<string>> Settings)?)`
- [ ] Add a public method `GetModSettings(Guid collectionId, string dirName, string modName)` that invokes it
- [ ] Handle missing mod / error

**Stop and verify:** Pick one mod from Step 1's output. Call `GetModSettings` for it. Log: `[ModSnapshot] Mod "Bikini Armor" — Enabled: true, Priority: 5, Settings: { "Version": ["v2"] }`. Confirm disabled mods show `Enabled: false`.

---

## Step 3 — Wire `GetCollectionForObject`

**Goal:** Get the player's current Penumbra collection.

- [ ] In `PenumbraService.cs`, add subscriber for `Penumbra.GetCollectionForObject`  
  Takes: `int gameObjectIndex`  
  Returns: `(bool ObjectValid, bool IndividualSet, (Guid Id, string Name) EffectiveCollection)`
- [ ] Add public method `GetPlayerCollection()` that calls with index `0` (player)

**Stop and verify:** Log `[ModSnapshot] Player collection: "My Body Mods" (guid)`.

---

## Step 4 — Get Design Equipment ItemIds

**Goal:** Given a design GUID, extract what equipment slots it has (with ItemIds).

- [ ] Call `Glamourer.GetDesignJObject(designId)` — already available in `GlamourerService`
- [ ] Parse the JObject: `design["Equipment"]` → for each slot, extract `ItemId`
- [ ] Return `Dictionary<EquipSlot, uint>` (slot → ItemId)
- [ ] Skip empty slots (ItemId = 0 or missing)

**Stop and verify:** Log `[ModSnapshot] Design "Summer Casual" equipment: Head=12345, Body=23456, Hands=34567, ...`.

---

## Step 5 — First Integration: Log All Mods for a Design (Console Only)

**Goal:** This is the big one. On lock icon click, run Steps 1-4 together and dump everything to console. No storage, no restore — just log.

- [ ] Wire the lock icon click handler in `MainWindow.DesignCard.cs` (currently `/* TODO: quickshot feature */`)
- [ ] Handler calls a new method `ModStateService.LogModSnapshot(Guid designId)` that:
  1. Gets player collection (Step 3) — abort if no collection
  2. Gets all mod changed items (Step 1)
  3. Gets design equipment ItemIds (Step 4)
  4. Converts ItemIds to game paths *(may need placeholder — see Note below)*
  5. Filters mods whose changed items overlap with equipment paths
  6. For each matching mod, gets settings (Step 2) — includes Enabled state
  7. Logs everything to console

**Console output should look like:**
```
[ModSnapshot] Design: "Summer Casual" (guid)
[ModSnapshot] Collection: "My Body Mods" (guid)
[ModSnapshot] Equipment: Head=12345, Body=23456, Hands=34567, Legs=45678, Feet=56789
[ModSnapshot] 
[ModSnapshot] Mods affecting this design:
[ModSnapshot]   Body (ItemId=23456):
[ModSnapshot]     ✅ "Bikini Armor Mod" [bikini_armor_v2] Priority=5
[ModSnapshot]       Settings: Version -> v2
[ModSnapshot]     ❌ "Other Body Mod" [other_body_mod] Priority=3 (DISABLED)
[ModSnapshot]   Head (ItemId=12345):
[ModSnapshot]     ✅ "Hair Texture HD" [hair_hd_mod] Priority=1
[ModSnapshot]   No mods for: Hands, Legs, Feet
```

**⚠️ Note on ItemId → game path:**  
This may need `Penumbra.GameData` reference. If the conversion is complex, start with a **simplified approach**: match on ItemId as a string substring of changed item keys. Many changed item paths contain the item ID as `e12345`. We can refine later.

**Stop and verify:** In-game, open Wardrobe, click the lock icon on a design that has equipment mods. Check Dalamud console. Confirm both enabled ✅ and disabled ❌ mods appear. Confirm mods for irrelevant items don't appear.

---

## Step 6 — Store Snapshot in Config

**Goal:** Save the captured mod state so we can use it later.

- [ ] Add `ModSnapshot` model (see `ModSnapshot.md` Section 5)
- [ ] Add `List<ModSnapshot> ModSnapshots` to `Configuration.cs`
- [ ] After logging (Step 5), also save the snapshot to `config.ModSnapshots`
- [ ] Replace existing snapshot for same design (overwrite)
- [ ] Add `ModStateService.HasSnapshot(Guid designId)` → bool
- [ ] Add `ModStateService.GetSnapshot(Guid designId)` → ModSnapshot?

**Stop and verify:** Click lock, check config JSON on disk has the snapshot data. Click lock again, verify it overwrites. Restart plugin, verify snapshot persists.

---

## Step 7 — Lock Icon Visual Feedback

**Goal:** User can see which designs have a snapshot.

- [ ] On card render, check `ModStateService.HasSnapshot(designId)`
- [ ] If has snapshot: tint lock icon gold/yellow
- [ ] Change tooltip: "Mod snapshot saved" vs "Save current state of your mods"

**Stop and verify:** Lock icon turns gold after clicking. Stays gold after plugin restart. Different design without snapshot shows default lock.

---

## Step 8 — Wire Restore IPCs

**Goal:** Add Penumbra IPC subscribers for setting mod state.

- [ ] `Penumbra.TrySetMod` — `(Guid collectionId, string dirName, bool enabled, string modName)` → `PenumbraApiEc`
- [ ] `Penumbra.TrySetModPriority` — `(Guid collectionId, string dirName, int priority, string modName)` → `PenumbraApiEc`
- [ ] `Penumbra.TrySetModSetting` — `(Guid collectionId, string dirName, string optionGroup, string value, string modName)` → `PenumbraApiEc`
- [ ] `Penumbra.TrySetModSettings` — `(Guid collectionId, string dirName, string optionGroup, IReadOnlyList<string> values, string modName)` → `PenumbraApiEc`
- [ ] Wrap in public methods on `PenumbraService`

**Stop and verify:** Log each call. No visual change yet — just confirm IPCs don't error.

---

## Step 9 — Restore on Apply (Console Only First)

**Goal:** When applying a design that has a snapshot, restore mod state. Log everything.

- [ ] In the design apply flow (double-click or Apply button), after `Glamourer.ApplyDesign`:
- [ ] Check `ModStateService.HasSnapshot(designId)` → if no, skip
- [ ] Call `ModStateService.RestoreSnapshot(designId)`:
  1. Get player collection
  2. Get snapshot from config
  3. For each mod in snapshot:
     - `TrySetMod(collectionId, dirName, enabled, modName)`
     - If enabled: `TrySetModPriority(...)` then `TrySetModSetting(s)(...)` for each option
  4. Log each action

**Console output:**
```
[ModSnapshot] Restoring mod state for "Summer Casual"
[ModSnapshot]   Collection: "My Body Mods"
[ModSnapshot]   [bikini_armor_v2] → Enabled, Priority=5, Settings applied
[ModSnapshot]   [other_body_mod] → Disabled
[ModSnapshot]   [hair_hd_mod] → Enabled, Priority=1, Settings applied
[ModSnapshot] Restore complete — 3 mods set
```

**Stop and verify:** Apply a design with a snapshot. Check Dalamud console. Check Penumbra UI — did the mods actually change state? Try with a design that has NO snapshot — confirm no Penumbra calls happen.

---

## Step 10 — Polish & Edge Cases

- [ ] Handle Penumbra not available (graceful skip, log warning)
- [ ] Handle collection missing/deleted since snapshot (log, skip restore)
- [ ] Handle mod no longer installed (skip, log)
- [ ] Add "Clear Snapshot" button? (Shift+click lock icon?)
- [ ] Re-snapshot flow: clicking lock again overwrites (already works from Step 6)
- [ ] Test: what if collection changed between capture and restore? (Should still work — we restore to the exact per-mod state, not the collection)

---

## Step 11 — Merge & Cleanup

- [ ] Final review of all changes against `ModSnapshot.md` analysis
- [ ] Remove debug logging (or demote to Verbose)
- [ ] Update `docs/FEATURES.md` with Mod Snapshot feature
- [ ] Update README if needed
- [ ] PR to main

---

## Dependencies

```
Step 0 ──► Step 1 ──► Step 2 ──► Step 3 ──► Step 5 ──► Step 6 ──► Step 7
                          │            │                        │
                          └────────────┘                        │
                                │                               │
                          Step 4 ───────────────────────────────┘
                                                               │
                          Step 8 ──► Step 9 ──► Step 10 ──► Step 11
```

Steps 1, 2, 3, 4 can be done in parallel. Step 5 ties them together. Step 8 can be done anytime before Step 9.

---

*Last updated: Planning phase. Steps will be checked off as completed.*
