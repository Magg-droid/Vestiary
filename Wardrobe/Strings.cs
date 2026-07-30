namespace Wardrobe;

/// <summary>
/// All user-facing strings. Change here for localization or rewording.
/// For parametrized strings, use methods (e.g., <c>Strings.DesignCount(12)</c>).
/// </summary>
public static class Strings
{
    // ── Generic ─────────────────────────────────────
    public const string Save   = "Save";
    public const string Cancel = "Cancel";
    public const string Edit   = "Edit";
    public const string Delete = "Delete";

    // ── Main window · Tab bar ───────────────────────
    public const string TabRightClickTooltip = "Right-click for options";

    // ── Main window · Empty state ───────────────────
    public const string EmptyHeading       = "No collections yet";
    public const string EmptyDescription   = "Collections let you organize your Glamourer designs into groups.";
    public const string EmptyCtaButton     = "+  Create Your First Collection";
    public const string EmptyHint          = "You can also use the + button above the tabs.";

    // ── Main window · Gallery ───────────────────────
    public const string NoDesigns           = "No designs in this collection.";
    public const string GlamourerNotFound   = "Glamourer not found or not installed";

    // ── Main window · Design card ───────────────────
    public const string CardApply          = "Apply";
    public const string CardEdit           = "Edit";
    public const string CardDelete         = "Delete";
    public const string CardHide           = "Hide";
    public const string CardUnhide         = "Unhide";
    public const string TooltipApply       = "Apply this design";
    public const string TooltipApplyCtrl   = "Ctrl+Click: Equipment only";
    public const string TooltipEdit        = "Edit configuration";
    public const string TooltipHide        = "Hide this design from the gallery";
    public const string TooltipUnhide       = "Show this design in the gallery again";
    public const string TooltipDelete      = "Delete the design from Glamourer";
    public const string TooltipDeleteCtrl  = "Ctrl+Click to confirm";
    public const string TooltipCamera      = "Take snapshot";
    public const string TooltipUpload      = "Upload from file";
    public const string TooltipClipboard   = "Paste from clipboard";
    public const string TooltipThumbnail   = "Double-click to apply";
    public const string TooltipFavAdd      = "Add to favourites";
    public const string TooltipFavRemove   = "Remove from favourites";

    // ── Collection editor window ────────────────────
    public const string ColCreateTitle     = "Create New Collection";
    public const string ColEditTitle       = "Edit Collection";
    public const string ColNameLabel       = "Collection Name:";
    public const string ColNameHint        = "e.g., Dresses, Casual, Formal";
    public const string ColNameTooltip1    = "A collection is just a way to browse and organize";
    public const string ColNameTooltip2    = "your Glamourer designs. It does not modify or affect";
    public const string ColNameTooltip3    = "Glamourer in any way.";
    public const string ColFoldersLabel    = "Glamourer Folders:";
    public const string ColFoldersTooltip1 = "These are folder paths from Glamourer's design list.";
    public const string ColFoldersTooltip2 = "Only designs under these folders appear in this collection.";
    public const string ColFoldersTooltip3 = "Leave empty to include uncategorized designs instead.";
    public const string ColErrorEmptyName  = "⚠ Collection name is required";
    public const string ColErrorOk         = "OK";

    public static string ColDesignsMatch(int count) =>
        $"✓ {count} design(s) match these folders";

    public static string ColUncategorizedHint(int count) =>
        $"No folders selected — {count} uncategorized design(s) would be included";

    // ── Design editor window ────────────────────────
    public const string DesignEditTitle       = "Edit Design Metadata";
    public const string DesignNameLabel       = "Design Name:";
    public const string DesignNicknameLabel   = "Nickname:";
    public const string DesignNicknameHint    = "e.g., My Casual Look";
    public const string DesignNicknameEmpty   = "Leave empty to display the original design name from Glamourer.";
    public const string DesignImageLabel      = "Custom Image:";
    public const string DesignChooseImage     = "Choose Image";
    public const string DesignFromClipboard   = "From Clipboard";
    public const string DesignCamera          = "Camera";
    public const string DesignClearImage      = "Clear Image";
    public const string DesignImagePreviewNo  = "Image preview not available";
    public const string DesignNoImage         = "No image selected";
    public const string DesignSelectedPrefix  = "Selected: ";   // followed by filename

    // ── Camera window ───────────────────────────────
    public const string CameraCapture       = "Capture";
    public const string CameraCancel        = "Cancel";
    public const string CameraReleaseToPlace = "Release to place";
    public const string CameraHint          = "Drag to move  ·  Corners to resize  ·  Hold Shift+right click to rotate";

    public static string CameraDimensions(float w, float h) =>
        $"{w:F0} × {h:F0}";
}
