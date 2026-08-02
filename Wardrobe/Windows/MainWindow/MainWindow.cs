using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public partial class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly string noPreviewImagePath;
    private readonly string cameraIconPath;
    private readonly string uploadIconPath;
    private readonly string clipboardIconPath;
    private readonly string viewIconPath;
    private readonly string hiddenIconPath;
    private readonly string saveModsIconPath;
    private readonly string starEmptyPath;
    private readonly string starFilledPath;
    private readonly string searchIconPath;
    private readonly Plugin plugin;
    private readonly UtilityService utility;
    private readonly CollectionService collectionService;
    private readonly DesignMetadataService designMetadataService;
    private readonly HiddenDesignService hiddenDesignService;
    private readonly FavoriteService favoriteService;
    private CollectionEditorWindow? collectionEditorWindow;
    private Guid selectedCollectionId = Guid.Empty;
    private int dragTabIndex = -1;
    private Guid editingDesignId = Guid.Empty;
    private string editingDesignName = string.Empty;
    private string searchText = string.Empty;
    private int _currentView = 0; // 0=Glamour, 1=Emotes
    private string _emoteModSearch = string.Empty;
    private Guid _justOpenedModCombo;
    private Guid _editingCardId;
    internal string _pendingEmoteCommand = string.Empty; // which card is in edit mode // which card's mod dropdown is open

    public MainWindow(
        Plugin plugin,
        UtilityService utility,
        string goatImagePath,
        CollectionService collectionService,
        DesignMetadataService designMetadataService,
        HiddenDesignService hiddenDesignService,
        FavoriteService favoriteService,
        string noPreviewImagePath,
        string cameraIconPath,
        string uploadIconPath,
        string clipboardIconPath,
        string viewIconPath,
        string hiddenIconPath,
        string starEmptyPath,
        string starFilledPath,
        string searchIconPath,
        string saveModsIconPath
    )
        : base("Wardrobe##With a hidden ID", ImGuiWindowFlags.None)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.goatImagePath = goatImagePath;
        this.noPreviewImagePath = noPreviewImagePath;
        this.cameraIconPath = cameraIconPath;
        this.uploadIconPath = uploadIconPath;
        this.clipboardIconPath = clipboardIconPath;
        this.viewIconPath = viewIconPath;
        this.hiddenIconPath = hiddenIconPath;
        this.starEmptyPath = starEmptyPath;
        this.starFilledPath = starFilledPath;
        this.searchIconPath = searchIconPath;
        this.saveModsIconPath = saveModsIconPath;
        this.plugin = plugin;
        this.utility = utility;
        this.collectionService = collectionService;
        this.designMetadataService = designMetadataService;
        this.hiddenDesignService = hiddenDesignService;
        this.favoriteService = favoriteService;
        this.collectionEditorWindow = null!;
    }

    public void SetCollectionEditorWindow(CollectionEditorWindow editor) =>
        collectionEditorWindow = editor;

    public void ShowEmotes() => _currentView = 1;

    /// <summary>
    /// Gets designs for a collection. Handles the special "Favorites" case via service.
    /// </summary>
    private Dictionary<Guid, (string, string, uint, bool)> GetDesignsForCollection(Guid collectionId)
    {
        var fav = collectionService.GetCollections().FirstOrDefault(c => c.Id == collectionId);
        if (fav != null && fav.Name == "Favorites")
        {
            var favDesigns = favoriteService.GetFavoritesFromAllCollections(collectionService.GetDesignsByCollection);
            // Auto-clean: if no designs left, clear stale favorites
            if (favDesigns.Count == 0 && plugin.Configuration.FavoriteDesignIds.Count > 0)
            {
                plugin.Configuration.FavoriteDesignIds.Clear();
                plugin.Configuration.Collections.RemoveAll(c => c.Name == "Favorites");
                plugin.Configuration.Save();
                selectedCollectionId = plugin.Configuration.Collections.Count > 0 ? plugin.Configuration.Collections[0].Id : Guid.Empty;
            }
            return favDesigns;
        }
        return collectionService.GetDesignsByCollection(collectionId);
    }

    public void Dispose() { }

    /// <summary>
    /// True when a sub-window (config, guide) is open and should block main window interaction.
    /// </summary>
    private bool IsInteractionBlocked =>
        plugin.IsConfigOpen || plugin.GuideWin.IsOpen;

    /// <summary>
    /// Filters designs by search text. Matches nickname first, then Glamourer display name. Case-insensitive.
    /// </summary>
    private Dictionary<Guid, (string, string, uint, bool)> FilterBySearch(
        Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> designs)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return designs;

        var q = searchText.Trim().ToLower();
        return designs.Where(d =>
        {
            var nick = designMetadataService.GetDisplayName(d.Key);
            return nick.ToLower().Contains(q) || d.Value.DisplayName.ToLower().Contains(q);
        }).ToDictionary(d => d.Key, d => d.Value);
    }

    public override void Draw()
    {
        if (plugin.IsCameraActive)
            return;

        try
        {
            if (!plugin.Configuration.EnableEmotes) _currentView = 0;

            var collections = collectionService.GetCollections();

            if (selectedCollectionId == Guid.Empty && collections.Count > 0)
                selectedCollectionId = collections[0].Id;

            if (selectedCollectionId != Guid.Empty && !collections.Any(c => c.Id == selectedCollectionId))
                selectedCollectionId = collections.Count > 0 ? collections[0].Id : Guid.Empty;

            var sortedCollections = collections.OrderBy(c => c.Order).ToList();
            var dl = ImGui.GetWindowDrawList();

            // ── Full-width top bar: WARDROBE + search ──
            DrawTopBar();
            ImGui.Spacing();

            // Separator line
            var sepPos = ImGui.GetCursorScreenPos();
            dl.AddLine(
                new Vector2(sepPos.X, sepPos.Y),
                new Vector2(sepPos.X + ImGui.GetContentRegionAvail().X, sepPos.Y),
                ImGui.GetColorU32(ThemeManager.Current.RailDivider), 1f);
            ImGui.Spacing();

            // ── Split: left rail + right content ──
            DrawRail();

            ImGui.SameLine();

            ImGui.BeginChild("##MainContent", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar);

            // Emote view: skip chips + status, go straight to gallery
            if (_currentView == 1)
            {
                ImGui.Dummy(new Vector2(0, 4f));
                DrawEmoteGallery();
                ImGui.EndChild();
                return;
            }

            // Chip row + status row on one line: chips left, hidden+count right
            ImGui.Dummy(new Vector2(0, 5f));
            DrawChipAndStatusRow(sortedCollections);
            ImGui.Dummy(new Vector2(0, 16f));

            // Gallery
            DrawGalleryContent();

            ImGui.EndChild();
        }
        catch (Exception)
        {
            ImGui.TextColored(ThemeManager.Current.TextError, Strings.GlamourerNotFound);
        }
    }
}
