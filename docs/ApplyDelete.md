# Plan: Add Apply & Delete Buttons to Gallery Cards

## TL;DR
Add two action buttons to each gallery card: "Apply" (regular click to apply design via Glamourer IPC) and "Delete" (Ctrl+click required to delete design from Glamourer, regular click does nothing). The buttons will be positioned horizontally below the design name on each card, replacing the single "Edit" button.

## Steps

### Phase 1: Backend IPC Integration
1. **Update GlamourerService.cs** — Add two new methods:
   - `public GlamourerApiEc ApplyDesign(Guid designId)` — wraps IPC call to apply design to player (object index 0), returns status code
   - `public GlamourerApiEc DeleteDesign(Guid designId)` — wraps IPC call to delete design, returns status code
   - Both should log success/error via `IPluginLog`

### Phase 2: UI Layout Changes (*depends on Phase 1*)
2. **Update MainWindow.cs DrawDesignCard()** — Modify button section:
   - Replace single "Edit" button with three buttons: "Apply", "Edit", "Delete"
   - Layout: Horizontal row centered on card, approximately 75px each button with 6px spacing between
   - Total width: ~261px (fits in 260px card with small padding adjustment)
   - Apply button: Blue color (0.4, 0.6, 0.9)
   - Edit button: Rose-gold color (0.65, 0.4, 0.4)  
   - Delete button: Red color (0.9, 0.3, 0.3)
   - Button height: 28px (slightly smaller than current Edit button to fit 3 buttons)

### Phase 3: Apply Button Implementation (*depends on Phase 1*)
3. **Implement Apply button click handler** in MainWindow.cs:
   - Detect regular click on "Apply" button
   - Call `glamourerService.ApplyDesign(designId)`
   - Log result (Success, DesignNotFound, ActorNotFound, etc.)
   - Optional: Add error message display or toast notification

### Phase 4: Delete Button Implementation (*depends on Phase 1*)
4. **Implement Delete button with Ctrl+click detection** in MainWindow.cs:
   - Detect "Delete" button click
   - Check `ImGui.GetIO().KeyCtrl` to see if Ctrl is held
   - If Ctrl held: Call `glamourerService.DeleteDesign(designId)` → triggers gallery refresh
   - If NOT Ctrl held: Display tooltip or do nothing (prevent accidental deletion)
   - Log deletion attempt and result
   - Refresh design list after successful deletion

### Phase 5: Verification & Polish (*depends on Phase 4*)
5. **Test all three buttons**:
   - Apply: Click button → design applies to character in-game
   - Edit: Click button → editor window opens (existing functionality)
   - Delete (regular click): Verify nothing happens, consider adding hover tooltip "Ctrl+Click to delete"
   - Delete (Ctrl+Click): Verify design is deleted and gallery refreshes

## Relevant files

- `Services/GlamourerService.cs` — Add ApplyDesign() and DeleteDesign() methods
- `Windows/MainWindow.cs` — Modify DrawDesignCard() to add Apply/Delete buttons and handlers

## Verification

1. **Apply button**: Click Apply → Character model updates in-game, logs success
2. **Delete confirmation**: Hover Delete button → Tooltip shows "Ctrl+Click to delete"
3. **Delete with Ctrl**: Ctrl+Click Delete → Design removed from gallery, logs deletion
4. **Delete without Ctrl**: Regular click Delete → Nothing happens, no error
5. **Error handling**: Apply design that doesn't exist → Logs error gracefully
6. **Button layout**: All three buttons fit on 260px card width without wrapping

## Decisions

- **No confirmation dialog**: Ctrl+click requirement IS the confirmation mechanism (prevents accidental deletes)
- **Apply to player only**: Always apply to object index 0 (player character), key=0 (no locking), use default flags
- **Glamourer as source of truth**: Don't cache deletion locally, rely on Glamourer IPC response
- **Button order**: Apply (blue), Edit (rose-gold), Delete (red) — left to right logical flow
- **Gallery refresh**: After deletion, fetch fresh design list from GlamourerService to update gallery

## Further Considerations

1. **Glamourer IPC availability**: Should we add try-catch around IPC calls? What if Glamourer not running?
   - *Recommendation*: GlamourerService methods should catch exceptions, log error, return failure status code
2. **Undo feature**: Should users be able to undo deletion? 
   - *Recommendation*: Skip for MVP — Glamourer designs are file-based, deletion is meant to be final
3. **Apply flags**: Should we add options for how design applies (equipment only, customization only, etc.)?
   - *Recommendation*: Use default flags for MVP — most users want full design apply
