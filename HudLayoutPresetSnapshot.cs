using System.Numerics;

namespace FFXIVHudPlugin;

[Serializable]
public sealed class HudLayoutPresetSnapshot
{
    public bool UnlockLayout { get; set; }
    public bool EnableStatusTooltips { get; set; } = true;
    public float GlobalScale { get; set; } = 1.5f;
    public float GlobalOpacity { get; set; } = 1.0f;
    public Vector2 CenterAnchor { get; set; } = new(0.5f, 0.5f);
    public bool LayoutUsesScreenCenterOrigin { get; set; }
    public bool LayoutUsesUnscaledPixelOffsets { get; set; } = true;
    public float HudOffsetX { get; set; }
    public float HudOffsetY { get; set; }
    public float OrbRadius { get; set; } = 56f;
    public float OrbOffsetX { get; set; }
    public float OrbOffsetY { get; set; }
    public float OrbThickness { get; set; } = 10f;
    public float MpRingThicknessScale { get; set; } = 0.55f;
    public float Hotbar1SlotSize { get; set; } = HotbarLayout.DefaultSlotSize;
    public float Hotbar1SlotGap { get; set; } = HotbarLayout.DefaultSlotGap;
    public float Hotbar2SlotSize { get; set; } = HotbarLayout.DefaultSlotSize;
    public float Hotbar2SlotGap { get; set; } = HotbarLayout.DefaultSlotGap;
    public float HotbarSlotSize { get; set; } = HotbarLayout.DefaultSlotSize;
    public float HotbarSlotGap { get; set; } = HotbarLayout.DefaultSlotGap;
    public bool Hotbar1Enabled { get; set; } = true;
    public bool Hotbar2Enabled { get; set; } = true;
    public float HotbarVerticalOffset { get; set; } = -14f;
    public float Hotbar1OffsetX { get; set; }
    public float Hotbar1OffsetY { get; set; }
    public float Hotbar2OffsetX { get; set; }
    public float Hotbar2OffsetY { get; set; }
    public int Hotbar1VisibleSlotCount { get; set; } = HotbarSlotVisibility.DefaultTotalSlots;
    public int Hotbar2VisibleSlotCount { get; set; } = HotbarSlotVisibility.DefaultTotalSlots;
    public int Hotbar1SlotsPerRow { get; set; } = HotbarGridLayout.DefaultSlotsPerRow;
    public int Hotbar2SlotsPerRow { get; set; } = HotbarGridLayout.DefaultSlotsPerRow;
    public float BuffRowOffsetX { get; set; }
    public float BuffRowYOffset { get; set; }
    public bool UseSplitStatusLayout { get; set; }
    public bool UsesFourWayGrowDirection { get; set; } = true;
    public float BuffOffsetX { get; set; }
    public float BuffOffsetY { get; set; }
    public StatusLaneGrowDirection BuffGrowDirection { get; set; } = StatusLaneGrowDirection.RightToLeftUp;
    public StatusTimerPlacement BuffTimerPlacement { get; set; } = StatusTimerPlacement.Bottom;
    public int BuffMaxIconsPerRow { get; set; } = StatusLaneLayout.DefaultMaxIconsPerRow;
    public float BuffIconSize { get; set; } = 40f;
    public float BuffIconGap { get; set; } = 8f;
    public float DebuffOffsetX { get; set; }
    public float DebuffOffsetY { get; set; }
    public StatusLaneGrowDirection DebuffGrowDirection { get; set; } = StatusLaneGrowDirection.LeftToRightUp;
    public StatusTimerPlacement DebuffTimerPlacement { get; set; } = StatusTimerPlacement.Bottom;
    public int DebuffMaxIconsPerRow { get; set; } = StatusLaneLayout.DefaultMaxIconsPerRow;
    public float DebuffIconSize { get; set; } = 40f;
    public float DebuffIconGap { get; set; } = 8f;
    public float LimitBreakOffsetX { get; set; } = -150f;
    public float LimitBreakYOffset { get; set; } = 172f;
    public bool ShowSlidecastMarker { get; set; } = true;
    public float SlidecastOffsetSeconds { get; set; } = 0.50f;
    public uint ColorHpFill { get; set; } = 0xFF4AB34A;
    public uint ColorHpBack { get; set; } = 0x40202020;
    public uint ColorMpFill { get; set; } = 0xFFA755E5;
    public uint ColorMpBack { get; set; } = 0x552A1424;
    public uint ColorAccent { get; set; } = 0xFF37AFD4;
    public uint ColorGaugeBack { get; set; } = 0x90202020;
    public uint ColorTextPrimary { get; set; } = 0xFFFFFFFF;
    public uint ColorTextSecondary { get; set; } = 0xE0E0E0E0;
    public uint ColorBuffTint { get; set; } = 0xFFFFFFFF;
    public uint ColorDebuffTint { get; set; } = 0xFFF2A0A0;
    public List<uint> LeftHotbarActions { get; set; } = new();
    public List<uint> RightHotbarActions { get; set; } = new();
    public List<uint> LeftHotbar2Actions { get; set; } = new();
    public List<uint> RightHotbar2Actions { get; set; } = new();

