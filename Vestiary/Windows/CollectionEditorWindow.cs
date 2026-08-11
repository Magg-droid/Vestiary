using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Vestiary.Models;
using Vestiary.Services;

namespace Vestiary.Windows;

public class CollectionEditorWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly CollectionService collectionService;
    
    private string collectionName = string.Empty;
    private string folderPathsText = string.Empty; // Comma-separated or newline-separated
    private Collection? editingCollection = null;
    private bool isEditing = false;

    public CollectionEditorWindow(Plugin plugin, CollectionService collectionService)
        : base("Create/Edit Collection##CollectionEditor", ImGuiWindowFlags.None)
    {
        this.plugin = plugin;
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
        if (!IsOpen || plugin.IsCameraActive)
            return;

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(550, 400), ImGuiCond.Appearing);

        // Header Section
        ImGui.Spacing();
        // Collection Name Section
        ImGui.AlignTextToFramePadding();
        ImGui.Text(Strings.ColNameLabel);
        ImGui.SameLine();
        ImGui.TextDisabled("*");
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextNormal);
            ImGui.Text(Strings.ColNameTooltip1);
            ImGui.Text(Strings.ColNameTooltip2);
            ImGui.Text(Strings.ColNameTooltip3);
            ImGui.PopStyleColor();
            ImGui.EndTooltip();
        }
        ImGui.InputTextWithHint("##CollectionName", Strings.ColNameHint, ref collectionName, 100);

        ImGui.Spacing();
        ImGui.Spacing();

        // Folder Paths Section
        ImGui.AlignTextToFramePadding();
        ImGui.Text(Strings.ColFoldersLabel);
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextNormal);
            ImGui.Text(Strings.ColFoldersTooltip1);
            ImGui.Text(Strings.ColFoldersTooltip2);
            ImGui.Spacing();
            ImGui.Text(Strings.ColFoldersTooltip3);
            ImGui.PopStyleColor();
            ImGui.EndTooltip();
        }

        ImGui.InputTextMultiline("##FolderPaths", ref folderPathsText, 500, new Vector2(ImGui.GetWindowWidth() - 30, 100));

        // Live design count
        try
        {
            var paths = folderPathsText
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (paths.Count > 0)
            {
                var allDesigns = plugin.GlamourerService.GetDesignList();
                int matchCount = allDesigns.Count(kvp =>
                    paths.Any(path => kvp.Value.FullPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)));
                ImGui.TextColored(ThemeManager.Current.TextSuccess, Strings.ColDesignsMatch(matchCount));
            }
            else
            {
                var allDesigns = plugin.GlamourerService.GetDesignList();
                int uncatCount = allDesigns.Count(kvp => !kvp.Value.FullPath.Contains("/"));
                ImGui.TextColored(ThemeManager.Current.TextGreyHint, Strings.ColUncategorizedHint(uncatCount));
            }
        }
        catch
        {
            // Glamourer might not be available; silently ignore
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Buttons - left aligned
        float buttonWidth = 100;

        if (ImGui.Button(Strings.Save, new Vector2(buttonWidth, 0)))
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
        if (ImGui.Button(Strings.Cancel, new Vector2(buttonWidth, 0)))
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
            ImGui.TextColored(ThemeManager.Current.TextError, Strings.ColErrorEmptyName);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            float buttonWidth = 100;
            if (ImGui.Button(Strings.ColErrorOk + "##EmptyName", new Vector2(buttonWidth, 0)))
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
