# Vestiary Coding Standards

> 📁 **Context:** [Architecture.md](Architecture.md)

> These standards are lightweight and practical — they exist to keep the codebase maintainable as it grows, not to add ceremony.

---

## 0. Core Architecture Principle

### Thin Windows, Fat Services

> **Windows display data. Services perform actions.**

This project follows an Angular-inspired service-oriented architecture. The main principle:

- **Windows** should only draw ImGui UI, display data, and forward user actions.
- **Windows** should **never** contain business logic, IPC calls, configuration manipulation, or file operations.
- **Services** contain all application logic and are injected into windows through the constructor.

```csharp
// ❌  Bad — business logic in the window
if (ImGui.Button("Hide"))
{
    configuration.HiddenDesignIds.Add(designId);
    configuration.Save();
    RefreshGallery();
}

// ✅  Good — window delegates to a service
if (ImGui.Button("Hide"))
{
    galleryService.HideDesign(designId);
}
```

### One Responsibility Per Service

Each service owns a specific feature domain:

| Service | Owns |
|---|---|
| `GlamourerService` | All IPC communication |
| `CollectionService` | Collection CRUD |
| `HiddenDesignService` | Hide/show designs |
| `ThumbnailService` | Thumbnail file management |
| `CameraService` | Camera capture flow |
| `GalleryService` | Orchestrates multiple services for the gallery |

### Service Dependency Direction

```
MainWindow
      │
      ▼
GalleryService      ← Main orchestrator
      │
      ├── GlamourerService
      ├── CollectionService
      ├── HiddenDesignService
      └── ThumbnailService
```

- Windows depend on services. Services never depend on windows.
- The UI never talks directly to configuration or IPC.
- `GalleryService` is the main entry point for window interactions.

### Feature Development Pattern

For every new feature, create:

1. **Model** (if state is needed)
2. **Service** (all logic)
3. **UI** (rendering only)

Example — adding "Favorites":

```
Models/FavoriteDesign.cs
Services/FavoriteService.cs
Windows/UI   → Favorite button in DesignCard
```

### UI State

UI-only state should live in a dedicated model, not scattered across window fields:

```csharp
public class GalleryState
{
    public Guid SelectedCollectionId;
    public bool ShowHidden;
    public string SearchText = "";
    public GallerySortMode SortMode;
}
```

---

## 1. Theme Colors

**All UI colors live in `Vestiary/RoseGoldTheme.cs`.**

- Never hardcode a `Vector4` color in window code.
- Import `RoseGoldTheme` and use its named constants.
- Adding a future theme (e.g., `OceanTheme`) means copying the file, tweaking values, and toggling in config.

```csharp
// ❌  Don't
ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.7f, 1f), "Hello");

// ✅  Do
ImGui.TextColored(RoseGoldTheme.TextHeading, "Hello");
```

| Property naming | Example |
|---|---|
| `Text*` | `TextHeading`, `TextMuted`, `TextSubtle`, `TextError` |
| `Tab*` | `TabSelected`, `TabHovered`, `TabBorderLine` |
| `Card*` | `CardBg`, `CardBorder` |
| `*Btn / *BtnHover / *BtnActive` | `ApplyBtn`, `CtaBtnHover`, `DeleteBtnActive` |

---

## 2. User-Facing Strings

**All user-facing strings live in `Vestiary/Strings.cs`.**

- Labels, tooltips, button text, hint text, error messages — everything.
- For strings with dynamic values, use a method:

```csharp
// ❌  Don't
ImGui.Text($"✓ {count} design(s) match these folders");

// ✅  Do
ImGui.Text(Strings.ColDesignsMatch(count));
```

| Naming | Example |
|---|---|
| `[Section][Purpose]` | `EmptyHeading`, `ColNameLabel`, `TabRightClickTooltip` |
| Methods for parametrized | `ColDesignsMatch(int count)` |

---

## 3. File Organization

