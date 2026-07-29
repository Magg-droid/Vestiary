using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly string noPreviewImagePath;
    private readonly string cameraIconPath;
    private readonly string uploadIconPath;
    private readonly string clipboardIconPath;
    private readonly Plugin plugin;
    private readonly CollectionService collectionService;
    private readonly DesignMetadataService designMetadataService;
    private CollectionEditorWindow? collectionEditorWindow;
    private DesignEditorWindow? designEditorWindow;
    private Guid selectedCollectionId = Guid.Empty;
    private int dragTabIndex = -1;

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(
        Plugin plugin,
        string goatImagePath,
        CollectionService collectionService,
        DesignMetadataService designMetadataService,
        string noPreviewImagePath,
        string cameraIconPath,
        string uploadIconPath,
        string clipboardIconPath
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
        this.plugin = plugin;
        this.collectionService = collectionService;
        this.designMetadataService = designMetadataService;
        this.collectionEditorWindow = null!; // Will be set after construction
        this.designEditorWindow = null!; // Will be set after construction
    }

    public void SetCollectionEditorWindow(CollectionEditorWindow editor)
    {
        collectionEditorWindow = editor;
    }

    public void SetDesignEditorWindow(DesignEditorWindow editor)
    {
        designEditorWindow = editor;
    }

    /// <summary>
    /// Check if a design has a custom image.
    /// </summary>
    private bool HasCustomImage(Guid designId)
    {
        try
        {
            var metadata = designMetadataService.GetMetadata(designId);
            return !string.IsNullOrEmpty(metadata?.CustomImagePath) && File.Exists(metadata.CustomImagePath);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() { }

    public override void Draw()
    {
        // Don't draw anything if camera overlay is active
        if (plugin.IsCameraActive)
            return;

        try
        {
            var collections = collectionService.GetCollections();

            // Initialize selectedCollectionId on first draw
            if (selectedCollectionId == Guid.Empty && collections.Count > 0)
                selectedCollectionId = collections[0].Id;

            // Verify selected collection still exists; fallback if deleted
            if (
                selectedCollectionId != Guid.Empty
                && !collections.Any(c => c.Id == selectedCollectionId)
            )
            {
                selectedCollectionId = collections.Count > 0 ? collections[0].Id : Guid.Empty;
            }

            // Draw custom tab bar with drag-to-reorder
            ImGui.Spacing();
            var sortedCollections = collections.OrderBy(c => c.Order).ToList();
            var dl = ImGui.GetWindowDrawList();

            const float tabPadX = 14f;
            const float tabPadY = 6f;
            const float tabRounding = 6f;
            const float tabSpacing = 3f;
            var tabBarStart = ImGui.GetCursorScreenPos();
            float cursorX = tabBarStart.X;
            // Fallback height so "+" button and line render correctly even with zero tabs
            float maxTabH = ImGui.CalcTextSize("+").Y + tabPadY * 2;

            for (int i = 0; i < sortedCollections.Count; i++)
            {
                var collection = sortedCollections[i];
                bool isSelected = selectedCollectionId == collection.Id;

                var textSize = ImGui.CalcTextSize(collection.Name);
                float tabW = textSize.X + tabPadX * 2;
                float tabH = textSize.Y + tabPadY * 2;
                if (tabH > maxTabH) maxTabH = tabH;

                var tabMin = new Vector2(cursorX, tabBarStart.Y);
                var tabMax = new Vector2(cursorX + tabW, tabBarStart.Y + tabH + (isSelected ? 2f : 0f));

                // Tab background with only top corners rounded, flat bottom
                uint tabBg;
                if (isSelected)
                    tabBg = ImGui.GetColorU32(RoseGoldTheme.TabSelected);
                else if (ImGui.IsMouseHoveringRect(tabMin, tabMax))
                    tabBg = ImGui.GetColorU32(RoseGoldTheme.TabHovered);
                else
                    tabBg = ImGui.GetColorU32(RoseGoldTheme.TabDefault);

                dl.AddRectFilled(tabMin, tabMax, tabBg, tabRounding, ImDrawFlags.RoundCornersTop);

                // Tab text
                uint textCol = ImGui.GetColorU32(isSelected
                    ? RoseGoldTheme.TabTextActive
                    : RoseGoldTheme.TabTextIdle);
                var textPos = new Vector2(cursorX + tabPadX, tabBarStart.Y + tabPadY);
                dl.AddText(textPos, textCol, collection.Name);

                // Invisible button for interaction
                ImGui.SetCursorScreenPos(tabMin);
                ImGui.InvisibleButton($"##tab_{collection.Id}", new Vector2(tabW, tabH));

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    selectedCollectionId = collection.Id;

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Strings.TabRightClickTooltip);

                // Right-click context menu
                if (ImGui.BeginPopupContextItem($"##tabctx_{collection.Id}"))
                {
                    if (ImGui.MenuItem(Strings.Edit))
                    {
                        collectionEditorWindow?.OpenEdit(collection);
                        ImGui.CloseCurrentPopup();
                    }
                    if (ImGui.MenuItem(Strings.Delete))
                    {
                        collectionService.DeleteCollection(collection.Id);
                        if (selectedCollectionId == collection.Id)
                            selectedCollectionId = Guid.Empty;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }

                // Drag source
                if (ImGui.BeginDragDropSource())
                {
                    dragTabIndex = i;
                    ImGui.SetDragDropPayload("COLLECTION_TAB", ReadOnlySpan<byte>.Empty);
                    ImGui.Text(collection.Name);
                    ImGui.EndDragDropSource();
                }

                // Drop target
                if (ImGui.BeginDragDropTarget())
                {
                    ImGui.AcceptDragDropPayload("COLLECTION_TAB");
                    if (dragTabIndex >= 0 && dragTabIndex != i)
                    {
                        collectionService.SwapOrder(dragTabIndex, i);
                        dragTabIndex = -1;
                    }
                    ImGui.EndDragDropTarget();
                }

                cursorX += tabW + tabSpacing;
            }

            // Bottom line right below the tallest tab
            float lineY = tabBarStart.Y + maxTabH + 3f;
            var lineEnd = new Vector2(tabBarStart.X + ImGui.GetContentRegionAvail().X, lineY);
            dl.AddLine(new Vector2(tabBarStart.X, lineY), lineEnd,
                ImGui.GetColorU32(RoseGoldTheme.TabBorderLine), 1.5f);

            // "+" button at end of tab bar
            float plusW = 28f;
            var plusMin = new Vector2(cursorX + 4f, tabBarStart.Y + 2f);
            var plusMax = new Vector2(plusMin.X + plusW, tabBarStart.Y + maxTabH);
            bool plusHover = ImGui.IsMouseHoveringRect(plusMin, plusMax);
            uint plusBg = ImGui.GetColorU32(plusHover
                ? RoseGoldTheme.PlusBtn
                : RoseGoldTheme.PlusBtnInactive);
            dl.AddRectFilled(plusMin, plusMax, plusBg, tabRounding, ImDrawFlags.RoundCornersTop);
            var plusTextSize = ImGui.CalcTextSize("+");
            dl.AddText(new Vector2(plusMin.X + (plusW - plusTextSize.X) / 2f, plusMin.Y + 4f),
                ImGui.GetColorU32(RoseGoldTheme.TabPlusIcon), "+");
            ImGui.SetCursorScreenPos(plusMin);
            ImGui.InvisibleButton("##new_collection", new Vector2(plusW, plusMax.Y - plusMin.Y));
            if (ImGui.IsItemClicked())
                collectionEditorWindow?.OpenCreate();

            ImGui.NewLine();

            // Display design count + settings button at top right
            if (selectedCollectionId != Guid.Empty)
            {
                var designs = collectionService.GetDesignsByCollection(selectedCollectionId);
                string countText = $"{designs.Count} designs";
                Vector2 countSize = ImGui.CalcTextSize(countText);
                float btnW = 90f;
                float btnH = 26f;
                float rightMargin = 28f;
                float totalW = countSize.X + 12f + btnW + rightMargin;
                float countX = tabBarStart.X + ImGui.GetWindowWidth() - totalW;
                float countY = tabBarStart.Y + (maxTabH + 3f - countSize.Y) / 2f;
                float btnY = tabBarStart.Y + (maxTabH + 3f - btnH) / 2f;
                float btnX = countX + countSize.X + 12f;

                // Count text
                dl.AddText(new Vector2(countX, countY),
                    ImGui.GetColorU32(RoseGoldTheme.CountText), countText);

                // Settings button (save/restore cursor so layout isn't affected)
                var savedCursor = ImGui.GetCursorScreenPos();
                ImGui.SetCursorScreenPos(new Vector2(btnX, btnY));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 1f));
                ImGui.PushStyleColor(ImGuiCol.Button, RoseGoldTheme.EditBtn);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RoseGoldTheme.EditBtnHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, RoseGoldTheme.EditBtnActive);
                if (ImGui.Button("Settings", new Vector2(btnW, btnH)))
                    plugin.ToggleConfigUi();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open Settings");
                ImGui.PopStyleColor(3);
                ImGui.PopStyleVar(2);
                ImGui.SetCursorScreenPos(savedCursor);
            }

            ImGui.Dummy(new Vector2(0, 8f));

            // Display designs from selected collection
            if (selectedCollectionId != Guid.Empty)
            {
                var designs = collectionService.GetDesignsByCollection(selectedCollectionId);

                ImGui.Spacing();

                if (designs.Count > 0)
                {
                    // Scrollable gallery area
                    ImGui.BeginChild(
                        "##DesignGalleryScroll",
                        new Vector2(-1, -1),
                        false,
                        ImGuiWindowFlags.None
                    );
                    DrawDesignGallery(designs);
                    ImGui.EndChild();
                }
                else
                {
                    ImGui.TextColored(
                        RoseGoldTheme.TextMuted,
                        Strings.NoDesigns
                    );
                }
            }
            else if (collections.Count == 0)
            {
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();

                float availW = ImGui.GetContentRegionAvail().X;

                // Centered icon (upload icon looks like a folder)
                const float iconSize = 48f;
                var iconTex = plugin.TextureCache.GetOrLoadTexture(uploadIconPath)?.GetWrapOrDefault();
                ImGui.SetCursorPosX(Math.Max(0, (availW - iconSize) / 2f));
                if (iconTex != null)
                    ImGui.Image(iconTex.Handle, new Vector2(iconSize, iconSize));
                else
                    ImGui.Dummy(new Vector2(iconSize, iconSize));

                ImGui.Spacing();

                // Centered heading
                ImGui.PushStyleColor(ImGuiCol.Text, RoseGoldTheme.TextHeading);
                var headingSize = ImGui.CalcTextSize(Strings.EmptyHeading);
                ImGui.SetCursorPosX(Math.Max(0, (availW - headingSize.X) / 2f));
                ImGui.Text(Strings.EmptyHeading);
                ImGui.PopStyleColor();

                ImGui.Spacing();

                // Centered description
                ImGui.PushStyleColor(ImGuiCol.Text, RoseGoldTheme.TextMuted);
                var descText = Strings.EmptyDescription;
                var descSize = ImGui.CalcTextSize(descText);
                ImGui.SetCursorPosX(Math.Max(0, (availW - descSize.X) / 2f));
                ImGui.Text(descText);
                ImGui.PopStyleColor();

                ImGui.Spacing();
                ImGui.Spacing();

                // CTA button — wide with generous internal padding
                float btnWidth = 325f;
                ImGui.SetCursorPosX(Math.Max(0, (availW - btnWidth) / 2f));

                ImGui.PushStyleColor(ImGuiCol.Button, RoseGoldTheme.CtaBtn);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RoseGoldTheme.CtaBtnHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, RoseGoldTheme.CtaBtnActive);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(28f, 10f));

                if (ImGui.Button(Strings.EmptyCtaButton, new Vector2(btnWidth, 0)))
                    collectionEditorWindow?.OpenCreate();

                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor(3);

                ImGui.Spacing();

                // Subtle hint
                ImGui.PushStyleColor(ImGuiCol.Text, RoseGoldTheme.TextSubtle);
                var hintSize = ImGui.CalcTextSize(Strings.EmptyHint);
                ImGui.SetCursorPosX(Math.Max(0, (availW - hintSize.X) / 2f));
                ImGui.Text(Strings.EmptyHint);
                ImGui.PopStyleColor();
            }
        }
        catch (Exception)
        {
            ImGui.TextColored(RoseGoldTheme.TextError, Strings.GlamourerNotFound);
        }
    }

    private void DrawDesignGallery(
        Dictionary<
            Guid,
            (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)
        > designs
    )
    {
        const float cardWidth = 260f;
        const float cardHeight = 400f;
        const float cardSpacing = 25f;
        const float verticalGap = 25f;

        float availableWidth = ImGui.GetContentRegionAvail().X;
        int columnsPerRow = Math.Max(
            1,
            (int)((availableWidth - cardSpacing) / (cardWidth + cardSpacing))
        );

        // Calculate total width needed for one row of cards
        float totalRowWidth = (cardWidth * columnsPerRow) + (cardSpacing * (columnsPerRow - 1));
        float centerOffset = Math.Max(0, (availableWidth - totalRowWidth) / 2);

        int designIndex = 0;
        foreach (var designEntry in designs)
        {
            int columnIndex = designIndex % columnsPerRow;
            Guid designId = designEntry.Key;
            var design = designEntry.Value;

            // Center align the first column of each row
            if (columnIndex == 0)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + centerOffset);
            }

            DrawDesignCard(designId, design.DisplayName, cardWidth, cardHeight);

            if (columnIndex < columnsPerRow - 1 && designIndex < designs.Count - 1)
            {
                ImGui.SameLine(0, cardSpacing);
            }
            else if (columnIndex == columnsPerRow - 1 || designIndex == designs.Count - 1)
            {
                ImGui.Dummy(new Vector2(0, verticalGap));
            }

            designIndex++;
        }
    }

    private void DrawDesignCard(Guid designId, string glamourerName, float width, float height)
    {
        const float cornerRounding = 12f;
        const float borderThickness = 1.5f;

        // Draw card background and border with rounded corners
        Vector2 cardStartPos = ImGui.GetCursorScreenPos();
        Vector2 cardEndPos = cardStartPos + new Vector2(width, height);
        bool isCardHovered = ImGui.IsMouseHoveringRect(cardStartPos, cardEndPos);

        ImGui
            .GetWindowDrawList()
            .AddRectFilled(
                cardStartPos,
                cardEndPos,
                ImGui.GetColorU32(isCardHovered
                    ? RoseGoldTheme.CardBgHovered
                    : RoseGoldTheme.CardBg),
                cornerRounding
            );
        ImGui
            .GetWindowDrawList()
            .AddRect(
                cardStartPos,
                cardEndPos,
                ImGui.GetColorU32(isCardHovered
                    ? RoseGoldTheme.CardBorder
                    : RoseGoldTheme.CardBorderIdle),
                cornerRounding,
                0,
                borderThickness
            );

        // Use a child region for layout
        ImGui.BeginChild(
            $"##DesignCard_{designId}",
            new Vector2(width, height),
            false,
            ImGuiWindowFlags.None
        );

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f);

        // Large thumbnail area (220x300px) - takes most of card space
        const float thumbWidth = 240f;
        const float thumbHeight = 300f;
        float thumbPadX = 10f;

        ImGui.SetCursorPosX(thumbPadX);
        Vector2 thumbStartPos = ImGui.GetCursorScreenPos();
        Vector2 thumbEndPos = thumbStartPos + new Vector2(thumbWidth, thumbHeight);

        // Draw thumbnail with nice styling
        ImGui
            .GetWindowDrawList()
            .AddRectFilled(
                thumbStartPos,
                thumbEndPos,
                ImGui.GetColorU32(RoseGoldTheme.ThumbBg),
                4f
            );
        ImGui
            .GetWindowDrawList()
            .AddRect(
                thumbStartPos,
                thumbEndPos,
                ImGui.GetColorU32(RoseGoldTheme.ThumbBorder),
                4f,
                0,
                1f
            );

        // Try to load and display custom image or fallback to placeholder
        var metadata = designMetadataService.GetMetadata(designId);
        string customImagePath = metadata?.CustomImagePath ?? "";
        bool hasCustomImage = !string.IsNullOrEmpty(customImagePath) && File.Exists(customImagePath);
        
        if (hasCustomImage)
        {
            // Try to load the texture
            var wrap = plugin.TextureCache.GetOrLoadTexture(customImagePath)?.GetWrapOrDefault();
            
            if (wrap != null)
            {
                // Display the actual image texture
                ImGui.GetWindowDrawList().AddImage(
                    wrap.Handle,
                    thumbStartPos,
                    thumbEndPos,
                    Vector2.Zero,
                    Vector2.One,
                    ImGui.GetColorU32(Vector4.One)
                );
            }
            else
            {
                // Fallback: show placeholder with custom image indicator
                string thumbText = "✓ Custom Image";
                Vector4 thumbTextColor = RoseGoldTheme.ThumbCustomImg;
                Vector2 textSize = ImGui.CalcTextSize(thumbText);
                Vector2 textPos = thumbStartPos + new Vector2((thumbWidth - textSize.X) / 2, (thumbHeight - textSize.Y) / 2);
                ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(thumbTextColor), thumbText);
            }
        }
        else
        {
            // No custom image - show placeholder
            string thumbText = "No Preview";
            Vector4 thumbTextColor = RoseGoldTheme.ThumbNoPreview;
            Vector2 textSize = ImGui.CalcTextSize(thumbText);
            Vector2 textPos = thumbStartPos + new Vector2((thumbWidth - textSize.X) / 2, (thumbHeight - textSize.Y) / 2);
            ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(thumbTextColor), thumbText);
        }

        // ── Camera icon in top-right corner of thumbnail ──
        const float iconSize = 28f;
        const float iconPadX = 8f;
        const float iconPadY = -3f;
        var iconMin = new Vector2(thumbEndPos.X - iconSize - iconPadX, thumbStartPos.Y + iconPadY);
        var iconMax = new Vector2(thumbEndPos.X - iconPadX, thumbStartPos.Y + iconPadY + iconSize);
        bool isIconHovered = ImGui.IsMouseHoveringRect(iconMin, iconMax);

        var cdl = ImGui.GetWindowDrawList();
        uint iconTint = ImGui.GetColorU32(isIconHovered
            ? RoseGoldTheme.IconHovered
            : RoseGoldTheme.IconDefault);

        // Load and draw camera icon texture
        var camTex = plugin.TextureCache.GetOrLoadTexture(cameraIconPath)?.GetWrapOrDefault();
        if (camTex != null)
            cdl.AddImage(camTex.Handle, iconMin, iconMax, Vector2.Zero, Vector2.One, iconTint);

        // Tooltip on hover
        if (isIconHovered)
            ImGui.SetTooltip(Strings.TooltipCamera);

        // Click detection without touching cursor (avoids layout shift)
        if (isIconHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var capturedDesignId = designId;
            plugin.CloseSubWindows();
            plugin.ShowCameraOverlay(path =>
            {
                designMetadataService.UpsertMetadata(capturedDesignId, customImagePath: path);
                plugin.TextureCache.InvalidateTexture(path);
            });
        }

        // ── Upload icon (file picker) ──
        const float iconGap = 4f;
        var uploadMin = new Vector2(iconMin.X, iconMax.Y + iconGap);
        var uploadMax = new Vector2(iconMax.X, uploadMin.Y + iconSize);
        bool isUploadHovered = ImGui.IsMouseHoveringRect(uploadMin, uploadMax);
        uint uploadTint = ImGui.GetColorU32(isUploadHovered
            ? RoseGoldTheme.IconHovered
            : RoseGoldTheme.IconDefault);
        var uploadTex = plugin.TextureCache.GetOrLoadTexture(uploadIconPath)?.GetWrapOrDefault();
        if (uploadTex != null)
            cdl.AddImage(uploadTex.Handle, uploadMin, uploadMax, Vector2.Zero, Vector2.One, uploadTint);
        if (isUploadHovered)
            ImGui.SetTooltip(Strings.TooltipUpload);
        if (isUploadHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var capturedDesignId = designId;
            plugin.CloseSubWindows();
            plugin.OpenImageFilePicker(path =>
            {
                designMetadataService.UpsertMetadata(capturedDesignId, customImagePath: path);
                plugin.TextureCache.InvalidateTexture(path);
            });
        }

        // ── Clipboard icon ──
        var clipMin = new Vector2(iconMin.X, uploadMax.Y + iconGap);
        var clipMax = new Vector2(iconMax.X, clipMin.Y + iconSize);
        bool isClipHovered = ImGui.IsMouseHoveringRect(clipMin, clipMax);
        uint clipTint = ImGui.GetColorU32(isClipHovered
            ? RoseGoldTheme.IconHovered
            : RoseGoldTheme.IconDefault);
        var clipTex = plugin.TextureCache.GetOrLoadTexture(clipboardIconPath)?.GetWrapOrDefault();
        if (clipTex != null)
            cdl.AddImage(clipTex.Handle, clipMin, clipMax, Vector2.Zero, Vector2.One, clipTint);
        if (isClipHovered)
            ImGui.SetTooltip(Strings.TooltipClipboard);
        if (isClipHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var capturedDesignId = designId;
            plugin.CloseSubWindows();
            plugin.CopyImageFromClipboard(path =>
            {
                designMetadataService.UpsertMetadata(capturedDesignId, customImagePath: path);
                plugin.TextureCache.InvalidateTexture(path);
            });
        }

        // ── Double-click thumbnail to apply (only if no action icon is hovered) ──
        bool anyIconHovered = isIconHovered || isUploadHovered || isClipHovered;
        bool thumbHovered = ImGui.IsMouseHoveringRect(thumbStartPos, thumbEndPos);
        if (thumbHovered && !anyIconHovered)
        {
            ImGui.SetTooltip(Strings.TooltipThumbnail);
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                plugin.CloseSubWindows();
                plugin.GlamourerService.ApplyDesign(designId, plugin.Configuration.ApplyEquipmentOnly || ImGui.GetIO().KeyCtrl);
            }
        }

        ImGui.Dummy(new Vector2(thumbWidth, thumbHeight));

        // Draw border line below image
        ImGui
            .GetWindowDrawList()
            .AddLine(
                new Vector2(cardStartPos.X, thumbEndPos.Y + 8f),
                new Vector2(cardEndPos.X, thumbEndPos.Y + 8f),
                ImGui.GetColorU32(RoseGoldTheme.CardLine),
                1.5f
            );

        // Minimal spacing
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 12f);

        // Design name - centered in a box-like display
        string displayedName = designMetadataService.GetDisplayName(designId);
        string originalName = displayedName;

        const int maxNameChars = 24;

        // Truncate if too long
        if (displayedName.Length > maxNameChars)
        {
            displayedName = displayedName.Substring(0, maxNameChars - 3) + "...";
        }

        // Center align the name
        Vector2 nameSize = ImGui.CalcTextSize(displayedName);
        float nameX = Math.Max(8f, (width - nameSize.X) / 2);
        ImGui.SetCursorPosX(nameX);

        // Fancy rose-gold color for name
        ImGui.PushStyleColor(ImGuiCol.Text, RoseGoldTheme.TextHeading);
        ImGui.Text(displayedName);
        ImGui.PopStyleColor();

        // Tooltip with full name on hover if truncated
        if (displayedName.EndsWith("...") && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(originalName);
            ImGui.EndTooltip();
        }

        // Spacing before buttons
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);

        // Three-button layout: Apply | Edit | Delete
        float btnWidth = 62f;
        float deleteBtnWidth = 70f; // Slightly wider for "Delete"
        float btnHeight = 28f;
        float btnSpacing = 12f;
        float totalBtnWidth = (btnWidth * 2) + deleteBtnWidth + (btnSpacing * 2);
        float btnStartX = (width - totalBtnWidth) / 2;

        ImGui.SetCursorPosX(btnStartX);

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);

        // Apply button - Muted Steel Blue
        ImGui.PushStyleColor(ImGuiCol.Button, RoseGoldTheme.ApplyBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RoseGoldTheme.ApplyBtnHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, RoseGoldTheme.ApplyBtnActive);

        if (ImGui.Button(Strings.CardApply + $"##btn_apply_{designId}", new Vector2(btnWidth, btnHeight)))
        {
            plugin.CloseSubWindows();
            bool equipmentOnly = plugin.Configuration.ApplyEquipmentOnly || ImGui.GetIO().KeyCtrl;
            plugin.GlamourerService.ApplyDesign(designId, equipmentOnly);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(Strings.TooltipApply);
            ImGui.TextDisabled(Strings.TooltipApplyCtrl);
            ImGui.EndTooltip();
        }

        ImGui.PopStyleColor(3);

        // Edit button - Muted Warm Grey
        ImGui.SameLine(btnStartX + btnWidth + btnSpacing);
        ImGui.PushStyleColor(ImGuiCol.Button, RoseGoldTheme.EditBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RoseGoldTheme.EditBtnHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, RoseGoldTheme.EditBtnActive);

        if (ImGui.Button(Strings.CardEdit + $"##btn_edit_{designId}", new Vector2(btnWidth, btnHeight)))
        {
            plugin.CloseSubWindows();
            designEditorWindow?.OpenEdit(designId);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(Strings.TooltipEdit);
            ImGui.EndTooltip();
        }

        ImGui.PopStyleColor(3);

        // Delete button - Muted Red-Grey
        ImGui.SameLine(btnStartX + (btnWidth * 2) + (btnSpacing * 2));
        ImGui.PushStyleColor(ImGuiCol.Button, RoseGoldTheme.DeleteBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RoseGoldTheme.DeleteBtnHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, RoseGoldTheme.DeleteBtnActive);

        if (ImGui.Button(Strings.CardDelete + $"##btn_delete_{designId}", new Vector2(deleteBtnWidth, btnHeight)))
        {
            if (ImGui.GetIO().KeyCtrl)
            {
                plugin.CloseSubWindows();
                plugin.GlamourerService.DeleteDesign(designId);
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(Strings.TooltipDelete);
            ImGui.TextDisabled(Strings.TooltipDeleteCtrl);
            ImGui.EndTooltip();
        }

        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar();

        ImGui.EndChild();
    }
}
