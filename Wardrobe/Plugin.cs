using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Wardrobe.Services;
using Wardrobe.Windows;

namespace Wardrobe;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;

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
    public HiddenDesignService HiddenDesignService { get; init; }
    public PenumbraService PenumbraService { get; init; }
    public ModStateService ModStateService { get; init; }
    public FavoriteService FavoriteService { get; init; }
    public UtilityService UtilityService { get; init; }
    public TextureCache TextureCache { get; init; }

    public bool IsCameraActive { get; private set; }
    private bool wasMainWindowOpen;
    private bool wasDesignEditorOpen;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ThemeManager.SetTheme(Configuration.ThemeName);

        var pluginDir = PluginInterface.AssemblyLocation.Directory?.FullName!;

        GlamourerService = new GlamourerService(PluginInterface, Log);
        CollectionService = new CollectionService(Configuration, GlamourerService);
        DesignMetadataService = new DesignMetadataService(Configuration, GlamourerService);
        HiddenDesignService = new HiddenDesignService(Configuration);
        PenumbraService = new PenumbraService(PluginInterface, Log, DataManager);
        ModStateService = new ModStateService(Configuration, PenumbraService, GlamourerService);
        FavoriteService = new FavoriteService(Configuration, CollectionService);
        UtilityService = new UtilityService(pluginDir, Log, Configuration);
        TextureCache = new TextureCache(TextureProvider);

        var goatImagePath = Path.Combine(pluginDir, "goat.png");
        var noPreviewImagePath = Path.Combine(pluginDir, "..", "..", "Data", "no-preview.jpg");
        var cameraIconPath = Path.Combine(pluginDir, "camera_icon.png");
        var uploadIconPath = Path.Combine(pluginDir, "upload_icon.png");
        var clipboardIconPath = Path.Combine(pluginDir, "clipboard_icon.png");
        var viewIconPath = Path.Combine(pluginDir, "view.png");
        var hiddenIconPath = Path.Combine(pluginDir, "hidden.png");
        var saveModsIconPath = Path.Combine(pluginDir, "save_mods_icon.png");
        var starEmptyPath = Path.Combine(pluginDir, "star_empty.png");
        var starFilledPath = Path.Combine(pluginDir, "star_filled.png");
        var searchIconPath = Path.Combine(pluginDir, "search_icon.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, UtilityService, goatImagePath, CollectionService,
            DesignMetadataService, HiddenDesignService, FavoriteService, noPreviewImagePath,
            cameraIconPath, uploadIconPath, clipboardIconPath, viewIconPath, hiddenIconPath,
            starEmptyPath, starFilledPath, searchIconPath, saveModsIconPath);
        CollectionEditorWindow = new CollectionEditorWindow(this, CollectionService);
        DesignEditorWindow = new DesignEditorWindow(this, UtilityService, DesignMetadataService, GlamourerService);
        CameraWindow = new CameraWindow(this, UtilityService);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(CollectionEditorWindow);
        WindowSystem.AddWindow(DesignEditorWindow);
        WindowSystem.AddWindow(CameraWindow);

        MainWindow.SetCollectionEditorWindow(CollectionEditorWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Wardrobe plugin"
        });
        CommandManager.AddHandler(ShortCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Wardrobe plugin (shortcut)"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
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

        MainWindow.Toggle();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();

    public void CloseSubWindows()
    {
        DesignEditorWindow.IsOpen = false;
        CollectionEditorWindow.IsOpen = false;
        ConfigWindow.IsOpen = false;
    }

    public void ShowCameraOverlay(Action<string> onImageCaptured)
    {
        wasMainWindowOpen = MainWindow.IsOpen;
        wasDesignEditorOpen = DesignEditorWindow.IsOpen;

        MainWindow.IsOpen = false;
        DesignEditorWindow.IsOpen = false;
        CollectionEditorWindow.IsOpen = false;
        ConfigWindow.IsOpen = false;

        UtilityService.ToggleGameUI();

        IsCameraActive = true;
        CameraWindow.Open(onImageCaptured);
    }

    public void OnCameraClosed()
    {
        UtilityService.ToggleGameUI();

        IsCameraActive = false;

        if (wasMainWindowOpen) MainWindow.IsOpen = true;
        if (wasDesignEditorOpen) DesignEditorWindow.IsOpen = true;
    }
}
