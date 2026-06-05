using Dalamud.Configuration;
using Dalamud.Plugin;
using System.Numerics;

namespace FFXIVHudPlugin;

[Serializable]
public sealed class HudConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 57;

    public bool Enabled { get; set; } = true;
    public bool UnlockLayout { get; set; } = false;
    public bool EnableStatusTooltips { get; set; } = true;
    public bool ShowTestStatusEffects { get; set; }
    public float GlobalScale { get; set; } = 2.25f;
    public float GlobalOpacity { get; set; } = 1.0f;
    public HudPreset Preset { get; set; } = HudPreset.Default;

    public Vector2 CenterAnchor { get; set; } = new(0.5f, 0.5f);
    public bool LayoutUsesScreenCenterOrigin { get; set; } = true;
    public bool LayoutUsesUnscaledPixelOffsets { get; set; } = true;
    public float HudOffsetX { get; set; } = 0f;
    public float HudOffsetY { get; set; } = 0f;
    public float OrbRadius { get; set; } = 56f;
    public float OrbOffsetX { get; set; } = 0f;
    public float OrbOffsetY { get; set; } = 0f;
    public float OrbThickness { get; set; } = 10f;
    public float MpRingThicknessScale { get; set; } = 1.20f;
    public float Hotbar1SlotSize { get; set; } = HotbarLayout.DefaultSlotSize;
    public float Hotbar1SlotGap { get; set; } = HotbarLayout.DefaultSlotGap;
    public float Hotbar2SlotSize { get; set; } = HotbarLayout.DefaultSlotSize;
    public float Hotbar2SlotGap { get; set; } = HotbarLayout.DefaultSlotGap;
    // Legacy serialized fields; migrated to per-hotbar settings in v41.
    public float HotbarSlotSize { get; set; } = HotbarLayout.DefaultSlotSize;
    public float HotbarSlotGap { get; set; } = HotbarLayout.DefaultSlotGap;
    public bool Hotbar1Enabled { get; set; } = true;
    public bool Hotbar2Enabled { get; set; } = false;
    public float HotbarVerticalOffset { get; set; } = -14f;
    public float Hotbar1OffsetX { get; set; } = 0f;
    public float Hotbar1OffsetY { get; set; } = -5f;
    public float Hotbar2OffsetX { get; set; } = 0f;
    public float Hotbar2OffsetY { get; set; } = 0f;
    public int Hotbar1VisibleSlotCount { get; set; } = HotbarSlotVisibility.DefaultTotalSlots;
    public int Hotbar2VisibleSlotCount { get; set; } = HotbarSlotVisibility.DefaultTotalSlots;
    public int Hotbar1SlotsPerRow { get; set; } = HotbarGridLayout.DefaultSlotsPerRow;
    public int Hotbar2SlotsPerRow { get; set; } = HotbarGridLayout.DefaultSlotsPerRow;
    public float BuffRowOffsetX { get; set; } = 0f;
    public float BuffRowYOffset { get; set; } = 8.6f;
    public float BuffOffsetX { get; set; } = 0f;
    public float BuffOffsetY { get; set; } = 8.6f;
    public StatusLaneGrowDirection BuffGrowDirection { get; set; } = StatusLaneGrowDirection.RightToLeftUp;
    public StatusTimerPlacement BuffTimerPlacement { get; set; } = StatusTimerPlacement.Bottom;
    public int BuffMaxIconsPerRow { get; set; } = StatusLaneLayout.DefaultMaxIconsPerRow;
    public float BuffIconSize { get; set; } = 78.3f;
    public float BuffIconGap { get; set; } = 8f;
    public float DebuffOffsetX { get; set; } = 0f;
    public float DebuffOffsetY { get; set; } = 8.6f;
    public StatusLaneGrowDirection DebuffGrowDirection { get; set; } = StatusLaneGrowDirection.LeftToRightUp;
    public StatusTimerPlacement DebuffTimerPlacement { get; set; } = StatusTimerPlacement.Bottom;
    public int DebuffMaxIconsPerRow { get; set; } = StatusLaneLayout.DefaultMaxIconsPerRow;
    public float DebuffIconSize { get; set; } = 78.3f;
    public float DebuffIconGap { get; set; } = 8f;
    public float LimitBreakOffsetX { get; set; } = -150f;
    public float LimitBreakYOffset { get; set; } = 172f;
    public bool ShowSlidecastMarker { get; set; } = true;
    public float SlidecastOffsetSeconds { get; set; } = 0.50f;

    public bool MinimapEnabled { get; set; }
    public bool MinimapSquare { get; set; }
    public bool MinimapNorthLocked { get; set; }
    public float MinimapSize { get; set; } = MinimapLayout.DefaultSize;
    public float MinimapOffsetX { get; set; } = MinimapLayout.DefaultOffsetX;
    public float MinimapOffsetY { get; set; } = MinimapLayout.DefaultOffsetY;
    public float MinimapVisibleRangeYalms { get; set; } = MinimapLayout.DefaultVisibleRangeYalms;
    public float MinimapFacingConeSizeScale { get; set; } = MinimapLayout.DefaultFacingConeSizeScale;
    public float MinimapFacingConeOpacity { get; set; } = MinimapLayout.DefaultFacingConeOpacity;
    public float MinimapBorderThickness { get; set; } = MinimapLayout.DefaultBorderThickness;
    public uint MinimapBorderColor { get; set; } = MinimapLayout.DefaultBorderColor;
    public bool MinimapShowNativeMarkers { get; set; } = true;
    public bool MinimapShowCardinalDirections { get; set; }
    public bool MinimapShowDiagnostics { get; set; }
    public float MinimapMarkerIconSize { get; set; } = MinimapLayout.DefaultMarkerIconSize;
    public float MinimapPlayerPinSize { get; set; } = MinimapLayout.DefaultPlayerPinSize;
    public bool MinimapUseRolePinColor { get; set; } = true;
    public uint MinimapPlayerPinColor { get; set; } = MinimapLayout.DefaultPlayerPinColor;

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

    public List<uint> LeftHotbarActions { get; set; } = new() { 9, 15, 16, 17, 18, 19 };
    public List<uint> RightHotbarActions { get; set; } = new() { 20, 21, 22, 23, 24, 25 };
    public List<uint> LeftHotbar2Actions { get; set; } = new();
    public List<uint> RightHotbar2Actions { get; set; } = new();
    public List<SquadronCapturedCommandConfig> CapturedSquadronCommands { get; set; } = new();
    public HudLayoutPresetSnapshot? DefaultPresetSnapshot { get; set; }
    public HudLayoutPresetSnapshot? ExpandedPresetSnapshot { get; set; }
    public HudLayoutPresetSnapshot? ArpgPresetSnapshot { get; set; }
    public List<NamedHudLayoutPreset> CustomLayouts { get; set; } = new();
    public string SelectedCustomLayoutName { get; set; } = string.Empty;
    public ActionCameraConfiguration ActionCamera { get; set; } = new();

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        var migrationChanged = this.ApplyMigrations();
        var normalizationChanged = this.NormalizeAfterLoad();
        if (migrationChanged || normalizationChanged)
        {
            this.Save();
        }
    }

    private bool ApplyMigrations()
    {
        var didChange = false;
        if (this.Version < 2)
        {
            // Force a visibly larger baseline to ensure existing users immediately see the scale change.
            this.GlobalScale = 3.0f;
            this.CenterAnchor = new Vector2(0.5f, 0.72f);
            this.BuffRowYOffset = -138f;
            this.LimitBreakYOffset = 230f;
            this.Version = 2;
            didChange = true;
        }

        if (this.Version < 3)
        {
            // Reduce the previously forced oversized layout by 50%.
            this.GlobalScale = float.Clamp(this.GlobalScale * 0.5f, 0.5f, 4.0f);
            this.BuffRowYOffset *= 0.5f;
            this.LimitBreakYOffset *= 0.5f;
            this.Version = 3;
            didChange = true;
        }

        if (this.Version < 4)
        {
            // Buff/debuff rows are now anchored above hotbars; use 0 as neutral offset.
            this.BuffRowYOffset = 0f;
            this.Version = 4;
            didChange = true;
        }

        if (this.Version < 5)
        {
            // Increase status icon readability to better match the native HUD scale.
            this.BuffIconSize = Math.Max(this.BuffIconSize, 40f);
            this.BuffIconGap = Math.Max(this.BuffIconGap, 8f);
            this.Version = 5;
            didChange = true;
        }

        if (this.Version < 6)
        {
            this.Version = 6;
            didChange = true;
        }

        if (this.Version < 7)
        {
            // New global HUD pixel offsets default to centered.
            this.HudOffsetX = 0f;
            this.HudOffsetY = 0f;
            this.Version = 7;
            didChange = true;
        }

        if (this.Version < 8)
        {
            // Match updated HP/MP orb palette with default-game-inspired green and magenta tones.
            this.ColorHpFill = 0xFF3FCF46;
            this.ColorMpFill = 0xFFE45DB2;
            this.ColorMpBack = 0x552A1424;
            this.Version = 8;
            didChange = true;
        }

        if (this.Version < 9)
        {
            // Correct accent to true gold in ABGR packing (not blue-tinted).
            this.ColorAccent = 0xFF37AFD4;
            this.Version = 9;
            didChange = true;
        }

        if (this.Version < 10)
        {
            // Align HP/MP colors to the default player bar palette references.
            this.ColorHpFill = 0xFF4AB34A;
            this.ColorMpFill = 0xFFA755E5;
            this.Version = 10;
            didChange = true;
        }

        if (this.Version < 11)
        {
            // Enable slidecast marker defaults for the top cast arc.
            this.ShowSlidecastMarker = true;
            this.SlidecastOffsetSeconds = Math.Clamp(this.SlidecastOffsetSeconds, 0.05f, 1.20f);
            this.Version = 11;
            didChange = true;
        }

        if (this.Version < 12)
        {
            this.Version = 12;
            didChange = true;
        }

        if (this.Version < 14)
        {
            // Drag/drop assignment replaced with picker-based slot assignment.
            this.Version = 14;
            didChange = true;
        }

        if (this.Version < 17)
        {
            // Removed plugin-managed keybind overrides and formatting controls.
            this.Version = 17;
            didChange = true;
        }

        if (this.Version < 18)
        {
            // Persist discovered mission-only squadron order commands across reloads.
            this.CapturedSquadronCommands ??= new();
            this.Version = 18;
            didChange = true;
        }

        if (this.Version < 19)
        {
            // Click-through, native UI replacement, and debug overlay are always on; presets consolidated to Default.
            this.Preset = HudPreset.Default;
            this.Version = 19;
            didChange = true;
        }

        if (this.Version < 20)
        {
            this.LeftHotbar2Actions ??= new();
            this.RightHotbar2Actions ??= new();
            this.Version = 20;
            didChange = true;
        }

        if (this.Version < 21)
        {
            this.Hotbar1OffsetX = 0f;
            this.Hotbar1OffsetY = 0f;
            this.Hotbar2OffsetX = 0f;
            this.Hotbar2OffsetY = 0f;
            this.Version = 21;
            didChange = true;
        }

        if (this.Version < 22)
        {
            this.Hotbar1Enabled = true;
            this.Hotbar2Enabled = true;
            this.Version = 22;
            didChange = true;
        }

        if (this.Version < 23)
        {
            this.Version = 23;
            didChange = true;
        }

        if (this.Version < 24)
        {
            // Preserve the user's current tuned layout as the Expanded preset (not code defaults).
            this.ExpandedPresetSnapshot ??= HudLayoutPresetSnapshot.CaptureFrom(this);
            this.DefaultPresetSnapshot ??= HudLayoutPresetSnapshot.CreateFactoryDefault();
            this.Version = 24;
            didChange = true;
        }

        if (this.Version < 25)
        {
            // Default preset is now a fixed tuned layout; refresh stored snapshot to match.
            this.DefaultPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginDefaultLayout();
            this.Version = 25;
            didChange = true;
        }

        if (this.Version < 26)
        {
            this.CustomLayouts ??= new();
            this.ExpandedPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginExpandedLayout();
            this.Version = 26;
            didChange = true;
        }

        if (this.Version < 27)
        {
            this.ExpandedPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginExpandedLayout();
            this.Version = 27;
            didChange = true;
        }

        if (this.Version < 28)
        {
            if (this.Hotbar1VisibleSlotCount < 1)
            {
                this.Hotbar1VisibleSlotCount = HotbarSlotVisibility.DefaultTotalSlots;
            }
            else
            {
                this.Hotbar1VisibleSlotCount = HotbarSlotVisibility.ClampTotal(this.Hotbar1VisibleSlotCount);
            }

            if (this.Hotbar2VisibleSlotCount < 1)
            {
                this.Hotbar2VisibleSlotCount = HotbarSlotVisibility.DefaultTotalSlots;
            }
            else
            {
                this.Hotbar2VisibleSlotCount = HotbarSlotVisibility.ClampTotal(this.Hotbar2VisibleSlotCount);
            }

            this.Version = 28;
            didChange = true;
        }

        if (this.Version < 29)
        {
            this.ExpandedPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginExpandedLayout();
            this.Version = 29;
            didChange = true;
        }

        if (this.Version < 30)
        {
            this.UnlockLayout = false;
            this.Version = 30;
            didChange = true;
        }

        if (this.Version < 31)
        {
            this.Version = 31;
            didChange = true;
        }

        if (this.Version < 32)
        {
            this.Version = 32;
            didChange = true;
        }

        if (this.Version < 33)
        {
            this.BuffOffsetX = this.BuffRowOffsetX;
            this.DebuffOffsetX = this.BuffRowOffsetX;
            this.BuffOffsetY = this.BuffRowYOffset;
            this.DebuffOffsetY = this.BuffRowYOffset;
            this.DebuffIconSize = this.BuffIconSize;
            this.DebuffIconGap = this.BuffIconGap;
            this.BuffGrowDirection = StatusLaneGrowDirection.RightToLeftUp;
            this.DebuffGrowDirection = StatusLaneGrowDirection.LeftToRightUp;
            this.DefaultPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginDefaultLayout();
            this.ExpandedPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginExpandedLayout();
            this.Version = 33;
            didChange = true;
        }

        if (this.Version < 34)
        {
            this.BuffTimerPlacement = StatusTimerPlacement.Bottom;
            this.DebuffTimerPlacement = StatusTimerPlacement.Bottom;
            this.Version = 34;
            didChange = true;
        }

        if (this.Version < 35)
        {
            this.ArpgPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginArpgLayout();
            this.Version = 35;
            didChange = true;
        }

        if (this.Version < 36)
        {
            this.BuffMaxIconsPerRow = StatusLaneLayout.ClampMaxIconsPerRow(this.BuffMaxIconsPerRow);
            this.DebuffMaxIconsPerRow = StatusLaneLayout.ClampMaxIconsPerRow(this.DebuffMaxIconsPerRow);
            this.Version = 36;
            didChange = true;
        }

        if (this.Version < 37)
        {
            this.BuffGrowDirection = StatusLaneLayout.MigrateLegacyGrowDirection((int)this.BuffGrowDirection);
            this.DebuffGrowDirection = StatusLaneLayout.MigrateLegacyGrowDirection((int)this.DebuffGrowDirection);
            this.Version = 37;
            didChange = true;
        }

        if (this.Version < 38)
        {
            this.Hotbar1SlotsPerRow = HotbarGridLayout.DefaultSlotsPerRow;
            this.Hotbar2SlotsPerRow = HotbarGridLayout.DefaultSlotsPerRow;
            this.Version = 38;
            didChange = true;
        }

        if (this.Version < 39)
        {
            this.LayoutUsesScreenCenterOrigin = false;
            this.Version = 39;
            didChange = true;
        }

        if (this.Version < 40)
        {
            this.LayoutUsesUnscaledPixelOffsets = false;
            this.Version = 40;
            didChange = true;
        }

        if (this.Version < 41)
        {
            var legacySlotSize = this.HotbarSlotSize > 0f ? this.HotbarSlotSize : HotbarLayout.DefaultSlotSize;
            var legacySlotGap = this.HotbarSlotGap >= 0f ? this.HotbarSlotGap : HotbarLayout.DefaultSlotGap;
            this.Hotbar1SlotSize = legacySlotSize;
            this.Hotbar1SlotGap = legacySlotGap;
            this.Hotbar2SlotSize = legacySlotSize;
            this.Hotbar2SlotGap = legacySlotGap;
            this.Version = 41;
            didChange = true;
        }

        if (this.Version < 42)
        {
            this.MinimapSize = MinimapLayout.DefaultSize;
            this.MinimapOffsetX = MinimapLayout.DefaultOffsetX;
            this.MinimapOffsetY = MinimapLayout.DefaultOffsetY;
            this.MinimapVisibleRangeYalms = MinimapLayout.DefaultVisibleRangeYalms;
            this.Version = 42;
            didChange = true;
        }

        if (this.Version < 43)
        {
            this.MinimapNorthLocked = false;
            this.Version = 43;
            didChange = true;
        }

        if (this.Version < 44)
        {
            this.MinimapFacingConeSizeScale = MinimapLayout.DefaultFacingConeSizeScale;
            this.MinimapFacingConeOpacity = MinimapLayout.DefaultFacingConeOpacity;
            this.Version = 44;
            didChange = true;
        }

        if (this.Version < 45)
        {
            this.MinimapBorderThickness = MinimapLayout.DefaultBorderThickness;
            this.Version = 45;
            didChange = true;
        }

        if (this.Version < 46)
        {
            this.MinimapBorderColor = MinimapLayout.DefaultBorderColor;
            this.Version = 46;
            didChange = true;
        }

        if (this.Version < 47)
        {
            this.MinimapBorderColor = HudColorConversion.MigrateLegacyArgbToImGuiColor(this.MinimapBorderColor);
            this.Version = 47;
            didChange = true;
        }

        if (this.Version < 48)
        {
            this.MinimapShowNativeMarkers = true;
            this.Version = 48;
            didChange = true;
        }

        if (this.Version < 49)
        {
            this.MinimapShowDiagnostics = false;
            this.Version = 49;
            didChange = true;
        }

        if (this.Version < 51)
        {
            if (this.MinimapMarkerIconSize < MinimapLayout.MinMarkerIconSize)
            {
                this.MinimapMarkerIconSize = MinimapLayout.DefaultMarkerIconSize;
            }

            this.Version = 51;
            didChange = true;
        }

        if (this.Version < 52)
        {
            if (this.MinimapPlayerPinSize < MinimapLayout.MinPlayerPinSize)
            {
                this.MinimapPlayerPinSize = MinimapLayout.DefaultPlayerPinSize;
            }

            if (this.MinimapPlayerPinColor == 0)
            {
                this.MinimapPlayerPinColor = MinimapLayout.DefaultPlayerPinColor;
            }

            this.Version = 52;
            didChange = true;
        }

        if (this.Version < 53)
        {
            this.Version = 53;
            didChange = true;
        }

        if (this.Version < 54)
        {
            this.Version = 54;
            didChange = true;
        }

        if (this.Version < 55)
        {
            this.MinimapShowCardinalDirections = false;
            this.Version = 55;
            didChange = true;
        }

        if (this.Version < 56)
        {
            this.ActionCamera ??= new ActionCameraConfiguration();
            this.Version = 56;
            didChange = true;
        }

        if (this.Version < 57)
        {
            this.ActionCamera ??= new ActionCameraConfiguration();
            this.ActionCamera.BackendMode = ActionCameraBackendMode.RmbLatch;
            this.ActionCamera.UnlockMode = ActionCameraUnlockMode.Toggle;
            this.ActionCamera.ToggleUnlockKey = Dalamud.Game.ClientState.Keys.VirtualKey.CAPITAL;
            this.ActionCamera.HoldUnlockKey = Dalamud.Game.ClientState.Keys.VirtualKey.LMENU;
            this.ActionCamera.EscAlwaysUnlock = true;
            this.ActionCamera.ReacquireOnToggle = true;
            this.ActionCamera.EnableSoftTargetSuggestion = false;
            this.ActionCamera.SoftTargetScreenRadius = 280f;
            this.Version = 57;
            didChange = true;
        }
        return didChange;
    }

    private bool NormalizeAfterLoad()
    {
        var changed = false;

        if (this.LeftHotbarActions is null)
        {
            this.LeftHotbarActions = new();
            changed = true;
        }

        if (this.RightHotbarActions is null)
        {
            this.RightHotbarActions = new();
            changed = true;
        }

        if (this.LeftHotbar2Actions is null)
        {
            this.LeftHotbar2Actions = new();
            changed = true;
        }

        if (this.RightHotbar2Actions is null)
        {
            this.RightHotbar2Actions = new();
            changed = true;
        }

        if (this.CapturedSquadronCommands is null)
        {
            this.CapturedSquadronCommands = new();
            changed = true;
        }

        if (this.CustomLayouts is null)
        {
            this.CustomLayouts = new();
            changed = true;
        }

        if (this.ActionCamera is null)
        {
            this.ActionCamera = new ActionCameraConfiguration();
            changed = true;
        }

        this.SelectedCustomLayoutName ??= string.Empty;

        if (!Enum.IsDefined(typeof(HudPreset), this.Preset))
        {
            this.Preset = HudPreset.Default;
            changed = true;
        }

        var normalizedGlobalScale = ClampFinite(this.GlobalScale, 2.25f, 0.5f, 4.0f);
        if (!NearlyEqual(normalizedGlobalScale, this.GlobalScale))
        {
            this.GlobalScale = normalizedGlobalScale;
            changed = true;
        }

        var normalizedGlobalOpacity = ClampFinite(this.GlobalOpacity, 1.0f, 0.2f, 1.0f);
        if (!NearlyEqual(normalizedGlobalOpacity, this.GlobalOpacity))
        {
            this.GlobalOpacity = normalizedGlobalOpacity;
            changed = true;
        }

        this.OrbRadius = NormalizeFloatField(this.OrbRadius, 56f, 32f, 160f, ref changed);
        this.OrbThickness = NormalizeFloatField(this.OrbThickness, 10f, 4f, 28f, ref changed);
        this.MpRingThicknessScale = NormalizeFloatField(this.MpRingThicknessScale, 1.20f, 0.2f, 1.2f, ref changed);
        this.SlidecastOffsetSeconds = NormalizeFloatField(this.SlidecastOffsetSeconds, 0.50f, 0.05f, 1.20f, ref changed);

        this.Hotbar1SlotSize = NormalizeFloatField(this.Hotbar1SlotSize, HotbarLayout.DefaultSlotSize, HotbarLayout.MinSlotSize, HotbarLayout.MaxSlotSize, ref changed);
        this.Hotbar1SlotGap = NormalizeFloatField(this.Hotbar1SlotGap, HotbarLayout.DefaultSlotGap, HotbarLayout.MinSlotGap, HotbarLayout.MaxSlotGap, ref changed);
        this.Hotbar2SlotSize = NormalizeFloatField(this.Hotbar2SlotSize, HotbarLayout.DefaultSlotSize, HotbarLayout.MinSlotSize, HotbarLayout.MaxSlotSize, ref changed);
        this.Hotbar2SlotGap = NormalizeFloatField(this.Hotbar2SlotGap, HotbarLayout.DefaultSlotGap, HotbarLayout.MinSlotGap, HotbarLayout.MaxSlotGap, ref changed);
        this.HotbarSlotSize = NormalizeFloatField(this.HotbarSlotSize, HotbarLayout.DefaultSlotSize, HotbarLayout.MinSlotSize, HotbarLayout.MaxSlotSize, ref changed);
        this.HotbarSlotGap = NormalizeFloatField(this.HotbarSlotGap, HotbarLayout.DefaultSlotGap, HotbarLayout.MinSlotGap, HotbarLayout.MaxSlotGap, ref changed);

        this.Hotbar1VisibleSlotCount = NormalizeIntField(this.Hotbar1VisibleSlotCount, HotbarSlotVisibility.ClampTotal(this.Hotbar1VisibleSlotCount), ref changed);
        this.Hotbar2VisibleSlotCount = NormalizeIntField(this.Hotbar2VisibleSlotCount, HotbarSlotVisibility.ClampTotal(this.Hotbar2VisibleSlotCount), ref changed);
        this.Hotbar1SlotsPerRow = NormalizeIntField(this.Hotbar1SlotsPerRow, HotbarGridLayout.ClampSlotsPerRow(this.Hotbar1SlotsPerRow), ref changed);
        this.Hotbar2SlotsPerRow = NormalizeIntField(this.Hotbar2SlotsPerRow, HotbarGridLayout.ClampSlotsPerRow(this.Hotbar2SlotsPerRow), ref changed);

        this.BuffIconSize = NormalizeFloatField(this.BuffIconSize, 78.3f, 18f, 120f, ref changed);
        this.BuffIconGap = NormalizeFloatField(this.BuffIconGap, 8f, 0f, 18f, ref changed);
        this.DebuffIconSize = NormalizeFloatField(this.DebuffIconSize, 78.3f, 18f, 120f, ref changed);
        this.DebuffIconGap = NormalizeFloatField(this.DebuffIconGap, 8f, 0f, 18f, ref changed);
        this.BuffMaxIconsPerRow = NormalizeIntField(this.BuffMaxIconsPerRow, StatusLaneLayout.ClampMaxIconsPerRow(this.BuffMaxIconsPerRow), ref changed);
        this.DebuffMaxIconsPerRow = NormalizeIntField(this.DebuffMaxIconsPerRow, StatusLaneLayout.ClampMaxIconsPerRow(this.DebuffMaxIconsPerRow), ref changed);

        if (!Enum.IsDefined(typeof(StatusLaneGrowDirection), this.BuffGrowDirection))
        {
            this.BuffGrowDirection = StatusLaneGrowDirection.RightToLeftUp;
            changed = true;
        }

        if (!Enum.IsDefined(typeof(StatusLaneGrowDirection), this.DebuffGrowDirection))
        {
            this.DebuffGrowDirection = StatusLaneGrowDirection.LeftToRightUp;
            changed = true;
        }

        if (!Enum.IsDefined(typeof(StatusTimerPlacement), this.BuffTimerPlacement))
        {
            this.BuffTimerPlacement = StatusTimerPlacement.Bottom;
            changed = true;
        }

        if (!Enum.IsDefined(typeof(StatusTimerPlacement), this.DebuffTimerPlacement))
        {
            this.DebuffTimerPlacement = StatusTimerPlacement.Bottom;
            changed = true;
        }

        this.MinimapSize = NormalizeFloatField(this.MinimapSize, MinimapLayout.DefaultSize, MinimapLayout.MinSize, MinimapLayout.MaxSize, ref changed);
        this.MinimapVisibleRangeYalms = NormalizeFloatField(this.MinimapVisibleRangeYalms, MinimapLayout.DefaultVisibleRangeYalms, MinimapLayout.MinVisibleRangeYalms, MinimapLayout.MaxVisibleRangeYalms, ref changed);
        this.MinimapFacingConeSizeScale = NormalizeFloatField(this.MinimapFacingConeSizeScale, MinimapLayout.DefaultFacingConeSizeScale, MinimapLayout.MinFacingConeSizeScale, MinimapLayout.MaxFacingConeSizeScale, ref changed);
        this.MinimapFacingConeOpacity = NormalizeFloatField(this.MinimapFacingConeOpacity, MinimapLayout.DefaultFacingConeOpacity, MinimapLayout.MinFacingConeOpacity, MinimapLayout.MaxFacingConeOpacity, ref changed);
        this.MinimapBorderThickness = NormalizeFloatField(this.MinimapBorderThickness, MinimapLayout.DefaultBorderThickness, MinimapLayout.MinBorderThickness, MinimapLayout.MaxBorderThickness, ref changed);
        this.MinimapMarkerIconSize = NormalizeFloatField(this.MinimapMarkerIconSize, MinimapLayout.DefaultMarkerIconSize, MinimapLayout.MinMarkerIconSize, MinimapLayout.MaxMarkerIconSize, ref changed);
        this.MinimapPlayerPinSize = NormalizeFloatField(this.MinimapPlayerPinSize, MinimapLayout.DefaultPlayerPinSize, MinimapLayout.MinPlayerPinSize, MinimapLayout.MaxPlayerPinSize, ref changed);
        this.ActionCamera.HorizontalSensitivity = NormalizeFloatField(
            this.ActionCamera.HorizontalSensitivity,
            1.0f,
            0.1f,
            5.0f,
            ref changed);
        this.ActionCamera.VerticalSensitivity = NormalizeFloatField(
            this.ActionCamera.VerticalSensitivity,
            1.0f,
            0.1f,
            5.0f,
            ref changed);

        if (!Enum.IsDefined(typeof(ActionCameraUnlockMode), this.ActionCamera.UnlockMode))
        {
            this.ActionCamera.UnlockMode = ActionCameraUnlockMode.Toggle;
            changed = true;
        }

        if (!Enum.IsDefined(typeof(ActionCameraBackendMode), this.ActionCamera.BackendMode))
        {
            this.ActionCamera.BackendMode = ActionCameraBackendMode.RmbLatch;
            changed = true;
        }

        this.ActionCamera.SoftTargetScreenRadius = NormalizeFloatField(
            this.ActionCamera.SoftTargetScreenRadius,
            280f,
            80f,
            1200f,
            ref changed);

        this.MinimapOffsetX = NormalizeFiniteFloat(this.MinimapOffsetX, MinimapLayout.DefaultOffsetX, ref changed);
        this.MinimapOffsetY = NormalizeFiniteFloat(this.MinimapOffsetY, MinimapLayout.DefaultOffsetY, ref changed);
        this.HudOffsetX = NormalizeFiniteFloat(this.HudOffsetX, 0f, ref changed);
        this.HudOffsetY = NormalizeFiniteFloat(this.HudOffsetY, 0f, ref changed);
        this.OrbOffsetX = NormalizeFiniteFloat(this.OrbOffsetX, 0f, ref changed);
        this.OrbOffsetY = NormalizeFiniteFloat(this.OrbOffsetY, 0f, ref changed);
        this.Hotbar1OffsetX = NormalizeFiniteFloat(this.Hotbar1OffsetX, 0f, ref changed);
        this.Hotbar1OffsetY = NormalizeFiniteFloat(this.Hotbar1OffsetY, -5f, ref changed);
        this.Hotbar2OffsetX = NormalizeFiniteFloat(this.Hotbar2OffsetX, 0f, ref changed);
        this.Hotbar2OffsetY = NormalizeFiniteFloat(this.Hotbar2OffsetY, 0f, ref changed);
        this.BuffOffsetX = NormalizeFiniteFloat(this.BuffOffsetX, 0f, ref changed);
        this.BuffOffsetY = NormalizeFiniteFloat(this.BuffOffsetY, 8.6f, ref changed);
        this.DebuffOffsetX = NormalizeFiniteFloat(this.DebuffOffsetX, 0f, ref changed);
        this.DebuffOffsetY = NormalizeFiniteFloat(this.DebuffOffsetY, 8.6f, ref changed);
        this.BuffRowOffsetX = NormalizeFiniteFloat(this.BuffRowOffsetX, 0f, ref changed);
        this.BuffRowYOffset = NormalizeFiniteFloat(this.BuffRowYOffset, 8.6f, ref changed);
        this.HotbarVerticalOffset = NormalizeFiniteFloat(this.HotbarVerticalOffset, -14f, ref changed);
        this.LimitBreakOffsetX = NormalizeFiniteFloat(this.LimitBreakOffsetX, -150f, ref changed);
        this.LimitBreakYOffset = NormalizeFiniteFloat(this.LimitBreakYOffset, 172f, ref changed);

        for (var i = this.CapturedSquadronCommands.Count - 1; i >= 0; i--)
        {
            var command = this.CapturedSquadronCommands[i];
            if (command is null)
            {
                this.CapturedSquadronCommands.RemoveAt(i);
                changed = true;
                continue;
            }

            if (command.TargetKey is null)
            {
                command.TargetKey = string.Empty;
                changed = true;
            }

            if (command.DisplayName is null)
            {
                command.DisplayName = string.Empty;
                changed = true;
            }
        }

        for (var i = this.CustomLayouts.Count - 1; i >= 0; i--)
        {
            var layout = this.CustomLayouts[i];
            if (layout is null)
            {
                this.CustomLayouts.RemoveAt(i);
                changed = true;
                continue;
            }

            if (layout.Name is null)
            {
                layout.Name = string.Empty;
                changed = true;
            }

            if (layout.Layout is null)
            {
                layout.Layout = new HudLayoutPresetSnapshot();
                changed = true;
            }
        }

        return changed;
    }

    private static float NormalizeFiniteFloat(float value, float fallback, ref bool changed)
    {
        var normalized = float.IsFinite(value) ? value : fallback;
        if (!NearlyEqual(normalized, value))
        {
            changed = true;
        }

        return normalized;
    }

    private static float NormalizeFloatField(float value, float fallback, float min, float max, ref bool changed)
    {
        var normalized = ClampFinite(value, fallback, min, max);
        if (!NearlyEqual(normalized, value))
        {
            changed = true;
        }

        return normalized;
    }

    private static int NormalizeIntField(int value, int normalized, ref bool changed)
    {
        if (value != normalized)
        {
            changed = true;
        }

        return normalized;
    }

    private static float ClampFinite(float value, float fallback, float min, float max)
    {
        var finiteValue = float.IsFinite(value) ? value : fallback;
        return Math.Clamp(finiteValue, min, max);
    }

    private static bool NearlyEqual(float a, float b) =>
        MathF.Abs(a - b) <= 0.0001f;

    public static void ApplyPreset(HudConfiguration config, HudPreset preset)
    {
        var appliedFromCustomLayout = TryApplyPresetOverrideFromCustomLayout(config, preset);
        if (!appliedFromCustomLayout)
        {
            var snapshot = preset switch
            {
                // Always use the authoritative Default layout so the button matches the tuned baseline.
                HudPreset.Default => HudLayoutPresetSnapshot.CreatePluginDefaultLayout(),
                HudPreset.Expanded => HudLayoutPresetSnapshot.CreatePluginExpandedLayout(),
                HudPreset.Arpg => HudLayoutPresetSnapshot.CreatePluginArpgLayout(),
                _ => HudLayoutPresetSnapshot.CreatePluginArpgLayout(),
            };

            snapshot.ApplyTo(config);
            config.SelectedCustomLayoutName = string.Empty;
        }

        ApplyPresetSupplementalSettings(config, preset);
        config.Preset = preset;
    }

    private static bool TryApplyPresetOverrideFromCustomLayout(HudConfiguration config, HudPreset preset)
    {
        var expectedLayoutName = preset switch
        {
            HudPreset.Default => "Default",
            HudPreset.Expanded => "Expanded",
            HudPreset.Arpg => "ARPG",
            _ => string.Empty,
        };

        if (expectedLayoutName.Length == 0)
        {
            return false;
        }

        return TryApplyCustomLayout(config, expectedLayoutName);
    }

    private static void ApplyPresetSupplementalSettings(HudConfiguration config, HudPreset preset)
    {
        if (preset == HudPreset.Default || preset == HudPreset.Expanded)
        {
            config.MinimapOffsetY = -820f;
            return;
        }

        if (preset == HudPreset.Arpg)
        {
            config.MinimapOffsetY = -996.5f;
        }
    }

    public static bool TryApplyCustomLayout(HudConfiguration config, string layoutName)
    {
        var normalizedName = layoutName.Trim();
        if (normalizedName.Length == 0)
        {
            return false;
        }

        config.CustomLayouts ??= new();
        for (var i = 0; i < config.CustomLayouts.Count; i++)
        {
            var entry = config.CustomLayouts[i];
            if (!string.Equals(entry.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entry.Layout.ApplyTo(config);
            config.SelectedCustomLayoutName = entry.Name;
            return true;
        }

        return false;
    }

    public static bool TrySaveCustomLayout(HudConfiguration config, string layoutName, out string error)
    {
        error = string.Empty;
        var normalizedName = layoutName.Trim();
        if (normalizedName.Length == 0)
        {
            error = "Enter a layout name before saving.";
            return false;
        }

        if (normalizedName.Length > 48)
        {
            error = "Layout name must be 48 characters or fewer.";
            return false;
        }

        config.CustomLayouts ??= new();
        var snapshot = HudLayoutPresetSnapshot.CaptureFrom(config);
        NamedHudLayoutPreset? existing = null;
        for (var i = 0; i < config.CustomLayouts.Count; i++)
        {
            if (string.Equals(config.CustomLayouts[i].Name, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                existing = config.CustomLayouts[i];
                break;
            }
        }

        if (existing is not null)
        {
            existing.Name = normalizedName;
            existing.Layout = snapshot;
        }
        else
        {
            if (config.CustomLayouts.Count >= 24)
            {
                error = "Maximum of 24 custom layouts reached. Delete one before saving a new layout.";
                return false;
            }

            config.CustomLayouts.Add(new NamedHudLayoutPreset
            {
                Name = normalizedName,
                Layout = snapshot,
            });
        }

        config.SelectedCustomLayoutName = normalizedName;
        return true;
    }

    public static bool TryDeleteCustomLayout(HudConfiguration config, string layoutName)
    {
        var normalizedName = layoutName.Trim();
        if (normalizedName.Length == 0)
        {
            return false;
        }

        config.CustomLayouts ??= new();
        for (var i = config.CustomLayouts.Count - 1; i >= 0; i--)
        {
            if (!string.Equals(config.CustomLayouts[i].Name, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            config.CustomLayouts.RemoveAt(i);
            if (string.Equals(config.SelectedCustomLayoutName, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                config.SelectedCustomLayoutName = string.Empty;
            }

            return true;
        }

        return false;
    }

    public static void ApplyDefaultPreset(HudConfiguration config)
    {
        ApplyPreset(config, HudPreset.Default);
    }

    public void Save()
    {
        this.pluginInterface?.SavePluginConfig(this);
    }
}

[Serializable]
public sealed class SquadronCapturedCommandConfig
{
    public string TargetKey { get; set; } = string.Empty;
    public HotbarAssignCommandKind CommandKind { get; set; } = HotbarAssignCommandKind.GeneralAction;
    public uint CommandId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public uint IconId { get; set; }
}

public enum HudPreset
{
    Default = 0,
    Expanded = 1,
    Arpg = 2,
}
