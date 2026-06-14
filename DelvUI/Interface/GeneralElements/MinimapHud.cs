using DelvUI.Config;
using DelvUI.Config.Tree;
using DelvUI.Enums;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    public class MinimapHud : DraggableHudElement, IHudElementWithVisibilityConfig
    {
        private const float MinimapZoomStepYalms = 20f;
        private const float MinimapControlButtonSize = 26f;
        private const float MinimapControlPadding = 8f;
        private const float MinimapControlGap = 4f;
        private static readonly TimeSpan MinimapControlsGrace = TimeSpan.FromMilliseconds(220);

        private MinimapConfig Config => (MinimapConfig)_config;
        public VisibilityConfig VisibilityConfig => Config.VisibilityConfig;

        private readonly MinimapStateProvider _stateProvider;
        private bool _capturedNativeNorthLock;
        private bool _appliedNativeNorthLock;
        private bool _nativeNorthLockOriginal;
        private DateTime _minimapControlsVisibleUntil = DateTime.MinValue;

        public MinimapHud(MinimapConfig config, string displayName) : base(config, displayName)
        {
            _stateProvider = new MinimapStateProvider(config);
        }

        protected override (List<Vector2>, List<Vector2>) ChildrenPositionsAndSizes()
        {
            var clampedSize = MinimapLayout.ClampSize(Config.Size);
            var size = new Vector2(clampedSize, clampedSize);
            return (new List<Vector2> { Config.Position - (size * 0.5f) }, new List<Vector2> { size });
        }

        public override void DrawChildren(Vector2 origin)
        {
            if (!Config.Enabled)
            {
                EnsureNativeState(false);
                return;
            }

            EnsureNativeState(true);
            var center = origin + Config.Position;
            var snapshot = _stateProvider.Build();
            AddDrawAction(Config.StrataLevel, () =>
            {
                var draw = ImGui.GetWindowDrawList();
                MinimapRenderer.Draw(draw, Config, snapshot, center);
            });

            AddDrawAction(Config.StrataLevel, () =>
            {
                DrawControlsOverlay(center);
            });
        }

        protected override void InternalDispose()
        {
            EnsureNativeState(false);
            base.InternalDispose();
        }

        private void EnsureNativeState(bool enabled)
        {
            NativeMinimapVisibility.SetVisible(!enabled);

            if (!enabled)
            {
                RestoreNativeNorthLockState();
                return;
            }

            CaptureNativeNorthLockStateIfNeeded();
            MinimapNativeNorthLock.Apply(Config.NorthLock);
            _appliedNativeNorthLock = true;
        }

        private void CaptureNativeNorthLockStateIfNeeded()
        {
            if (_capturedNativeNorthLock)
            {
                return;
            }

            if (!MinimapNativeNorthLock.TryGetCurrent(out var currentNorthLock))
            {
                return;
            }

            _nativeNorthLockOriginal = currentNorthLock;
            _capturedNativeNorthLock = true;
        }

        private void RestoreNativeNorthLockState()
        {
            if (!_appliedNativeNorthLock)
            {
                return;
            }

            if (_capturedNativeNorthLock)
            {
                MinimapNativeNorthLock.Apply(_nativeNorthLockOriginal);
            }

            _appliedNativeNorthLock = false;
            _capturedNativeNorthLock = false;
            _nativeNorthLockOriginal = false;
        }

        private void DrawControlsOverlay(Vector2 center)
        {
            var mousePos = ImGui.GetMousePos();
            var minimapHovered = IsPointInMinimapArea(mousePos, center);
            if (minimapHovered)
            {
                _minimapControlsVisibleUntil = DateTime.UtcNow + MinimapControlsGrace;
            }

            var controlsVisible = minimapHovered || DateTime.UtcNow < _minimapControlsVisibleUntil;
            if (!controlsVisible)
            {
                return;
            }

            var controlsWidth = (MinimapControlButtonSize * 4f) + (MinimapControlGap * 3f) + (MinimapControlPadding * 2f);
            var controlsHeight = MinimapControlButtonSize + (MinimapControlPadding * 2f);
            var controlsPos = GetMinimapControlsPosition(center, controlsWidth, controlsHeight);

            var controlsFlags = ImGuiWindowFlags.NoDecoration |
                                ImGuiWindowFlags.NoSavedSettings |
                                ImGuiWindowFlags.NoFocusOnAppearing |
                                ImGuiWindowFlags.NoNav;

            ImGui.SetNextWindowPos(controlsPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(controlsWidth, controlsHeight), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(MinimapControlPadding, MinimapControlPadding));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xCC11161E);
            ImGui.PushStyleColor(ImGuiCol.Border, 0xAA4D5A6A);

            if (ImGui.Begin("AetherUI##MinimapControls", controlsFlags))
            {
                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                {
                    _minimapControlsVisibleUntil = DateTime.UtcNow + MinimapControlsGrace;
                }

                if (ImGui.Button("-##MinimapZoomOut", new Vector2(MinimapControlButtonSize, MinimapControlButtonSize)))
                {
                    Config.VisibleRangeYalms = MinimapLayout.ClampVisibleRange(Config.VisibleRangeYalms + MinimapZoomStepYalms);
                    ConfigurationManager.Instance.ForceNeedsSave();
                }

                ImGui.SameLine(0f, MinimapControlGap);
                if (ImGui.Button("+##MinimapZoomIn", new Vector2(MinimapControlButtonSize, MinimapControlButtonSize)))
                {
                    Config.VisibleRangeYalms = MinimapLayout.ClampVisibleRange(Config.VisibleRangeYalms - MinimapZoomStepYalms);
                    ConfigurationManager.Instance.ForceNeedsSave();
                }

                ImGui.SameLine(0f, MinimapControlGap);
                if (ImGui.Button("##MinimapSettings", new Vector2(MinimapControlButtonSize, MinimapControlButtonSize)))
                {
                    ToggleMinimapConfig();
                }
                DrawMinimapCogIcon(ImGui.GetWindowDrawList(), ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), 0xFFEAF2FF);

                ImGui.SameLine(0f, MinimapControlGap);
                var northLocked = Config.NorthLock;
                var pushedNorthStyles = northLocked;
                if (pushedNorthStyles)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, 0xFF335C92);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF3D70B3);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF4A83CB);
                }

                if (ImGui.Button("##MinimapNorthLock", new Vector2(MinimapControlButtonSize, MinimapControlButtonSize)))
                {
                    Config.NorthLock = !Config.NorthLock;
                    northLocked = Config.NorthLock;
                    ConfigurationManager.Instance.ForceNeedsSave();
                }

                DrawMinimapLockIcon(
                    ImGui.GetWindowDrawList(),
                    ImGui.GetItemRectMin(),
                    ImGui.GetItemRectMax(),
                    northLocked ? 0xFFBFE0FF : 0xFFEAF2FF);

                if (pushedNorthStyles)
                {
                    ImGui.PopStyleColor(3);
                }
            }

            ImGui.End();
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
        }

        private bool IsPointInMinimapArea(Vector2 point, Vector2 center)
        {
            var size = MinimapLayout.ClampSize(Config.Size);
            var half = size * 0.5f;
            if (Config.Square)
            {
                return Math.Abs(point.X - center.X) <= half &&
                       Math.Abs(point.Y - center.Y) <= half;
            }

            return Vector2.Distance(point, center) <= half;
        }

        private Vector2 GetMinimapControlsPosition(Vector2 center, float controlsWidth, float controlsHeight)
        {
            var size = MinimapLayout.ClampSize(Config.Size);
            var half = size * 0.5f;
            var mapMin = center - new Vector2(half, half);
            var mapMax = center + new Vector2(half, half);
            var borderInset = 6f;

            Vector2 pos;
            if (Config.Square)
            {
                pos = new Vector2(
                    mapMax.X - controlsWidth - borderInset,
                    mapMax.Y - controlsHeight - borderInset);
            }
            else
            {
                var dir = Vector2.Normalize(new Vector2(1f, 1f));
                var edgePoint = center + (dir * (half - borderInset));
                pos = new Vector2(
                    edgePoint.X - controlsWidth,
                    edgePoint.Y - controlsHeight);
            }

            var viewport = ImGui.GetMainViewport();
            var minX = viewport.Pos.X + 4f;
            var minY = viewport.Pos.Y + 4f;
            var maxX = viewport.Pos.X + viewport.Size.X - controlsWidth - 4f;
            var maxY = viewport.Pos.Y + viewport.Size.Y - controlsHeight - 4f;
            pos.X = Math.Clamp(pos.X, minX, maxX);
            pos.Y = Math.Clamp(pos.Y, minY, maxY);
            return pos;
        }

        private static void DrawMinimapCogIcon(ImDrawListPtr draw, Vector2 min, Vector2 max, uint color)
        {
            var iconCenter = (min + max) * 0.5f;
            var buttonSize = Math.Min(max.X - min.X, max.Y - min.Y);
            var toothInner = buttonSize * 0.20f;
            var toothOuter = buttonSize * 0.34f;

            for (var i = 0; i < 8; i++)
            {
                var angle = (MathF.PI * 2f * i) / 8f;
                var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                draw.AddLine(iconCenter + (dir * toothInner), iconCenter + (dir * toothOuter), color, 1.7f);
            }

            draw.AddCircle(iconCenter, buttonSize * 0.24f, color, 16, 1.8f);
            draw.AddCircle(iconCenter, buttonSize * 0.12f, color, 16, 1.6f);
        }

        private static void DrawMinimapLockIcon(ImDrawListPtr draw, Vector2 min, Vector2 max, uint color)
        {
            var iconCenter = (min + max) * 0.5f;
            var buttonSize = Math.Min(max.X - min.X, max.Y - min.Y);
            var bodyWidth = buttonSize * 0.50f;
            var bodyHeight = buttonSize * 0.31f;
            var bodyMin = new Vector2(iconCenter.X - (bodyWidth * 0.5f), iconCenter.Y + (buttonSize * 0.05f));
            var bodyMax = new Vector2(bodyMin.X + bodyWidth, bodyMin.Y + bodyHeight);
            draw.AddRect(bodyMin, bodyMax, color, 2.6f, ImDrawFlags.None, 1.8f);

            var shackleRadius = bodyWidth * 0.36f;
            var shackleCenter = new Vector2(iconCenter.X, bodyMin.Y - (buttonSize * 0.02f));
            draw.PathClear();
            draw.PathArcTo(shackleCenter, shackleRadius, MathF.PI, 0f, 14);
            draw.PathStroke(color, ImDrawFlags.None, 1.8f);
            var shackleLegTopY = shackleCenter.Y;
            var shackleLegBottomY = bodyMin.Y + (buttonSize * 0.015f);
            draw.AddLine(new Vector2(bodyMin.X + (bodyWidth * 0.18f), shackleLegTopY), new Vector2(bodyMin.X + (bodyWidth * 0.18f), shackleLegBottomY), color, 1.8f);
            draw.AddLine(new Vector2(bodyMax.X - (bodyWidth * 0.18f), shackleLegTopY), new Vector2(bodyMax.X - (bodyWidth * 0.18f), shackleLegBottomY), color, 1.8f);

            var keyholeTop = new Vector2(iconCenter.X, bodyMin.Y + (bodyHeight * 0.42f));
            draw.AddCircleFilled(keyholeTop, buttonSize * 0.052f, color, 10);
            draw.AddLine(
                keyholeTop + new Vector2(0f, buttonSize * 0.04f),
                keyholeTop + new Vector2(0f, buttonSize * 0.13f),
                color,
                1.6f);
        }

        private static void ToggleMinimapConfig()
        {
            if (ConfigurationManager.Instance.IsConfigWindowOpened)
            {
                ConfigurationManager.Instance.CloseConfigWindow();
                return;
            }

            BaseNode node = ConfigurationManager.Instance.ConfigBaseNode;
            node.SelectedOptionName = "Minimap";
            node.RefreshSelectedNode();
            ConfigurationManager.Instance.OpenConfigWindow();
        }
    }
}
