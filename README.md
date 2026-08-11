# 🧥 Vestiary

**A visual gallery for your Glamourer outfits.**

Tired of scrolling through a long text list of designs? Vestiary turns your Glamourer library into a beautiful grid of thumbnail cards — browse, organize, and swap outfits in one click.

<br>

<p align="center">
  <sub>Built for <a href="https://github.com/goatcorp/Dalamud">Dalamud</a> · Companion to <a href="https://github.com/Ottermandias/Glamourer">Glamourer</a></sub>
</p>

---

## ✨ At a Glance

| Feature | |
|---|---|
| 🖼️ **Visual gallery** | Every design gets a card with a thumbnail you can customize |
| 📁 **Collections** | Group outfits however you like — Dresses, Tanks, Seasonal, whatever |
| ⭐ **Favorites** | Star the ones you reach for most |
| 🙈 **Hide designs** | Clean up your gallery without deleting anything from Glamourer |
| 🎲 **Random Pick** | Feeling indecisive? Hit one button |
| 🎰 **Glamour Roulette** | Timer-based auto-swapping through your wardrobe |
| 🎭 **Emotes view** | Emote cards with per-emote Penumbra mod states |
| 💾 **Save Mods** | Outfits remember their Penumbra setup and restore it on apply |
| 🎨 **Themes** | Classic, Ocean, Midnight Purple, Forest |
| ⚡ **Minimized mode** | Compact floating bar for quick swaps |

---

## 📦 Installation

1. In Dalamud, go to **Settings → Experimental → Custom Plugin Repositories**
2. Paste this URL and click **+** :

   ```
   https://raw.githubusercontent.com/Magg-droid/plugin-collection/main/pluginmaster.json
   ```

3. Open the **Plugin Installer**, search for **Vestiary**, and install

> **Requires:** Glamourer · **Recommended:** Penumbra (for Save Mods and emote state restore)

---

## ⌨️ Commands

| Command | What it does |
|---|---|
| `/vestiary` or `/vs` | Open Vestiary |
| `/vsguide` | Open the built-in guide |
| `/vsemotes` | Jump straight to Emotes view |
| `/vsrandom` | Apply a random outfit from all collections |
| `/vsrandom Dresses` | Apply a random outfit from the named collection |

Hidden designs are always skipped · Favorites get excluded from random by default · Holds Ctrl on apply for equipment-only swap.

---

## 🎯 How It Works

Vestiary **does not touch** your Glamourer designs. It reads them through Glamourer's IPC, displays them as cards, and applies them when you click. Collections, nicknames, thumbnails, favorites — that's all Vestiary. Your Glamourer setup stays exactly as you left it.

---

## 🐛 Help & Feedback

- Open the in-plugin guide from the sidebar, or type `/vsguide`
- Report bugs or suggest features on [GitHub Issues](https://github.com/Magg-droid/Vestiary/issues)
- Reach me on Discord: **megunim.**

---

## 🙏 Credits

Vestiary wouldn't exist without these incredible projects:

- [Dalamud](https://github.com/goatcorp/Dalamud) — XIVLauncher's plugin framework
- [Glamourer](https://github.com/Ottermandias/Glamourer) — the source of truth for designs
- [Penumbra](https://github.com/xivdev/Penumbra) — runtime modding
- The FFXIV plugin community 💜