    public static HudLayoutPresetSnapshot CaptureFrom(HudConfiguration config)
    {
        return new HudLayoutPresetSnapshot
        {
            UnlockLayout = config.UnlockLayout,
            EnableStatusTooltips = config.EnableStatusTooltips,
            GlobalScale = config.GlobalScale,
            GlobalOpacity = config.GlobalOpacity,
            CenterAnchor = config.CenterAnchor,
            LayoutUsesScreenCenterOrigin = config.LayoutUsesScreenCenterOrigin,
            LayoutUsesUnscaledPixelOffsets = config.LayoutUsesUnscaledPixelOffsets,
            HudOffsetX = config.HudOffsetX,
            HudOffsetY = config.HudOffsetY,
            OrbRadius = config.OrbRadius,
            OrbOffsetX = config.OrbOffsetX,
            OrbOffsetY = config.OrbOffsetY,
            OrbThickness = config.OrbThickness,
            MpRingThicknessScale = config.MpRingThicknessScale,
            Hotbar1SlotSize = config.Hotbar1SlotSize,
            Hotbar1SlotGap = config.Hotbar1SlotGap,
            Hotbar2SlotSize = config.Hotbar2SlotSize,
            Hotbar2SlotGap = config.Hotbar2SlotGap,
            HotbarSlotSize = config.HotbarSlotSize,
            HotbarSlotGap = config.HotbarSlotGap,
            Hotbar1Enabled = config.Hotbar1Enabled,
            Hotbar2Enabled = config.Hotbar2Enabled,
            HotbarVerticalOffset = config.HotbarVerticalOffset,
            Hotbar1OffsetX = config.Hotbar1OffsetX,
            Hotbar1OffsetY = config.Hotbar1OffsetY,
            Hotbar2OffsetX = config.Hotbar2OffsetX,
            Hotbar2OffsetY = config.Hotbar2OffsetY,
            Hotbar1VisibleSlotCount = config.Hotbar1VisibleSlotCount,
            Hotbar2VisibleSlotCount = config.Hotbar2VisibleSlotCount,
            Hotbar1SlotsPerRow = config.Hotbar1SlotsPerRow,
            Hotbar2SlotsPerRow = config.Hotbar2SlotsPerRow,
            BuffRowOffsetX = config.BuffRowOffsetX,
            BuffRowYOffset = config.BuffRowYOffset,
            UseSplitStatusLayout = true,
            UsesFourWayGrowDirection = true,
            BuffOffsetX = config.BuffOffsetX,
            BuffOffsetY = config.BuffOffsetY,
            BuffGrowDirection = config.BuffGrowDirection,
            BuffTimerPlacement = config.BuffTimerPlacement,
            BuffMaxIconsPerRow = config.BuffMaxIconsPerRow,
            BuffIconSize = config.BuffIconSize,
            BuffIconGap = config.BuffIconGap,
            DebuffOffsetX = config.DebuffOffsetX,
            DebuffOffsetY = config.DebuffOffsetY,
            DebuffGrowDirection = config.DebuffGrowDirection,
            DebuffTimerPlacement = config.DebuffTimerPlacement,
            DebuffMaxIconsPerRow = config.DebuffMaxIconsPerRow,
            DebuffIconSize = config.DebuffIconSize,
            DebuffIconGap = config.DebuffIconGap,
            LimitBreakOffsetX = config.LimitBreakOffsetX,
            LimitBreakYOffset = config.LimitBreakYOffset,
            ShowSlidecastMarker = config.ShowSlidecastMarker,
            SlidecastOffsetSeconds = config.SlidecastOffsetSeconds,
            ColorHpFill = config.ColorHpFill,
            ColorHpBack = config.ColorHpBack,
            ColorMpFill = config.ColorMpFill,
            ColorMpBack = config.ColorMpBack,
            ColorAccent = config.ColorAccent,
            ColorGaugeBack = config.ColorGaugeBack,
            ColorTextPrimary = config.ColorTextPrimary,
            ColorTextSecondary = config.ColorTextSecondary,
            ColorBuffTint = config.ColorBuffTint,
            ColorDebuffTint = config.ColorDebuffTint,
            LeftHotbarActions = new List<uint>(config.LeftHotbarActions),
            RightHotbarActions = new List<uint>(config.RightHotbarActions),
            LeftHotbar2Actions = new List<uint>(config.LeftHotbar2Actions),
            RightHotbar2Actions = new List<uint>(config.RightHotbar2Actions),
        };
    }

