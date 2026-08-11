# Vestiary — Feature List

> What the user gets. Updated with each release.

---

## Browse & Apply

- Browse Glamourer designs as visual cards with thumbnails
- Apply a design — double-click the thumbnail or click the Apply button
- Equipment-only mode: toggle in Settings, or hold Ctrl while applying
- Search bar — filter designs in the current tab by nickname or name
- `/vestiary` and `/vs` slash commands to open the plugin

---

## Collections

- Create, edit, and delete collections to organize designs
- Drag and drop tabs to reorder collections
- Filter designs by Glamourer folder paths (prefix-match, multiple folders per collection)
- Uncategorized fallback when no folders are specified

---

## Thumbnails

- **Camera snapshot** — 4:5 overlay with movable/resizable viewfinder, auto-hides game UI
- **Upload from file** — browse your disk for any image
- **Paste from clipboard** — paste image data or copy a file from File Explorer
- Custom thumbnails persist across plugin updates (stored in plugin configs folder)

---

## Organize Designs

- **Hide** a design — removes it from the gallery without deleting it from Glamourer
- **Show Hidden** mode — toggle to browse hidden designs at reduced opacity
- Eye icon in the header for quick Show Hidden switching
- **Unhide** a design to restore it to the gallery
- **Delete from Glamourer** — available on hidden cards with Ctrl+Click and confirmation popup

---

## Design Cards

- Inline rename — double-click the design name to set a nickname (clear to reset)
- Action icons: camera snapshot, file upload, clipboard paste
- **Favourites** — star icon (top-left), golden when favourited, auto-creates "Favorites" collection tab
- **Save Mods** — floppy disk icon (top-left, below star), golden when saved
  - Left-click: capture current Penumbra mod state for this outfit
  - Right-click: clear saved mods
  - Auto-restore on apply — enables/disables mods to match saved state
  - Catches new mods — disables mods added since capture
- Tooltips on all actions
- Card hover highlights and icon highlights with interaction gating (no bleed-through from overlaying windows)

---

## UI & Experience

- Four themes: Classic (warm grey), Ocean (grey-blue), Midnight Purple (grey-lavender), Forest (charcoal sage)
- Live theme switching via radio buttons in Settings — no reload
- Empty-state wizard for new users (icon + heading + call-to-action button)
- Centered "No designs" / "No hidden designs" headings when galleries are empty
- Favorites collection — appears on first favourite, removed when empty, always last tab
- Settings window with toggles: Apply Equipment Only, Show Hidden, Theme
- Search bar — magnifying glass icon inside input, live filtering, theme-styled
