using DelvUI.Config;
using DelvUI.Config.Attributes;
using DelvUI.Enums;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    [Section("Minimap")]
    [SubSection("General", 0)]
    public class MinimapConfig : MovablePluginConfigObject
    {
        [Checkbox("Square Minimap")]
        [Order(6)]
        public bool Square = false;

        [Checkbox("North Lock")]
        [Order(7)]
        public bool NorthLock = false;

        [DragFloat("Size", min = MinimapLayout.MinSize, max = MinimapLayout.MaxSize, velocity = 1f)]
        [Order(8)]
        public float Size = MinimapLayout.DefaultSize;

        [DragFloat("Visible Range (yalms)", min = MinimapLayout.MinVisibleRangeYalms, max = MinimapLayout.MaxVisibleRangeYalms, velocity = 0.5f)]
        [Order(9)]
        public float VisibleRangeYalms = MinimapLayout.DefaultVisibleRangeYalms;

        [DragFloat("Facing Cone Size", min = MinimapLayout.MinFacingConeSizeScale, max = MinimapLayout.MaxFacingConeSizeScale, velocity = 0.01f)]
        [Order(10)]
        public float FacingConeSizeScale = MinimapLayout.DefaultFacingConeSizeScale;

        [DragFloat("Facing Cone Opacity", min = MinimapLayout.MinFacingConeOpacity, max = MinimapLayout.MaxFacingConeOpacity, velocity = 0.01f)]
        [Order(11)]
        public float FacingConeOpacity = MinimapLayout.DefaultFacingConeOpacity;

        [DragFloat("Border Thickness", min = MinimapLayout.MinBorderThickness, max = MinimapLayout.MaxBorderThickness, velocity = 0.1f)]
        [Order(12)]
        public float BorderThickness = MinimapLayout.DefaultBorderThickness;

        [ColorEdit4("Border Color")]
        [Order(13)]
        public PluginConfigColor BorderColor = PluginConfigColor.FromHex(MinimapLayout.DefaultBorderColor);

        [Checkbox("Show Native Markers")]
        [Order(14)]
        public bool ShowNativeMarkers = true;

        [Checkbox("Show Cardinal Directions")]
        [Order(15)]
        public bool ShowCardinalDirections = false;

        // Kept for backward-compatible config deserialization, but hidden from settings UI.
        public bool ShowDiagnostics = false;

        [DragFloat("Marker Icon Size", min = MinimapLayout.MinMarkerIconSize, max = MinimapLayout.MaxMarkerIconSize, velocity = 0.5f)]
        [Order(17)]
        public float MarkerIconSize = MinimapLayout.DefaultMarkerIconSize;

        [DragFloat("Player Pin Size", min = MinimapLayout.MinPlayerPinSize, max = MinimapLayout.MaxPlayerPinSize, velocity = 0.25f)]
        [Order(18)]
        public float PlayerPinSize = MinimapLayout.DefaultPlayerPinSize;

        [Checkbox("Use Role Pin Color")]
        [Order(19)]
        public bool UseRolePinColor = true;

        [Checkbox("Use Job Pin Color")]
        [Order(20)]
        public bool UseJobPinColor = false;

        public PluginConfigColor PlayerPinColor = PluginConfigColor.FromHex(MinimapLayout.DefaultPlayerPinColor);

        [NestedConfig("Visibility", 70)]
        public VisibilityConfig VisibilityConfig = new VisibilityConfig();

        [ManualDraw]
        [ManualDrawPriority(21)]
        [ManualDrawParent(nameof(Enabled))]
        public bool DrawCustomPinColor(ref bool changed)
        {
            if (UseRolePinColor || UseJobPinColor)
            {
                return false;
            }

            var colorVector = PlayerPinColor.Vector;
            if (ImGui.ColorEdit4("Custom Pin Color", ref colorVector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar))
            {
                PlayerPinColor.Vector = colorVector;
                changed = true;
            }

            return false;
        }

        public new static MinimapConfig DefaultConfig()
        {
            return new MinimapConfig
            {
                Enabled = false,
                Position = new Vector2(MinimapLayout.DefaultOffsetX, MinimapLayout.DefaultOffsetY),
                Strata = StrataLevel.HIGH,
                Size = MinimapLayout.DefaultSize,
                VisibleRangeYalms = MinimapLayout.DefaultVisibleRangeYalms,
                FacingConeSizeScale = MinimapLayout.DefaultFacingConeSizeScale,
                FacingConeOpacity = MinimapLayout.DefaultFacingConeOpacity,
                BorderThickness = MinimapLayout.DefaultBorderThickness,
                BorderColor = PluginConfigColor.FromHex(MinimapLayout.DefaultBorderColor),
                ShowNativeMarkers = true,
                ShowCardinalDirections = false,
                ShowDiagnostics = false,
                MarkerIconSize = MinimapLayout.DefaultMarkerIconSize,
                PlayerPinSize = MinimapLayout.DefaultPlayerPinSize,
                UseRolePinColor = true,
                UseJobPinColor = false,
                PlayerPinColor = PluginConfigColor.FromHex(MinimapLayout.DefaultPlayerPinColor)
            };
        }
    }
}
