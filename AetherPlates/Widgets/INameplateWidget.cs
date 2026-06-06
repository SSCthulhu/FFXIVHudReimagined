using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Widgets;

public interface INameplateWidget
{
    string Id { get; }
    Vector2 GetDesiredSize(NameplateContext context);
    void Draw(
        NameplateContext context,
        DrawContext drawContext,
        WidgetLayout layout);
}
