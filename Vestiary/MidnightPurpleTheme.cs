using System.Numerics;

namespace Vestiary;

/// <summary>
/// Midnight Purple — dark charcoal with muted purple accents.
/// </summary>
public class MidnightPurpleTheme : ITheme
{
    public Vector4 TextHeading    => new(0.953f, 0.933f, 0.973f, 1f);   // #F3EEF8
    public Vector4 TextNormal     => new(0.784f, 0.745f, 0.843f, 1f);   // #C8BED7
    public Vector4 TextMuted      => new(0.604f, 0.565f, 0.667f, 1f);   // #9A90AA
    public Vector4 TextSubtle     => new(0.431f, 0.396f, 0.502f, 0.8f); // #6E6580
    public Vector4 TextError      => new(1f,    0.420f, 0.420f, 1f);
    public Vector4 TextSuccess    => new(0.698f, 0.541f, 0.878f, 1f);   // #B28AE0
    public Vector4 TextGreyHint   => new(0.494f, 0.451f, 0.569f, 0.8f); // #7E7391

    public Vector4 TabSelected    => new(0.141f, 0.110f, 0.200f, 1f);
    public Vector4 TabHovered     => new(0.102f, 0.078f, 0.149f, 0.8f);
    public Vector4 TabDefault     => new(0.071f, 0.071f, 0.102f, 0.6f);
    public Vector4 TabBorderLine  => new(0.204f, 0.165f, 0.271f, 0.8f);
    public Vector4 TabTextActive  => new(1f,    1f,    1f,    1f);
    public Vector4 TabTextIdle    => new(0.788f, 0.753f, 0.843f, 0.9f);
    public Vector4 TabPlusIcon    => new(0.784f, 0.745f, 0.843f, 1f);

    public Vector4 PlusBtn        => new(0.490f, 0.349f, 0.671f, 1f);   // #7D59AB
    public Vector4 PlusBtnInactive => new(0.165f, 0.137f, 0.220f, 0.6f);

    public Vector4 CountText      => new(0.847f, 0.816f, 0.894f, 0.9f); // #D8D0E4

    public Vector4 CardBg         => new(0.071f, 0.071f, 0.102f, 1f);   // #12121A
    public Vector4 CardBgHovered  => new(0.090f, 0.090f, 0.129f, 1f);
    public Vector4 CardBorder     => new(0.227f, 0.184f, 0.290f, 0.85f); // #3A2F4A
    public Vector4 CardBorderIdle => new(0.173f, 0.141f, 0.220f, 0.7f);
    public Vector4 CardLine       => new(0.141f, 0.114f, 0.188f, 0.7f); // #241D30

    public Vector4 ThumbBg        => new(0.047f, 0.047f, 0.071f, 1f);   // #0C0C12
    public Vector4 ThumbBorder    => new(0.153f, 0.129f, 0.200f, 0.4f); // #272133
    public Vector4 ThumbCustomImg => new(0.478f, 0.800f, 0.478f, 0.9f);
    public Vector4 ThumbNoPreview => new(0.553f, 0.514f, 0.627f, 1f);   // #8D83A0

    public Vector4 IconDefault    => new(1f,    1f,    1f,    0.60f);
    public Vector4 IconHovered    => new(1f,    1f,    1f,    1f);
    public Vector4 SaveModsGold   => new(0.941f, 0.788f, 0.290f, 1f);   // #F0C94A

    public Vector4 ApplyBtn       => new(0.416f, 0.290f, 0.573f, 1f);   // #6A4A92
    public Vector4 ApplyBtnHover  => new(0.482f, 0.353f, 0.643f, 1f);   // #7B5AA4
    public Vector4 ApplyBtnActive => new(0.353f, 0.243f, 0.494f, 1f);

    public Vector4 EditBtn        => new(0.102f, 0.102f, 0.141f, 1f);
    public Vector4 EditBtnHover   => new(0.137f, 0.137f, 0.184f, 1f);
    public Vector4 EditBtnActive  => new(0.169f, 0.169f, 0.220f, 1f);

