using System.Numerics;

namespace Wardrobe;

/// <summary>
/// Color utility helpers for theme authors and UI code.
/// </summary>
public static class Palette
{
    public static Vector4 WithAlpha(Vector4 color, float alpha) => color with { W = alpha };
    public static Vector4 Mix(Vector4 from, Vector4 to, float amount) => Vector4.Lerp(from, to, amount);

    public static Vector4 Lighten(Vector4 color, float amount) =>
        Vector4.Lerp(color, new Vector4(1f, 1f, 1f, color.W), amount);

    public static Vector4 Darken(Vector4 color, float amount) =>
        Vector4.Lerp(color, new Vector4(0f, 0f, 0f, color.W), amount);

    public static uint Pack(Vector4 color) =>
        (uint)(color.W * 255) << 24 |
        (uint)(color.Z * 255) << 16 |
        (uint)(color.Y * 255) << 8 |
        (uint)(color.X * 255);
}
