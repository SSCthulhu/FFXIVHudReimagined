using Dalamud.Interface.Textures;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace FFXIVHudPlugin;

public static class HudRenderer
{
    public static void DrawCenterOrb(ImDrawListPtr draw, HudConfiguration config, HudStateSnapshot snapshot, HudLayoutRects layout, float alpha)
    {
        var scale = config.GlobalScale;
        var center = layout.OrbCenter;
        var radius = config.OrbRadius * scale;
        var thickness = config.OrbThickness * scale;
        var mpThickness = thickness * config.MpRingThicknessScale;

        var hpFill = ApplyAlpha(config.ColorHpFill, alpha);
        var mpBack = ApplyAlpha(config.ColorMpBack, alpha);
        var mpFill = ApplyAlpha(config.ColorMpFill, alpha);
        var accent = ApplyAlpha(config.ColorAccent, alpha);
        var textPrimary = ApplyAlpha(config.ColorTextPrimary, alpha);

        var mpRadius = radius + (mpThickness * 0.5f);
        var hpAnimated = Math.Clamp(snapshot.HpAnimatedRatio, 0f, 1f);
        var hpActual = Math.Clamp(snapshot.HpRatio, 0f, 1f);
        // Opaque in-shape backdrop for contrast (stays fully within orb/ring silhouette).
        draw.AddCircleFilled(center, radius + (mpThickness * 0.55f), ApplyAlpha(0xC4141418, alpha), 100);
        draw.AddCircleFilled(center, radius, ApplyAlpha(0xD0181C18, alpha), 96);
        // Always draw the MP ring background so the orb resembles the native paired HP/MP treatment.
        var mpAnimated = Math.Clamp(snapshot.MpAnimatedRatio, 0f, 1f);
        var mpActual = Math.Clamp(snapshot.MpRatio, 0f, 1f);
        DrawArc(draw, center, mpRadius, -MathF.PI * 0.5f, MathF.PI * 1.5f, ApplyAlpha(0xD21B1E24, alpha), mpThickness, 100);
        DrawArc(draw, center, mpRadius, -MathF.PI * 0.5f, MathF.PI * 1.5f, mpBack, mpThickness * 0.72f, 100);
        DrawArc(draw, center, mpRadius, -MathF.PI * 0.5f, (MathF.PI * 2f * mpAnimated) - MathF.PI * 0.5f, mpFill, mpThickness, 100);
        if (mpAnimated > mpActual + 0.001f)
        {
            var trailStart = (MathF.PI * 2f * mpActual) - MathF.PI * 0.5f;
            var trailEnd = (MathF.PI * 2f * mpAnimated) - MathF.PI * 0.5f;
            var trailColor = ApplyAlpha(0xFF3B8BFF, alpha);
            DrawArc(draw, center, mpRadius, trailStart, trailEnd, trailColor, mpThickness * 0.98f, 80);
            // Keep the same matte/glassy treatment on the draining segment.
            DrawArc(
                draw,
                center,
                mpRadius - (mpThickness * 0.20f),
                trailStart,
                trailEnd,
                ApplyAlpha(0x40FFFFFF, alpha),
                Math.Max(1f, mpThickness * 0.30f),
                80);
            DrawArc(
                draw,
                center,
                mpRadius + (mpThickness * 0.18f),
                trailStart,
                trailEnd,
                ApplyAlpha(0x4A000000, alpha),
                Math.Max(1f, mpThickness * 0.24f),
                80);
            // Bright inner edge to make the spent segment clearly readable in motion.
            DrawArc(
                draw,
                center,
                mpRadius - (mpThickness * 0.40f),
                trailStart,
                trailEnd,
                ApplyAlpha(0x66D1ECFF, alpha),
                Math.Max(1f, mpThickness * 0.16f),
                80);
        }
        else if (mpActual > mpAnimated + 0.001f)
        {
            // MP regen segment: blue/cyan refill edge between animated value and actual value.
            var regenStart = (MathF.PI * 2f * mpAnimated) - MathF.PI * 0.5f;
            var regenEnd = (MathF.PI * 2f * mpActual) - MathF.PI * 0.5f;
            var regenColor = ApplyAlpha(0xFFF4AE4A, alpha);
            DrawArc(draw, center, mpRadius, regenStart, regenEnd, regenColor, mpThickness * 0.95f, 80);
            DrawArc(
                draw,
                center,
                mpRadius - (mpThickness * 0.22f),
                regenStart,
                regenEnd,
                ApplyAlpha(0x74FFF2A8, alpha),
                Math.Max(1f, mpThickness * 0.30f),
                80);
            DrawArc(
                draw,
                center,
                mpRadius + (mpThickness * 0.16f),
                regenStart,
                regenEnd,
                ApplyAlpha(0x4A6D3B12, alpha),
                Math.Max(1f, mpThickness * 0.24f),
                80);
        }
        if (mpAnimated > 0.001f)
        {
            // Matte-glass treatment on MP ring: inner highlight and outer shade.
            DrawArc(
                draw,
                center,
                mpRadius - (mpThickness * 0.20f),
                -MathF.PI * 0.5f,
                (MathF.PI * 2f * mpAnimated) - MathF.PI * 0.5f,
                ApplyAlpha(0x42FFFFFF, alpha),
                Math.Max(1f, mpThickness * 0.30f),
                100);
            DrawArc(
                draw,
                center,
                mpRadius + (mpThickness * 0.18f),
                -MathF.PI * 0.5f,
                (MathF.PI * 2f * mpAnimated) - MathF.PI * 0.5f,
                ApplyAlpha(0x4C000000, alpha),
                Math.Max(1f, mpThickness * 0.24f),
                100);
        }

        // Diablo-style HP: circle fills from bottom to top, and empties from top downward.
        DrawVerticalCircleFill(draw, center, radius - 0.5f * scale, hpAnimated, hpFill, 100);
        if (hpAnimated > hpActual + 0.001f)
        {
            // HP loss segment: darker red section that drains away.
            DrawVerticalCircleBand(
                draw,
                center,
                radius - 0.5f * scale,
                hpActual,
                hpAnimated,
                ApplyAlpha(0xFF2D2DCC, alpha),
                100);
        }
        else if (hpActual > hpAnimated + 0.001f)
        {
            // HP gain segment: blue/cyan section to match MP regen style.
            DrawVerticalCircleBand(
                draw,
                center,
                radius - 0.5f * scale,
                hpAnimated,
                hpActual,
                ApplyAlpha(0xFFF4AE4A, alpha),
                100);
        }
        DrawVerticalGradientCircleOverlay(
            draw,
            center,
            radius - 1.2f * scale,
            hpAnimated,
            ApplyAlpha(0xB0246E24, alpha),
            ApplyAlpha(0xB07EFF84, alpha),
            1.0f * scale);
        DrawHorizontalGradientCircleOverlay(
            draw,
            center,
            radius - 1.3f * scale,
            hpAnimated,
            ApplyAlpha(0x38000000, alpha),
            ApplyAlpha(0x5AFFFFFF, alpha),
            1.0f * scale);
        var shieldRatio = Math.Clamp(snapshot.ShieldRatio, 0f, 1f);
        if (shieldRatio > 0.001f)
        {
            // Shield reveal: same bottom-up behavior, but with continuous gradient (no hard band layers).
            DrawVerticalCircleFill(
                draw,
                center,
                radius - 0.75f * scale,
                shieldRatio,
                ApplyAlpha(0xB02A1704, alpha),
                120);
            DrawVerticalGradientCircleOverlay(
                draw,
                center,
                radius - 0.80f * scale,
                shieldRatio,
                ApplyAlpha(0xD02A1704, alpha),
                ApplyAlpha(0xE8FFC837, alpha),
                1.0f * scale);
            DrawHorizontalGradientCircleOverlay(
                draw,
                center,
                radius - 0.84f * scale,
                shieldRatio,
                ApplyAlpha(0x30000000, alpha),
                ApplyAlpha(0x66FFEF9A, alpha),
                1.0f * scale);

            // Very soft glassy specular sweeps (kept subtle to avoid hard bands).
            DrawArc(
                draw,
                center,
                radius - 0.98f * scale,
                -2.45f,
                -0.85f,
                ApplyAlpha(0x60FFF4C8, alpha),
                Math.Max(1f, 1.0f * scale),
                72);
            DrawArc(
                draw,
                center,
                radius - 0.70f * scale,
                -2.35f,
                -1.15f,
                ApplyAlpha(0x34FFFFFF, alpha),
                Math.Max(1f, 0.8f * scale),
                72);
        }
        DrawArc(
            draw,
            center,
            radius - 1.8f * scale,
            -2.50f,
            -0.70f,
            ApplyAlpha(0x38FFFFFF, alpha),
            Math.Max(1f, 1.5f * scale),
            64);
        // Raised metal-like trim around health and mana rings.
        DrawRaisedCircularBorder(draw, center, radius - 0.8f * scale, 2.4f * scale, alpha, accent);
        DrawRaisedCircularBorder(draw, center, mpRadius - (mpThickness * 0.5f), 1.6f * scale, alpha, accent);
        DrawRaisedCircularBorder(draw, center, mpRadius + (mpThickness * 0.5f), 2.0f * scale, alpha, accent);

        var hpText = snapshot.HasPlayer ? $"{MathF.Round(snapshot.HpRatio * 100f)}%" : "--";
        var hpValueText = snapshot.HasPlayer ? $"{snapshot.CurrentHp}/{Math.Max(1u, snapshot.MaxHp)}" : "No Player";
        var font = ImGui.GetFont();
        var baseFontSize = ImGui.GetFontSize();
        var percentFontSize = baseFontSize * (1.9f * scale);
        var valueFontSize = baseFontSize * (0.72f * scale);
        var hpTextSize = EstimateScaledTextSize(hpText, percentFontSize, baseFontSize);
        var hpValueSize = EstimateScaledTextSize(hpValueText, valueFontSize, baseFontSize);
        var hpPctPos = PixelSnap(new Vector2(center.X - hpTextSize.X * 0.5f, center.Y - hpTextSize.Y * 0.62f));
        draw.AddText(font, percentFontSize, new Vector2(hpPctPos.X + 0.5f, hpPctPos.Y + 0.5f), ApplyAlpha(0x90000000, alpha), hpText);
        draw.AddText(font, percentFontSize, hpPctPos, textPrimary, hpText);

        var hpValuePos = PixelSnap(new Vector2(center.X - hpValueSize.X * 0.5f, center.Y + hpTextSize.Y * 0.28f));
        draw.AddText(font, valueFontSize, new Vector2(hpValuePos.X + 0.5f, hpValuePos.Y + 0.5f), ApplyAlpha(0x90000000, alpha), hpValueText);
        draw.AddText(font, valueFontSize, hpValuePos, textPrimary, hpValueText);
        if (snapshot.HasPlayer && snapshot.ShieldAmount > 0)
        {
            var shieldPercent = snapshot.ShieldRatio * 100f;
            var shieldText = $"{shieldPercent:0.#}% (+{snapshot.ShieldAmount})";
            var shieldFontSize = valueFontSize * 0.74f;
            var shieldSize = EstimateScaledTextSize(shieldText, shieldFontSize, baseFontSize);
            var shieldPos = PixelSnap(new Vector2(center.X - shieldSize.X * 0.5f, hpValuePos.Y + hpValueSize.Y + 2f * scale));
            var shieldColor = ApplyAlpha(0xFFD3EEFF, alpha);
            draw.AddText(font, shieldFontSize, new Vector2(shieldPos.X + 0.5f, shieldPos.Y + 0.5f), ApplyAlpha(0x90000000, alpha), shieldText);
            draw.AddText(font, shieldFontSize, shieldPos, shieldColor, shieldText);
        }
    }

