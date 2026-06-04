namespace FFXIVHudPlugin;

public static class HotbarLayout
{
    public const float DefaultSlotSize = 44f;
    public const float DefaultSlotGap = 8f;
    public const float MinSlotSize = 26f;
    public const float MaxSlotSize = 80f;
    public const float MinSlotGap = 0f;
    public const float MaxSlotGap = 18f;

    public static float GetSlotSize(HudConfiguration config, int barIndex) =>
        barIndex == GameHotbar.Hotbar2BarIndex ? config.Hotbar2SlotSize : config.Hotbar1SlotSize;

    public static float GetSlotGap(HudConfiguration config, int barIndex) =>
        barIndex == GameHotbar.Hotbar2BarIndex ? config.Hotbar2SlotGap : config.Hotbar1SlotGap;

    public static float GetScaledSlotSize(HudConfiguration config, int barIndex) =>
        GetSlotSize(config, barIndex) * config.GlobalScale;

    public static float GetScaledSlotGap(HudConfiguration config, int barIndex) =>
        GetSlotGap(config, barIndex) * config.GlobalScale;
}
