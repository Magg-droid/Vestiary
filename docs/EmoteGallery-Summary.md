# Emote Gallery — Session Summary (v1.0.0.2 Beta)

## What We Built

🎭 **Emote Gallery** — create cards linked to FFXIV emotes. Save mod state per card, auto-restore on apply, auto-play the emote.

---

## Architecture

### Emote Card
```
EmoteCard
├── Id: Guid
├── Name: string
├── EmoteName: string       // from FFXIV Emote sheet
├── ThumbnailPath: string?
└── Mods: List<ModEntry>    // reuses Save Mods model
```

### Capture (💾 click)
1. Get all mods touching the emote's identifiers (`"Emote: Name"` + `"Action: Name"`)
2. Snapshot current Penumbra state (enabled/disabled + priority + options)
3. Store in config

### Restore (Apply / double-click)
1. Set each mod's enabled state
2. Apply priority + option settings (always, even if mod was already ON)
3. Play emote via ECommons `Chat.SendMessage`

---

## Key Files

| File | Purpose |
|---|---|
| `Models/EmoteCard.cs` | Card data model |
| `Services/EmoteService.cs` | CRUD + capture/restore + Penumbra IPC |
| `Windows/MainWindow.EmoteGallery.cs` | Full emote gallery UI (tab, grid, cards, edit) |
| `Windows/MainWindow.cs` | Pill toggle + view branch + `_pendingEmoteCommand` queue |
| `Configuration.cs` | `EnableEmotes` + `EmoteCards` lists |
| `Strings.cs` | `SettingsEnableEmotes` + `SettingsEnableEmotesTooltip` |
| `Windows/ConfigWindow.cs` | Enable Emotes (Beta) checkbox |

---

## Emote Gallery Layout

### Tab bar (matches glamour style exactly)
```
tabPadX=14f, tabPadY=6f, tabRounding=6f
Tab: "All Emotes" — always selected, +2f taller
Settings button: 90x26, FrameRounding 4f, FramePadding(8f,1f), EditBtn colors
Border: TabBorderLine 1.5f, full content width
38px gap below border → gallery grid
```

### Pill toggle (Glamour | Emotes)
```
iOS-style segmented control: 180x32, 2px inset
Active: TabSelected fill + TabTextActive text
Inactive: transparent fill + TextSubtle text
FrameRounding: pillH/2, FramePadding(0,2f), ItemSpacing(0,0)
2px gap → TabBorderLine separator 1.5f
```

### Card layout (260x400, 240x300 thumbnail)
```
Name → 24px gap → Emote → 28px gap → Buttons
Inputs: FramePadding(4f,-1f), width-20f (240px)
Buttons: 28px tall, Apply=ApplyBtn green, Save/Edit=grey, Delete=red
All elements aligned at X=10f (thumbnail left edge)
```

### Features gated behind `plugin.Configuration.EnableEmotes` (default OFF)
When off: no pill, no emote view, plugin behaves exactly as v1.0.0.0

---

## Emote Auto-Play (ECommons)

1. Apply button sets `_pendingEmoteCommand = ecmd`
2. `ProcessPendingEmote()` hooked to `UiBuilder.Draw` picks it up next frame
3. Calls `Chat.SendMessage(cmd)` — same approach as OpenToD plugin

```
Plugin.cs: ECommonsMain.Init / Dispose
EmoteGallery.cs: Chat.SendMessage via pending queue
Wardrobe.csproj: <PackageReference Include="ECommons" Version="*" />
```

---

## Emote List Source
FFXIV game data via `Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Emote>()`
- Shows all emotes (not filtered to unlocked)
- Search dropdown in edit mode
- Emote name stored on card, command looked up from TextCommand link

---

## Known Limitations (Beta)
- All FFXIV emotes shown (not just unlocked ones)
- No "play" animation on the card itself
- Emote command hint not shown on card (auto-plays instead)
- Card thumbnails don't render actual emote preview

---

## Current Version
**v1.0.0.2** — Emote Gallery (Beta)

---

## TODO / Future Ideas

### 🔍 Search & Filter
- [ ] Search icon/bar to filter emote cards by name (match glamour gallery)
- [ ] Filter by emote category (Dance, Expression, Pose from Emote sheet)

### 🏷️ Card Management
- [ ] Delete confirmation dialog
- [ ] Card count in header ("12 emote cards")
- [ ] Reorder cards via drag-and-drop
- [ ] Favorite/star icon on cards

### 🎨 Visual
- [ ] Emote preview thumbnail (auto-capture on first play)
- [ ] Category color coding on cards

### ⚡ Bulk Operations
- [ ] "Save All" — capture mods for all cards at once
- [ ] "Restore All" — restore mods for all cards at once

### 📋 Import/Export
- [ ] Export emote card setups to share
- [ ] Import from other users

### 🔧 Mod Info
- [ ] Hover tooltip showing which mods are active per card
- [ ] Show mod count on card ("3 mods")

### 🎬 Emote Playback
- [ ] "Play" button plays emote without switching mods
- [ ] Playlist/queue mode for multiple emotes

### 🎯 Quality of Life
- [ ] Better empty state (not just + card)
- [ ] Card right-click menu (Delete, Rename, etc.)
- [ ] Undo delete
- [ ] Keyboard shortcuts

