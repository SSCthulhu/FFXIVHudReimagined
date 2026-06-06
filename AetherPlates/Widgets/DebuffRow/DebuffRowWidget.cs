namespace FFXIVHudPlugin.AetherPlates.Widgets.DebuffRow;

using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using FFXIVHudPlugin.AetherPlates.Widgets.BuffRow;
using System.Numerics;

public sealed class DebuffRowWidget : INameplateWidget
{
    public string Id => "debuff_row";

    public Vector2 GetDesiredSize(NameplateContext context)
    {
        var cfg = context.Profile.DebuffRow;
        var maxIcons = Math.Clamp(cfg.MaxIcons, 1, 24);
        return new Vector2((cfg.IconSize * maxIcons) + (cfg.IconGap * (maxIcons - 1)), cfg.IconSize);
    }

    public void Draw(NameplateContext context, DrawContext drawContext, WidgetLayout layout)
    {
        BuffRowWidget.DrawStatusRow(context, drawContext, layout, true);
    }
}
