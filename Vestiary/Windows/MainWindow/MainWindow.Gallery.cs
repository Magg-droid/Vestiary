using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Vestiary.Windows;

public partial class MainWindow
{
    private void DrawGalleryContent()
    {
        if (!IsGlobalSearchActive && selectedCollectionId == Guid.Empty)
        {
            DrawEmptyCollectionsState();
            return;
        }

        var allDesigns = IsGlobalSearchActive
            ? GetDesignsAcrossCollections(collectionService.GetCollections())
            : GetDesignsForCollection(selectedCollectionId);
        var visibleDesigns = hiddenDesignService.GetVisibleDesigns(allDesigns);
        var hiddenDesigns = hiddenDesignService.GetHiddenDesigns(allDesigns);
        var visibleFiltered = FilterBySearch(visibleDesigns);
        var hiddenFiltered = FilterBySearch(hiddenDesigns);
        var designsToShow = hiddenDesignService.ShowHidden ? hiddenFiltered : visibleFiltered;
        var sortedDesigns = SortDesignsForDisplay(designsToShow);

        ImGui.Spacing();

        if (sortedDesigns.Count > 0)
        {
            if (plugin.Configuration.IsMinimized)
            {
                // No inner child — let window itself scroll, avoids scrollbar gap
                DrawDesignGallery(sortedDesigns, hiddenDesignService.ShowHidden);
            }
            else
            {
                ImGui.BeginChild("##DesignGalleryScroll", new Vector2(-1, -1), false, ImGuiWindowFlags.None);
                DrawDesignGallery(sortedDesigns, hiddenDesignService.ShowHidden);
                ImGui.EndChild();
            }
        }
        else
        {
            ImGui.Spacing();
            ImGui.Spacing();
            float availW = ImGui.GetContentRegionAvail().X;
            string msg = IsGlobalSearchActive
                ? Strings.NoSearchResults
                : hiddenDesignService.ShowHidden
                    ? "No hidden designs"
                    : Strings.NoDesigns;
            ImGui.SetWindowFontScale(1.5f);
            ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextHeading);
            var size = ImGui.CalcTextSize(msg);
            ImGui.SetCursorPosX(Math.Max(0, (availW - size.X) / 2f));
            ImGui.Text(msg);
            ImGui.PopStyleColor();
            ImGui.SetWindowFontScale(1f);
        }
    }

    private List<KeyValuePair<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>> SortDesignsForDisplay(
        Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> designs)
    {
        var mode = plugin.Configuration.DesignSortMode;

        IEnumerable<KeyValuePair<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>> sorted = mode switch
        {
            DesignSortMode.OldestFirst =>
                designs.OrderBy(d => plugin.GlamourerService.GetDesignLastEdit(d.Key)),
            DesignSortMode.NewestFirst =>
                designs.OrderByDescending(d => plugin.GlamourerService.GetDesignLastEdit(d.Key)),
            DesignSortMode.Recent =>
                designs.OrderByDescending(d => GetLastAppliedAt(d.Key)),
            _ => designs,
        };

        return sorted.ToList();
    }

    private DateTime GetLastAppliedAt(Guid designId) =>
        plugin.Configuration.LastAppliedAt.TryGetValue(designId, out var appliedAt)
            ? appliedAt
            : DateTime.MinValue;

    private void DrawEmptyCollectionsState()
    {
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        float availW = ImGui.GetContentRegionAvail().X;

        const float iconSize = 48f;
        var iconTex = plugin.TextureCache.GetOrLoadTexture(uploadIconPath)?.GetWrapOrDefault();
        ImGui.SetCursorPosX(Math.Max(0, (availW - iconSize) / 2f));
        if (iconTex != null)
            ImGui.Image(iconTex.Handle, new Vector2(iconSize, iconSize));
        else
            ImGui.Dummy(new Vector2(iconSize, iconSize));

        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextHeading);
        var headingSize = ImGui.CalcTextSize(Strings.EmptyHeading);
        ImGui.SetCursorPosX(Math.Max(0, (availW - headingSize.X) / 2f));
        ImGui.Text(Strings.EmptyHeading);
        ImGui.PopStyleColor();

        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextMuted);
        var descSize = ImGui.CalcTextSize(Strings.EmptyDescription);
        ImGui.SetCursorPosX(Math.Max(0, (availW - descSize.X) / 2f));
        ImGui.Text(Strings.EmptyDescription);
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.Spacing();

        float btnWidth = 325f;
        ImGui.SetCursorPosX(Math.Max(0, (availW - btnWidth) / 2f));
        ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.CtaBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.CtaBtnHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.CtaBtnActive);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(28f, 10f));
        if (ImGui.Button(Strings.EmptyCtaButton, new Vector2(btnWidth, 0)))
            collectionEditorWindow?.OpenCreate();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);

        ImGui.Spacing();

        ImGui.SetCursorPosX(Math.Max(0, (availW - btnWidth) / 2f));
        ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.TextSubtle);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.TextMuted);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.TextMuted);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(28f, 10f));
        if (ImGui.Button(Strings.EmptyGuideButton, new Vector2(btnWidth, 0)))
            plugin.GuideWin.Toggle();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);

        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextSubtle);
        var hintSize = ImGui.CalcTextSize(Strings.EmptyHint);
        ImGui.SetCursorPosX(Math.Max(0, (availW - hintSize.X) / 2f));
        ImGui.Text(Strings.EmptyHint);
        ImGui.PopStyleColor();
    }
}
