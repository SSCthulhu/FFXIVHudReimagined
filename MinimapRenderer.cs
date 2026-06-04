using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace FFXIVHudPlugin;

public static class MinimapRenderer
{
    private readonly struct MapFrameTransform
    {
        public float Yaw { get; init; }
        public bool FlipX { get; init; }
        public bool FlipY { get; init; }

        public bool HasRotationOrFlip =>
            MathF.Abs(Yaw) > 0.0001f || FlipX || FlipY;
    }

    private const float SquareCornerRounding = 10f;
    private const float NorthRingRadiusScale = 0.92f;
    private const float BlipClipScale = 0.84f;
    internal const float BlipClipScaleForMarkers = BlipClipScale;
    private const int CircularMapSegments = 48;

    public static void Draw(
        ImDrawListPtr draw,
        HudConfiguration config,
        MinimapSnapshot snapshot,
        Vector2 center,
        float alpha)
    {
        if (!config.MinimapEnabled || !snapshot.IsActive)
        {
            return;
        }

        var size = MinimapLayout.ClampSize(config.MinimapSize);
        var contentHalf = size * 0.5f;
        var northLocked = config.MinimapNorthLocked;
        var frame = ResolveMapFrameTransform(northLocked, snapshot);
        var circular = !config.MinimapSquare;
        var visibleRange = MinimapLayout.ClampVisibleRange(config.MinimapVisibleRangeYalms);
        var pixelsPerYalm = (contentHalf * 0.86f) / visibleRange;
        var frameCorners = BuildFrameCorners(center, contentHalf, frame);

        var hasMap = DrawMapBackground(draw, center, contentHalf, snapshot, frame, alpha, circular);
        if (!hasMap)
        {
            DrawFallbackBackground(draw, center, contentHalf, frame, alpha, circular);
        }

        DrawNorthIndicatorRing(
            draw,
            center,
            contentHalf * NorthRingRadiusScale,
            northLocked,
            frame,
            alpha,
            config.MinimapSquare);
        if (config.MinimapShowNativeMarkers)
        {
            DrawIconMarkers(draw, center, contentHalf, snapshot, frame, northLocked, alpha, config.MinimapSquare);
        }

        DrawBlips(draw, center, contentHalf, snapshot, pixelsPerYalm, frame, northLocked, alpha, config.MinimapSquare);
        DrawMapTitle(draw, frameCorners, snapshot.MapTitle, alpha);
        DrawPlayerIndicator(
            draw,
            center,
            contentHalf,
            snapshot,
            northLocked,
            alpha,
            MinimapLayout.ClampFacingConeSizeScale(config.MinimapFacingConeSizeScale),
            MinimapLayout.ClampFacingConeOpacity(config.MinimapFacingConeOpacity));

        DrawOuterBorder(
            draw,
            center,
            contentHalf,
            frameCorners,
            circular,
            config.MinimapSquare && northLocked,
            MinimapLayout.ClampBorderThickness(config.MinimapBorderThickness),
            config.MinimapBorderColor,
            alpha);
    }

    private static void DrawOuterBorder(
        ImDrawListPtr draw,
        Vector2 center,
        float contentHalf,
        Vector2[] frameCorners,
        bool circular,
        bool roundedAxisAlignedSquare,
        float thickness,
        uint borderColorArgb,
        float alpha)
    {
        if (thickness <= 0.001f)
        {
            return;
        }

        var borderColor = ApplyAlpha(borderColorArgb, alpha);
        if (circular)
        {
            draw.AddCircle(center, contentHalf, borderColor, CircularMapSegments, thickness);
            return;
        }

        DrawFrameBorder(draw, frameCorners, borderColor, roundedAxisAlignedSquare, thickness);
    }

    private static bool DrawMapBackground(
        ImDrawListPtr draw,
        Vector2 center,
        float contentHalf,
        MinimapSnapshot snapshot,
        MapFrameTransform frame,
        float alpha,
        bool circular)
    {
        if (!snapshot.HasMapTexture || snapshot.MapTexture is null)
        {
            return false;
        }

        if (!snapshot.MapTexture.TryGetWrap(out var wrap, out _) || wrap.Handle == 0 || wrap.Width <= 0 || wrap.Height <= 0)
        {
            wrap = snapshot.MapTexture.GetWrapOrDefault();
            if (wrap.Handle == 0 || wrap.Width <= 0 || wrap.Height <= 0)
            {
                return false;
            }
        }

        var uvMin = snapshot.MapUvMin;
        var uvMax = snapshot.MapUvMax;
        var tint = ApplyAlpha(0xFFFFFFFF, alpha);

        if (circular)
        {
            if (!frame.HasRotationOrFlip)
            {
                DrawMapImageRounded(draw, wrap.Handle, center, contentHalf, uvMin, uvMax, tint);
            }
            else
            {
                DrawMapImageCircularFan(draw, wrap.Handle, center, contentHalf, uvMin, uvMax, frame, tint);
            }

            return true;
        }

        var screenCorners = BuildFrameCorners(center, contentHalf, frame);
        draw.AddImageQuad(
            wrap.Handle,
            screenCorners[0],
            screenCorners[1],
            screenCorners[2],
            screenCorners[3],
            new Vector2(uvMin.X, uvMin.Y),
            new Vector2(uvMax.X, uvMin.Y),
            new Vector2(uvMax.X, uvMax.Y),
            new Vector2(uvMin.X, uvMax.Y),
            tint);
        return true;
    }