    /// <summary>
    /// Fixed Default layout: single-row hotbars (6 slots each) flanking orb; buffs/debuffs above (screen-center offsets).
    /// </summary>
    private static readonly Vector2 PresetConversionViewport = new(2560f, 1440f);

    public static HudLayoutPresetSnapshot CreatePluginDefaultLayout()
    {
        return new HudLayoutPresetSnapshot
        {
            UnlockLayout = false,
            EnableStatusTooltips = true,
            GlobalScale = 2.2f,
            GlobalOpacity = 1.0f,
            CenterAnchor = new Vector2(0.5f, 0.5f),
            LayoutUsesScreenCenterOrigin = true,
            LayoutUsesUnscaledPixelOffsets = true,
            HudOffsetX = 0f,
            HudOffsetY = 0f,
            OrbRadius = 56f,
            OrbOffsetX = 0f,
            OrbOffsetY = 455f,
            OrbThickness = 10f,
            MpRingThicknessScale = 1.20f,
            Hotbar1SlotSize = 44f,
            Hotbar1SlotGap = 8f,
            Hotbar2SlotSize = 44f,
            Hotbar2SlotGap = 8f,
            Hotbar1Enabled = true,
            Hotbar2Enabled = true,
            HotbarVerticalOffset = 0f,
            Hotbar1OffsetX = -176f,
            Hotbar1OffsetY = 458.8f,
            Hotbar2OffsetX = 864.6f,
            Hotbar2OffsetY = 458.8f,
            Hotbar1VisibleSlotCount = 6,
            Hotbar2VisibleSlotCount = 6,
            Hotbar1SlotsPerRow = HotbarGridLayout.DefaultSlotsPerRow,
            Hotbar2SlotsPerRow = HotbarGridLayout.DefaultSlotsPerRow,
            UseSplitStatusLayout = true,
            UsesFourWayGrowDirection = true,
            BuffOffsetX = -539.4f,
            BuffOffsetY = 233.7f,
            BuffGrowDirection = StatusLaneGrowDirection.RightToLeftUp,
            BuffTimerPlacement = StatusTimerPlacement.Bottom,
            BuffMaxIconsPerRow = StatusLaneLayout.DefaultMaxIconsPerRow,
            BuffIconSize = 78.3f,
            BuffIconGap = 8f,
            DebuffOffsetX = 539.4f,
            DebuffOffsetY = 233.7f,
            DebuffGrowDirection = StatusLaneGrowDirection.LeftToRightUp,
            DebuffTimerPlacement = StatusTimerPlacement.Bottom,
            DebuffMaxIconsPerRow = StatusLaneLayout.DefaultMaxIconsPerRow,
            DebuffIconSize = 78.3f,
            DebuffIconGap = 8f,
            LimitBreakOffsetX = -330f,
            LimitBreakYOffset = 831.5f,
            ShowSlidecastMarker = true,
            SlidecastOffsetSeconds = 0.50f,
            ColorHpFill = 0xFF4AB34A,
            ColorHpBack = 0x40202020,
            ColorMpFill = 0xFFA755E5,
            ColorMpBack = 0x552A1424,
            ColorAccent = 0xFF37AFD4,
            ColorGaugeBack = 0x90202020,
            ColorTextPrimary = 0xFFFFFFFF,
            ColorTextSecondary = 0xE0E0E0E0,
            ColorBuffTint = 0xFFFFFFFF,
            ColorDebuffTint = 0xFFF2A0A0,
            LeftHotbarActions = new List<uint> { 9, 15, 16, 17, 18, 19 },
            RightHotbarActions = new List<uint> { 20, 21, 22, 23, 24, 25 },
            LeftHotbar2Actions = new(),
            RightHotbar2Actions = new(),
        };
    }

