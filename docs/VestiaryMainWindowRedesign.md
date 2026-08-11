## Plan: Vestiary Main Window Visual Redesign

Use the first two screenshots as the current Vestiary UI and the third screenshot as the visual target. The goal is a UI-only redesign that keeps the exact same features and interactions we already have. No pagination, no new workflow, no new feature set, and no service or data-model changes.

The redesign should follow the updated wireframe: a left browse rail with Glamour, Emotes, and Settings; a top search row with the Show hidden checkbox and count; and a chip row with All, Favorites, Recent, Imported, and + above the same card grid. Only layout, spacing, hierarchy, colors, card styling, and polish should change.

**Text Wireframe**

```text
CURRENT WARDROBE INPUTS
- Screenshot 1 and 2 = current Vestiary UI
- Screenshot 3 = visual target only

TARGET WARDROBE LAYOUT

┌──────────────────────────────────────────────────────────────────────────────────────────────┐
│ WARDROBE                                                             [ Search designs...]    │
├──────────────────────┬───────────────────────────────────────────────────────────────────────┤
│                      │                                                                       │
│ Browse               │ ☐ Show hidden                                       124 designs       │
│──────────────────────├───────────────────────────────────────────────────────────────────────┤
│ - Glamour            │ [ All ] [ Favorites ] [ Recent ] [ Imported ] [+]                     │
│ - Emotes             ├───────────────────────────────────────────────────────────────────────┤
│ - Settings           │                                                                       │
│                      │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐          │
│                      │  │ Thumbnail  │ │ Thumbnail  │ │ Thumbnail  │ │ Thumbnail  │          │
│                      │  │            │ │            │ │            │ │            │          │
│                      │  │ Winter     │ │ Azure      │ │ Night      │ │ Sunlit     │          │
│                      │  │ Rose       │ │ Bloom      │ │ Oath       │ │ Grace      │          │
│                      │  └────────────┘ └────────────┘ └────────────┘ └────────────┘          │
│                      │                                                                       │
│                      │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐          │
│                      │  │ Thumbnail  │ │ Thumbnail  │ │ No Preview │ │ No Preview │          │
│                      │  │            │ │            │ │            │ │            │          │
│                      │  │ Ebon       │ │ Celestial  │ │ Unknown    │ │ Unknown    │          │
│                      │  │ Sovereign  │ │ Dance      │ │ Design     │ │ Design     │          │
│                      │  └────────────┘ └────────────┘ └────────────┘ └────────────┘          │
│                      │                                                                       │
│                      │  ┌────────────┐ ┌────────────┐                                        │
│                      │  │ Thumbnail  │ │ Thumbnail  │                                        │
│                      │  │ Silver     │ │ Frost      │                                        │
│                      │  │ Whispers   │ │ Bloom      │                                        │
│                      │  └────────────┘ └────────────┘                                        │
│                      │                                                                       │
└──────────────────────┴───────────────────────────────────────────────────────────────────────┘

CARD CONTENT IN EACH TILE
- Thumbnail image
- Favorite star
- Existing action icons
- Design name
- Existing Apply / Hide buttons
- Existing hover states and tooltips

EMOTE SECTION
- Uses the same visual language as the gallery
- Keeps its current cards and actions
- No new behavior
```

**Steps**
1. Audit the current layout ownership and lock the redesign to presentation surfaces only. Use the existing partial split in `Windows/MainWindow/` as the control surface and keep the current service calls, selection logic, and input handling intact.
2. Rework the main window composition to match the wireframe: left browse rail, top search/status row, section chips beneath it, and a centered responsive grid. Keep the same behavior, same actions, same data, and the same navigation model.
3. Refresh the gallery card visuals. Update card sizing, spacing, hover states, image framing, icon placement, and metadata alignment to feel more premium and closer to the reference, while preserving all click targets, tooltips, and existing actions.
4. Refine the header and rail styling. Make the browse rail, search box, count, Show hidden checkbox, section chips, and the collection-create plus button feel more modern and cohesive, and keep the plus button beside the Imported chip, but keep the current search, settings, section switching, and collection creation behavior exactly as it works today.
5. Apply the same visual treatment to the emote section. It should use the same wireframe language and card styling, but keep its current behavior, card actions, and restore/apply flow unchanged.
6. Tune the theme layer to support the new look. Update `ClassicTheme` and, if needed, the other theme variants so the redesigned UI can use consistent colors, contrast, and hover states without hardcoding colors in window code.
7. Validate that behavior did not change. Confirm the same actions still apply, favorite, hide/unhide, delete, search, upload, restore, and switch views exactly as before.

**Relevant files**
- `d:\Projects\Plugins\Wardrobe\Vestiary\Windows\MainWindow\MainWindow.cs` — overall window composition and view switching
- `d:\Projects\Plugins\Wardrobe\Vestiary\Windows\MainWindow\MainWindow.TabBar.cs` — collection tab rendering and drag/drop behavior
- `d:\Projects\Plugins\Wardrobe\Vestiary\Windows\MainWindow\MainWindow.HeaderRow.cs` — search, counts, hidden toggle, and settings controls
- `d:\Projects\Plugins\Wardrobe\Vestiary\Windows\MainWindow\MainWindow.Gallery.cs` — gallery container and empty state
- `d:\Projects\Plugins\Wardrobe\Vestiary\Windows\MainWindow\MainWindow.DesignCard.cs` — card layout, thumbnail, actions, and button presentation
- `d:\Projects\Plugins\Wardrobe\Vestiary\Windows\MainWindow\MainWindow.EmoteGallery.cs` — emote mode layout, which must remain functional and visually consistent
- `d:\Projects\Plugins\Wardrobe\Vestiary\ClassicTheme.cs` — primary theme colors and visual tuning
- `d:\Projects\Plugins\Wardrobe\Vestiary\OceanTheme.cs` — secondary theme consistency if the redesign needs shared palette changes
- `d:\Projects\Plugins\Wardrobe\Vestiary\MidnightPurpleTheme.cs` — secondary theme consistency if the redesign needs shared palette changes
- `d:\Projects\Plugins\Wardrobe\Vestiary\ForestTheme.cs` — secondary theme consistency if the redesign needs shared palette changes
- `d:\Projects\Plugins\Wardrobe\Vestiary\ITheme.cs` — extend only if the redesign truly needs new shared color tokens

**Verification**
1. Build the Vestiary solution and confirm there are no compile errors in the touched UI and theme files.
2. Exercise the main window manually: collection tabs, search, settings, hidden toggle, card hover states, thumbnail actions, apply, favorite, hide/unhide, delete, and emote mode.
3. Confirm the visual updates do not change behavior when resizing the window or switching collections.
4. If a detail panel is added, verify it stays presentation-only and does not alter the existing apply/edit flows.

**Decisions**
- Keep this as a visual-first redesign rather than a feature rewrite.
- Prefer reusing the existing gallery and card architecture instead of introducing a new interaction model.
- Do not add pagination, infinite scroll changes, filtering changes, or a new content browsing model.
- Do not add new functional features; only restyle the current screens.
- Keep the existing collection-create plus button in the glamour view beside the Imported chip.
- Keep the left browse rail in the design, but do not turn it into a new navigation system.
- Do not change configuration schema, service APIs, or persistence format.

**Further Considerations**
1. Decide whether PI should implement only the visual refresh first, or whether the split-pane/right-detail look should be staged as a second pass.
2. Decide whether the redesign should keep the current collection tab model, or visually reinterpret it as a sidebar while preserving the same underlying collection logic.
3. Decide whether all theme variants should be updated now, or whether the initial pass should focus on `ClassicTheme` and then propagate the palette later.