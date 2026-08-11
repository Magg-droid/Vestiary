# Vestiary — Data Flow

> How data moves between Glamourer, Vestiary services, and the UI.

---

## Data Sources

```
┌─────────────────────────────────────────────────────┐
│  Glamourer (IPC)                                    │
│  • GetDesignListExtended → Dict<Guid, DesignInfo>   │
│  • ApplyDesign(Guid)                                 │
│  • DeleteDesign(Guid)                                │
│  • GetStateBase64(0,0) → base64+gzip JSON           │
│  • AddDesign(JSON, name) → Guid                     │
└──────────┬──────────────────────────────────────────┘
           │
           ▼
    GlamourerService.cs    ←──── all IPC here
           │
           ▼
┌──────────────────────────────────────────────────────┐
│  Vestiary (our data)                                 │
│                                                      │
│  Configuration.cs (persisted to disk)                │
│  ├── Collections[]          (user-created tabs)      │
│  ├── DesignMetadata[]       (nicknames, image paths) │
│  ├── HiddenDesignIds[]      (hidden from gallery)    │
│  ├── FavoriteDesignIds[]    (starred designs)        │
│  ├── ApplyEquipmentOnly     (settings toggle)        │
│  ├── ShowHidden             (eye toggle)             │
│  └── ThemeName              (Classic/Ocean/etc)      │
│                                                      │
│  Services                                            │
│  ├── CollectionService      filters designs by folder│
│  ├── DesignMetadataService  CRUD nicknames + images  │
│  ├── HiddenDesignService    hide/show designs        │
│  ├── FavoriteService        star/unstar designs      │
│  └── UtilityService         file picker, clipboard   │
└──────────┬───────────────────────────────────────────┘
           │
           ▼
    MainWindow (UI)
    ├── Draw() calls TabBar → HeaderRow → Gallery
    ├── Gallery shows filtered design cards
    └── Cards render thumbnail + actions
```

---

## Key Data Shapes

### Glamourer → Vestiary

```
DesignInfo {
  DisplayName: string      // "Casual Summer Outfit"
  FullPath: string         // "Casual/Summer" (folder hierarchy)
  DisplayColor: uint       // folder color from Glamourer
  ShownInQdb: bool         // in Glamourer's Quick Design Bar
}
```

### Vestiary → UI (augmented DesignInfo)

```
CardData = DesignInfo + DesignMetadata + favorites + hidden

{
  DisplayName:  "Casual Summer Outfit"   // from Glamourer
  FullPath:     "Casual/Summer"          // from Glamourer
  Nickname:     "My fav outfit"          // from DesignMetadata (optional)
  CustomImage:  "path/to/thumb.png"      // from DesignMetadata (optional)
  IsHidden:     false                    // from HiddenDesignService
  IsFavorite:   true                     // from FavoriteService
}
```

The UI shows Nickname if set, otherwise falls back to DisplayName.

---

## Folder Filtering (Collections)

```
Collection: { Name: "Casual", FolderPaths: ["Casual"] }

→ Matches designs with FullPath starting with "Casual"
→ "Casual/Summer" ✓
→ "Casual/Winter" ✓  
→ "Tank/Raid"      ✗ (mismatched folder)
→ "" (empty)       ✗ (uncategorized)
```

Multiple FolderPaths = union of matches. Empty FolderPaths = uncategorized only.

---

## Hidden vs Favorites

```
Hidden:   design is REMOVED from gallery (but still in Glamourer)
          → shown at 50% opacity when "Show Hidden" is on

Favorite: design appears in "Favorites" collection tab
          → golden star icon on card
          → tab auto-created on first favorite, auto-removed on last unfavorite
```