    /// <summary>
    /// Fixed Expanded layout: dual 6x2 hotbars flanking a lowered orb; buffs/debuffs above (screen-center offsets).
    /// </summary>
    public static HudLayoutPresetSnapshot CreatePluginExpandedLayout()
    {
        return new HudLayoutPresetSnapshot
        {
            UnlockLayout = false,
            EnableStatusTooltips = true,
            GlobalScale = 2.2f,
            GlobalOpacity = 1.0f,
            CenterAnchor = new Vector2(0.5f, 0.5f),
            LayoutUsesScreenCenterOrigin = true,
            LayoutUsesUnscaledPixelOffsets = true,
            HudOffsetX = 0f,
            HudOffsetY = 0f,
            OrbRadius = 56f,
            OrbOffsetX = 0f,
            OrbOffsetY = 455f,
            OrbThickness = 10f,
            MpRingThicknessScale = 1.20f,
            Hotbar1SlotSize = 44f,
            Hotbar1SlotGap = 8f,
            Hotbar2SlotSize = 44f,
            Hotbar2SlotGap = 8f,
            Hotbar1Enabled = true,
            Hotbar2Enabled = true,
            HotbarVerticalOffset = 0f,
            Hotbar1OffsetX = -521.4f,
            Hotbar1OffsetY = 490.5f,
            Hotbar2OffsetX = 521.4f,
            Hotbar2OffsetY = 490.5f,
            Hotbar1VisibleSlotCount = HotbarSlotVisibility.DefaultTotalSlots,
            Hotbar2VisibleSlotCount = HotbarSlotVisibility.DefaultTotalSlots,
            Hotbar1SlotsPerRow = 6,
            Hotbar2SlotsPerRow = 6,
            UseSplitStatusLayout = true,
            UsesFourWayGrowDirection = true,
            BuffOffsetX = -522f,
            BuffOffsetY = 200f,
            BuffGrowDirection = StatusLaneGrowDirection.RightToLeftUp,
            BuffTimerPlacement = StatusTimerPlacement.Bottom,
            BuffMaxIconsPerRow = StatusLaneLayout.DefaultMaxIconsPerRow,
            BuffIconSize = 77f,
            BuffIconGap = 8f,
            DebuffOffsetX = 522f,
            DebuffOffsetY = 200f,
            DebuffGrowDirection = StatusLaneGrowDirection.LeftToRightUp,
            DebuffTimerPlacement = StatusTimerPlacement.Bottom,
            DebuffMaxIconsPerRow = StatusLaneLayout.DefaultMaxIconsPerRow,
            DebuffIconSize = 77f,
            DebuffIconGap = 8f,
            LimitBreakOffsetX = -330f,
            LimitBreakYOffset = 927.5f,
            ShowSlidecastMarker = true,
            SlidecastOffsetSeconds = 0.50f,
            ColorHpFill = 0xFF4AB34A,
            ColorHpBack = 0x40202020,
            ColorMpFill = 0xFFA755E5,
            ColorMpBack = 0x552A1424,
            ColorAccent = 0xFF37AFD4,
            ColorGaugeBack = 0x90202020,
            ColorTextPrimary = 0xFFFFFFFF,
            ColorTextSecondary = 0xE0E0E0E0,
            ColorBuffTint = 0xFFFFFFFF,
            ColorDebuffTint = 0xFFF2A0A0,
            LeftHotbarActions = new List<uint> { 9, 15, 16, 17, 18, 19 },
            RightHotbarActions = new List<uint> { 20, 21, 22, 23, 24, 25 },
            LeftHotbar2Actions = new(),
            RightHotbar2Actions = new(),
        };
    }

