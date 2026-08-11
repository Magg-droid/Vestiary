# Emote Gallery — Plan

> Extension of Vestiary beyond Glamourer designs.  
> v1.1.0 planning phase.

---

## Concept

A gallery of emote/animation cards. Each card links to one Penumbra mod (the emote mod). Clicking the card restores the correct mod setup so you can play that emote without accidental NSFW/surprise mods.

### User flow

1. Create a card — pick a Penumbra mod from a dropdown, name it, add a thumbnail
2. Set up Penumbra mods how you want for that emote
3. Click 💾 to save the mod state
4. Later: double-click the card → mods restore → user types the emote
5. Switch to another card → its mods restore

---

## Architecture

### View

A toggle in MainWindow header: **Glamour** | **Emotes**

Emote view is a flat gallery grid (no collections/tabs). Each card has:
- Thumbnail
- Name (editable)
- 💾 Save Mods icon (capture/restore)
- Double-click → restore mods

### Model

```csharp
EmoteCard
├── Id: Guid
├── Name: string
├── ModDirectory: string          // the linked Penumbra mod
├── ThumbnailPath: string?
└── Mods: List<ModEntry>          // snapshot (reuse ModEntry from Save Mods)
```

### Capture (💾 click)

1. Get the chosen mod's changed items → find which animation/action identifiers it modifies
2. Find ALL other mods that touch those same identifiers
3. Snapshot: chosen mod = ON, all conflicting = OFF
4. Save in config

### Restore (double-click / apply)

1. Read snapshot → linked mod directory + saved mods
2. Get ALL mods touching the same animation identifiers (bulk IPC)
3. Chosen mod → enable
4. All other matching mods → disable (catches new mods)
5. Log: "Emote restored — Mod A on, 3 mods off"

### Create/Edit

- Dropdown of Penumbra mods (filter to animation-related if possible)
- Name field
- Thumbnail upload (reuse existing camera/upload/clipboard)
- Save to config

---

## Reuse from Save Mods

| Component | Reused? |
|---|---|
| `ModStateService` — Penumbra IPCs | ✅ All wired |
| `ModSnapshot` / `ModEntry` models | ✅ Reuse |
| `PenumbraService` — bulk IPCs | ✅ |
| Card layout + icons | ✅ Same rendering |
| `ThemeManager` | ✅ |
| 💾 icon + gold state | ✅ |
| New mod detection | ✅ Same logic |

---

## What's New

| Component | Purpose |
|---|---|
| `EmoteCard` model | Card data |
| `EmoteService` | CRUD + capture/restore |
| Emote gallery panel | Flat grid |
| Emote editor | Create/edit card UI |
| View toggle | Glamour ↔ Emotes in header |
| `/vsemotes` command | Quick open to emote view |

---

## What's NOT in Scope (v1)

- Hub view with plates (future)
- Hair/skin/tattoo galleries (future)
- Collections/tabs for emotes (flat only)
- "Play" button (just restore mods, user does /emote)
- Automatic emote/animation detection from mod files

---

## Steps

| Step | What |
|---|---|
| 1 | `EmoteCard` model + add to `Configuration` |
| 2 | `EmoteService` — CRUD, capture, restore |
| 3 | View toggle button in header (Glamour | Emotes) |
| 4 | Emote gallery panel — flat grid of cards |
| 5 | 💾 icon on emote cards (capture) |
| 6 | Double-click restore on emote cards |
| 7 | Create/Edit emote window (mod dropdown + name + thumbnail) |
| 8 | `/vsemotes` command |
| 9 | Coding standards review, bump, release |
