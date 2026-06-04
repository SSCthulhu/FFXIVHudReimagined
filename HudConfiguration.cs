using Dalamud.Configuration;
using Dalamud.Plugin;
using System.Numerics;

namespace FFXIVHudPlugin;

[Serializable]
public sealed class HudConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 55;

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

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        this.ApplyMigrations();
    }

    private void ApplyMigrations()
    {
        if (this.Version < 2)
        {
            // Force a visibly larger baseline to ensure existing users immediately see the scale change.
            this.GlobalScale = 3.0f;
            this.CenterAnchor = new Vector2(0.5f, 0.72f);
            this.BuffRowYOffset = -138f;
            this.LimitBreakYOffset = 230f;
            this.Version = 2;
            this.Save();
        }

        if (this.Version < 3)
        {
            // Reduce the previously forced oversized layout by 50%.
            this.GlobalScale = float.Clamp(this.GlobalScale * 0.5f, 0.5f, 4.0f);
            this.BuffRowYOffset *= 0.5f;
            this.LimitBreakYOffset *= 0.5f;
            this.Version = 3;
            this.Save();
        }

        if (this.Version < 4)
        {
            // Buff/debuff rows are now anchored above hotbars; use 0 as neutral offset.
            this.BuffRowYOffset = 0f;
            this.Version = 4;
            this.Save();
        }

        if (this.Version < 5)
        {
            // Increase status icon readability to better match the native HUD scale.
            this.BuffIconSize = Math.Max(this.BuffIconSize, 40f);
            this.BuffIconGap = Math.Max(this.BuffIconGap, 8f);
            this.Version = 5;
            this.Save();
        }

        if (this.Version < 6)
        {
            this.Version = 6;
            this.Save();
        }

        if (this.Version < 7)
        {
            // New global HUD pixel offsets default to centered.
            this.HudOffsetX = 0f;
            this.HudOffsetY = 0f;
            this.Version = 7;
            this.Save();
        }

        if (this.Version < 8)
        {
            // Match updated HP/MP orb palette with default-game-inspired green and magenta tones.
            this.ColorHpFill = 0xFF3FCF46;
            this.ColorMpFill = 0xFFE45DB2;
            this.ColorMpBack = 0x552A1424;
            this.Version = 8;
            this.Save();
        }

        if (this.Version < 9)
        {
            // Correct accent to true gold in ABGR packing (not blue-tinted).
            this.ColorAccent = 0xFF37AFD4;
            this.Version = 9;
            this.Save();
        }

        if (this.Version < 10)
        {
            // Align HP/MP colors to the default player bar palette references.
            this.ColorHpFill = 0xFF4AB34A;
            this.ColorMpFill = 0xFFA755E5;
            this.Version = 10;
            this.Save();
        }

        if (this.Version < 11)
        {
            // Enable slidecast marker defaults for the top cast arc.
            this.ShowSlidecastMarker = true;
            this.SlidecastOffsetSeconds = Math.Clamp(this.SlidecastOffsetSeconds, 0.05f, 1.20f);
            this.Version = 11;
            this.Save();
        }

        if (this.Version < 12)
        {
            this.Version = 12;
            this.Save();
        }

        if (this.Version < 14)
        {
            // Drag/drop assignment replaced with picker-based slot assignment.
            this.Version = 14;
            this.Save();
        }

        if (this.Version < 17)
        {
            // Removed plugin-managed keybind overrides and formatting controls.
            this.Version = 17;
            this.Save();
        }

        if (this.Version < 18)
        {
            // Persist discovered mission-only squadron order commands across reloads.
            this.CapturedSquadronCommands ??= new();
            this.Version = 18;
            this.Save();
        }

        if (this.Version < 19)
        {
            // Click-through, native UI replacement, and debug overlay are always on; presets consolidated to Default.
            this.Preset = HudPreset.Default;
            this.Version = 19;
            this.Save();
        }

        if (this.Version < 20)
        {
            this.LeftHotbar2Actions ??= new();
            this.RightHotbar2Actions ??= new();
            this.Version = 20;
            this.Save();
        }

        if (this.Version < 21)
        {
            this.Hotbar1OffsetX = 0f;
            this.Hotbar1OffsetY = 0f;
            this.Hotbar2OffsetX = 0f;
            this.Hotbar2OffsetY = 0f;
            this.Version = 21;
            this.Save();
        }

        if (this.Version < 22)
        {
            this.Hotbar1Enabled = true;
            this.Hotbar2Enabled = true;
            this.Version = 22;
            this.Save();
        }

        if (this.Version < 23)
        {
            this.Version = 23;
            this.Save();
        }

        if (this.Version < 24)
        {
            // Preserve the user's current tuned layout as the Expanded preset (not code defaults).
            this.ExpandedPresetSnapshot ??= HudLayoutPresetSnapshot.CaptureFrom(this);
            this.DefaultPresetSnapshot ??= HudLayoutPresetSnapshot.CreateFactoryDefault();
            this.Version = 24;
            this.Save();
        }

        if (this.Version < 25)
        {
            // Default preset is now a fixed tuned layout; refresh stored snapshot to match.
            this.DefaultPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginDefaultLayout();
            this.Version = 25;
            this.Save();
        }

        if (this.Version < 26)
        {
            this.CustomLayouts ??= new();
            this.ExpandedPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginExpandedLayout();
            this.Version = 26;
            this.Save();
        }

        if (this.Version < 27)
        {
            this.ExpandedPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginExpandedLayout();
            this.Version = 27;
            this.Save();
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
            this.Save();
        }

        if (this.Version < 29)
        {
            this.ExpandedPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginExpandedLayout();
            this.Version = 29;
            this.Save();
        }

        if (this.Version < 30)
        {
            this.UnlockLayout = false;
            this.Version = 30;
            this.Save();
        }

        if (this.Version < 31)
        {
            this.Version = 31;
            this.Save();
        }

        if (this.Version < 32)
        {
            this.Version = 32;
            this.Save();
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
            this.Save();
        }

        if (this.Version < 34)
        {
            this.BuffTimerPlacement = StatusTimerPlacement.Bottom;
            this.DebuffTimerPlacement = StatusTimerPlacement.Bottom;
            this.Version = 34;
            this.Save();
        }

        if (this.Version < 35)
        {
            this.ArpgPresetSnapshot = HudLayoutPresetSnapshot.CreatePluginArpgLayout();
            this.Version = 35;
            this.Save();
        }

        if (this.Version < 36)
        {
            this.BuffMaxIconsPerRow = StatusLaneLayout.ClampMaxIconsPerRow(this.BuffMaxIconsPerRow);
            this.DebuffMaxIconsPerRow = StatusLaneLayout.ClampMaxIconsPerRow(this.DebuffMaxIconsPerRow);
            this.Version = 36;
            this.Save();
        }

        if (this.Version < 37)
        {
            this.BuffGrowDirection = StatusLaneLayout.MigrateLegacyGrowDirection((int)this.BuffGrowDirection);
            this.DebuffGrowDirection = StatusLaneLayout.MigrateLegacyGrowDirection((int)this.DebuffGrowDirection);
            this.Version = 37;
            this.Save();
        }

        if (this.Version < 38)
        {
            this.Hotbar1SlotsPerRow = HotbarGridLayout.DefaultSlotsPerRow;
            this.Hotbar2SlotsPerRow = HotbarGridLayout.DefaultSlotsPerRow;
            this.Version = 38;
            this.Save();
        }

        if (this.Version < 39)
        {
            this.LayoutUsesScreenCenterOrigin = false;
            this.Version = 39;
            this.Save();
        }

        if (this.Version < 40)
        {
            this.LayoutUsesUnscaledPixelOffsets = false;
            this.Version = 40;
            this.Save();
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
            this.Save();
        }

        if (this.Version < 42)
        {
            this.MinimapSize = MinimapLayout.DefaultSize;
            this.MinimapOffsetX = MinimapLayout.DefaultOffsetX;
            this.MinimapOffsetY = MinimapLayout.DefaultOffsetY;
            this.MinimapVisibleRangeYalms = MinimapLayout.DefaultVisibleRangeYalms;
            this.Version = 42;
            this.Save();
        }

        if (this.Version < 43)
        {
            this.MinimapNorthLocked = false;
            this.Version = 43;
            this.Save();
        }

        if (this.Version < 44)
        {
            this.MinimapFacingConeSizeScale = MinimapLayout.DefaultFacingConeSizeScale;
            this.MinimapFacingConeOpacity = MinimapLayout.DefaultFacingConeOpacity;
            this.Version = 44;
            this.Save();
        }

        if (this.Version < 45)
        {
            this.MinimapBorderThickness = MinimapLayout.DefaultBorderThickness;
            this.Version = 45;
            this.Save();
        }

        if (this.Version < 46)
        {
            this.MinimapBorderColor = MinimapLayout.DefaultBorderColor;
            this.Version = 46;
            this.Save();
        }

        if (this.Version < 47)
        {
            this.MinimapBorderColor = HudColorConversion.MigrateLegacyArgbToImGuiColor(this.MinimapBorderColor);
            this.Version = 47;
            this.Save();
        }

        if (this.Version < 48)
        {
            this.MinimapShowNativeMarkers = true;
            this.Version = 48;
            this.Save();
        }

        if (this.Version < 49)
        {
            this.MinimapShowDiagnostics = false;
            this.Version = 49;
            this.Save();
        }

        if (this.Version < 51)
        {
            if (this.MinimapMarkerIconSize < MinimapLayout.MinMarkerIconSize)
            {
                this.MinimapMarkerIconSize = MinimapLayout.DefaultMarkerIconSize;
            }

            this.Version = 51;
            this.Save();
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
            this.Save();
        }

        if (this.Version < 53)
        {
            this.Version = 53;
            this.Save();
        }

        if (this.Version < 54)
        {
            this.Version = 54;
            this.Save();
        }

        if (this.Version < 55)
        {
            this.MinimapShowCardinalDirections = false;
            this.Version = 55;
            this.Save();
        }

        this.CapturedSquadronCommands ??= new();
        this.LeftHotbar2Actions ??= new();
        this.RightHotbar2Actions ??= new();
        this.CustomLayouts ??= new();
    }

    public static void ApplyPreset(HudConfiguration config, HudPreset preset)
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
        config.Preset = preset;
        config.SelectedCustomLayoutName = string.Empty;
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
