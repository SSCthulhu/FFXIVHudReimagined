using Dalamud.Bindings.ImGui;

namespace FFXIVHudPlugin.AetherPlates.Rendering;

public sealed class ImGuiRenderer
{
    public enum DrawLayer
    {
        Background = 0,
        Foreground = 1,
        Window = 2,
    }

    private readonly DrawLayer drawLayer;

    public ImGuiRenderer(DrawLayer drawLayer = DrawLayer.Background)
    {
        this.drawLayer = drawLayer;
    }

    public DrawContext BeginNameplateDraw()
    {
        var drawList = this.drawLayer switch
        {
            DrawLayer.Foreground => ImGui.GetForegroundDrawList(),
            DrawLayer.Window => ImGui.GetWindowDrawList(),
            _ => ImGui.GetBackgroundDrawList(),
        };
        return new DrawContext(drawList);
    }
}