    public static void DrawStatusRows(ImDrawListPtr draw, HudConfiguration config, HudStateSnapshot snapshot, HudLayoutRects layout, float alpha)
    {
        // Keep status icon scale independent from overall HUD scale so buffs/debuffs stay consistent.
        const float statusIconWidthScale = 0.78f;
        var buffIconHeight = config.BuffIconSize;
        var buffIconWidth = config.BuffIconSize * statusIconWidthScale;
        DrawStatusLane(
            draw,
            config,
            snapshot.Buffs,
            layout.LeftBuffStart,
            buffIconWidth,
            buffIconHeight,
            config.BuffIconGap,
            config.BuffGrowDirection,
            config.BuffTimerPlacement,
            StatusLaneLayout.ClampMaxIconsPerRow(config.BuffMaxIconsPerRow),
            ApplyAlpha(config.ColorBuffTint, alpha),
            ApplyAlpha(config.ColorTextPrimary, alpha));
        var debuffIconHeight = config.DebuffIconSize;
        var debuffIconWidth = config.DebuffIconSize * statusIconWidthScale;
        DrawStatusLane(
            draw,
            config,
            snapshot.Debuffs,
            layout.RightDebuffStart,
            debuffIconWidth,
            debuffIconHeight,
            config.DebuffIconGap,
            config.DebuffGrowDirection,
            config.DebuffTimerPlacement,
            StatusLaneLayout.ClampMaxIconsPerRow(config.DebuffMaxIconsPerRow),
            ApplyAlpha(config.ColorDebuffTint, alpha),
            ApplyAlpha(config.ColorTextPrimary, alpha));
    }

