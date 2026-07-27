using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly string noPreviewImagePath;
    private readonly Plugin plugin;
    private readonly CollectionService collectionService;
    private readonly DesignMetadataService designMetadataService;
    private CollectionEditorWindow? collectionEditorWindow;
    private DesignEditorWindow? designEditorWindow;
    private Guid selectedCollectionId = Guid.Empty;

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(
        Plugin plugin,
        string goatImagePath,
        CollectionService collectionService,
        DesignMetadataService designMetadataService,
        string noPreviewImagePath
    )
        : base("Wardrobe##With a hidden ID", ImGuiWindowFlags.None)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.goatImagePath = goatImagePath;
        this.noPreviewImagePath = noPreviewImagePath;
        this.plugin = plugin;
        this.collectionService = collectionService;
        this.designMetadataService = designMetadataService;
        this.collectionEditorWindow = null!; // Will be set after construction
        this.designEditorWindow = null!; // Will be set after construction
    }

    public void SetCollectionEditorWindow(CollectionEditorWindow editor)
    {
        collectionEditorWindow = editor;
    }

    public void SetDesignEditorWindow(DesignEditorWindow editor)
    {
        designEditorWindow = editor;
    }

    /// <summary>
    /// Check if a design has a custom image.
    /// </summary>
    private bool HasCustomImage(Guid designId)
    {
        try
        {
            var metadata = designMetadataService.GetMetadata(designId);
            return !string.IsNullOrEmpty(metadata?.CustomImagePath) && File.Exists(metadata.CustomImagePath);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() { }

    public override void Draw()
    {
        try
        {
            var collections = collectionService.GetCollections();

            // Initialize selectedCollectionId on first draw
            if (selectedCollectionId == Guid.Empty && collections.Count > 0)
                selectedCollectionId = collections[0].Id;

            // Verify selected collection still exists; fallback if deleted
            if (
                selectedCollectionId != Guid.Empty
                && !collections.Any(c => c.Id == selectedCollectionId)
            )
            {
                selectedCollectionId = collections.Count > 0 ? collections[0].Id : Guid.Empty;
            }

            // Draw tab bar with spacing
            ImGui.Spacing();
            if (ImGui.BeginTabBar("##CollectionsTabBar"))
            {
                foreach (var collection in collections)
                {
                    bool isSelected = selectedCollectionId == collection.Id;
                    if (ImGui.BeginTabItem($"{collection.Name}##tab_{collection.Id}"))
                    {
                        selectedCollectionId = collection.Id;
                        ImGui.EndTabItem();
                    }

                    // Right-click context menu for tab
                    if (ImGui.BeginPopupContextItem($"##tab_context_{collection.Id}"))
                    {
                        if (ImGui.MenuItem("Edit"))
                        {
                            collectionEditorWindow?.OpenEdit(collection);
                            ImGui.CloseCurrentPopup();
                        }

                        if (ImGui.MenuItem("Delete", "Del"))
                        {
                            collectionService.DeleteCollection(collection.Id);
                            if (selectedCollectionId == collection.Id)
                            {
                                selectedCollectionId = Guid.Empty;
                            }
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.EndPopup();
                    }
                }

                // "+" button to create new collection
                if (ImGui.TabItemButton("+", ImGuiTabItemFlags.Trailing))
                {
                    collectionEditorWindow?.OpenCreate();
                }

                ImGui.EndTabBar();
            }

            // Display design count at top right
            if (selectedCollectionId != Guid.Empty)
            {
                var designs = collectionService.GetDesignsByCollection(selectedCollectionId);
                string countText = $"{designs.Count} designs";
                Vector2 countSize = ImGui.CalcTextSize(countText);
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - countSize.X - 15f);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 30f);
                ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.7f, 1f), countText);
            }

            ImGui.Spacing();
            ImGui.Separator();

            ImGui.Dummy(new Vector2(0, 20f));

            // Display designs from selected collection
            if (selectedCollectionId != Guid.Empty)
            {
                var designs = collectionService.GetDesignsByCollection(selectedCollectionId);

                ImGui.Spacing();

                if (designs.Count > 0)
                {
                    // Scrollable gallery area
                    ImGui.BeginChild(
                        "##DesignGalleryScroll",
                        new Vector2(-1, -1),
                        false,
                        ImGuiWindowFlags.None
                    );
                    DrawDesignGallery(designs);
                    ImGui.EndChild();
                }
                else
                {
                    ImGui.TextColored(
                        new Vector4(0.6f, 0.6f, 0.6f, 1f),
                        "No designs in this collection."
                    );
                }
            }
            else if (collections.Count == 0)
            {
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.7f, 1f), "No collections yet");
                ImGui.TextWrapped(
                    "Click the '+' tab to create your first collection and organize your designs!"
                );
            }
        }
        catch (Exception)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Glamourer not found or not installed");
        }
    }

    private void DrawDesignGallery(
        Dictionary<
            Guid,
            (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)
        > designs
    )
    {
        const float cardWidth = 260f;
        const float cardHeight = 400f;
        const float cardSpacing = 25f;
        const float verticalGap = 25f;

        float availableWidth = ImGui.GetContentRegionAvail().X;
        int columnsPerRow = Math.Max(
            1,
            (int)((availableWidth - cardSpacing) / (cardWidth + cardSpacing))
        );

        // Calculate total width needed for one row of cards
        float totalRowWidth = (cardWidth * columnsPerRow) + (cardSpacing * (columnsPerRow - 1));
        float centerOffset = Math.Max(0, (availableWidth - totalRowWidth) / 2);

        int designIndex = 0;
        foreach (var designEntry in designs)
        {
            int columnIndex = designIndex % columnsPerRow;
            Guid designId = designEntry.Key;
            var design = designEntry.Value;

            // Center align the first column of each row
            if (columnIndex == 0)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + centerOffset);
            }

            DrawDesignCard(designId, design.DisplayName, cardWidth, cardHeight);

            if (columnIndex < columnsPerRow - 1 && designIndex < designs.Count - 1)
            {
                ImGui.SameLine(0, cardSpacing);
            }
            else if (columnIndex == columnsPerRow - 1 || designIndex == designs.Count - 1)
            {
                ImGui.Dummy(new Vector2(0, verticalGap));
            }

            designIndex++;
        }
    }

    private void DrawDesignCard(Guid designId, string glamourerName, float width, float height)
    {
        const float cornerRounding = 12f;
        const float borderThickness = 1.5f;

        // Draw card background and border with rounded corners
        Vector2 cardStartPos = ImGui.GetCursorScreenPos();
        Vector2 cardEndPos = cardStartPos + new Vector2(width, height);

        ImGui
            .GetWindowDrawList()
            .AddRectFilled(
                cardStartPos,
                cardEndPos,
                ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.12f, 0.95f)),
                cornerRounding
            );
        ImGui
            .GetWindowDrawList()
            .AddRect(
                cardStartPos,
                cardEndPos,
                ImGui.GetColorU32(new Vector4(0.4f, 0.4f, 0.45f, 0.7f)),
                cornerRounding,
                0,
                borderThickness
            );

        // Use a child region for layout
        ImGui.BeginChild(
            $"##DesignCard_{designId}",
            new Vector2(width, height),
            false,
            ImGuiWindowFlags.None
        );

        // Padding around image - small top padding
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10f);

        // Large thumbnail area (220x300px) - takes most of card space
        const float thumbWidth = 240f;
        const float thumbHeight = 300f;
        float thumbPadX = 10f;

        ImGui.SetCursorPosX(thumbPadX);
        Vector2 thumbStartPos = ImGui.GetCursorScreenPos();
        Vector2 thumbEndPos = thumbStartPos + new Vector2(thumbWidth, thumbHeight);

        // Draw thumbnail with nice styling
        ImGui
            .GetWindowDrawList()
            .AddRectFilled(
                thumbStartPos,
                thumbEndPos,
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.15f, 1f)),
                4f
            );
        ImGui
            .GetWindowDrawList()
            .AddRect(
                thumbStartPos,
                thumbEndPos,
                ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.35f, 0.4f)),
                4f,
                0,
                1f
            );

        // Try to load and display custom image or fallback to placeholder
        var metadata = designMetadataService.GetMetadata(designId);
        bool hasCustomImage = !string.IsNullOrEmpty(metadata?.CustomImagePath) && File.Exists(metadata.CustomImagePath);
        
        if (hasCustomImage && metadata?.CustomImagePath != null)
        {
            // Try to load the texture
            var wrap = plugin.TextureCache.GetOrLoadTexture(metadata.CustomImagePath)?.GetWrapOrDefault();
            
            if (wrap != null)
            {
                // Display the actual image texture
                ImGui.GetWindowDrawList().AddImage(
                    wrap.Handle,
                    thumbStartPos,
                    thumbEndPos,
                    Vector2.Zero,
                    Vector2.One,
                    ImGui.GetColorU32(Vector4.One)
                );
            }
            else
            {
                // Fallback: show placeholder with custom image indicator
                string thumbText = "✓ Custom Image";
                Vector4 thumbTextColor = new Vector4(0.6f, 0.85f, 0.6f, 0.9f);
                Vector2 textSize = ImGui.CalcTextSize(thumbText);
                Vector2 textPos = thumbStartPos + new Vector2((thumbWidth - textSize.X) / 2, (thumbHeight - textSize.Y) / 2);
                ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(thumbTextColor), thumbText);
            }
        }
        else
        {
            // No custom image - show placeholder
            string thumbText = "No Preview";
            Vector4 thumbTextColor = new Vector4(0.5f, 0.5f, 0.55f, 0.7f);
            Vector2 textSize = ImGui.CalcTextSize(thumbText);
            Vector2 textPos = thumbStartPos + new Vector2((thumbWidth - textSize.X) / 2, (thumbHeight - textSize.Y) / 2);
            ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(thumbTextColor), thumbText);
        }

        ImGui.Dummy(new Vector2(thumbWidth, thumbHeight));

        // Draw border line below image
        ImGui
            .GetWindowDrawList()
            .AddLine(
                new Vector2(cardStartPos.X, thumbEndPos.Y + 8f),
                new Vector2(cardEndPos.X, thumbEndPos.Y + 8f),
                ImGui.GetColorU32(new Vector4(0.4f, 0.4f, 0.45f, 0.6f)),
                1.5f
            );

        // Minimal spacing
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 12f);

        // Design name - centered in a box-like display
        string displayedName = designMetadataService.GetDisplayName(designId);
        string originalName = displayedName;

        const int maxNameChars = 24;

        // Truncate if too long
        if (displayedName.Length > maxNameChars)
        {
            displayedName = displayedName.Substring(0, maxNameChars - 3) + "...";
        }

        // Center align the name
        Vector2 nameSize = ImGui.CalcTextSize(displayedName);
        float nameX = Math.Max(8f, (width - nameSize.X) / 2);
        ImGui.SetCursorPosX(nameX);

        // Fancy rose-gold color for name
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.8f, 0.7f, 1f));
        ImGui.TextWrapped(displayedName);
        ImGui.PopStyleColor();

        // Tooltip with full name on hover if truncated
        if (displayedName.EndsWith("...") && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(originalName);
            ImGui.EndTooltip();
        }

        // Spacing before button
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);

        // Edit button - centered
        float buttonWidth = 85f;
        ImGui.SetCursorPosX((width - buttonWidth) / 2);

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.4f, 0.4f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.5f, 0.5f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.9f, 0.6f, 0.6f, 1f));

        if (ImGui.Button($"Edit##btn_{designId}", new Vector2(buttonWidth, 30)))
        {
            designEditorWindow?.OpenEdit(designId);
        }

        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar();

        ImGui.EndChild();
    }
}