    private static void DrawMapImageRounded(
        ImDrawListPtr draw,
        ImTextureID textureHandle,
        Vector2 center,
        float radius,
        Vector2 uvMin,
        Vector2 uvMax,
        uint tint)
    {
        var min = center - new Vector2(radius, radius);
        var max = center + new Vector2(radius, radius);
        draw.AddImageRounded(textureHandle, min, max, uvMin, uvMax, tint, radius);
    }

    private static void DrawMapImageCircularFan(
        ImDrawListPtr draw,
        ImTextureID textureHandle,
        Vector2 center,
        float radius,
        Vector2 uvMin,
        Vector2 uvMax,
        MapFrameTransform frame,
        uint tint)
    {
        var centerUv = LocalOffsetToMapUv(Vector2.Zero, radius, uvMin, uvMax);
        for (var i = 0; i < CircularMapSegments; i++)
        {
            var angle0 = (i / (float)CircularMapSegments) * MathF.Tau;
            var angle1 = ((i + 1) / (float)CircularMapSegments) * MathF.Tau;
            var offset0 = new Vector2(MathF.Sin(angle0) * radius, -MathF.Cos(angle0) * radius);
            var offset1 = new Vector2(MathF.Sin(angle1) * radius, -MathF.Cos(angle1) * radius);
            var pos0 = center + offset0;
            var pos1 = center + offset1;
            var uv0 = LocalOffsetToMapUv(TransformMapSampleOffset(offset0, frame), radius, uvMin, uvMax);
            var uv1 = LocalOffsetToMapUv(TransformMapSampleOffset(offset1, frame), radius, uvMin, uvMax);

            draw.AddImageQuad(
                textureHandle,
                center,
                pos0,
                pos1,
                pos1,
                centerUv,
                uv0,
                uv1,
                uv1,
                tint);
        }
    }

    private static Vector2 LocalOffsetToMapUv(Vector2 localOffset, float half, Vector2 uvMin, Vector2 uvMax)
    {
        var u = (localOffset.X / half + 1f) * 0.5f;
        var v = (localOffset.Y / half + 1f) * 0.5f;
        return new Vector2(
            uvMin.X + (u * (uvMax.X - uvMin.X)),
            uvMin.Y + (v * (uvMax.Y - uvMin.Y)));
    }

    private static void DrawFallbackBackground(
        ImDrawListPtr draw,
        Vector2 center,
        float contentHalf,
        MapFrameTransform frame,
        float alpha,
        bool circular)
    {
        var backgroundColor = ApplyAlpha(0xC0101418, alpha);
        if (circular)
        {
            draw.AddCircleFilled(center, contentHalf, backgroundColor);
            return;
        }

        var corners = BuildFrameCorners(center, contentHalf, frame);
        draw.AddQuadFilled(corners[0], corners[1], corners[2], corners[3], backgroundColor);
    }

    private static void DrawFrameBorder(
        ImDrawListPtr draw,
        Vector2[] corners,
        uint color,
        bool roundedAxisAligned,
        float thickness)
    {
        if (roundedAxisAligned)
        {
            GetAxisAlignedBounds(corners, out var min, out var max);
            draw.AddRect(min, max, color, SquareCornerRounding, ImDrawFlags.None, thickness);
            return;
        }

        draw.AddPolyline(ref corners[0], 4, color, ImDrawFlags.Closed, thickness);
    }

    private static void DrawNorthIndicatorRing(
        ImDrawListPtr draw,
        Vector2 center,
        float radius,
        bool northLocked,
        MapFrameTransform frame,
        float alpha,
        bool square)
    {
        var ringColor = ApplyAlpha(0x40485868, alpha);
        if (square)
        {
            var innerHalf = radius * 0.82f;
            var innerCorners = BuildFrameCorners(center, innerHalf, frame);
            draw.AddPolyline(ref innerCorners[0], 4, ringColor, ImDrawFlags.Closed, 1f);
        }
        else
        {
            draw.AddCircle(center, radius, ringColor, CircularMapSegments, 1.2f);
            draw.AddCircle(center, radius * 0.55f, ApplyAlpha(0x28303840, alpha), 32, 1f);
        }

        var northOffset = northLocked
            ? new Vector2(0f, -radius * 0.88f)
            : TransformMapLocalOffset(new Vector2(0f, -radius * 0.88f), frame);
        var northPos = center + northOffset;
        draw.AddTriangleFilled(
            northPos + new Vector2(0f, -6f),
            northPos + new Vector2(-5f, 4f),
            northPos + new Vector2(5f, 4f),
            ApplyAlpha(0xFFE8C070, alpha));
    }

