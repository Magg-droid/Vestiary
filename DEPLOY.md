# Deployment Checklist

## 1. Bump Version

Update the version number in these **two files**:

| File | Key |
|------|-----|
| `Vestiary/Vestiary.csproj` | `<Version>X.X.X.X</Version>` |
| `Vestiary/Vestiary.json` | `"AssemblyVersion": "X.X.X.X"` |

## 2. Build (Release)

```bash
dotnet clean Vestiary/Vestiary.csproj -c Release
dotnet build Vestiary/Vestiary.csproj -c Release
```

## 3. Generate latest.zip

```bash
cd Vestiary/bin/Release

powershell -Command "Compress-Archive -Path Vestiary.dll,Vestiary.deps.json,Vestiary.json,ECommons.dll,*.png -DestinationPath latest.zip -Force"
```

Verify:

```bash
unzip -l latest.zip
```

## 4. Commit & Tag

```bash
git add Vestiary/Vestiary.json Vestiary/Vestiary.csproj
git commit -m "vX.X.X.X: <short description>"
git tag vX.X.X.X
```

## 5. Push

```bash
git push origin main
git push origin vX.X.X.X
```

## 6. GitHub Release

1. Go to: https://github.com/Magg-droid/Vestiary/releases
2. Click **"Draft a new release"**
3. Choose tag: `vX.X.X.X`
4. Title: `vX.X.X.X`
5. Attach `Vestiary/bin/Release/latest.zip`
6. Publish

---

## Output Location

| Artifact | Path |
|----------|------|
| DLL | `Vestiary/bin/Release/Vestiary.dll` |
| Plugin JSON | `Vestiary/bin/Release/Vestiary.json` |
| Release zip | `Vestiary/bin/Release/latest.zip` |
