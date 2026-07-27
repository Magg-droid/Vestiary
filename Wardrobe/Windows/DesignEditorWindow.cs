using System;
using System.Numerics;
using System.IO;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public class DesignEditorWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly DesignMetadataService designMetadataService;
    private readonly GlamourerService glamourerService;

    private Guid editingDesignId = Guid.Empty;
    private string designName = string.Empty;
    private string nickname = string.Empty;
    private string customImagePath = string.Empty;

    public DesignEditorWindow(Plugin plugin, DesignMetadataService designMetadataService, GlamourerService glamourerService)
        : base("Edit Design Metadata##DesignEditor", ImGuiWindowFlags.None)
    {
        this.plugin = plugin;
        this.designMetadataService = designMetadataService;
        this.glamourerService = glamourerService;

        IsOpen = false;
    }

    /// <summary>
    /// Open the editor for a design.
    /// </summary>
    public void OpenEdit(Guid designId)
    {
        editingDesignId = designId;

        // Load design name from Glamourer
        var designs = glamourerService.GetDesignList();
        if (designs.TryGetValue(designId, out var design))
        {
            designName = design.DisplayName;
        }
        else
        {
            designName = "Unknown Design";
        }

        // Load existing metadata
        var metadata = designMetadataService.GetMetadata(designId);
        nickname = metadata?.Nickname ?? string.Empty;
        customImagePath = metadata?.CustomImagePath ?? string.Empty;

        IsOpen = true;
    }

    public override void Draw()
    {
        if (!IsOpen)
            return;

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(500, 350), ImGuiCond.Appearing);

        // Header Section
        ImGui.Spacing();

        // Title - left aligned
        ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.7f, 1f), "Edit Design Metadata");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Design Name (Read-only)
        ImGui.Text("Design Name:");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), designName);

        ImGui.Spacing();
        ImGui.Spacing();

        // Nickname Section
        ImGui.Text("Nickname:");
        ImGui.SameLine();
        ImGui.TextDisabled("(Optional)");
        ImGui.InputTextWithHint("##Nickname", "e.g., My Casual Look", ref nickname, 100);

        ImGui.Spacing();
        ImGui.TextWrapped("Leave empty to display the original design name from Glamourer.");

        ImGui.Spacing();
        ImGui.Spacing();

        // Custom Image Upload section
        ImGui.Text("Custom Image:");
        ImGui.SameLine();
        ImGui.TextDisabled("(Optional)");

        ImGui.Spacing();
        if (ImGui.Button("Choose Image", new Vector2(150, 0)))
        {
            plugin.OpenImageFilePicker(OnImageSelected);
        }

        ImGui.SameLine();
        if (ImGui.Button("From Clipboard", new Vector2(160, 0)))
        {
            plugin.CopyImageFromClipboard(OnImageSelected);
        }

        ImGui.SameLine();
        if (!string.IsNullOrEmpty(customImagePath))
        {
            if (ImGui.Button("Clear Image", new Vector2(120, 0)))
            {
                customImagePath = string.Empty;
            }
        }
        
        if (!string.IsNullOrEmpty(customImagePath))
        {
            ImGui.Spacing();
            ImGui.TextWrapped($"Selected: {Path.GetFileName(customImagePath)}");
            
            // Show image preview if available
            ImGui.Spacing();
            ImGui.Spacing();
            var texture = plugin.TextureCache.GetOrLoadTexture(customImagePath)?.GetWrapOrDefault();
            if (texture != null)
            {
                float previewSize = 120f;
                ImGui.Image(texture.Handle, new Vector2(previewSize, previewSize));
            }
            else
            {
                ImGui.TextDisabled("Image preview not available");
            }
        }
        else
        {
            ImGui.Spacing();
            ImGui.TextDisabled("No image selected");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Buttons - left aligned
        if (ImGui.Button("Save", new Vector2(100, 0)))
        {
            designMetadataService.UpsertMetadata(editingDesignId, nickname, customImagePath);
            Reset();
            IsOpen = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(100, 0)))
        {
            Reset();
            IsOpen = false;
        }
    }

    private void Reset()
    {
        editingDesignId = Guid.Empty;
        designName = string.Empty;
        nickname = string.Empty;
        customImagePath = string.Empty;
    }

    private void OnImageSelected(string selectedPath)
    {
        try
        {
            if (!File.Exists(selectedPath))
                return;

            // Copy to thumbnails folder with design ID as filename
            var extension = Path.GetExtension(selectedPath);
            var thumbnailsDir = Path.Combine(plugin.PluginDirectory, "thumbnails");
            var destinationPath = Path.Combine(thumbnailsDir, $"{editingDesignId}{extension}");

            // Create thumbnails directory if it doesn't exist
            Directory.CreateDirectory(thumbnailsDir);

            // Overwrite if already exists
            File.Copy(selectedPath, destinationPath, overwrite: true);

            customImagePath = destinationPath;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, $"Failed to copy image file: {selectedPath}");
        }
    }

    public void Dispose()
    {
    }
}
