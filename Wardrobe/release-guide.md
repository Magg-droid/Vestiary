# Wardrobe Release Guide

Give this to your agent whenever you want to publish a new release.

## Repos

- **Source:** `https://github.com/Magg-droid/Wardrobe` — plugin code, builds, GitHub Releases
- **Distribution:** `https://github.com/Magg-droid/plugin-collection` — `pluginmaster.json` (the store listing)

Users add `https://raw.githubusercontent.com/Magg-droid/plugin-collection/main/pluginmaster.json` to Dalamud.

---

## Step 1: Bump the version

In the Wardrobe source repo, update both of these to the same version:

**`Wardrobe.json`:**
```json
"AssemblyVersion": "1.0.0.2"
```

**`.csproj`** (e.g. `Wardrobe.csproj`):
```xml
<Version>1.0.0.2</Version>
```

---

## Step 2: Build

```bash
dotnet build -c Release
```

Find the output `.zip` (usually `bin/Release/latest.zip`). Verify it contains `Wardrobe.dll` and `Wardrobe.json`.

---

## Step 3: Create a GitHub Release

Go to `https://github.com/Magg-droid/Wardrobe/releases` → **Draft a new release**:

- **Tag:** `v1.0.0.2` (must match the version exactly)
- **Title:** `v1.0.0.2`
- **Body:** paste your changelog
- **Attach:** the `latest.zip` from Step 2

Click **Publish release**. After publishing, right-click the attached `.zip` → **Copy link address**. You'll get:

```
https://github.com/Magg-droid/Wardrobe/releases/download/v1.0.0.2/latest.zip
```

---

## Step 4: Update pluginmaster.json

In `https://github.com/Magg-droid/plugin-collection`, edit `pluginmaster.json`. Find the Wardrobe entry and update these fields:

```json
{
  "AssemblyVersion": "1.0.0.2",
  "DownloadLinkInstall": "https://github.com/Magg-droid/Wardrobe/releases/download/v1.0.0.2/latest.zip",
  "DownloadLinkUpdate": "https://github.com/Magg-droid/Wardrobe/releases/download/v1.0.0.2/latest.zip",
  "DownloadLinkTesting": "https://github.com/Magg-droid/Wardrobe/releases/download/v1.0.0.2/latest.zip",
  "LastUpdate": 1785200000,
  "Changelog": "What changed in this release."
}
```

- `LastUpdate` is a Unix timestamp — generate a current one at https://www.unixtimestamp.com
- `DownloadLinkInstall`, `DownloadLinkUpdate`, and `DownloadLinkTesting` all point to the same `.zip` unless you have a separate testing channel

Commit and push.

---

## Verify before telling users

- [ ] `AssemblyVersion` in `Wardrobe.json` (inside the `.zip`) matches `pluginmaster.json`
- [ ] All three download URLs point to the correct GitHub Release `.zip`
- [ ] `DalamudApiLevel` is correct (check what API level your Dalamud is using)
- [ ] Actually download the `.zip` from the release URL and confirm it has `Wardrobe.dll` + `Wardrobe.json`

---

## Done

Users who added your `pluginmaster.json` URL to Dalamud will see the update. Dalamud checks on its own schedule (typically within a few hours). Users can force-refresh in the plugin installer by clicking the refresh button.
