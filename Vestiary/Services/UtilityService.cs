using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Dalamud.Plugin.Services;

namespace Vestiary.Services;

/// <summary>
/// Shared utilities: file picker, clipboard, Scroll Lock toggle, thumbnail directory setup.
/// No domain logic — just pure helpers.
/// </summary>
public class UtilityService
{
    private readonly string pluginDir;
    private readonly IPluginLog log;
    private readonly ModStateService? modStateService;

    public string ThumbnailsDirectory { get; }

    public bool CanMigrateFromWardrobe
    {
        get
        {
            var configsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher", "pluginConfigs");
            return File.Exists(Path.Combine(configsRoot, "Wardrobe.json"))
                || File.Exists(Path.Combine(configsRoot, "Wardrobe", "Wardrobe.json"));
        }
    }

    // ── P/Invoke for SendInput ──────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_SCROLL = 0x91;

    public UtilityService(string pluginDir, IPluginLog log, Configuration configuration, ModStateService? modStateService = null)
    {
        this.pluginDir = pluginDir;
        this.log = log;
        this.modStateService = modStateService;

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "pluginConfigs",
            "Vestiary");
        ThumbnailsDirectory = Path.Combine(configDir, "thumbnails");
        Directory.CreateDirectory(ThumbnailsDirectory);
        log.Information($"Thumbnails directory: {ThumbnailsDirectory}");