    private static void DrawIconMarkers(
        ImDrawListPtr draw,
        Vector2 center,
        float contentHalf,
        MinimapSnapshot snapshot,
        MapFrameTransform frame,
        bool northLocked,
        float alpha,
        bool square)
    {
        var clipRadius = contentHalf * BlipClipScale;
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

            var screenOffset = marker.UsesNativeScreenOffset
                ? marker.ScreenOffset
                : MinimapMapMath.MapTextureDeltaToScreenOffset(
                    marker.MapTextureDelta,
                    snapshot.MapUvMin,
                    snapshot.MapUvMax,
                    contentHalf);
            if (!marker.UsesNativeScreenOffset && !northLocked)
            {
                screenOffset = TransformMapLocalOffset(screenOffset, frame);
            }

            var pos = center + screenOffset;
            if (square && !IsInsideSquare(pos, center, clipRadius))
            {
                continue;
            }

            if (!square && Vector2.Distance(pos, center) > clipRadius)
            {
                continue;
            }

            var half = marker.Size * 0.5f;
            draw.AddImage(
                wrap.Handle,
                pos - new Vector2(half, half),
                pos + new Vector2(half, half),
                Vector2.Zero,
                Vector2.One,
                ApplyAlpha(0xFFFFFFFF, alpha));
        }
    }

    private static void DrawBlips(
        ImDrawListPtr draw,
        Vector2 center,
        float contentHalf,
        MinimapSnapshot snapshot,
        float pixelsPerYalm,
        MapFrameTransform frame,
        bool northLocked,
        float alpha,
        bool square)
    {
        var clipRadius = contentHalf * BlipClipScale;
        foreach (var blip in snapshot.Blips)
        {
            var screenOffset = blip.LocalOffset * pixelsPerYalm;
            if (!northLocked)
            {
                screenOffset = TransformMapLocalOffset(screenOffset, frame);
            }

            var pos = center + new Vector2(screenOffset.X, -screenOffset.Y);
            if (square && !IsInsideSquare(pos, center, clipRadius))
            {
                continue;
            }

            if (!square && Vector2.Distance(pos, center) > clipRadius)
            {
                continue;
            }

            draw.AddCircleFilled(pos, blip.Radius, ApplyAlpha(blip.Color, alpha));
        }
    }

    private static void DrawPlayerIndicator(
        ImDrawListPtr draw,
        Vector2 center,
        float contentHalf,
        MinimapSnapshot snapshot,
        bool northLocked,
        float alpha,
        float facingConeSizeScale,
        float facingConeOpacity)
    {
        var pinColor = ApplyAlpha(0xFF2F9BFF, alpha);
        var outlineColor = ApplyAlpha(0xFF0E1E2E, alpha);
        const float pinRadius = 7.5f;

        var facing = GetScreenFacingDirection(snapshot, northLocked);
        DrawFacingCone(
            draw,
            center,
            facing,
            contentHalf * facingConeSizeScale,
            alpha * facingConeOpacity);

        draw.AddCircleFilled(center, pinRadius + 1.2f, outlineColor);
        draw.AddCircleFilled(center, pinRadius, pinColor);
    }

    private static void DrawFacingCone(
        ImDrawListPtr draw,
        Vector2 center,
        Vector2 facing,
        float radius,
        float alpha)
    {
        if (alpha <= 0.001f || radius <= 0.5f)
        {
            return;
        }

        const int segments = 28;
        const float halfAngleRadians = 43f * (MathF.PI / 180f);
        var facingAngle = MathF.Atan2(facing.X, facing.Y);
        var fillColor = ApplyAlpha(0x99E85830, alpha);
        var edgeColor = ApplyAlpha(0xCCE85830, MathF.Min(alpha * 1.35f, 1f));

        draw.PathClear();
        draw.PathLineTo(center);
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var angle = facingAngle - halfAngleRadians + (t * 2f * halfAngleRadians);
            var point = center + new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * radius;
            draw.PathLineTo(point);
        }

        draw.PathFillConvex(fillColor);
        draw.PathStroke(edgeColor, ImDrawFlags.Closed, 1.2f);
    }

    /// <summary>
    /// Screen-space facing on the minimap (ImGui Y-down). North-locked uses camera view; rotating mode stays fixed up.
    /// </summary>
    private static Vector2 GetScreenFacingDirection(MinimapSnapshot snapshot, bool northLocked)
    {
        if (!northLocked)
        {
            return new Vector2(0f, -1f);
        }

        var yaw = ResolveNorthLockedConeYaw(snapshot);
        var direction = new Vector2(MathF.Sin(yaw), MathF.Cos(yaw));
        return direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : new Vector2(0f, 1f);
    }

    private static float ResolveNorthLockedConeYaw(MinimapSnapshot snapshot)
    {
        if (snapshot.HasCameraMapYaw)
        {
            // View-matrix azimuth points opposite the minimap cone art orientation.
            return snapshot.CameraMapYaw + MathF.PI;
        }

        return snapshot.PlayerYaw;
    }

    private static void DrawMapTitle(ImDrawListPtr draw, Vector2[] frameCorners, string mapTitle, float alpha)
    {
        if (string.IsNullOrWhiteSpace(mapTitle))
        {
            return;
        }

        GetAxisAlignedBounds(frameCorners, out var min, out var max);
        var font = ImGui.GetFont();
        var fontSize = ImGui.GetFontSize() * 0.85f;
        var textSize = fontSize > 0f
            ? ImGui.CalcTextSize(mapTitle) * (fontSize / ImGui.GetFontSize())
            : ImGui.CalcTextSize(mapTitle);
        var pos = new Vector2(min.X + 6f, max.Y - textSize.Y - 4f);
        draw.AddText(font, fontSize, pos, ApplyAlpha(0xB0FFFFFF, alpha), mapTitle);
    }

    private static MapFrameTransform ResolveMapFrameTransform(bool northLocked, MinimapSnapshot snapshot)
    {
        if (northLocked)
        {
            return default;
        }

        if (snapshot.HasNativeMapFrame && !snapshot.NativeNorthLockedUp)
        {
            return new MapFrameTransform
            {
                Yaw = snapshot.NativeMapImageRotation,
                FlipX = snapshot.NativeMapImageScaleX < 0f,
                FlipY = snapshot.NativeMapImageScaleY < 0f,
            };
        }

        return new MapFrameTransform
        {
            Yaw = -snapshot.PlayerYaw,
            FlipY = true,
        };
    }

    private static Vector2 TransformMapLocalOffset(Vector2 offset, MapFrameTransform frame)
    {
        if (frame.FlipX)
        {
            offset.X = -offset.X;
        }

        if (frame.FlipY)
        {
            offset.Y = -offset.Y;
        }

        return RotateOffset(offset, frame.Yaw);
    }

    private static Vector2 TransformMapSampleOffset(Vector2 offset, MapFrameTransform frame)
    {
        if (frame.FlipX)
        {
            offset.X = -offset.X;
        }

        if (frame.FlipY)
        {
            offset.Y = -offset.Y;
        }

        return RotateOffset(offset, -frame.Yaw);
    }

    private static Vector2[] BuildFrameCorners(Vector2 center, float half, MapFrameTransform frame)
    {
        var localCorners = new[]
        {
            new Vector2(-half, -half),
            new Vector2(half, -half),
            new Vector2(half, half),
            new Vector2(-half, half),
        };

        var screenCorners = new Vector2[4];
        for (var i = 0; i < localCorners.Length; i++)
        {
            screenCorners[i] = center + TransformMapLocalOffset(localCorners[i], frame);
        }

        return screenCorners;
    }

    private static void GetAxisAlignedBounds(Vector2[] corners, out Vector2 min, out Vector2 max)
    {
        min = corners[0];
        max = corners[0];
        for (var i = 1; i < corners.Length; i++)
        {
            min = Vector2.Min(min, corners[i]);
            max = Vector2.Max(max, corners[i]);
        }
    }

    private static Vector2 RotateOffset(Vector2 offset, float yaw)
    {
        var sin = MathF.Sin(yaw);
        var cos = MathF.Cos(yaw);
        return new Vector2(
            (offset.X * cos) - (offset.Y * sin),
            (offset.X * sin) + (offset.Y * cos));
    }

    private static bool IsInsideSquare(Vector2 point, Vector2 center, float halfExtent)
    {
        return MathF.Abs(point.X - center.X) <= halfExtent &&
               MathF.Abs(point.Y - center.Y) <= halfExtent;
    }

    private static uint ApplyAlpha(uint rgba, float alpha)
    {
        var a = (byte)(Math.Clamp(alpha, 0f, 1f) * ((rgba >> 24) & 0xFF));
        return (rgba & 0x00FFFFFF) | ((uint)a << 24);
    }
}
