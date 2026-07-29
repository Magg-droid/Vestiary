# Design Thumbnail Gallery

> 📁 **Context:** [Architecture.md](Architecture.md) | [CodingStandards.md](CodingStandards.md)

## TL;DR
✅ **COMPLETE**: Thumbnail gallery displaying Glamourer designs with responsive grid layout, design names, and metadata editing. Each card shows: placeholder thumbnail (240x300px), design name (centered, truncated), and Edit button. Clicking Edit opens a modal window to edit Nickname. Data persisted in Configuration.

**Status**: Gallery UI and collections system fully implemented and polished. Ready for image upload feature.

## Context
- Glamourer provides: DisplayName, FullPath, DisplayColor, ShownInQdb per design (Guid key)
- We can fetch design base64 images via Glamourer.GetDesignBase64(designId)
- Need to store Wardrobe-specific metadata (Nickname, custom image path) separately

## Implementation Status

### ✅ COMPLETED

**Phase 1: Data Model & Configuration**
- ✅ `Models/DesignMetadata.cs` — Design metadata model with Nickname, CustomImagePath, Order
- ✅ `Configuration.cs` — Extended with DesignMetadata list

**Phase 2: Service Layer**
- ✅ `Services/DesignMetadataService.cs` — Full CRUD operations for metadata
  - GetMetadata(designId) / UpsertMetadata() / DeleteMetadata() / GetDisplayName()
  - Automatic persistence to Configuration

**Phase 3: Edit Window**
- ✅ `Windows/DesignEditorWindow.cs` — Modal editor (left-aligned UI)
  - Read-only Design Name display
  - Nickname input field (optional)
  - Save/Cancel buttons with proper styling
  - Data persistence working correctly

**Phase 4: Thumbnail Gallery**
- ✅ `Windows/MainWindow.cs` — Fully implemented responsive gallery
  - **Layout**: Center-aligned cards in responsive grid
  - **Card size**: 260x400px with 12px corner rounding
  - **Thumbnail**: 240x300px with custom image, "No Preview" fallback, camera/upload/clipboard action icons
  - **Separator**: Full-width grey line below thumbnail image
  - **Design name**: Centered, truncated at 24 chars, rose-gold color, tooltip on hover
  - **Buttons**: Apply (steel blue), Edit (warm grey), Delete (red-grey) — centered three-button layout
  - **Spacing**: 25px horizontal gap between cards, 25px vertical gap between rows
  - **Design count**: Displayed top-right ("57 designs")
  - **Collections**: Tab-based navigation with right-click context menu (Edit/Delete) and hover tooltip
  - **Empty state**: Centered call-to-action with "Create Your First Collection" button for new users

**Phase 5: Integration**
- ✅ Services wired in `Plugin.cs`
- ✅ Windows injected and connected

### ✅ COMPLETED

**Phase 6: Image Upload**
- ✅ File picker UI in DesignEditorWindow (Windows.Forms OpenFileDialog)
- ✅ Clipboard upload UI (supports screenshots and file paths)
- ✅ Image storage in `%PluginDir%\thumbnails\{DesignId}.{extension}`
- ✅ Custom image display in gallery cards (replaces "No Preview")
- ✅ Graceful fallback for missing images
- ✅ Image caching for performance (texture dictionary)
- ✅ Dual-workflow support (file picker + clipboard)
- ✅ Format support: PNG, JPG, BMP, GIF, WEBP

## Gallery Card Specifications

### Card Dimensions
- **Card size**: 260w x 400h px
- **Corner rounding**: 12px
- **Border**: 1.5px, grey color (0.4, 0.4, 0.45, 0.7)
- **Background**: Dark (0.08, 0.08, 0.12, 0.95)

### Thumbnail Area
- **Size**: 240w x 300h px (10px padding on left)
- **Background**: Darker shade (0.1, 0.1, 0.15, 1f)
- **Border**: 1px, subtle grey (0.3, 0.3, 0.35, 0.4)
- **Corner rounding**: 4px
- **Placeholder text**: "No Preview" (centered, grey 0.5, 0.5, 0.55)
- **Separator line**: Full-width grey (0.4, 0.4, 0.45, 0.6), 1.5px thick, 8px below thumbnail

### Design Name
- **Text**: Glamourer DisplayName or Nickname (if set)
- **Color**: Rose-gold (0.9, 0.8, 0.7, 1f)
- **Alignment**: Centered
- **Max length**: 24 characters with "..." truncation
- **Tooltip**: Full name on hover if truncated
- **Positioning**: 12px below separator line

### Action Buttons
- **Layout**: Three buttons — Apply (steel blue), Edit (warm grey), Delete (muted red)
- **Sizes**: Apply 62px, Edit 62px, Delete 70px — all 28px height
- **Spacing**: 12px between buttons, centered as a group
- **Colors**: See `RoseGoldTheme.cs` for actual values
- **Rounding**: 4px
- **Positioning**: Centered, below design name
- **Tooltips**: Each button has contextual tooltip with keyboard modifier hints

### Grid Layout
- **Spacing**: 25px horizontal gap between cards, 25px vertical gap between rows
- **Alignment**: Center-aligned (cards centered within window)
- **Responsive**: Column count auto-calculated based on available width
- **Padding**: Top 20px before gallery starts

## Verified Workflows

### ✅ Collection Management
1. Create collection "SFW/Dresses" → Tab appears with "+" button
2. Switch collections → Gallery filters designs correctly
3. Edit collection → Name updates in tab
4. Delete collection → Tab removed, falls back to first collection
5. Rename collection (e.g., SFW/Dresses → New/Dresses) → Images persist because they're keyed by design ID ✅

### ✅ Metadata Editing
1. Click Edit button → DesignEditorWindow opens with current metadata
2. Enter Nickname → Save → Name persists after plugin reload
3. Clear Nickname → Save → Reverts to Glamourer DisplayName
4. Long design names → Truncated with tooltip on hover
5. Cancel button → Closes without saving

### ✅ Gallery Display
1. 57+ designs display in responsive grid (5 columns at typical window width)
2. Design count shows correctly at top-right
3. Cards centered in viewport
4. Proper spacing between cards (25px)
5. Separators and styling consistent across all cards

## Next Steps: Image Upload Integration

**File reference**: Character Select+ plugin at `d:\Projects\Plugins\Character-Select--master\`

**Pattern**: 
1. Use `plugin.OpenFilePicker()` for native Windows file dialog
2. Copy selected image to `%PluginDir%\thumbnails\{DesignId}.png`
3. Store filename in `DesignMetadata.CustomImagePath`
4. Load via `Plugin.TextureProvider.GetFromFile()` in gallery

**DesignEditorWindow updates needed**:
- Add "Choose Image" button (120px)
- Add "Clear Image" button (60px) if image selected
- Implement file picker callback
- Display selected image preview

**MainWindow updates needed**:
- Replace "No Preview" placeholder with actual image if `CustomImagePath` exists
- Graceful fallback to placeholder if image file missing
- Cache loaded textures for performance
