using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
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

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Wardrobe");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private CollectionEditorWindow CollectionEditorWindow { get; init; }
    private DesignEditorWindow DesignEditorWindow { get; init; }

    public GlamourerService GlamourerService { get; init; }
    public CollectionService CollectionService { get; init; }
    public DesignMetadataService DesignMetadataService { get; init; }
    public TextureCache TextureCache { get; init; }
    public string PluginDirectory { get; private set; } = string.Empty;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        GlamourerService = new GlamourerService(PluginInterface);
        CollectionService = new CollectionService(Configuration, GlamourerService);
        DesignMetadataService = new DesignMetadataService(Configuration, GlamourerService);
        TextureCache = new TextureCache(TextureProvider);

        // Load plugin assets
        var pluginDir = PluginInterface.AssemblyLocation.Directory?.FullName!;
        PluginDirectory = pluginDir;
        var goatImagePath = Path.Combine(pluginDir, "goat.png");
        var noPreviewImagePath = Path.Combine(pluginDir, "..", "..", "Data", "no-preview.jpg");

        // Create thumbnails folder for design images
        var thumbnailsDir = Path.Combine(pluginDir, "thumbnails");
        Directory.CreateDirectory(thumbnailsDir);

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath, CollectionService, DesignMetadataService, noPreviewImagePath);
        CollectionEditorWindow = new CollectionEditorWindow(CollectionService);
        DesignEditorWindow = new DesignEditorWindow(this, DesignMetadataService, GlamourerService);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(CollectionEditorWindow);
        WindowSystem.AddWindow(DesignEditorWindow);
        
        MainWindow.SetCollectionEditorWindow(CollectionEditorWindow);
        MainWindow.SetDesignEditorWindow(DesignEditorWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
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
        TextureCache.Dispose();

        CommandManager.RemoveHandler(CommandName);
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
                try
                {
                    if (Clipboard.ContainsImage())
                    {
                        image = Clipboard.GetImage();
                        Log.Information("Successfully retrieved image from clipboard");
                    }
                    else
                    {
                        Log.Warning("Clipboard does not contain an image");
                    }
                }
                catch (Exception clipboardEx)
                {
                    Log.Error(clipboardEx, "Failed to access clipboard");
                    return;
                }

                if (image != null)
                {
                    try
                    {
                        using (image)
                        {
                            // Generate unique filename based on timestamp
                            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                            var filename = $"clipboard_{timestamp}.png";
                            var thumbnailsDir = Path.Combine(PluginDirectory, "thumbnails");
                            var savePath = Path.Combine(thumbnailsDir, filename);

                            // Create thumbnails directory if needed
                            Directory.CreateDirectory(thumbnailsDir);

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
}
