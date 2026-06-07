using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using FFXIVHudPlugin.AetherPlates.Styles;
using FFXIVHudPlugin.AetherPlates.Widgets;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Core;

public sealed class NameplateRenderer
{
    private static readonly string[] PreferredDrawOrder =
    {
        "health_bar",
        "cast_bar",
        "cast_bar_text",
        "buff_row",
        "debuff_row",
        "target_indicator",
        "name_text",
    };

    private readonly WidgetRegistry widgetRegistry;
    private readonly LayoutEngine layoutEngine;
    private readonly StyleManager styleManager;
    private readonly ImGuiRenderer renderer;

    public NameplateRenderer(
        WidgetRegistry widgetRegistry,
        LayoutEngine layoutEngine,
        StyleManager styleManager,
        ImGuiRenderer renderer)
    {
        this.widgetRegistry = widgetRegistry;
        this.layoutEngine = layoutEngine;
        this.styleManager = styleManager;
        this.renderer = renderer;
    }

    public void DrawNameplate(NameplateContext context, IReadOnlySet<string> enabledWidgetIds)
    {
        var style = this.styleManager.Select(context);
        context = context with { ActiveStyle = style };
        var drawContext = this.renderer.BeginNameplateDraw();
        var drawnIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < PreferredDrawOrder.Length; i++)
        {
            var widgetId = PreferredDrawOrder[i];
            if (!enabledWidgetIds.Contains(widgetId))
            {
                continue;
            }

            if (!this.widgetRegistry.TryGet(widgetId, out var prioritizedWidget) || prioritizedWidget is null)
            {
                continue;
            }

            this.DrawWidget(context, style, drawContext, prioritizedWidget);
            drawnIds.Add(widgetId);
        }

        foreach (var widget in this.widgetRegistry.Widgets)
        {
            if (!enabledWidgetIds.Contains(widget.Id) || drawnIds.Contains(widget.Id))
            {
                continue;
            }

            this.DrawWidget(context, style, drawContext, widget);
        }
    }

    private void DrawWidget(
        NameplateContext context,
        NameplateStyle style,
        DrawContext drawContext,
        INameplateWidget widget)
    {
        if (widget is null)
        {
            return;
        }

        var desired = widget.GetDesiredSize(context);
        var layout = this.layoutEngine.Calculate(context, style, widget.Id, desired);
        if (!layout.Visible)
        {
            return;
        }

        widget.Draw(context, drawContext, layout);
    }
}
