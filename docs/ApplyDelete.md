# Apply & Delete Buttons - Implementation Status

## TL;DR
✅ **COMPLETE**: Three-button layout on each gallery card (Apply | Edit | Delete) with keyboard modifiers for safety. Apply uses Glamourer IPC with equipment-only mode (Ctrl+Click). Delete requires Ctrl+Click confirmation. All buttons styled with muted monochromatic color scheme.

## Implementation Status

### ✅ COMPLETED

**Phase 1: Backend IPC Integration**
- ✅ `GlamourerService.ApplyDesign()` — Wraps IPC call with proper flags
  - Regular click: flags=0x07 (Once | Equipment | Customization = full design)
  - Ctrl+Click: flags=0x03 (Once | Equipment = equipment only)
  - Logs success/error via IPluginLog
- ✅ `GlamourerService.DeleteDesign()` — Wraps IPC delete call
  - Logs result and handles errors gracefully

**Phase 2: UI Layout**
- ✅ Three-button layout (Apply | Edit | Delete)
- ✅ 62px width, 28px height per button
- ✅ 12px spacing between buttons
- ✅ Horizontally left-aligned on card
- ✅ Total width fits within 260px card

**Phase 3: Apply Button**
- ✅ Regular click applies full design
- ✅ Tooltip: "Apply this design"
- ✅ Keyboard modifier: "[disabled] Ctrl+Click: Equipment only"
- ✅ Color: Muted steel blue (0.45, 0.55, 0.65 base)
  - Hover: (0.55, 0.65, 0.75)
  - Active: (0.60, 0.70, 0.80)

**Phase 4: Delete Button**
- ✅ Ctrl+Click detection via `ImGui.GetIO().KeyCtrl`
- ✅ Regular click: tooltip shown, no action
- ✅ Ctrl+Click: design deleted via Glamourer IPC
- ✅ Gallery refreshes after deletion
- ✅ Tooltip: "Delete the design from Glamourer" + "[disabled] Ctrl+Click to confirm"
- ✅ Color: Muted red-grey (0.60, 0.40, 0.40 base)
  - Hover: (0.70, 0.50, 0.50)
  - Active: (0.75, 0.55, 0.55)

**Phase 5: Edit Button**
- ✅ Opens DesignEditorWindow for metadata editing
- ✅ Color: Muted warm grey (0.55, 0.50, 0.45 base)
  - Hover: (0.65, 0.60, 0.55)
  - Active: (0.70, 0.65, 0.60)

## Relevant files

- `Services/GlamourerService.cs` — Add ApplyDesign() and DeleteDesign() methods
- `Windows/MainWindow.cs` — Modify DrawDesignCard() to add Apply/Delete buttons and handlers

## Verification ✅

- ✅ Apply button: Click Apply → Character model updates in-game, logs success
- ✅ Equipment-only mode: Ctrl+Click Apply → Applies equipment only (customization unchanged)
- ✅ Delete confirmation: Hover Delete → Tooltip shows "Ctrl+Click to confirm"
- ✅ Delete with Ctrl: Ctrl+Click Delete → Design removed from gallery, logs deletion
- ✅ Delete without Ctrl: Regular click Delete → Nothing happens, tooltip shown
- ✅ Error handling: Failed IPC calls logged gracefully
- ✅ Button layout: All three buttons fit on 260px card width without wrapping
- ✅ Color scheme: Muted monochromatic theme applied to all buttons
- ✅ Hover/Active states: All buttons have proper visual feedback
- ✅ Tooltips: All buttons have descriptive tooltips with modifier hints

## Design Decisions

- **No confirmation dialog**: Ctrl+click requirement IS the confirmation mechanism (prevents accidental deletes)
- **Apply to player only**: Always apply to object index 0 (player character), key=0 (no locking)
- **Glamourer as source of truth**: Don't cache deletion locally, rely on Glamourer IPC response
- **Button order**: Apply (steel blue), Edit (warm grey), Delete (red-grey) — left to right logical flow
- **Equipment-only mode**: Added Ctrl+Click variant for quick equipment-only applies (flags=0x03)
- **Color scheme**: Muted monochromatic theme for cohesive dark UI
  - Avoid bright colors that clash with dark theme
  - Distinct enough to be recognizable
  - Consistent hover/active state progression
- **Gallery refresh**: After deletion, fetch fresh design list from GlamourerService to update gallery
