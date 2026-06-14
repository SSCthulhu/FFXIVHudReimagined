using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    internal static class MinimapRenderer
    {
        private const int CircularSegments = 48;
        private const float BlipClipScale = 0.84f;
        private const float Tau = MathF.PI * 2f;
        private const float CardinalTextScale = 1.0f;
        private const float CardinalOutlineRadius = 2.25f;

        public static void Draw(ImDrawListPtr draw, MinimapConfig config, MinimapSnapshot snapshot, Vector2 center)
        {
            if (!config.Enabled || !snapshot.IsActive)
            {
                return;
            }

            var size = MinimapLayout.ClampSize(config.Size);
            var half = size * 0.5f;
            var square = config.Square;
            var northLocked = config.NorthLock;
            var borderColor = config.BorderColor.Base;
            var mapYaw = ResolveMapYaw(snapshot, northLocked);

            DrawMapBackground(draw, center, half, snapshot, square, mapYaw, northLocked);
            if (config.ShowNativeMarkers)
            {
                DrawFateAreas(draw, center, half, snapshot, square, mapYaw, northLocked);
                DrawIconMarkers(draw, center, half, snapshot, square, mapYaw, northLocked);
            }

            DrawBlips(draw, center, half, snapshot, square, mapYaw, northLocked);
            DrawPlayerIndicator(draw, center, half, snapshot, config, northLocked);

            if (config.ShowCardinalDirections)
            {
                DrawCardinals(draw, center, half, northLocked, mapYaw);
            }

            var thickness = MinimapLayout.ClampBorderThickness(config.BorderThickness);
            if (thickness > 0.001f)
            {
                if (square && northLocked)
                {
                    draw.AddRect(center - new Vector2(half), center + new Vector2(half), borderColor, 10f, ImDrawFlags.None, thickness);
                }
                else if (square)
                {
                    var borderCorners = BuildRotatedCorners(center, half, mapYaw);
                    draw.AddPolyline(ref borderCorners[0], 4, borderColor, ImDrawFlags.Closed, thickness);
                }
                else
                {
                    draw.AddCircle(center, half, borderColor, CircularSegments, thickness);
                }
            }
        }

        private static void DrawMapBackground(
            ImDrawListPtr draw,
            Vector2 center,
            float half,
            MinimapSnapshot snapshot,
            bool square,
            float mapYaw,
            bool northLocked)
        {
            if (snapshot.HasMapTexture && MinimapTextureUtil.IsDrawable(snapshot.MapTexture))
            {
                var wrap = snapshot.MapTexture!.GetWrapOrEmpty();
                var min = center - new Vector2(half);
                var max = center + new Vector2(half);
                if (square && northLocked)
                {
                    draw.AddImageRounded(wrap.Handle, min, max, snapshot.MapUvMin, snapshot.MapUvMax, 0xFFFFFFFF, 10f);
                }
                else if (square)
                {
                    var corners = BuildRotatedCorners(center, half, mapYaw);
                    draw.AddImageQuad(
                        wrap.Handle,
                        corners[0], corners[1], corners[2], corners[3],
                        new Vector2(snapshot.MapUvMin.X, snapshot.MapUvMin.Y),
                        new Vector2(snapshot.MapUvMax.X, snapshot.MapUvMin.Y),
                        new Vector2(snapshot.MapUvMax.X, snapshot.MapUvMax.Y),
                        new Vector2(snapshot.MapUvMin.X, snapshot.MapUvMax.Y),
                        0xFFFFFFFF);
                }
                else
                {
                    if (northLocked)
                    {
                        draw.AddImageRounded(wrap.Handle, min, max, snapshot.MapUvMin, snapshot.MapUvMax, 0xFFFFFFFF, half);
                    }
                    else
                    {
                        DrawRotatedCircularMap(
                            draw,
                            wrap.Handle,
                            center,
                            half,
                            snapshot.MapUvMin,
                            snapshot.MapUvMax,
                            mapYaw);
                    }
                }
            }
            else
            {
                if (square && northLocked)
                {
                    draw.AddRectFilled(center - new Vector2(half), center + new Vector2(half), 0xC0101418, 10f);
                }
                else if (square)
                {
                    var corners = BuildRotatedCorners(center, half, mapYaw);
                    draw.AddQuadFilled(corners[0], corners[1], corners[2], corners[3], 0xC0101418);
                }
                else
                {
                    draw.AddCircleFilled(center, half, 0xC0101418, CircularSegments);
                }
            }
        }

        private static void DrawCardinals(ImDrawListPtr draw, Vector2 center, float half, bool northLocked, float mapYaw)
        {
            var nOffset = new Vector2(0, -half * 0.82f);
            var eOffset = new Vector2(half * 0.82f, 0);
            var sOffset = new Vector2(0, half * 0.82f);
            var wOffset = new Vector2(-half * 0.82f, 0);

            if (!northLocked)
            {
                nOffset = Rotate(nOffset, mapYaw);
                eOffset = Rotate(eOffset, mapYaw);
                sOffset = Rotate(sOffset, mapYaw);
                wOffset = Rotate(wOffset, mapYaw);
            }

            var n = center + nOffset;
            var e = center + eOffset;
            var s = center + sOffset;
            var w = center + wOffset;
            DrawCardinalLabel(draw, n, "N", 0xFF4AA8FF);
            DrawCardinalLabel(draw, e, "E", 0xFFD0D0D0);
            DrawCardinalLabel(draw, s, "S", 0xFFD0D0D0);
            DrawCardinalLabel(draw, w, "W", 0xFFD0D0D0);
        }

        private static void DrawCardinalLabel(ImDrawListPtr draw, Vector2 center, string text, uint color)
        {
            var baseFontSize = ImGui.GetFontSize();
            var fontSize = baseFontSize * CardinalTextScale;
            var textSize = ImGui.CalcTextSize(text) * CardinalTextScale;
            var textPos = center - (textSize * 0.5f);

            // Thick, readable black border.
            for (var i = 0; i < 8; i++)
            {
                var angle = (Tau * i) / 8f;
                var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * CardinalOutlineRadius;
                draw.AddText(ImGui.GetFont(), fontSize, textPos + offset, 0xFF000000, text);
            }

            draw.AddText(ImGui.GetFont(), fontSize, textPos, color, text);
        }

        private static void DrawBlips(ImDrawListPtr draw, Vector2 center, float half, MinimapSnapshot snapshot, bool square)
        {
            var clipRadius = half * BlipClipScale;
            foreach (var blip in snapshot.Blips)
            {
                var offset = blip.ScreenOffset;
                var pos = center + offset;
                if (!IsInsideShape(pos, center, clipRadius, square))
                {
                    continue;
                }

                draw.AddCircleFilled(pos, blip.Radius, blip.Color, 20);
                draw.AddCircle(pos, blip.Radius, 0xFF000000, 20, 1.5f);
            }
        }

        private static void DrawBlips(
            ImDrawListPtr draw,
            Vector2 center,
            float half,
            MinimapSnapshot snapshot,
            bool square,
            float mapYaw,
            bool northLocked)
        {
            var clipRadius = half * BlipClipScale;
            foreach (var blip in snapshot.Blips)
            {
                var offset = northLocked ? blip.ScreenOffset : Rotate(blip.ScreenOffset, mapYaw);
                var pos = center + offset;
                if (!IsInsideShape(pos, center, clipRadius, square))
                {
                    continue;
                }

                draw.AddCircleFilled(pos, blip.Radius, blip.Color, 20);
                draw.AddCircle(pos, blip.Radius, 0xFF000000, 20, 1.5f);
            }
        }

        private static void DrawFateAreas(
            ImDrawListPtr draw,
            Vector2 center,
            float half,
            MinimapSnapshot snapshot,
            bool square,
            float mapYaw,
            bool northLocked)
        {
            var clipRadius = half * BlipClipScale;
            foreach (var area in snapshot.FateAreas)
            {
                var offset = northLocked ? area.ScreenOffset : Rotate(area.ScreenOffset, mapYaw);
                var pos = center + offset;
                if (!IsInsideShape(pos, center, clipRadius, square))
                {
                    continue;
                }

                draw.AddCircleFilled(pos, area.RadiusPixels, 0x503878B8, 48);
                draw.AddCircle(pos, area.RadiusPixels, 0xFF5098E8, 48, 2f);
            }
        }

        private static void DrawIconMarkers(
            ImDrawListPtr draw,
            Vector2 center,
            float half,
            MinimapSnapshot snapshot,
            bool square,
            float mapYaw,
            bool northLocked)
        {
            var clipRadius = half * BlipClipScale;
            foreach (var marker in snapshot.IconMarkers)
            {
                if (marker.Texture is null)
                {
                    continue;
                }

                var wrap = marker.Texture.GetWrapOrEmpty();
                if (wrap.Handle == 0 || wrap.Width <= 0 || wrap.Height <= 0)
                {
                    continue;
                }

                var offset = northLocked ? marker.ScreenOffset : Rotate(marker.ScreenOffset, mapYaw);
                var pos = center + offset;
                if (!IsInsideShape(pos, center, clipRadius, square))
                {
                    continue;
                }

                var halfSize = marker.Size * 0.5f;
                draw.AddImage(wrap.Handle, pos - new Vector2(halfSize), pos + new Vector2(halfSize), Vector2.Zero, Vector2.One, 0xFFFFFFFF);
            }
        }

        private static void DrawPlayerIndicator(
            ImDrawListPtr draw,
            Vector2 center,
            float half,
            MinimapSnapshot snapshot,
            MinimapConfig config,
            bool northLocked)
        {
            var facingScale = MinimapLayout.ClampFacingConeSizeScale(config.FacingConeSizeScale);
            var facingOpacity = MinimapLayout.ClampFacingConeOpacity(config.FacingConeOpacity);
            var coneRadius = half * facingScale;
            var cameraDirection = GetCameraDirection(snapshot, northLocked);
            DrawFacingCone(draw, center, coneRadius, cameraDirection, facingOpacity);

            var pinColor = snapshot.PlayerPinFillColor;
            var pinSize = MinimapLayout.ClampPlayerPinSize(config.PlayerPinSize);
            var playerDirection = GetPlayerDirection(snapshot, northLocked);
            DrawPin(draw, center, pinSize, playerDirection, pinColor);
        }

        private static void DrawFacingCone(
            ImDrawListPtr draw,
            Vector2 center,
            float radius,
            Vector2 facingDirection,
            float opacity)
        {
            const int segments = 28;
            const float halfAngle = 43f * (MathF.PI / 180f);
            var normalized = facingDirection.LengthSquared() > 0.0001f
                ? Vector2.Normalize(facingDirection)
                : new Vector2(0f, -1f);
            var centerAngle = MathF.Atan2(normalized.X, normalized.Y);
            var fillColor = ApplyAlpha(0x99E85830, opacity);
            var strokeColor = ApplyAlpha(0xCCE85830, MathF.Min(opacity * 1.15f, 1f));
            draw.PathClear();
            draw.PathLineTo(center);
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = centerAngle - halfAngle + (t * 2f * halfAngle);
                var point = center + new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * radius;
                draw.PathLineTo(point);
            }

            draw.PathFillConvex(fillColor);
            draw.PathStroke(strokeColor, ImDrawFlags.Closed, 1.2f);
        }

        private static void DrawPin(ImDrawListPtr draw, Vector2 center, float radius, Vector2 facingDirection, uint fillColor)
        {
            var facing = facingDirection.LengthSquared() > 0.0001f
                ? Vector2.Normalize(facingDirection)
                : new Vector2(0f, -1f);

            var facingAngle = MathF.Atan2(facing.X, facing.Y);
            var bulbRadius = radius * 1.05f;
            var bulbOffset = radius * 0.48f;
            var tipDistance = radius * 1.55f;
            var backCenter = center - (facing * bulbOffset);
            var tip = center + (facing * tipDistance);

            const int arcSegments = 28;
            const float arcHalfSpan = MathF.PI * 0.78f;
            var arcStart = facingAngle + MathF.PI - arcHalfSpan;
            var arcEnd = facingAngle + MathF.PI + arcHalfSpan;

            draw.PathClear();
            for (var i = 0; i <= arcSegments; i++)
            {
                var t = i / (float)arcSegments;
                var angle = arcStart + (t * (arcEnd - arcStart));
                var point = backCenter + new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * bulbRadius;
                draw.PathLineTo(point);
            }
            draw.PathLineTo(tip);
            draw.PathStroke(0xFF000000, ImDrawFlags.Closed, MathF.Max(2f, radius * 0.30f));

            draw.PathClear();
            for (var i = 0; i <= arcSegments; i++)
            {
                var t = i / (float)arcSegments;
                var angle = arcStart + (t * (arcEnd - arcStart));
                var point = backCenter + new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * bulbRadius;
                draw.PathLineTo(point);
            }
            draw.PathLineTo(tip);
            draw.PathFillConvex(fillColor);
        }

        private static Vector2 GetCameraDirection(MinimapSnapshot snapshot, bool northLocked)
        {
            if (!northLocked)
            {
                return new Vector2(0f, -1f);
            }

            var yaw = snapshot.HasCameraMapYaw
                ? snapshot.CameraMapYaw + MathF.PI
                : snapshot.PlayerYaw;
            return new Vector2(MathF.Sin(yaw), MathF.Cos(yaw));
        }

        private static Vector2 GetPlayerDirection(MinimapSnapshot snapshot, bool northLocked)
        {
            if (!northLocked)
            {
                return new Vector2(0f, -1f);
            }

            return new Vector2(MathF.Sin(snapshot.PlayerYaw), MathF.Cos(snapshot.PlayerYaw));
        }

        private static float ResolveMapYaw(MinimapSnapshot snapshot, bool northLocked)
        {
            if (northLocked)
            {
                return 0f;
            }

            if (snapshot.HasNativeMapFrame && !snapshot.NativeNorthLockedUp)
            {
                return snapshot.NativeMapImageRotation;
            }

            return -snapshot.PlayerYaw;
        }

        private static Vector2 Rotate(Vector2 point, float radians)
        {
            var sin = MathF.Sin(radians);
            var cos = MathF.Cos(radians);
            return new Vector2(
                (point.X * cos) - (point.Y * sin),
                (point.X * sin) + (point.Y * cos));
        }

        private static Vector2[] BuildRotatedCorners(Vector2 center, float half, float yaw)
        {
            var corners = new[]
            {
                new Vector2(-half, -half),
                new Vector2(half, -half),
                new Vector2(half, half),
                new Vector2(-half, half),
            };

            return new[]
            {
                center + Rotate(corners[0], yaw),
                center + Rotate(corners[1], yaw),
                center + Rotate(corners[2], yaw),
                center + Rotate(corners[3], yaw)
            };
        }

        private static void DrawRotatedCircularMap(
            ImDrawListPtr draw,
            ImTextureID textureHandle,
            Vector2 center,
            float radius,
            Vector2 uvMin,
            Vector2 uvMax,
            float yaw)
        {
            var centerUv = LocalOffsetToUv(Vector2.Zero, radius, uvMin, uvMax);
            for (var i = 0; i < CircularSegments; i++)
            {
                var angle0 = (i / (float)CircularSegments) * Tau;
                var angle1 = ((i + 1) / (float)CircularSegments) * Tau;
                var offset0 = new Vector2(MathF.Sin(angle0) * radius, -MathF.Cos(angle0) * radius);
                var offset1 = new Vector2(MathF.Sin(angle1) * radius, -MathF.Cos(angle1) * radius);

                var sample0 = Rotate(offset0, -yaw);
                var sample1 = Rotate(offset1, -yaw);
                var uv0 = LocalOffsetToUv(sample0, radius, uvMin, uvMax);
                var uv1 = LocalOffsetToUv(sample1, radius, uvMin, uvMax);

                draw.AddImageQuad(
                    textureHandle,
                    center,
                    center + offset0,
                    center + offset1,
                    center + offset1,
                    centerUv,
                    uv0,
                    uv1,
                    uv1,
                    0xFFFFFFFF);
            }
        }

        private static Vector2 LocalOffsetToUv(Vector2 localOffset, float half, Vector2 uvMin, Vector2 uvMax)
        {
            var u = (localOffset.X / half + 1f) * 0.5f;
            var v = (localOffset.Y / half + 1f) * 0.5f;
            return new Vector2(
                uvMin.X + (u * (uvMax.X - uvMin.X)),
                uvMin.Y + (v * (uvMax.Y - uvMin.Y)));
        }

        private static bool IsInsideShape(Vector2 point, Vector2 center, float halfExtent, bool square)
        {
            if (square)
            {
                return MathF.Abs(point.X - center.X) <= halfExtent && MathF.Abs(point.Y - center.Y) <= halfExtent;
            }

            return Vector2.Distance(point, center) <= halfExtent;
        }

        private static uint ApplyAlpha(uint colorAbgr, float alpha)
        {
            var clampedAlpha = Math.Clamp(alpha, 0f, 1f);
            var srcAlpha = (byte)((colorAbgr >> 24) & 0xFF);
            var resultAlpha = (byte)Math.Clamp((int)MathF.Round(srcAlpha * clampedAlpha), 0, 255);
            return (colorAbgr & 0x00FFFFFF) | ((uint)resultAlpha << 24);
        }
    }
}
