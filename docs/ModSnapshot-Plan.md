# Save Mods — Implementation Plan

> Reference: `docs/ModSnapshot.md` (analysis & architecture)  
> Branch: `feature/quickshot` → merged to `main`

---

## Completed ✅

| Step | What | Status |
|---|---|---|
| 0 | Branch setup, merge main, restore favorites/search/strings | ✅ |
| 1-3 | Penumbra IPCs: mod list, changed items, collection, mod settings | ✅ |
| 4-5 | Lock click → design equipment → ItemId→Name → filter mods → log | ✅ |
| 6 | Store snapshot in config (`ModSnapshot` model) | ✅ |
| 7 | Gold tint when snapshot exists, tooltip reflects state | ✅ |
| 8 | Wire restore IPCs: `TrySetMod`, `TrySetModPriority`, `TrySetModSetting(s)` | ✅ |
| 9 | Restore on apply — enables/disables mods per snapshot | ✅ |
| UX | Rename lock → floppy disk "Save Mods" icon, top-left below star | ✅ |
| UX | Right-click to clear, tooltips, console toasts | ✅ |
| New | Snapshot stores ItemNames — catches new mods on restore | ✅ |
| Perf | Bulk IPC for changed items on restore (1 call vs 4000) | ✅ |

---

## Architecture (as built)

```
💾 Save Mods icon (top-left, below star)
    ├─ No snapshot → dim icon, "Save mods for this outfit"
    ├─ Has snapshot → gold icon, "Mods saved — click to re-save"
    ├─ Left click → capture + log
    └─ Right click → clear snapshot

Capture:
    1. Get design equipment ItemIds → convert to names via IDataManager
    2. Skip empty slots + The Emperor's New items
    3. Get all mods + changed items (bulk IPC)
    4. Filter: mods whose changed items match design item names
    5. Get each matching mod's enabled/priority/settings
    6. Store ModSnapshot (with ItemNames) in config

Restore (on apply):
    1. Read snapshot → ItemNames + saved mods
    2. Bulk IPC: get all mod changed items
    3. Find current matching mods for ItemNames
    4. In snapshot → set to saved enabled/disabled
    5. NOT in snapshot (new mod) → disable + log 🆕
```

---

## Files Changed

| File | Change |
|---|---|
| `Models/ModSnapshot.cs` | New — stores DesignId, ItemNames, Mods with enabled/priority/settings |
| `Services/ModStateService.cs` | New — CaptureState, RestoreState, ClearSnapshot, HasSnapshot |
| `Services/PenumbraService.cs` | New — all Penumbra IPC bridge (mod list, settings, restore, item names) |
| `Services/GlamourerService.cs` | Added GetDesignJObject, GetDesignEquipment |
| `ITheme.cs` + 4 themes | Added SaveModsGold color |
| `Strings.cs` | Added TooltipSaveModsSave/ReSave/Clear |
| `Configuration.cs` | Added ModSnapshots list |
| `MainWindow.cs` | Added saveModsIconPath parameter |
| `MainWindow.DesignCard.cs` | Save Mods icon rendering + capture/restore hooks |
| `Data/save_mods_icon.png` | Floppy disk icon (white, 32x32) |
| `Wardrobe.csproj` | Added save_mods_icon.png, bumped version |
| `Wardrobe.json` | Bumped to 0.8.1.0 |
