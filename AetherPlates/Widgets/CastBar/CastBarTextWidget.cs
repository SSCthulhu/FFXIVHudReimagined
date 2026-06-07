namespace FFXIVHudPlugin.AetherPlates.Widgets.CastBar;

using Dalamud.Bindings.ImGui;
using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using System.Numerics;

public sealed class CastBarTextWidget : INameplateWidget
{
    public string Id => "cast_bar_text";

    public Vector2 GetDesiredSize(NameplateContext context)
    {
        return new Vector2(180f, 18f);
    }

    public void Draw(NameplateContext context, DrawContext drawContext, WidgetLayout layout)
    {
        var cast = context.Tracked.CastInfo;
        if (!cast.IsCasting || cast.TotalTime <= 0.001f)
        {
            return;
        }

        var castName = string.IsNullOrWhiteSpace(cast.ActionName) ? "Casting..." : cast.ActionName;
        var scaleFactor = Math.Clamp(context.GlobalScale, 0.5f, 3.0f);
        var stroke = Math.Max(1f, 1.2f * scaleFactor);
        var baseFontSizeSetting = Math.Clamp(context.CategoryVisual.CastBarTextFontSize, 8f, 64f);
        var textSize = baseFontSizeSetting * scaleFactor;

        using var fontScope = GameFontRegistry.PushFont(context.FontFamilyId);
        var baseFontSize = Math.Max(1f, ImGui.GetFontSize());
        var measuredText = ImGui.CalcTextSize(castName) * (textSize / baseFontSize);
        var textPos = new Vector2(
            layout.Position.X + MathF.Max(0f, (layout.Size.X - measuredText.X) * 0.5f),
            layout.Position.Y + MathF.Max(0f, (layout.Size.Y - measuredText.Y) * 0.5f));

        drawContext.DrawText(textPos + new Vector2(-stroke, 0f), 0xF0000000, castName, textSize);
        drawContext.DrawText(textPos + new Vector2(stroke, 0f), 0xF0000000, castName, textSize);
        drawContext.DrawText(textPos + new Vector2(0f, -stroke), 0xF0000000, castName, textSize);
        drawContext.DrawText(textPos + new Vector2(0f, stroke), 0xF0000000, castName, textSize);
        drawContext.DrawText(textPos + new Vector2(-stroke, -stroke), 0xD0000000, castName, textSize);
        drawContext.DrawText(textPos + new Vector2(stroke, -stroke), 0xD0000000, castName, textSize);
        drawContext.DrawText(textPos + new Vector2(-stroke, stroke), 0xD0000000, castName, textSize);
        drawContext.DrawText(textPos + new Vector2(stroke, stroke), 0xD0000000, castName, textSize);
        drawContext.DrawText(textPos, 0xFFFFFFFF, castName, textSize);
    }
}
