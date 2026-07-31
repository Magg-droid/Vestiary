using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Wardrobe.Services;

namespace Wardrobe.Windows;

public class GuideWindow : Window, IDisposable
{
    private readonly TextureCache textureCache;
    private readonly string cameraIconPath;
    private readonly string uploadIconPath;
    private readonly string clipboardIconPath;
    private readonly string starFilledPath;
    private readonly string saveModsIconPath;

    private static readonly (string Title, string? Body)[] Sections =
    [
        ("Getting Started",
            "Open Wardrobe with /wardrobe or /wr. Your Glamourer designs will appear as visual cards.\n\n" +
            "Double-click a card to apply the outfit.\n" +
            "Or click the Apply button.\n" +
            "Hold Ctrl while applying to change equipment only.\n\n" +
            "Type /wrguide to open this guide at any time."),

        ("Collections",
            "Collections let you group your designs into separate tabs.\n\n" +
            "Create a collection — click the + tab and choose which Glamourer folders to include.\n" +
            "Edit a collection — right-click the tab.\n" +
            "Reorder collections — drag the tabs.\n" +
            "Delete a collection — right-click the tab and choose Delete.\n\n" +
            "A collection with no folders will show uncategorized designs."),

        ("Thumbnails", null),

        ("Search & Rename",
            "Use the search bar to find designs in the current collection.\n\n" +
            "Search works with both your nickname and the original Glamourer name.\n\n" +
            "To rename a design, double-click its name and enter a nickname. Clear the nickname to restore the original name."),

        ("Favorites", null),

        ("Hide & Delete",
            "Hide removes a design from the gallery without deleting it.\n" +
            "Unhide restores a hidden design.\n\n" +
            "Use the eye button in the header to show or hide hidden designs.\n\n" +
            "To permanently delete a design from Glamourer, hold Ctrl, click Delete, and confirm."),

        ("Save Mods", null),

        ("Settings",
            "Apply Equipment Only — apply equipment without changing your character's appearance. Hold Ctrl while applying to temporarily switch modes.\n\n" +
            "Show Hidden — display the hidden designs tab.\n\n" +
            "Enable Save Mods — save and restore your Penumbra mod settings when applying designs.\n\n" +
            "Theme — choose between Classic, Ocean, Midnight Purple, and Forest. The theme changes immediately."),

        ("Need help?",
            "Found a bug or have a suggestion? Contact me on Discord."),
    ];

    private int _selectedIndex;
    private const float SidebarWidth = 190f;

