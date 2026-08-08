# Glamour Roulette — Feature Plan

> 🎲 Automated outfit & mod snapshot randomizer with timer interval support.

---

## Overview

**Glamour Roulette** allows players to automatically swap outfits (and associated Penumbra mod snapshots) on a configurable timer interval (e.g., every 15 minutes) or manually on demand.

This feature is gated behind a settings flag (`EnableGlamourRoulette`) and appears as a dedicated section in the left side rail.

---

## UI Layout Wireframe (Full Main Window Context)

Below is the layout of the Wardrobe window when **Roulette** is selected in the **Left Side Rail**:

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ Wardrobe — Glamour Roulette                                                                           ─  □  ✕  │
├─────────────────┬───────────────────────────────────────────────────────────────────────────────────────────────┤
│                 │                                                                                               │
│  [Glamour]      │   Glamour Roulette                                                                            │
│                 │   Automated periodic outfit randomizer                                                        │
│  [Emotes]       │  ───────────────────────────────────────────────────────────────────────────────────────────  │
│                 │                                                                                               │
│ >[Roulette]<    │   ┌─ STATUS & CONTROLS ────────────────────────────────────────────────────────────────────┐  │
│  (Active Tab)   │   │                                                                                            │  │
│                 │   │   [🟢 ROULETTE ACTIVE]       Next Swap: 14m 32s               [🎲 Swap Now]              │  │
│  [Settings]     │   │   (Click to Toggle OFF)      Pool: 39 outfits (3 collections)                             │  │
│                 │   │                                                                                            │  │
│  [Help]         │   └────────────────────────────────────────────────────────────────────────────────────────────┘  │
│                 │                                                                                               │
│  [Minimize]     │   ┌─ TIMER INTERVAL ───────────────────────────────────────────────────────────────────┐  │
│                 │   │                                                                                            │  │
│                 │   │   Presets: [ 5m ]  [ 10m ]  [ 15m ]  [ 30m ]  [ 60m ]     Custom: [━━━━━━●━━━━] 15m       │  │
│                 │   │                                                                                            │  │
│                 │   └────────────────────────────────────────────────────────────────────────────────────────────┘  │
│                 │                                                                                               │
│                 │   ┌─ INCLUDED COLLECTIONS ─────────────────────────────────────────────────────────────────┐  │
│                 │   │                                                                                            │  │
│                 │   │   [x] Exclude Favorites                                                                    │  │
│                 │   │                                                                                            │  │
│                 │   │   Select collections to include (if none selected, all non-favorites are included):       │  │
│                 │   │   [x] Dresses (14 outfits)                  [x] Casual Outfits (8 outfits)                   │  │
│                 │   │   [x] New Hot Dresses (6 outfits)           [ ] Formal Wear (5 outfits)                      │  │
│                 │   │   [x] Summer Collection (11 outfits)        [ ] Uncategorized (3 outfits)                    │  │
│                 │   │                                                                                            │  │
│                 │   └────────────────────────────────────────────────────────────────────────────────────────────┘  │
│                 │                                                                                               │
└─────────────────┴───────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## User Flow

1. **Enable in Settings**: User enables "Enable Glamour Roulette" in `ConfigWindow`.
2. **Access Section**: **Roulette** appears in the left sidebar rail under Glamour / Emotes.
3. **Configure Options**:
   * **Timer Interval**: Select minutes via quick chips or smooth slider (5 to 120 mins).
   * **Collection Picker**: Checkboxes to select which collections are included in the pool.
4. **Start Roulette**: Click the **ROULETTE ACTIVE / OFF** status button.
   * **First Trigger**: Instantly applies a random visible outfit from selected collections.
   * **Timer Starts**: Live countdown displays time until the next automatic swap.
5. **Minimized View Quick Toggle**:
   * A compact `[🎰]` icon in the Minimized View top toolbar shows status and lets users toggle Roulette on/off in compact mode.
   * **Emote View Guard**: If user is currently in the Emotes section (`_currentView == 1`), the `[🎰]` icon button is **disabled** and hover tooltip says `"Only available for glamour"`.

---

## Architecture & Service Design

Following Wardrobe's **Thin Windows, Fat Services** architecture:

```
MainWindow (Left Rail Navigation)
      │
      ├──────► Glamour View (_currentView == 0)
      ├──────► Emotes View  (_currentView == 1)
      └──────► Roulette View (_currentView == 2) ──► RouletteService (Timer, selection pool, state)
                                                          │
                                                          ├── GlamourerService (Apply design)
                                                          ├── ModStateService (Restore Penumbra mod snapshot)
                                                          ├── CollectionService (Fetch collections & visible designs)
                                                          └── Configuration (Persist settings & roulette state)
```

### 1. State (`Configuration.cs`)

```csharp
public bool EnableGlamourRoulette { get; set; } = false;
public bool RouletteActive { get; set; } = false;
public int RouletteIntervalMinutes { get; set; } = 15;
public bool RouletteExcludeFavorites { get; set; } = true;
public List<Guid> RouletteCollectionIds { get; set; } = new();
```

### 2. Service (`Services/RouletteService.cs`)

* **Responsibilities**:
  * Manages timer state, last trigger timestamp, and next trigger calculation.
  * Subscribes to Dalamud `IFramework.Update` to count down time accurately.
  * Orchestrates random design selection across chosen collections.
  * Invokes `GlamourerService.ApplyDesign()` and `ModStateService.RestoreState()`.
  * Exposes state queries (`IsActive`, `RemainingSeconds`, `NextTriggerTime`, `ToggleActive()`).

### 3. Strings (`Strings.cs`)

All user-facing text added to `Strings.cs`:
* `RailRoulette`, `RouletteHeading`, `RouletteStatusActive`, `RouletteStatusInactive`
* `RouletteStartButton`, `RouletteStopButton`, `RouletteSwapNowButton`
* `RouletteIntervalLabel`, `RouletteCollectionsLabel`
* `TooltipRouletteMinimizedActive`, `TooltipRouletteMinimizedInactive`, `TooltipRandomButtonGlamourOnly`

### 4. UI Components

* **`ConfigWindow.cs`**: Checkbox for `EnableGlamourRoulette` ("Enable Glamour Roulette").
* **`MainWindow.Rail.cs`**: Render "Roulette" item in the side navigation rail when enabled.
* **`MainWindow.Roulette.cs` (New partial class)**: Renders the right-panel Roulette dashboard (status card, timer, swap button, collection selection grid).
* **`MainWindow.cs` (Minimized View Top Bar)**: Adds `[🎰]` Roulette toggle icon button to top toolbar alongside `[≡] [◀▶] [🎲]`. Disabled when in Emotes view with `"Only available for glamour"` tooltip.

---

## Verification Plan

- [ ] Enable Glamour Roulette in Settings → "Roulette" appears in side rail.
- [ ] Disable Glamour Roulette in Settings → "Roulette" hides from side rail & minimized top bar.
- [ ] Toggle Roulette ON → Instantly applies a random outfit, timer begins countdown.
- [ ] Wait for timer expiration → Automatically applies next random outfit.
- [ ] Click `[🎰]` in Minimized View (Glamour view) → Toggles Roulette state and updates tooltip.
- [ ] In Minimized View (Emote view) → `[🎰]` icon disabled, tooltip says `"Only available for glamour"`.
- [ ] Clean build (`dotnet build`) with 0 errors/warnings.
