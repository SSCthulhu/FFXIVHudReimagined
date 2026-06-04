using System.Numerics;

namespace FFXIVHudPlugin;

public static class StatusLaneLayout
{
    public const int DefaultMaxIconsPerRow = 10;
    public const int MinMaxIconsPerRow = 1;
    public const int MaxMaxIconsPerRow = 24;
    public const int MaxStatusRows = 3;
    private const float StatusIconWidthScale = 0.78f;
    private const float StatusTimerRowReserve = 22f;
    private const float StatusRowGap = 6f;

    public static int ClampMaxIconsPerRow(int maxIconsPerRow) =>
        Math.Clamp(maxIconsPerRow, MinMaxIconsPerRow, MaxMaxIconsPerRow);

    public static int GetMaxVisibleStatusCount(int maxIconsPerRow) =>
        ClampMaxIconsPerRow(maxIconsPerRow) * MaxStatusRows;

    public static bool IsHorizontalGrowRightToLeft(StatusLaneGrowDirection growDirection) =>
        growDirection is StatusLaneGrowDirection.RightToLeftUp or StatusLaneGrowDirection.RightToLeftDown;

    public static bool IsRowGrowthUp(StatusLaneGrowDirection growDirection) =>
        growDirection is StatusLaneGrowDirection.LeftToRightUp or StatusLaneGrowDirection.RightToLeftUp;

    public static StatusLaneGrowDirection MigrateLegacyGrowDirection(int legacyValue) =>
        legacyValue == 1
            ? StatusLaneGrowDirection.RightToLeftUp
            : StatusLaneGrowDirection.LeftToRightUp;

    public static float GetIconWidth(float iconHeight) => iconHeight * StatusIconWidthScale;

    public static Vector2 GetMaxLaneSize(
        StatusLaneGrowDirection growDirection,
        int maxIconsPerRow,
        float iconHeight,
        float iconGap)
    {
        var iconWidth = GetIconWidth(iconHeight);
        var columns = ClampMaxIconsPerRow(maxIconsPerRow);
        var laneWidth = columns <= 0 ? 0f : (iconWidth * columns) + (iconGap * (columns - 1));
        var rowStep = iconHeight + StatusTimerRowReserve + StatusRowGap;
        var laneHeight = iconHeight + (rowStep * (MaxStatusRows - 1));
        return new Vector2(laneWidth, laneHeight);
    }

    public static Vector2 GetLaneCenterFromStart(
        Vector2 start,
        StatusLaneGrowDirection growDirection,
        int maxIconsPerRow,
        float iconHeight,
        float iconGap)
    {
        var iconWidth = GetIconWidth(iconHeight);
        var laneSize = GetMaxLaneSize(growDirection, maxIconsPerRow, iconHeight, iconGap);
        var growRightToLeft = IsHorizontalGrowRightToLeft(growDirection);
        var growRowsUp = IsRowGrowthUp(growDirection);

        var centerX = growRightToLeft
            ? start.X - (laneSize.X * 0.5f)
            : start.X + (laneSize.X * 0.5f);
        var centerY = growRowsUp
            ? start.Y + (iconHeight * 0.5f) - (laneSize.Y - iconHeight) * 0.5f
            : start.Y + (laneSize.Y * 0.5f) - (iconHeight * 0.5f);
        return new Vector2(centerX, centerY);
    }

    public static Vector2 GetLaneStartFromCenter(
        Vector2 center,
        StatusLaneGrowDirection growDirection,
        int maxIconsPerRow,
        float iconHeight,
        float iconGap)
    {
        var iconWidth = GetIconWidth(iconHeight);
        var laneSize = GetMaxLaneSize(growDirection, maxIconsPerRow, iconHeight, iconGap);
        var growRightToLeft = IsHorizontalGrowRightToLeft(growDirection);
        var growRowsUp = IsRowGrowthUp(growDirection);

        var startX = growRightToLeft
            ? center.X + (laneSize.X * 0.5f)
            : center.X - (laneSize.X * 0.5f);
        var startY = growRowsUp
            ? center.Y - (iconHeight * 0.5f) + (laneSize.Y - iconHeight) * 0.5f
            : center.Y - (laneSize.Y * 0.5f) + (iconHeight * 0.5f);
        return new Vector2(startX, startY);
    }
}