    public GuideWindow(
        TextureCache textureCache,
        string cameraIconPath, string uploadIconPath, string clipboardIconPath,
        string starFilledPath, string saveModsIconPath)
        : base("Wardrobe Guide##WRGuide", ImGuiWindowFlags.NoScrollbar)
    {
        this.textureCache = textureCache;
        this.cameraIconPath = cameraIconPath;
        this.uploadIconPath = uploadIconPath;
        this.clipboardIconPath = clipboardIconPath;
        this.starFilledPath = starFilledPath;
        this.saveModsIconPath = saveModsIconPath;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(540, 360),
            MaximumSize = new Vector2(850, 2000),
        };
        IsOpen = false;
    }

    public override void Draw()
    {
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(640, 440), ImGuiCond.Appearing);

        ImGui.Spacing();

        ImGui.BeginChild("##GuideSidebar", new Vector2(SidebarWidth, 0), true);

        for (int i = 0; i < Sections.Length; i++)
        {
            bool isSelected = i == _selectedIndex;
            bool pushed = false;

            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ThemeManager.Current.TextHeading);
                ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.TextSubtle);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.TextSubtle);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.TextSubtle);
                pushed = true;
            }

            if (ImGui.Button($"{Sections[i].Title}##nav_{i}", new Vector2(SidebarWidth - 16, 0)))
                _selectedIndex = i;

            if (pushed)
                ImGui.PopStyleColor(4);

            ImGui.Spacing();
        }

        ImGui.EndChild();
        ImGui.SameLine();

        ImGui.BeginChild("##GuideContent", Vector2.Zero, false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);

        var (title, body) = Sections[_selectedIndex];

        ImGui.TextColored(ThemeManager.Current.TextHeading, title);
        ImGui.Spacing();

        var dl = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        dl.AddLine(new Vector2(cursor.X, cursor.Y + 2), new Vector2(cursor.X + avail, cursor.Y + 2),
            ImGui.GetColorU32(ThemeManager.Current.SeparatorColor), 1f);
        ImGui.Spacing();
        ImGui.Spacing();

        if (_selectedIndex == 2) // Thumbnails — render with icons
            DrawThumbnailsSection();
        else if (_selectedIndex == 4) // Favorites — render with icon
            DrawFavoritesSection();
        else if (_selectedIndex == 6) // Save Mods — render with icons
            DrawSaveModsSection();
        else if (body != null)
        {
            ImGui.PushTextWrapPos();
            ImGui.TextWrapped(body);
            ImGui.PopTextWrapPos();
        }

        ImGui.EndChild();
    }

    private void DrawThumbnailsSection()
    {
        float iconS = 24f;
        ImGui.PushTextWrapPos();

        DrawIconLine(cameraIconPath, iconS, "Camera — opens a 4:5 overlay with a movable viewfinder");
        ImGui.Spacing();
        DrawIconLine(uploadIconPath, iconS, "Upload — pick an image file from your disk");
        ImGui.Spacing();
        DrawIconLine(clipboardIconPath, iconS, "Clipboard — paste directly (Win+Shift+S works great)");
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextWrapped("Thumbnails are saved locally and persist across updates.");
        ImGui.PopTextWrapPos();
    }

    private void DrawFavoritesSection()
    {
        float iconS = 24f;
        ImGui.PushTextWrapPos();
        DrawIconLine(starFilledPath, iconS, "Click the star icon on any design card to favorite it.", 3f);
        ImGui.Spacing();
        ImGui.TextWrapped("A Favorites tab appears automatically when you favorite something, and disappears when empty.");
        ImGui.PopTextWrapPos();
    }

    private void DrawSaveModsSection()
    {
        float iconS = 24f;
        ImGui.PushTextWrapPos();

        ImGui.TextWrapped("Your outfits remember their Penumbra mod setup and restore it automatically when you apply.");
        ImGui.Spacing();
        ImGui.TextWrapped("Setup: Settings \u2192 Enable Save Mods");
        ImGui.Spacing();
        ImGui.Spacing();

        DrawIconLine(saveModsIconPath, iconS, "How it works:", 3f);
        ImGui.Spacing();
        ImGui.TextWrapped("  1. Open Penumbra, set up your mods for the outfit");
        ImGui.TextWrapped("  2. Click the floppy disk icon on the card (top-left, below the star)");
        ImGui.TextWrapped("  3. Icon turns gold — mods are saved");
        ImGui.TextWrapped("  4. Apply the outfit — mods restore automatically");
        ImGui.Spacing();

        ImGui.TextWrapped("Left-click = save  |  Right-click = clear  |  Gold = saved  |  Dim = not saved");
        ImGui.Spacing();
        ImGui.TextWrapped("New mods get turned OFF automatically. Only equipment mods are captured.");

        ImGui.PopTextWrapPos();
    }

    private void DrawIconLine(string iconPath, float iconSize, string text, float yOffset = 0f)
    {
        const float baseOffsetY = -9f;
        var tex = textureCache.GetOrLoadTexture(iconPath)?.GetWrapOrDefault();
        if (tex != null)
        {
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddImage(tex.Handle,
                new Vector2(pos.X, pos.Y + baseOffsetY + yOffset),
                new Vector2(pos.X + iconSize, pos.Y + baseOffsetY + yOffset + iconSize),
                Vector2.Zero, Vector2.One,
                ImGui.GetColorU32(ThemeManager.Current.IconDefault));
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + iconSize + 8f);
        }
        ImGui.TextWrapped(text);
    }

    public void Dispose() { }
}
