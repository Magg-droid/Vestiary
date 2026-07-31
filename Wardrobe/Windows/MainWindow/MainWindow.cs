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

            // Segmented pill control (only if emotes enabled)
            if (plugin.Configuration.EnableEmotes)
            {
                ImGui.Spacing();
                float pillW = 180f; float pillH = 32f; float inset = 2f;
            var pillStart = ImGui.GetCursorScreenPos();
            var pillEnd = pillStart + new Vector2(pillW, pillH);

            var pillDl = ImGui.GetWindowDrawList();
            pillDl.AddRectFilled(pillStart, pillEnd, ImGui.GetColorU32(ThemeManager.Current.CardBg), pillH / 2);
            pillDl.AddRect(pillStart, pillEnd, ImGui.GetColorU32(ThemeManager.Current.CardBorder), pillH / 2, 0, 1f);

            ImGui.SetCursorScreenPos(pillStart + new Vector2(inset, inset));
            float innerW = (pillW - inset * 2) / 2;
            float innerH = pillH - inset * 2;

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, innerH / 2);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 2f));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            if (_currentView == 0) { ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.TabSelected); ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TabTextActive); }
            else { ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero with { W = 0 }); ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextSubtle); }
            if (ImGui.Button("Glamour", new Vector2(innerW, innerH))) _currentView = 0;
            ImGui.PopStyleColor(2);

            ImGui.SameLine(0, 0);

            if (_currentView == 1) { ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.TabSelected); ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TabTextActive); }
            else { ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero with { W = 0 }); ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextSubtle); }
            if (ImGui.Button("Emotes", new Vector2(innerW, innerH))) _currentView = 1;
            ImGui.PopStyleColor(2);

            ImGui.PopStyleVar(3);
            ImGui.Spacing();
            ImGui.Dummy(new Vector2(0, 2f));

            // Separator below pill
            var sepY = ImGui.GetCursorScreenPos().Y;
            pillDl.AddLine(new Vector2(pillStart.X, sepY),
                new Vector2(pillStart.X + ImGui.GetContentRegionAvail().X, sepY),
                ImGui.GetColorU32(ThemeManager.Current.TabBorderLine), 1.5f);
            ImGui.Spacing();

            if (_currentView == 1) { DrawEmoteGallery(); return; }
        }

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
