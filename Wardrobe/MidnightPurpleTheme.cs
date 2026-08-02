using System.Numerics;

namespace Wardrobe;

/// <summary>
/// Midnight Purple — muted grey-lavender. Soft, moody, elegant.
/// </summary>
public class MidnightPurpleTheme : ITheme
{
    public Vector4 TextHeading    => new(0.72f, 0.68f, 0.76f, 1f);
    public Vector4 TextNormal     => new(0.62f, 0.58f, 0.65f, 0.9f);
    public Vector4 TextMuted      => new(0.50f, 0.46f, 0.54f, 1f);
    public Vector4 TextSubtle     => new(0.36f, 0.34f, 0.40f, 0.7f);
    public Vector4 TextError      => new(1f,    0.35f, 0.35f, 1f);
    public Vector4 TextSuccess    => new(0.5f,  0.8f,  0.5f,  0.9f);
    public Vector4 TextGreyHint   => new(0.44f, 0.40f, 0.48f, 0.7f);

    public Vector4 TabSelected    => new(0.22f, 0.16f, 0.26f, 1f);
    public Vector4 TabHovered     => new(0.16f, 0.12f, 0.18f, 0.8f);
    public Vector4 TabDefault     => new(0.10f, 0.08f, 0.13f, 0.6f);
    public Vector4 TabBorderLine  => new(0.26f, 0.22f, 0.28f, 0.8f);
    public Vector4 TabTextActive  => new(0.82f, 0.78f, 0.86f, 1f);
    public Vector4 TabTextIdle    => new(0.54f, 0.50f, 0.58f, 0.9f);
    public Vector4 TabPlusIcon    => new(0.78f, 0.74f, 0.82f, 1f);

    public Vector4 PlusBtn        => new(0.28f, 0.18f, 0.36f, 1f);
    public Vector4 PlusBtnInactive => new(0.18f, 0.12f, 0.24f, 0.6f);

    public Vector4 CountText      => new(0.58f, 0.54f, 0.62f, 0.8f);

    public Vector4 CardBg         => new(0.10f, 0.08f, 0.13f, 0.95f);
    public Vector4 CardBgHovered  => new(0.10f, 0.08f, 0.14f, 0.95f);
    public Vector4 CardBorder     => new(0.38f, 0.32f, 0.42f, 0.9f);
    public Vector4 CardBorderIdle => new(0.28f, 0.24f, 0.32f, 0.7f);
    public Vector4 CardLine       => new(0.25f, 0.22f, 0.30f, 0.6f);

    public Vector4 ThumbBg        => new(0.08f, 0.06f, 0.11f, 1f);
    public Vector4 ThumbBorder    => new(0.24f, 0.20f, 0.28f, 0.4f);
    public Vector4 ThumbCustomImg => new(0.52f, 0.68f, 0.52f, 0.9f);
    public Vector4 ThumbNoPreview => new(0.42f, 0.36f, 0.44f, 0.7f);

    public Vector4 IconDefault    => new(1f,    1f,    1f,    0.6f);
    public Vector4 IconHovered    => new(1f,    1f,    1f,    0.95f);
    public Vector4 SaveModsGold  => new(0.8f, 0.68f, 0.05f, 1f);

    public Vector4 ApplyBtn       => new(0.46f, 0.38f, 0.58f, 1f);
    public Vector4 ApplyBtnHover  => new(0.54f, 0.45f, 0.65f, 1f);
    public Vector4 ApplyBtnActive => new(0.58f, 0.50f, 0.70f, 1f);

    public Vector4 EditBtn        => new(0.44f, 0.38f, 0.46f, 1f);
    public Vector4 EditBtnHover   => new(0.52f, 0.45f, 0.54f, 1f);
    public Vector4 EditBtnActive  => new(0.58f, 0.52f, 0.60f, 1f);

    public Vector4 UnhideBtn      => new(0.50f, 0.42f, 0.25f, 1f);
    public Vector4 UnhideBtnHover => new(0.58f, 0.50f, 0.30f, 1f);
    public Vector4 UnhideBtnActive => new(0.64f, 0.55f, 0.34f, 1f);

    public Vector4 DeleteBtn      => new(0.48f, 0.28f, 0.30f, 1f);
    public Vector4 DeleteBtnHover => new(0.58f, 0.36f, 0.38f, 1f);
    public Vector4 DeleteBtnActive => new(0.65f, 0.42f, 0.44f, 1f);

    public Vector4 CtaBtn         => new(0.24f, 0.16f, 0.30f, 1f);
    public Vector4 CtaBtnHover    => new(0.30f, 0.22f, 0.38f, 1f);
    public Vector4 CtaBtnActive   => new(0.18f, 0.12f, 0.24f, 1f);

    public Vector4 SeparatorColor => new(0.32f, 0.28f, 0.38f, 0.5f);

    public Vector4 SearchBg => new(0.15f, 0.12f, 0.20f, 0.85f);

    public Vector4 CameraVignette => new(0f,    0f,    0f,    0.4f);
    public Vector4 CameraBorder   => new(0.64f, 0.58f, 0.68f, 0.45f);
    public Vector4 CameraGrid     => new(1f,    1f,    1f,    0.12f);
    public Vector4 CameraText     => new(0.64f, 0.58f, 0.68f, 0.7f);
    public Vector4 CameraTextHov  => new(0.78f, 0.74f, 0.82f, 1f);

    public Vector4 CamCaptureBtn  => new(0.28f, 0.18f, 0.36f, 0.9f);
    public Vector4 CamCaptureHov  => new(0.34f, 0.24f, 0.44f, 1f);
    public Vector4 CamCaptureAct  => new(0.40f, 0.30f, 0.50f, 1f);
    public Vector4 CamCancelBtn   => new(0.50f, 0.30f, 0.30f, 0.9f);
    public Vector4 CamCancelHov   => new(0.58f, 0.38f, 0.38f, 1f);
    public Vector4 CamCancelAct   => new(0.65f, 0.44f, 0.44f, 1f);

    // ── Browse rail (left sidebar) ───────────────────
    public Vector4 RailBg          => new(0.07f, 0.06f, 0.10f, 1f);
    public Vector4 RailItemBgActive => new(0.18f, 0.12f, 0.24f, 1f);
    public Vector4 RailItemBgHovered => new(0.12f, 0.09f, 0.16f, 0.8f);
    public Vector4 RailTextActive  => new(0.82f, 0.78f, 0.86f, 1f);
    public Vector4 RailTextIdle    => new(0.50f, 0.46f, 0.54f, 0.9f);
    public Vector4 RailDivider     => new(0.20f, 0.16f, 0.24f, 0.6f);

    // ── Collection chips ─────────────────────────────
    public Vector4 ChipBg          => new(0.10f, 0.08f, 0.13f, 0.95f);
    public Vector4 ChipBgActive    => new(0.22f, 0.16f, 0.26f, 1f);
    public Vector4 ChipBgHovered   => new(0.15f, 0.11f, 0.18f, 0.9f);
    public Vector4 ChipText        => new(0.54f, 0.50f, 0.58f, 0.9f);
    public Vector4 ChipTextActive  => new(0.82f, 0.78f, 0.86f, 1f);
    public Vector4 ChipBorder      => new(0.25f, 0.20f, 0.28f, 0.6f);

    // ── Window chrome ────────────────────────────────
    public Vector4 WindowBg => new(0.05f, 0.04f, 0.08f, 1f);
}
