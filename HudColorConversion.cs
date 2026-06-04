using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Converts between ImGui IM_COL32 (AABBGGRR) and ColorEdit4 RGBA vectors.
/// </summary>
internal static class HudColorConversion
{
    public static Vector4 ToVector4(uint imColor)
    {
        return new Vector4(
            (imColor & 0xFF) / 255f,
            ((imColor >> 8) & 0xFF) / 255f,
            ((imColor >> 16) & 0xFF) / 255f,
            ((imColor >> 24) & 0xFF) / 255f);
    }

    public static uint ToImGuiColor(Vector4 rgba)
    {
        var r = (byte)Math.Clamp((int)MathF.Round(rgba.X * 255f), 0, 255);
        var g = (byte)Math.Clamp((int)MathF.Round(rgba.Y * 255f), 0, 255);
        var b = (byte)Math.Clamp((int)MathF.Round(rgba.Z * 255f), 0, 255);
        var a = (byte)Math.Clamp((int)MathF.Round(rgba.W * 255f), 0, 255);
        return ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
    }

    /// <summary>
    /// Fixes colors saved with the pre-0.0.20 AARRGGBB packing.
    /// </summary>
    public static uint MigrateLegacyArgbToImGuiColor(uint legacyArgb)
    {
        var a = (legacyArgb >> 24) & 0xFF;
        var r = (legacyArgb >> 16) & 0xFF;
        var g = (legacyArgb >> 8) & 0xFF;
        var b = legacyArgb & 0xFF;
        return (a << 24) | (b << 16) | (g << 8) | r;
    }
}
