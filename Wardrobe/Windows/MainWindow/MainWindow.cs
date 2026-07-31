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
    private readonly string lockIconPath;
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
        string lockIconPath,
        string starEmptyPath,
        string starFilledPath,
        string searchIconPath
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
        this.lockIconPath = lockIconPath;
        this.starEmptyPath = starEmptyPath;
        this.starFilledPath = starFilledPath;
        this.searchIconPath = searchIconPath;
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

    /// <summary>
    /// Gets designs for a collection. Handles the special "Favorites" case via service.
    /// </summary>
    private Dictionary<Guid, (string, string, uint, bool)> GetDesignsForCollection(Guid collectionId)
    {
        var fav = collectionService.GetCollections().FirstOrDefault(c => c.Id == collectionId);
        if (fav != null && fav.Name == "Favorites")
            return favoriteService.GetFavoritesFromAllCollections(collectionService.GetDesignsByCollection);
        return collectionService.GetDesignsByCollection(collectionId);
    }

    public void Dispose() { }

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
            var collections = collectionService.GetCollections();

            if (selectedCollectionId == Guid.Empty && collections.Count > 0)
                selectedCollectionId = collections[0].Id;

            if (selectedCollectionId != Guid.Empty && !collections.Any(c => c.Id == selectedCollectionId))
                selectedCollectionId = collections.Count > 0 ? collections[0].Id : Guid.Empty;

            var sortedCollections = collections.OrderBy(c => c.Order).ToList();
            var dl = ImGui.GetWindowDrawList();

            ImGui.Spacing();
            var (maxTabH, tabBarStart) = DrawTabBar(sortedCollections, dl);
            ImGui.NewLine();

            DrawHeaderRow(dl, maxTabH, tabBarStart);
            ImGui.Dummy(new Vector2(0, 8f));

            DrawGalleryContent();
        }
        catch (Exception)
        {
            ImGui.TextColored(ThemeManager.Current.TextError, Strings.GlamourerNotFound);
        }
    }
}
