using System.Numerics;

namespace FFXIVHudPlugin;

internal static class HudLayoutMigration
{
    public static void MigrateLayoutOffsetsIfNeeded(HudConfiguration config, Vector2 viewportPos, Vector2 viewportSize)
    {
        if (!config.LayoutUsesScreenCenterOrigin)
        {
            MigrateLegacyAnchorToScreenCenter(config, viewportPos, viewportSize);
        }

        if (!config.LayoutUsesUnscaledPixelOffsets)
        {
            MigrateScaledOffsetsToUnscaledCenterAnchors(config, viewportPos, viewportSize);
        }

        MigrateRunawayMinimapOffsetsIfNeeded(config, viewportSize);
    }

    private static void MigrateRunawayMinimapOffsetsIfNeeded(HudConfiguration config, Vector2 viewportSize)
    {
        var bounds = MinimapLayout.GetOffsetBounds(viewportSize, config.MinimapSize);
        var clampedX = bounds.ClampX(config.MinimapOffsetX);
        var clampedY = bounds.ClampY(config.MinimapOffsetY);
        if (MathF.Abs(clampedX - config.MinimapOffsetX) < 0.01f &&
            MathF.Abs(clampedY - config.MinimapOffsetY) < 0.01f)
        {
            return;
        }

        config.MinimapOffsetX = clampedX;
        config.MinimapOffsetY = clampedY;
        config.Save();
    }

