using System.Numerics;

namespace Wardrobe;

public interface ITheme
{
    // ── Text ────────────────────────────────────────
    Vector4 TextHeading { get; }
    Vector4 TextNormal { get; }
    Vector4 TextMuted { get; }
    Vector4 TextSubtle { get; }
    Vector4 TextError { get; }
    Vector4 TextSuccess { get; }
    Vector4 TextGreyHint { get; }

    // ── Tab bar ─────────────────────────────────────
    Vector4 TabSelected { get; }
    Vector4 TabHovered { get; }
    Vector4 TabDefault { get; }
    Vector4 TabBorderLine { get; }
    Vector4 TabTextActive { get; }
    Vector4 TabTextIdle { get; }
    Vector4 TabPlusIcon { get; }

    // ── "+" button ──────────────────────────────────
    Vector4 PlusBtn { get; }
    Vector4 PlusBtnInactive { get; }

    // ── Design counts ───────────────────────────────
    Vector4 CountText { get; }

    // ── Design cards ────────────────────────────────
    Vector4 CardBg { get; }
    Vector4 CardBgHovered { get; }
    Vector4 CardBorder { get; }
    Vector4 CardBorderIdle { get; }
    Vector4 CardLine { get; }

    // ── Thumbnail area ──────────────────────────────
    Vector4 ThumbBg { get; }
    Vector4 ThumbBorder { get; }
    Vector4 ThumbCustomImg { get; }
    Vector4 ThumbNoPreview { get; }

    // ── Thumbnail action icons ──────────────────────
    Vector4 IconDefault { get; }
    Vector4 IconHovered { get; }
    Vector4 SaveModsGold { get; }

    // ── Apply button ────────────────────────────────
    Vector4 ApplyBtn { get; }
    Vector4 ApplyBtnHover { get; }
    Vector4 ApplyBtnActive { get; }

    // ── Edit / Settings button ──────────────────────
    Vector4 EditBtn { get; }
    Vector4 EditBtnHover { get; }
    Vector4 EditBtnActive { get; }

    // ── Unhide button ──────────────────────────────
    Vector4 UnhideBtn { get; }
    Vector4 UnhideBtnHover { get; }
    Vector4 UnhideBtnActive { get; }

    // ── Hide / Delete button ────────────────────────
    Vector4 DeleteBtn { get; }
    Vector4 DeleteBtnHover { get; }
    Vector4 DeleteBtnActive { get; }

    // ── CTA button ──────────────────────────────────
    Vector4 CtaBtn { get; }
    Vector4 CtaBtnHover { get; }
    Vector4 CtaBtnActive { get; }

    // ── Separator ──────────────────────────────────
    Vector4 SeparatorColor { get; }

    // ── Search ─────────────────────────────────────
    Vector4 SearchBg { get; }

    // ── Camera overlay ──────────────────────────────
    Vector4 CameraVignette { get; }
    Vector4 CameraBorder { get; }
    Vector4 CameraGrid { get; }
    Vector4 CameraText { get; }
    Vector4 CameraTextHov { get; }

    // ── Camera buttons ──────────────────────────────
    Vector4 CamCaptureBtn { get; }
    Vector4 CamCaptureHov { get; }
    Vector4 CamCaptureAct { get; }
    Vector4 CamCancelBtn { get; }
    Vector4 CamCancelHov { get; }
    Vector4 CamCancelAct { get; }
}
