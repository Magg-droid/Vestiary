# Wardrobe

A visual companion for Glamourer.

Wardrobe turns your Glamourer design list into a visual gallery so you can browse, organize, and apply outfits faster.

- Current internal plugin version: 1.0.0.6

## What Wardrobe Does

- Shows Glamourer designs as thumbnail cards
- Lets you organize outfits into collections
- Supports favorites, hide/unhide, and search
- Applies outfits with one click or double-click
- Supports equipment-only apply mode
- Can save/restore Penumbra mod state per outfit
- Includes Emotes view with emote collections and emote cards
- Includes Glamour Roulette for timed outfit randomization

## Requirements

- XIVLauncher / Dalamud
- Glamourer (required)
- Penumbra (recommended for Save Mods and emote state restore features)

Wardrobe does not replace Glamourer. Glamourer remains the source of truth for designs.

## Installation

Add this repo in Dalamud custom repositories:

https://raw.githubusercontent.com/Magg-droid/plugin-collection/main/pluginmaster.json

1. Open Dalamud settings
2. Go to Experimental / Custom Plugin Repositories
3. Add the raw pluginmaster URL above
4. Open Plugin Installer and install Wardrobe

## Commands

- /wardrobe: Open Wardrobe
- /wr: Shortcut to open Wardrobe
- /wrguide: Open Wardrobe guide window
- /wremotes: Open directly to Emotes view
- /wrrandom: Apply random visible outfit from all non-Favorites collections
- /wrrandom [Collection Name]: Apply random visible outfit from a specific collection

Random command behavior:

- Hidden outfits are always excluded
- /wrrandom excludes Favorites by design
- Apply Equipment Only setting is respected
- Immediate repeat picks are avoided when more than one outfit is available

## Main Features

### Glamour Gallery

- Visual card grid with thumbnail preview
- Upload thumbnail from file or clipboard
- Snapshot capture flow
- Favorites collection support
- Hide/Unhide without deleting from Glamourer
- Optional delete from Glamourer with safety flow
- Search across collections
- Random Pick button for selected collection

### Emotes View

- Emote cards with optional thumbnails
- Save and restore Penumbra mod state per emote card
- Emote collections with chip tabs and + create chip
- Move cards between emote collections
- Search support

### Glamour Roulette

- Timer-based random outfit swapping
- Manual Swap Now trigger
- Collection include/exclude controls
- Hidden designs excluded from the pool
- Respects Apply Equipment Only
- Avoids immediate repeat picks when possible

### UI / Quality of Life

- Full view and minimized view
- Minimized menu navigation for both Glamour and Emotes collections
- Multiple themes
- Keyboard-assisted equipment-only apply (Ctrl)

## Troubleshooting

- If cards do not appear, verify Glamourer is installed and enabled.
- If Save Mods does not restore as expected, verify Penumbra is running and your player collection is available.

## Guide & Help

- Open the in-plugin guide from the Browse rail via Help
- Command alternative: /wrguide
- Report issues and requests: https://github.com/Magg-droid/Wardrobe/issues

## Feedback

- Discord: megunim.
- Bug reports and suggestions: https://github.com/Magg-droid/Wardrobe/issues

## Credits

- Dalamud
- Glamourer
- Penumbra
- FFXIV plugin community
