# Quickshot — Developer Guide

> Feature branch: `feature/quickshot`

## What's already done

- **Lock icon** — 4th action icon on every design card (below camera/upload/clipboard)
- Tooltip: "Save current state of your mods"
- Click handler wired in `MainWindow.DesignCard.cs` — currently a TODO

## Architecture

This project follows Angular-style service-oriented architecture:
- **Windows** render UI only — never contain business logic
- **Services** contain all logic — injected via constructor
- See `docs/CodingStandards.md` for full standards

## Services you'll work with

| Service | What it does | Status |
|---|---|---|
| `PenumbraService` | IPC bridge to Penumbra | **Skeleton** — methods exist, need IPC wiring |
| `ModStateService` | Capture/restore mod state per design | **Skeleton** — logic written, calls Penumbra/Glamourer methods |
| `GlamourerService` | Existing IPC to Glamourer | Needs `GetDesignState()` / `ApplyDesignState()` |

## Models

| Model | Purpose |
|---|---|
| `ModState` | Stores captured state: Penumbra collection, Glamourer JSON, active mods, timestamp |

## What needs doing

1. **Penumbra IPC** (`Services/PenumbraService.cs`)
   - Wire `GetCollectionForPlayer()` — use Dalamud IPC subscriber
   - Wire `GetModsInCollection()` 
   - Wire `SetCollectionForPlayer()`
   - Wire `IsAvailable()`

2. **Glamourer state** (`Services/GlamourerService.cs`)
   - Add `GetDesignState(Guid designId)` — returns JSON of current design state
   - Add `ApplyDesignState(Guid designId, string json)` — applies saved state

3. **Wire the lock icon** (`Windows/MainWindow/MainWindow.DesignCard.cs`)
   - The lock icon click handler is at the bottom of `DrawThumbnailIcons`
   - Call `plugin.ModStateService.CaptureState(designId)` on click
   - On apply (double-click or Apply button), call `ModStateService.RestoreState(designId)`

4. **Visual feedback** (stretch goal)
   - Change lock icon color when state is captured (e.g., yellow/gold tint)
   - Show tooltip "Mod state saved" vs "Save current state of your mods"
