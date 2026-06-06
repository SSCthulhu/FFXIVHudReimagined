using Dalamud.Bindings.ImGui;

namespace FFXIVHudPlugin.AetherPlates.Rendering;

public sealed class ImGuiRenderer
{
    public DrawContext BeginNameplateDraw()
    {
        var drawList = ImGui.GetForegroundDrawList();
        return new DrawContext(drawList);
    }
}
