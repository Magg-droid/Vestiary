using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Wardrobe.Models;

namespace Wardrobe.Windows;

public partial class MainWindow
{
    /// <summary>
    /// Draws the collection tab bar. Returns (maxTabHeight, tabBarStartPosition).
    /// </summary>
    private (float maxTabH, Vector2 tabBarStart) DrawTabBar(
        List<Collection> sortedCollections, ImDrawListPtr dl)
    {
        const float tabPadX = 14f;
        const float tabPadY = 6f;
        const float tabRounding = 6f;
        const float tabSpacing = 3f;

        var tabBarStart = ImGui.GetCursorScreenPos();
        float cursorX = tabBarStart.X;
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

            uint tabBg = isSelected
                ? ImGui.GetColorU32(ThemeManager.Current.TabSelected)
                : ImGui.IsMouseHoveringRect(tabMin, tabMax)
                    ? ImGui.GetColorU32(ThemeManager.Current.TabHovered)
                    : ImGui.GetColorU32(ThemeManager.Current.TabDefault);

            dl.AddRectFilled(tabMin, tabMax, tabBg, tabRounding, ImDrawFlags.RoundCornersTop);

            uint textCol = ImGui.GetColorU32(isSelected
                ? ThemeManager.Current.TabTextActive
                : ThemeManager.Current.TabTextIdle);
            dl.AddText(new Vector2(cursorX + tabPadX, tabBarStart.Y + tabPadY), textCol, collection.Name);

            ImGui.SetCursorScreenPos(tabMin);
            ImGui.InvisibleButton($"##tab_{collection.Id}", new Vector2(tabW, tabH));

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                selectedCollectionId = collection.Id;

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Strings.TabRightClickTooltip);

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

            if (ImGui.BeginDragDropSource())
            {
                dragTabIndex = i;
                ImGui.SetDragDropPayload("COLLECTION_TAB", ReadOnlySpan<byte>.Empty);
                ImGui.Text(collection.Name);
                ImGui.EndDragDropSource();
            }

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

        // Bottom line
        float lineY = tabBarStart.Y + maxTabH + 3f;
        var lineEnd = new Vector2(tabBarStart.X + ImGui.GetContentRegionAvail().X, lineY);
        dl.AddLine(new Vector2(tabBarStart.X, lineY), lineEnd,
            ImGui.GetColorU32(ThemeManager.Current.TabBorderLine), 1.5f);

        // "+" button
        float plusW = 28f;
        var plusMin = new Vector2(cursorX + 4f, tabBarStart.Y + 2f);
        var plusMax = new Vector2(plusMin.X + plusW, tabBarStart.Y + maxTabH);
        bool plusHover = ImGui.IsMouseHoveringRect(plusMin, plusMax);
        uint plusBg = ImGui.GetColorU32(plusHover ? ThemeManager.Current.PlusBtn : ThemeManager.Current.PlusBtnInactive);
        dl.AddRectFilled(plusMin, plusMax, plusBg, tabRounding, ImDrawFlags.RoundCornersTop);
        var plusTextSize = ImGui.CalcTextSize("+");
        dl.AddText(new Vector2(plusMin.X + (plusW - plusTextSize.X) / 2f, plusMin.Y + 4f),
            ImGui.GetColorU32(ThemeManager.Current.TabPlusIcon), "+");
        ImGui.SetCursorScreenPos(plusMin);
        ImGui.InvisibleButton("##new_collection", new Vector2(plusW, plusMax.Y - plusMin.Y));
        if (ImGui.IsItemClicked())
            collectionEditorWindow?.OpenCreate();

        return (maxTabH, tabBarStart);
    }
}
