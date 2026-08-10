# Minimized View — Plan

> Quick-access floating thumbnail grid. Hide all chrome, shrink cards, persist the window.

---

## Concept

A toggleable compact mode that strips the main window down to just thumbnails. The user keeps it pinned on screen as a quick-access outfit launcher — double-click a thumbnail to apply, everything else hidden.

---

## User Flow

1. User clicks a "Minimize" button in the rail (or presses a shortcut like `/wrm` for "Wardrobe mini")
2. Window shrinks: rail, top bar, chips, search, status row all disappear
3. Only the card grid remains, with cards at ~50% normal size
4. A small "Expand" button in the top-right corner restores normal mode
5. Double-click a card → applies the outfit (same as normal mode)
6. Window can be resized, moved, and stays where the user puts it
7. Minimized state persists across plugin reload / game restart

---

## Architecture

### State

Add to `Configuration.cs`:

```csharp
public bool IsMinimized { get; set; } = false;
```

### Toggle

| Trigger | Action |
|---|---|
| Rail button: "Minimize" (with collapse icon) | Sets `Configuration.IsMinimized = true`, saves |
| Top-right "Expand" button (only visible when minimized) | Sets `Configuration.IsMinimized = false`, saves |
| `/wrm` command (optional) | Toggles minimized state

The toggle lives in `Plugin.cs` (config-backed) and is read by `MainWindow.Draw()`.

---

## What Gets Hidden

When `Configuration.IsMinimized == true`, the `Draw()` method skips:

| Component | Method | Hidden? |
|---|---|---|
| Browse rail | `DrawRail()` | ✅ Hidden |
| Top bar (Browse + search) | `DrawTopBar()` | ✅ Hidden |
| Separator line | inline in `Draw()` | ✅ Hidden |
| Chip row + status | `DrawChipAndStatusRow()` | ✅ Hidden |
| Collection selector | — | Still active (uses `selectedCollectionId`) |
| Card action icons | `DrawActionIcon()` calls | ✅ Hidden |
| Card buttons (Apply/Edit/Hide) | Inside `DrawDesignCard()` | ✅ Hidden |
| Favorites star | Inside `DrawDesignCard()` | ✅ Hidden |
| Save mods icon | Inside `DrawDesignCard()` | ✅ Hidden |
| Add emote card | `DrawAddEmoteCard()` | ✅ Hidden |

### What stays visible

- Design name (below thumbnail)
- Thumbnail image
- Double-click to apply
- A small "Expand" button floating top-right
- Window title bar (native ImGui window chrome)

---

## Card Dimensions

| Mode | Card size | Thumbnail | Spacing |
|---|---|---|---|
| Normal | 260 × 400 | 240 × 300 | 25px gap |
| Minimized | 110 × 155 | 100 × 125 | 8px gap |

Thumbnails maintain the same 4:5 aspect ratio as normal mode (240×300 → 100×125).
The thumbnail is loaded once and scaled by ImGui — no distortion, same source image.
At 100×125, each card is ~25% the area of a normal card. In a compact window (~500px wide)
you get 4 cards per row vs. ~2 in normal mode.

---

## Window Behavior

### Size constraints (minimized mode)

```csharp
// Normal
MinimumSize = new Vector2(375, 330);
MaximumSize = new Vector2(float.MaxValue, float.MaxValue);

// Minimized — allow tiny windows
MinimumSize = new Vector2(180, 150);
MaximumSize = new Vector2(float.MaxValue, float.MaxValue);
```

The window should still be resizable. User can make it a small square or a wide strip.

### Window flags

In minimized mode, consider adding:
- `ImGuiWindowFlags.NoScrollbar` on the outer window (inner gallery child handles scroll)

No `AlwaysAutoResize` — user controls size.

### Position persistence

ImGui/Dalamud windows already remember position via `WindowSystem`. No extra work needed.

---

## Minimized Header

A small overlay in the top-right of the content area:

```
┌──────────────────────────────┐
│ [card] [card] [card]    [↔] │  ← expand button
│ [card] [card] [card]        │
│ [card] [card]               │
└──────────────────────────────┘
```

- 24×24 icon button, semi-transparent background
- Hover: full opacity, tooltip "Expand"
- Uses existing icon or a simple text "↔" / "□"
- Positioned via `ImGui.SetCursorPos` at top-right of content area

---

## Collection Switching in Minimized Mode

### Option A: Keep current collection only (simplest)

The minimized view shows whatever collection was selected before minimizing. To change collections, user must expand, switch, then minimize again.

**Pros:** Zero additional UI. Clean.
**Cons:** Extra click to switch collections.

### Option B: Tiny dropdown in minimized header

Add a small combo/dropdown next to the expand button listing collections.

**Pros:** Full functionality in minimized mode.
**Cons:** More UI in an already minimal view.

**Decision:** Start with Option A (keep current collection). Add Option B later if users ask for it.

---

## Emote Gallery in Minimized Mode

If the user is on the Emotes view and minimizes, the same rules apply:

- Hide rail, top bar, chips, status
- Show emote cards at reduced size (140×200)
- Hide save/edit/delete icons
- Double-click still restores mods + plays emote
- Expand button still visible

---

## Implementation Steps

| Step | What | File(s) |
|---|---|---|
| 1 | Add `IsMinimized` to `Configuration` | `Configuration.cs` |
| 2 | Add `RailMinimize` string | `Strings.cs` |
| 3 | Add "Minimize" button to rail | `MainWindow.Rail.cs` |
| 4 | Branch `Draw()`: skip chrome when minimized | `MainWindow.cs` |
| 5 | Adjust size constraints when minimized | `MainWindow.cs` (constructor or Draw) |
| 6 | Add minimized card rendering (smaller, no actions) | `MainWindow.DesignCard.cs` |
| 7 | Add expand button overlay in top-right | `MainWindow.cs` (or new partial) |
| 8 | Handle window flags change on minimize/expand | `MainWindow.cs` |
| 9 | Build, test, bump, release | — |

---

## What's NOT in Scope

- Favorites-only minimized view
- Emote-only minimized view
- Collection switching dropdown (v1 → Option A)
- Pinning the window always-on-top (Dalamud window system doesn't support this natively)
- Tray/minimap icon integration
- Transparency/opacity settings
- Auto-hide when entering combat/duty

---

## Design Decisions

| Decision | Rationale |
|---|---|
| Config-backed toggle, not UI-only state | Survives plugin reload, matches existing pattern (ShowHidden, EnableEmotes) |
| Double-click still applies | Core value — quick access. No buttons needed. |
| No collection switching in v1 | Keeps minimized mode truly minimal. Easy to add later. |
| Same DrawDesignCard with bool param | Avoid duplicating card rendering. Just gate size + visibility. |
| Expand button uses draw list, not ImGui window | Keeps it inside the content area, moves with scroll. |

---

## Verification

- [ ] Toggle minimize from rail → window shrinks, chrome hides, cards resize
- [ ] Toggle expand → window restores, chrome returns, cards resize
- [ ] Double-click card in minimized mode → outfit applies
- [ ] State persists across `/vestiary` close/reopen
- [ ] State persists across plugin reload
- [ ] Works with collections (shows correct collection's designs)
- [ ] Works with emote gallery
- [ ] Window resizable to small sizes in minimized mode
- [ ] No compile errors, no regressions in normal mode