    public static void DrawHotbars(ImDrawListPtr draw, HudConfiguration config, HudStateSnapshot snapshot, HudLayoutRects layout, float alpha)
    {
        if (config.Hotbar1Enabled)
        {
            DrawHotbarGrid(
                draw,
                config,
                snapshot.LeftHotbar,
                snapshot.RightHotbar,
                layout.Hotbar1Start,
                config.Hotbar1SlotsPerRow,
                HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar1BarIndex),
                HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar1BarIndex),
                alpha);
        }

        if (config.Hotbar2Enabled)
        {
            DrawHotbarGrid(
                draw,
                config,
                snapshot.LeftHotbar2,
                snapshot.RightHotbar2,
                layout.Hotbar2Start,
                config.Hotbar2SlotsPerRow,
                HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar2BarIndex),
                HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar2BarIndex),
                alpha);
        }
    }

    public static void DrawLimitBreak(ImDrawListPtr draw, HudConfiguration config, HudStateSnapshot snapshot, HudLayoutRects layout, float alpha)
    {
        if (snapshot.LimitBreak.MaxSegments <= 0)
        {
            return;
        }

        var center = layout.OrbCenter;
        var scale = config.GlobalScale;
        var orbRadius = config.OrbRadius * scale;
        var mpThickness = (config.OrbThickness * scale) * config.MpRingThicknessScale;

        // First-pass LB redesign:
        // Three segmented arcs wrap around the lower half of the orb (left-mid to right-mid).
        var lbRadius = orbRadius + mpThickness + (6.5f * scale);
        var lbThickness = 9.6f * scale;
        // Bottom-half sweep around the orb, filling left -> right.
        var startAngle = MathF.PI;
        var endAngle = 0f;
        var totalSweep = startAngle - endAngle;
        var gapAngle = 0.06f;
        var segmentCount = Math.Clamp(snapshot.LimitBreak.MaxSegments, 1, 3);
        var segmentSweep = (totalSweep - (gapAngle * (segmentCount - 1))) / segmentCount;

        var track = ApplyAlpha(0xCC1E222A, alpha);
        var trackInner = ApplyAlpha(0x902A303A, alpha);
        var trim = ApplyAlpha(config.ColorAccent, alpha);
        var fillCharge = ApplyAlpha(0xFFFFA24A, alpha);
        var fillChargeBright = ApplyAlpha(0xCCFFE1A2, alpha);
        var fillFull = ApplyAlpha(0xFF37AFD4, alpha);
        var fillFullBright = ApplyAlpha(0xCC8ADCF2, alpha);

        for (var i = 0; i < segmentCount; i++)
        {
            var segStart = startAngle - (i * (segmentSweep + gapAngle));
            var segEnd = segStart - segmentSweep;
            var segmentFill = i < snapshot.LimitBreak.SegmentFill.Count
                ? Math.Clamp(snapshot.LimitBreak.SegmentFill[i], 0f, 1f)
                : 0f;

            // Base track + trim.
            DrawArc(draw, center, lbRadius, segStart, segEnd, track, lbThickness, 64);
            DrawArc(draw, center, lbRadius, segStart, segEnd, trackInner, lbThickness * 0.58f, 64);
            DrawArc(draw, center, lbRadius + (lbThickness * 0.46f), segStart, segEnd, trim, 1.4f * scale, 64);
            DrawArc(draw, center, lbRadius - (lbThickness * 0.50f), segStart, segEnd, trim, 1.0f * scale, 64);
            DrawArcEndCap(draw, center, lbRadius, segStart, lbThickness, trim, 64);
            DrawArcEndCap(draw, center, lbRadius, segEnd, lbThickness, trim, 64);

            if (segmentFill <= 0.001f)
            {
                continue;
            }

            var fillEnd = segStart - (segmentSweep * segmentFill);
            var isFull = segmentFill >= 0.999f;
            var fillColor = isFull ? fillFull : fillCharge;
            var fillBright = isFull ? fillFullBright : fillChargeBright;
            DrawArc(draw, center, lbRadius, segStart, fillEnd, fillColor, lbThickness * 0.88f, 64);
            DrawArc(draw, center, lbRadius - (lbThickness * 0.20f), segStart, fillEnd, fillBright, lbThickness * 0.34f, 64);

            if (isFull)
            {
                // Subtle glow for completed segments.
                DrawArc(draw, center, lbRadius + (lbThickness * 0.82f), segStart, segEnd, ApplyAlpha(0x6037AFD4, alpha), 2.4f * scale, 64);
                DrawArc(draw, center, lbRadius + (lbThickness * 1.08f), segStart, segEnd, ApplyAlpha(0x3037AFD4, alpha), 3.2f * scale, 64);
            }
        }
    }

    public static void DrawCastArc(ImDrawListPtr draw, HudConfiguration config, HudStateSnapshot snapshot, HudLayoutRects layout, float alpha)
    {
        if (!snapshot.IsCasting || snapshot.CastProgressRatio <= 0.001f)
        {
            return;
        }

        var center = layout.OrbCenter;
        var scale = config.GlobalScale;
        var orbRadius = config.OrbRadius * scale;
        var mpThickness = (config.OrbThickness * scale) * config.MpRingThicknessScale;
        var castRadius = orbRadius + mpThickness + (6.5f * scale);
        var castThickness = 9.6f * scale;

        // Continuous top-half cast arc, filling left -> right.
        var startAngle = MathF.PI;
        var endAngle = MathF.PI * 2f;
        var totalSweep = endAngle - startAngle;

        var track = ApplyAlpha(0xB01E222A, alpha);
        var trackInner = ApplyAlpha(0x7A2A303A, alpha);
        var trim = ApplyAlpha(config.ColorAccent, alpha);
        var fill = ApplyAlpha(0xFFF7F7FF, alpha);
        var fillBright = ApplyAlpha(0xFFDFEEFF, alpha);

        DrawArc(draw, center, castRadius, startAngle, endAngle, track, castThickness, 96);
        DrawArc(draw, center, castRadius, startAngle, endAngle, trackInner, castThickness * 0.58f, 96);
        DrawArc(draw, center, castRadius + (castThickness * 0.46f), startAngle, endAngle, trim, 1.2f * scale, 96);
        DrawArc(draw, center, castRadius - (castThickness * 0.50f), startAngle, endAngle, trim, 0.9f * scale, 96);
        DrawArcEndCap(draw, center, castRadius, startAngle, castThickness, trim, 96);
        DrawArcEndCap(draw, center, castRadius, endAngle, castThickness, trim, 96);

        var fillEnd = startAngle + (totalSweep * snapshot.CastProgressRatio);
        DrawArc(draw, center, castRadius, startAngle, fillEnd, fill, castThickness * 0.82f, 96);
        DrawArc(draw, center, castRadius - (castThickness * 0.20f), startAngle, fillEnd, fillBright, castThickness * 0.30f, 96);

        if (config.ShowSlidecastMarker && snapshot.CastTotalSeconds > 0.05f)
        {
            var slidecastSeconds = Math.Clamp(config.SlidecastOffsetSeconds, 0.05f, snapshot.CastTotalSeconds);
            var slidecastStartRatio = Math.Clamp((snapshot.CastTotalSeconds - slidecastSeconds) / snapshot.CastTotalSeconds, 0f, 1f);
            var markerStart = startAngle + (totalSweep * slidecastStartRatio);
            var markerEnd = endAngle;
            var inSlidecastWindow = snapshot.CastProgressRatio >= slidecastStartRatio;

            // Match Simple Tweaks behavior: red window until ready, then green/ready highlight.
            var markerColor = inSlidecastWindow
                ? ApplyAlpha(0xFF4DCC4D, alpha)
                : ApplyAlpha(0xFF4D4DCC, alpha);
            var markerEdge = inSlidecastWindow
                ? ApplyAlpha(0xFF66FF66, alpha)
                : ApplyAlpha(0xFF7777FF, alpha);

            DrawArc(draw, center, castRadius, markerStart, markerEnd, markerColor, castThickness * 0.26f, 96);
            DrawArc(draw, center, castRadius + (castThickness * 0.10f), markerStart, markerEnd, markerEdge, castThickness * 0.06f, 96);
            DrawArcEndCap(draw, center, castRadius, markerStart, castThickness * 0.30f, markerEdge, 96);
        }
    }

    public static void DrawDebugOverlay(ImDrawListPtr draw, HudConfiguration config, HudLayoutRects layout, float alpha)
    {
        var c = ApplyAlpha(0x90FFFFFF, alpha);
        var orb = config.OrbRadius * config.GlobalScale;
        draw.AddCircle(layout.OrbCenter, orb, c, 60, 1f);

        DrawCross(draw, layout.Center, ApplyAlpha(0x90FFE08A, alpha));
        DrawCross(draw, layout.OrbCenter, c);
        if (config.Hotbar1Enabled)
        {
            DrawCross(draw, layout.Hotbar1Start, c);
        }

        if (config.Hotbar2Enabled)
        {
            DrawCross(draw, layout.Hotbar2Start, c);
        }
        DrawCross(draw, layout.LeftBuffStart, c);
        DrawCross(draw, layout.RightDebuffStart, c);
        DrawCross(draw, layout.LimitBreakStart, c);

        var debugText = $"Scale {config.GlobalScale:0.00} | Opacity {config.GlobalOpacity:0.00}";
        draw.AddText(new Vector2(layout.OrbCenter.X - 120f, layout.OrbCenter.Y + orb + 14f), c, debugText);
    }

    private static void DrawStatusLane(
        ImDrawListPtr draw,
        HudConfiguration config,
        IReadOnlyList<StatusViewModel> statuses,
        Vector2 start,
        float iconWidth,
        float iconHeight,
        float gap,
        StatusLaneGrowDirection growDirection,
        StatusTimerPlacement timerPlacement,
        int maxIconsPerRow,
        uint tint,
        uint textColor)
    {
        var maxColumns = StatusLaneLayout.ClampMaxIconsPerRow(maxIconsPerRow);
        var maxVisible = StatusLaneLayout.GetMaxVisibleStatusCount(maxColumns);
        const float rowGap = 6f;
        var timerFontSize = ImGui.GetFontSize() * 1.45f;
        var timerBaseSize = EstimateScaledTextSize("99.9", timerFontSize, ImGui.GetFontSize());

        for (var i = 0; i < Math.Min(statuses.Count, maxVisible); i++)
        {
            var col = i % maxColumns;
            var row = i / maxColumns;
            var rowStep = iconHeight + timerBaseSize.Y + rowGap;
            var rowOffsetY = row * rowStep;
            var growRightToLeft = StatusLaneLayout.IsHorizontalGrowRightToLeft(growDirection);
            var growRowsUp = StatusLaneLayout.IsRowGrowthUp(growDirection);
            var x = growRightToLeft
                ? start.X - ((iconWidth + gap) * col) - iconWidth
                : start.X + ((iconWidth + gap) * col);
            var y = growRowsUp ? start.Y - rowOffsetY : start.Y + rowOffsetY;
            var rectMin = new Vector2(x, y);
            var rectMax = rectMin + new Vector2(iconWidth, iconHeight);
            var status = statuses[i];
            if (status.Icon is null)
            {
                DrawStatusPlaceholder(draw, status, rectMin, rectMax, 4f);
            }
            else
            {
                DrawTextureOrPlaceholder(draw, status.Icon, rectMin, rectMax, tint, 4f);
            }

            var timer = FormatStatusTime(status.RemainingTime);
            if (!status.ShowTimer || string.IsNullOrEmpty(timer))
            {
                continue;
            }

            var timerSize = EstimateScaledTextSize(timer, timerFontSize, ImGui.GetFontSize());
            // Improve readability against bright terrain by drawing a compact dark timer plate.
            var platePadX = 5f;
            var platePadY = 1f;
            var plateWidth = timerSize.X + platePadX * 2f;
            var plateHeight = timerSize.Y + platePadY * 2f;
            Vector2 plateMin;
            Vector2 plateMax;
            if (timerPlacement == StatusTimerPlacement.Top)
            {
                plateMax = new Vector2(rectMin.X + (iconWidth + plateWidth) * 0.5f, rectMin.Y - 1f);
                plateMin = new Vector2(plateMax.X - plateWidth, plateMax.Y - plateHeight);
            }
            else
            {
                plateMin = new Vector2(rectMin.X + (iconWidth - plateWidth) * 0.5f, rectMax.Y - 1f);
                plateMax = new Vector2(plateMin.X + plateWidth, plateMin.Y + plateHeight);
            }
            draw.AddRectFilled(plateMin, plateMax, 0xC0000000, 3f);
            draw.AddRect(plateMin, plateMax, 0x70000000, 3f, ImDrawFlags.None, 1f);

            var timerPos = PixelSnap(new Vector2(plateMin.X + platePadX, plateMin.Y + platePadY - 0.5f));
            var shadow = 0xE0000000;
            var timerColor = GetStatusTimerColor(status.RemainingTime, textColor);
            draw.AddText(ImGui.GetFont(), timerFontSize, new Vector2(timerPos.X - 1f, timerPos.Y), shadow, timer);
            draw.AddText(ImGui.GetFont(), timerFontSize, new Vector2(timerPos.X + 1f, timerPos.Y), shadow, timer);
            draw.AddText(ImGui.GetFont(), timerFontSize, new Vector2(timerPos.X, timerPos.Y - 1f), shadow, timer);
            draw.AddText(ImGui.GetFont(), timerFontSize, new Vector2(timerPos.X, timerPos.Y + 1f), shadow, timer);
            draw.AddText(ImGui.GetFont(), timerFontSize, timerPos, timerColor, timer);

            if (config.EnableStatusTooltips)
            {
                DrawStatusTooltipIfHovered(status, rectMin, rectMax);
            }
        }
    }

    private static void DrawHotbarGrid(
        ImDrawListPtr draw,
        HudConfiguration config,
        IReadOnlyList<HotbarSlotViewModel> leftSlots,
        IReadOnlyList<HotbarSlotViewModel> rightSlots,
        Vector2 gridStart,
        int slotsPerRow,
        float slotSize,
        float gap,
        float alpha)
    {
        DrawHotbarGridLane(draw, config, leftSlots, gridStart, slotsPerRow, slotSize, gap, alpha);
        DrawHotbarGridLane(draw, config, rightSlots, gridStart, slotsPerRow, slotSize, gap, alpha);
    }

    private static void DrawHotbarGridLane(
        ImDrawListPtr draw,
        HudConfiguration config,
        IReadOnlyList<HotbarSlotViewModel> slots,
        Vector2 gridStart,
        int slotsPerRow,
        float slotSize,
        float gap,
        float alpha)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var min = HotbarGridLayout.GetSlotTopLeft(gridStart, slot.GameSlotIndex, slotsPerRow, slotSize, gap);
            var max = min + new Vector2(slotSize, slotSize);
            var hovered = ImGui.IsMouseHoveringRect(min, max);

            var showPlaceholderCross = slot.ActionId != 0;
            DrawTextureOrPlaceholder(draw, slot.Icon, min, max, slot.IsUsable ? 0xFFFFFFFF : 0x80A0A0A0, 3f, showPlaceholderCross);
            // Force a neutral black border; no accent overlay to avoid blue/cyan appearance.
            draw.AddRect(min, max, ApplyAlpha(0xFF000000, alpha), 3f, ImDrawFlags.None, 4.6f);
            draw.AddRect(min, max, ApplyAlpha(0xFF000000, alpha), 3f, ImDrawFlags.None, 2.2f);
            if (hovered)
            {
                // Bright hover frame and subtle in-slot tint so hovered button is obvious.
                draw.AddRectFilled(min, max, ApplyAlpha(0x2AFFFFFF, alpha), 3f);
                draw.AddRect(min, max, ApplyAlpha(0xFFFFF3B8, alpha), 3f, ImDrawFlags.None, 2.6f);
                draw.AddRect(min, max, ApplyAlpha(0x90FFFFFF, alpha), 3f, ImDrawFlags.None, 1.2f);
            }

            if (slot.CooldownRatio > 0.01f)
            {
                DrawCooldownClockOverlay(draw, min, max, slot.CooldownRatio, alpha);
                DrawCooldownTimerText(draw, min, max, slot.CooldownSecondsRemaining, alpha);
            }

            if (slot.ChargesMax > 1)
            {
                var charges = $"{slot.ChargesCurrent}/{slot.ChargesMax}";
                var textSize = ImGui.CalcTextSize(charges);
                draw.AddText(new Vector2(max.X - textSize.X - 2f, min.Y + 2f), ApplyAlpha(0xFFFFFFFF, alpha), charges);
            }

            if (slot.IsProc)
            {
                DrawProcBorder(draw, min, max, alpha);
            }

            var keyPos = Vector2.Zero;
            var keyFont = ImGui.GetFont();
            var keyFontSize = ImGui.GetFontSize() * 2.2f;
            var scaledKeySize = EstimateScaledTextSize(slot.Keybind, keyFontSize, ImGui.GetFontSize());
            // Match default-game feel: anchor keybind in the top-left with strong local contrast.
            keyPos = new Vector2(min.X + 4f, min.Y + 1f);
            var chipPad = new Vector2(3f, 1f);
            var chipMin = new Vector2(keyPos.X - chipPad.X, keyPos.Y - chipPad.Y);
            var chipMax = new Vector2(keyPos.X + scaledKeySize.X + chipPad.X, keyPos.Y + scaledKeySize.Y + chipPad.Y);
            draw.AddRectFilled(chipMin, chipMax, ApplyAlpha(0xB0000000, alpha), 2f);
            draw.AddRect(chipMin, chipMax, ApplyAlpha(0x80000000, alpha), 2f, ImDrawFlags.None, 1f);
            var keyColor = ApplyAlpha(0xFFF4F4F4, alpha);
            var keyShadow = ApplyAlpha(0xCC000000, alpha);
            draw.AddText(keyFont, keyFontSize, new Vector2(keyPos.X - 1f, keyPos.Y), keyShadow, slot.Keybind);
            draw.AddText(keyFont, keyFontSize, new Vector2(keyPos.X + 1f, keyPos.Y), keyShadow, slot.Keybind);
            draw.AddText(keyFont, keyFontSize, new Vector2(keyPos.X, keyPos.Y - 1f), keyShadow, slot.Keybind);
            draw.AddText(keyFont, keyFontSize, new Vector2(keyPos.X, keyPos.Y + 1f), keyShadow, slot.Keybind);
            draw.AddText(keyFont, keyFontSize, keyPos, keyColor, slot.Keybind);
            // Extra pass for a subtle bold effect after outlining.
            draw.AddText(keyFont, keyFontSize, new Vector2(keyPos.X + 0.5f, keyPos.Y), keyColor, slot.Keybind);

            if (config.EnableStatusTooltips)
            {
                DrawHotbarActionTooltipIfHovered(slot, min, max, alpha);
            }
        }
    }

    private static void DrawStatusPlaceholder(
        ImDrawListPtr draw,
        StatusViewModel status,
        Vector2 min,
        Vector2 max,
        float rounding)
    {
        // Layout-preview placeholders (no game icon): strong buff vs debuff read at a glance.
        uint fill;
        uint border;
        uint glyphColor;
        string glyph;
        if (status.IsDebuff)
        {
            fill = 0xC0522838;
            border = 0xFFFF8A8A;
            glyphColor = 0xFFFFD4D4;
            glyph = "D";
        }
        else
        {
            fill = 0xC0284858;
            border = 0xFF8AD4FF;
            glyphColor = 0xFFD4F0FF;
            glyph = "B";
        }

        draw.AddRectFilled(min, max, fill, rounding);
        draw.AddRect(min, max, border, rounding, ImDrawFlags.None, 2.6f);

        var font = ImGui.GetFont();
        var iconSpan = Math.Min(max.X - min.X, max.Y - min.Y);
        var fontSize = iconSpan * 0.52f;
        var textSize = EstimateScaledTextSize(glyph, fontSize, ImGui.GetFontSize());
        var pos = PixelSnap(new Vector2(
            min.X + ((max.X - min.X) - textSize.X) * 0.5f,
            min.Y + ((max.Y - min.Y) - textSize.Y) * 0.5f));
        draw.AddText(font, fontSize, pos, glyphColor, glyph);
    }

    private static void DrawTextureOrPlaceholder(ImDrawListPtr draw, ISharedImmediateTexture? texture, Vector2 min, Vector2 max, uint tint, float rounding, bool showCross = true)
    {
        if (texture is not null)
        {
            var wrap = texture.GetWrapOrEmpty();
            draw.AddImageRounded(wrap.Handle, min, max, Vector2.Zero, Vector2.One, tint, rounding);
            return;
        }

        draw.AddRectFilled(min, max, 0x80404040, rounding);
        if (showCross)
        {
            draw.AddLine(min, max, 0x90FFFFFF, 1f);
            draw.AddLine(new Vector2(min.X, max.Y), new Vector2(max.X, min.Y), 0x90FFFFFF, 1f);
        }
    }

    private static void DrawCooldownClockOverlay(ImDrawListPtr draw, Vector2 min, Vector2 max, float cooldownRatio, float alpha)
    {
        var ratio = Math.Clamp(cooldownRatio, 0f, 1f);
        if (ratio <= 0.001f)
        {
            return;
        }

        // Glass-fill style recovery:
        // - On use (ratio = 1): icon is fully dimmed.
        // - During cooldown: color returns from bottom toward top.
        // - At ready (ratio -> 0): dim overlay fully clears.
        var dimAlpha = alpha * (0.28f + (ratio * 0.62f));
        var restoredHeight = (max.Y - min.Y) * (1f - ratio);
        var overlayMaxY = Math.Clamp(max.Y - restoredHeight, min.Y, max.Y);
        var overlayMin = min;
        var overlayMax = new Vector2(max.X, overlayMaxY);

        if (overlayMax.Y > overlayMin.Y + 0.5f)
        {
            draw.AddRectFilled(overlayMin, overlayMax, ApplyAlpha(0xFF000000, dimAlpha), 3f);
        }
    }

    private static void DrawProcBorder(ImDrawListPtr draw, Vector2 min, Vector2 max, float alpha)
    {
        var time = (float)DateTime.UtcNow.TimeOfDay.TotalSeconds;
        var pulse01 = (MathF.Sin(time * 6.0f) * 0.5f) + 0.5f;
        var pulseGlow = 0.80f + (0.55f * pulse01);
        var pulseExpand = 1.8f + (1.4f * pulse01);

        // Strong, obvious proc treatment with bright gold glow + animated "marching ants" highlights.
        var outerGlow = ApplyAlpha(0x7035B7FF, alpha * pulseGlow);
        var midGlow = ApplyAlpha(0xA040C8FF, alpha * pulseGlow);
        var coreGold = ApplyAlpha(0xFF4FD8FF, alpha * (0.92f + (0.38f * pulse01)));
        var brightGold = ApplyAlpha(0xFFFFF2B8, alpha * (0.95f + (0.50f * pulse01)));

        // Expand outward so the proc frame reads larger than the base slot border.
        var expand = pulseExpand;
        var borderMin = new Vector2(min.X - expand, min.Y - expand);
        var borderMax = new Vector2(max.X + expand, max.Y + expand);

        draw.AddRect(borderMin, borderMax, outerGlow, 4f, ImDrawFlags.None, 7.2f + (2.4f * pulse01));
        draw.AddRect(borderMin, borderMax, midGlow, 4f, ImDrawFlags.None, 4.8f + (1.8f * pulse01));
        draw.AddRect(borderMin, borderMax, coreGold, 4f, ImDrawFlags.None, 3.4f + (1.0f * pulse01));
        draw.AddRect(borderMin, borderMax, brightGold, 4f, ImDrawFlags.None, 1.9f + (0.8f * pulse01));

        var inset = 1.2f;
        var antsMin = new Vector2(borderMin.X + inset, borderMin.Y + inset);
        var antsMax = new Vector2(borderMax.X - inset, borderMax.Y - inset);
        var antLen = 6f;
        var gap = 4f;
        var stride = antLen + gap;
        var phase = (time * 34f) % stride;
        var antColorA = ApplyAlpha(0xFFF0DC8A, alpha * (0.85f + (0.45f * pulse01)));
        var antColorB = ApplyAlpha(0xFF8DEAFF, alpha * (0.75f + (0.55f * pulse01)));

        DrawMarchingAntEdge(draw, new Vector2(antsMin.X, antsMin.Y), new Vector2(antsMax.X, antsMin.Y), phase, antLen, stride, antColorA, antColorB, 2.8f);
        DrawMarchingAntEdge(draw, new Vector2(antsMax.X, antsMin.Y), new Vector2(antsMax.X, antsMax.Y), phase + stride * 0.25f, antLen, stride, antColorA, antColorB, 2.8f);
        DrawMarchingAntEdge(draw, new Vector2(antsMax.X, antsMax.Y), new Vector2(antsMin.X, antsMax.Y), phase + stride * 0.50f, antLen, stride, antColorA, antColorB, 2.8f);
        DrawMarchingAntEdge(draw, new Vector2(antsMin.X, antsMax.Y), new Vector2(antsMin.X, antsMin.Y), phase + stride * 0.75f, antLen, stride, antColorA, antColorB, 2.8f);
    }

    private static void DrawMarchingAntEdge(
        ImDrawListPtr draw,
        Vector2 start,
        Vector2 end,
        float phase,
        float antLen,
        float stride,
        uint colorA,
        uint colorB,
        float thickness)
    {
        var dir = end - start;
        var length = dir.Length();
        if (length <= 0.001f)
        {
            return;
        }

        var norm = dir / length;
        var offset = phase % stride;
        var pos = -offset;
        var idx = 0;
        while (pos < length)
        {
            var segStart = Math.Max(0f, pos);
            var segEnd = Math.Min(length, pos + antLen);
            if (segEnd > segStart + 0.5f)
            {
                var a = start + norm * segStart;
                var b = start + norm * segEnd;
                draw.AddLine(a, b, (idx % 2 == 0) ? colorA : colorB, thickness);
            }

            pos += stride;
            idx++;
        }
    }

    private static void DrawCooldownTimerText(ImDrawListPtr draw, Vector2 min, Vector2 max, float cooldownSecondsRemaining, float alpha)
    {
        // Do not render timer text for GCD-only cooldowns that don't expose remaining seconds.
        if (cooldownSecondsRemaining <= 0.01f)
        {
            return;
        }

        var text = FormatCooldownTime(cooldownSecondsRemaining);
        if (text.Length == 0)
        {
            return;
        }

        var font = ImGui.GetFont();
        var fontSize = ImGui.GetFontSize() * 1.95f;
        var textSize = EstimateScaledTextSize(text, fontSize, ImGui.GetFontSize());
        var bottomInset = 2f;
        var pos = PixelSnap(new Vector2(
            min.X + ((max.X - min.X - textSize.X) * 0.5f),
            max.Y - textSize.Y - bottomInset));

        var shadow = ApplyAlpha(0xE0000000, alpha);
        var color = ApplyAlpha(0xFFFFFFFF, alpha);
        draw.AddText(font, fontSize, new Vector2(pos.X - 1f, pos.Y), shadow, text);
        draw.AddText(font, fontSize, new Vector2(pos.X + 1f, pos.Y), shadow, text);
        draw.AddText(font, fontSize, new Vector2(pos.X, pos.Y - 1f), shadow, text);
        draw.AddText(font, fontSize, new Vector2(pos.X, pos.Y + 1f), shadow, text);
        draw.AddText(font, fontSize, pos, color, text);
    }

    private static void DrawArc(ImDrawListPtr draw, Vector2 center, float radius, float startAngle, float endAngle, uint color, float thickness, int segments)
    {
        var delta = endAngle - startAngle;
        if (MathF.Abs(delta) <= 0.01f)
        {
            return;
        }

        draw.PathClear();
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var a = startAngle + (delta * t);
            draw.PathLineTo(new Vector2(center.X + MathF.Cos(a) * radius, center.Y + MathF.Sin(a) * radius));
        }

        draw.PathStroke(color, ImDrawFlags.None, thickness);
    }

    private static void DrawRaisedCircularBorder(ImDrawListPtr draw, Vector2 center, float radius, float thickness, float alpha, uint baseAccent)
    {
        var outerShadow = ApplyAlpha(0xFF223A5A, alpha);
        var innerShadow = ApplyAlpha(0xFF14263A, alpha);
        var core = ApplyAlpha(baseAccent, alpha);
        var brightRim = ApplyAlpha(0xFF9FE9FF, alpha);
        var topShine = ApplyAlpha(0xA0C8F7FF, alpha);
        var bottomShade = ApplyAlpha(0x80342012, alpha);

        draw.AddCircle(center, radius + thickness * 0.70f, outerShadow, 96, Math.Max(1f, thickness * 0.50f));
        draw.AddCircle(center, radius, core, 96, Math.Max(1f, thickness));
        draw.AddCircle(center, radius - thickness * 0.55f, brightRim, 96, Math.Max(1f, thickness * 0.35f));
        draw.AddCircle(center, radius - thickness * 1.00f, innerShadow, 96, Math.Max(1f, thickness * 0.25f));

        // Directional highlight/shadow to create a beveled metal read.
        DrawArc(
            draw,
            center,
            radius - thickness * 0.10f,
            -2.55f,
            -0.70f,
            topShine,
            Math.Max(1f, thickness * 0.40f),
            48);
        DrawArc(
            draw,
            center,
            radius + thickness * 0.10f,
            0.55f,
            2.65f,
            bottomShade,
            Math.Max(1f, thickness * 0.45f),
            48);
    }

    private static void DrawVerticalCircleFill(ImDrawListPtr draw, Vector2 center, float radius, float fillRatio, uint color, int segments)
    {
        var ratio = float.Clamp(fillRatio, 0f, 1f);
        if (ratio <= 0f)
        {
            return;
        }

        var clipTop = center.Y + radius - (2f * radius * ratio);
        draw.PathClear();
        var pointCount = 0;
        for (var i = 0; i <= segments; i++)
        {
            var a = (MathF.PI * 2f) * (i / (float)segments);
            var p = new Vector2(center.X + MathF.Cos(a) * radius, center.Y + MathF.Sin(a) * radius);
            if (p.Y >= clipTop)
            {
                draw.PathLineTo(p);
                pointCount++;
            }
        }

        if (pointCount > 2)
        {
            draw.PathFillConvex(color);
        }
    }

    private static void DrawVerticalCircleBand(ImDrawListPtr draw, Vector2 center, float radius, float lowRatio, float highRatio, uint color, int segments)
    {
        var low = Math.Clamp(lowRatio, 0f, 1f);
        var high = Math.Clamp(highRatio, 0f, 1f);
        if (high <= low + 0.0005f)
        {
            return;
        }

        var top = center.Y + radius - (2f * radius * high);
        var bottom = center.Y + radius - (2f * radius * low);
        draw.PathClear();
        var pointCount = 0;
        for (var i = 0; i <= segments; i++)
        {
            var a = (MathF.PI * 2f) * (i / (float)segments);
            var p = new Vector2(center.X + MathF.Cos(a) * radius, center.Y + MathF.Sin(a) * radius);
            if (p.Y >= top && p.Y <= bottom)
            {
                draw.PathLineTo(p);
                pointCount++;
            }
        }

        if (pointCount > 2)
        {
            draw.PathFillConvex(color);
        }
    }

    private static void DrawVerticalGradientCircleOverlay(
        ImDrawListPtr draw,
        Vector2 center,
        float radius,
        float fillRatio,
        uint bottomColor,
        uint topColor,
        float thickness)
    {
        var ratio = Math.Clamp(fillRatio, 0f, 1f);
        if (ratio <= 0.001f || radius <= 1f)
        {
            return;
        }

        var clipTop = center.Y + radius - (2f * radius * ratio);
        var yStart = Math.Max(clipTop, center.Y - radius);
        var yEnd = center.Y + radius;
        var step = Math.Max(1f, thickness);
        for (var y = yStart; y <= yEnd; y += step)
        {
            var dy = y - center.Y;
            var xRadiusSq = (radius * radius) - (dy * dy);
            if (xRadiusSq <= 0f)
            {
                continue;
            }

            var xRadius = MathF.Sqrt(xRadiusSq);
            var xMin = center.X - xRadius;
            var xMax = center.X + xRadius;
            var t = Math.Clamp((y - yStart) / Math.Max(1f, yEnd - yStart), 0f, 1f);
            var color = LerpAbgr(bottomColor, topColor, 1f - t);
            draw.AddLine(new Vector2(xMin, y), new Vector2(xMax, y), color, step);
        }
    }

    private static uint LerpAbgr(uint from, uint to, float t)
    {
        var clamped = Math.Clamp(t, 0f, 1f);
        var fa = (from >> 24) & 0xFF;
        var fb = (from >> 16) & 0xFF;
        var fg = (from >> 8) & 0xFF;
        var fr = from & 0xFF;
        var ta = (to >> 24) & 0xFF;
        var tb = (to >> 16) & 0xFF;
        var tg = (to >> 8) & 0xFF;
        var tr = to & 0xFF;

        var a = (uint)(fa + ((ta - fa) * clamped));
        var b = (uint)(fb + ((tb - fb) * clamped));
        var g = (uint)(fg + ((tg - fg) * clamped));
        var r = (uint)(fr + ((tr - fr) * clamped));
        return (a << 24) | (b << 16) | (g << 8) | r;
    }

    private static void DrawHorizontalGradientCircleOverlay(
        ImDrawListPtr draw,
        Vector2 center,
        float radius,
        float fillRatio,
        uint leftColor,
        uint rightColor,
        float thickness)
    {
        var ratio = Math.Clamp(fillRatio, 0f, 1f);
        if (ratio <= 0.001f || radius <= 1f)
        {
            return;
        }

        var clipTop = center.Y + radius - (2f * radius * ratio);
        var xStart = center.X - radius;
        var xEnd = center.X + radius;
        var step = Math.Max(1f, thickness);
        for (var x = xStart; x <= xEnd; x += step)
        {
            var dx = x - center.X;
            var yRadiusSq = (radius * radius) - (dx * dx);
            if (yRadiusSq <= 0f)
            {
                continue;
            }

            var yRadius = MathF.Sqrt(yRadiusSq);
            var yMin = center.Y - yRadius;
            var yMax = center.Y + yRadius;
            if (yMax < clipTop)
            {
                continue;
            }

            yMin = Math.Max(yMin, clipTop);
            var t = Math.Clamp((x - xStart) / Math.Max(1f, xEnd - xStart), 0f, 1f);
            var color = LerpAbgr(leftColor, rightColor, t);
            draw.AddLine(new Vector2(x, yMin), new Vector2(x, yMax), color, step);
        }
    }

    private static void DrawCross(ImDrawListPtr draw, Vector2 point, uint color)
    {
        draw.AddLine(new Vector2(point.X - 6f, point.Y), new Vector2(point.X + 6f, point.Y), color, 1f);
        draw.AddLine(new Vector2(point.X, point.Y - 6f), new Vector2(point.X, point.Y + 6f), color, 1f);
    }

    private static void DrawArcEndCap(ImDrawListPtr draw, Vector2 center, float radius, float angle, float thickness, uint color, int segments)
    {
        // Draw a straight radial separator line instead of a rounded cap.
        var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var outer = center + dir * (radius + thickness * 0.52f);
        var inner = center + dir * (radius - thickness * 0.52f);
        draw.AddLine(inner, outer, color, Math.Max(1f, thickness * 0.22f));
    }

    private static uint ApplyAlpha(uint rgba, float alpha)
    {
        var a = (byte)(Math.Clamp(alpha, 0f, 1f) * ((rgba >> 24) & 0xFF));
        return (rgba & 0x00FFFFFF) | ((uint)a << 24);
    }

    private static Vector2 EstimateScaledTextSize(string text, float targetFontSize, float baseFontSize)
    {
        var baseSize = ImGui.CalcTextSize(text);
        if (baseFontSize <= 0.001f)
        {
            return baseSize;
        }

        var scale = targetFontSize / baseFontSize;
        return baseSize * scale;
    }

    private static string FormatStatusTime(float remainingTime)
    {
        if (remainingTime <= 0.05f)
        {
            return string.Empty;
        }

        if (remainingTime >= 10f)
        {
            return $"{MathF.Floor(remainingTime):0}";
        }

        return $"{remainingTime:0.0}";
    }

    private static string FormatCooldownTime(float secondsRemaining)
    {
        if (secondsRemaining <= 0.01f)
        {
            return string.Empty;
        }

        if (secondsRemaining >= 10f)
        {
            return $"{MathF.Ceiling(secondsRemaining):0}";
        }

        return $"{secondsRemaining:0.0}";
    }

    private static void DrawStatusTooltipIfHovered(StatusViewModel status, Vector2 rectMin, Vector2 rectMax)
    {
        var mousePos = ImGui.GetMousePos();
        var isHovered = mousePos.X >= rectMin.X &&
                        mousePos.X <= rectMax.X &&
                        mousePos.Y >= rectMin.Y &&
                        mousePos.Y <= rectMax.Y;
        if (!isHovered)
        {
            return;
        }

        var tooltipPos = new Vector2(rectMin.X, rectMax.Y + 10f);
        ImGui.SetNextWindowPos(tooltipPos);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14f, 10f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);
        ImGui.PushStyleColor(ImGuiCol.PopupBg, 0xD0181818);
        ImGui.PushStyleColor(ImGuiCol.Border, 0x90303030);

        ImGui.BeginTooltip();
        ImGui.SetWindowFontScale(1.5f);
        var titleColor = status.IsDebuff ? 0xFFFFB6B6 : 0xFFFFFFFF;
        ImGui.TextColored(titleColor, status.Name);
        if (!string.IsNullOrWhiteSpace(status.Description))
        {
            ImGui.Spacing();
            ImGui.PushTextWrapPos(700f);
            ImGui.TextUnformatted(status.Description);
            ImGui.PopTextWrapPos();
        }

        ImGui.SetWindowFontScale(1.0f);
        ImGui.EndTooltip();
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(2);
    }

    private static uint GetStatusTimerColor(float remainingTime, uint fallback)
    {
        if (remainingTime <= 0.05f)
        {
            return fallback;
        }

        var alpha = fallback & 0xFF000000;

        if (remainingTime < 3f)
        {
            // ABGR packed: red
            return alpha | 0x005A5AFF;
        }

        if (remainingTime <= 5f)
        {
            // ABGR packed: yellow
            return alpha | 0x005AD9FF;
        }

        // ABGR packed: green
        return alpha | 0x007DFF7D;
    }

    private static Vector2 PixelSnap(Vector2 point)
    {
        return new Vector2(MathF.Round(point.X), MathF.Round(point.Y));
    }

    private static void DrawHotbarActionTooltipIfHovered(HotbarSlotViewModel slot, Vector2 rectMin, Vector2 rectMax, float alpha)
    {
        if (slot.ActionId == 0)
        {
            return;
        }

        var mousePos = ImGui.GetMousePos();
        var isHovered = mousePos.X >= rectMin.X &&
                        mousePos.X <= rectMax.X &&
                        mousePos.Y >= rectMin.Y &&
                        mousePos.Y <= rectMax.Y;
        if (!isHovered)
        {
            return;
        }

        var tooltipPos = new Vector2(rectMin.X + 4f, rectMax.Y + 10f);
        ImGui.SetNextWindowPos(tooltipPos);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16f, 14f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 9f);
        ImGui.PushStyleColor(ImGuiCol.PopupBg, ApplyAlpha(0xEE1A1C22, alpha));
        ImGui.PushStyleColor(ImGuiCol.Border, ApplyAlpha(0xA0404248, alpha));

        ImGui.BeginTooltip();
        var iconSize = new Vector2(48f, 48f);
        if (slot.Icon is not null)
        {
            var wrap = slot.Icon.GetWrapOrEmpty();
            ImGui.Image(wrap.Handle, iconSize);
        }
        else
        {
            ImGui.Dummy(iconSize);
        }

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.SetWindowFontScale(1.25f);
        ImGui.TextUnformatted(slot.Label);
        ImGui.SetWindowFontScale(1.0f);

        ImGui.TextColored(0xFFC0C6D2, $"{slot.TooltipKindLabel} [{slot.TooltipId}]");
        var cast = slot.CastTimeSeconds <= 0.05f ? "Instant" : $"{slot.CastTimeSeconds:0.0}s";
        var recast = slot.RecastTimeSeconds <= 0.05f ? "--" : $"{slot.RecastTimeSeconds:0.0}s";
        var range = slot.RangeYalms > 0 ? $"{slot.RangeYalms}y" : "--";
        var radius = slot.RadiusYalms > 0 ? $"{slot.RadiusYalms}y" : "--";
        ImGui.TextColored(0xFFE0E0E0, $"Cast {cast}    Recast {recast}");
        ImGui.TextColored(0xFFE0E0E0, $"Range {range}    Radius {radius}");
        ImGui.EndGroup();

        ImGui.Separator();
        if (!string.IsNullOrWhiteSpace(slot.Description))
        {
            ImGui.PushTextWrapPos(720f);
            ImGui.TextUnformatted(slot.Description);
            ImGui.PopTextWrapPos();
        }
        else
        {
            ImGui.TextUnformatted("No description available.");
        }

        if (!string.IsNullOrWhiteSpace(slot.JobAbbrev) || slot.RequiredLevel > 0)
        {
            ImGui.Spacing();
            var req = slot.RequiredLevel > 0 ? $"Lv. {slot.RequiredLevel}" : "--";
            var job = string.IsNullOrWhiteSpace(slot.JobAbbrev) ? "--" : slot.JobAbbrev;
            ImGui.TextColored(0xFFBDD77C, $"Acquired {req}    Affinity {job}");
        }

        ImGui.EndTooltip();
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(2);
    }
}
