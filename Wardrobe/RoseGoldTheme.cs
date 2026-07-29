using System.Numerics;

namespace Wardrobe;

/// <summary>
/// Rose Gold — the default dark theme with warm rose-gold accents.
/// To add a new theme, create a copy of this class and toggle in Configuration.
/// </summary>
public static class RoseGoldTheme
{
    // ── Text ────────────────────────────────────────
    public static readonly Vector4 TextHeading    = new(0.9f,  0.8f,  0.7f,  1f);    // rose-gold headings
    public static readonly Vector4 TextNormal     = new(0.7f,  0.7f,  0.7f,  0.9f);  // tab labels, body
    public static readonly Vector4 TextMuted      = new(0.6f,  0.6f,  0.6f,  1f);    // descriptions
    public static readonly Vector4 TextSubtle     = new(0.4f,  0.4f,  0.4f,  0.7f);  // hint text
    public static readonly Vector4 TextError      = new(1f,    0.3f,  0.3f,  1f);    // error messages
    public static readonly Vector4 TextSuccess    = new(0.5f,  0.8f,  0.5f,  0.9f);  // success / match count
    public static readonly Vector4 TextGreyHint   = new(0.5f,  0.5f,  0.5f,  0.7f);  // empty-state hints

    // ── Tab bar ─────────────────────────────────────
    public static readonly Vector4 TabSelected    = new(0.20f, 0.28f, 0.38f, 1f);
    public static readonly Vector4 TabHovered     = new(0.18f, 0.18f, 0.24f, 0.8f);
    public static readonly Vector4 TabDefault     = new(0.12f, 0.12f, 0.18f, 0.6f);
    public static readonly Vector4 TabBorderLine  = new(0.3f,  0.3f,  0.38f, 0.8f);
    public static readonly Vector4 TabTextActive  = new(0.95f, 0.85f, 0.75f, 1f);
    public static readonly Vector4 TabTextIdle    = new(0.7f,  0.7f,  0.7f,  0.9f);
    public static readonly Vector4 TabPlusIcon    = new(0.9f,  0.9f,  0.9f,  1f);

    // ── "+" (new collection) button on tab bar ──────
    public static readonly Vector4 PlusBtn        = new(0.2f,  0.5f,  0.2f,  1f);
    public static readonly Vector4 PlusBtnInactive = new(0.15f, 0.35f, 0.15f, 0.6f);

    // ── Design counts / info ────────────────────────
    public static readonly Vector4 CountText      = new(0.8f,  0.75f, 0.7f,  0.8f);

    // ── Design cards ────────────────────────────────
    public static readonly Vector4 CardBg         = new(0.12f, 0.12f, 0.16f, 0.95f);
    public static readonly Vector4 CardBgHovered  = new(0.08f, 0.08f, 0.12f, 0.95f);
    public static readonly Vector4 CardBorder     = new(0.55f, 0.5f,  0.48f, 0.9f);
    public static readonly Vector4 CardBorderIdle = new(0.4f,  0.4f,  0.45f, 0.7f);
    public static readonly Vector4 CardLine       = new(0.4f,  0.4f,  0.45f, 0.6f);

    // ── Thumbnail area ──────────────────────────────
    public static readonly Vector4 ThumbBg        = new(0.1f,  0.1f,  0.15f, 1f);
    public static readonly Vector4 ThumbBorder    = new(0.3f,  0.3f,  0.35f, 0.4f);
    public static readonly Vector4 ThumbCustomImg = new(0.6f,  0.85f, 0.6f,  0.9f);  // "custom image" placeholder
    public static readonly Vector4 ThumbNoPreview = new(0.5f,  0.5f,  0.55f, 0.7f);  // "no preview" placeholder

    // ── Thumbnail action icons (camera / upload / clipboard) ──
    public static readonly Vector4 IconDefault    = new(1f,    1f,    1f,    0.6f);
    public static readonly Vector4 IconHovered    = new(1f,    1f,    1f,    0.95f);

    // ── Apply button (blue) ─────────────────────────
    public static readonly Vector4 ApplyBtn       = new(0.45f, 0.55f, 0.65f, 1f);
    public static readonly Vector4 ApplyBtnHover  = new(0.55f, 0.65f, 0.75f, 1f);
    public static readonly Vector4 ApplyBtnActive = new(0.60f, 0.70f, 0.80f, 1f);

    // ── Edit button (warm grey) ─────────────────────
    public static readonly Vector4 EditBtn        = new(0.55f, 0.50f, 0.45f, 1f);
    public static readonly Vector4 EditBtnHover   = new(0.65f, 0.60f, 0.55f, 1f);
    public static readonly Vector4 EditBtnActive  = new(0.70f, 0.65f, 0.60f, 1f);

    // ── Delete button (muted red) ───────────────────
    public static readonly Vector4 DeleteBtn      = new(0.60f, 0.40f, 0.40f, 1f);
    public static readonly Vector4 DeleteBtnHover = new(0.70f, 0.50f, 0.50f, 1f);
    public static readonly Vector4 DeleteBtnActive = new(0.75f, 0.55f, 0.55f, 1f);

    // ── CTA (Create Collection) button ──────────────
    public static readonly Vector4 CtaBtn         = new(0.15f, 0.35f, 0.15f, 1f);
    public static readonly Vector4 CtaBtnHover    = new(0.2f,  0.45f, 0.2f,  1f);
    public static readonly Vector4 CtaBtnActive   = new(0.12f, 0.28f, 0.12f, 1f);

    // ── Camera overlay ──────────────────────────────
    public static readonly Vector4 CameraVignette = new(0f,    0f,    0f,    0.4f);
    public static readonly Vector4 CameraBorder   = new(0.9f,  0.8f,  0.7f,  0.45f);
    public static readonly Vector4 CameraGrid     = new(1f,    1f,    1f,    0.12f);
    public static readonly Vector4 CameraText     = new(0.9f,  0.8f,  0.7f,  0.7f);
    public static readonly Vector4 CameraTextHov  = new(1f,    0.9f,  0.8f,  1f);

    // ── Camera buttons ──────────────────────────────
    public static readonly Vector4 CamCaptureBtn  = new(0.25f, 0.55f, 0.25f, 0.9f);
    public static readonly Vector4 CamCaptureHov  = new(0.35f, 0.65f, 0.35f, 1f);
    public static readonly Vector4 CamCaptureAct  = new(0.45f, 0.75f, 0.45f, 1f);
    public static readonly Vector4 CamCancelBtn   = new(0.50f, 0.25f, 0.25f, 0.9f);
    public static readonly Vector4 CamCancelHov   = new(0.60f, 0.35f, 0.35f, 1f);
    public static readonly Vector4 CamCancelAct   = new(0.70f, 0.45f, 0.45f, 1f);
}
