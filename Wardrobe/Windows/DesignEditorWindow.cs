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
                // Invalidate texture cache before clearing
                plugin.TextureCache.InvalidateTexture(customImagePath);
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
            var thumbnailsDir = Path.Combine(plugin.PluginDirectory, "thumbnails");
            
            if (!string.IsNullOrEmpty(customImagePath) && File.Exists(customImagePath))
            {
                // Keep the current image, delete all other old versions
                var oldFiles = Directory.GetFiles(thumbnailsDir, $"{editingDesignId}_*");
                foreach (var oldFile in oldFiles)
                {
                    if (oldFile != customImagePath)
                    {
                        try
                        {
                            plugin.TextureCache.InvalidateTexture(oldFile);
                            File.Delete(oldFile);
                            Plugin.Log.Information($"Cleaned up old image: {oldFile}");
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Warning(ex, $"Failed to clean up old image: {oldFile}");
                        }
                    }
                }
            }
            else
            {
                // No custom image selected - delete ALL image files for this design
                var allFiles = Directory.GetFiles(thumbnailsDir, $"{editingDesignId}*");
                foreach (var file in allFiles)
                {
                    try
                    {
                        plugin.TextureCache.InvalidateTexture(file);
                        File.Delete(file);
                        Plugin.Log.Information($"Deleted image file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Warning(ex, $"Failed to delete image file: {file}");
                    }
                }
                customImagePath = string.Empty;
            }
            
            // Clear ALL cached textures to ensure fresh load in gallery
            plugin.TextureCache.ClearAll();
            
            // Save metadata with the final image path (or empty string if cleared)
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

            var extension = Path.GetExtension(selectedPath);
            var thumbnailsDir = Path.Combine(plugin.PluginDirectory, "thumbnails");
            
            // Create thumbnails directory if it doesn't exist
            Directory.CreateDirectory(thumbnailsDir);

            // Delete any existing image files for this design (any extension)
            // This cleans up old images
            var existingFiles = Directory.GetFiles(thumbnailsDir, $"{editingDesignId}.*");
            foreach (var existingFile in existingFiles)
            {
                try
                {
                    // Invalidate from cache first
                    plugin.TextureCache.InvalidateTexture(existingFile);
                    
                    // Then delete the file
                    File.Delete(existingFile);
                    Plugin.Log.Information($"Deleted old image: {existingFile}");
                }
                catch (Exception deleteEx)
                {
                    Plugin.Log.Warning(deleteEx, $"Failed to delete old image: {existingFile}");
                }
            }

            // Use a timestamp to create a unique filename each time
            // This ensures no cached texture conflicts
            string timestamp = DateTime.Now.Ticks.ToString();
            var destinationPath = Path.Combine(thumbnailsDir, $"{editingDesignId}_{timestamp}{extension}");

            // Copy the new image with unique name
            File.Copy(selectedPath, destinationPath, overwrite: true);

            // If the source was a clipboard temp file in the thumbnails directory, delete it now
            // that we've copied it to the design-specific path
            var thumbnailsDirNorm = Path.GetFullPath(thumbnailsDir);
            var sourceDir = Path.GetFullPath(Path.GetDirectoryName(selectedPath) ?? "");
            var sourceFileName = Path.GetFileName(selectedPath);
            if (string.Equals(sourceDir, thumbnailsDirNorm, StringComparison.OrdinalIgnoreCase) &&
                sourceFileName.StartsWith("clipboard_", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    plugin.TextureCache.InvalidateTexture(selectedPath);
                    File.Delete(selectedPath);
                    Plugin.Log.Information($"Deleted clipboard temp file: {selectedPath}");
                }
                catch (Exception deleteEx)
                {
                    Plugin.Log.Warning(deleteEx, $"Failed to delete clipboard temp file: {selectedPath}");
                }
            }

            customImagePath = destinationPath;
            Plugin.Log.Information($"New image saved with unique name: {destinationPath}");
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
