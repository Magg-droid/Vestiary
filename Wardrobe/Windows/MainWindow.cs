using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;
    private readonly CollectionService collectionService;
    private CollectionEditorWindow? collectionEditorWindow;
    private Guid selectedCollectionId = Guid.Empty;

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath, CollectionService collectionService)
        : base("Wardrobe##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;
        this.collectionService = collectionService;
        this.collectionEditorWindow = null!; // Will be set after construction
    }

    public void SetCollectionEditorWindow(CollectionEditorWindow editor)
    {
        collectionEditorWindow = editor;
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
            if (selectedCollectionId != Guid.Empty && !collections.Any(c => c.Id == selectedCollectionId))
            {
                selectedCollectionId = collections.Count > 0 ? collections[0].Id : Guid.Empty;
            }

            // Draw tab bar
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

                        if (ImGui.MenuItem("Delete"))
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

            ImGui.Separator();

            // Display designs from selected collection
            if (selectedCollectionId != Guid.Empty)
            {
                var designs = collectionService.GetDesignsByCollection(selectedCollectionId);
                ImGui.Text($"Collection contains {designs.Count} designs");

                if (designs.Count > 0)
                {
                    ImGui.TextWrapped("Design list will be rendered here with thumbnails (TODO)");
                }
            }
            else if (collections.Count == 0)
            {
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "No collections created yet. Click + to create one.");
            }
        }
        catch (Exception)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Glamourer not found or not installed");
        }
    }
}
