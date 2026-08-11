using System;
using System.Drawing;
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

    public string ThumbnailsDirectory { get; }

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

    public UtilityService(string pluginDir, IPluginLog log, Configuration configuration)
    {
        this.pluginDir = pluginDir;
        this.log = log;

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "pluginConfigs",
            "Vestiary");
        ThumbnailsDirectory = Path.Combine(configDir, "thumbnails");
        Directory.CreateDirectory(ThumbnailsDirectory);
        log.Information($"Thumbnails directory: {ThumbnailsDirectory}");

        MigrateThumbnails(configuration);
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
                        var filename = $"clipboard_{timestamp}.png";
                        var savePath = Path.Combine(ThumbnailsDirectory, filename);
                        image.Save(savePath, ImageFormat.Png);
                        log.Information($"Image saved to: {savePath}");
                        onImageSaved(savePath);
                    }
                }
                else if (sourceFilePath != null)
                {
                    var originalExtension = Path.GetExtension(sourceFilePath);
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    var filename = $"clipboard_{timestamp}{originalExtension}";
                    var savePath = Path.Combine(ThumbnailsDirectory, filename);
                    File.Copy(sourceFilePath, savePath, overwrite: true);
                    log.Information($"Image file copied to: {savePath}");
                    onImageSaved(savePath);
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

    private void MigrateThumbnails(Configuration configuration)
    {
        try
        {
            var oldDir = Path.Combine(pluginDir, "thumbnails");
            if (!Directory.Exists(oldDir)) return;

            var migrated = false;
            foreach (var file in Directory.GetFiles(oldDir))
            {
                var dest = Path.Combine(ThumbnailsDirectory, Path.GetFileName(file));
                if (!File.Exists(dest))
                {
                    File.Copy(file, dest, overwrite: false);
                    log.Information($"Migrated thumbnail to config: {Path.GetFileName(file)}");
                    migrated = true;
                }
            }

            if (migrated)
            {
                foreach (var meta in configuration.DesignMetadata)
                {
                    if (string.IsNullOrEmpty(meta.CustomImagePath)) continue;
                    var fileName = Path.GetFileName(meta.CustomImagePath);
                    var newPath = Path.Combine(ThumbnailsDirectory, fileName);
                    if (File.Exists(newPath))
                        meta.CustomImagePath = newPath;
                }
                configuration.Save();
            }
        }
        catch (Exception ex)
        {
            log.Warning(ex, "Thumbnail migration error (non-fatal)");
        }
    }
}
