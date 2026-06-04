using System.Numerics;

namespace FFXIVHudPlugin;

public static class HudLayoutOrigin
{
    public static Vector2 GetScreenCenter(Vector2 viewportPos, Vector2 viewportSize) =>
        viewportPos + (viewportSize * 0.5f);

    /// <summary>
    /// Screen pixel offset from the viewport center. Positive Y moves down.
    /// </summary>
    public static Vector2 GetElementCenter(
        Vector2 screenCenter,
        Vector2 hudOffset,
        float elementOffsetX,
        float elementOffsetY) =>
        screenCenter + hudOffset + new Vector2(elementOffsetX, elementOffsetY);

    public static Vector2 GetTopLeftFromCenter(Vector2 center, float width, float height) =>
        new Vector2(center.X - (width * 0.5f), center.Y - (height * 0.5f));

    /// <summary>
    /// Drag limits for screen-center pixel offsets: ±viewport width on X, ±viewport height on Y.
    /// </summary>
    public static ScreenOffsetBounds GetOffsetBounds(Vector2 viewportSize) =>
        new(-viewportSize.X, viewportSize.X, -viewportSize.Y, viewportSize.Y);
}

public readonly record struct ScreenOffsetBounds(float MinX, float MaxX, float MinY, float MaxY)
{
    public float ClampX(float value) => Math.Clamp(value, this.MinX, this.MaxX);

    public float ClampY(float value) => Math.Clamp(value, this.MinY, this.MaxY);
}
