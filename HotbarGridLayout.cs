using System.Numerics;

namespace FFXIVHudPlugin;

public static class HotbarGridLayout
{
    public const int DefaultSlotsPerRow = HotbarSlotVisibility.MaxTotalSlots;
    public const int MinSlotsPerRow = 1;
    public const int MaxSlotsPerRow = HotbarSlotVisibility.MaxTotalSlots;
    public const float HotbarStackGap = 8f;

    public static int ClampSlotsPerRow(int slotsPerRow) =>
        Math.Clamp(slotsPerRow, MinSlotsPerRow, MaxSlotsPerRow);

    public static int GetRowCount(int visibleSlotCount, int slotsPerRow)
    {
        var visible = HotbarSlotVisibility.ClampTotal(visibleSlotCount);
        var perRow = ClampSlotsPerRow(slotsPerRow);
        if (visible <= 0)
        {
            return 0;
        }

        return (visible + perRow - 1) / perRow;
    }

    public static float GetGridWidth(int slotsPerRow, float slotSize, float slotGap) =>
        HotbarSlotVisibility.LaneLength(ClampSlotsPerRow(slotsPerRow), slotSize, slotGap);

    public static float GetGridHeight(int visibleSlotCount, int slotsPerRow, float slotSize, float slotGap)
    {
        var rowCount = GetRowCount(visibleSlotCount, slotsPerRow);
        if (rowCount <= 0)
        {
            return 0f;
        }

        if (rowCount == 1)
        {
            return slotSize;
        }

        return (slotSize * rowCount) + (slotGap * (rowCount - 1));
    }

    public static Vector2 GetSlotTopLeft(
        Vector2 gridStart,
        int gameSlotIndex,
        int slotsPerRow,
        float slotSize,
        float slotGap)
    {
        var perRow = ClampSlotsPerRow(slotsPerRow);
        var column = gameSlotIndex % perRow;
        var row = gameSlotIndex / perRow;
        var stride = slotSize + slotGap;
        return new Vector2(
            gridStart.X + (column * stride),
            gridStart.Y + (row * stride));
    }

    public static bool TryGetSlotRect(
        Vector2 gridStart,
        int gameSlotIndex,
        int visibleSlotCount,
        int slotsPerRow,
        float slotSize,
        float slotGap,
        out Vector2 min,
        out Vector2 max)
    {
        min = Vector2.Zero;
        max = Vector2.Zero;
        if (!HotbarSlotVisibility.IsGameSlotVisible(gameSlotIndex, visibleSlotCount))
        {
            return false;
        }

        min = GetSlotTopLeft(gridStart, gameSlotIndex, slotsPerRow, slotSize, slotGap);
        max = min + new Vector2(slotSize, slotSize);
        return true;
    }
}
