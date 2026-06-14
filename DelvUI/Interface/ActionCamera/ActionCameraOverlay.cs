using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace DelvUI.Interface.ActionCamera
{
    internal static class ActionCameraOverlay
    {
        public static void Draw(ActionCameraConfig config, ActionCameraRuntimeState state)
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
                DrawReticle(drawList, center, config);
            }

            if (config.ShowDebugOverlay)
            {
                DrawDebugWindow(state);
            }
        }

        private static void DrawReticle(ImDrawListPtr drawList, Vector2 center, ActionCameraConfig config)
        {
            float size = config.ReticleSize;
            bool targetLocked = ActionCameraManager.Instance?.HasLockedTarget == true;
            uint color = targetLocked ? 0xFFE7B85A : config.ReticleColor.Base;
            drawList.AddLine(new Vector2(center.X - size, center.Y), new Vector2(center.X + size, center.Y), color, 1.5f);
            drawList.AddLine(new Vector2(center.X, center.Y - size), new Vector2(center.X, center.Y + size), color, 1.5f);

            if (targetLocked)
            {
                drawList.AddCircle(center, size + 5f, color, 32, 1.6f);
            }
        }

        private static void DrawDebugWindow(ActionCameraRuntimeState state)
        {
            ImGui.SetNextWindowBgAlpha(0.55f);
            if (!ImGui.Begin("Aether UI Action Camera Debug", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
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
            ImGui.End();
        }
    }
}
