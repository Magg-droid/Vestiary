using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public class DesignEditorWindow : Window, IDisposable
{
    private readonly DesignMetadataService designMetadataService;
    private readonly GlamourerService glamourerService;

    private Guid editingDesignId = Guid.Empty;
    private string designName = string.Empty;
    private string nickname = string.Empty;

    public DesignEditorWindow(DesignMetadataService designMetadataService, GlamourerService glamourerService)
        : base("Edit Design Metadata##DesignEditor", ImGuiWindowFlags.None)
    {
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

        // Load existing nickname from metadata
        var metadata = designMetadataService.GetMetadata(designId);
        nickname = metadata?.Nickname ?? string.Empty;

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

        // TODO: Custom Image Upload section (placeholder for now)
        ImGui.TextDisabled("Custom Image: (TODO - Not yet implemented)");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Buttons - left aligned
        if (ImGui.Button("Save", new Vector2(100, 0)))
        {
            designMetadataService.UpsertMetadata(editingDesignId, nickname);
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
    }

    public void Dispose()
    {
    }
}
