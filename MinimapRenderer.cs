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
    private const int FateAreaSegments = 56;
    private const uint FateAreaFillColor = 0x503878B8;
    private const uint FateAreaRingColor = 0xFF5098E8;
    private const float FateAreaFillAlphaScale = 0.32f;
    private const float FateAreaRingThickness = 2f;
    private const float PartyBlipOutlineThickness = 2f;
    private const float MarkerCenterDotScale = 0.30f;
    private const float MarkerCenterGlowScale = 0.50f;
    private const int MarkerGlowRingCount = 7;
    private const float CardinalTextScale = 1.35f;
    private const uint NorthAccentColor = 0xFF4AA8FF;

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
        var visibleRange = snapshot.VisibleRangeYalms > 0f
            ? snapshot.VisibleRangeYalms
            : MinimapLayout.ClampVisibleRange(config.MinimapVisibleRangeYalms);
        var pixelsPerYalm = (contentHalf * 0.86f) / Math.Max(visibleRange, 1f);
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
        if (config.MinimapShowCardinalDirections)
        {
            DrawCardinalDirections(draw, center, contentHalf * NorthRingRadiusScale, northLocked, frame, alpha);
        }
        if (config.MinimapShowNativeMarkers)
        {
            DrawFateAreas(draw, center, contentHalf, snapshot, frame, northLocked, alpha, config.MinimapSquare);
            DrawIconMarkers(draw, center, contentHalf, snapshot, frame, northLocked, alpha, config.MinimapSquare);
        }

        DrawPlayerIndicator(
            draw,
            center,
            contentHalf,
            snapshot,
            northLocked,
            alpha,
            MinimapLayout.ClampFacingConeSizeScale(config.MinimapFacingConeSizeScale),
            MinimapLayout.ClampFacingConeOpacity(config.MinimapFacingConeOpacity),
            MinimapLayout.ClampPlayerPinSize(config.MinimapPlayerPinSize),
            snapshot.PlayerPinFillColor);
        DrawBlips(draw, center, contentHalf, snapshot, frame, northLocked, alpha, config.MinimapSquare);
        DrawMapTitle(draw, frameCorners, snapshot.MapTitle, alpha);

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

        if (!MinimapTextureUtil.IsDrawable(snapshot.MapTexture))
        {
            return false;
        }

        var wrap = snapshot.MapTexture!.GetWrapOrEmpty();
        if (wrap.Handle == 0 || wrap.Width <= 0 || wrap.Height <= 0)
        {
            return false;
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
        var northOffset = northLocked
            ? new Vector2(0f, -radius * 0.88f)
            : TransformMapLocalOffset(new Vector2(0f, -radius * 0.88f), frame);
        var northPos = center + northOffset;
        draw.AddCircleFilled(northPos, 17f, ApplyAlpha(0xD0182028, alpha), 24);
        draw.AddCircle(northPos, 17f, ApplyAlpha(0xE0D0DEEA, alpha), 24, 1.5f);
        var northFontSize = ImGui.GetFontSize() * CardinalTextScale;
        var northTextSize = ImGui.CalcTextSize("N") * CardinalTextScale;
        draw.AddText(
            ImGui.GetFont(),
            northFontSize,
            northPos - (northTextSize * 0.5f),
            ApplyAlpha(NorthAccentColor, alpha),
            "N");
    }

    private static void DrawCardinalDirections(
        ImDrawListPtr draw,
        Vector2 center,
        float radius,
        bool northLocked,
        MapFrameTransform frame,
        float alpha)
    {
        DrawCardinalLabel(draw, center, radius, northLocked, frame, alpha, new Vector2(1f, 0f), "E");
        DrawCardinalLabel(draw, center, radius, northLocked, frame, alpha, new Vector2(0f, 1f), "S");
        DrawCardinalLabel(draw, center, radius, northLocked, frame, alpha, new Vector2(-1f, 0f), "W");
    }

    private static void DrawCardinalLabel(
        ImDrawListPtr draw,
        Vector2 center,
        float radius,
        bool northLocked,
        MapFrameTransform frame,
        float alpha,
        Vector2 direction,
        string label)
    {
        var localOffset = direction * (radius * 0.88f);
        var offset = northLocked ? localOffset : TransformMapLocalOffset(localOffset, frame);
        var pos = center + offset;
        var textSize = ImGui.CalcTextSize(label);
        var scaledTextSize = textSize * CardinalTextScale;
        var textPos = pos - (scaledTextSize * 0.5f);
        draw.AddText(
            ImGui.GetFont(),
            ImGui.GetFontSize() * CardinalTextScale,
            textPos + new Vector2(1.5f, 1.5f),
            ApplyAlpha(0xCCFFFFFF, alpha),
            label);
        draw.AddText(
            ImGui.GetFont(),
            ImGui.GetFontSize() * CardinalTextScale,
            textPos,
            ApplyAlpha(0xFF000000, alpha),
            label);
    }

    private static void DrawFateAreas(
        ImDrawListPtr draw,
        Vector2 center,
        float contentHalf,
        MinimapSnapshot snapshot,
        MapFrameTransform frame,
        bool northLocked,
        float alpha,
        bool square)
    {
        if (snapshot.FateAreas.Count == 0)
        {
            return;
        }

        var clipRadius = contentHalf * BlipClipScale;
        var fillColor = ApplyAlpha(FateAreaFillColor, alpha * FateAreaFillAlphaScale);
        var ringColor = ApplyAlpha(FateAreaRingColor, alpha);

        foreach (var area in snapshot.FateAreas)
        {
            var screenOffset = area.ScreenOffset;
            if (!northLocked)
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

            var radius = area.RadiusPixels;
            draw.AddCircleFilled(pos, radius, fillColor, FateAreaSegments);
            DrawDashedCircle(draw, pos, radius, ringColor, FateAreaRingThickness);
        }
    }

    private static void DrawDashedCircle(
        ImDrawListPtr draw,
        Vector2 center,
        float radius,
        uint color,
        float thickness)
    {
        for (var i = 0; i < FateAreaSegments; i += 2)
        {
            var angleStart = (i / (float)FateAreaSegments) * MathF.PI * 2f;
            var angleEnd = ((i + 1) / (float)FateAreaSegments) * MathF.PI * 2f;
            draw.PathClear();
            draw.PathArcTo(center, radius, angleStart, angleEnd, 4);
            draw.PathStroke(color, ImDrawFlags.None, thickness);
        }
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

            var screenOffset = marker.ScreenOffset;
            if (!northLocked)
            {
                screenOffset = TransformMapLocalOffset(screenOffset, frame);
            }

            // Texture-space offsets: +Y is ImGui-down, matching the map UV quad (unlike party blips).
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
        MapFrameTransform frame,
        bool northLocked,
        float alpha,
        bool square)
    {
        var clipRadius = contentHalf * BlipClipScale;
        foreach (var blip in snapshot.Blips)
        {
            var screenOffset = blip.ScreenOffset;
            if (!northLocked)
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

            draw.AddCircleFilled(pos, blip.Radius, ApplyAlpha(blip.Color, alpha));
            draw.AddCircle(pos, blip.Radius, ApplyAlpha(0xFF000000, alpha), 24, PartyBlipOutlineThickness);
            DrawRadialCenterGlow(draw, pos, blip.Radius, alpha);
            draw.AddCircleFilled(pos, blip.Radius * MarkerCenterDotScale, ApplyAlpha(0xFFFFFFFF, alpha), 16);
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
        float facingConeOpacity,
        float playerPinSize,
        uint playerPinColorArgb)
    {
        var cameraFacing = GetCameraFacingDirection(snapshot, northLocked);
        if (!TryDrawNativeVisionCone(
                draw,
                center,
                contentHalf,
                snapshot.PlayerIndicator,
                northLocked,
                alpha,
                facingConeSizeScale,
                facingConeOpacity))
        {
            DrawFacingCone(
                draw,
                center,
                cameraFacing,
                contentHalf * facingConeSizeScale,
                alpha * facingConeOpacity);
        }

        var characterFacing = GetCharacterFacingDirection(snapshot, northLocked);
        var pinFill = ApplyAlpha(playerPinColorArgb, alpha);
        var pinOutline = ApplyAlpha(0xFF000000, alpha);
        DrawTeardropPin(draw, center, characterFacing, playerPinSize, pinFill, pinOutline);
    }

    private static bool TryDrawNativeVisionCone(
        ImDrawListPtr draw,
        Vector2 center,
        float contentHalf,
        MinimapPlayerIndicatorAssets indicator,
        bool northLocked,
        float alpha,
        float facingConeSizeScale,
        float facingConeOpacity)
    {
        if (!indicator.IsValid || !MinimapTextureUtil.IsDrawable(indicator.ConeTexture))
        {
            return false;
        }

        var nativeMapSize = indicator.NativeMapSize > 1f ? indicator.NativeMapSize : 200f;
        var scale = (contentHalf * 2f) / nativeMapSize;
        var coneHalf = indicator.ConeSize * scale * facingConeSizeScale * 0.5f;
        if (coneHalf.X < 0.5f || coneHalf.Y < 0.5f)
        {
            return false;
        }

        var coneRotation = northLocked ? indicator.NativeConeRotation : 0f;
        DrawRotatedImage(
            draw,
            indicator.ConeTexture!.GetWrapOrEmpty().Handle,
            center,
            coneHalf,
            coneRotation,
            indicator.ConeUvMin,
            indicator.ConeUvMax,
            ApplyAlpha(0xFFFFFFFF, alpha * facingConeOpacity));

        return true;
    }

    /// <summary>
    /// Rounded teardrop pin at minimap center; points in character facing, independent of camera cone.
    /// </summary>
    private static void DrawTeardropPin(
        ImDrawListPtr draw,
        Vector2 center,
        Vector2 facing,
        float radius,
        uint fillColor,
        uint outlineColor)
    {
        if (facing.LengthSquared() < 0.0001f)
        {
            facing = new Vector2(0f, -1f);
        }
        else
        {
            facing = Vector2.Normalize(facing);
        }

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

        var outlineThickness = MathF.Max(9f, radius * 0.68f);
        draw.PathStroke(outlineColor, ImDrawFlags.Closed, outlineThickness);

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

        var markerAlpha = (fillColor >> 24) / 255f;
        DrawRadialCenterGlow(draw, center, radius, markerAlpha);
        draw.AddCircleFilled(center, radius * MarkerCenterDotScale, ApplyAlpha(0xFFFFFFFF, markerAlpha), 16);
    }

    private static void DrawRadialCenterGlow(ImDrawListPtr draw, Vector2 center, float markerRadius, float alpha)
    {
        // Build a smooth radial falloff by stacking several translucent rings.
        var maxRadius = markerRadius * MarkerCenterGlowScale;
        for (var i = MarkerGlowRingCount; i >= 1; i--)
        {
            var t = i / (float)MarkerGlowRingCount;
            var ringRadius = maxRadius * t;
            var ringAlpha = alpha * (0.30f * t * t);
            draw.AddCircleFilled(center, ringRadius, ApplyAlpha(0xFFFFFFFF, ringAlpha), 20);
        }
    }

    private static void DrawRotatedImage(
        ImDrawListPtr draw,
        ImTextureID texture,
        Vector2 center,
        Vector2 halfSize,
        float rotation,
        Vector2 uvMin,
        Vector2 uvMax,
        uint tint)
    {
        if (texture.Handle == 0)
        {
            return;
        }

        var cos = MathF.Cos(rotation);
        var sin = MathF.Sin(rotation);
        var corners = new Vector2[4];
        var localCorners = new[]
        {
            new Vector2(-halfSize.X, -halfSize.Y),
            new Vector2(halfSize.X, -halfSize.Y),
            new Vector2(halfSize.X, halfSize.Y),
            new Vector2(-halfSize.X, halfSize.Y),
        };

        for (var i = 0; i < 4; i++)
        {
            var local = localCorners[i];
            corners[i] = center + new Vector2(
                (local.X * cos) - (local.Y * sin),
                (local.X * sin) + (local.Y * cos));
        }

        draw.AddImageQuad(
            texture,
            corners[0],
            corners[1],
            corners[2],
            corners[3],
            new Vector2(uvMin.X, uvMin.Y),
            new Vector2(uvMax.X, uvMin.Y),
            new Vector2(uvMax.X, uvMax.Y),
            new Vector2(uvMin.X, uvMax.Y),
            tint);
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
    /// Vision cone direction (camera view when north-locked).
    /// </summary>
    private static Vector2 GetCameraFacingDirection(MinimapSnapshot snapshot, bool northLocked)
    {
        if (!northLocked)
        {
            return new Vector2(0f, -1f);
        }

        var yaw = ResolveNorthLockedConeYaw(snapshot);
        var direction = new Vector2(MathF.Sin(yaw), MathF.Cos(yaw));
        return direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : new Vector2(0f, 1f);
    }

    /// <summary>
    /// Teardrop pin direction (character body facing when north-locked).
    /// </summary>
    private static Vector2 GetCharacterFacingDirection(MinimapSnapshot snapshot, bool northLocked)
    {
        if (!northLocked)
        {
            return new Vector2(0f, -1f);
        }

        var direction = new Vector2(MathF.Sin(snapshot.PlayerYaw), MathF.Cos(snapshot.PlayerYaw));
        return direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : new Vector2(0f, -1f);
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
