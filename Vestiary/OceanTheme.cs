using System.Numerics;

namespace Vestiary;

/// <summary>
/// Ocean — dark charcoal with muted azure accents.
/// </summary>
public class OceanTheme : ITheme
{
    public Vector4 TextHeading    => new(0.898f, 0.929f, 0.961f, 1f);
    public Vector4 TextNormal     => new(0.780f, 0.824f, 0.867f, 0.95f);
    public Vector4 TextMuted      => new(0.596f, 0.643f, 0.694f, 1f);
    public Vector4 TextSubtle     => new(0.416f, 0.455f, 0.502f, 0.8f);
    public Vector4 TextError      => new(1f,    0.420f, 0.420f, 1f);
    public Vector4 TextSuccess    => new(0.550f, 0.720f, 0.850f, 0.9f);
    public Vector4 TextGreyHint   => new(0.478f, 0.518f, 0.565f, 0.8f);

    public Vector4 TabSelected    => new(0.094f, 0.149f, 0.188f, 1f);
    public Vector4 TabHovered     => new(0.071f, 0.114f, 0.149f, 0.8f);
    public Vector4 TabDefault     => new(0.063f, 0.086f, 0.118f, 0.6f);
    public Vector4 TabBorderLine  => new(0.141f, 0.188f, 0.231f, 0.8f);
    public Vector4 TabTextActive  => new(0.925f, 0.949f, 0.976f, 1f);
    public Vector4 TabTextIdle    => new(0.576f, 0.624f, 0.675f, 0.9f);
    public Vector4 TabPlusIcon    => new(0.855f, 0.886f, 0.918f, 1f);

    public Vector4 PlusBtn        => new(0.243f, 0.408f, 0.565f, 1f);
    public Vector4 PlusBtnInactive => new(0.106f, 0.169f, 0.227f, 0.6f);

    public Vector4 CountText      => new(0.850f, 0.875f, 0.900f, 0.9f);

    public Vector4 CardBg         => new(0.063f, 0.086f, 0.118f, 1f);
    public Vector4 CardBgHovered  => new(0.078f, 0.102f, 0.133f, 1f);
    public Vector4 CardBorder     => new(0.141f, 0.188f, 0.231f, 0.85f);
    public Vector4 CardBorderIdle => new(0.106f, 0.149f, 0.192f, 0.7f);
    public Vector4 CardLine       => new(0.098f, 0.133f, 0.173f, 0.7f);

    public Vector4 ThumbBg        => new(0.051f, 0.071f, 0.090f, 1f);
    public Vector4 ThumbBorder    => new(0.110f, 0.137f, 0.169f, 0.4f);
    public Vector4 ThumbCustomImg => new(0.478f, 0.800f, 0.478f, 0.9f);
    public Vector4 ThumbNoPreview => new(0.540f, 0.580f, 0.625f, 1f);

    public Vector4 IconDefault    => new(1f,    1f,    1f,    0.60f);
    public Vector4 IconHovered    => new(1f,    1f,    1f,    1f);
    public Vector4 SaveModsGold   => new(0.941f, 0.788f, 0.290f, 1f);

    public Vector4 ApplyBtn       => new(0.290f, 0.482f, 0.659f, 1f);
    public Vector4 ApplyBtnHover  => new(0.357f, 0.549f, 0.722f, 1f);
    public Vector4 ApplyBtnActive => new(0.243f, 0.408f, 0.565f, 1f);

    public Vector4 EditBtn        => new(0.106f, 0.122f, 0.137f, 1f);
    public Vector4 EditBtnHover   => new(0.145f, 0.161f, 0.176f, 1f);
    public Vector4 EditBtnActive  => new(0.176f, 0.192f, 0.208f, 1f);

    public Vector4 UnhideBtn      => new(0.475f, 0.373f, 0.208f, 1f);
    public Vector4 UnhideBtnHover => new(0.549f, 0.435f, 0.247f, 1f);
    public Vector4 UnhideBtnActive => new(0.612f, 0.490f, 0.286f, 1f);

    public Vector4 DeleteBtn      => new(0.443f, 0.227f, 0.243f, 1f);
    public Vector4 DeleteBtnHover => new(0.529f, 0.290f, 0.310f, 1f);
    public Vector4 DeleteBtnActive => new(0.604f, 0.353f, 0.369f, 1f);

    public Vector4 CtaBtn         => new(0.184f, 0.314f, 0.408f, 1f);
    public Vector4 CtaBtnHover    => new(0.227f, 0.380f, 0.494f, 1f);
    public Vector4 CtaBtnActive   => new(0.153f, 0.267f, 0.341f, 1f);

    public Vector4 SeparatorColor => new(0.480f, 0.520f, 0.560f, 0.7f);

    public Vector4 SearchBg => new(0.071f, 0.094f, 0.125f, 1f);

    public Vector4 CameraVignette => new(0f,    0f,    0f,    0.45f);
    public Vector4 CameraBorder   => new(0.502f, 0.549f, 0.600f, 0.45f);
    public Vector4 CameraGrid     => new(1f,    1f,    1f,    0.10f);
    public Vector4 CameraText     => new(0.596f, 0.643f, 0.694f, 0.8f);
    public Vector4 CameraTextHov  => new(0.925f, 0.949f, 0.976f, 1f);

    public Vector4 CamCaptureBtn  => new(0.184f, 0.314f, 0.408f, 0.9f);
    public Vector4 CamCaptureHov  => new(0.227f, 0.380f, 0.494f, 1f);
    public Vector4 CamCaptureAct  => new(0.243f, 0.408f, 0.565f, 1f);
    public Vector4 CamCancelBtn   => new(0.443f, 0.227f, 0.243f, 0.9f);
    public Vector4 CamCancelHov   => new(0.529f, 0.290f, 0.310f, 1f);
    public Vector4 CamCancelAct   => new(0.604f, 0.353f, 0.369f, 1f);

    public Vector4 RailBg          => new(0.051f, 0.071f, 0.090f, 1f);
    public Vector4 RailItemBgActive => new(0.170f, 0.300f, 0.420f, 1f);
    public Vector4 RailItemBgHovered => new(0.067f, 0.110f, 0.145f, 0.8f);
    public Vector4 RailTextActive  => new(0.925f, 0.949f, 0.976f, 1f);
    public Vector4 RailTextIdle    => new(0.576f, 0.624f, 0.675f, 0.9f);
    public Vector4 RailDivider     => new(0.141f, 0.188f, 0.231f, 0.7f);

    public Vector4 ChipBg          => new(0.063f, 0.086f, 0.118f, 1f);
    public Vector4 ChipBgActive    => new(0.170f, 0.300f, 0.420f, 1f);
    public Vector4 ChipBgHovered   => new(0.090f, 0.118f, 0.157f, 0.9f);
    public Vector4 ChipText        => new(0.596f, 0.643f, 0.694f, 1f);
    public Vector4 ChipTextActive  => new(0.925f, 0.949f, 0.976f, 1f);
    public Vector4 ChipBorder      => new(0.141f, 0.188f, 0.231f, 0.8f);

    public Vector4 WindowBg => new(0.039f, 0.059f, 0.078f, 1f);
}
