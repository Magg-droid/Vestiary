# Vestiary

A visual companion for Glamourer.

Vestiary turns your Glamourer design list into a visual gallery so you can browse, organize, and apply outfits faster.

- Current internal plugin version: 1.1.4.0

## What Vestiary Does

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

Vestiary does not replace Glamourer. Glamourer remains the source of truth for designs.

## Installation

Add this repo in Dalamud custom repositories:

https://raw.githubusercontent.com/Magg-droid/plugin-collection/main/pluginmaster.json

1. Open Dalamud settings
2. Go to Experimental / Custom Plugin Repositories
3. Add the raw pluginmaster URL above
4. Open Plugin Installer and install Vestiary

## Commands

- /vestiary: Open Vestiary
- /vs: Shortcut to open Vestiary
- /vsguide: Open Vestiary guide window
- /vsemotes: Open directly to Emotes view
- /vsrandom: Apply random visible outfit from all non-Favorites collections
- /vsrandom [Collection Name]: Apply random visible outfit from a specific collection

Random command behavior:

- Hidden outfits are always excluded
- /vsrandom excludes Favorites by design
- Apply Equipment Only setting is respected
- Immediate repeat picks are avoided when more than one outfit is available

## Main Features

### Create Collection 
When you first land on the plugin you will be greeted by this screen, create a new collection to get started

<img width="458" height="370" alt="image" src="https://github.com/user-attachments/assets/2b3b7de7-008c-4c24-ad59-f9d358feafad" />

### Glamour Gallery

<img width="1375" height="1144" alt="ChatGPT Image Aug 10, 2026, 10_00_08 PM" src="https://github.com/user-attachments/assets/4f67586e-e6b9-44ae-925e-fdc5a22a2cb4" />


### Emotes View

<img width="689" height="524" alt="image" src="https://github.com/user-attachments/assets/67fa93e3-1b71-4811-a80e-aa5ac97b6e50" />


### Glamour Roulette
<img width="686" height="523" alt="image" src="https://github.com/user-attachments/assets/d769cb93-652d-4f4d-b0ae-bd7a1d059f43" />


### UI / Quality of Life

- Minimized menu navigation for both Glamour and Emotes collections
- Multiple themes

<img width="506" height="466" alt="image" src="https://github.com/user-attachments/assets/12bda415-c881-4633-b4be-fea4c2ea50f2" />


## Guide & Help

- Open the in-plugin guide from the Browse rail via Help
- Command alternative: /vsguide
- Report issues and requests: [GitHub Issues](https://github.com/Magg-droid/Vestiary/issues)

## Feedback

- Discord: megunim.
- Bug reports and suggestions: [GitHub Issues](https://github.com/Magg-droid/Vestiary/issues)

## Credits

- Dalamud
- Glamourer
- Penumbra
- FFXIV plugin community
