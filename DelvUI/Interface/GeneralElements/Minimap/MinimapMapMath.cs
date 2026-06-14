using Lumina.Excel.Sheets;
using System;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    internal static class MinimapMapMath
    {
        public const float MapTextureResolution = 2048f;
        public const float MapTextureCenter = 1024f;
        private const float MinUvHalfSpan = 0.008f;

        public static Vector2 WorldToMapTextureCoords(Vector3 worldPosition, int offsetX, int offsetY, uint sizeFactor)
        {
            var scale = sizeFactor / 100f;
            return new Vector2(
                ((worldPosition.X + offsetX) * scale) + MapTextureCenter,
                ((worldPosition.Z + offsetY) * scale) + MapTextureCenter);
        }

        public static Vector2 WorldToMapTextureCoords(Vector3 worldPosition, Map map) =>
            WorldToMapTextureCoords(worldPosition, map.OffsetX, map.OffsetY, map.SizeFactor);

        public static bool TryGetVisibleMapUvWindow(Vector2 mapTexturePixelCoords, float visibleRangeYalms, uint sizeFactor, out Vector2 uvMin, out Vector2 uvMax)
        {
            var halfPixels = visibleRangeYalms * (sizeFactor / 100f);
            var halfUv = Math.Max(halfPixels / MapTextureResolution, MinUvHalfSpan);
            var centerUv = mapTexturePixelCoords / MapTextureResolution;

            uvMin = new Vector2(centerUv.X - halfUv, centerUv.Y - halfUv);
            uvMax = new Vector2(centerUv.X + halfUv, centerUv.Y + halfUv);

            uvMin = ClampUv(uvMin);
            uvMax = ClampUv(uvMax);
            return uvMax.X > uvMin.X && uvMax.Y > uvMin.Y;
        }

        public static Vector2 MapTextureDeltaToScreenOffset(Vector2 mapTextureDelta, Vector2 mapUvMin, Vector2 mapUvMax, float contentHalf)
        {
            var halfSpanPixels = GetVisibleMapHalfSpanPixels(mapUvMin, mapUvMax);
            if (halfSpanPixels.X < 0.001f || halfSpanPixels.Y < 0.001f)
            {
                return Vector2.Zero;
            }

            var scaleX = contentHalf / halfSpanPixels.X;
            var scaleY = contentHalf / halfSpanPixels.Y;
            return new Vector2(mapTextureDelta.X * scaleX, mapTextureDelta.Y * scaleY);
        }

        public static Vector2 GetVisibleMapHalfSpanPixels(Vector2 mapUvMin, Vector2 mapUvMax) =>
            new(
                Math.Max((mapUvMax.X - mapUvMin.X) * MapTextureResolution * 0.5f, 0.001f),
                Math.Max((mapUvMax.Y - mapUvMin.Y) * MapTextureResolution * 0.5f, 0.001f));

        private static Vector2 ClampUv(Vector2 uv) =>
            new(Math.Clamp(uv.X, 0f, 1f), Math.Clamp(uv.Y, 0f, 1f));
    }
}
