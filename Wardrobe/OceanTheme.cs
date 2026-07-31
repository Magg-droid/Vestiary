using System.Numerics;

namespace Wardrobe;

/// <summary>
/// Ocean — cool muted grey-blue. Soft, elegant, image-first.
/// </summary>
public class OceanTheme : ITheme
{
    public Vector4 TextHeading    => new(0.72f, 0.76f, 0.80f, 1f);
    public Vector4 TextNormal     => new(0.60f, 0.64f, 0.68f, 0.9f);
    public Vector4 TextMuted      => new(0.48f, 0.52f, 0.56f, 1f);
    public Vector4 TextSubtle     => new(0.34f, 0.38f, 0.42f, 0.7f);
    public Vector4 TextError      => new(1f,    0.35f, 0.35f, 1f);
    public Vector4 TextSuccess    => new(0.5f,  0.8f,  0.5f,  0.9f);
    public Vector4 TextGreyHint   => new(0.42f, 0.45f, 0.48f, 0.7f);

    public Vector4 TabSelected    => new(0.18f, 0.22f, 0.28f, 1f);
    public Vector4 TabHovered     => new(0.14f, 0.16f, 0.20f, 0.8f);
    public Vector4 TabDefault     => new(0.08f, 0.10f, 0.13f, 0.6f);
    public Vector4 TabBorderLine  => new(0.24f, 0.26f, 0.30f, 0.8f);
    public Vector4 TabTextActive  => new(0.80f, 0.84f, 0.88f, 1f);
    public Vector4 TabTextIdle    => new(0.52f, 0.56f, 0.60f, 0.9f);
    public Vector4 TabPlusIcon    => new(0.76f, 0.80f, 0.84f, 1f);

    public Vector4 PlusBtn        => new(0.22f, 0.38f, 0.48f, 1f);
    public Vector4 PlusBtnInactive => new(0.14f, 0.24f, 0.30f, 0.6f);

    public Vector4 CountText      => new(0.55f, 0.58f, 0.62f, 0.8f);

    public Vector4 CardBg         => new(0.08f, 0.10f, 0.13f, 0.95f);
    public Vector4 CardBgHovered  => new(0.10f, 0.12f, 0.15f, 0.95f);
    public Vector4 CardBorder     => new(0.36f, 0.40f, 0.44f, 0.9f);
    public Vector4 CardBorderIdle => new(0.26f, 0.30f, 0.34f, 0.7f);
    public Vector4 CardLine       => new(0.24f, 0.28f, 0.32f, 0.6f);

    public Vector4 ThumbBg        => new(0.07f, 0.09f, 0.12f, 1f);
    public Vector4 ThumbBorder    => new(0.22f, 0.26f, 0.30f, 0.4f);
    public Vector4 ThumbCustomImg => new(0.52f, 0.68f, 0.52f, 0.9f);
    public Vector4 ThumbNoPreview => new(0.40f, 0.44f, 0.48f, 0.7f);

    public Vector4 IconDefault    => new(1f,    1f,    1f,    0.6f);
    public Vector4 IconHovered    => new(1f,    1f,    1f,    0.95f);
    public Vector4 LockGold       => new(1f,    0.85f, 0.2f, 1f);

    public Vector4 ApplyBtn       => new(0.38f, 0.52f, 0.62f, 1f);
    public Vector4 ApplyBtnHover  => new(0.45f, 0.60f, 0.70f, 1f);
    public Vector4 ApplyBtnActive => new(0.50f, 0.65f, 0.74f, 1f);

    public Vector4 EditBtn        => new(0.42f, 0.44f, 0.46f, 1f);
    public Vector4 EditBtnHover   => new(0.50f, 0.52f, 0.55f, 1f);
    public Vector4 EditBtnActive  => new(0.56f, 0.58f, 0.60f, 1f);

    public Vector4 UnhideBtn      => new(0.50f, 0.42f, 0.26f, 1f);
    public Vector4 UnhideBtnHover => new(0.58f, 0.50f, 0.30f, 1f);
    public Vector4 UnhideBtnActive => new(0.64f, 0.55f, 0.34f, 1f);

    public Vector4 DeleteBtn      => new(0.48f, 0.30f, 0.30f, 1f);
    public Vector4 DeleteBtnHover => new(0.58f, 0.38f, 0.38f, 1f);
    public Vector4 DeleteBtnActive => new(0.65f, 0.44f, 0.44f, 1f);

    public Vector4 CtaBtn         => new(0.18f, 0.28f, 0.34f, 1f);
    public Vector4 CtaBtnHover    => new(0.24f, 0.35f, 0.42f, 1f);
    public Vector4 CtaBtnActive   => new(0.14f, 0.22f, 0.28f, 1f);

    public Vector4 SeparatorColor => new(0.32f, 0.34f, 0.38f, 0.5f);

    public Vector4 SearchBg => new(0.14f, 0.16f, 0.20f, 0.85f);

    public Vector4 CameraVignette => new(0f,    0f,    0f,    0.4f);
    public Vector4 CameraBorder   => new(0.62f, 0.68f, 0.74f, 0.45f);
    public Vector4 CameraGrid     => new(1f,    1f,    1f,    0.12f);
    public Vector4 CameraText     => new(0.62f, 0.68f, 0.74f, 0.7f);
    public Vector4 CameraTextHov  => new(0.78f, 0.82f, 0.86f, 1f);

    public Vector4 CamCaptureBtn  => new(0.24f, 0.38f, 0.48f, 0.9f);
    public Vector4 CamCaptureHov  => new(0.30f, 0.45f, 0.55f, 1f);
    public Vector4 CamCaptureAct  => new(0.36f, 0.50f, 0.60f, 1f);
    public Vector4 CamCancelBtn   => new(0.50f, 0.30f, 0.30f, 0.9f);
    public Vector4 CamCancelHov   => new(0.58f, 0.38f, 0.38f, 1f);
    public Vector4 CamCancelAct   => new(0.65f, 0.44f, 0.44f, 1f);
}
