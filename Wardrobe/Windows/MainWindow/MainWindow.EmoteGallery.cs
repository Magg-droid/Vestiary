using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ECommons.Automation;

namespace Wardrobe.Windows;

public partial class MainWindow
{
    private void DrawEmoteGallery()
    {
        var cards = plugin.EmoteService.GetCards();

        // Filter by search text (name first, then emote name)
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var q = searchText.Trim().ToLower();
            cards = cards
                .Where(c =>
                    (!string.IsNullOrEmpty(c.Name) && c.Name.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.EmoteName) && c.EmoteName.ToLower().Contains(q)))
                .OrderBy(c =>
                    (!string.IsNullOrEmpty(c.Name) && c.Name.ToLower().Contains(q)) ? 0 : 1)
                .ToList();
        }

        // ── Gallery ──
        var emoteSheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Emote>();
        var sortedEmotes = new List<string>();
        var emoteCommands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var emote in emoteSheet)
        {
            var name = emote.Name.ToString();
            if (string.IsNullOrEmpty(name)) continue;
            if (!sortedEmotes.Contains(name)) sortedEmotes.Add(name);
            var cmd = emote.TextCommand.ValueNullable?.Command.ToString() ?? "";
            if (!string.IsNullOrEmpty(cmd)) emoteCommands[name] = cmd;
        }
        sortedEmotes.Sort(StringComparer.OrdinalIgnoreCase);

        ImGui.BeginChild("##EmoteGalleryScroll", new Vector2(-1, -1), false, ImGuiWindowFlags.None);

        ImGui.Dummy(new Vector2(0, 10f));

        const float cardWidth = 260f;
        const float cardHeight = 400f;
        const float cardSpacing = 25f;
        var availW = ImGui.GetContentRegionAvail().X;
        int cols = Math.Max(1, (int)((availW - cardSpacing) / (cardWidth + cardSpacing)));
        float totalRowW = cardWidth * cols + cardSpacing * (cols - 1);
        const float leftMargin = 8f;
        float startX = leftMargin;

        int col = 0;
        foreach (var card in cards)
        {
            if (col == 0) ImGui.SetCursorPosX(startX);
            else ImGui.SameLine(0, cardSpacing);
            DrawEmoteCard(card, cardWidth, cardHeight, sortedEmotes, emoteCommands);
            col++;
            if (col >= cols) { col = 0; ImGui.SetCursorPosY(ImGui.GetCursorPosY() + cardSpacing); }
        }

        if (col == 0) ImGui.SetCursorPosX(startX);
        else ImGui.SameLine(0, cardSpacing);
        DrawAddEmoteCard(cardWidth, cardHeight);

        ImGui.EndChild();
    }

    private void DrawEmoteCard(Models.EmoteCard card, float width, float height, List<string> emoteNames, Dictionary<string, string> emoteCommands)
    {
        var cardStart = ImGui.GetCursorScreenPos();
        var cardEnd = cardStart + new Vector2(width, height);
        bool hovered = !IsInteractionBlocked && ImGui.IsMouseHoveringRect(cardStart, cardEnd);
        bool editing = _editingCardId == card.Id;

        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(cardStart, cardEnd,
            ImGui.GetColorU32(hovered ? ThemeManager.Current.CardBgHovered : ThemeManager.Current.CardBg), 12f);
        dl.AddRect(cardStart, cardEnd,
            ImGui.GetColorU32(hovered ? ThemeManager.Current.CardBorder : ThemeManager.Current.CardBorderIdle), 12f, 0, 1.5f);

        ImGui.BeginChild($"##emote_{card.Id}", new Vector2(width, height), false);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);

        const float thumbW = 240f; const float thumbH = 300f;
        ImGui.SetCursorPosX(10f);
        var thumbStart = ImGui.GetCursorScreenPos();
        var thumbEnd = thumbStart + new Vector2(thumbW, thumbH);
        dl.AddRectFilled(thumbStart, thumbEnd, ImGui.GetColorU32(ThemeManager.Current.ThumbBg), 4f);
        dl.AddRect(thumbStart, thumbEnd, ImGui.GetColorU32(ThemeManager.Current.ThumbBorder), 4f, 0, 1f);

        if (!string.IsNullOrEmpty(card.ThumbnailPath))
        {
            var thumbTex = plugin.TextureCache.GetOrLoadTexture(card.ThumbnailPath)?.GetWrapOrDefault();
            if (thumbTex != null)
                dl.AddImage(thumbTex.Handle, thumbStart, thumbEnd, Vector2.Zero, Vector2.One, ImGui.GetColorU32(Vector4.One));
        }
        else
        {
            var placeholder = string.IsNullOrEmpty(card.EmoteName) ? "Emote" : card.Name;
            var textSize = ImGui.CalcTextSize(placeholder);
            dl.AddText(thumbStart + new Vector2((thumbW - textSize.X) / 2, (thumbH - textSize.Y) / 2),
                ImGui.GetColorU32(ThemeManager.Current.TextMuted), placeholder);
        }

        ImGui.Dummy(new Vector2(thumbW, thumbH));

        // Icons (hidden in edit mode)
        const float iconSize = 28f; const float iconGap = 4f;

        if (!editing)
        {
            var saveMin = new Vector2(thumbStart.X + 6f, thumbStart.Y + 6f);
            var saveMax = new Vector2(saveMin.X + iconSize, saveMin.Y + iconSize);
            bool isSaveHovered = !IsInteractionBlocked && ImGui.IsMouseHoveringRect(saveMin, saveMax);
            bool hasState = plugin.EmoteService.HasState(card.Id);
            var saveTex = plugin.TextureCache.GetOrLoadTexture(saveModsIconPath)?.GetWrapOrDefault();
            if (saveTex != null)
            {
                uint tint = isSaveHovered ? ImGui.GetColorU32(ThemeManager.Current.IconHovered)
                    : hasState ? ImGui.GetColorU32(ThemeManager.Current.SaveModsGold) : ImGui.GetColorU32(ThemeManager.Current.IconDefault);
                dl.AddImage(saveTex.Handle, saveMin, saveMax, Vector2.Zero, Vector2.One, tint);
            }
            if (isSaveHovered)
            {
                ImGui.SetTooltip(hasState ? Strings.TooltipSaveModsReSave + "\n" + Strings.TooltipSaveModsClear : Strings.TooltipSaveModsSave);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) plugin.EmoteService.CaptureState(card.Id);
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && hasState) { card.Mods.Clear(); plugin.Configuration.Save(); }
            }

            const float iconPadX = 8f; const float iconPadY = -3f;
            var iconMin = new Vector2(thumbEnd.X - iconSize - iconPadX, thumbStart.Y + iconPadY);
            var iconMax = new Vector2(thumbEnd.X - iconPadX, thumbStart.Y + iconPadY + iconSize);
            DrawEmoteIcon(iconMin, iconMax, cameraIconPath, Strings.TooltipCamera,
                path => plugin.ShowCameraOverlay(p => card.ThumbnailPath = p));
            var uploadMin = new Vector2(iconMin.X, iconMax.Y + iconGap);
            var uploadMax = new Vector2(iconMax.X, uploadMin.Y + iconSize);
            DrawEmoteIcon(uploadMin, uploadMax, uploadIconPath, Strings.TooltipUpload,
                p => plugin.UtilityService.OpenImageFilePicker(p => card.ThumbnailPath = p));
            var clipMin = new Vector2(iconMin.X, uploadMax.Y + iconGap);
            var clipMax = new Vector2(iconMax.X, clipMin.Y + iconSize);
            DrawEmoteIcon(clipMin, clipMax, clipboardIconPath, Strings.TooltipClipboard,
                p => plugin.UtilityService.CopyImageFromClipboard(p => card.ThumbnailPath = p));
        }

        // Name
        float nameY = ImGui.GetCursorPosY();
        ImGui.SetCursorPosX(10f);
        if (editing) { ImGui.SetNextItemWidth(width - 20f); ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4f, -1f)); var n = card.Name; if (ImGui.InputText($"##en_{card.Id}", ref n, 64)) card.Name = n; ImGui.PopStyleVar(); }
        else ImGui.TextColored(string.IsNullOrEmpty(card.Name) ? ThemeManager.Current.TextSubtle : ThemeManager.Current.TextNormal,
            string.IsNullOrEmpty(card.Name) ? "Emote Name" : card.Name);

        // Emote — fixed position
        float emoteY = nameY + 24f;
        ImGui.SetCursorPosY(emoteY);
        ImGui.SetCursorPosX(10f);
        if (editing)
        {
            ImGui.SetNextItemWidth(width - 20f);
            var label = !string.IsNullOrEmpty(card.EmoteName) ? card.EmoteName : "Pick emote...";
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4f, -1f));
            if (ImGui.BeginCombo($"##esel_{card.Id}", label))
            {
                if (_justOpenedModCombo != card.Id) { _emoteModSearch = string.Empty; _justOpenedModCombo = card.Id; }
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint($"##esrch_{card.Id}", "Search...", ref _emoteModSearch, 32);
                foreach (var name in emoteNames)
                {
                    if (!string.IsNullOrEmpty(_emoteModSearch) && !name.Contains(_emoteModSearch, StringComparison.OrdinalIgnoreCase)) continue;
                    if (ImGui.Selectable(name)) { card.EmoteName = name; if (string.IsNullOrEmpty(card.Name)) card.Name = name; _emoteModSearch = string.Empty; plugin.Configuration.Save(); }
                }
                ImGui.EndCombo();
            }
            ImGui.PopStyleVar();
        }
        else ImGui.TextDisabled(string.IsNullOrEmpty(card.EmoteName) ? "No emote selected" : card.EmoteName);

        // Buttons — fixed position
        float btnY = emoteY + 28f;
        ImGui.SetCursorPosY(btnY);
        ImGui.SetCursorPosX(10f);
        float btnW = (width - 36f) / 3; float btnH = 28f;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);

        ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.ApplyBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.ApplyBtnHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.ApplyBtnActive);
        if (ImGui.Button($"Apply##ea_{card.Id}", new Vector2(btnW, btnH)))
        {
            plugin.EmoteService.RestoreState(card.Id);
            if (!string.IsNullOrEmpty(card.EmoteName) && emoteCommands.TryGetValue(card.EmoteName, out var ecmd))
                _pendingEmoteCommand = ecmd;
        }
        ImGui.PopStyleColor(3);
        ImGui.SameLine();
        if (editing)
        {
            if (ImGui.Button($"Save##es_{card.Id}", new Vector2(btnW, btnH))) { plugin.Configuration.Save(); _editingCardId = Guid.Empty; }
        }
        else { if (ImGui.Button($"Edit##ee_{card.Id}", new Vector2(btnW, btnH))) _editingCardId = card.Id; }

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.DeleteBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.DeleteBtnHover);
        if (ImGui.Button($"Delete##ed_{card.Id}", new Vector2(btnW, btnH))) plugin.EmoteService.DeleteCard(card.Id);
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar();

        bool anyIconHovered = ImGui.IsMouseHoveringRect(new Vector2(thumbStart.X, thumbStart.Y),
            new Vector2(thumbEnd.X, thumbStart.Y + 70f)); // icons area
        if (ImGui.IsMouseHoveringRect(thumbStart, thumbEnd) && !anyIconHovered)
        {
            ImGui.SetTooltip("Double-click to restore mods");
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                plugin.EmoteService.RestoreState(card.Id);
                if (!string.IsNullOrEmpty(card.EmoteName) && emoteCommands.TryGetValue(card.EmoteName, out var dcmd))
                    _pendingEmoteCommand = dcmd;
            }
        }
        ImGui.EndChild();
    }

    private void DrawAddEmoteCard(float width, float height)
    {
        var cardStart = ImGui.GetCursorScreenPos(); var cardEnd = cardStart + new Vector2(width, height);
        bool hovered = !IsInteractionBlocked && ImGui.IsMouseHoveringRect(cardStart, cardEnd);
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(cardStart, cardEnd,
            ImGui.GetColorU32(hovered ? ThemeManager.Current.CardBgHovered : ThemeManager.Current.CardBg), 12f);
        dl.AddRect(cardStart, cardEnd,
            ImGui.GetColorU32(hovered ? ThemeManager.Current.CardBorder : ThemeManager.Current.CardBorderIdle), 12f, 0, 1.5f);

        float thumbW = 240f; float thumbH = 300f;
        var thumbStart = new Vector2(cardStart.X + 10f, cardStart.Y + 8f);
        var thumbEnd = thumbStart + new Vector2(thumbW, thumbH);
        dl.AddRectFilled(thumbStart, thumbEnd, ImGui.GetColorU32(ThemeManager.Current.ThumbBg), 8f);
        dl.AddRect(thumbStart, thumbEnd, ImGui.GetColorU32(ThemeManager.Current.ThumbBorder), 8f, 0, 1f);
        var plus = "+"; var plusSize = ImGui.CalcTextSize(plus);
        dl.AddText(thumbStart + new Vector2((thumbW - plusSize.X) / 2, thumbH / 2 - plusSize.Y - 6),
            ImGui.GetColorU32(ThemeManager.Current.TextMuted), plus);
        var label = "Create Emote Card"; var labelSize = ImGui.CalcTextSize(label);
        dl.AddText(thumbStart + new Vector2((thumbW - labelSize.X) / 2, thumbH / 2 + 6),
            ImGui.GetColorU32(ThemeManager.Current.TextSubtle), label);
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            plugin.EmoteService.CreateCard("Emote Name", "");
    }

    private void DrawEmoteIcon(Vector2 min, Vector2 max, string iconPath, string tooltip, Action<string> onClick)
    {
        bool hovered = !IsInteractionBlocked && ImGui.IsMouseHoveringRect(min, max);
        var tex = plugin.TextureCache.GetOrLoadTexture(iconPath)?.GetWrapOrDefault();
        if (tex != null)
            ImGui.GetWindowDrawList().AddImage(tex.Handle, min, max, Vector2.Zero, Vector2.One,
                ImGui.GetColorU32(hovered ? ThemeManager.Current.IconHovered : ThemeManager.Current.IconDefault));
        if (hovered) { ImGui.SetTooltip(tooltip); if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) onClick(""); }
    }
}
