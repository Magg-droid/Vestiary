using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Wardrobe.Windows;

public partial class MainWindow
{
    private void DrawHeaderRow(ImDrawListPtr dl, float maxTabH, Vector2 tabBarStart)
    {
        if (selectedCollectionId == Guid.Empty)
            return;

        var allDesigns = GetDesignsForCollection(selectedCollectionId);
        var visibleDesigns = hiddenDesignService.GetVisibleDesigns(allDesigns);
        var hiddenDesigns = hiddenDesignService.GetHiddenDesigns(allDesigns);
        var visibleFiltered = FilterBySearch(visibleDesigns);
        var hiddenFiltered = FilterBySearch(hiddenDesigns);

        string countText;
        if (hiddenDesignService.ShowHidden)
            countText = $"{hiddenFiltered.Count} designs";
        else if (hiddenDesigns.Count > 0)
            countText = $"{visibleFiltered.Count} designs ({hiddenDesigns.Count} hidden)";
        else
            countText = $"{visibleFiltered.Count} designs";

        var countSize = ImGui.CalcTextSize(countText);
        float eyeS = 18f;
        float btnW = 90f;
        float btnH = 26f;
        float gap = 8f;
        float sepW = ImGui.CalcTextSize("|").X;
        float rightMargin = 16f;
        float totalRightW = countSize.X + gap + sepW + gap + eyeS + gap + sepW + gap + btnW + rightMargin;
        float countX = tabBarStart.X + ImGui.GetWindowWidth() - totalRightW;

        float rowY = tabBarStart.Y + (maxTabH + 3f) / 2f;
        float btnY = tabBarStart.Y + (maxTabH + 3f - btnH) / 2f;
        float sepTextH = ImGui.CalcTextSize("|").Y;
        var sepColor = ThemeManager.Current.SeparatorColor;

        // ── Search bar (icon inside input) ──
        float searchIconS = 18f;
        float searchInputW = 140f;
        float searchInputH = 22f;
        float searchGap = 8f;
        float searchTotalW = searchInputW + searchGap + sepW + searchGap;
        float searchX = countX - searchTotalW;
        float searchInputY = tabBarStart.Y + (maxTabH + 3f - searchInputH) / 2f - 3f;

        var savedBeforeSearch = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(new Vector2(searchX, searchInputY));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(searchIconS + 6f, 2f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ThemeManager.Current.SearchBg);
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextNormal);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, ThemeManager.Current.TextSubtle);
        ImGui.SetNextItemWidth(searchInputW);
        ImGui.InputTextWithHint("##searchInput", Strings.SearchHint, ref searchText, 64);
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar(2);
        ImGui.SetCursorScreenPos(savedBeforeSearch);

        // Magnifying glass icon overlaid inside
        var searchTex = plugin.TextureCache.GetOrLoadTexture(searchIconPath)?.GetWrapOrDefault();
        if (searchTex != null)
        {
            float iconPad = 4f;
            float searchIconY = searchInputY + (searchInputH - searchIconS) / 2f + 4f;
            dl.AddImage(searchTex.Handle,
                new Vector2(searchX + iconPad, searchIconY),
                new Vector2(searchX + iconPad + searchIconS, searchIconY + searchIconS),
                Vector2.Zero, Vector2.One,
                ImGui.GetColorU32(ThemeManager.Current.IconDefault));
        }

        // Tooltip for overflow text
        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(searchText) && searchText.Length > 12)
            ImGui.SetTooltip(searchText);

        // Separator after search
        dl.AddText(new Vector2(searchX + searchTotalW - searchGap - sepW, rowY - sepTextH / 2f),
            ImGui.GetColorU32(sepColor), "|");

        // ── Right side: count, eye, settings ──
        float curX = countX;

        dl.AddText(new Vector2(curX, rowY - countSize.Y / 2f),
            ImGui.GetColorU32(ThemeManager.Current.CountText), countText);
        curX += countSize.X + gap;

        dl.AddText(new Vector2(curX, rowY - sepTextH / 2f), ImGui.GetColorU32(sepColor), "|");
        curX += sepW + gap;

        // Eye icon toggle
        var eyeTex = plugin.TextureCache.GetOrLoadTexture(
            hiddenDesignService.ShowHidden ? hiddenIconPath : viewIconPath)?.GetWrapOrDefault();
        var savedCursor = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(new Vector2(curX, rowY - eyeS / 2f));
        if (eyeTex != null)
        {
            dl.AddImage(eyeTex.Handle,
                new Vector2(curX, rowY - eyeS / 2f),
                new Vector2(curX + eyeS, rowY + eyeS / 2f),
                Vector2.Zero, Vector2.One,
                ImGui.GetColorU32(ThemeManager.Current.IconDefault));
            ImGui.SetCursorScreenPos(new Vector2(curX, rowY - eyeS / 2f));
            if (ImGui.InvisibleButton("##eyeToggle", new Vector2(eyeS, eyeS)))
                hiddenDesignService.ShowHidden = !hiddenDesignService.ShowHidden;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(hiddenDesignService.ShowHidden
                    ? Strings.TooltipEyeShowVisible : Strings.TooltipEyeShowHidden);
        }
        curX += eyeS + gap;

        dl.AddText(new Vector2(curX, rowY - sepTextH / 2f), ImGui.GetColorU32(sepColor), "|");
        curX += sepW + gap;

        // Settings button
        ImGui.SetCursorScreenPos(new Vector2(curX, btnY));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.EditBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.EditBtnHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.EditBtnActive);
        if (ImGui.Button(Strings.Settings, new Vector2(btnW, btnH)))
            plugin.ToggleConfigUi();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Strings.TooltipSettings);
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar(2);
        ImGui.SetCursorScreenPos(savedCursor);
    }
}
