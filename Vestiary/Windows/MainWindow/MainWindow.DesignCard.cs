using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Vestiary.Windows;

public partial class MainWindow
{
    private void DrawDesignGallery(
        List<KeyValuePair<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)>> designs,
        bool isHidden = false)
    {
        bool minimized = plugin.Configuration.IsMinimized;
        const float cardWidth = 260f;
        const float cardHeight = 400f;
        const float cardSpacing = 25f;
        const float verticalGap = 25f;

        float actualCardW = minimized ? 88f : cardWidth;
        float actualCardH = minimized ? 108f : cardHeight;
        float actualSpacing = minimized ? 6f : cardSpacing;
        float actualVGap = minimized ? 6f : verticalGap;

        float availableWidth = ImGui.GetContentRegionAvail().X;
        int columnsPerRow = Math.Max(1, (int)((availableWidth - actualSpacing) / (actualCardW + actualSpacing)));
        float totalRowWidth = actualCardW * columnsPerRow + actualSpacing * (columnsPerRow - 1);
        float leftMargin = plugin.Configuration.IsMinimized ? 6f : 8f;

        int designIndex = 0;
        foreach (var entry in designs)
        {
            int columnIndex = designIndex % columnsPerRow;
            if (columnIndex == 0)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + leftMargin);

            DrawDesignCard(entry.Key, entry.Value.DisplayName, actualCardW, actualCardH, isHidden);

            if (columnIndex < columnsPerRow - 1 && designIndex < designs.Count - 1)
                ImGui.SameLine(0, actualSpacing);
            else
                ImGui.Dummy(new Vector2(0, actualVGap));

            designIndex++;
        }
    }

    private void DrawDesignCard(Guid designId, string glamourerName, float width, float height, bool isHidden = false)
    {
        if (isHidden)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);

        const float cornerRounding = 12f;
        const float borderThickness = 1.5f;

        var cardStartPos = ImGui.GetCursorScreenPos();
        var cardEndPos = cardStartPos + new Vector2(width, height);
        bool isCardHovered = !IsInteractionBlocked
            && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows)
            && ImGui.IsMouseHoveringRect(cardStartPos, cardEndPos);

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(cardStartPos, cardEndPos,
            ImGui.GetColorU32(isCardHovered ? ThemeManager.Current.CardBgHovered : ThemeManager.Current.CardBg),
            cornerRounding);
        drawList.AddRect(cardStartPos, cardEndPos,
            ImGui.GetColorU32(isCardHovered ? ThemeManager.Current.CardBorder : ThemeManager.Current.CardBorderIdle),
            cornerRounding, 0, borderThickness);

        ImGui.BeginChild($"##DesignCard_{designId}", new Vector2(width, height), false, ImGuiWindowFlags.None);

        bool minimized = plugin.Configuration.IsMinimized;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (minimized ? 4f : 8f));

        // Thumbnail
        float thumbWidth = minimized ? 80f : 240f;
        float thumbHeight = minimized ? 100f : 300f;
        float thumbPadX = minimized ? 4f : 10f;
        ImGui.SetCursorPosX(thumbPadX);
        var thumbStartPos = ImGui.GetCursorScreenPos();
        var thumbEndPos = thumbStartPos + new Vector2(thumbWidth, thumbHeight);

        drawList.AddRectFilled(thumbStartPos, thumbEndPos,
            ImGui.GetColorU32(ThemeManager.Current.ThumbBg), 4f);
        drawList.AddRect(thumbStartPos, thumbEndPos,
            ImGui.GetColorU32(ThemeManager.Current.ThumbBorder), 4f, 0, 1f);

        DrawThumbnailImage(designId, thumbStartPos, thumbEndPos, thumbWidth, thumbHeight);

        if (!plugin.Configuration.IsMinimized)
            DrawThumbnailIcons(designId, thumbStartPos, thumbEndPos);

        DrawThumbnailDoubleClick(designId, thumbStartPos, thumbEndPos);

        ImGui.Dummy(new Vector2(thumbWidth, thumbHeight));

        if (!minimized)
        {
            // Border line
            drawList.AddLine(
                new Vector2(cardStartPos.X, thumbEndPos.Y + 8f),
                new Vector2(cardEndPos.X, thumbEndPos.Y + 8f),
                ImGui.GetColorU32(ThemeManager.Current.CardLine), 1.5f);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 12f);
        }

        if (!minimized)
            DrawDesignName(designId, width);

        if (minimized && ImGui.IsMouseHoveringRect(thumbStartPos, thumbEndPos))
        {
            ImGui.SetTooltip(designMetadataService.GetDisplayName(designId));
        }

        if (!minimized)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);
            DrawDesignButtons(designId, width, isHidden);
        }

        ImGui.EndChild();
    }

    private void DrawThumbnailImage(Guid designId, Vector2 thumbStartPos, Vector2 thumbEndPos,
        float thumbWidth, float thumbHeight)
    {
        var metadata = designMetadataService.GetMetadata(designId);
        string customImagePath = metadata?.CustomImagePath ?? "";
        bool hasCustomImage = !string.IsNullOrEmpty(customImagePath) && File.Exists(customImagePath);

        if (hasCustomImage)
        {
            var wrap = plugin.TextureCache.GetOrLoadTexture(customImagePath)?.GetWrapOrDefault();
            if (wrap != null)
            {
                ImGui.GetWindowDrawList().AddImage(wrap.Handle,
                    thumbStartPos, thumbEndPos, Vector2.Zero, Vector2.One, ImGui.GetColorU32(Vector4.One));
            }
            else
            {
                var textSize = ImGui.CalcTextSize("✓ Custom Image");
                var textPos = thumbStartPos + new Vector2((thumbWidth - textSize.X) / 2, (thumbHeight - textSize.Y) / 2);
                ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ThemeManager.Current.ThumbCustomImg), "✓ Custom Image");
            }
        }
        else
        {
            if (plugin.Configuration.IsMinimized)
            {
                var line1 = "No";
                var line2 = "Preview";
                var line1Size = ImGui.CalcTextSize(line1);
                var line2Size = ImGui.CalcTextSize(line2);
                float gap = 2f;
                float totalH = line1Size.Y + gap + line2Size.Y;
                float startY = thumbStartPos.Y + (thumbHeight - totalH) / 2f;

                var line1Pos = new Vector2(thumbStartPos.X + (thumbWidth - line1Size.X) / 2f, startY);
                var line2Pos = new Vector2(thumbStartPos.X + (thumbWidth - line2Size.X) / 2f, startY + line1Size.Y + gap);
                ImGui.GetWindowDrawList().AddText(line1Pos, ImGui.GetColorU32(ThemeManager.Current.ThumbNoPreview), line1);
                ImGui.GetWindowDrawList().AddText(line2Pos, ImGui.GetColorU32(ThemeManager.Current.ThumbNoPreview), line2);
            }
            else
            {
                var textSize = ImGui.CalcTextSize("No Preview");
                var textPos = thumbStartPos + new Vector2((thumbWidth - textSize.X) / 2, (thumbHeight - textSize.Y) / 2);
                ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ThemeManager.Current.ThumbNoPreview), "No Preview");
            }
        }
    }

    private void DrawThumbnailIcons(Guid designId, Vector2 thumbStartPos, Vector2 thumbEndPos)
    {
        bool windowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);
        const float iconSize = 28f;
        const float iconPadX = 8f;
        const float iconPadY = -3f;
        const float iconGap = 4f;

        var iconMin = new Vector2(thumbEndPos.X - iconSize - iconPadX, thumbStartPos.Y + iconPadY);
        var iconMax = new Vector2(thumbEndPos.X - iconPadX, thumbStartPos.Y + iconPadY + iconSize);

        // Camera
        _isCameraHovered = DrawActionIcon(iconMin, iconMax, cameraIconPath, Strings.TooltipCamera, windowHovered,
            path => plugin.ShowCameraOverlay(p =>
            {
                designMetadataService.SetCustomImage(designId, p);
                plugin.TextureCache.InvalidateTexture(p);
            }));

        // Upload
        var uploadMin = new Vector2(iconMin.X, iconMax.Y + iconGap);
        var uploadMax = new Vector2(iconMax.X, uploadMin.Y + iconSize);
        _isUploadHovered = DrawActionIcon(uploadMin, uploadMax, uploadIconPath, Strings.TooltipUpload, windowHovered,
            path => utility.OpenImageFilePicker(p =>
            {
                designMetadataService.SetCustomImage(designId, p);
                plugin.TextureCache.InvalidateTexture(p);
            }));

        // Clipboard
        var clipMin = new Vector2(iconMin.X, uploadMax.Y + iconGap);
        var clipMax = new Vector2(iconMax.X, clipMin.Y + iconSize);
        _isClipboardHovered = DrawActionIcon(clipMin, clipMax, clipboardIconPath, Strings.TooltipClipboard, windowHovered,
            path => utility.CopyImageFromClipboard(p =>
            {
                designMetadataService.SetCustomImage(designId, p);
                plugin.TextureCache.InvalidateTexture(p);
            }));

        // Favourite — top-left corner of thumbnail
        var favMin = new Vector2(thumbStartPos.X + 4f, thumbStartPos.Y + 4f);
        var favMax = new Vector2(favMin.X + iconSize, favMin.Y + iconSize);
        bool isFav = favoriteService.IsFavorite(designId);
        string favPath = isFav ? starFilledPath : starEmptyPath;
        string favTooltip = isFav ? Strings.TooltipFavRemove : Strings.TooltipFavAdd;
        _isFavHovered = DrawActionIcon(favMin, favMax, favPath, favTooltip, windowHovered, _ =>
        {
            favoriteService.Toggle(designId);
        });

        // Save Mods — below favourite star (only if enabled in settings)
        _lastSaveModsHovered = false;
        if (plugin.Configuration.EnableSaveMods)
        {
            var saveMin = new Vector2(thumbStartPos.X + 4f, favMax.Y + iconGap);
            var saveMax = new Vector2(saveMin.X + iconSize, saveMin.Y + iconSize);
            bool isSaveHovered = windowHovered && ImGui.IsMouseHoveringRect(saveMin, saveMax);
            bool hasSnapshot = plugin.ModStateService.HasSnapshot(designId);

            var saveTex = plugin.TextureCache.GetOrLoadTexture(saveModsIconPath)?.GetWrapOrDefault();
            if (saveTex != null)
            {
                uint saveTint;
                if (isSaveHovered)
                    saveTint = ImGui.GetColorU32(ThemeManager.Current.IconHovered);
                else if (hasSnapshot)
                    saveTint = ImGui.GetColorU32(ThemeManager.Current.SaveModsGold);
                else
                    saveTint = ImGui.GetColorU32(ThemeManager.Current.IconDefault);

                ImGui.GetWindowDrawList().AddImage(saveTex.Handle, saveMin, saveMax, Vector2.Zero, Vector2.One, saveTint);
            }

            if (isSaveHovered)
            {
                var tooltip = hasSnapshot
                    ? Strings.TooltipSaveModsReSave + "\n" + Strings.TooltipSaveModsClear
                    : Strings.TooltipSaveModsSave;
                ImGui.SetTooltip(tooltip);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    plugin.ModStateService.CaptureState(designId);
                    plugin.PenumbraService.LogModsForDesign(designId, plugin.GlamourerService);
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && hasSnapshot)
                {
                    plugin.ModStateService.ClearSnapshot(designId);
                }
            }

            _lastSaveModsHovered = isSaveHovered;
        }
    }

    private bool _isCameraHovered;
    private bool _isUploadHovered;
    private bool _isClipboardHovered;
    private bool _isFavHovered;
    private bool _lastSaveModsHovered;

    private bool DrawActionIcon(Vector2 min, Vector2 max, string iconPath, string tooltip,
        bool windowHovered, Action<string> onClick)
    {
        bool hovered = !IsInteractionBlocked && windowHovered && ImGui.IsMouseHoveringRect(min, max);
        uint tint = ImGui.GetColorU32(hovered ? ThemeManager.Current.IconHovered : ThemeManager.Current.IconDefault);
        var tex = plugin.TextureCache.GetOrLoadTexture(iconPath)?.GetWrapOrDefault();
        if (tex != null)
            ImGui.GetWindowDrawList().AddImage(tex.Handle, min, max, Vector2.Zero, Vector2.One, tint);
        if (hovered)
            ImGui.SetTooltip(tooltip);
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            plugin.CloseSubWindows();
            onClick("");
        }
        return hovered;
    }

    private void DrawThumbnailDoubleClick(Guid designId, Vector2 thumbStartPos, Vector2 thumbEndPos)
    {
        if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows))
            return;

        bool thumbHovered = ImGui.IsMouseHoveringRect(thumbStartPos, thumbEndPos);
        bool anyIconHovered = _isCameraHovered || _isUploadHovered || _isClipboardHovered || _lastSaveModsHovered || _isFavHovered;
        if (thumbHovered && !anyIconHovered)
        {
            ImGui.SetTooltip(Strings.TooltipThumbnail);
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                plugin.CloseSubWindows();
                plugin.GlamourerService.ApplyDesign(designId,
                    plugin.Configuration.ApplyEquipmentOnly || ImGui.GetIO().KeyCtrl);
                plugin.ModStateService.RestoreState(designId);
            }
        }
    }

    private void DrawDesignName(Guid designId, float width)
    {
        string displayName = designMetadataService.GetDisplayName(designId);
        bool isEditing = editingDesignId == designId;

        if (isEditing)
        {
            ImGui.SetCursorPosX(8f);
            ImGui.SetNextItemWidth(width - 16f);
            bool enterPressed = ImGui.InputText($"##rename_{designId}", ref editingDesignName, 64,
                ImGuiInputTextFlags.EnterReturnsTrue);
            if (enterPressed || ImGui.IsItemDeactivated())
            {
                var trimmed = editingDesignName.Trim();
                designMetadataService.SetNickname(designId, trimmed);
                editingDesignId = Guid.Empty;
                editingDesignName = string.Empty;
            }
        }
        else
        {
            const int maxNameChars = 24;
            string truncated = displayName.Length > maxNameChars
                ? displayName[..(maxNameChars - 3)] + "..."
                : displayName;

            var nameSize = ImGui.CalcTextSize(truncated);
            float nameX = Math.Max(8f, (width - nameSize.X) / 2);
            ImGui.SetCursorPosX(nameX);

            ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextHeading);
            ImGui.Text(truncated);
            ImGui.PopStyleColor();

            if (truncated.EndsWith("...") && ImGui.IsItemHovered())
                ImGui.SetTooltip(displayName);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Strings.TooltipRename);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left)
                && ImGui.GetIO().MouseClickedCount[(int)ImGuiMouseButton.Left] == 2)
            {
                editingDesignId = designId;
                editingDesignName = displayName;
            }
        }
    }

    private void DrawDesignButtons(Guid designId, float width, bool isHidden)
    {
        float btnW = isHidden ? 72f : 80f;
        const float btnHeight = 28f;
        const float btnSpacing = 12f;
        int btnCount = isHidden ? 3 : 2;
        float totalBtnWidth = btnW * btnCount + btnSpacing * (btnCount - 1);
        float btnStartX = (width - totalBtnWidth) / 2;

        ImGui.SetCursorPosX(btnStartX);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);

        // Apply
        ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.ApplyBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.ApplyBtnHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.ApplyBtnActive);
        if (ImGui.Button(Strings.CardApply + $"##btn_apply_{designId}", new Vector2(btnW, btnHeight)))
        {
            plugin.CloseSubWindows();
            plugin.GlamourerService.ApplyDesign(designId,
                plugin.Configuration.ApplyEquipmentOnly || ImGui.GetIO().KeyCtrl);
            plugin.ModStateService.RestoreState(designId);
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(Strings.TooltipApply);
            ImGui.TextDisabled(Strings.TooltipApplyCtrl);
            ImGui.EndTooltip();
        }
        ImGui.PopStyleColor(3);

        // Hide / Unhide
        ImGui.SameLine(btnStartX + btnW + btnSpacing);
        bool alreadyHidden = hiddenDesignService.IsHidden(designId);
        if (alreadyHidden)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.UnhideBtn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.UnhideBtnHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.UnhideBtnActive);
            if (ImGui.Button(Strings.CardUnhide + $"##btn_unhide_{designId}", new Vector2(btnW, btnHeight)))
                hiddenDesignService.ShowDesign(designId);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Strings.TooltipUnhide);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.DeleteBtn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.DeleteBtnHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.DeleteBtnActive);
            if (ImGui.Button(Strings.CardHide + $"##btn_hide_{designId}", new Vector2(btnW, btnHeight)))
                hiddenDesignService.HideDesign(designId);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Strings.TooltipHide);
        }
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar(); // FrameRounding

        // Delete (hidden cards only, at full opacity)
        if (isHidden)
        {
            ImGui.PopStyleVar(); // pop Alpha
            ImGui.SameLine(btnStartX + btnW * 2 + btnSpacing * 2);
            ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.DeleteBtn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.DeleteBtnHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.DeleteBtnActive);
            if (ImGui.Button(Strings.CardDelete + $"##btn_delete_{designId}", new Vector2(btnW, btnHeight)))
            {
                if (ImGui.GetIO().KeyCtrl)
                    ImGui.OpenPopup($"##confirm_delete_{designId}");
            }
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(Strings.TooltipDelete);
                ImGui.TextDisabled(Strings.TooltipDeleteCtrl);
                ImGui.EndTooltip();
            }

            if (ImGui.BeginPopup($"##confirm_delete_{designId}"))
            {
                ImGui.Text(Strings.ConfirmDeleteTitle);
                ImGui.Text(Strings.ConfirmDeleteBody);
                ImGui.Spacing();
                if (ImGui.Button($"{Strings.Yes}##del_yes"))
                {
                    plugin.CloseSubWindows();
                    plugin.GlamourerService.DeleteDesign(designId);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button($"{Strings.No}##del_no"))
                    ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
        }

        if (isHidden)
            ImGui.PopStyleVar(); // pop Alpha
    }
}