        MigrateFromWardrobe(configuration);
    }

    // ── Thumbnail Resize ───────────────────────────

    private const int ThumbMaxWidth = 480;
    private const int ThumbMaxHeight = 600;
    private const int ThumbJpegQuality = 85;

    /// <summary>
    /// Resizes an image to fit within maxWidth x maxHeight (maintaining aspect ratio),
    /// saves as JPEG, and returns the new .jpg path. Does not upscale images that are
    /// already smaller than the target.
    /// </summary>
    public string ResizeThumbnail(string sourcePath, string destPathWithoutExtension)
    {
        var destPath = destPathWithoutExtension + ".jpg";

        try
        {
            using var img = Image.FromFile(sourcePath);

            int newWidth = img.Width;
            int newHeight = img.Height;

            // Only downscale if the image exceeds target dimensions
            if (newWidth > ThumbMaxWidth || newHeight > ThumbMaxHeight)
            {
                double ratio = (double)img.Width / img.Height;
                double targetRatio = (double)ThumbMaxWidth / ThumbMaxHeight;

                if (ratio > targetRatio)
                {
                    newWidth = ThumbMaxWidth;
                    newHeight = (int)(ThumbMaxWidth / ratio);
                }
                else
                {
                    newHeight = ThumbMaxHeight;
                    newWidth = (int)(ThumbMaxHeight * ratio);
                }
            }

            using var bmp = new Bitmap(newWidth, newHeight);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(img, 0, 0, newWidth, newHeight);

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)ThumbJpegQuality);
            var jpegCodec = ImageCodecInfo.GetImageEncoders()
                .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            bmp.Save(destPath, jpegCodec, encoderParams);

            log.Information($"Thumbnail resized: {img.Width}x{img.Height} → {newWidth}x{newHeight}, saved to {destPath}");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to resize thumbnail: {sourcePath}");
            // Fallback: save as JPEG without resize, or just copy the original
            try
            {
                using var img = Image.FromFile(sourcePath);
                img.Save(destPath, ImageFormat.Jpeg);
            }
            catch
            {
                try { File.Copy(sourcePath, destPath, overwrite: true); } catch { }
            }
        }

        return destPath;
    }

    // ── Orphan Cleanup ─────────────────────────────

    /// <summary>
    /// Delete thumbnail files and metadata entries for designs that no longer
    /// exist in Glamourer. Call once after plugin initialization.
    /// </summary>
    public void CleanupOrphanedThumbnails(ICollection<Guid> activeDesignIds, List<Models.DesignMetadata> metadata)
    {
        try
        {
            var activeSet = new HashSet<Guid>(activeDesignIds);
            var orphanedIds = new HashSet<Guid>();
            int filesDeleted = 0;

            if (!Directory.Exists(ThumbnailsDirectory))
                return;

            foreach (var file in Directory.GetFiles(ThumbnailsDirectory))
            {
                var fileName = Path.GetFileName(file);
                var idEnd = fileName.IndexOf('_');
                if (idEnd < 0)
                    idEnd = fileName.LastIndexOf('.');
                if (idEnd < 0)
                    continue;

                if (Guid.TryParse(fileName[..idEnd], out var designId) && !activeSet.Contains(designId))
                {
                    try
                    {
                        File.Delete(file);
                        orphanedIds.Add(designId);
                        filesDeleted++;
                        log.Information($"Cleaned up orphaned thumbnail: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        log.Warning(ex, $"Failed to delete orphaned thumbnail: {fileName}");
                    }
                }
            }

            // Also prune orphaned metadata entries
            int metadataRemoved = metadata.RemoveAll(m => orphanedIds.Contains(m.DesignId) || !activeSet.Contains(m.DesignId));

            if (filesDeleted > 0 || metadataRemoved > 0)
                log.Information($"Orphan cleanup: {filesDeleted} thumbnail(s), {metadataRemoved} metadata entry(s) removed");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed during orphaned thumbnail cleanup");
        }
    }

    // ── Scroll Lock ─────────────────────────────────

    public void ToggleGameUI()
    {
        var press = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION { ki = new KEYBDINPUT { wVk = VK_SCROLL } }
        };
        var release = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION { ki = new KEYBDINPUT { wVk = VK_SCROLL, dwFlags = KEYEVENTF_KEYUP } }
        };

        SendInput(1, new[] { press }, Marshal.SizeOf<INPUT>());
        Thread.Sleep(30);
        SendInput(1, new[] { release }, Marshal.SizeOf<INPUT>());
    }

    // ── File picker ─────────────────────────────────

    public void OpenImageFilePicker(Action<string> onFileSelected)
    {
        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "Select Image",
                    Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                    onFileSelected(dialog.FileName);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to open file picker");
            }
        });

        thread.TrySetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    // ── Clipboard ───────────────────────────────────

    public void CopyImageFromClipboard(Action<string> onImageSaved)
    {
        var thread = new Thread(() =>
        {
            try
            {
                log.Information("Attempting to get image from clipboard...");

                Image? image = null;
                string? sourceFilePath = null;

                try
                {
                    if (Clipboard.ContainsImage())
                    {
                        image = Clipboard.GetImage();
                        log.Information("Successfully retrieved image data from clipboard");
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        var files = Clipboard.GetFileDropList();
                        log.Information($"Clipboard contains {files.Count} file(s)");

                        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
                        foreach (var file in files)
                        {
                            if (string.IsNullOrEmpty(file)) continue;
                            var ext = Path.GetExtension(file).ToLower();
                            if (imageExtensions.Contains(ext) && File.Exists(file))
                            {
                                sourceFilePath = file;
                                log.Information($"Found image file in clipboard: {file}");
                                break;
                            }
                        }

                        if (sourceFilePath == null)
                            log.Warning("Clipboard contains files but no image files found");
                    }
                    else
                    {
                        log.Warning("Clipboard does not contain an image or image files");
                    }
                }
                catch (Exception clipboardEx)
                {
                    log.Error(clipboardEx, "Failed to access clipboard");
                    return;
                }

                if (image != null)
                {
                    using (image)
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                        var tempPath = Path.Combine(ThumbnailsDirectory, $"clipboard_{timestamp}_temp.png");
                        image.Save(tempPath, ImageFormat.Png);
                        var destBase = Path.Combine(ThumbnailsDirectory, $"clipboard_{timestamp}");
                        var finalPath = ResizeThumbnail(tempPath, destBase);
                        try { File.Delete(tempPath); } catch { }
                        log.Information($"Clipboard image saved & resized: {finalPath}");
                        onImageSaved(finalPath);
                    }
                }
                else if (sourceFilePath != null)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    var destBase = Path.Combine(ThumbnailsDirectory, $"clipboard_{timestamp}");
                    var finalPath = ResizeThumbnail(sourceFilePath, destBase);
                    log.Information($"Clipboard file resized: {finalPath}");
                    onImageSaved(finalPath);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed in CopyImageFromClipboard");
            }
        });

        thread.TrySetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        log.Information("Clipboard thread started");
    }

    // ── Migration ───────────────────────────────────

    /// <summary>
    /// Migrates data from the old Wardrobe plugin config to Vestiary.
    /// Deserializes Wardrobe.json with Newtonsoft.Json (which handles $type natively),
    /// merges all user data into the live config, copies thumbnails, fixes image paths,
    /// and saves. Data is visible immediately; no plugin reload required.
    /// Pass force=true to re-run even if previously migrated (used by the settings button).
    /// </summary>
    public void MigrateFromWardrobe(Configuration configuration, bool force = false)
    {
        try
        {
            var configsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher", "pluginConfigs");

            var oldConfigPath = Path.Combine(configsRoot, "Wardrobe.json");
            if (!File.Exists(oldConfigPath))
                oldConfigPath = Path.Combine(configsRoot, "Wardrobe", "Wardrobe.json");

            if (!File.Exists(oldConfigPath))
                return;

            var markerPath = Path.Combine(configsRoot, "Vestiary", ".migrated_from_wardrobe");

            if (!force && File.Exists(markerPath))
                return;

            log.Information("[Migration] Wardrobe config found — migrating to Vestiary...");

            Directory.CreateDirectory(Path.Combine(configsRoot, "Vestiary"));
            Directory.CreateDirectory(ThumbnailsDirectory);

            var json = File.ReadAllText(oldConfigPath);
            var typeMap = new Dictionary<string, string>
            {
                { @"""Wardrobe.Configuration, Wardrobe""",  @"""Vestiary.Configuration, Vestiary""" },
                { @"""Wardrobe.Models.Collection, Wardrobe""",  @"""Vestiary.Models.Collection, Vestiary""" },
                { @"""Wardrobe.Models.DesignMetadata, Wardrobe""",  @"""Vestiary.Models.DesignMetadata, Vestiary""" },
                { @"""Wardrobe.Models.EmoteCollection, Wardrobe""",  @"""Vestiary.Models.EmoteCollection, Vestiary""" },
                { @"""Wardrobe.Models.ModEntry, Wardrobe""",  @"""Vestiary.Models.ModEntry, Vestiary""" },
                { @"""Wardrobe.Models.ModSnapshot, Wardrobe""",  @"""Vestiary.Models.ModSnapshot, Vestiary""" },
            };
            foreach (var kv in typeMap)
                json = json.Replace(kv.Key, kv.Value);

            var oldConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(json);
            if (oldConfig == null)
            {
                log.Warning("[Migration] Failed to deserialize Wardrobe.json");
                return;
            }

            // Route ModSnapshots to the dedicated file (they're no longer in Configuration)
            var snapshots = Newtonsoft.Json.JsonConvert.DeserializeObject<LegacyMigrationConfig>(json)?.ModSnapshots;
            if (snapshots?.Count > 0 && modStateService != null)
            {
                modStateService.ImportSnapshots(snapshots);
                log.Information($"[Migration]   {snapshots.Count} snapshots to mod-snapshots.json");
            }

            configuration.Collections = oldConfig.Collections ?? new();
            configuration.DesignMetadata = oldConfig.DesignMetadata ?? new();
            configuration.HiddenDesignIds = oldConfig.HiddenDesignIds ?? new();
            configuration.FavoriteDesignIds = oldConfig.FavoriteDesignIds ?? new();
            configuration.EmoteCards = oldConfig.EmoteCards ?? new();
            configuration.EmoteCollections = oldConfig.EmoteCollections ?? new();
            configuration.ApplyEquipmentOnly = oldConfig.ApplyEquipmentOnly;
            configuration.ShowHidden = oldConfig.ShowHidden;
            configuration.EnableSaveMods = oldConfig.EnableSaveMods;
            configuration.EnableEmotes = oldConfig.EnableEmotes;
            configuration.EnableGlamourRoulette = oldConfig.EnableGlamourRoulette;
            configuration.RouletteActive = oldConfig.RouletteActive;
            configuration.RouletteIntervalMinutes = oldConfig.RouletteIntervalMinutes;
            configuration.RouletteExcludeFavorites = oldConfig.RouletteExcludeFavorites;
            configuration.RouletteCollectionIds = oldConfig.RouletteCollectionIds ?? new();
            configuration.IsMinimized = oldConfig.IsMinimized;
            configuration.ThemeName = oldConfig.ThemeName ?? "Ocean";

            log.Information(
                $"[Migration]   Loaded: {configuration.Collections.Count} collections, " +
                $"{configuration.DesignMetadata.Count} metadata, " +
                $"{configuration.FavoriteDesignIds.Count} favorites, " +
                $"{configuration.EmoteCards.Count} emote cards");

            var oldThumbsDir = Path.Combine(configsRoot, "Wardrobe", "thumbnails");
            if (Directory.Exists(oldThumbsDir))
            {
                foreach (var file in Directory.GetFiles(oldThumbsDir))
                {
                    var dest = Path.Combine(ThumbnailsDirectory, Path.GetFileName(file));
                    if (!File.Exists(dest))
                    {
                        File.Copy(file, dest);
                        log.Information($"[Migration]   Thumbnail: {Path.GetFileName(file)}");
                    }
                }
            }

            foreach (var meta in configuration.DesignMetadata)
            {
                if (string.IsNullOrEmpty(meta.CustomImagePath)) continue;
                var fileName = Path.GetFileName(meta.CustomImagePath);
                var newPath = Path.Combine(ThumbnailsDirectory, fileName);
                if (File.Exists(newPath))
                    meta.CustomImagePath = newPath;
            }
            foreach (var card in configuration.EmoteCards)
            {
                if (string.IsNullOrEmpty(card.ThumbnailPath)) continue;
                var fileName = Path.GetFileName(card.ThumbnailPath);
                var newPath = Path.Combine(ThumbnailsDirectory, fileName);
                if (File.Exists(newPath))
                    card.ThumbnailPath = newPath;
            }

            configuration.Save();
            File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
            log.Information("[Migration] Wardrobe → Vestiary migration complete.");
        }
        catch (Exception ex)
        {
            log.Warning(ex, "[Migration] Wardrobe → Vestiary migration error (non-fatal)");
        }
    }

    /// <summary>Minimal DTO for extracting ModSnapshots from old Wardrobe.json during migration.</summary>
    [Serializable]
    private class LegacyMigrationConfig
    {
        public List<Models.ModSnapshot>? ModSnapshots { get; set; }
    }

}
