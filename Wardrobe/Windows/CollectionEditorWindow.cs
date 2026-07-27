using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Wardrobe.Models;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public class CollectionEditorWindow : Window, IDisposable
{
    private readonly CollectionService collectionService;
    
    private string collectionName = string.Empty;
    private string folderPathsText = string.Empty; // Comma-separated or newline-separated
    private Collection? editingCollection = null;
    private bool isEditing = false;

    public CollectionEditorWindow(CollectionService collectionService)
        : base("Create/Edit Collection##CollectionEditor", ImGuiWindowFlags.None)
    {
        this.collectionService = collectionService;
        
        IsOpen = false;
    }

    /// <summary>
    /// Open the editor for creating a new collection.
    /// </summary>
    public void OpenCreate()
    {
        Reset();
        isEditing = false;
        editingCollection = null;
        IsOpen = true;
    }

    /// <summary>
    /// Open the editor for editing an existing collection.
    /// </summary>
    public void OpenEdit(Collection collection)
    {
        collectionName = collection.Name;
        folderPathsText = string.Join("\n", collection.FolderPaths);
        editingCollection = collection;
        isEditing = true;
        IsOpen = true;
    }

    public override void Draw()
    {
        if (!IsOpen)
            return;

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(500, 350), ImGuiCond.Appearing);

        ImGui.Text("Collection Name:");
        ImGui.InputText("##CollectionName", ref collectionName, 100);

        ImGui.Spacing();
        ImGui.Text("Folder Paths (one per line):");
        ImGui.InputTextMultiline("##FolderPaths", ref folderPathsText, 500, new Vector2(400, 150));

        ImGui.Spacing();
        ImGui.Separator();

        // Buttons
        float buttonWidth = 120;
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (buttonWidth * 2 + 20));

        if (ImGui.Button("Save", new Vector2(buttonWidth, 0)))
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                ImGui.OpenPopup("ErrorPopup##EmptyName");
            }
            else
            {
                var paths = folderPathsText
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();

                // Allow empty paths (for "Uncategorized" collections)
                if (isEditing && editingCollection != null)
                {
                    collectionService.UpdateCollection(editingCollection.Id, collectionName, paths);
                }
                else
                {
                    collectionService.CreateCollection(collectionName, paths);
                }
                Reset();
                IsOpen = false;
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
        {
            Reset();
            IsOpen = false;
        }

        // Error modals
        DrawErrorModals();
    }

    private void DrawErrorModals()
    {
        // Empty name error
        bool showEmptyNameError = true;
        if (ImGui.BeginPopupModal("ErrorPopup##EmptyName", ref showEmptyNameError, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Collection name cannot be empty.");
            if (ImGui.Button("OK##EmptyName", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }


    }

    private void Reset()
    {
        collectionName = string.Empty;
        folderPathsText = string.Empty;
        editingCollection = null;
        isEditing = false;
    }

    public void Dispose()
    {
    }
}