    /// <summary>
    /// Fixed ARPG layout: orb left, 6-slot hotbar, compact buff/debuff rows (screen-center offsets).
    /// </summary>
    public static HudLayoutPresetSnapshot CreatePluginArpgLayout()
    {
        return new HudLayoutPresetSnapshot
        {
            UnlockLayout = false,
            EnableStatusTooltips = true,
            GlobalScale = 2.2f,
            GlobalOpacity = 1.0f,
            CenterAnchor = new Vector2(0.5f, 0.5f),
            LayoutUsesScreenCenterOrigin = true,
            LayoutUsesUnscaledPixelOffsets = true,
            HudOffsetX = 0f,
            HudOffsetY = 182f,
            OrbRadius = 56f,
            OrbOffsetX = -534f,
            OrbOffsetY = 455f,
            OrbThickness = 10f,
            MpRingThicknessScale = 1.20f,
            Hotbar1SlotSize = 44f,
            Hotbar1SlotGap = 8f,
            Hotbar2SlotSize = 44f,
            Hotbar2SlotGap = 8f,
            Hotbar1Enabled = true,
            Hotbar2Enabled = false,
            HotbarVerticalOffset = 0f,
            Hotbar1OffsetX = 0f,
            Hotbar1OffsetY = 460f,
            Hotbar2OffsetX = 0f,
            Hotbar2OffsetY = 403f,
            Hotbar1VisibleSlotCount = 6,
            Hotbar2VisibleSlotCount = HotbarSlotVisibility.DefaultTotalSlots,
            Hotbar1SlotsPerRow = 6,
            Hotbar2SlotsPerRow = HotbarGridLayout.DefaultSlotsPerRow,
            UseSplitStatusLayout = true,
            UsesFourWayGrowDirection = true,
            BuffOffsetX = -139.5f,
            BuffOffsetY = 222f,
            BuffGrowDirection = StatusLaneGrowDirection.LeftToRightUp,
            BuffTimerPlacement = StatusTimerPlacement.Bottom,
            BuffMaxIconsPerRow = 6,
            BuffIconSize = 78f,
            BuffIconGap = 8f,
            DebuffOffsetX = 208f,
            DebuffOffsetY = 222f,
            DebuffGrowDirection = StatusLaneGrowDirection.RightToLeftUp,
            DebuffTimerPlacement = StatusTimerPlacement.Bottom,
            DebuffMaxIconsPerRow = 4,
            DebuffIconSize = 78f,
            DebuffIconGap = 8f,
            LimitBreakOffsetX = -787.5f,
            LimitBreakYOffset = 761.4f,
            ShowSlidecastMarker = true,
            SlidecastOffsetSeconds = 0.50f,
            ColorHpFill = 0xFF4AB34A,
            ColorHpBack = 0x40202020,
            ColorMpFill = 0xFFA755E5,
            ColorMpBack = 0x552A1424,
            ColorAccent = 0xFF37AFD4,
            ColorGaugeBack = 0x90202020,
            ColorTextPrimary = 0xFFFFFFFF,
            ColorTextSecondary = 0xE0E0E0E0,
            ColorBuffTint = 0xFFFFFFFF,
            ColorDebuffTint = 0xFFF2A0A0,
            LeftHotbarActions = new List<uint> { 9, 15, 16, 17, 18, 19 },
            RightHotbarActions = new List<uint> { 20, 21, 22, 23, 24, 25 },
            LeftHotbar2Actions = new(),
            RightHotbar2Actions = new(),
        };
    }

