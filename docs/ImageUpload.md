# Image Upload & Display

> 📁 **Context:** [Architecture.md](Architecture.md) | [CodingStandards.md](CodingStandards.md)

## TL;DR
✅ **COMPLETE**: Users can upload design thumbnails via file picker or clipboard. Images are stored in `%PluginDir%\thumbnails\` and displayed in gallery cards. Supports PNG/JPG/BMP/GIF/WEBP formats with graceful fallback for missing images. Texture caching ensures smooth scrolling.

---

## Implementation Status

### ✅ COMPLETED

**Phase 1: Setup & Infrastructure**
- ✅ Thumbnails folder created at persistent config path (`%appdata%/XIVLauncher/pluginConfigs/Vestiary/thumbnails/`) on plugin init
- ✅ `Plugin.OpenImageFilePicker()` implemented using Windows.Forms on STA thread
- ✅ `Plugin.CopyImageFromClipboard()` implemented with dual-workflow support:
  - Windows+Shift+S screenshots (Clipboard.ContainsImage)
  - File Explorer Ctrl+C files (Clipboard.ContainsFileDropList)
- ✅ Both methods save to thumbnails folder with unique filenames
- ✅ Proper error logging via IPluginLog

**Phase 2: UI - Upload in Editor**
- ✅ DesignEditorWindow shows "Choose Image" and "From Clipboard" buttons
- ✅ File picker filter: PNG/JPG/BMP/GIF/WEBP
- ✅ `OnImageSelected()` callback implemented
- ✅ Images copied to persistent config folder (`pluginConfigs/Vestiary/thumbnails/`)
- ✅ Metadata updated via DesignMetadataService
- ✅ Thread-safe operations with try-catch

**Phase 3: Display in Gallery**
- ✅ Texture caching via Dictionary<Guid, IShaderResourceView>
- ✅ Custom images displayed in 240x300px thumbnail area
- ✅ "No Preview" fallback for designs without images
- ✅ Graceful error handling for missing/corrupted files
- ✅ File existence check before load

**Phase 4: Persistence & Cleanup**
- ✅ Missing images degrade gracefully to placeholder
- ✅ Images keyed by design ID (persists across collection renames)
- ✅ Thumbnail files deleted when design metadata cleared
- ✅ Dual-workflow clipboard support (screenshots + files)

---

## Files to Modify

| File | Changes |
|------|---------|
| `Plugin.cs` | Add `OpenImageFilePicker()` and `CopyImageFromClipboard()` methods, create thumbnails folder on init |
| `Services/TextureCache.cs` | New service for caching loaded textures |
| `Services/DesignMetadataService.cs` | Metadata CRUD operations (GetMetadata, UpsertMetadata, DeleteMetadata) |
| `Windows/DesignEditorWindow.cs` | Add Choose Image, From Clipboard, and Clear Image buttons |
| `Windows/MainWindow.cs` | Add texture loading and image display in gallery cards |

---

## Verification ✅

### Functional Tests
- ✅ Click "Choose Image" → File picker opens
- ✅ Select PNG/JPG → Copies to thumbnails folder
- ✅ Close/reopen editor → Image selection persists
- ✅ Gallery shows image instead of "No Preview"
- ✅ Manually delete image file → Gallery shows placeholder gracefully
- ✅ Click "Clear Image" → Metadata cleared, shows placeholder
- ✅ Rename collection → Images still display
- ✅ Upload PNG/JPG/BMP/GIF/WEBP → All formats work
- ✅ Press Windows+Shift+S → Screenshot copied to clipboard
- ✅ Click "From Clipboard" → Image saves with timestamp filename
- ✅ Multiple clipboard pastes → Unique filenames prevent overwrites
- ✅ File Explorer Ctrl+C files → File path clipboard workflow works
- ✅ Extension filtering → Only valid image formats accepted

### Visual Tests
- ✅ Image fills 240x300px area (scaled appropriately)
- ✅ Image quality acceptable, no distortion
- ✅ "No Preview" displays when no image
- ✅ Buttons styled consistently
- ✅ No layout shifts or rendering issues

### Edge Cases
- ✅ Very large image file → Handles gracefully
- ✅ Disk full → Error logged, no crash
- ✅ File picker cancelled → No action taken
- ✅ Corrupted image file → Placeholder shown
- ✅ Concurrent image uploads → Thread-safe

---

## Implementation Order

1. Step 1 — Create thumbnails folder infrastructure
2. Step 2 — Add OpenFilePicker to Plugin.cs
3. Steps 3-4 — Add upload UI and file handling
4. Steps 5-6 — Add texture caching and image display
5. Steps 7-10 — Error handling, persistence, cleanup

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Copy files to plugin config folder | Survives version bumps and plugin relocation |
| Key by Design ID, not collection name | Images persist when collections renamed ✅ |
| Native Windows file dialog | Reliable, user-friendly (from Character Select+ pattern) |
| Texture caching | Essential for smooth scrolling through many designs |
| Graceful fallback | Missing images show "No Preview", no crashes |
| STA thread + Framework callback | Thread-safe, works with Dalamud framework |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| File I/O permissions denied | Wrap in try-catch, log errors, show user message |
| Invalid image format | Validate format before copying (PNG/JPG only) |
| Memory leaks in cache | Properly dispose textures when clearing |
| Thread safety issues | Use lock{} for concurrent file operations |
| Missing image files | Graceful fallback to "No Preview" placeholder |
| Disk space exhausted | Log error, show message, don't crash |

---

## Testing Strategy

1. **Unit**: Test SetCustomImage/ClearCustomImage in isolation
2. **Integration**: Verify file picker → copy → metadata save → display flow
3. **Visual**: Screenshot comparisons (placeholder vs image)
4. **Edge cases**: Large files, corrupted images, missing files
5. **Performance**: Confirm smooth scrolling with texture caching

---

## Notes

- **Character Select+ Reference**: Uses path storage; we use file copying for better portability
- **Dual input methods**: File picker for explicit selection, clipboard for quick screenshots
- **Clipboard workflow**: Windows+Shift+S → Screenshot in clipboard → Click "From Clipboard" → Auto-saves with timestamp
- **Thread model**: File picker and clipboard access run on STA thread with proper error handling
- **Texture caching**: ISharedImmediateTexture caching for smooth scrolling through many designs
- **Future enhancements**: Image cropping, aspect ratio locking, drag-and-drop support, bulk upload