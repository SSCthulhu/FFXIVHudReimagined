using FFXIVHudPlugin.AetherPlates.Data;

namespace FFXIVHudPlugin.AetherPlates.Styles;

public sealed class StyleManager
{
    private readonly Func<IReadOnlyList<NameplateStyle>> styleProvider;
    private readonly NameplateStyle fallback;

    public StyleManager(Func<IReadOnlyList<NameplateStyle>> styleProvider)
    {
        this.styleProvider = styleProvider;
        this.fallback = CreateFallback();
    }

    public NameplateStyle Select(NameplateContext context)
    {
        var styles = this.styleProvider();
        for (var i = 0; i < styles.Count; i++)
        {
            var style = styles[i];
            if (MatchesAll(style, context))
            {
                return style;
            }
        }

        return this.fallback;
    }

    public static NameplateStyle CreateFallback()
    {
        var style = new NameplateStyle
        {
            Id = "default",
            DisplayName = "Default",
        };

        style.WidgetLayouts["health_bar"] = new WidgetLayoutRule
        {
            WidgetId = "health_bar",
            Anchor = Layout.WidgetAnchor.Top,
            Offset = new System.Numerics.Vector2(0f, -34f),
            Size = new System.Numerics.Vector2(140f, 14f),
            Visible = true,
        };
        style.WidgetLayouts["name_text"] = new WidgetLayoutRule
        {
            WidgetId = "name_text",
            Anchor = Layout.WidgetAnchor.Top,
            Offset = new System.Numerics.Vector2(20f, -39f),
            Size = new System.Numerics.Vector2(180f, 18f),
            Visible = true,
        };
        style.WidgetLayouts["target_indicator"] = new WidgetLayoutRule
        {
            WidgetId = "target_indicator",
            Anchor = Layout.WidgetAnchor.Top,
            Offset = new System.Numerics.Vector2(0f, -70f),
            Size = new System.Numerics.Vector2(24f, 12f),
            Visible = true,
        };
        style.WidgetLayouts["cast_bar"] = new WidgetLayoutRule
        {
            WidgetId = "cast_bar",
            Anchor = Layout.WidgetAnchor.Top,
            Offset = new System.Numerics.Vector2(0f, -22f),
            Size = new System.Numerics.Vector2(140f, 10f),
            Visible = true,
        };
        style.WidgetLayouts["cast_bar_text"] = new WidgetLayoutRule
        {
            WidgetId = "cast_bar_text",
            Anchor = Layout.WidgetAnchor.Top,
            Offset = new System.Numerics.Vector2(0f, -20f),
            Size = new System.Numerics.Vector2(180f, 18f),
            Visible = true,
        };
        style.WidgetLayouts["buff_row"] = new WidgetLayoutRule
        {
            WidgetId = "buff_row",
            Anchor = Layout.WidgetAnchor.TopRight,
            Offset = new System.Numerics.Vector2(76f, -32f),
            Size = new System.Numerics.Vector2(138.8f, 20f),
            Visible = true,
        };
        style.WidgetLayouts["debuff_row"] = new WidgetLayoutRule
        {
            WidgetId = "debuff_row",
            Anchor = Layout.WidgetAnchor.TopLeft,
            Offset = new System.Numerics.Vector2(-76f, -32f),
            Size = new System.Numerics.Vector2(138.8f, 20f),
            Visible = true,
        };

        return style;
    }

    private static bool MatchesAll(NameplateStyle style, NameplateContext context)
    {
        if (style.Conditions.Count == 0)
        {
            return true;
        }

        for (var i = 0; i < style.Conditions.Count; i++)
        {
            if (!style.Conditions[i].Matches(context))
            {
                return false;
            }
        }

        return true;
    }
}