    private static void MigrateLegacyAnchorToScreenCenter(HudConfiguration config, Vector2 viewportPos, Vector2 viewportSize)
    {
        var uiScale = config.GlobalScale;
        var legacyOrigin = viewportPos + (viewportSize * config.CenterAnchor) + new Vector2(config.HudOffsetX, config.HudOffsetY);
        var screenCenter = HudLayoutOrigin.GetScreenCenter(viewportPos, viewportSize);
        var newHudOffset = Vector2.Zero;

        var laneYOffset = config.HotbarVerticalOffset * uiScale;
        var hotbar1SlotSize = HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar1BarIndex);
        var hotbar1SlotGap = HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar1BarIndex);
        var hotbar2SlotSize = HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar2BarIndex);
        var hotbar2SlotGap = HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar2BarIndex);
        var orbRadius = config.OrbRadius * uiScale;
        var laneEdgeGap = 34f * uiScale;
        var stackGap = HotbarGridLayout.HotbarStackGap * uiScale;
        var statusGapAboveHotbar = 14f * uiScale;
        var timerReserve = 15f * uiScale;

        var legacyOrbCenter = legacyOrigin + (new Vector2(config.OrbOffsetX, config.OrbOffsetY) * uiScale);

        var hotbar1Width = HotbarGridLayout.GetGridWidth(config.Hotbar1SlotsPerRow, hotbar1SlotSize, hotbar1SlotGap);
        var hotbar1Height = HotbarGridLayout.GetGridHeight(
            config.Hotbar1VisibleSlotCount,
            config.Hotbar1SlotsPerRow,
            hotbar1SlotSize,
            hotbar1SlotGap);
        var legacyHotbar1Start = new Vector2(
            legacyOrigin.X - (hotbar1Width * 0.5f),
            legacyOrigin.Y + laneYOffset) + (new Vector2(config.Hotbar1OffsetX, config.Hotbar1OffsetY) * uiScale);
        var legacyHotbar1Center = legacyHotbar1Start + new Vector2(hotbar1Width * 0.5f, hotbar1Height * 0.5f);

        var hotbar2Width = HotbarGridLayout.GetGridWidth(config.Hotbar2SlotsPerRow, hotbar2SlotSize, hotbar2SlotGap);
        var hotbar2Height = HotbarGridLayout.GetGridHeight(
            config.Hotbar2VisibleSlotCount,
            config.Hotbar2SlotsPerRow,
            hotbar2SlotSize,
            hotbar2SlotGap);
        var legacyHotbar2Start = new Vector2(
            legacyOrigin.X - (hotbar2Width * 0.5f),
            legacyHotbar1Start.Y + hotbar1Height + stackGap) + (new Vector2(config.Hotbar2OffsetX, config.Hotbar2OffsetY) * uiScale);
        var legacyHotbar2Center = legacyHotbar2Start + new Vector2(hotbar2Width * 0.5f, hotbar2Height * 0.5f);

        var legacyBuffStart = new Vector2(
            legacyOrigin.X - orbRadius - laneEdgeGap + (config.BuffOffsetX * uiScale),
            legacyOrigin.Y + laneYOffset - config.BuffIconSize - timerReserve - statusGapAboveHotbar + (config.BuffOffsetY * uiScale));
        var legacyDebuffStart = new Vector2(
            legacyOrigin.X + orbRadius + laneEdgeGap + (config.DebuffOffsetX * uiScale),
            legacyOrigin.Y + laneYOffset - config.DebuffIconSize - timerReserve - statusGapAboveHotbar + (config.DebuffOffsetY * uiScale));
        var legacyLimitBreakCenter = new Vector2(
            legacyOrbCenter.X - (150f * uiScale),
            legacyOrbCenter.Y + (config.LimitBreakYOffset * uiScale));

        ApplyElementCenters(
            config,
            screenCenter,
            newHudOffset,
            legacyOrbCenter,
            legacyHotbar1Center,
            legacyHotbar2Center,
            StatusLaneLayout.GetLaneCenterFromStart(
                legacyBuffStart,
                config.BuffGrowDirection,
                StatusLaneLayout.ClampMaxIconsPerRow(config.BuffMaxIconsPerRow),
                config.BuffIconSize,
                config.BuffIconGap),
            StatusLaneLayout.GetLaneCenterFromStart(
                legacyDebuffStart,
                config.DebuffGrowDirection,
                StatusLaneLayout.ClampMaxIconsPerRow(config.DebuffMaxIconsPerRow),
                config.DebuffIconSize,
                config.DebuffIconGap),
            legacyLimitBreakCenter);

        config.CenterAnchor = new Vector2(0.5f, 0.5f);
        config.HotbarVerticalOffset = 0f;
        config.LayoutUsesScreenCenterOrigin = true;
        config.Save();
    }

    private static void MigrateScaledOffsetsToUnscaledCenterAnchors(
        HudConfiguration config,
        Vector2 viewportPos,
        Vector2 viewportSize)
    {
        var uiScale = config.GlobalScale;
        var screenCenter = HudLayoutOrigin.GetScreenCenter(viewportPos, viewportSize);
        var hudOffset = new Vector2(config.HudOffsetX, config.HudOffsetY);

        var orbCenter = screenCenter + hudOffset + (new Vector2(config.OrbOffsetX, config.OrbOffsetY) * uiScale);

        var hotbar1SlotSize = HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar1BarIndex);
        var hotbar1SlotGap = HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar1BarIndex);
        var hotbar2SlotSize = HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar2BarIndex);
        var hotbar2SlotGap = HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar2BarIndex);
        var hotbar1Width = HotbarGridLayout.GetGridWidth(config.Hotbar1SlotsPerRow, hotbar1SlotSize, hotbar1SlotGap);
        var hotbar1Height = HotbarGridLayout.GetGridHeight(
            config.Hotbar1VisibleSlotCount,
            config.Hotbar1SlotsPerRow,
            hotbar1SlotSize,
            hotbar1SlotGap);
        var hotbar1Center = screenCenter + hudOffset + (new Vector2(config.Hotbar1OffsetX, config.Hotbar1OffsetY) * uiScale);

        var hotbar2Width = HotbarGridLayout.GetGridWidth(config.Hotbar2SlotsPerRow, hotbar2SlotSize, hotbar2SlotGap);
        var hotbar2Height = HotbarGridLayout.GetGridHeight(
            config.Hotbar2VisibleSlotCount,
            config.Hotbar2SlotsPerRow,
            hotbar2SlotSize,
            hotbar2SlotGap);
        var hotbar2Center = screenCenter + hudOffset + (new Vector2(config.Hotbar2OffsetX, config.Hotbar2OffsetY) * uiScale);

        var buffStart = screenCenter + hudOffset + (new Vector2(config.BuffOffsetX, config.BuffOffsetY) * uiScale);
        var debuffStart = screenCenter + hudOffset + (new Vector2(config.DebuffOffsetX, config.DebuffOffsetY) * uiScale);
        var limitBreakCenter = screenCenter + hudOffset + (new Vector2(config.LimitBreakOffsetX, config.LimitBreakYOffset) * uiScale);

        ApplyElementCenters(
            config,
            screenCenter,
            Vector2.Zero,
            orbCenter,
            hotbar1Center,
            hotbar2Center,
            StatusLaneLayout.GetLaneCenterFromStart(
                buffStart,
                config.BuffGrowDirection,
                StatusLaneLayout.ClampMaxIconsPerRow(config.BuffMaxIconsPerRow),
                config.BuffIconSize,
                config.BuffIconGap),
            StatusLaneLayout.GetLaneCenterFromStart(
                debuffStart,
                config.DebuffGrowDirection,
                StatusLaneLayout.ClampMaxIconsPerRow(config.DebuffMaxIconsPerRow),
                config.DebuffIconSize,
                config.DebuffIconGap),
            limitBreakCenter);

        config.HudOffsetX = 0f;
        config.HudOffsetY = 0f;
        config.LayoutUsesUnscaledPixelOffsets = true;
        config.Save();
    }

    private static void ApplyElementCenters(
        HudConfiguration config,
        Vector2 screenCenter,
        Vector2 hudOffset,
        Vector2 orbCenter,
        Vector2 hotbar1Center,
        Vector2 hotbar2Center,
        Vector2 buffCenter,
        Vector2 debuffCenter,
        Vector2 limitBreakCenter)
    {
        config.HudOffsetX = hudOffset.X;
        config.HudOffsetY = hudOffset.Y;
        config.OrbOffsetX = orbCenter.X - screenCenter.X - hudOffset.X;
        config.OrbOffsetY = orbCenter.Y - screenCenter.Y - hudOffset.Y;
        config.Hotbar1OffsetX = hotbar1Center.X - screenCenter.X - hudOffset.X;
        config.Hotbar1OffsetY = hotbar1Center.Y - screenCenter.Y - hudOffset.Y;
        config.Hotbar2OffsetX = hotbar2Center.X - screenCenter.X - hudOffset.X;
        config.Hotbar2OffsetY = hotbar2Center.Y - screenCenter.Y - hudOffset.Y;
        config.BuffOffsetX = buffCenter.X - screenCenter.X - hudOffset.X;
        config.BuffOffsetY = buffCenter.Y - screenCenter.Y - hudOffset.Y;
        config.DebuffOffsetX = debuffCenter.X - screenCenter.X - hudOffset.X;
        config.DebuffOffsetY = debuffCenter.Y - screenCenter.Y - hudOffset.Y;
        config.LimitBreakOffsetX = limitBreakCenter.X - screenCenter.X - hudOffset.X;
        config.LimitBreakYOffset = limitBreakCenter.Y - screenCenter.Y - hudOffset.Y;
    }

    public static void ConvertPresetSnapshotOffsets(HudLayoutPresetSnapshot snapshot, Vector2 viewportSize)
    {
        var config = new HudConfiguration
        {
            GlobalScale = snapshot.GlobalScale,
            CenterAnchor = snapshot.CenterAnchor,
            HudOffsetX = snapshot.HudOffsetX,
            HudOffsetY = snapshot.HudOffsetY,
            OrbRadius = snapshot.OrbRadius,
            OrbOffsetX = snapshot.OrbOffsetX,
            OrbOffsetY = snapshot.OrbOffsetY,
            Hotbar1SlotSize = snapshot.Hotbar1SlotSize > 0f ? snapshot.Hotbar1SlotSize : snapshot.HotbarSlotSize,
            Hotbar1SlotGap = snapshot.Hotbar1SlotGap >= 0f ? snapshot.Hotbar1SlotGap : snapshot.HotbarSlotGap,
            Hotbar2SlotSize = snapshot.Hotbar2SlotSize > 0f ? snapshot.Hotbar2SlotSize : snapshot.HotbarSlotSize,
            Hotbar2SlotGap = snapshot.Hotbar2SlotGap >= 0f ? snapshot.Hotbar2SlotGap : snapshot.HotbarSlotGap,
            HotbarSlotSize = snapshot.HotbarSlotSize,
            HotbarSlotGap = snapshot.HotbarSlotGap,
            HotbarVerticalOffset = snapshot.HotbarVerticalOffset,
            Hotbar1OffsetX = snapshot.Hotbar1OffsetX,
            Hotbar1OffsetY = snapshot.Hotbar1OffsetY,
            Hotbar2OffsetX = snapshot.Hotbar2OffsetX,
            Hotbar2OffsetY = snapshot.Hotbar2OffsetY,
            Hotbar1VisibleSlotCount = snapshot.Hotbar1VisibleSlotCount,
            Hotbar2VisibleSlotCount = snapshot.Hotbar2VisibleSlotCount,
            Hotbar1SlotsPerRow = snapshot.Hotbar1SlotsPerRow <= 0
                ? HotbarGridLayout.DefaultSlotsPerRow
                : snapshot.Hotbar1SlotsPerRow,
            Hotbar2SlotsPerRow = snapshot.Hotbar2SlotsPerRow <= 0
                ? HotbarGridLayout.DefaultSlotsPerRow
                : snapshot.Hotbar2SlotsPerRow,
            BuffOffsetX = snapshot.BuffOffsetX,
            BuffOffsetY = snapshot.BuffOffsetY,
            DebuffOffsetX = snapshot.DebuffOffsetX,
            DebuffOffsetY = snapshot.DebuffOffsetY,
            BuffIconSize = snapshot.BuffIconSize,
            DebuffIconSize = snapshot.DebuffIconSize,
            BuffGrowDirection = snapshot.BuffGrowDirection,
            DebuffGrowDirection = snapshot.DebuffGrowDirection,
            BuffMaxIconsPerRow = snapshot.BuffMaxIconsPerRow,
            DebuffMaxIconsPerRow = snapshot.DebuffMaxIconsPerRow,
            BuffIconGap = snapshot.BuffIconGap,
            DebuffIconGap = snapshot.DebuffIconGap,
            LimitBreakYOffset = snapshot.LimitBreakYOffset,
            LimitBreakOffsetX = snapshot.LimitBreakOffsetX,
            LayoutUsesScreenCenterOrigin = snapshot.LayoutUsesScreenCenterOrigin,
            LayoutUsesUnscaledPixelOffsets = snapshot.LayoutUsesUnscaledPixelOffsets,
        };

        MigrateLayoutOffsetsIfNeeded(config, Vector2.Zero, viewportSize);

        snapshot.CenterAnchor = config.CenterAnchor;
        snapshot.HudOffsetX = config.HudOffsetX;
        snapshot.HudOffsetY = config.HudOffsetY;
        snapshot.HotbarVerticalOffset = config.HotbarVerticalOffset;
        snapshot.OrbOffsetX = config.OrbOffsetX;
        snapshot.OrbOffsetY = config.OrbOffsetY;
        snapshot.Hotbar1OffsetX = config.Hotbar1OffsetX;
        snapshot.Hotbar1OffsetY = config.Hotbar1OffsetY;
        snapshot.Hotbar2OffsetX = config.Hotbar2OffsetX;
        snapshot.Hotbar2OffsetY = config.Hotbar2OffsetY;
        snapshot.BuffOffsetX = config.BuffOffsetX;
        snapshot.BuffOffsetY = config.BuffOffsetY;
        snapshot.DebuffOffsetX = config.DebuffOffsetX;
        snapshot.DebuffOffsetY = config.DebuffOffsetY;
        snapshot.LimitBreakYOffset = config.LimitBreakYOffset;
        snapshot.LimitBreakOffsetX = config.LimitBreakOffsetX;
        snapshot.LayoutUsesScreenCenterOrigin = config.LayoutUsesScreenCenterOrigin;
        snapshot.LayoutUsesUnscaledPixelOffsets = config.LayoutUsesUnscaledPixelOffsets;
    }
}
