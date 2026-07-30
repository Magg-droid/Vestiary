using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Wardrobe.Windows;

public partial class MainWindow
{
    private void DrawGalleryContent()
    {
        if (selectedCollectionId == Guid.Empty)
        {
            DrawEmptyCollectionsState();
            return;
        }

        var allDesigns = GetDesignsForCollection(selectedCollectionId);
        var visibleDesigns = hiddenDesignService.GetVisibleDesigns(allDesigns);
        var hiddenDesigns = hiddenDesignService.GetHiddenDesigns(allDesigns);
        var visibleFiltered = FilterBySearch(visibleDesigns);
        var hiddenFiltered = FilterBySearch(hiddenDesigns);
        var designsToShow = hiddenDesignService.ShowHidden ? hiddenFiltered : visibleFiltered;

        ImGui.Spacing();

        if (designsToShow.Count > 0)
        {
            ImGui.BeginChild("##DesignGalleryScroll", new Vector2(-1, -1), false, ImGuiWindowFlags.None);
            DrawDesignGallery(designsToShow, hiddenDesignService.ShowHidden);
            ImGui.EndChild();
        }
        else
        {
            ImGui.Spacing();
            ImGui.Spacing();
            float availW = ImGui.GetContentRegionAvail().X;
            string msg = hiddenDesignService.ShowHidden
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

        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextSubtle);
        var hintSize = ImGui.CalcTextSize(Strings.EmptyHint);
        ImGui.SetCursorPosX(Math.Max(0, (availW - hintSize.X) / 2f));
        ImGui.Text(Strings.EmptyHint);
        ImGui.PopStyleColor();
    }
}
