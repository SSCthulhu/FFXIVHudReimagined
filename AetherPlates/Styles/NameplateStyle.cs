using FFXIVHudPlugin.AetherPlates.Layout;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Styles;

[Serializable]
public sealed class NameplateStyle
{
    public string Id { get; set; } = "default";
    public string DisplayName { get; set; } = "Default";
    public List<StyleCondition> Conditions { get; set; } = new();
    public Dictionary<string, WidgetLayoutRule> WidgetLayouts { get; set; } = new(StringComparer.Ordinal);
    public uint HealthColor { get; set; } = 0xFF4AB34A;
    public uint HealthBackgroundColor { get; set; } = 0xAA202020;
    public uint NameColor { get; set; } = 0xFFFFFFFF;
}

[Serializable]
public sealed class WidgetLayoutRule
{
    public string WidgetId { get; set; } = string.Empty;
    public WidgetAnchor Anchor { get; set; } = WidgetAnchor.Top;
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = Vector2.Zero;
    public bool Visible { get; set; } = true;

    public static WidgetLayoutRule Default(string widgetId)
    {
        return new WidgetLayoutRule
        {
            WidgetId = widgetId,
            Anchor = WidgetAnchor.Top,
            Offset = Vector2.Zero,
            Size = Vector2.Zero,
            Visible = true,
        };
    }
}
