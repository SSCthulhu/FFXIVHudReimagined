using System.Numerics;
using System.Collections.Generic;

namespace DelvUI.Interface.GeneralElements
{
    internal static class MinimapMarkerPlacement
    {
        public static bool TryGetMarkerScreenOffset(
            float markerWorldX,
            float markerWorldZ,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            out Vector2 screenOffset)
        {
            screenOffset = Vector2.Zero;
            if (!float.IsFinite(markerWorldX) || !float.IsFinite(markerWorldZ))
            {
                return false;
            }

            var playerTex = MinimapMapMath.WorldToMapTextureCoords(playerPosition, offsetX, offsetY, sizeFactor);
            var markerTex = MinimapMapMath.WorldToMapTextureCoords(new Vector3(markerWorldX, 0f, markerWorldZ), offsetX, offsetY, sizeFactor);
            var texDelta = markerTex - playerTex;
            var maxTexDistance = visibleRangeYalms * (sizeFactor / 100f);
            if (texDelta.Length() > maxTexDistance)
            {
                return false;
            }

            screenOffset = MinimapMapMath.MapTextureDeltaToScreenOffset(texDelta, mapUvMin, mapUvMax, contentHalf);
            return true;
        }

        public static bool TryAddIconMarker(
            float markerWorldX,
            float markerWorldZ,
            uint iconId,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            float markerIconSize,
            MinimapMarkerIconCache iconCache,
            List<MinimapIconMarker> markers)
        {
            if (iconId == 0 || !float.IsFinite(markerWorldX) || !float.IsFinite(markerWorldZ))
            {
                return false;
            }

            if (!TryGetMarkerScreenOffset(
                    markerWorldX,
                    markerWorldZ,
                    playerPosition,
                    offsetX,
                    offsetY,
                    sizeFactor,
                    visibleRangeYalms,
                    contentHalf,
                    mapUvMin,
                    mapUvMax,
                    out var screenOffset))
            {
                return false;
            }

            if (!iconCache.TryGetDrawableIcon(iconId, out var texture))
            {
                return false;
            }

            markers.Add(new MinimapIconMarker
            {
                UsesNativeScreenOffset = true,
                ScreenOffset = screenOffset,
                IconId = iconId,
                Size = markerIconSize,
                Texture = texture
            });
            return true;
        }
    }
}
