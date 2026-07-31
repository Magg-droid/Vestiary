# Save Mods — Architecture

> Formerly "Mod Snapshot" / "Quickshot"  
> Implemented in v0.8.1.0

---

## Overview

💾 **Save Mods** captures your Penumbra mod state for an outfit. When you apply the outfit later, it restores the exact mod setup — including disabling new mods that weren't there when the snapshot was taken.

---

## How It Works

### Capture (💾 left-click)

```
User clicks 💾
    │
    ▼
Get design equipment ItemIds from Glamourer
    │
    ▼
Convert ItemId → Item Name via FFXIV game data (IDataManager)
    Skip empty slots, skip "The Emperor's New" items
    │
    ▼
Get all Penumbra mods + their changed items (bulk IPC)
    │
    ▼
Filter: mods whose changed items match design item names
    → Finds both enabled AND disabled mods
    │
    ▼
Get each matching mod's state: enabled/disabled, priority, option settings
    │
    ▼
Store ModSnapshot in config:
    - DesignId
    - ItemNames (equipment item names for this design)
    - Mods[] (DirName, ModName, Enabled, Priority, Settings)
    │
    ▼
💾 icon turns gold · Console: "12 mods saved (4 enabled, 8 disabled)"
```

### Restore (auto on apply)

```
User applies design
    │
    ▼
Glamourer applies the outfit (existing flow)
    │
    ▼
Has snapshot? → No → done
    │
    ▼ Yes
Read snapshot → ItemNames + saved mods
    │
    ▼
Get ALL mod changed items (bulk, 1 IPC)
    │
    ▼
Find current mods matching ItemNames
    │
    ├─ In snapshot → set to saved enabled/disabled + priority + settings
    │
    └─ NOT in snapshot (new mod since capture) → disable + log 🆕
    │
    ▼
Console: "🔄 Restored — 5 enabled, 10 disabled, 0 errors"
```

### Clear (right-click)

Right-click 💾 → removes snapshot. Icon goes dim.

---

## Key Design Decisions

| Decision | Why |
|---|---|
| **Bulk IPC for changed items** | 1 call instead of 4000 per-mod calls — avoids hitch |
| **ItemNames stored in snapshot** | Enables catching new mods on restore without re-capturing |
| **Equipment-only filtering** | Ignores hair/skin/body mods — only gear-specific mods |
| **Skip "The Emperor's New"** | Invisible slots would pull in body mods |
| **Error code 1 = NothingChanged** | Not a failure — already in desired state |
| **Floppy disk icon** | Universal "save" symbol, distinct from camera/favorites |
| **Top-left position** | Next to star — both have gold state, grouped visually |

---

## Models

```csharp
ModSnapshot
├── DesignId: Guid
├── CapturedAt: DateTime
├── ItemNames: List<string>         // equipment item names
└── Mods: List<ModEntry>
    ├── DirName: string             // Penumbra mod directory
    ├── ModName: string             // display name
    ├── Enabled: bool
    ├── Priority: int
    └── Settings: Dict<string, List<string>>
```

---

## IPC Endpoints Used

### Glamourer
```
GetDesignListExtended
GetDesignJObject
ApplyDesign
```

### Penumbra — Read
```
GetModList
GetChangedItemAdapterList       (bulk)
GetCurrentModSettings.V5
GetCollection
ApiVersions                     (availability check)
```

### Penumbra — Write
```
TrySetMod.V5
TrySetModPriority.V5
TrySetModSetting.V5
TrySetModSettings.V5
```

---

## PenumbraApiEc Error Codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | NothingChanged (not an error) |
| 2 | CollectionMissing |
| 3 | ModMissing |
| 4+ | Various errors |
