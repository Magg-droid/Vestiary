# Deployment Checklist

## 1. Bump Version

Update the version number in these **two files**:

| File | Key |
|------|-----|
| `Wardrobe/Wardrobe.csproj` | `<Version>X.X.X.X</Version>` |
| `Wardrobe/Wardrobe.json` | `"AssemblyVersion": "X.X.X.X"` |

## 2. Build (Release)

```bash
dotnet clean Wardrobe/Wardrobe.csproj -c Release
dotnet build Wardrobe/Wardrobe.csproj -c Release
```

## 3. Generate latest.zip

```bash
cd Wardrobe/bin/Release

powershell -Command "Compress-Archive -Path Wardrobe.dll,Wardrobe.deps.json,Wardrobe.json,ECommons.dll,*.png -DestinationPath latest.zip -Force"
```

Verify:

```bash
unzip -l latest.zip
```

## 4. Commit & Tag

```bash
git add Wardrobe/Wardrobe.json Wardrobe/Wardrobe.csproj
git commit -m "vX.X.X.X: <short description>"
git tag vX.X.X.X
```

## 5. Push

```bash
git push origin main
git push origin vX.X.X.X
```

## 6. GitHub Release

1. Go to: https://github.com/Magg-droid/Wardrobe/releases
2. Click **"Draft a new release"**
3. Choose tag: `vX.X.X.X`
4. Title: `vX.X.X.X`
5. Attach `Wardrobe/bin/Release/latest.zip`
6. Publish

---

## Output Location

| Artifact | Path |
|----------|------|
| DLL | `Wardrobe/bin/Release/Wardrobe.dll` |
| Plugin JSON | `Wardrobe/bin/Release/Wardrobe.json` |
| Release zip | `Wardrobe/bin/Release/latest.zip` |
