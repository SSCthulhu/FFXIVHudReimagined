using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Widgets.NameText;

public sealed class NameTextWidget : INameplateWidget
{
    public string Id => "name_text";

    public Vector2 GetDesiredSize(NameplateContext context)
    {
        return new Vector2(180f, 18f);
    }

    public void Draw(NameplateContext context, DrawContext drawContext, WidgetLayout layout)
    {
        var displayText = context.Tracked.Name;
        var truncateAt = Math.Max(6, context.Profile.NameText.TruncateAt);
        if (displayText.Length > truncateAt)
        {
            displayText = $"{displayText[..Math.Max(3, truncateAt - 3)]}...";
        }

        var baseFontSizeSetting = Math.Clamp(context.CategoryVisual.NameTextFontSize, 8f, 64f);
        var fontSize = baseFontSizeSetting * Math.Clamp(context.Profile.NameText.FontScale, 0.7f, 2.4f) * Math.Clamp(context.GlobalScale, 0.5f, 3.0f);
        var scaleFactor = Math.Clamp(context.GlobalScale, 0.5f, 3.0f);
        var stroke = Math.Max(1f, 1.2f * scaleFactor);
        using var fontScope = GameFontRegistry.PushFont(context.FontFamilyId);
        var baseFontSize = Math.Max(1f, ImGui.GetFontSize());
        var textSize = ImGui.CalcTextSize(displayText) * (fontSize / baseFontSize);
        var pos = new Vector2(
            layout.Position.X + MathF.Max(0f, (layout.Size.X - textSize.X) * 0.5f),
            layout.Position.Y + MathF.Max(0f, (layout.Size.Y - textSize.Y) * 0.5f));

        if (context.Profile.NameText.Outline)
        {
            drawContext.DrawText(pos + new Vector2(-stroke, 0f), 0xF0000000, displayText, fontSize);
            drawContext.DrawText(pos + new Vector2(stroke, 0f), 0xF0000000, displayText, fontSize);
            drawContext.DrawText(pos + new Vector2(0f, -stroke), 0xF0000000, displayText, fontSize);
            drawContext.DrawText(pos + new Vector2(0f, stroke), 0xF0000000, displayText, fontSize);
            drawContext.DrawText(pos + new Vector2(-stroke, -stroke), 0xD0000000, displayText, fontSize);
            drawContext.DrawText(pos + new Vector2(stroke, -stroke), 0xD0000000, displayText, fontSize);
            drawContext.DrawText(pos + new Vector2(-stroke, stroke), 0xD0000000, displayText, fontSize);
            drawContext.DrawText(pos + new Vector2(stroke, stroke), 0xD0000000, displayText, fontSize);
        }
        else if (context.Profile.NameText.Shadow)
        {
            drawContext.DrawText(pos + new Vector2(stroke, stroke), 0xCC000000, displayText, fontSize);
        }

        var textColor = context.ActiveStyle?.NameColor ?? 0xFFFFFFFF;
        drawContext.DrawText(pos, textColor, displayText, fontSize);
    }
}