    public static HudLayoutPresetSnapshot CreateFactoryDefault()
    {
        return CreatePluginDefaultLayout();
    }

    public void ApplyTo(HudConfiguration config)
    {
        if (!this.LayoutUsesScreenCenterOrigin || !this.LayoutUsesUnscaledPixelOffsets)
        {
            HudLayoutMigration.ConvertPresetSnapshotOffsets(this, PresetConversionViewport);
        }

        config.UnlockLayout = this.UnlockLayout;
        config.EnableStatusTooltips = this.EnableStatusTooltips;
        config.GlobalScale = this.GlobalScale;
        config.GlobalOpacity = this.GlobalOpacity;
        config.CenterAnchor = this.CenterAnchor;
        config.HudOffsetX = this.HudOffsetX;
        config.HudOffsetY = this.HudOffsetY;
        config.OrbRadius = this.OrbRadius;
        config.OrbOffsetX = this.OrbOffsetX;
        config.OrbOffsetY = this.OrbOffsetY;
        config.OrbThickness = this.OrbThickness;
        config.MpRingThicknessScale = this.MpRingThicknessScale;
        config.Hotbar1SlotSize = ResolveSlotSize(this.Hotbar1SlotSize, this.HotbarSlotSize);
        config.Hotbar1SlotGap = ResolveSlotGap(this.Hotbar1SlotGap, this.HotbarSlotGap);
        config.Hotbar2SlotSize = ResolveSlotSize(this.Hotbar2SlotSize, this.HotbarSlotSize);
        config.Hotbar2SlotGap = ResolveSlotGap(this.Hotbar2SlotGap, this.HotbarSlotGap);
        config.HotbarSlotSize = this.HotbarSlotSize;
        config.HotbarSlotGap = this.HotbarSlotGap;
        config.Hotbar1Enabled = this.Hotbar1Enabled;
        config.Hotbar2Enabled = this.Hotbar2Enabled;
        config.HotbarVerticalOffset = this.HotbarVerticalOffset;
        config.Hotbar1OffsetX = this.Hotbar1OffsetX;
        config.Hotbar1OffsetY = this.Hotbar1OffsetY;
        config.Hotbar2OffsetX = this.Hotbar2OffsetX;
        config.Hotbar2OffsetY = this.Hotbar2OffsetY;
        config.Hotbar1VisibleSlotCount = HotbarSlotVisibility.ClampTotal(this.Hotbar1VisibleSlotCount);
        config.Hotbar2VisibleSlotCount = HotbarSlotVisibility.ClampTotal(this.Hotbar2VisibleSlotCount);
        config.Hotbar1SlotsPerRow = this.Hotbar1SlotsPerRow <= 0
            ? HotbarGridLayout.DefaultSlotsPerRow
            : HotbarGridLayout.ClampSlotsPerRow(this.Hotbar1SlotsPerRow);
        config.Hotbar2SlotsPerRow = this.Hotbar2SlotsPerRow <= 0
            ? HotbarGridLayout.DefaultSlotsPerRow
            : HotbarGridLayout.ClampSlotsPerRow(this.Hotbar2SlotsPerRow);
        this.ApplyStatusLayoutTo(config);
        config.LimitBreakOffsetX = this.LimitBreakOffsetX;
        config.LimitBreakYOffset = this.LimitBreakYOffset;
        config.LayoutUsesScreenCenterOrigin = true;
        config.LayoutUsesUnscaledPixelOffsets = this.LayoutUsesUnscaledPixelOffsets;
        config.CenterAnchor = new Vector2(0.5f, 0.5f);
        config.ShowSlidecastMarker = this.ShowSlidecastMarker;
        config.SlidecastOffsetSeconds = this.SlidecastOffsetSeconds;
        config.ColorHpFill = this.ColorHpFill;
        config.ColorHpBack = this.ColorHpBack;
        config.ColorMpFill = this.ColorMpFill;
        config.ColorMpBack = this.ColorMpBack;
        config.ColorAccent = this.ColorAccent;
        config.ColorGaugeBack = this.ColorGaugeBack;
        config.ColorTextPrimary = this.ColorTextPrimary;
        config.ColorTextSecondary = this.ColorTextSecondary;
        config.ColorBuffTint = this.ColorBuffTint;
        config.ColorDebuffTint = this.ColorDebuffTint;
        config.LeftHotbarActions = new List<uint>(this.LeftHotbarActions);
        config.RightHotbarActions = new List<uint>(this.RightHotbarActions);
        config.LeftHotbar2Actions = new List<uint>(this.LeftHotbar2Actions);
        config.RightHotbar2Actions = new List<uint>(this.RightHotbar2Actions);
    }

