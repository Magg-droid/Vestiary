using System.Numerics;

namespace Vestiary;

/// <summary>
/// Rose — dark charcoal with a muted blush-rose accent. Soft, elegant, fashion-catalog feel.
/// </summary>
public class RoseTheme : ITheme
{
    public Vector4 TextHeading    => new(0.953f, 0.914f, 0.933f, 1f);
    public Vector4 TextNormal     => new(0.800f, 0.745f, 0.780f, 0.95f);
    public Vector4 TextMuted      => new(0.620f, 0.565f, 0.604f, 1f);
    public Vector4 TextSubtle     => new(0.443f, 0.396f, 0.431f, 0.8f);
    public Vector4 TextError      => new(1f,    0.420f, 0.420f, 1f);
    public Vector4 TextSuccess    => new(0.800f, 0.549f, 0.678f, 0.9f);
    public Vector4 TextGreyHint   => new(0.502f, 0.455f, 0.490f, 0.8f);

    public Vector4 TabSelected    => new(0.161f, 0.118f, 0.145f, 1f);
    public Vector4 TabHovered     => new(0.122f, 0.090f, 0.110f, 0.8f);
    public Vector4 TabDefault     => new(0.075f, 0.059f, 0.075f, 0.6f);
    public Vector4 TabBorderLine  => new(0.220f, 0.180f, 0.200f, 0.8f);
    public Vector4 TabTextActive  => new(0.961f, 0.925f, 0.941f, 1f);
    public Vector4 TabTextIdle    => new(0.620f, 0.565f, 0.604f, 0.9f);
    public Vector4 TabPlusIcon    => new(0.800f, 0.745f, 0.780f, 1f);

    public Vector4 PlusBtn        => new(0.647f, 0.388f, 0.490f, 1f);   // #A5637D
    public Vector4 PlusBtnInactive => new(0.220f, 0.137f, 0.169f, 0.6f);

    public Vector4 CountText      => new(0.855f, 0.796f, 0.827f, 0.9f);

    public Vector4 CardBg         => new(0.075f, 0.059f, 0.075f, 1f);
    public Vector4 CardBgHovered  => new(0.090f, 0.071f, 0.090f, 1f);
    public Vector4 CardBorder     => new(0.240f, 0.190f, 0.220f, 0.85f);
    public Vector4 CardBorderIdle => new(0.180f, 0.145f, 0.169f, 0.7f);
    public Vector4 CardLine       => new(0.149f, 0.118f, 0.141f, 0.7f);

    public Vector4 ThumbBg        => new(0.055f, 0.043f, 0.055f, 1f);
    public Vector4 ThumbBorder    => new(0.165f, 0.129f, 0.153f, 0.4f);
    public Vector4 ThumbCustomImg => new(0.478f, 0.800f, 0.478f, 0.9f);
    public Vector4 ThumbNoPreview => new(0.553f, 0.490f, 0.525f, 1f);

    public Vector4 IconDefault    => new(1f,    1f,    1f,    0.60f);
    public Vector4 IconHovered    => new(1f,    1f,    1f,    1f);
    public Vector4 SaveModsGold   => new(0.941f, 0.788f, 0.290f, 1f);

    public Vector4 ApplyBtn       => new(0.647f, 0.388f, 0.490f, 1f);   // #A5637D
    public Vector4 ApplyBtnHover  => new(0.725f, 0.463f, 0.561f, 1f);   // #B9768F
    public Vector4 ApplyBtnActive => new(0.545f, 0.314f, 0.408f, 1f);

    public Vector4 EditBtn        => new(0.118f, 0.094f, 0.110f, 1f);
    public Vector4 EditBtnHover   => new(0.157f, 0.129f, 0.149f, 1f);
    public Vector4 EditBtnActive  => new(0.188f, 0.157f, 0.180f, 1f);

    public Vector4 UnhideBtn      => new(0.475f, 0.373f, 0.208f, 1f);
    public Vector4 UnhideBtnHover => new(0.549f, 0.435f, 0.247f, 1f);
    public Vector4 UnhideBtnActive => new(0.612f, 0.490f, 0.286f, 1f);

    public Vector4 DeleteBtn      => new(0.443f, 0.227f, 0.243f, 1f);
    public Vector4 DeleteBtnHover => new(0.529f, 0.290f, 0.310f, 1f);
    public Vector4 DeleteBtnActive => new(0.604f, 0.353f, 0.369f, 1f);

    public Vector4 CtaBtn         => new(0.506f, 0.290f, 0.373f, 1f);
    public Vector4 CtaBtnHover    => new(0.647f, 0.388f, 0.490f, 1f);
    public Vector4 CtaBtnActive   => new(0.427f, 0.235f, 0.310f, 1f);

    public Vector4 SeparatorColor => new(0.502f, 0.455f, 0.486f, 0.7f);

    public Vector4 SearchBg => new(0.075f, 0.059f, 0.075f, 1f);

    public Vector4 CameraVignette => new(0f,    0f,    0f,    0.45f);
    public Vector4 CameraBorder   => new(0.522f, 0.475f, 0.506f, 0.45f);
    public Vector4 CameraGrid     => new(1f,    1f,    1f,    0.10f);
    public Vector4 CameraText     => new(0.620f, 0.565f, 0.604f, 0.8f);
    public Vector4 CameraTextHov  => new(0.961f, 0.925f, 0.941f, 1f);

    public Vector4 CamCaptureBtn  => new(0.506f, 0.290f, 0.373f, 0.9f);
    public Vector4 CamCaptureHov  => new(0.647f, 0.388f, 0.490f, 1f);
    public Vector4 CamCaptureAct  => new(0.545f, 0.314f, 0.408f, 1f);
    public Vector4 CamCancelBtn   => new(0.443f, 0.227f, 0.243f, 0.9f);
    public Vector4 CamCancelHov   => new(0.529f, 0.290f, 0.310f, 1f);
    public Vector4 CamCancelAct   => new(0.604f, 0.353f, 0.369f, 1f);

    public Vector4 RailBg          => new(0.055f, 0.043f, 0.055f, 1f);
    public Vector4 RailItemBgActive => new(0.300f, 0.170f, 0.230f, 1f);
    public Vector4 RailItemBgHovered => new(0.098f, 0.078f, 0.094f, 0.8f);
    public Vector4 RailTextActive  => new(0.961f, 0.925f, 0.941f, 1f);
    public Vector4 RailTextIdle    => new(0.620f, 0.565f, 0.604f, 0.9f);
    public Vector4 RailDivider     => new(0.180f, 0.145f, 0.169f, 0.7f);

    public Vector4 ChipBg          => new(0.075f, 0.059f, 0.075f, 1f);
    public Vector4 ChipBgActive    => new(0.300f, 0.170f, 0.230f, 1f);
    public Vector4 ChipBgHovered   => new(0.106f, 0.086f, 0.102f, 0.9f);
    public Vector4 ChipText        => new(0.620f, 0.565f, 0.604f, 1f);
    public Vector4 ChipTextActive  => new(0.961f, 0.925f, 0.941f, 1f);
    public Vector4 ChipBorder      => new(0.200f, 0.160f, 0.188f, 0.8f);

    public Vector4 WindowBg => new(0.043f, 0.031f, 0.043f, 1f);
}
