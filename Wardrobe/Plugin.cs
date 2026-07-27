using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Wardrobe.Windows;
using Wardrobe.Services;

namespace Wardrobe;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/wardrobe";
    private const string ShortCommandName = "/wr";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Wardrobe");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private CollectionEditorWindow CollectionEditorWindow { get; init; }
    private DesignEditorWindow DesignEditorWindow { get; init; }
    private CameraWindow CameraWindow { get; init; }

    public GlamourerService GlamourerService { get; init; }
    public CollectionService CollectionService { get; init; }
    public DesignMetadataService DesignMetadataService { get; init; }
    public TextureCache TextureCache { get; init; }
    public string PluginDirectory { get; private set; } = string.Empty;

    /// <summary>Persistent thumbnails directory in plugin config folder (survives version bumps).</summary>
    public string ThumbnailsDirectory { get; private set; } = string.Empty;

    /// <summary>Whether the camera overlay is currently active (suppresses other windows).</summary>
    public bool IsCameraActive { get; private set; }

    // Window state saved before camera opens, restored after camera closes
    private bool wasMainWindowOpen;
    private bool wasDesignEditorOpen;

    // ── P/Invoke for SendInput (works with DirectInput games like FFXIV) ──
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

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        GlamourerService = new GlamourerService(PluginInterface, Log);
        CollectionService = new CollectionService(Configuration, GlamourerService);
        DesignMetadataService = new DesignMetadataService(Configuration, GlamourerService);
        TextureCache = new TextureCache(TextureProvider);

        // Load plugin assets
        var pluginDir = PluginInterface.AssemblyLocation.Directory?.FullName!;
        PluginDirectory = pluginDir;

        // Persistent thumbnails in plugin config folder (survives version bumps)
        // Dalamud stores config at: %appdata%/XIVLauncher/pluginConfigs/{PluginName}/
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "pluginConfigs", PluginInterface.Manifest.Name);
        ThumbnailsDirectory = Path.Combine(configDir, "thumbnails");
        Directory.CreateDirectory(ThumbnailsDirectory);
        Log.Information($"Thumbnails directory: {ThumbnailsDirectory}");

        // Migrate any existing thumbnails from the old versioned folder
        MigrateThumbnails(pluginDir);

        var goatImagePath = Path.Combine(pluginDir, "goat.png");
        var noPreviewImagePath = Path.Combine(pluginDir, "..", "..", "Data", "no-preview.jpg");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath, CollectionService, DesignMetadataService, noPreviewImagePath);
        CollectionEditorWindow = new CollectionEditorWindow(this, CollectionService);
        DesignEditorWindow = new DesignEditorWindow(this, DesignMetadataService, GlamourerService);
        CameraWindow = new CameraWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(CollectionEditorWindow);
        WindowSystem.AddWindow(DesignEditorWindow);
        WindowSystem.AddWindow(CameraWindow);
        
        MainWindow.SetCollectionEditorWindow(CollectionEditorWindow);
        MainWindow.SetDesignEditorWindow(DesignEditorWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Wardrobe plugin"
        });
        CommandManager.AddHandler(ShortCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Wardrobe plugin (shortcut)"
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [Wardrobe] ===A cool log message from Wardrobe===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        CameraWindow.Dispose();
        TextureCache.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(ShortCommandName);
    }

    private void OnCommand(string command, string args)
    {
        try
        {
            var designs = GlamourerService.GetDesignList();
            Log.Information($"Wardrobe found {designs.Count} Glamourer designs.");
        }
        catch (Exception ex)
        {
            Log.Error($"Glamourer not found or not installed: {ex.Message}");
        }
        
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();

    /// <summary>Close all sub-windows (editor, config) — keeps main window open.</summary>
    public void CloseSubWindows()
    {
        DesignEditorWindow.IsOpen = false;
        CollectionEditorWindow.IsOpen = false;
        ConfigWindow.IsOpen = false;
    }

    public void ShowCameraOverlay(Action<string> onImageCaptured)
    {
        // Save current window states so we can restore them later
        wasMainWindowOpen = MainWindow.IsOpen;
        wasDesignEditorOpen = DesignEditorWindow.IsOpen;

        // Hide all plugin windows
        MainWindow.IsOpen = false;
        DesignEditorWindow.IsOpen = false;
        CollectionEditorWindow.IsOpen = false;
        ConfigWindow.IsOpen = false;

        // Toggle game UI off
        ToggleGameUI();

        IsCameraActive = true;
        CameraWindow.Open(onImageCaptured);
    }

    /// <summary>Called by CameraWindow when it closes, to restore everything.</summary>
    public void OnCameraClosed()
    {
        // Toggle game UI back on
        ToggleGameUI();

        IsCameraActive = false;

        // Restore windows that were open before camera
        if (wasMainWindowOpen)
            MainWindow.IsOpen = true;
        if (wasDesignEditorOpen)
            DesignEditorWindow.IsOpen = true;
    }

    /// <summary>
    /// Simulate a Scroll Lock key press using SendInput.
    /// SendInput works with DirectInput-based games (like FFXIV) where keybd_event may fail.
    /// </summary>
    private static void ToggleGameUI()
    {
        var press = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_SCROLL,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        var release = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_SCROLL,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { press }, Marshal.SizeOf<INPUT>());
        Thread.Sleep(30);
        SendInput(1, new[] { release }, Marshal.SizeOf<INPUT>());
    }

    public void OpenImageFilePicker(Action<string> onFileSelected)
    {
        // Windows Forms requires STA thread
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Image",
                    Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    onFileSelected(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open file picker");
            }
        });
        
        thread.TrySetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
    }

    public void CopyImageFromClipboard(Action<string> onImageSaved)
    {
        // Windows Forms clipboard access requires STA thread
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                Log.Information("Attempting to get image from clipboard...");
                
                // Get image from clipboard
                Image? image = null;
                string? sourceFilePath = null;
                try
                {
                    if (Clipboard.ContainsImage())
                    {
                        image = Clipboard.GetImage();
                        Log.Information("Successfully retrieved image data from clipboard");
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        // Check if file was copied from File Explorer (Ctrl+C)
                        var files = Clipboard.GetFileDropList();
                        Log.Information($"Clipboard contains {files.Count} file(s)");
                        
                        // Find first image file
                        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
                        foreach (var file in files)
                        {
                            if (string.IsNullOrEmpty(file))
                                continue;
                            
                            var ext = Path.GetExtension(file).ToLower();
                            if (imageExtensions.Contains(ext) && File.Exists(file))
                            {
                                sourceFilePath = file;
                                Log.Information($"Found image file in clipboard: {file}");
                                break;
                            }
                        }
                        
                        if (sourceFilePath == null)
                        {
                            Log.Warning("Clipboard contains files but no image files found");
                        }
                    }
                    else
                    {
                        Log.Warning("Clipboard does not contain an image or image files");
                    }
                }
                catch (Exception clipboardEx)
                {
                    Log.Error(clipboardEx, "Failed to access clipboard");
                    return;
                }

                // Process clipboard image data
                if (image != null)
                {
                    try
                    {
                        using (image)
                        {
                            // Generate unique filename based on timestamp
                            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                            var filename = $"clipboard_{timestamp}.png";
                            var savePath = Path.Combine(ThumbnailsDirectory, filename);

                            // Save image as PNG
                            image.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);

                            Log.Information($"Image saved to: {savePath}");
                            onImageSaved(savePath);
                        }
                    }
                    catch (Exception saveEx)
                    {
                        Log.Error(saveEx, "Failed to save image");
                    }
                }
                // Process file from File Explorer
                else if (sourceFilePath != null)
                {
                    try
                    {
                        // Generate filename with original extension
                        var originalExtension = Path.GetExtension(sourceFilePath);
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                        var filename = $"clipboard_{timestamp}{originalExtension}";
                        var savePath = Path.Combine(ThumbnailsDirectory, filename);

                        // Copy file
                        File.Copy(sourceFilePath, savePath, overwrite: true);

                        Log.Information($"Image file copied to: {savePath}");
                        onImageSaved(savePath);
                    }
                    catch (Exception copyEx)
                    {
                        Log.Error(copyEx, "Failed to copy image file");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed in CopyImageFromClipboard");
            }
        });

        thread.TrySetApartmentState(System.Threading.ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Log.Information("Clipboard thread started");
    }

    /// <summary>
    /// One-time migration: copy thumbnails from the old versioned folder into the persistent config folder,
    /// and fix up stored metadata paths.
    /// </summary>
    private void MigrateThumbnails(string oldVersionDir)
    {
        try
        {
            var oldDir = Path.Combine(oldVersionDir, "thumbnails");
            if (!Directory.Exists(oldDir))
                return;

            var migrated = false;
            foreach (var file in Directory.GetFiles(oldDir))
            {
                var dest = Path.Combine(ThumbnailsDirectory, Path.GetFileName(file));
                if (!File.Exists(dest))
                {
                    File.Copy(file, dest, overwrite: false);
                    Log.Information($"Migrated thumbnail to config: {Path.GetFileName(file)}");
                    migrated = true;
                }
            }

            if (migrated)
            {
                foreach (var meta in Configuration.DesignMetadata)
                {
                    if (string.IsNullOrEmpty(meta.CustomImagePath))
                        continue;
                    var fileName = Path.GetFileName(meta.CustomImagePath);
                    var newPath = Path.Combine(ThumbnailsDirectory, fileName);
                    if (File.Exists(newPath))
                        meta.CustomImagePath = newPath;
                }
                Configuration.Save();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Thumbnail migration error (non-fatal)");
        }
    }
}
