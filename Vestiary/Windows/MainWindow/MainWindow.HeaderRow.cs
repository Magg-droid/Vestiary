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

        // "Browse" title
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
        ImGui.SetCursorPosX(12f);
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextHeading);
        ImGui.SetWindowFontScale(1.25f);
        ImGui.Text(Strings.BrowseHeading);
        ImGui.SetWindowFontScale(1f);
        ImGui.PopStyleColor();

        // Search input — right aligned with icon overlay
        const float searchInputW = 180f;
        const float searchInputH = 28f;
        const float searchIconS = 16f;

        float searchX = start.X + availW - searchInputW - 12f;
        float searchY = start.Y + 4f;

        ImGui.SetCursorScreenPos(new Vector2(searchX, searchY));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(searchIconS + 8f, 4f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ThemeManager.Current.SearchBg);
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextNormal);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, ThemeManager.Current.TextSubtle);
        ImGui.SetNextItemWidth(searchInputW);
        ImGui.InputTextWithHint("##searchTop", Strings.SearchHint, ref searchText, 64);
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar(2);

        // Search icon overlay
        var searchTex = plugin.TextureCache.GetOrLoadTexture(searchIconPath)?.GetWrapOrDefault();
        if (searchTex != null)
        {
            float iconPad = 6f;
            float iconY = searchY + (searchInputH - searchIconS) / 2f + 3f;
            dl.AddImage(searchTex.Handle,
                new Vector2(searchX + iconPad, iconY),
                new Vector2(searchX + iconPad + searchIconS, iconY + searchIconS),
                Vector2.Zero, Vector2.One,
                ImGui.GetColorU32(ThemeManager.Current.IconDefault));
        }
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

        string countText;
        if (hiddenDesignService.ShowHidden)
            countText = Strings.DesignCount(hiddenFiltered.Count);
        else if (hiddenDesigns.Count > 0)
            countText = Strings.DesignCountWithHidden(visibleFiltered.Count, hiddenDesigns.Count);
        else
            countText = Strings.DesignCount(visibleFiltered.Count);

        var hiddenLabelSize = ImGui.CalcTextSize(Strings.ShowHiddenLabel);
        var countSize = ImGui.CalcTextSize(countText);
        var sepSize = ImGui.CalcTextSize("|");
        float checkboxW = 18f;
        float gap = 10f;
        float rightEdge = start.X + availW - 12f;

        // Count
        dl.AddText(new Vector2(rightEdge - countSize.X, start.Y + 4f),
            ImGui.GetColorU32(ThemeManager.Current.CountText), countText);

        // Separator
        float sepX = rightEdge - countSize.X - gap - sepSize.X;
        dl.AddText(new Vector2(sepX, start.Y + 4f),
            ImGui.GetColorU32(ThemeManager.Current.SeparatorColor), "|");

        // Checkbox
        float checkboxX = sepX - gap * 2 - checkboxW - 4f - hiddenLabelSize.X;
        ImGui.SetCursorScreenPos(new Vector2(checkboxX, start.Y + 3f));
        bool showHidden = hiddenDesignService.ShowHidden;
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ThemeManager.Current.CardBg);
        ImGui.PushStyleColor(ImGuiCol.CheckMark, ThemeManager.Current.TextNormal);
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextSubtle);
        if (ImGui.Checkbox(Strings.ShowHiddenLabel, ref showHidden))
            hiddenDesignService.ShowHidden = showHidden;
        var checkboxMin = ImGui.GetItemRectMin();
        var checkboxMax = ImGui.GetItemRectMax();
        ImGui.PopStyleColor(3);

        float randomX = checkboxX;
        if (!IsGlobalSearchActive)
        {
            bool canRandom = selectedCollectionId != Guid.Empty && visibleDesigns.Count > 0;
            float randomW = Math.Max(102f, ImGui.CalcTextSize(Strings.RandomButton).X + 24f);
            const float randomH = 30f;
            randomX = checkboxX - 12f - randomW;

            float rowCenterY = (checkboxMin.Y + checkboxMax.Y) * 0.5f;
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
            const float searchChipPadY = 5f;
            const float searchChipRounding = 16f;

            var textSize = ImGui.CalcTextSize(Strings.SearchResultsChip);
            float chipW = textSize.X + searchChipPadX * 2;
            float chipH = textSize.Y + searchChipPadY * 2;
            var chipMin = new Vector2(start.X, start.Y);
            var chipMax = new Vector2(start.X + chipW, start.Y + chipH);

            dl.AddRectFilled(chipMin, chipMax, ImGui.GetColorU32(ThemeManager.Current.ChipBgActive), searchChipRounding);
            dl.AddRect(chipMin, chipMax, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), searchChipRounding, 0, 1f);
            dl.AddText(new Vector2(chipMin.X + searchChipPadX, chipMin.Y + searchChipPadY),
                ImGui.GetColorU32(ThemeManager.Current.ChipTextActive), Strings.SearchResultsChip);

            ImGui.SetCursorScreenPos(chipMin);
            ImGui.InvisibleButton("##search_results_chip", new Vector2(chipW, chipH));

            ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + chipH));
            ImGui.Dummy(new Vector2(availW, 0));
            return;
        }

        // ── Left side: collection chips ──
        const float chipPadX = 14f;
        const float chipPadY = 5f;
        const float chipRounding = 16f;
        const float chipSpacing = 6f;
        const float plusChipW = 36f;

        float cursorX = start.X;
        float maxH = ImGui.CalcTextSize("+").Y + chipPadY * 2;
        float chipRightLimit = randomX - 12f; // don't overlap random button or status
        int renderedCount = 0;
        bool overflowed = false;

        for (int i = 0; i < sortedCollections.Count; i++)
        {
            var collection = sortedCollections[i];
            bool isSelected = selectedCollectionId == collection.Id;

            var textSize = ImGui.CalcTextSize(collection.Name);
            float chipW = textSize.X + chipPadX * 2;
            float chipH = textSize.Y + chipPadY * 2;

            // Reserve space for "+N" overflow chip (if not last) and "+" button
            float reserveW = plusChipW + chipSpacing; // "+" button
            if (i < sortedCollections.Count - 1)
                reserveW += plusChipW + chipSpacing; // overflow chip

            if (cursorX + chipW + reserveW > chipRightLimit)
            {
                overflowed = true;
                break;
            }

            if (chipH > maxH) maxH = chipH;
            renderedCount++;

            var chipMin = new Vector2(cursorX, start.Y);
            var chipMax = new Vector2(cursorX + chipW, start.Y + chipH);
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
            dl.AddText(new Vector2(cursorX + chipPadX, start.Y + chipPadY), textCol, collection.Name);

            ImGui.SetCursorScreenPos(chipMin);
            ImGui.InvisibleButton($"##chiprow_{collection.Id}", new Vector2(chipW, chipH));

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
            float overH = overSize.Y + chipPadY * 2;
            if (overH > maxH) maxH = overH;

            var overMin = new Vector2(cursorX, start.Y);
            var overMax = new Vector2(cursorX + overW, start.Y + overH);
            bool overHover = ImGui.IsMouseHoveringRect(overMin, overMax);
            uint overBg = ImGui.GetColorU32(overHover ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg);
            dl.AddRectFilled(overMin, overMax, overBg, chipRounding);
            dl.AddRect(overMin, overMax, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), chipRounding, 0, 1f);
            dl.AddText(new Vector2(cursorX + chipPadX, start.Y + chipPadY),
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
        var plusMax = new Vector2(plusMin.X + plusChipW, start.Y + maxH);
        bool plusHover = ImGui.IsMouseHoveringRect(plusMin, plusMax);
        uint plusBg = ImGui.GetColorU32(plusHover ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg);
        dl.AddRectFilled(plusMin, plusMax, plusBg, chipRounding);
        dl.AddRect(plusMin, plusMax, ImGui.GetColorU32(ThemeManager.Current.ChipBorder), chipRounding, 0, 1f);
        var plusTextSize = ImGui.CalcTextSize("+");
        dl.AddText(new Vector2(plusMin.X + (plusChipW - plusTextSize.X) / 2f, plusMin.Y + chipPadY),
            ImGui.GetColorU32(ThemeManager.Current.ChipText), "+");
        ImGui.SetCursorScreenPos(plusMin);
        ImGui.InvisibleButton("##new_collection_row", new Vector2(plusChipW, maxH));
        if (ImGui.IsItemClicked())
            collectionEditorWindow?.OpenCreate();
        if (plusHover)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // Advance cursor past this row
        ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + maxH));
        ImGui.Dummy(new Vector2(availW, 0));
    }
}
