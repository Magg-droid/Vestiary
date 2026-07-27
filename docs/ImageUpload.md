# Image Upload & Display Implementation Plan

## TL;DR

Implement image upload and display for design thumbnails. Users can:
1. **File Picker**: Browse and select image files (PNG/JPG/BMP/GIF/WEBP)
2. **Clipboard**: Press Windows+Shift+S to screenshot, then click "From Clipboard"

Selected images get copied to `%PluginDir%\thumbnails\` with timestamp-based filenames, and gallery cards display the image instead of "No Preview" placeholder. Falls back gracefully if file missing.

**Approach**: Copy images to plugin folder (not storing paths like Character Select+), keyed by design ID for persistence across collection renames.

**Reference**: Character Select+ plugin pattern for file picker implementation.

---

## Implementation Steps

### Phase 1: Setup & Infrastructure (Blocks: Phases 2-3)

**Step 1: Create thumbnails folder**
- Location: `%PluginDir%\thumbnails\`
- In `Plugin.cs` initialization: Create folder if not exists using `Directory.CreateDirectory()`
- Reference: `/memories/repo/image-upload-pattern.md`

**Step 2: Add OpenFilePicker to Plugin.cs**
- Copy implementation from Character Select+ (already documented in repo memory)
- Method signature: `public void OpenFilePicker(string title, string filter, Action<string> onFileSelected)`
- Uses System.Windows.Forms.OpenFileDialog on STA thread
- Callback on file selected via `OnImageSelected()` in DesignEditorWindow
- Add using statements: System.Windows.Forms, System.Drawing

**Step 2b: Add CopyImageFromClipboard to Plugin.cs**
- Method signature: `public void CopyImageFromClipboard(Action<string> onImageSaved)`
- Accesses Windows clipboard via System.Windows.Forms.Clipboard on STA thread
- Saves image from clipboard to `%PluginDir%\thumbnails\clipboard_{timestamp}.png`
- Callback with saved path for metadata update
- Error handling: Logs if clipboard doesn't contain image or clipboard access fails

### Phase 2: UI - Upload in Editor (Depends on: Phase 1)

**Step 3: Update DesignEditorWindow.cs**
- After "Custom Image: (Optional)" line, add three buttons:
  - "Choose Image" button (150px) → calls `plugin.OpenImageFilePicker(OnImageSelected)`
  - "From Clipboard" button (140px) → calls `plugin.CopyImageFromClipboard(OnImageSelected)`
  - "Clear Image" button (120px, only if image selected) → clears metadata and customImagePath
- File picker filter: `"Image files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp"`
- Implement `OnImageSelected(string selectedPath)` callback:
  - Copy file to `%PluginDir%\thumbnails\{DesignId}.{extension}` (overwrite if exists)
  - Update metadata via `designMetadataService.UpsertMetadata()`
  - Handle thread safety with proper try-catch
  - Log success/error

**Step 4: Add image handling to DesignMetadataService.cs**
- Add method: `public void SetCustomImage(Guid designId, string sourceFilePath)`
  - Validate source file exists and is PNG/JPG
  - Copy to thumbnails folder with design ID as name
  - Update DesignMetadata.CustomImagePath
  - Save configuration
- Add method: `public void ClearCustomImage(Guid designId)`
  - Delete thumbnail file if exists
  - Clear CustomImagePath
  - Save configuration

### Phase 3: Display in Gallery (Depends on: Phase 1)

**Step 5: Add texture caching to MainWindow.cs**
- Existing: `private Dictionary<Guid, IntPtr> thumbnailCache = new();`
- Change to: `private Dictionary<Guid, IShaderResourceView> thumbnailCache = new();`
- Initialize empty in constructor

**Step 6: Update DrawDesignCard() to display images**
- Replace "No Preview" placeholder rendering with:
  - Check if custom image path exists for design
  - If exists: Load texture via `Plugin.TextureProvider.GetFromFile()`
  - Cache texture in dictionary
  - Render with `ImGui.Image(texture, size)`
  - If not exists OR load fails: Show "No Preview" text (current behavior)
- Keep thumbnail box styling (240x300px, rounded corners, border)

**Step 7: Implement graceful fallback**
- Wrap texture loading in try-catch
- If file missing or corrupted: Log warning, use placeholder
- Check file exists before attempting load (File.Exists())
- No crashes, just degradation to placeholder

### Phase 4: Persistence & Cleanup

**Step 8: Handle missing images**
- On plugin load/draw: DesignMetadata may point to deleted file
- In DrawDesignCard(): Check File.Exists() before loading
- Fallback to placeholder if missing (user can re-upload)

**Step 9: Collection rename persistence**
- Images keyed by design ID (Guid), not collection name
- Rename collection → Same designs → Same images ✅
- Verify in testing

**Step 10: Optional cleanup on design deletion**
- When DesignMetadataService.DeleteMetadata() called:
  - Also delete corresponding thumbnail file
  - Or leave file (safer for recovery)
- Recommendation: Delete file for cleanliness

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

## Verification Checklist

### Functional Tests
- [x] Click "Choose Image" → File picker opens
- [x] Select PNG/JPG → Copies to thumbnails folder
- [x] Close/reopen editor → Image selection persists
- [x] Gallery shows image instead of "No Preview"
- [x] Manually delete image file → Gallery shows placeholder gracefully
- [x] Click "Clear Image" → Metadata cleared, shows placeholder
- [x] Rename collection → Images still display
- [x] Upload PNG/JPG/BMP/GIF/WEBP → All formats work
- [x] Press Windows+Shift+S → Screenshot copied to clipboard
- [x] Click "From Clipboard" → Image saves with timestamp filename
- [x] Multiple clipboard pastes → Unique filenames prevent overwrites

### Visual Tests
- [ ] Image fills 240x300px area (scaled appropriately)
- [ ] Image quality acceptable, no distortion
- [ ] "No Preview" displays when no image
- [ ] Buttons styled consistently with rose-gold theme
- [ ] No layout shifts or rendering issues

### Edge Cases
- [ ] Very large image file → Handles gracefully
- [ ] Disk full → Error logged, no crash
- [ ] File picker cancelled → No action taken
- [ ] Corrupted image file → Placeholder shown
- [ ] Concurrent image uploads → Thread-safe via lock{}

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
| Copy files to plugin folder | More durable than storing paths (survives plugin relocation) |
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