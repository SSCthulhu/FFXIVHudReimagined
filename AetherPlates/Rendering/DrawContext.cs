using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Rendering;

public sealed class DrawContext
{
    private readonly ImDrawListPtr drawList;

    public DrawContext(ImDrawListPtr drawList)
    {
        this.drawList = drawList;
    }

    public void DrawText(Vector2 position, uint color, string text, float fontSize = 0f)
    {
        if (fontSize <= 0f)
        {
            this.drawList.AddText(position, color, text);
            return;
        }

        this.drawList.AddText(ImGui.GetFont(), fontSize, position, color, text);
    }

    public void DrawText(Vector2 position, uint color, string text, ImFontPtr font, float fontSize)
    {
        if (fontSize <= 0f)
        {
            DrawText(position, color, text, fontSize);
            return;
        }

        this.drawList.AddText(font, fontSize, position, color, text);
    }

    public void DrawRect(Vector2 min, Vector2 max, uint color, float rounding = 0f, float thickness = 1f)
    {
        this.drawList.AddRect(min, max, color, rounding, ImDrawFlags.None, thickness);
    }

    public void DrawFilledRect(Vector2 min, Vector2 max, uint color, float rounding = 0f)
    {
        this.drawList.AddRectFilled(min, max, color, rounding);
    }

    public void DrawImage(ISharedImmediateTexture texture, Vector2 min, Vector2 max, uint tintColor)
    {
        var wrap = texture.GetWrapOrEmpty();
        this.drawList.AddImage(wrap.Handle, min, max, Vector2.Zero, Vector2.One, tintColor);
    }

    public void DrawBorder(Vector2 min, Vector2 max, uint color, float rounding, float thickness)
    {
        this.drawList.AddRect(min, max, color, rounding, ImDrawFlags.None, thickness);
    }

    public void DrawGlow(Vector2 min, Vector2 max, uint color, float strength)
    {
        this.drawList.AddRect(min, max, color, 4f, ImDrawFlags.None, MathF.Max(1f, strength));
    }
}