    public Vector4 UnhideBtn      => new(0.475f, 0.373f, 0.208f, 1f);
    public Vector4 UnhideBtnHover => new(0.549f, 0.435f, 0.247f, 1f);
    public Vector4 UnhideBtnActive => new(0.612f, 0.490f, 0.286f, 1f);

    public Vector4 DeleteBtn      => new(0.443f, 0.227f, 0.243f, 1f);
    public Vector4 DeleteBtnHover => new(0.529f, 0.290f, 0.310f, 1f);
    public Vector4 DeleteBtnActive => new(0.604f, 0.353f, 0.369f, 1f);

    public Vector4 CtaBtn         => new(0.431f, 0.290f, 0.604f, 1f);   // #6E4A9A
    public Vector4 CtaBtnHover    => new(0.490f, 0.349f, 0.671f, 1f);   // #7D59AB
    public Vector4 CtaBtnActive   => new(0.361f, 0.227f, 0.510f, 1f);   // #5C3A82

    public Vector4 SeparatorColor => new(0.494f, 0.451f, 0.569f, 0.7f);

    public Vector4 SearchBg => new(0.071f, 0.071f, 0.102f, 1f);         // #12121A

    public Vector4 CameraVignette => new(0f,    0f,    0f,    0.45f);
    public Vector4 CameraBorder   => new(0.510f, 0.482f, 0.584f, 0.45f);
    public Vector4 CameraGrid     => new(1f,    1f,    1f,    0.10f);
    public Vector4 CameraText     => new(0.604f, 0.565f, 0.667f, 0.8f);
    public Vector4 CameraTextHov  => new(0.953f, 0.933f, 0.973f, 1f);

    public Vector4 CamCaptureBtn  => new(0.431f, 0.290f, 0.604f, 0.9f);
    public Vector4 CamCaptureHov  => new(0.490f, 0.349f, 0.671f, 1f);
    public Vector4 CamCaptureAct  => new(0.361f, 0.227f, 0.510f, 1f);
    public Vector4 CamCancelBtn   => new(0.443f, 0.227f, 0.243f, 0.9f);
    public Vector4 CamCancelHov   => new(0.529f, 0.290f, 0.310f, 1f);
    public Vector4 CamCancelAct   => new(0.604f, 0.353f, 0.369f, 1f);

    // ── Browse rail (left sidebar) ───────────────────
    public Vector4 RailBg          => new(0.043f, 0.043f, 0.071f, 1f);  // #0B0B12
    public Vector4 RailItemBgActive => new(0.361f, 0.227f, 0.510f, 1f); // #5C3A82
    public Vector4 RailItemBgHovered => new(0.090f, 0.075f, 0.125f, 0.8f);
    public Vector4 RailTextActive  => new(1f,    1f,    1f,    1f);
    public Vector4 RailTextIdle    => new(0.788f, 0.753f, 0.843f, 0.9f);
    public Vector4 RailDivider     => new(0.141f, 0.114f, 0.188f, 0.7f);

    // ── Collection chips ─────────────────────────────
    public Vector4 ChipBg          => new(0.078f, 0.075f, 0.110f, 1f);  // #14131C
    public Vector4 ChipBgActive    => new(0.361f, 0.227f, 0.510f, 1f);  // #5C3A82
    public Vector4 ChipBgHovered   => new(0.102f, 0.086f, 0.141f, 0.9f);
    public Vector4 ChipText        => new(0.784f, 0.745f, 0.843f, 1f);  // #C8BED7
    public Vector4 ChipTextActive  => new(1f,    1f,    1f,    1f);
    public Vector4 ChipBorder      => new(0.192f, 0.153f, 0.247f, 0.8f); // #31273F

    // ── Window chrome ────────────────────────────────
    public Vector4 WindowBg => new(0.031f, 0.031f, 0.051f, 1f);         // #08080D
}
