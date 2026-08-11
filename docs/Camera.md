# Camera Overlay

> 📁 **Context:** [Architecture.md](Architecture.md) | [CodingStandards.md](CodingStandards.md)

The camera overlay lets users capture a cropped screenshot of the game through a **4:5 aspect-ratio viewfinder**. It is triggered from the Design Editor window and returns the captured image to be used as a custom thumbnail for a design.

---

## How It's Triggered

Two ways to open the camera overlay:

1. **From the Design Editor** — Click the **"Camera"** button in `DesignEditorWindow.cs`.
2. **From the gallery card** — Click the camera icon (📷) in the top-right corner of any design card in `MainWindow.cs`.

Both paths call `Plugin.ShowCameraOverlay(onImageCaptured)` with a callback that updates the design's metadata with the saved image path.

---

## What Happens When Camera Opens

### 1. Window State Saved & Hidden
`Plugin.ShowCameraOverlay()` saves which windows are open, hides **all** plugin windows, and sets `IsCameraActive = true`. This flag suppresses the MainWindow and DesignEditorWindow `Draw()` methods so nothing renders behind the overlay.

```csharp
// Plugin.cs
wasMainWindowOpen = MainWindow.IsOpen;
wasDesignEditorOpen = DesignEditorWindow.IsOpen;
MainWindow.IsOpen = false;
DesignEditorWindow.IsOpen = false;
// ... hide all other windows too
```

### 2. Game UI Toggled Off
Scroll Lock is simulated via the Windows **SendInput** API to hide the FFXIV HUD for a clean screenshot.

```csharp
// Plugin.cs - ToggleGameUI()
SendInput / VK_SCROLL press → 30ms sleep → release
```

### 3. Camera Overlay Opens
`CameraWindow.Open(callback)` initializes the viewfinder centered on screen at 60% of viewport height, maintaining the 4:5 ratio.

---

## CameraWindow Architecture

**File:** `Vestiary/Windows/CameraWindow.cs`  
**Class:** `CameraWindow : Window, IDisposable`

### ImGui Flags
The window is a full-screen transparent overlay:

| Flag | Purpose |
|------|---------|
| `NoTitleBar` | No window chrome |
| `NoResize` / `NoMove` | Managed manually, not by ImGui |
| `NoScrollbar` / `NoScrollWithMouse` | No scrolling |
| `NoCollapse` | Can't be collapsed |
| `NoSavedSettings` | Fresh state every time |
| `NoBackground` | Transparent (we draw our own vignette) |

### Key Constants

| Constant | Value | Meaning |
|----------|-------|---------|
| `Ratio` | `4f / 5f` (0.8) | Viewfinder aspect ratio (portrait 4:5) |
| `HandleR` | `14f` | Corner handle hit-test radius in pixels |
| `MinW` | `120f` | Minimum viewfinder width |
| `MinH` | `150f` | Minimum viewfinder height |
| `Inset` | `8f` | Inner margin for the capture area (capture happens inside the inset) |

### State Machine

```
                  ┌──────────────────────────────┐
                  │         IDLE                 │
                  │  (showing hint text)         │
                  └──────┬───────────┬───────────┘
                         │           │
              Left-click │           │ Left-click
              inside     │           │ on corner
              frame      │           │
                         ▼           ▼
                  ┌──────────┐  ┌──────────────┐
                  │ DRAGGING │  │  RESIZING    │
                  │          │  │              │
                  │ shows:   │  │ shows:       │
                  │ "Release │  │ "W × H"      │
                  │ to place"│  │ dimensions   │
                  └──────────┘  └──────────────┘
```

---

## Feature Details

### Shift Key Detection (user32.dll P/Invoke)

```csharp
[DllImport("user32.dll")]
private static extern short GetAsyncKeyState(int vKey);
private const int VK_SHIFT = 0x10;
private bool ShiftHeld => (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
```

**Why user32.dll and not ImGui key state?**  
ImGui only sees keys when the game window has focus. `GetAsyncKeyState` works globally, so the overlay still responds even if focus drifts. This is essential because `PreDraw()` must react to Shift **before** ImGui processes input.

