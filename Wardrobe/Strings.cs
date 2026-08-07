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
    public const string Settings = "Settings";
    public const string Yes    = "Yes";
    public const string No     = "No";

    // ── Main window · Browse rail ──────────────────
    public const string RailGlamour  = "Glamour";
    public const string RailEmotes   = "Emotes";
    public const string RailHelp     = "Help";
    public const string RailMinimize = "Minimize";
    public const string RailExpand   = "Expand";
    public const string BrowseHeading = "Browse";

    // ── Main window · Emote cards ──────────────────
    public const string EmotePlaceholder      = "Emote";
    public const string EmoteDefaultName      = "Emote Name";
    public const string EmoteNoneSelected     = "No emote selected";
    public const string EmoteDefaultCollectionName = "Emotes";
    public const string EmotePickHint         = "Pick emote...";
    public const string EmoteDoubleClickTip   = "Double-click to restore mods";
    public const string NoEmoteCards          = "No emote cards yet.";
    public const string NoEmoteSearchResults  = "No matching emote cards.";
    public const string ThumbNoPreviewLine1   = "No";
    public const string ThumbNoPreviewLine2   = "Preview";
    public const string CreateEmoteCardLabel  = "Create Emote Card";
    public const string EmoteCollectionNameHint = "Collection name...";
    public const string EmoteCollectionCreateButton = "Create Collection";
    public const string EmoteMoveToCollection = "Move to collection";

    public static string EmoteCardCount(int count) =>
        $"{count} cards";

    // ── Main window · Tab bar ───────────────────────
    public const string SearchHint        = "Search...";
    public const string SearchResultsChip = "Search Results";
    public const string RandomButton      = "Random Pick";
    public const string ShowHiddenLabel   = "Show hidden";
    public const string TabRightClickTooltip = "Right-click for options";
    public const string TooltipRandomButton = "Apply a random visible design from this collection";
    public const string TooltipRandomButtonDisabled = "No visible designs available in this collection";
    public const string CommandHelpOpenPlugin = "Open the Wardrobe plugin";
    public const string CommandHelpOpenPluginShortcut = "Open the Wardrobe plugin (shortcut)";
    public const string CommandHelpOpenGuide = "Open the Wardrobe user guide";
    public const string CommandHelpOpenEmotes = "Open Wardrobe in Emotes view";
    public const string CommandHelpRandom = "Apply a random visible design. Usage: /wrrandom [Collection Name]";
    public const string RandomCommandNoCollections = "No collections are available.";
    public const string RandomCommandNoNonFavoriteCollections = "No non-favorites collections are available.";
    public static string RandomCommandCollectionNotFound(string collectionName) =>
        $"Collection '{collectionName}' was not found.";
    public static string RandomCommandDuplicateCollection(string collectionName) =>
        $"Multiple collections named '{collectionName}' found. Please rename one and try again.";
    public static string RandomCommandNoVisibleDesigns(string sourceLabel) =>
        $"No visible designs found in {sourceLabel}.";
    public const string TooltipSettings     = "Open Settings";
    public const string TooltipEyeShowHidden  = "Show hidden designs";
    public const string TooltipEyeShowVisible = "Show visible designs";
    public const string SettingsEnableSaveMods = "Enable Save Mods";
    public const string SettingsEnableSaveModsTooltip = "Save and restore Penumbra mod state per outfit";
    public const string SettingsEnableEmotes = "Enable Emotes (Beta)";
    public const string SettingsEnableEmotesTooltip = "Experimental emote gallery with mod capture and auto-play";
    public const string TooltipSaveModsSave   = "Save mods for this outfit";
    public const string TooltipSaveModsReSave = "Mods saved — click to re-save";
    public const string TooltipSaveModsClear  = "Right-click to clear saved mods";

    // ── Main window · Empty state ───────────────────
    public const string EmptyHeading       = "No collections yet";
    public const string EmptyDescription   = "Collections let you organize your Glamourer designs into groups.";
    public const string EmptyCtaButton     = "+  Create Your First Collection";
    public const string EmptyGuideButton   = "View Guide";
    public const string EmptyHint          = "You can also use the + button above the tabs.";

    // ── Main window · Gallery ───────────────────────
    public const string NoDesigns           = "No designs in this collection.";
    public const string NoSearchResults     = "No matching designs across collections.";
    public const string GlamourerNotFound   = "Glamourer not found or not installed";

    public static string DesignCount(int count) =>
        $"{count} designs";

    public static string DesignCountWithHidden(int visible, int hidden) =>
        $"{visible} designs ({hidden} hidden)";

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
    public const string TooltipRename        = "Double-click to rename";
    public const string TooltipFavAdd        = "Add to favourites";
    public const string TooltipFavRemove     = "Remove from favourites";
    public const string ConfirmDeleteTitle = "Are you sure you want to delete";
    public const string ConfirmDeleteBody  = "this design from Glamourer?";

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

    // ── Settings window ─────────────────────────────
    public const string SettingsApplyEquipmentOnly = "Apply Equipment Only";
    public const string SettingsApplyEquipmentTooltip = "When enabled, design apply will only change gear, not character appearance";
    public const string SettingsShowHidden = "Show Hidden";
    public const string SettingsShowHiddenTooltip = "Show hidden designs instead of visible ones";
    public const string SettingsThemeHeading = "Theme";
    public const string SettingsThemeClassic = "Classic";
    public const string SettingsThemeOcean   = "Ocean";
    public const string SettingsThemePurple  = "Midnight Purple";
    public const string SettingsThemeForest  = "Forest";

    // ── Camera window ───────────────────────────────
    public const string CameraCapture       = "Capture";
    public const string CameraCancel        = "Cancel";
    public const string CameraReleaseToPlace = "Release to place";
    public const string CameraHint          = "Drag to move  ·  Corners to resize  ·  Hold Shift+right click to rotate";

    public static string CameraDimensions(float w, float h) =>
        $"{w:F0} × {h:F0}";
}
