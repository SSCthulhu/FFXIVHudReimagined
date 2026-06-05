using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Optional reticle and debug overlay renderer for action camera.
/// </summary>
internal static class ActionCameraOverlay
{
    public static void Draw(ActionCameraConfiguration config, ActionCameraRuntimeState state)
    {
        if (!state.Active && !config.ShowDebugOverlay)
        {
            return;
        }

        var drawList = ImGui.GetForegroundDrawList();
        var viewport = ImGui.GetMainViewport();
        var center = viewport.Pos + (viewport.Size * 0.5f);

        if (config.ShowReticle && state.Active)
        {
            DrawReticle(drawList, center);
        }

        if (config.EnableSoftTargetSuggestion && state.SoftTargetHasCandidate)
        {
            DrawSoftTargetHighlight(drawList, new Vector2(state.SoftTargetScreenX, state.SoftTargetScreenY));
        }

        if (config.ShowDebugOverlay)
        {
            DrawDebugWindow(state);
        }
    }

    private static void DrawReticle(ImDrawListPtr drawList, Vector2 center)
    {
        const float size = 6f;
        const uint color = 0xCCFFFFFF;
        drawList.AddLine(new Vector2(center.X - size, center.Y), new Vector2(center.X + size, center.Y), color, 1.5f);
        drawList.AddLine(new Vector2(center.X, center.Y - size), new Vector2(center.X, center.Y + size), color, 1.5f);
    }

    private static void DrawDebugWindow(ActionCameraRuntimeState state)
    {
        ImGui.SetNextWindowBgAlpha(0.55f);
        if (!ImGui.Begin("Action Camera Debug", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
        {
            ImGui.End();
            return;
        }

        ImGui.TextUnformatted($"Camera Active: {state.Active}");
        ImGui.TextUnformatted($"Cursor Locked: {state.CursorLocked}");
        ImGui.TextUnformatted($"Hold Unlock Held: {state.HoldUnlockHeld}");
        ImGui.TextUnformatted($"UI Focused: {state.UiFocused}");
        ImGui.TextUnformatted($"Backend: {state.BackendName}");
        ImGui.TextUnformatted($"IsLatched: {state.IsLatched}");
        ImGui.TextUnformatted($"UnlockReason: {state.UnlockReason}");
        ImGui.TextUnformatted($"PendingRelock: {state.PendingRelock}");
        ImGui.TextUnformatted($"Mouse DX: {state.MouseDeltaX:0.00}");
        ImGui.TextUnformatted($"Mouse DY: {state.MouseDeltaY:0.00}");
        ImGui.TextUnformatted($"Yaw: {state.Yaw:0.000}");
        ImGui.TextUnformatted($"Pitch: {state.Pitch:0.000}");
        ImGui.TextUnformatted($"Provider Available: {state.ProviderAvailable}");
        ImGui.TextUnformatted($"Camera Write Applied: {state.CameraWriteApplied}");
        ImGui.TextUnformatted($"Camera Write Persisted: {state.CameraWritePersisted}");
        ImGui.TextUnformatted($"Readback Yaw: {state.ReadbackYaw:0.000}");
        ImGui.TextUnformatted($"Readback Pitch: {state.ReadbackPitch:0.000}");
        ImGui.TextUnformatted($"UpdateTick: {state.UpdateTick}");
        ImGui.TextUnformatted($"LateUpdateTick: {state.LateUpdateTick}");
        ImGui.TextUnformatted($"LastError: {state.LastError}");
        ImGui.TextUnformatted($"SoftTargetCandidate: {state.SoftTargetHasCandidate}");
        ImGui.TextUnformatted($"SoftTargetObjectId: {state.SoftTargetObjectId}");
        ImGui.TextUnformatted($"SoftTargetScreen: {state.SoftTargetScreenX:0.0}, {state.SoftTargetScreenY:0.0}");
        ImGui.TextUnformatted($"SoftTargetScore: {state.SoftTargetScore:0.0}");
        ImGui.End();
    }

    private static void DrawSoftTargetHighlight(ImDrawListPtr drawList, Vector2 position)
    {
        const uint outer = 0xB000B0FF;
        const uint inner = 0xD0FFFFFF;
        drawList.AddCircle(position, 14f, outer, 32, 2.0f);
        drawList.AddCircle(position, 8f, inner, 32, 1.5f);
    }
}
