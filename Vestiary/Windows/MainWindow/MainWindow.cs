using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Vestiary.Services;

namespace Vestiary.Windows;

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
    private readonly string sortIconPath;
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
    private Guid _selectedEmoteCollectionId = Guid.Empty;
    private Guid _editingEmoteCollectionId = Guid.Empty;
    private string _editingEmoteCollectionName = string.Empty;
    private string _newEmoteCollectionName = string.Empty;
    private Guid _lastRandomButtonDesignId = Guid.Empty;
    internal string _pendingEmoteCommand = string.Empty; // which card is in edit mode // which card's mod dropdown is open
    private Vector2 _lastWindowSize;
    private int _stableFrames;
    private bool _minimizedMenuOpen;
    private bool _popupInteractionBlocked;

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
        string sortIconPath,
        string saveModsIconPath
    )
        : base("Vestiary##With a hidden ID", ImGuiWindowFlags.None)
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
        this.sortIconPath = sortIconPath;
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
    /// True when an in-window popup or menu is open and should block card interactions.
    /// Separate windows (config, guide) are intentionally not included — they are not
    /// modal, and ImGui's window hover checks already prevent clicks through them.
    /// </summary>
    private bool IsInteractionBlocked =>
        _minimizedMenuOpen || _popupInteractionBlocked;

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

    private bool IsGlobalSearchActive => !string.IsNullOrWhiteSpace(searchText);

    private Dictionary<Guid, (string, string, uint, bool)> GetDesignsAcrossCollections(
        IEnumerable<Vestiary.Models.Collection> collections)
    {
        var merged = new Dictionary<Guid, (string, string, uint, bool)>();
        foreach (var collection in collections)
        {
            foreach (var entry in GetDesignsForCollection(collection.Id))
            {
                // De-duplicate designs that appear in more than one collection.
                if (!merged.ContainsKey(entry.Key))
                    merged.Add(entry.Key, entry.Value);
            }
        }

        return merged;
    }

    private void ApplyRandomVisibleDesignFromSelectedCollection()
    {
        if (selectedCollectionId == Guid.Empty)
            return;

        var allDesigns = GetDesignsForCollection(selectedCollectionId);
        var visibleDesigns = hiddenDesignService.GetVisibleDesigns(allDesigns);
        if (visibleDesigns.Count == 0)
            return;

        if (!RandomSelectionHelper.TryPickDesign(visibleDesigns, ref _lastRandomButtonDesignId, out var designId))
            return;

        plugin.CloseSubWindows();
        plugin.GlamourerService.ApplyDesign(designId, plugin.Configuration.ApplyEquipmentOnly);
        plugin.ModStateService.RestoreState(designId);
    }

    public override void Draw()
    {
        if (plugin.IsCameraActive)
            return;

        // Dynamic size constraints for minimized mode
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = plugin.Configuration.IsMinimized
                ? new Vector2(180, 150)
                : new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        // Hide title bar in minimized mode (takes effect next frame)
        if (plugin.Configuration.IsMinimized)
            Flags |= ImGuiWindowFlags.NoTitleBar;
        else
            Flags &= ~ImGuiWindowFlags.NoTitleBar;

        try
        {
            _popupInteractionBlocked = false;

            if (!plugin.Configuration.EnableEmotes) _currentView = 0;

            var collections = collectionService.GetCollections();

            if (selectedCollectionId == Guid.Empty && collections.Count > 0)
                selectedCollectionId = collections[0].Id;

            if (selectedCollectionId != Guid.Empty && !collections.Any(c => c.Id == selectedCollectionId))
                selectedCollectionId = collections.Count > 0 ? collections[0].Id : Guid.Empty;

            var sortedCollections = collections.OrderBy(c => c.Order).ToList();
            var emoteCollections = plugin.EmoteService.GetCollections();
            if (_selectedEmoteCollectionId == Guid.Empty && emoteCollections.Count > 0)
                _selectedEmoteCollectionId = emoteCollections[0].Id;
            if (_selectedEmoteCollectionId != Guid.Empty && !emoteCollections.Any(c => c.Id == _selectedEmoteCollectionId))
                _selectedEmoteCollectionId = emoteCollections.Count > 0 ? emoteCollections[0].Id : Guid.Empty;
            var dl = ImGui.GetWindowDrawList();

            // ── Minimized mode: skip all chrome, just show gallery ──
            if (plugin.Configuration.IsMinimized)
            {
                // Track resize state for snap-to-grid
                const float cardW = 88f;
                const float gap = 6f;
                const float margin = 6f;
                var currentSize = ImGui.GetWindowSize();
                bool resizing = Math.Abs(currentSize.X - _lastWindowSize.X) > 1f;
                _lastWindowSize = currentSize;
                _stableFrames = resizing ? 0 : _stableFrames + 1;

                // Snap window width to nearest perfect grid
                if (!resizing && _stableFrames >= 2)
                {
                    float contentW = ImGui.GetContentRegionAvail().X;
                    int n = Math.Max(2, (int)Math.Round((contentW - margin * 2 + gap) / (cardW + gap)));
                    int totalCards = _currentView == 1
                        ? GetFilteredEmoteCards().Count
                        : GetDesignsForCollection(selectedCollectionId).Count;
                    if (totalCards > 0) n = Math.Min(n, totalCards);
                    float neededW = margin * 2 + n * cardW + (n - 1) * gap;
                    float excess = contentW - neededW;
                    if (Math.Abs(excess) > 2f)
                    {
                        ImGui.SetWindowSize(new Vector2(currentSize.X - excess, currentSize.Y));
                        _stableFrames = -10;
                    }
                }

                // ── Fixed top toolbar (doesn't scroll) ──
                const float barH = 28f;
                var barMin = ImGui.GetWindowPos();
                var barMax = barMin + new Vector2(ImGui.GetWindowSize().X, barH);

                dl.AddRectFilled(barMin, barMax,
                    ImGui.GetColorU32(ThemeManager.Current.RailBg), 0f);

                // ── Hamburger menu (left) ──
                const float btnSize = 22f;
                float btnY = barMin.Y + 3f;

                var hamPos = new Vector2(barMin.X + 4f, btnY);
                var hamEnd = hamPos + new Vector2(btnSize, btnSize);
                bool hamHovered = ImGui.IsMouseHoveringRect(hamPos, hamEnd, false);

                uint btnBg = ImGui.GetColorU32(hamHovered ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg);
                dl.AddRectFilled(hamPos, hamEnd, btnBg, 4f);
                dl.AddRect(hamPos, hamEnd, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), 4f, 0, 1f);

                // Hamburger icon: three centered horizontal lines
                float hcx = hamPos.X + btnSize / 2f;
                float hcy = hamPos.Y + btnSize / 2f;
                uint hamCol = ImGui.GetColorU32(hamHovered ? ThemeManager.Current.RailTextActive : ThemeManager.Current.RailTextIdle);
                for (int li = 0; li < 3; li++)
                    dl.AddLine(new Vector2(hcx - 5f, hcy - 5f + li * 5f), new Vector2(hcx + 5f, hcy - 5f + li * 5f), hamCol, 1.5f);

                ImGui.SetCursorScreenPos(hamPos);
                ImGui.InvisibleButton("##hamburger", new Vector2(btnSize, btnSize));
                if (ImGui.IsItemClicked())
                    ImGui.OpenPopup("##minimized_menu");
                if (hamHovered)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                // Hamburger menu popup
                _minimizedMenuOpen = ImGui.BeginPopup("##minimized_menu");
                if (_minimizedMenuOpen)
                {
                    if (ImGui.BeginMenu(Strings.RailGlamour))
                    {
                        foreach (var col in sortedCollections)
                        {
                            if (ImGui.MenuItem(col.Name, "", selectedCollectionId == col.Id))
                            {
                                _currentView = 0;
                                selectedCollectionId = col.Id;
                                ImGui.CloseCurrentPopup();
                            }
                        }
                        ImGui.EndMenu();
                    }

                    if (plugin.Configuration.EnableEmotes && ImGui.BeginMenu(Strings.RailEmotes))
                    {
                        if (emoteCollections.Count == 0)
                        {
                            if (ImGui.MenuItem(Strings.RailEmotes))
                            {
                                _currentView = 1;
                                ImGui.CloseCurrentPopup();
                            }
                        }
                        else
                        {
                            foreach (var emoteCollection in emoteCollections)
                            {
                                if (ImGui.MenuItem(emoteCollection.Name, string.Empty,
                                        _selectedEmoteCollectionId == emoteCollection.Id))
                                {
                                    _currentView = 1;
                                    _selectedEmoteCollectionId = emoteCollection.Id;
                                    ImGui.CloseCurrentPopup();
                                }
                            }
                        }

                        ImGui.EndMenu();
                    }
                    ImGui.EndPopup();
                }

                // ── Expand button (left, beside hamburger) ──
                var expPos = new Vector2(barMin.X + btnSize + 8f, btnY);
                var expEnd = expPos + new Vector2(btnSize, btnSize);
                bool expHovered = ImGui.IsMouseHoveringRect(expPos, expEnd, false);

                dl.AddRectFilled(expPos, expEnd,
                    ImGui.GetColorU32(expHovered ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg), 4f);
                dl.AddRect(expPos, expEnd,
                    ImGui.GetColorU32(ThemeManager.Current.ChipBorder), 4f, 0, 1f);

                // Expand icon: two outward arrows ← → centered
                float ecx = expPos.X + btnSize / 2f;
                float ecy = expPos.Y + btnSize / 2f;
                uint expCol = ImGui.GetColorU32(expHovered ? ThemeManager.Current.RailTextActive : ThemeManager.Current.RailTextIdle);
                // Left arrow
                dl.AddTriangleFilled(new Vector2(ecx - 6f, ecy), new Vector2(ecx - 2f, ecy - 4f), new Vector2(ecx - 2f, ecy + 4f), expCol);
                // Right arrow
                dl.AddTriangleFilled(new Vector2(ecx + 6f, ecy), new Vector2(ecx + 2f, ecy - 4f), new Vector2(ecx + 2f, ecy + 4f), expCol);

                ImGui.SetCursorScreenPos(expPos);
                ImGui.InvisibleButton("##expandBtn", new Vector2(btnSize, btnSize));
                if (ImGui.IsItemClicked())
                {
                    plugin.Configuration.IsMinimized = false;
                    plugin.Configuration.Save();
                }
                if (expHovered)
                {
                    ImGui.SetTooltip(Strings.RailExpand);
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                // ── Random Pick button (left, beside expand) ──
                bool isEmotes = _currentView == 1;
                bool canRandom = false;
                if (!isEmotes && selectedCollectionId != Guid.Empty)
                {
                    var allDesigns = GetDesignsForCollection(selectedCollectionId);
                    var visibleDesigns = hiddenDesignService.GetVisibleDesigns(allDesigns);
                    canRandom = visibleDesigns.Count > 0;
                }

                var rndPos = new Vector2(barMin.X + (btnSize * 2f) + 12f, btnY);
                var rndEnd = rndPos + new Vector2(btnSize, btnSize);
                bool rndHovered = ImGui.IsMouseHoveringRect(rndPos, rndEnd, false);
                bool rndEnabled = !isEmotes && canRandom;

                dl.AddRectFilled(rndPos, rndEnd,
                    ImGui.GetColorU32(rndHovered && rndEnabled ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg), 4f);
                dl.AddRect(rndPos, rndEnd,
                    ImGui.GetColorU32(ThemeManager.Current.ChipBorder), 4f, 0, 1f);

                // Dice icon: centered 11x11 square with 3 diagonal dots
                float rcx = rndPos.X + btnSize / 2f;
                float rcy = rndPos.Y + btnSize / 2f;
                uint rndCol = ImGui.GetColorU32(!rndEnabled
                    ? ThemeManager.Current.TextSubtle
                    : (rndHovered ? ThemeManager.Current.RailTextActive : ThemeManager.Current.RailTextIdle));

                dl.AddRect(new Vector2(rcx - 5.5f, rcy - 5.5f), new Vector2(rcx + 5.5f, rcy + 5.5f), rndCol, 2f, 0, 1.2f);
                dl.AddCircleFilled(new Vector2(rcx - 2.5f, rcy - 2.5f), 1.1f, rndCol);
                dl.AddCircleFilled(new Vector2(rcx, rcy), 1.1f, rndCol);
                dl.AddCircleFilled(new Vector2(rcx + 2.5f, rcy + 2.5f), 1.1f, rndCol);

                ImGui.SetCursorScreenPos(rndPos);
                ImGui.BeginDisabled(!rndEnabled);
                ImGui.InvisibleButton("##randomBtnMinimized", new Vector2(btnSize, btnSize));
                if (ImGui.IsItemClicked() && rndEnabled)
                {
                    ApplyRandomVisibleDesignFromSelectedCollection();
                }
                ImGui.EndDisabled();

                if (rndHovered)
                {
                    if (isEmotes)
                        ImGui.SetTooltip(Strings.TooltipRandomButtonGlamourOnly);
                    else if (canRandom)
                        ImGui.SetTooltip(Strings.TooltipRandomButton);
                    else
                        ImGui.SetTooltip(Strings.TooltipRandomButtonDisabled);

                    if (rndEnabled)
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                // ── Roulette button (left, beside random pick) ──
                if (plugin.Configuration.EnableGlamourRoulette)
                {
                    var rltPos = new Vector2(barMin.X + (btnSize * 3f) + 16f, btnY);
                    var rltEnd = rltPos + new Vector2(btnSize, btnSize);
                    bool rltHovered = ImGui.IsMouseHoveringRect(rltPos, rltEnd, false);
                    bool rltEnabled = !isEmotes;
                    bool rltActive = plugin.RouletteService.IsActive;

                    uint rltBg = rltActive
                        ? ImGui.GetColorU32(ThemeManager.Current.ApplyBtn)
                        : ImGui.GetColorU32(rltHovered && rltEnabled ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg);
                    dl.AddRectFilled(rltPos, rltEnd, rltBg, 4f);
                    dl.AddRect(rltPos, rltEnd, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), 4f, 0, 1f);

                    // Slot machine icon: 3 vertical reel lines inside a box
                    float slx = rltPos.X + btnSize / 2f;
                    float sly = rltPos.Y + btnSize / 2f;
                    uint slCol = ImGui.GetColorU32(!rltEnabled
                        ? ThemeManager.Current.TextSubtle
                        : (rltActive ? ThemeManager.Current.TabTextActive : (rltHovered ? ThemeManager.Current.RailTextActive : ThemeManager.Current.RailTextIdle)));

                    dl.AddRect(new Vector2(slx - 6f, sly - 5f), new Vector2(slx + 6f, sly + 5f), slCol, 2f, 0, 1.2f);
                    dl.AddLine(new Vector2(slx - 2f, sly - 5f), new Vector2(slx - 2f, sly + 5f), slCol, 1f);
                    dl.AddLine(new Vector2(slx + 2f, sly - 5f), new Vector2(slx + 2f, sly + 5f), slCol, 1f);

                    ImGui.SetCursorScreenPos(rltPos);
                    ImGui.BeginDisabled(!rltEnabled);
                    ImGui.InvisibleButton("##rouletteBtnMinimized", new Vector2(btnSize, btnSize));
                    if (ImGui.IsItemClicked() && rltEnabled)
                    {
                        plugin.RouletteService.ToggleRoulette();
                    }
                    ImGui.EndDisabled();

                    if (rltHovered)
                    {
                        if (isEmotes)
                        {
                            ImGui.SetTooltip(Strings.TooltipRandomButtonGlamourOnly);
                        }
                        else if (rltActive)
                        {
                            var rem = plugin.RouletteService.RemainingTime;
                            string remStr = $"{rem.Minutes:D2}m {rem.Seconds:D2}s";
                            ImGui.SetTooltip(Strings.TooltipRouletteMinimizedActive(remStr));
                        }
                        else
                        {
                            ImGui.SetTooltip(Strings.TooltipRouletteMinimizedInactive);
                        }

                        if (rltEnabled)
                            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    }
                }

                // Push content below the fixed toolbar — auto-height child clips content
                ImGui.SetCursorPos(new Vector2(0, barH + 2f));
                ImGui.BeginChild("##MinimizedContent", new Vector2(-1, 0), false,
                    ImGuiWindowFlags.NoScrollbar);

                if (_currentView == 1)
                    DrawEmoteGallery();
                else
                    DrawGalleryContent();

                ImGui.EndChild();
                return;
            }

            // ── Split: left rail + right content ──
            DrawRail();

            ImGui.SameLine();

            ImGui.BeginChild("##MainContent", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar);

            // Top bar: Browse + search (content area only)
            DrawTopBar();

            // Separator line (content area)
            var sepPos = ImGui.GetCursorScreenPos();
            dl.AddLine(
                new Vector2(sepPos.X, sepPos.Y),
                new Vector2(sepPos.X + ImGui.GetContentRegionAvail().X, sepPos.Y),
                ImGui.GetColorU32(ThemeManager.Current.RailDivider), 1f);
            ImGui.Spacing();

            // Emote view: skip chips + status, go straight to gallery
            if (_currentView == 1)
            {
                if (IsGlobalSearchActive)
                {
                    ImGui.Dummy(new Vector2(0, 5f));
                    DrawEmoteSearchStatusRow(GetFilteredEmoteCards().Count);
                    ImGui.Dummy(new Vector2(0, 12f));
                }
                else
                {
                    ImGui.Dummy(new Vector2(0, 5f));
                    DrawEmoteCollectionRow(emoteCollections);
                    ImGui.Dummy(new Vector2(0, 12f));
                }

                DrawEmoteGallery();
                ImGui.EndChild();
                return;
            }

            // Roulette view: render roulette dashboard
            if (_currentView == 2)
            {
                DrawRouletteView();
                ImGui.EndChild();
                return;
            }

            // Chip row + status row on one line: chips left, hidden+count right
            ImGui.Dummy(new Vector2(0, 5f));
            DrawChipAndStatusRow(sortedCollections);
            ImGui.Dummy(new Vector2(0, 1f));

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
