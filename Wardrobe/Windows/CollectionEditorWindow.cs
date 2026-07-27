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
        ImGui.SetNextWindowSize(new Vector2(550, 400), ImGuiCond.Appearing);

        // Header Section
        ImGui.Spacing();
        ImGui.Spacing();
        
        // Title - centered style
        string windowTitle = isEditing ? "Edit Collection" : "Create New Collection";
        float titleWidth = ImGui.CalcTextSize(windowTitle).X;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - titleWidth) / 2);
        ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.7f, 1f), windowTitle);
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Collection Name Section
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Collection Name:");
        ImGui.SameLine();
        ImGui.TextDisabled("*");
        ImGui.InputTextWithHint("##CollectionName", "e.g., Dresses, Casual, Formal", ref collectionName, 100);

        ImGui.Spacing();
        ImGui.Spacing();

        // Folder Paths Section
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Folder Paths:");
        ImGui.TextWrapped("(Optional) Enter paths one per line. Leave empty for uncategorized designs.");
        ImGui.InputTextMultiline("##FolderPaths", ref folderPathsText, 500, new Vector2(ImGui.GetWindowWidth() - 30, 120));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Buttons - centered
        float buttonWidth = 100;
        float buttonSpacing = 10;
        float totalButtonWidth = (buttonWidth * 2) + buttonSpacing;
        float buttonPosX = (ImGui.GetWindowWidth() - totalButtonWidth) / 2;

        ImGui.SetCursorPosX(buttonPosX);

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
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "⚠ Collection name is required");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            float buttonWidth = 100;
            float buttonPosX = (ImGui.GetWindowWidth() - buttonWidth) / 2;
            ImGui.SetCursorPosX(buttonPosX);
            
            if (ImGui.Button("OK##EmptyName", new Vector2(buttonWidth, 0)))
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
