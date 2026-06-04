namespace FFXIVHudPlugin;

public static class HotbarSlotVisibility
{
    public const int MaxSlotsPerSide = 6;
    public const int MaxTotalSlots = 12;
    public const int DefaultTotalSlots = 12;

    public static int ClampTotal(int visibleCount) =>
        Math.Clamp(visibleCount, 1, MaxTotalSlots);

    /// <summary>
    /// Slots 1-6 are the left lane; slots 7-12 are the right lane. Visibility always fills from slot 1
    /// and removes from the right (slot 12 first, then 11, and so on).
    /// </summary>
    public static int GetLeftVisibleCount(int visibleCount) =>
        Math.Min(MaxSlotsPerSide, ClampTotal(visibleCount));

    public static int GetRightVisibleCount(int visibleCount) =>
        Math.Max(0, ClampTotal(visibleCount) - MaxSlotsPerSide);

    public static bool IsGameSlotVisible(int gameSlotIndex, int visibleCount) =>
        gameSlotIndex >= 0 && gameSlotIndex < ClampTotal(visibleCount);

    public static float LaneLength(int slotCount, float slotSize, float slotGap)
    {
        if (slotCount <= 0)
        {
            return 0f;
        }

        if (slotCount == 1)
        {
            return slotSize;
        }

        return (slotSize * slotCount) + (slotGap * (slotCount - 1));
    }

    /// <summary>
    /// Maps a game hotbar slot index (0-11, slot 1-12 when displayed) to its draw index on the lane, or -1 if hidden.
    /// </summary>
    public static int TryGetDrawIndex(int gameSlotIndex, int visibleCount)
    {
        if (!IsGameSlotVisible(gameSlotIndex, visibleCount))
        {
            return -1;
        }

        return gameSlotIndex < MaxSlotsPerSide
            ? gameSlotIndex
            : gameSlotIndex - MaxSlotsPerSide;
    }

    public static IReadOnlyList<HotbarSlotViewModel> SliceLeft(
        IReadOnlyList<HotbarSlotViewModel> slots,
        int visibleCount)
    {
        var leftCount = GetLeftVisibleCount(visibleCount);
        if (leftCount <= 0 || slots.Count == 0)
        {
            return Array.Empty<HotbarSlotViewModel>();
        }

        return slots.Take(Math.Min(leftCount, slots.Count)).ToArray();
    }

    public static IReadOnlyList<HotbarSlotViewModel> SliceRight(
        IReadOnlyList<HotbarSlotViewModel> slots,
        int visibleCount)
    {
        var rightCount = GetRightVisibleCount(visibleCount);
        if (rightCount <= 0 || slots.Count == 0)
        {
            return Array.Empty<HotbarSlotViewModel>();
        }

        return slots.Take(Math.Min(rightCount, slots.Count)).ToArray();
    }
}
