using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Wardrobe.Windows;

public partial class MainWindow
{
    /// <summary>
    /// Draws the design count, eye toggle, and settings button at the top right.
    /// </summary>
    private void DrawHeaderRow(ImDrawListPtr dl, float maxTabH, Vector2 tabBarStart)
    {
        if (selectedCollectionId == Guid.Empty)
            return;

        var allDesigns = collectionService.GetDesignsByCollection(selectedCollectionId);
        var visibleDesigns = hiddenDesignService.GetVisibleDesigns(allDesigns);
        var hiddenDesigns = hiddenDesignService.GetHiddenDesigns(allDesigns);

        string countText;
        if (hiddenDesignService.ShowHidden)
            countText = $"Hidden designs ({hiddenDesigns.Count})";
        else if (hiddenDesigns.Count > 0)
            countText = $"{visibleDesigns.Count} designs ({hiddenDesigns.Count} hidden)";
        else
            countText = $"{visibleDesigns.Count} designs";

        var countSize = ImGui.CalcTextSize(countText);
        float eyeS = 18f;
        float btnW = 90f;
        float btnH = 26f;
        float gap = 8f;
        float sepW = ImGui.CalcTextSize("|").X;
        float rightMargin = 16f;
        float totalW = countSize.X + gap + sepW + gap + eyeS + gap + sepW + gap + btnW + rightMargin;
        float countX = tabBarStart.X + ImGui.GetWindowWidth() - totalW;
        float rowY = tabBarStart.Y + (maxTabH + 3f) / 2f;
        float btnY = tabBarStart.Y + (maxTabH + 3f - btnH) / 2f;
        var sepColor = new Vector4(0.35f, 0.35f, 0.35f, 0.5f);

        // Count text
        dl.AddText(new Vector2(countX, rowY - countSize.Y / 2f),
            ImGui.GetColorU32(RoseGoldTheme.CountText), countText);
        float curX = countX + countSize.X + gap;

        float sepTextH = ImGui.CalcTextSize("|").Y;

        // Separator
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
                ImGui.GetColorU32(RoseGoldTheme.IconDefault));
            ImGui.SetCursorScreenPos(new Vector2(curX, rowY - eyeS / 2f));
            if (ImGui.InvisibleButton("##eyeToggle", new Vector2(eyeS, eyeS)))
                hiddenDesignService.ShowHidden = !hiddenDesignService.ShowHidden;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(hiddenDesignService.ShowHidden
                    ? "Show visible designs" : "Show hidden designs");
        }
        curX += eyeS + gap;

        // Separator
        dl.AddText(new Vector2(curX, rowY - sepTextH / 2f), ImGui.GetColorU32(sepColor), "|");
        curX += sepW + gap;

        // Settings button
        ImGui.SetCursorScreenPos(new Vector2(curX, btnY));
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
}
