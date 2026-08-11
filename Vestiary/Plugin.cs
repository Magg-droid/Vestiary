using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using Vestiary.Services;
using Vestiary.Windows;

namespace Vestiary;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private const string CommandName = "/vestiary";
    private const string ShortCommandName = "/vs";
    private const string GuideCommandName = "/vsguide";
    private const string EmotesCommandName = "/vsemotes";
    private const string RandomCommandName = "/vsrandom";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Vestiary");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    internal GuideWindow GuideWin { get; init; }
    private CollectionEditorWindow CollectionEditorWindow { get; init; }
    private DesignEditorWindow DesignEditorWindow { get; init; }
    private CameraWindow CameraWindow { get; init; }

    public GlamourerService GlamourerService { get; init; }
    public CollectionService CollectionService { get; init; }
    public DesignMetadataService DesignMetadataService { get; init; }
    public HiddenDesignService HiddenDesignService { get; init; }
    public PenumbraService PenumbraService { get; init; }
    public ModStateService ModStateService { get; init; }
    public EmoteService EmoteService { get; init; }
    public FavoriteService FavoriteService { get; init; }
    public UtilityService UtilityService { get; init; }
    public RouletteService RouletteService { get; init; }
    public TextureCache TextureCache { get; init; }

    public bool IsConfigOpen => ConfigWindow.IsOpen;
    public bool IsCameraActive { get; private set; }
    private bool wasMainWindowOpen;
    private bool wasDesignEditorOpen;
    private Guid lastRandomCommandDesignId = Guid.Empty;

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
        EmoteService = new EmoteService(Configuration, PenumbraService);
        FavoriteService = new FavoriteService(Configuration, CollectionService);
        UtilityService = new UtilityService(pluginDir, Log, Configuration);
        RouletteService = new RouletteService(Configuration, GlamourerService, ModStateService, CollectionService, HiddenDesignService, Framework);
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
        GuideWin = new GuideWindow(TextureCache,
            cameraIconPath, uploadIconPath, clipboardIconPath,
            starFilledPath, saveModsIconPath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(CollectionEditorWindow);
        WindowSystem.AddWindow(DesignEditorWindow);
        WindowSystem.AddWindow(CameraWindow);
        WindowSystem.AddWindow(GuideWin);

        MainWindow.SetCollectionEditorWindow(CollectionEditorWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = Strings.CommandHelpOpenPlugin
        });
        CommandManager.AddHandler(ShortCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = Strings.CommandHelpOpenPluginShortcut
        });
        CommandManager.AddHandler(GuideCommandName, new CommandInfo(OnGuideCommand)
        {
            HelpMessage = Strings.CommandHelpOpenGuide
        });
        CommandManager.AddHandler(EmotesCommandName, new CommandInfo(OnEmotesCommand)
        {
            HelpMessage = Strings.CommandHelpOpenEmotes
        });
        CommandManager.AddHandler(RandomCommandName, new CommandInfo(OnRandomCommand)
        {
            HelpMessage = Strings.CommandHelpRandom
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw += ProcessPendingEmote;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        ECommonsMain.Init(PluginInterface, this);

        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= ProcessPendingEmote;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        CameraWindow.Dispose();
        GuideWin.Dispose();
        RouletteService.Dispose();
        TextureCache.Dispose();

        ECommonsMain.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(ShortCommandName);
        CommandManager.RemoveHandler(GuideCommandName);
        CommandManager.RemoveHandler(EmotesCommandName);
        CommandManager.RemoveHandler(RandomCommandName);

    }

    private void OnCommand(string command, string args)
    {
        try
        {
            var designs = GlamourerService.GetDesignList();
            Log.Information($"Vestiary found {designs.Count} Glamourer designs.");
        }
        catch (Exception ex)
        {
            Log.Error($"Glamourer not found or not installed: {ex.Message}");
        }

        MainWindow.Toggle();
    }

    private void ProcessPendingEmote()
    {
        if (string.IsNullOrEmpty(MainWindow._pendingEmoteCommand)) return;
        var cmd = MainWindow._pendingEmoteCommand;
        MainWindow._pendingEmoteCommand = string.Empty;
        Chat.SendMessage(cmd);
        Plugin.Log.Information($"[Emotes] Sent: {cmd}");
    }

    private void OnGuideCommand(string command, string args)
    {
        GuideWin.Toggle();
    }

    private void OnEmotesCommand(string command, string args)
    {
        if (!Configuration.EnableEmotes) return;
        MainWindow.ShowEmotes();
        MainWindow.Toggle();
    }

    private void OnRandomCommand(string command, string args)
    {
        try
        {
            if (!TryPickRandomDesign(args, out var designId, out var sourceLabel, out var reason))
            {
                Log.Warning($"[Random] {reason}");
                return;
            }

            CloseSubWindows();
            GlamourerService.ApplyDesign(designId, Configuration.ApplyEquipmentOnly);
            ModStateService.RestoreState(designId);
            Log.Information($"[Random] Applied design {designId} from {sourceLabel}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Random] Failed to apply random design.");
        }
    }

    private bool TryPickRandomDesign(string args, out Guid designId, out string sourceLabel, out string reason)
    {
        designId = Guid.Empty;
        sourceLabel = string.Empty;
        reason = string.Empty;

        var collections = CollectionService.GetCollections()
            .OrderBy(c => c.Order)
            .ToList();
        if (collections.Count == 0)
        {
            reason = Strings.RandomCommandNoCollections;
            return false;
        }

        Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> pool;
        var requestedName = args.Trim();

        if (string.IsNullOrWhiteSpace(requestedName))
        {
            var nonFavoriteCollections = collections
                .Where(c => !string.Equals(c.Name, "Favorites", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (nonFavoriteCollections.Count == 0)
            {
                reason = Strings.RandomCommandNoNonFavoriteCollections;
                return false;
            }

            var merged = new Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>();
            foreach (var collection in nonFavoriteCollections)
            {
                foreach (var entry in CollectionService.GetDesignsByCollection(collection.Id))
                {
                    if (!merged.ContainsKey(entry.Key))
                        merged.Add(entry.Key, entry.Value);
                }
            }

            pool = merged;
            sourceLabel = "all collections (excluding Favorites)";
        }
        else
        {
            var matches = collections
                .Where(c => string.Equals(c.Name, requestedName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 0)
            {
                reason = Strings.RandomCommandCollectionNotFound(requestedName);
                return false;
            }
            if (matches.Count > 1)
            {
                reason = Strings.RandomCommandDuplicateCollection(requestedName);
                return false;
            }

            var target = matches[0];
            pool = CollectionService.GetDesignsByCollection(target.Id);
            sourceLabel = $"collection '{target.Name}'";
        }

        var visiblePool = HiddenDesignService.GetVisibleDesigns(pool);
        if (visiblePool.Count == 0)
        {
            reason = Strings.RandomCommandNoVisibleDesigns(sourceLabel);
            return false;
        }

        if (!RandomSelectionHelper.TryPickDesign(visiblePool, ref lastRandomCommandDesignId, out designId))
        {
            reason = Strings.RandomCommandNoVisibleDesigns(sourceLabel);
            return false;
        }

        return true;
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
