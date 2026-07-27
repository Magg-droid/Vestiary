# Wardrobe Architecture

# Vision

Wardrobe is a visual companion plugin for Glamourer.

Glamourer remains the source of truth for all designs.
Wardrobe never modifies Glamourer data directly.

Wardrobe enhances the Glamourer experience by providing:

- Visual outfit browsing
- User collections
- Outfit thumbnails
- Fast outfit selection

The goal is to allow players with hundreds of Glamourer designs to find and apply outfits quickly without relying on memory or long text lists.

---

# Core Principles

- Glamourer owns the designs.
- Wardrobe owns the presentation.
- Wardrobe should never require users to reorganise their Glamourer setup.
- Everything should feel lightweight and optional.
- Clicking a thumbnail should be enough to apply an outfit.

---

# Main Concepts

## Glamourer

Provides:

- Designs
- Folder structure
- Tags
- Applying outfits

Wardrobe reads information from Glamourer whenever possible.

---

## Collections

Collections are the main navigation inside Wardrobe.

A collection represents one user-defined tab.

Examples:

- Dresses
- Casual
- Bikinis
- Wedding
- Combat
- Seasonal
- Uncategorized

A collection can be linked to one or more Glamourer folder paths.

Example:

"Dresses"

→ SFW/Dresses
→ NSFW/Dresses

A collection can also have NO paths, in which case it displays designs that don't exist in any folder (root-level designs).

Example:

"Uncategorized" (empty paths)

→ Shows "Spring Shirt - Caroline Towel"
→ Hides "SFW/Dresses/AM - Jaque Bridesmaid"

Collections exist only inside Wardrobe.

---

## Gallery

The gallery displays outfit thumbnails.

Users browse visually instead of searching through names.

Clicking a thumbnail applies the Glamourer design.

---

## Thumbnails

Each Glamourer design may have one thumbnail.

Users can either:

- Upload an existing screenshot
- Capture a new thumbnail directly inside Wardrobe

Thumbnails are stored by Wardrobe.

---

# Planned Workflow

First Launch

↓

Import Glamourer folders

↓

Create Collections

↓

Wardrobe displays outfits

↓

User adds thumbnails

↓

Click outfit

↓

Wardrobe tells Glamourer to apply the design

---

# Design Goals

- Fast
- Visual
- Minimal clicks
- Easy to maintain
- Non-destructive
- Compatible with future Glamourer updates

---

# Current Scope (MVP)

## Implemented ✅ (v0.0 MVP Complete)
- Read Glamourer designs
- Read Glamourer folder structure
- Create/edit/delete collections
- Collections with multiple folder paths per collection
- Uncategorized collections (designs without folders)
- Filter designs by collection
- Persistent collection storage in plugin Configuration
- Right-click context menu for Edit/Delete on collection tabs
- Display gallery with responsive outfit thumbnails (240x300px)
- Apply designs via Glamourer IPC (regular click)
- Apply equipment-only mode (Ctrl+Click)
- Delete designs (Ctrl+Click confirmation)
- Image upload via file picker (PNG/JPG/BMP/GIF/WEBP)
- Image upload via clipboard (Windows+Shift+S screenshots)
- Persistent thumbnail storage in plugin folder
- Thumbnail display in gallery cards
- Graceful fallback for missing images
- Texture caching for performance
- Left-aligned UI throughout
- Muted monochromatic color scheme

## TODO for Future Releases ❌
- Search/filter within collection
- Drag-and-drop to apply outfits
- Screenshot capture directly in Wardrobe
- Image cropping/preview
- Bulk operations
- Design statistics/favorites
- Backup/export collections