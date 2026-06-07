using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Widgets.TargetIndicator;

public sealed class TargetIndicatorWidget : INameplateWidget
{
    public string Id => "target_indicator";

    public Vector2 GetDesiredSize(NameplateContext context)
    {
        return new Vector2(24f, 12f);
    }

    public void Draw(NameplateContext context, DrawContext drawContext, WidgetLayout layout)
    {
        if (!context.IsTarget)
        {
            return;
        }

        var min = layout.Position;
        var max = layout.Position + layout.Size;
        var center = new Vector2((min.X + max.X) * 0.5f, max.Y);
        var color = context.Profile.TargetIndicator.Color;

        drawContext.DrawGlow(min - new Vector2(2f, 2f), max + new Vector2(2f, 2f), 0x8064A8FF, 2.5f);
        var scaleFactor = Math.Clamp(context.GlobalScale, 0.5f, 3.0f);
        using var fontScope = GameFontRegistry.PushFont(context.FontFamilyId);
        drawContext.DrawText(center + new Vector2(-4f, -10f) * scaleFactor, color, "▼", 14f * scaleFactor);
    }
}