### PreDraw — Shift to Hide Overlay

When Shift is held, the overlay window is moved off-screen (`-9999, -9999`) and sized to 1×1. This effectively hides the vignette and frame so the user can freely rotate the camera with right-click (FFXIV's built-in camera rotation) while still keeping the window technically "open."

When Shift is released, the window snaps back to full viewport on the next frame.

```csharp
public override void PreDraw()
{
    if (ShiftHeld)
    {
        Position = new Vector2(-9999, -9999);  // off-screen
        Size = new Vector2(1, 1);
        PositionCondition = ImGuiCond.Always;
        return;
    }

    var vp = ImGui.GetMainViewport();
    Position = vp.Pos;
    Size = vp.Size;
    PositionCondition = ImGuiCond.Always;
}
```

`ImGuiCond.Always` ensures the position/size override is applied every frame without exception.

---

### Drag to Move

- **Hit zone:** Entire frame area + 10px padding around the outside (`InDragZone()`).
- **How it works:** On `ImGuiMouseButton.Left` click inside the drag zone (but not on a corner), `isDragging` is set to `true` and `dragOffset = framePos - mouse` is recorded.
- **While dragging:** `framePos = mouse + dragOffset` keeps the frame following the cursor.
- **On release:** `isDragging` becomes `false`.
- **Clamping:** Position is clamped to stay within the viewport with 10px left/right margin and 35px top / 80px bottom margin.

```csharp
framePos.X = Math.Clamp(framePos.X, vp.Pos.X + 10, vp.Pos.X + vp.Size.X - frameSize.X - 10);
framePos.Y = Math.Clamp(framePos.Y, vp.Pos.Y + 35, vp.Pos.Y + vp.Size.Y - frameSize.Y - 80);
```

The larger bottom margin (80px) reserves room for the Capture/Cancel buttons.

---

### Corners to Resize

**Corner detection:** The 4 corners of the viewfinder frame are checked against the mouse position using a 14px radius (`HandleR`). The helper `Near()` does a simple absolute-distance check:

```csharp
static bool Near(Vector2 c, Vector2 m, float r) =>
    Math.Abs(m.X - c.X) <= r && Math.Abs(m.Y - c.Y) <= r;
```

**Opposite anchor:** When you grab a corner, the diagonally opposite corner becomes the fixed anchor point.

```csharp
Vector2 Opposite(int i) => i switch
{
    0 => framePos + frameSize,              // top-left → bottom-right
    1 => new(framePos.X, framePos.Y + frameSize.Y),  // top-right → bottom-left
    2 => new(framePos.X + frameSize.X, framePos.Y),  // bottom-left → top-right
    _ => framePos                            // bottom-right → top-left
};
```

**Resize logic (`ResizeFrom`):**
1. Calculate raw width/height from mouse to anchor distance.
2. Lock to 4:5 ratio — whichever dimension is "longer" relative to the ratio drives the other.
3. Clamp both dimensions to min/max bounds.
4. Re-lock ratio after clamping.
5. Position the frame so the anchor stays fixed and the frame extends toward the mouse.

```
             anchor ─────────────────────┐
               │                         │
               │     locked 4:5 area     │
               │                         │
               └──── mouse drags this corner
```

---

### Hint Text

Shown centered above the frame. Context-sensitive (priority order):

| State | Text |
|-------|------|
| Resizing | `{width} × {height}` (inner dimensions in px) |
| Dragging | `Release to place` |
| Idle | `Drag to move  ·  Corners to resize  ·  Hold Shift+right click to rotate` |

The `·` (middle dot, U+00B7) is used as a visual separator between hints.

---

### Frame & Vignette Rendering

All drawn via `ImGui.GetWindowDrawList()` (ImDrawList):

1. **Vignette** — 4 filled rectangles covering the area *around* the viewfinder at 40% opacity black. Darkens everything outside the frame.

2. **Outer frame border** — Rounded-rect outline at `(0.9, 0.8, 0.7, 0.45)` (warm gold, semi-transparent), 1.5px thick, no corner rounding.

3. **Inner frame border** — 8px inset from the outer frame, white at 12% opacity, 1px thick. Shows the actual capture area.

4. **Corner dots** — 4 filled circles at the corners:
   - **Idle:** 5px radius, warm gold at 70% opacity.
   - **Hovered/Resizing:** 6px radius, bright warm gold at 100% opacity.

5. **Hint text** — Above the frame, centered horizontally, warm gold at 70% opacity.

---

### Buttons

Two buttons rendered below the frame, centered:

| Button | Color | Size | Action |
|--------|-------|------|--------|
| **Capture** | Green `(0.25, 0.55, 0.25)` | 130×36px | Takes screenshot, saves to disk |
| **Cancel** | Red `(0.50, 0.25, 0.25)` | 130×36px | Closes overlay, restores everything |

---

### Screenshot Capture

```csharp
void Capture()
```

1. Calculates the capture rectangle: `framePos + Inset` to `framePos + frameSize - Inset`.
2. Uses `Graphics.CopyFromScreen()` (System.Drawing / GDI+) to capture the screen region.
3. Saves as PNG to `{PluginDirectory}/thumbnails/camera_{timestamp}.png`.
   - Filename format: `camera_yyyyMMdd_HHmmss_fff.png` (e.g., `camera_20250727_221530_421.png`)
4. Closes the overlay and invokes the `onImageCaptured` callback with the file path.

**Why `CopyFromScreen`?**  
FFXIV renders with DirectX, so standard screenshot APIs don't always work. `CopyFromScreen` is a GDI call that reads the final framebuffer, so it works regardless of the rendering backend.

---

### Key Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Cancel (same as Cancel button) |
| `Enter` / `Numpad Enter` | Capture (same as Capture button) |
| `Arrow Keys` | Nudge frame position by 1px (10px if Shift held) |
| `Shift + Right-click` | Temporarily hide overlay to rotate game camera |

---

### Closing & Cleanup

`Close()` is called by both Cancel and after a successful Capture:

1. Sets `isActive = false` and `IsOpen = false`.
2. Clears the callback reference.
3. Resets drag/resize state.
4. Calls `plugin.OnCameraClosed()`.

`OnCameraClosed()` in Plugin.cs restores everything:

```csharp
public void OnCameraClosed()
{
    ToggleGameUI();           // Send Scroll Lock to show HUD again
    IsCameraActive = false;
    if (wasMainWindowOpen)   MainWindow.IsOpen = true;
    if (wasDesignEditorOpen) DesignEditorWindow.IsOpen = true;
}
```

---

## Data Flow Summary

```
User clicks "Camera" in DesignEditor
        │
        ▼
Plugin.ShowCameraOverlay(callback)
        │
        ├─ Save window states
        ├─ Hide all plugin windows
        ├─ ToggleGameUI() — hide FFXIV HUD
        └─ CameraWindow.Open(callback)
                │
                ▼
        CameraWindow active (full viewport overlay)
        User positions/resizes the 4:5 frame
                │
        ┌───────┴───────┐
        ▼               ▼
    [Capture]       [Cancel / Esc]
        │               │
        ▼               ▼
    CopyFromScreen   Close()
    Save PNG         OnCameraClosed()
        │               │
        ▼               ▼
    Close()          ToggleGameUI() — show HUD
    callback(path)   Restore windows
        │
        ▼
    DesignEditor receives path
    Updates metadata.CustomImagePath
    Re-opens editor window
```

---

## Files Involved

| File | Role |
|------|------|
| `Vestiary/Windows/CameraWindow.cs` | Full camera overlay implementation |
| `Vestiary/Plugin.cs` | Orchestration: `ShowCameraOverlay()`, `OnCameraClosed()`, `ToggleGameUI()` |
| `Vestiary/Windows/DesignEditorWindow.cs` | Triggers the camera and handles the returned image |
| `Vestiary/Services/DesignMetadataService.cs` | Stores the `CustomImagePath` in design metadata |
