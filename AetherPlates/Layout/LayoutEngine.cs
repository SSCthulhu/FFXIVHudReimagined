using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Styles;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Layout;

public sealed class LayoutEngine
{
    public WidgetLayout Calculate(
        NameplateContext context,
        NameplateStyle style,
        string widgetId,
        Vector2 desiredSize)
    {
        if (!context.CategoryVisual.WidgetLayouts.TryGetValue(widgetId, out var rule))
        {
            rule = WidgetLayoutRule.Default(widgetId);
        }

        var scale = Math.Clamp(context.GlobalScale, 0.5f, 3.0f);
        var baseSize = new Vector2(
            rule.Size.X > 0f ? rule.Size.X : desiredSize.X,
            rule.Size.Y > 0f ? rule.Size.Y : desiredSize.Y);
        var size = baseSize * scale;
        var scaledOffset = rule.Offset * scale;
        var anchorPosition = ResolveAnchorPosition(context.AnchorScreenPosition, size, rule.Anchor);
        var finalPosition = anchorPosition + scaledOffset;

        return new WidgetLayout(
            widgetId,
            rule.Anchor,
            scaledOffset,
            size,
            finalPosition,
            rule.Visible);
    }

    private static Vector2 ResolveAnchorPosition(Vector2 center, Vector2 size, WidgetAnchor anchor)
    {
        var half = size * 0.5f;
        return anchor switch
        {
            WidgetAnchor.Top => new Vector2(center.X - half.X, center.Y - size.Y),
            WidgetAnchor.Bottom => new Vector2(center.X - half.X, center.Y),
            WidgetAnchor.Left => new Vector2(center.X - size.X, center.Y - half.Y),
            WidgetAnchor.Right => new Vector2(center.X, center.Y - half.Y),
            WidgetAnchor.Center => new Vector2(center.X - half.X, center.Y - half.Y),
            WidgetAnchor.TopLeft => new Vector2(center.X - size.X, center.Y - size.Y),
            WidgetAnchor.TopRight => new Vector2(center.X, center.Y - size.Y),
            WidgetAnchor.BottomLeft => new Vector2(center.X - size.X, center.Y),
            WidgetAnchor.BottomRight => center,
            _ => center,
        };
    }
}
