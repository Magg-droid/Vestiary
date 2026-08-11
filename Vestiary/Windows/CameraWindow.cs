using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Vestiary.Services;

namespace Vestiary.Windows;

/// <summary>
/// Camera overlay with a 4:5 movable, resizable viewfinder.
/// Hold SHIFT to temporarily hide the overlay and rotate the camera with right-click.
/// </summary>
public class CameraWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly UtilityService utility;
    private Action<string>? onImageCaptured;
    private bool isActive;

    private Vector2 framePos;
    private Vector2 frameSize;
    private bool isDragging;
    private Vector2 dragOffset;
    private int resizeCorner = -1;
    private Vector2 resizeAnchor;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    private const int VK_SHIFT = 0x10;
    private bool ShiftHeld => (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

    private const float HandleR = 14f, MinW = 120f, MinH = 150f;
    private const float Ratio = 4f / 5f, Inset = 8f;

    public CameraWindow(Plugin plugin, UtilityService utility) : base("Vestiary Camera##CameraOverlay")
    {
        this.plugin = plugin;
        this.utility = utility;
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings
                | ImGuiWindowFlags.NoBackground;
        IsOpen = false;
        RespectCloseHotkey = false;
    }

    public void Open(Action<string> callback)
    {
        onImageCaptured = callback;
        isActive = true; IsOpen = true;
        isDragging = false; resizeCorner = -1;
        var vp = ImGui.GetMainViewport();
        float h = vp.Size.Y * 0.6f;
        frameSize = new Vector2(h * Ratio, h);
        framePos = vp.Pos + (vp.Size - frameSize) / 2f;
    }

    public override void PreDraw()
    {
        // Hold Shift → hide overlay so user can rotate camera
        if (ShiftHeld)
        {
            Position = new Vector2(-9999, -9999);
            Size = new Vector2(1, 1);
            PositionCondition = ImGuiCond.Always;
            return;
        }

        var vp = ImGui.GetMainViewport();
        Position = vp.Pos;
        Size = vp.Size;
        PositionCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        if (!isActive || ShiftHeld) return;

        var io = ImGui.GetIO();
        var mouse = io.MousePos;
        var vp = ImGui.GetMainViewport();
        var dl = ImGui.GetWindowDrawList();

        // ── Corner detection ──
        static bool Near(Vector2 c, Vector2 m, float r) =>
            Math.Abs(m.X - c.X) <= r && Math.Abs(m.Y - c.Y) <= r;

        int hovered = -1;
        if (!isDragging && resizeCorner == -1)
        {
            Vector2[] crn = { framePos,
                new(framePos.X + frameSize.X, framePos.Y),
                new(framePos.X, framePos.Y + frameSize.Y), framePos + frameSize };
            for (int i = 0; i < 4; i++)
                if (Near(crn[i], mouse, HandleR)) { hovered = i; break; }
        }

        // ── Drag / resize ──
        if (resizeCorner == -1 && !isDragging && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            if (hovered >= 0) { resizeCorner = hovered; resizeAnchor = Opposite(hovered); }
            else if (InDragZone(mouse)) { isDragging = true; dragOffset = framePos - mouse; }
        }
        if (resizeCorner >= 0)
        {
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left)) ResizeFrom(resizeAnchor, mouse, vp);
            else resizeCorner = -1;
        }
        if (isDragging)
        {
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left)) framePos = mouse + dragOffset;
            else isDragging = false;
        }

        // Clamp
        framePos.X = Math.Clamp(framePos.X, vp.Pos.X + 10, vp.Pos.X + vp.Size.X - frameSize.X - 10);
        framePos.Y = Math.Clamp(framePos.Y, vp.Pos.Y + 35, Math.Max(vp.Pos.Y + 35, vp.Pos.Y + vp.Size.Y - frameSize.Y - 80));

        // Cursor
        if (resizeCorner >= 0 || hovered >= 0 || isDragging || InDragZone(mouse))
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

        // ═══════════ VIGNETTE ═══════════
        uint vig = ImGui.GetColorU32(ThemeManager.Current.CameraVignette);
        dl.AddRectFilled(vp.Pos, new(vp.Pos.X + vp.Size.X, framePos.Y), vig);
        dl.AddRectFilled(new(vp.Pos.X, framePos.Y + frameSize.Y), new(vp.Pos.X + vp.Size.X, vp.Pos.Y + vp.Size.Y), vig);
        dl.AddRectFilled(new(vp.Pos.X, framePos.Y), new(framePos.X, framePos.Y + frameSize.Y), vig);
        dl.AddRectFilled(new(framePos.X + frameSize.X, framePos.Y), new(vp.Pos.X + vp.Size.X, framePos.Y + frameSize.Y), vig);

        // ═══════════ FRAME ═══════════
        dl.AddRect(framePos, framePos + frameSize,
            ImGui.GetColorU32(ThemeManager.Current.CameraBorder), 0f, 0, 1.5f);

        Vector2 ip = framePos + new Vector2(Inset);
        dl.AddRect(ip, framePos + frameSize - new Vector2(Inset),
            ImGui.GetColorU32(ThemeManager.Current.CameraGrid), 2f, 0, 1f);

        // Corner dots
        bool hov = hovered >= 0 || resizeCorner >= 0;
        uint dCol = ImGui.GetColorU32(hov
            ? ThemeManager.Current.CameraTextHov : ThemeManager.Current.CameraText);
        float dR = hov ? 6f : 5f;
        void D(Vector2 c) => dl.AddCircleFilled(c, dR, dCol, 8);
        D(framePos); D(new(framePos.X + frameSize.X, framePos.Y));
        D(new(framePos.X, framePos.Y + frameSize.Y)); D(framePos + frameSize);

        // ── Hint text (single line, centered) ──
        uint hintCol = ImGui.GetColorU32(ThemeManager.Current.CameraText);

        string t;
        if (resizeCorner >= 0)
            t = Strings.CameraDimensions(frameSize.X - Inset * 2, frameSize.Y - Inset * 2);
        else if (isDragging)
            t = Strings.CameraReleaseToPlace;
        else
            t = Strings.CameraHint;

        var ts = ImGui.CalcTextSize(t);
        dl.AddText(new(framePos.X + (frameSize.X - ts.X) / 2f, framePos.Y - 22f), hintCol, t);

        // ═══════════ BUTTONS ═══════════
        const float bw = 130f, bh = 36f, gap = 20f;
        float totalW = bw * 2 + gap;
        float btnX = framePos.X + (frameSize.X - totalW) / 2f;
        float btnY = framePos.Y + frameSize.Y + 15f;
        ImGui.SetCursorScreenPos(new Vector2(btnX, btnY));

        ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.CamCaptureBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.CamCaptureHov);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.CamCaptureAct);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6f);
        if (ImGui.Button(Strings.CameraCapture, new Vector2(bw, bh))) Capture();
        ImGui.PopStyleVar(); ImGui.PopStyleColor(3);

        ImGui.SameLine(0, gap);
        ImGui.PushStyleColor(ImGuiCol.Button, ThemeManager.Current.CamCancelBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeManager.Current.CamCancelHov);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeManager.Current.CamCancelAct);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6f);
        if (ImGui.Button(Strings.CameraCancel, new Vector2(bw, bh))) Close();
        ImGui.PopStyleVar(); ImGui.PopStyleColor(3);

        // Keyboard
        if (ImGui.IsKeyPressed(ImGuiKey.Escape)) Close();
        if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)) Capture();
        float nudge = io.KeyShift ? 10f : 1f;
        if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow)) framePos.X -= nudge;
        if (ImGui.IsKeyPressed(ImGuiKey.RightArrow)) framePos.X += nudge;
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) framePos.Y -= nudge;
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) framePos.Y += nudge;
    }

    bool InDragZone(Vector2 m) => m.X >= framePos.X - 10 && m.X <= framePos.X + frameSize.X + 10
        && m.Y >= framePos.Y - 10 && m.Y <= framePos.Y + frameSize.Y + 10;

    Vector2 Opposite(int i) => i switch
    {
        0 => framePos + frameSize, 1 => new(framePos.X, framePos.Y + frameSize.Y),
        2 => new(framePos.X + frameSize.X, framePos.Y), _ => framePos
    };

    void ResizeFrom(Vector2 anchor, Vector2 mouse, ImGuiViewportPtr vp)
    {
        float rw = Math.Abs(mouse.X - anchor.X), rh = Math.Abs(mouse.Y - anchor.Y);
        float nw, nh;
        if (rw / rh > Ratio) { nh = rh; nw = nh * Ratio; }
        else { nw = rw; nh = nw / Ratio; }
        nw = Math.Clamp(nw, MinW, vp.Size.X - 20f);
        nh = Math.Clamp(nh, MinH, vp.Size.Y - 115f);
        if (nw / nh > Ratio) nh = nw / Ratio; else nw = nh * Ratio;
        // Re-clamp: ratio adjustment can push dimensions past bounds
        nw = Math.Clamp(nw, MinW, vp.Size.X - 20f);
        nh = Math.Clamp(nh, MinH, vp.Size.Y - 115f);
        framePos = new Vector2(mouse.X > anchor.X ? anchor.X : anchor.X - nw,
                               mouse.Y > anchor.Y ? anchor.Y : anchor.Y - nh);
        frameSize = new Vector2(nw, nh);
    }

    void Capture()
    {
        try
        {
            int sx = (int)(framePos.X + Inset), sy = (int)(framePos.Y + Inset);
            int sw = (int)(frameSize.X - Inset * 2), sh = (int)(frameSize.Y - Inset * 2);
            using var bmp = new Bitmap(sw, sh);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(sx, sy, 0, 0, new Size(sw, sh), CopyPixelOperation.SourceCopy);
            var dir = utility.ThumbnailsDirectory;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"camera_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            bmp.Save(path, ImageFormat.Png);
            var cb = onImageCaptured; Close(); cb?.Invoke(path);
        }
        catch (Exception ex) { Plugin.Log.Error(ex, "Capture failed"); }
    }

    void Close()
    {
        isActive = false; IsOpen = false; onImageCaptured = null;
        isDragging = false; resizeCorner = -1;
        plugin.OnCameraClosed();
    }

    public void Dispose() { }
}
