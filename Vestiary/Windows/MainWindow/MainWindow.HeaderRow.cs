using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Vestiary.Models;

namespace Vestiary.Windows;

public partial class MainWindow
{
    /// <summary>
    /// Top bar: "Browse" title on the left, search input on the right.
    /// </summary>
    private void DrawTopBar()
    {
        var dl = ImGui.GetWindowDrawList();
        var start = ImGui.GetCursorScreenPos();
        float availW = ImGui.GetContentRegionAvail().X;

        // Search input — right aligned with icon overlay
        const float searchInputW = 180f;
        const float searchInputH = 35f;
        const float searchIconS = 16f;
        const float topPad = 4f;

        float searchX = start.X + availW - searchInputW - 12f;
        float searchY = start.Y + topPad;
        float topBarH = topPad + searchInputH;

        // "Browse" title — same size as rail menu text, centered on the search box.
        var browseSize = ImGui.CalcTextSize(Strings.BrowseHeading);
        ImGui.SetCursorScreenPos(new Vector2(start.X + 8f, start.Y + topPad + searchInputH / 2f - browseSize.Y / 2f));
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextHeading);
        ImGui.Text(Strings.BrowseHeading);
        ImGui.PopStyleColor();

        ImGui.SetCursorScreenPos(new Vector2(searchX, searchY));
        float searchVPad = (searchInputH - ImGui.GetTextLineHeight()) / 2f;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(searchIconS + 8f, searchVPad));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ThemeManager.Current.SearchBg);
        ImGui.PushStyleColor(ImGuiCol.Border, ThemeManager.Current.ChipBorder);
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextNormal);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, ThemeManager.Current.TextSubtle);
        ImGui.SetNextItemWidth(searchInputW);
        ImGui.InputTextWithHint("##searchTop", Strings.SearchHint, ref searchText, 64);
        ImGui.PopStyleColor(4);
        ImGui.PopStyleVar(3);

        // Search icon overlay
        var searchTex = plugin.TextureCache.GetOrLoadTexture(searchIconPath)?.GetWrapOrDefault();
        if (searchTex != null)
        {
            float iconPad = 6f;
            float iconY = searchY + (searchInputH - searchIconS) / 2f;
            dl.AddImage(searchTex.Handle,
                new Vector2(searchX + iconPad, iconY),
                new Vector2(searchX + iconPad + searchIconS, iconY + searchIconS),
                Vector2.Zero, Vector2.One,
                ImGui.GetColorU32(ThemeManager.Current.IconDefault));
        }

        // Keep cursor at the bottom of the top bar so the divider sits below the search box.
        ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + topBarH));
        ImGui.Dummy(new Vector2(availW, 0));
    }


    /// <summary>
    /// Single row: collection chips on the left, hidden checkbox + count on the right.
    /// </summary>
    private void DrawChipAndStatusRow(System.Collections.Generic.List<Vestiary.Models.Collection> sortedCollections)
    {
        if (!IsGlobalSearchActive && selectedCollectionId == Guid.Empty)
            return;

        var dl = ImGui.GetWindowDrawList();
        var start = ImGui.GetCursorScreenPos();
        float availW = ImGui.GetContentRegionAvail().X;

        // ── Right side: hidden checkbox | count ──
        var allDesigns = IsGlobalSearchActive
            ? GetDesignsAcrossCollections(sortedCollections)
            : GetDesignsForCollection(selectedCollectionId);
        var visibleDesigns = hiddenDesignService.GetVisibleDesigns(allDesigns);
        var hiddenDesigns = hiddenDesignService.GetHiddenDesigns(allDesigns);
        var visibleFiltered = FilterBySearch(visibleDesigns);
        var hiddenFiltered = FilterBySearch(hiddenDesigns);

        int designsCount = hiddenDesignService.ShowHidden ? hiddenFiltered.Count : visibleFiltered.Count;
        int favoritesCount = favoriteService.GetFavoritesFromAllCollections(collectionService.GetDesignsByCollection).Count;

        var hiddenLabelSize = ImGui.CalcTextSize(Strings.ShowHiddenLabel);
        float checkboxW = 18f;
        float rightEdge = start.X + availW - 12f;

        // Checkbox — right aligned, vertically centered on the chips row
        float rowCenterY = start.Y + 17.5f;
        float checkboxX = rightEdge - checkboxW - 6f - hiddenLabelSize.X;
        ImGui.SetCursorScreenPos(new Vector2(checkboxX, rowCenterY - ImGui.GetFrameHeight() * 0.5f));
        bool showHidden = hiddenDesignService.ShowHidden;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ThemeManager.Current.CardBg);
        ImGui.PushStyleColor(ImGuiCol.Border, ThemeManager.Current.ChipBorder);
        ImGui.PushStyleColor(ImGuiCol.CheckMark, ThemeManager.Current.TextNormal);
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextSubtle);
        if (ImGui.Checkbox(Strings.ShowHiddenLabel, ref showHidden))
            hiddenDesignService.ShowHidden = showHidden;
        ImGui.PopStyleColor(4);
        ImGui.PopStyleVar();

        float randomX = checkboxX;
        if (!IsGlobalSearchActive)
        {
            bool canRandom = selectedCollectionId != Guid.Empty && visibleDesigns.Count > 0;
            float randomW = Math.Max(102f, ImGui.CalcTextSize(Strings.RandomButton).X + 24f);
            const float randomH = 35f;
            randomX = checkboxX - 12f - randomW;

            ImGui.SetCursorScreenPos(new Vector2(randomX, rowCenterY - randomH / 2f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
            ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.ChipBgActive);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.ApplyBtnHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.ApplyBtnActive);

            ImGui.BeginDisabled(!canRandom);
            if (ImGui.Button(Strings.RandomButton + "##random_glamour", new Vector2(randomW, randomH)))
                ApplyRandomVisibleDesignFromSelectedCollection();
            ImGui.EndDisabled();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(canRandom ? Strings.TooltipRandomButton : Strings.TooltipRandomButtonDisabled);

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();
        }

        if (IsGlobalSearchActive)
        {
            const float searchChipPadX = 14f;
            const float searchChipH = 35f;
            const float searchChipRounding = 4f;

            var textSize = ImGui.CalcTextSize(Strings.SearchResultsChip);
            float chipW = textSize.X + searchChipPadX * 2;
            var chipMin = new Vector2(start.X, start.Y);
            var chipMax = new Vector2(start.X + chipW, start.Y + searchChipH);

            dl.AddRectFilled(chipMin, chipMax, ImGui.GetColorU32(ThemeManager.Current.ChipBgActive), searchChipRounding);
            dl.AddRect(chipMin, chipMax, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), searchChipRounding, 0, 1f);
            dl.AddText(new Vector2(chipMin.X + searchChipPadX, chipMin.Y + (searchChipH - textSize.Y) / 2f),
                ImGui.GetColorU32(ThemeManager.Current.ChipTextActive), Strings.SearchResultsChip);

            ImGui.SetCursorScreenPos(chipMin);
            ImGui.InvisibleButton("##search_results_chip", new Vector2(chipW, searchChipH));

            ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + searchChipH));
            ImGui.Dummy(new Vector2(availW, 0));
            DrawCountLine(designsCount, favoritesCount);
            return;
        }

        // ── Left side: collection chips ──
        const float chipPadX = 14f;
        const float chipHeight = 35f;
        const float chipRounding = 4f;
        const float chipSpacing = 6f;
        const float plusChipW = 36f;

        float cursorX = start.X + 8f;
        float chipRightLimit = randomX - 12f; // don't overlap random button or status
        int renderedCount = 0;
        bool overflowed = false;

        for (int i = 0; i < sortedCollections.Count; i++)
        {
            var collection = sortedCollections[i];
            bool isSelected = selectedCollectionId == collection.Id;

            var textSize = ImGui.CalcTextSize(collection.Name);
            float chipW = textSize.X + chipPadX * 2;

            // Reserve space for "+N" overflow chip (if not last) and "+" button
            float reserveW = plusChipW + chipSpacing; // "+" button
            if (i < sortedCollections.Count - 1)
                reserveW += plusChipW + chipSpacing; // overflow chip

            if (cursorX + chipW + reserveW > chipRightLimit)
            {
                overflowed = true;
                break;
            }

            renderedCount++;

            var chipMin = new Vector2(cursorX, start.Y);
            var chipMax = new Vector2(cursorX + chipW, start.Y + chipHeight);
            bool hovered = ImGui.IsMouseHoveringRect(chipMin, chipMax);

            uint bg = isSelected
                ? ImGui.GetColorU32(ThemeManager.Current.ChipBgActive)
                : hovered
                    ? ImGui.GetColorU32(ThemeManager.Current.ChipBgHovered)
                    : ImGui.GetColorU32(ThemeManager.Current.ChipBg);

            dl.AddRectFilled(chipMin, chipMax, bg, chipRounding);
            dl.AddRect(chipMin, chipMax, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), chipRounding, 0, 1f);

            uint textCol = ImGui.GetColorU32(isSelected
                ? ThemeManager.Current.ChipTextActive
                : ThemeManager.Current.ChipText);
            dl.AddText(new Vector2(cursorX + chipPadX, start.Y + (chipHeight - textSize.Y) / 2f), textCol, collection.Name);

            ImGui.SetCursorScreenPos(chipMin);
            ImGui.InvisibleButton($"##chiprow_{collection.Id}", new Vector2(chipW, chipHeight));

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                selectedCollectionId = collection.Id;

            bool isFavorites = collection.Name == "Favorites";
            if (!isFavorites && hovered)
                ImGui.SetTooltip(Strings.TabRightClickTooltip);

            if (!isFavorites && ImGui.BeginPopupContextItem($"##chiprowctx_{collection.Id}"))
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

            if (ImGui.BeginDragDropSource())
            {
                dragTabIndex = i;
                ImGui.SetDragDropPayload("COLLECTION_CHIP", System.ReadOnlySpan<byte>.Empty);
                ImGui.Text(collection.Name);
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                ImGui.AcceptDragDropPayload("COLLECTION_CHIP");
                if (dragTabIndex >= 0 && dragTabIndex != i)
                {
                    collectionService.SwapOrder(dragTabIndex, i);
                    dragTabIndex = -1;
                }
                ImGui.EndDragDropTarget();
            }

            if (hovered)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            cursorX += chipW + chipSpacing;
        }

        // Overflow chip: "+N" when not all collections fit — click to see hidden list
        if (overflowed)
        {
            int remaining = sortedCollections.Count - renderedCount;
            string overflowLabel = $"+{remaining}";
            var overSize = ImGui.CalcTextSize(overflowLabel);
            float overW = overSize.X + chipPadX * 2;
            float overH = chipHeight;

            var overMin = new Vector2(cursorX, start.Y);
            var overMax = new Vector2(cursorX + overW, start.Y + overH);
            bool overHover = ImGui.IsMouseHoveringRect(overMin, overMax);
            uint overBg = ImGui.GetColorU32(overHover ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg);
            dl.AddRectFilled(overMin, overMax, overBg, chipRounding);
            dl.AddRect(overMin, overMax, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), chipRounding, 0, 1f);
            dl.AddText(new Vector2(cursorX + chipPadX, start.Y + (chipHeight - overSize.Y) / 2f),
                ImGui.GetColorU32(ThemeManager.Current.ChipText), overflowLabel);
            ImGui.SetCursorScreenPos(overMin);
            ImGui.InvisibleButton("##overflow_chips", new Vector2(overW, overH));
            if (overHover)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            // Popup with hidden collections (positioned below the chip)
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                ImGui.SetNextWindowPos(new Vector2(overMin.X, overMax.Y + 4f));
                ImGui.OpenPopup("##overflow_popup");
            }

            if (ImGui.BeginPopup("##overflow_popup"))
            {
                for (int j = renderedCount; j < sortedCollections.Count; j++)
                {
                    var col = sortedCollections[j];
                    if (ImGui.Selectable(col.Name, selectedCollectionId == col.Id))
                    {
                        selectedCollectionId = col.Id;
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndPopup();
            }

            cursorX += overW + chipSpacing;
        }

        // "+" chip
        var plusMin = new Vector2(cursorX + 2f, start.Y);
        var plusMax = new Vector2(plusMin.X + plusChipW, start.Y + chipHeight);
        bool plusHover = ImGui.IsMouseHoveringRect(plusMin, plusMax);
        uint plusBg = ImGui.GetColorU32(plusHover ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg);
        dl.AddRectFilled(plusMin, plusMax, plusBg, chipRounding);
        dl.AddRect(plusMin, plusMax, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), chipRounding, 0, 1f);
        var plusTextSize = ImGui.CalcTextSize("+");
        dl.AddText(new Vector2(plusMin.X + (plusChipW - plusTextSize.X) / 2f, plusMin.Y + (chipHeight - plusTextSize.Y) / 2f),
            ImGui.GetColorU32(ThemeManager.Current.ChipText), "+");
        ImGui.SetCursorScreenPos(plusMin);
        ImGui.InvisibleButton("##new_collection_row", new Vector2(plusChipW, chipHeight));
        if (ImGui.IsItemClicked())
            collectionEditorWindow?.OpenCreate();
        if (plusHover)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // Advance cursor past this row
        ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + chipHeight));
        ImGui.Dummy(new Vector2(availW, 0));

        DrawCountLine(designsCount, favoritesCount);
    }

    private void DrawCountLine(int designsCount, int favoritesCount)
    {
        var dl = ImGui.GetWindowDrawList();
        string designsText = Strings.DesignCount(designsCount);
        string favoritesText = Strings.FavoriteCount(favoritesCount);
        const string dot = Strings.StatsSeparator;
        const float statGap = 8f;

        var designsSize = ImGui.CalcTextSize(designsText);
        var dotSize = ImGui.CalcTextSize(dot);

        ImGui.Dummy(new Vector2(0, 16f));
        var pos = ImGui.GetCursorScreenPos();

        // Align with the gallery's left margin.
        float x = pos.X + 8f;
        float y = pos.Y;
        dl.AddText(new Vector2(x, y), ImGui.GetColorU32(ThemeManager.Current.CountText), designsText);
        float dotX = x + designsSize.X + statGap;
        dl.AddText(new Vector2(dotX, y), ImGui.GetColorU32(ThemeManager.Current.SeparatorColor), dot);
        float favX = dotX + dotSize.X + statGap;
        dl.AddText(new Vector2(favX, y), ImGui.GetColorU32(ThemeManager.Current.TextSuccess), favoritesText);

        ImGui.Dummy(new Vector2(0, designsSize.Y));
    }
}
