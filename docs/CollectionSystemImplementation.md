# Plan: Collections System Implementation for Wardrobe

> ⚠️ **ARCHIVED**: This was the original implementation plan. All tasks are now complete. See [CollectionSystem.md](CollectionSystem.md) for current status. Kept for historical reference.

## TL;DR

Implement a Collections feature to organize Glamourer designs by user-defined categories. Collections map to Glamourer folder paths, allowing users to group designs (e.g., "Dresses", "Casual") and filter the gallery view. Data persists in Configuration, CollectionService manages CRUD operations, and UI tabs switch between collections.

## Steps

### Phase 1: Data Model & Persistence (Parallel operations possible)

1. **Create Collection data model** — Add `Collection.cs` class with properties: `Id` (Guid), `Name` (string), `FolderPaths` (List<string>), `IsActive` (bool), `Order` (int). Should be serializable for Configuration.

2. **Update Configuration** — Add `Collections` (List<Collection>) property to [Configuration.cs](Configuration.cs). Increment Version. Add migration logic if needed for future updates.

3. **Create CollectionService** — New file `Services/CollectionService.cs` with methods:
   - `GetCollections()` — returns all collections
   - `CreateCollection(name, folderPaths)` — returns new collection
   - `DeleteCollection(id)` — removes collection
   - `UpdateCollection(id, name, folderPaths)` — updates collection
   - `GetDesignsByCollection(collectionId)` — filters designs from GlamourerService by folder paths
   - Each method persists changes to Configuration via `Configuration.Save()`

### Phase 2: UI Update (Depends on Phase 1)

4. **Add collection tabs to MainWindow** — Create tab bar above gallery area using ImGui TabBar/TabItem. Display all collections with "+" button to add new. *Depends on Phase 1.*

5. **Create CollectionEditor UI** — New file `Windows/CollectionEditorWindow.cs` (or modal in MainWindow) with:
   - Text input for collection name
   - Checkboxes or multi-select for available Glamourer folder paths (retrieve from GetDesignListExtended by extracting unique paths)
   - Save/Cancel buttons
   - *Depends on Phase 1.*

6. **Filter gallery by selected collection** — MainWindow stores `selectedCollectionId`, passes to GlamourerService.GetDesignsByCollection(), displays filtered results. *Depends on Phase 2.4.*

### Phase 3: Integration & Refinement (Depends on Phase 2)

7. **Display folder list** — Parse unique folder paths from Glamourer designs on first launch, populate UI for collection setup. Uses GetDesignListExtended data already fetched.

8. **Error handling** — Handle cases where collection is deleted but still referenced (graceful fallback to first collection or "All Designs").

## Relevant files

- [Configuration.cs](Configuration.cs) — Add `Collections` property
- `Services/CollectionService.cs` (NEW) — Collection CRUD & filtering logic
- [Windows/MainWindow.cs](Windows/MainWindow.cs) — Add tab bar, collection filtering
- `Windows/CollectionEditorWindow.cs` (NEW) — Collection creation/editing UI
- [Services/GlamourerService.cs](Services/GlamourerService.cs) — Already has design list with paths

## Verification

1. **Unit/manual tests**: Create a collection, verify it persists after restart
2. **Filter test**: Add designs to collection with specific folder path, verify gallery shows only those
3. **UI test**: Tabs render correctly, adding/deleting collections updates UI
4. **Edge case**: Delete collection while active — should fallback gracefully

## Decisions

- Collections stored in plugin Configuration (simple, lightweight, no separate DB)
- Folder paths matched as string prefixes (e.g., "SFW/Dresses" matches designs in that folder)
- Collections are soft-linked to Glamourer folders — no modification to Glamourer needed
- MVP only filters by folder path; tags/search deferred to future
- Default collection "All Designs" always available (optional, can be skipped for now)

## Task Breakdown

### TASK 1: Create Collection Data Model
**Objective**: Create the `Collection.cs` class that will hold collection data  
**Details**:
- Create new file: `Wardrobe/Models/Collection.cs`
- Properties: `Id` (Guid), `Name` (string), `FolderPaths` (List<string>), `Order` (int)
- Make it `[Serializable]` for Configuration persistence
- Status: **PENDING** → Ready to start

### TASK 2: Update Configuration (depends on Task 1)
**Objective**: Add Collections collection to persistent config  
**Details**:
- Update [Configuration.cs](Configuration.cs): add `public List<Collection> Collections { get; set; }` 
- Increment `Version` from 0 to 1
- Initialize default collections list
- Status: **PENDING**

### TASK 3: Create CollectionService (depends on Task 1-2)
**Objective**: Service layer for Collection CRUD operations  
**Details**:
- Create `Services/CollectionService.cs`
- Methods needed: `GetCollections()`, `CreateCollection()`, `UpdateCollection()`, `DeleteCollection()`, `GetDesignsByCollection()`
- Each method updates Configuration.Save()
- Status: **PENDING**

### TASK 4: Extract Unique Folder Paths from Glamourer
**Objective**: Get available folder paths to show in UI  
**Details**:
- Add method to GlamourerService: `GetUniqueFolderPaths()` 
- Extract from existing `GetDesignList()` data
- Return List<string>
- Status: **PENDING**

### TASK 5: Add Collection Tabs to MainWindow (depends on Task 1-3)
**Objective**: UI for switching between collections  
**Details**:
- Update [Windows/MainWindow.cs](Windows/MainWindow.cs)
- Add ImGui tab bar for collections
- Add "+" button to create new
- Store selected collection ID
- Status: **PENDING**

### TASK 6: Create CollectionEditor (depends on Task 1-4)
**Objective**: UI for creating/editing collections  
**Details**:
- Create `Windows/CollectionEditorWindow.cs` or inline modal
- Input for collection name
- Checkboxes for folder paths from Task 4
- Status: **PENDING**

### TASK 7: Filter Gallery by Collection (depends on Task 5)
**Objective**: Show only designs in selected collection  
**Details**:
- Get selected collection from tabs
- Call CollectionService.GetDesignsByCollection()
- Display filtered results
- Status: **PENDING**

### TASK 8: Error Handling (depends on all)
**Objective**: Handle edge cases gracefully  
**Details**:
- Deleted collection fallback
- Path validation
- Default collection setup
- Status: **PENDING**

---

## Further Considerations

1. **Folder path matching**: Should we use exact match, prefix match, or fuzzy? Currently recommend prefix match (user defines folder path, system matches any design FullPath starting with it).
   - Option A: Exact path only
   - Option B: Prefix match (recommended) — more flexible
   - Option C: Regex patterns — overkill for MVP

2. **Multiple folder paths per collection**: Architecture says one collection can link to multiple paths (e.g., "Dresses" → both "SFW/Dresses" and "NSFW/Dresses"). Should we support this in MVP or keep it 1:1 for now?
   - Option A: 1:1 (collection → single folder)
   - Option B: 1:N (collection → multiple folders, recommended per Architecture)

3. **Initial folder discovery**: Should we auto-discover folders from Glamourer on first launch, or require manual setup?
   - Option A: Manual entry only
   - Option B: Show available folders as checkboxes (recommended, better UX)