```
Vestiary/
├── RoseGoldTheme.cs      ← all colors
├── Strings.cs            ← all user-facing strings
├── Plugin.cs             ← plugin lifecycle (keep lean)
├── Configuration.cs      ← persisted settings
├── Models/
│   ├── Collection.cs
│   └── DesignMetadata.cs
├── Services/
│   ├── GlamourerService.cs
│   ├── GalleryService.cs
│   ├── CollectionService.cs
│   ├── HiddenDesignService.cs
│   ├── ThumbnailService.cs
│   ├── CameraService.cs
│   └── TextureCache.cs
├── Windows/
│   ├── MainWindow.cs
│   ├── ConfigWindow.cs
│   ├── CollectionEditorWindow.cs
│   ├── DesignEditorWindow.cs
│   └── CameraWindow.cs
└── UI/
    ├── DesignCard.cs
    ├── CollectionTabs.cs
    └── SharedStyles.cs
```

### When files get too large

Use **C# partial classes** to split a window into focused files without changing behavior:

```
Windows/
├── MainWindow.cs              (fields, constructor, Dispose)
├── MainWindow.TabBar.cs       (DrawTabBar, drag-drop)
├── MainWindow.Gallery.cs      (grid layout)
├── MainWindow.DesignCard.cs   (card rendering, buttons)
└── MainWindow.EmptyState.cs   (empty-state CTA)
```

Do this only when a file crosses ~500 lines and has clearly separable concerns.

---

## 4. Service Pattern

Services are plain classes injected through the constructor:

```csharp
public class MainWindow : Window
{
    private readonly GalleryService galleryService;

    public MainWindow(GalleryService galleryService)
    {
        this.galleryService = galleryService;
    }
}
```

- Keep services **stateless where possible**.
- Save/load goes through `Configuration` (which calls `configuration.Save()`).
- No service depends on a Window — windows depend on services.
- Prefer **clarity over cleverness**. Use descriptive names and explicit intermediate variables.
- Avoid overly compact LINQ when readability suffers.

---

## 5. DrawList vs. Widgets

ImGui has two ways to draw:

| Approach | Use for |
|---|---|
| **Widgets** (`ImGui.Button`, `ImGui.Text`, etc.) | Interactive elements, text, layout |
| **DrawList** (`dl.AddRectFilled`, `dl.AddText`, etc.) | Custom painted elements (tabs, cards, lines) |

**Rule:** Prefer widgets for standard UI. Use DrawList only when you need custom shapes or precise screen-space positioning.

---

## 6. Naming Conventions

| Thing | Convention | Example |
|---|---|---|
| Private fields | `camelCase` | `selectedCollectionId` |
| Public properties | `PascalCase` | `GlamourerService` |
| Methods | `PascalCase` | `DrawDesignCard()` |
| Constants | `PascalCase` | `TabRounding` |
| ImGui IDs | `##prefix_description` | `##tab_{id}`, `##EmptyState` |
| Services | `*Service` suffix | `CollectionService` |
| Windows | `*Window` suffix | `CollectionEditorWindow` |

---

## 7. Error Handling

- IPC calls to Glamourer are wrapped in `try/catch` — Glamourer may not be installed.
- Log errors via `Plugin.Log.Error(...)`.
- Don't crash the plugin; show a graceful message to the user.

```csharp
try
{
    var designs = plugin.GlamourerService.GetDesignList();
}
catch
{
    // Glamourer unavailable — handled gracefully upstream
}
```

---

## 8. ImGui IDs

- Always use `##` hidden labels for widgets that don't need visible text.
- Include a unique identifier (like `designId` or `collection.Id`) in IDs inside loops:

```csharp
// ❌  Don't — all buttons share the same ID
ImGui.Button("Apply");

// ✅  Do
ImGui.Button($"Apply##btn_apply_{designId}");
```

---

*Last updated: v0.5.0.0 — this document grows as patterns solidify.*