    private StatusLaneGrowDirection ResolveGrowDirection(StatusLaneGrowDirection stored)
    {
        if (this.UsesFourWayGrowDirection)
        {
            return stored;
        }

        return StatusLaneLayout.MigrateLegacyGrowDirection((int)stored);
    }

    private void ApplyStatusLayoutTo(HudConfiguration config)
    {
        if (this.UseSplitStatusLayout)
        {
            config.BuffOffsetX = this.BuffOffsetX;
            config.BuffOffsetY = this.BuffOffsetY;
            config.BuffGrowDirection = this.ResolveGrowDirection(this.BuffGrowDirection);
            config.BuffTimerPlacement = this.BuffTimerPlacement;
            config.BuffMaxIconsPerRow = StatusLaneLayout.ClampMaxIconsPerRow(this.BuffMaxIconsPerRow);
            config.BuffIconSize = this.BuffIconSize;
            config.BuffIconGap = this.BuffIconGap;
            config.DebuffOffsetX = this.DebuffOffsetX;
            config.DebuffOffsetY = this.DebuffOffsetY;
            config.DebuffGrowDirection = this.ResolveGrowDirection(this.DebuffGrowDirection);
            config.DebuffTimerPlacement = this.DebuffTimerPlacement;
            config.DebuffMaxIconsPerRow = StatusLaneLayout.ClampMaxIconsPerRow(this.DebuffMaxIconsPerRow);
            config.DebuffIconSize = this.DebuffIconSize;
            config.DebuffIconGap = this.DebuffIconGap;
            config.BuffRowOffsetX = this.BuffOffsetX;
            config.BuffRowYOffset = this.BuffOffsetY;
            return;
        }

        // Legacy combined buff/debuff offsets from layouts saved before split settings existed.
        config.BuffOffsetX = this.BuffRowOffsetX;
        config.DebuffOffsetX = this.BuffRowOffsetX;
        config.BuffOffsetY = this.BuffRowYOffset;
        config.DebuffOffsetY = this.BuffRowYOffset;
        config.BuffGrowDirection = StatusLaneGrowDirection.RightToLeftUp;
        config.BuffTimerPlacement = StatusTimerPlacement.Bottom;
        config.DebuffGrowDirection = StatusLaneGrowDirection.LeftToRightUp;
        config.DebuffTimerPlacement = StatusTimerPlacement.Bottom;
        config.BuffMaxIconsPerRow = StatusLaneLayout.DefaultMaxIconsPerRow;
        config.BuffIconSize = this.BuffIconSize;
        config.BuffIconGap = this.BuffIconGap;
        config.DebuffMaxIconsPerRow = StatusLaneLayout.DefaultMaxIconsPerRow;
        config.DebuffIconSize = this.BuffIconSize;
        config.DebuffIconGap = this.BuffIconGap;
        config.BuffRowOffsetX = this.BuffRowOffsetX;
        config.BuffRowYOffset = this.BuffRowYOffset;
    }

    private static float ResolveSlotSize(float perHotbarValue, float legacyValue) =>
        perHotbarValue > 0f ? perHotbarValue : legacyValue;

    private static float ResolveSlotGap(float perHotbarValue, float legacyValue) =>
        perHotbarValue >= 0f ? perHotbarValue : legacyValue;
}

[Serializable]
public sealed class NamedHudLayoutPreset
{
    public string Name { get; set; } = string.Empty;
    public HudLayoutPresetSnapshot Layout { get; set; } = new();
}
