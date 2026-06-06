using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Layout;

public enum WidgetAnchor
{
    Top = 0,
    Bottom = 1,
    Left = 2,
    Right = 3,
    Center = 4,
    TopLeft = 5,
    TopRight = 6,
    BottomLeft = 7,
    BottomRight = 8,
}

public sealed record WidgetLayout(
    string WidgetId,
    WidgetAnchor Anchor,
    Vector2 Offset,
    Vector2 Size,
    Vector2 Position,
    bool Visible);
