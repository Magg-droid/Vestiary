using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Wardrobe.Models;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public partial class MainWindow
{
    private void DrawRouletteView()
    {
        var roulette = plugin.RouletteService;
        var config = plugin.Configuration;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);
        ImGui.SetCursorPosX(12f);

        // ── Heading ──
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextHeading);
        ImGui.SetWindowFontScale(1.25f);
        ImGui.Text(Strings.RouletteHeading);
        ImGui.SetWindowFontScale(1f);
        ImGui.PopStyleColor();

        ImGui.SetCursorPosX(12f);
        ImGui.TextColored(ThemeManager.Current.TextSubtle, Strings.RouletteSubheading);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.BeginChild("##RouletteScrollContent", new Vector2(-1, -1), false);

        // ── Card 1: Status & Control ──
        DrawRouletteStatusCard(roulette, config);

        ImGui.Spacing();
        ImGui.Spacing();

        // ── Card 2: Timer Interval ──
        DrawRouletteTimerCard(config);

        ImGui.Spacing();
        ImGui.Spacing();

        // ── Card 3: Included Collections ──
        DrawRouletteCollectionsCard(roulette, config);

        ImGui.EndChild();
    }

    // ── Draws a small dice icon using draw list primitives (no emoji needed) ──
    private static void DrawDiceIcon(ImDrawListPtr dl, Vector2 center, float size, uint col)
    {
        float half = size / 2f;
        // Outer box with rounded corners
        dl.AddRect(center - new Vector2(half, half), center + new Vector2(half, half), col, 2f, 0, 1.3f);
        // Three dots: top-right, middle, bottom-left (face "3")
        float r = MathF.Max(1.5f, size * 0.10f);
        dl.AddCircleFilled(center + new Vector2(half * 0.45f, -half * 0.45f), r, col);
        dl.AddCircleFilled(center, r, col);
        dl.AddCircleFilled(center + new Vector2(-half * 0.45f, half * 0.45f), r, col);
    }

    // ── Returns visible design count for a collection, correctly handling Favorites ──
    private int GetVisibleCountForCollection(Collection col)
    {
        // Favorites is a virtual collection — use FavoriteService to get actual favorited designs
        if (string.Equals(col.Name, "Favorites", StringComparison.OrdinalIgnoreCase))
        {
            var allDesigns = plugin.GlamourerService.GetDesignList();
            var favDesigns = favoriteService.GetFavorites(allDesigns);
            return hiddenDesignService.GetVisibleDesigns(favDesigns).Count;
        }

        var designsInCol = collectionService.GetDesignsByCollection(col.Id);
        return hiddenDesignService.GetVisibleDesigns(designsInCol).Count;
    }

    private void DrawRouletteStatusCard(RouletteService roulette, Configuration config)
    {
        var dl = ImGui.GetWindowDrawList();
        float availW = ImGui.GetContentRegionAvail().X;
        var start = ImGui.GetCursorScreenPos();
        const float margin = 16f;
        const float topBottomPad = 16f;
        float innerW = MathF.Max(120f, availW - margin * 2f);
        bool compact = innerW < 760f;

        var visiblePool = roulette.GetRouletteDesignPool();
        bool canSwap = visiblePool.Count > 0;

        float btnW = compact ? innerW : MathF.Min(295f, innerW * 0.44f);
        const float btnH = 66f;
        float swapW = MathF.Min(150f, MathF.Max(120f, innerW * 0.24f));
        const float swapH = 40f;

        float statusY = start.Y + topBottomPad;
        float detailsY;
        float swapY;
        float cardH;

        if (compact)
        {
            detailsY = statusY + btnH + 14f;
            swapY = detailsY + 56f;
            cardH = (swapY + swapH + topBottomPad) - start.Y;
            swapW = MathF.Min(innerW, 170f);
        }
        else
        {
            float centerY = start.Y + 58f;
            statusY = centerY - btnH / 2f;
            detailsY = centerY - 20f;
            swapY = centerY - swapH / 2f;
            cardH = 116f;
        }

        // Card background
        uint cardBg = ImGui.GetColorU32(ThemeManager.Current.CardBg);
        uint cardBorder = ImGui.GetColorU32(ThemeManager.Current.CardBorder);
        dl.AddRectFilled(start, start + new Vector2(availW, cardH), cardBg, 8f);
        dl.AddRect(start, start + new Vector2(availW, cardH), cardBorder, 8f, 0, 1.2f);

        // ── Toggle Status Button ──
        bool active = roulette.IsActive;
        var btnPos = new Vector2(start.X + margin, statusY);

        uint toggleBg = active
            ? ImGui.GetColorU32(ThemeManager.Current.ChipBgActive)
            : ImGui.GetColorU32(ThemeManager.Current.ChipBg);
        uint toggleHov = active
            ? ImGui.GetColorU32(ThemeManager.Current.ApplyBtn)
            : ImGui.GetColorU32(ThemeManager.Current.ChipBgHovered);

        bool hovered = ImGui.IsMouseHoveringRect(btnPos, btnPos + new Vector2(btnW, btnH));
        dl.AddRectFilled(btnPos, btnPos + new Vector2(btnW, btnH), hovered ? toggleHov : toggleBg, 6f);
        dl.AddRect(btnPos, btnPos + new Vector2(btnW, btnH), ImGui.GetColorU32(ThemeManager.Current.ChipBorder), 6f, 0, 1f);

        // ── Colored status circle: green = active, red = inactive ──
        // ImGui ABGR byte order: 0xAABBGGRR
        const float circleR = 5f;
        float circleX = btnPos.X + 20f;
        float circleCenterY = btnPos.Y + btnH / 2f;
        uint greenCol = 0xFF40D060u; // bright green
        uint redCol   = 0xFF3344DDu; // red (ABGR: DD=R, 44=G, 33=B → warm red)
        dl.AddCircleFilled(new Vector2(circleX, circleCenterY), circleR, active ? greenCol : redCol);
        dl.AddCircle(new Vector2(circleX, circleCenterY), circleR, active ? 0xFF30B050u : 0xFF2233BBu, 16, 1f);

        // Status text — indented right of circle
        string mainText = active ? Strings.RouletteStatusActive : Strings.RouletteStatusInactive;
        string subText  = active ? Strings.RouletteStatusActiveSub : Strings.RouletteStatusInactiveSub;
        uint textCol = ImGui.GetColorU32(active ? ThemeManager.Current.TextHeading : ThemeManager.Current.TextNormal);

        var szMain = ImGui.CalcTextSize(mainText);
        var szSub  = ImGui.CalcTextSize(subText);
        float textBlockH = szMain.Y + szSub.Y + 4f;
        float textY = btnPos.Y + (btnH - textBlockH) / 2f;
        float textX = circleX + circleR + 12f;

        dl.AddText(new Vector2(textX, textY), textCol, mainText);
        dl.AddText(new Vector2(textX, textY + szMain.Y + 4f),
            ImGui.GetColorU32(active ? ThemeManager.Current.TextNormal : ThemeManager.Current.TextSubtle), subText);

        ImGui.SetCursorScreenPos(btnPos);
        if (ImGui.InvisibleButton("##rouletteToggleBtn", new Vector2(btnW, btnH)))
            roulette.ToggleRoulette();
        if (hovered)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // ── Status details ──
        float midX = compact ? (start.X + margin) : (btnPos.X + btnW + 24f);
        float midY = detailsY;

        string remainingStr = active
            ? $"{roulette.RemainingTime.Minutes:D2}m {roulette.RemainingTime.Seconds:D2}s"
            : "--:--";

        dl.AddText(new Vector2(midX, midY),
            ImGui.GetColorU32(ThemeManager.Current.TextHeading),
            $"Next Swap In: {remainingStr}");
        dl.AddText(new Vector2(midX, midY + 26f),
            ImGui.GetColorU32(ThemeManager.Current.TextSubtle),
            $"Pool: {visiblePool.Count} visible outfits in roulette");

        // ── Swap Now Button (Right) ──
        float swapX = compact
            ? (start.X + margin)
            : (start.X + availW - swapW - margin);
        var swapPos = new Vector2(swapX, swapY);

        bool swapHov = ImGui.IsMouseHoveringRect(swapPos, swapPos + new Vector2(swapW, swapH));

        uint swapBg = ImGui.GetColorU32(swapHov && canSwap ? ThemeManager.Current.ChipBgActive : ThemeManager.Current.ChipBg);
        dl.AddRectFilled(swapPos, swapPos + new Vector2(swapW, swapH), swapBg, 6f);
        dl.AddRect(swapPos, swapPos + new Vector2(swapW, swapH), ImGui.GetColorU32(ThemeManager.Current.ChipBorder), 6f, 0, 1f);

        uint swapTextCol = ImGui.GetColorU32(!canSwap ? ThemeManager.Current.TextSubtle : ThemeManager.Current.TextNormal);
        string swapLabel = Strings.RouletteSwapNow;
        var szSwapLabel = ImGui.CalcTextSize(swapLabel);
        const float diceSize = 13f;
        const float diceGap  = 6f;
        float totalInnerW  = diceSize + diceGap + szSwapLabel.X;
        float innerStartX  = swapPos.X + (swapW - totalInnerW) / 2f;
        float swapCenterY  = swapPos.Y + swapH / 2f;

        DrawDiceIcon(dl, new Vector2(innerStartX + diceSize / 2f, swapCenterY), diceSize, swapTextCol);
        dl.AddText(new Vector2(innerStartX + diceSize + diceGap, swapCenterY - szSwapLabel.Y / 2f), swapTextCol, swapLabel);

        ImGui.SetCursorScreenPos(swapPos);
        ImGui.BeginDisabled(!canSwap);
        if (ImGui.InvisibleButton("##rouletteSwapNowBtn", new Vector2(swapW, swapH)))
            roulette.TriggerRandomPick(manualTrigger: true);
        ImGui.EndDisabled();

        if (swapHov)
        {
            ImGui.SetTooltip(Strings.TooltipRouletteSwapNow);
            if (canSwap) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        // Reserve layout height
        ImGui.SetCursorScreenPos(start + new Vector2(0, cardH));
        ImGui.Dummy(new Vector2(availW, 0));
    }

    private void DrawRouletteTimerCard(Configuration config)
    {
        var dl = ImGui.GetWindowDrawList();
        float availW = ImGui.GetContentRegionAvail().X;
        var start = ImGui.GetCursorScreenPos();
        const float margin = 16f;
        const float topPad = 16f;
        const float bottomPad = 24f;
        float innerW = MathF.Max(120f, availW - margin * 2f);

        uint cardBg = ImGui.GetColorU32(ThemeManager.Current.CardBg);
        uint cardBorder = ImGui.GetColorU32(ThemeManager.Current.CardBorder);
        // Pre-calculate layout heights for a responsive card.
        int[] presets = new[] { 5, 10, 15, 30, 60 };
        const float pW = 48f;
        const float pH = 30f;
        const float chipGap = 8f;

        int chipsPerRow = Math.Max(1, (int)((innerW + chipGap) / (pW + chipGap)));
        int chipRows = (int)MathF.Ceiling(presets.Length / (float)chipsPerRow);

        float headingY = start.Y + topPad;
        float labelY = headingY + 30f;
        float chipsY = labelY + 28f;
        float chipsBottom = chipsY + chipRows * pH + (chipRows - 1) * chipGap;
        float sliderY = chipsBottom + 14f;
        float cardH = (sliderY + 20f + bottomPad) - start.Y;

        dl.AddRectFilled(start, start + new Vector2(availW, cardH), cardBg, 8f);
        dl.AddRect(start, start + new Vector2(availW, cardH), cardBorder, 8f, 0, 1.2f);

        // Row 1: Heading
        ImGui.SetCursorScreenPos(new Vector2(start.X + margin, headingY));
        ImGui.TextColored(ThemeManager.Current.TextHeading, Strings.RouletteTimerHeading);

        // Row 2: "Quick Select:" label — its own row
        ImGui.SetCursorScreenPos(new Vector2(start.X + margin, labelY));
        ImGui.TextColored(ThemeManager.Current.TextSubtle, Strings.RoulettePresetsLabel);

        // Row 3: Preset chips — wraps to additional rows on narrow windows.
        float chipsStartX = start.X + margin;

        for (int i = 0; i < presets.Length; i++)
        {
            int val = presets[i];
            bool isActive = config.RouletteIntervalMinutes == val;
            string label = $"{val}m";
            int col = i % chipsPerRow;
            int row = i / chipsPerRow;
            var pPos = new Vector2(chipsStartX + col * (pW + chipGap), chipsY + row * (pH + chipGap));

            bool pHov = ImGui.IsMouseHoveringRect(pPos, pPos + new Vector2(pW, pH));
            uint pBg = isActive
                ? ImGui.GetColorU32(ThemeManager.Current.ChipBgActive)
                : ImGui.GetColorU32(pHov ? ThemeManager.Current.ChipBgHovered : ThemeManager.Current.ChipBg);
            dl.AddRectFilled(pPos, pPos + new Vector2(pW, pH), pBg, 4f);
            dl.AddRect(pPos, pPos + new Vector2(pW, pH), ImGui.GetColorU32(ThemeManager.Current.ChipBorder), 4f, 0, 1f);

            var sz = ImGui.CalcTextSize(label);
            uint tCol = ImGui.GetColorU32(isActive ? ThemeManager.Current.ChipTextActive : ThemeManager.Current.ChipText);
            dl.AddText(new Vector2(pPos.X + (pW - sz.X) / 2f, pPos.Y + (pH - sz.Y) / 2f), tCol, label);

            ImGui.SetCursorScreenPos(pPos);
            if (ImGui.InvisibleButton($"##preset_{val}", new Vector2(pW, pH)))
            {
                config.RouletteIntervalMinutes = val;
                config.Save();
            }
            if (pHov) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        // Slider — moved below chips so narrow windows do not overlap.
        float sliderW = MathF.Min(320f, innerW);
        float sliderX = start.X + margin;
        ImGui.SetCursorScreenPos(new Vector2(sliderX, sliderY));
        ImGui.PushItemWidth(sliderW);
        int interval = config.RouletteIntervalMinutes;
        if (ImGui.SliderInt("##rouletteIntervalSlider", ref interval, 1, 120, "%d mins"))
        {
            config.RouletteIntervalMinutes = Math.Clamp(interval, 1, 120);
            config.Save();
        }
        ImGui.PopItemWidth();

        ImGui.SetCursorScreenPos(start + new Vector2(0, cardH));
        ImGui.Dummy(new Vector2(availW, 0));
    }

    private void DrawRouletteCollectionsCard(RouletteService roulette, Configuration config)
    {
        var dl = ImGui.GetWindowDrawList();
        float availW = ImGui.GetContentRegionAvail().X;
        var start = ImGui.GetCursorScreenPos();
        const float margin = 16f;
        const float colGap = 16f;
        float innerW = MathF.Max(120f, availW - margin * 2f);

        var collections = collectionService.GetCollections();
        bool excludeFav = config.RouletteExcludeFavorites;
        config.RouletteCollectionIds ??= new();

        // Pre-calculate visible collections — Favorites handled separately for correct count
        var visibleCols = collections
            .Where(c => !(excludeFav && string.Equals(c.Name, "Favorites", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Use one column on narrow widths so labels stay inside the card.
        int columns = innerW >= 700f ? 2 : 1;
        int colRowCount = (int)MathF.Ceiling(visibleCols.Count / (float)columns);

        const float headingY = 16f;
        const float dividerY = 42f;
        const float rowH = 38f;
        const float footerPad = 22f;

        // Dynamic Y positions so spacing remains consistent at all widths.
        float checkboxY = start.Y + 56f;
        float hintY = checkboxY + 28f;

        // Estimate wrapped hint height for correct card size and grid start.
        float hintWidth = MathF.Max(120f, innerW);
        float hintTextWidth = ImGui.CalcTextSize(Strings.RouletteSelectCollectionsHint).X;
        int hintLines = Math.Max(1, (int)MathF.Ceiling(hintTextWidth / hintWidth));
        float hintHeight = hintLines * ImGui.GetTextLineHeightWithSpacing();
        float gridStartY = hintY + hintHeight + 14f;
        float totalH = (gridStartY - start.Y) + colRowCount * rowH + footerPad;

        // Draw card background FIRST so widgets paint on top
        uint cardBg = ImGui.GetColorU32(ThemeManager.Current.CardBg);
        uint cardBorder = ImGui.GetColorU32(ThemeManager.Current.CardBorder);
        dl.AddRectFilled(start, start + new Vector2(availW, totalH), cardBg, 8f);
        dl.AddRect(start, start + new Vector2(availW, totalH), cardBorder, 8f, 0, 1.2f);

        // Heading
        ImGui.SetCursorScreenPos(start + new Vector2(margin, headingY));
        ImGui.TextColored(ThemeManager.Current.TextHeading, Strings.RouletteCollectionsHeading);

        // Thin separator under heading
        dl.AddLine(
            new Vector2(start.X + margin, start.Y + dividerY),
            new Vector2(start.X + availW - margin, start.Y + dividerY),
            ImGui.GetColorU32(ThemeManager.Current.CardBorder), 1f);

        // Exclude Favorites checkbox
        ImGui.SetCursorScreenPos(new Vector2(start.X + margin, checkboxY));
        if (ImGui.Checkbox(Strings.RouletteExcludeFavorites, ref excludeFav))
        {
            config.RouletteExcludeFavorites = excludeFav;
            config.Save();
        }

        float checkboxBottom = ImGui.GetItemRectMax().Y;
        hintY = checkboxBottom + 10f;

        // Hint text
        ImGui.SetCursorScreenPos(new Vector2(start.X + margin, hintY));
        ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextSubtle);
        float hintWrapPos = ImGui.GetCursorPosX() + innerW;
        ImGui.PushTextWrapPos(hintWrapPos);
        ImGui.TextWrapped(Strings.RouletteSelectCollectionsHint);
        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();

        // Collection checkboxes in 2-column grid
        // Count uses GetVisibleCountForCollection so Favorites shows actual favorited designs.
        float colW = columns == 1
            ? innerW
            : (innerW - colGap) / 2f;
        for (int i = 0; i < visibleCols.Count; i++)
        {
            var col = visibleCols[i];
            bool isChecked = config.RouletteCollectionIds.Contains(col.Id);
            int visCount = GetVisibleCountForCollection(col);
            string colLabel = $"{col.Name} ({visCount} outfits)";

            int colIndex = i % columns;
            int rowIndex = i / columns;
            float posX = start.X + margin + colIndex * (colW + colGap);
            float posY = gridStartY + rowIndex * rowH;
            ImGui.SetCursorScreenPos(new Vector2(posX, posY));

            if (ImGui.Checkbox($"{colLabel}##col_chk_{col.Id}", ref isChecked))
            {
                if (isChecked)
                {
                    if (!config.RouletteCollectionIds.Contains(col.Id))
                        config.RouletteCollectionIds.Add(col.Id);
                }
                else
                {
                    config.RouletteCollectionIds.Remove(col.Id);
                }
                config.Save();
            }
        }

        ImGui.SetCursorScreenPos(start + new Vector2(0, totalH));
        ImGui.Dummy(new Vector2(availW, 0));
    }
}
