using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace FFXIVHudPlugin;

public static class HudLayoutEngine
{
    public static HudLayoutRects Calculate(HudConfiguration config)
    {
        var viewport = ImGui.GetMainViewport();
        HudLayoutMigration.MigrateLayoutOffsetsIfNeeded(config, viewport.Pos, viewport.Size);

        var screenCenter = HudLayoutOrigin.GetScreenCenter(viewport.Pos, viewport.Size);
        var hudOffset = new Vector2(config.HudOffsetX, config.HudOffsetY);

        var orbCenter = HudLayoutOrigin.GetElementCenter(
            screenCenter,
            hudOffset,
            config.OrbOffsetX,
            config.OrbOffsetY);

        var hotbar1SlotSize = HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar1BarIndex);
        var hotbar1SlotGap = HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar1BarIndex);

        var hotbar1Width = HotbarGridLayout.GetGridWidth(config.Hotbar1SlotsPerRow, hotbar1SlotSize, hotbar1SlotGap);
        var hotbar1Height = HotbarGridLayout.GetGridHeight(
            config.Hotbar1VisibleSlotCount,
            config.Hotbar1SlotsPerRow,
            hotbar1SlotSize,
            hotbar1SlotGap);
        var hotbar1Center = HudLayoutOrigin.GetElementCenter(
            screenCenter,
            hudOffset,
            config.Hotbar1OffsetX,
            config.Hotbar1OffsetY);
        var hotbar1Start = HudLayoutOrigin.GetTopLeftFromCenter(hotbar1Center, hotbar1Width, hotbar1Height);

        var hotbar2SlotSize = HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar2BarIndex);
        var hotbar2SlotGap = HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar2BarIndex);

        var hotbar2Width = HotbarGridLayout.GetGridWidth(config.Hotbar2SlotsPerRow, hotbar2SlotSize, hotbar2SlotGap);
        var hotbar2Height = HotbarGridLayout.GetGridHeight(
            config.Hotbar2VisibleSlotCount,
            config.Hotbar2SlotsPerRow,
            hotbar2SlotSize,
            hotbar2SlotGap);
        var hotbar2Center = HudLayoutOrigin.GetElementCenter(
            screenCenter,
            hudOffset,
            config.Hotbar2OffsetX,
            config.Hotbar2OffsetY);
        var hotbar2Start = HudLayoutOrigin.GetTopLeftFromCenter(hotbar2Center, hotbar2Width, hotbar2Height);

        var buffCenter = HudLayoutOrigin.GetElementCenter(
            screenCenter,
            hudOffset,
            config.BuffOffsetX,
            config.BuffOffsetY);
        var leftBuffStart = StatusLaneLayout.GetLaneStartFromCenter(
            buffCenter,
            config.BuffGrowDirection,
            StatusLaneLayout.ClampMaxIconsPerRow(config.BuffMaxIconsPerRow),
            config.BuffIconSize,
            config.BuffIconGap);

        var debuffCenter = HudLayoutOrigin.GetElementCenter(
            screenCenter,
            hudOffset,
            config.DebuffOffsetX,
            config.DebuffOffsetY);
        var rightDebuffStart = StatusLaneLayout.GetLaneStartFromCenter(
            debuffCenter,
            config.DebuffGrowDirection,
            StatusLaneLayout.ClampMaxIconsPerRow(config.DebuffMaxIconsPerRow),
            config.DebuffIconSize,
            config.DebuffIconGap);

        var limitBreakCenter = HudLayoutOrigin.GetElementCenter(
            screenCenter,
            hudOffset,
            config.LimitBreakOffsetX,
            config.LimitBreakYOffset);

        var minimapCenter = HudLayoutOrigin.GetElementCenter(
            screenCenter,
            hudOffset,
            config.MinimapOffsetX,
            config.MinimapOffsetY);

        return new HudLayoutRects(
            screenCenter,
            orbCenter,
            hotbar1Start,
            hotbar2Start,
            leftBuffStart,
            rightDebuffStart,
            limitBreakCenter,
            minimapCenter);
    }
}
