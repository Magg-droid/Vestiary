# Vestiary Architecture

> **Read this first when returning after a break.**
> It explains what we built, why, and how the pieces connect.

---

## What Is This?

Vestiary is a **Dalamud plugin** for FFXIV. It's a visual browser for Glamourer outfit designs.

Instead of scrolling through a text list of hundreds of designs, you see outfit cards with thumbnails, organized into collections. One click applies the outfit.

---

## Core Philosophy

| Principle | What it means |
|---|---|
| **Glamourer owns the designs** | We never create or modify Glamourer designs. We only read and apply. |
| **Vestiary owns the presentation** | Collections, thumbnails, nicknames — all ours. None of it touches Glamourer. |
| **Non-destructive** | Deleting a collection or thumbnail never affects the Glamourer design. |
| **Lightweight & optional** | Nothing is automatic. Users opt into collections, thumbnails, etc. |

---

## File Map

```
Vestiary/
├── Plugin.cs                         ← Entry point, lifecycle, P/Invoke, clipboard, file picker
├── Configuration.cs                  ← Persisted settings (collections, metadata, version)
├── RoseGoldTheme.cs                  ← All 56 UI colors — change here to re-theme
├── Strings.cs                        ← All user-facing strings — change here to reword
│
├── Models/
│   ├── Collection.cs                 ← Id, Name, FolderPaths, Order
│   └── DesignMetadata.cs             ← Nickname, CustomImagePath per design
│
├── Services/
│   ├── GlamourerService.cs           ← IPC bridge: GetDesignList, ApplyDesign, DeleteDesign
│   ├── CollectionService.cs          ← CRUD for collections, filters designs by folder path
│   ├── DesignMetadataService.cs      ← CRUD for per-design metadata (nicknames, image paths)
│   ├── TextureCache.cs               ← Caches loaded textures so gallery scrolls smoothly
│   ├── HiddenDesignService.cs        ← Hide/show designs (planned — currently inline in MainWindow)
│   ├── ThumbnailService.cs           ← Thumbnail file management (planned)
│   └── GalleryService.cs             ← Main orchestrator (planned)
│
├── Windows/
│   ├── MainWindow.cs
│   ├── ConfigWindow.cs               ← Settings: Apply Equipment Only, Show Hidden
│   ├── CollectionEditorWindow.cs     ← Create/edit collection popup (name + folder paths)
│   └── CameraWindow.cs               ← Full-screen 4:5 camera overlay with drag/resize
```

---

## Data Flow — The Big Picture

```
┌──────────────┐     IPC      ┌──────────────┐
│  Glamourer   │ ←──────────→ │   Vestiary   │
│  (external)  │              │              │
│              │  GetDesignList│              │
│  - designs   │─────────────→│  Glamourer   │
│  - folders   │              │  Service     │
│  - apply     │←─────────────│              │
│              │  ApplyDesign │              │
└──────────────┘              └──────┬───────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
                    ▼               ▼               ▼
            CollectionService  DesignMetadata   TextureCache
                    │               │               │
                    ▼               ▼               │
            Configuration.cs  Configuration.cs      │
            (List<Collection>)  (Dict<Guid,Metadata>)│
                    │               │               │
                    └───────┬───────┘               │
                            │                       │
                            ▼                       │
                      MainWindow.cs                 │
                      (tabs, gallery, cards) ◄──────┘
                            │
                    ┌───────┼───────┬──────────────┐
                    ▼       ▼       ▼              ▼
            Apply design  Edit    Delete    Thumbnail actions
            (Glamourer)  metadata  design   (camera/upload/clipboard)
```

**Key:** Nothing in `Windows/` directly calls Glamourer IPC. Everything goes through services. Services talk to `Configuration` for persistence.

---

## Design Decisions — The "Why"

### Why prefix-matching for folder paths?
Collection has path `"Dresses"` → matches `"Dresses/Summer"`, `"Dresses/Formal/Wedding"`.
Chose prefix over exact match because Glamourer folders are hierarchical.
Prefix means one collection can cover an entire folder tree.

### Why multiple folder paths per collection?
A "Dresses" collection can include both `"SFW/Dresses"` and `"NSFW/Dresses"`.
Users organize by concept, not by folder structure.

### Why no auto-import of Glamourer folders?
We deliberately don't auto-create collections from Glamourer folders.
Reason: users may have messy folder structures. Let them curate.
Changed from early Architecture plans which suggested auto-discovery.

### Why manual text input for folder paths (not checkboxes)?
MVP decision — checkbox list requires IPC round-trips and UI complexity.
Planned to upgrade to checkbox-based picker in future version.
Current text input is simple, works, but assumes users know their folder paths.

### Why persistent config folder for thumbnails?
`%appdata%/XIVLauncher/pluginConfigs/Vestiary/thumbnails/`
Not inside the plugin version folder. Reason: Dalamud creates a new folder for each version,
so thumbnails would be lost on every update if stored there.

### Why copy images, not reference paths?
If we referenced `C:\Users\...\screenshot.png`, it breaks when the file is moved.
Copying to our thumbnails folder means we own the file.

### Why SendInput (P/Invoke) for Scroll Lock?
FFXIV uses DirectInput, which ignores `keybd_event`.
Windows `SendInput` API is the only reliable way to simulate Scroll Lock for toggling game UI.

### Why ImDrawList for tabs and cards?
ImGui widgets (TabBar, TabItem) don't support our custom look (top-rounded tabs, floating "+" button, gear icon).
DrawList gives pixel-perfect control but requires manual hit-testing with InvisibleButton.
Cost: no built-in accessibility, careful cursor management needed.

### Why RoseGoldTheme.cs and Strings.cs?
Extracted to avoid magic values scattered across files.
Enables future theme switching and localization.
See [CodingStandards.md](CodingStandards.md) for usage rules.

---

## Known Fragile Areas

These are things that broke before and may break again:

| Area | What went wrong | How we fixed it |
|---|---|---|
| **Tab bar cursor** | When no tabs exist, the drawn line and "+" button had zero height. Content overlapped the tab bar. | `maxTabH` has a minimum fallback based on font height. |
| **Window drag breaks gallery** | Using `SetCursorPosY()` with absolute positioning broke ImGui's relative layout when the window was resized. | Replaced with conditional `Dummy()` that only pushes when needed. Never use `SetCursorPosY` in window content. |
| **Thumbnails lost on update** | Images stored in versioned plugin folder disappeared on version bump. | Migrated to persistent config folder with automatic migration. |
| **Camera overlay window bleeding** | Other windows rendered behind the camera overlay because their `Draw()` wasn't suppressed. | `IsCameraActive` flag in Plugin.cs gate-checks every window's `Draw()`. |
| **IPC failures** | Glamourer may not be installed or may fail mid-call. | All IPC calls wrapped in try/catch. Gallery shows "Glamourer not found" message. |

---

## Coding Standards

See [CodingStandards.md](CodingStandards.md) — covers theme colors, strings, file organization,
naming conventions, ImGui ID rules, and the service pattern.

---

## Roadmap to v1.0

See [Architecture.md#TODO](#todo-for-future-releases) for the current list.

Priorities for next releases:
1. Performance (lazy-load thumbnails, cap texture cache)
2. UX polish (search/filter, favorites)
3. Onboarding (first-run guide)
4. Checkbox-based folder picker
5. Testing & beta feedback

---

*Last updated: v0.5.0.0*
