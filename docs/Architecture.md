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

A collection can be linked to one or more Glamourer folder paths.

Example:

"Dresses"

→ SFW/Dresses
→ NSFW/Dresses

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

- Read Glamourer designs
- Read Glamourer folders
- Create collections
- Display gallery
- Apply designs
- Upload thumbnails

Everything else is considered future work.