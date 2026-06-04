using Dalamud.Interface.Textures;
using System.Numerics;

namespace FFXIVHudPlugin;

public sealed class HudStateSnapshot
{
    public bool HasPlayer { get; init; }
    public string PlayerName { get; init; } = string.Empty;
    public uint CurrentHp { get; init; }
    public uint MaxHp { get; init; }
    public uint CurrentMp { get; init; }
    public uint MaxMp { get; init; }
    public float HpRatio => this.MaxHp == 0 ? 0f : this.CurrentHp / (float)this.MaxHp;
    public float HpAnimatedRatio { get; init; }
    public float ShieldRatio { get; init; }
    public uint ShieldAmount { get; init; }
    public float MpRatio => this.MaxMp == 0 ? 0f : this.CurrentMp / (float)this.MaxMp;
    public float MpAnimatedRatio { get; init; }
    public bool IsCasting { get; init; }
    public float CastProgressRatio { get; init; }
    public float CastTotalSeconds { get; init; }
    public IReadOnlyList<StatusViewModel> Buffs { get; init; } = Array.Empty<StatusViewModel>();
    public IReadOnlyList<StatusViewModel> Debuffs { get; init; } = Array.Empty<StatusViewModel>();
    public IReadOnlyList<HotbarSlotViewModel> LeftHotbar { get; init; } = Array.Empty<HotbarSlotViewModel>();
    public IReadOnlyList<HotbarSlotViewModel> RightHotbar { get; init; } = Array.Empty<HotbarSlotViewModel>();
    public IReadOnlyList<HotbarSlotViewModel> LeftHotbar2 { get; init; } = Array.Empty<HotbarSlotViewModel>();
    public IReadOnlyList<HotbarSlotViewModel> RightHotbar2 { get; init; } = Array.Empty<HotbarSlotViewModel>();
    public LimitBreakViewModel LimitBreak { get; init; } = LimitBreakViewModel.Empty;
    public MinimapSnapshot Minimap { get; init; } = MinimapSnapshot.Empty;
}

public static class GameHotbar
{
    public const int Hotbar1BarIndex = 0;
    public const int Hotbar2BarIndex = 1;
}

public enum StatusLaneGrowDirection
{
    LeftToRightUp = 0,
    RightToLeftDown = 1,
    LeftToRightDown = 2,
    RightToLeftUp = 3,
}

public enum StatusTimerPlacement
{
    Bottom = 0,
    Top = 1,
}

public sealed class StatusViewModel
{
    public uint StatusId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public float RemainingTime { get; init; }
    public bool ShowTimer { get; init; }
    public bool IsDebuff { get; init; }
    public ISharedImmediateTexture? Icon { get; init; }
}

public sealed class HotbarSlotViewModel
{
    /// <summary>Game hotbar slot index on this bar (0-11).</summary>
    public int GameSlotIndex { get; init; }
    public uint ActionId { get; init; }
    public uint TooltipId { get; init; }
    public string TooltipKindLabel { get; init; } = "Ability";
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Keybind { get; init; } = string.Empty;
    public float CooldownRatio { get; init; }
    public float CooldownSecondsRemaining { get; init; }
    public float CastTimeSeconds { get; init; }
    public float RecastTimeSeconds { get; init; }
    public int RangeYalms { get; init; }
    public int RadiusYalms { get; init; }
    public int RequiredLevel { get; init; }
    public string JobAbbrev { get; init; } = string.Empty;
    public int ChargesCurrent { get; init; }
    public int ChargesMax { get; init; }
    public bool IsUsable { get; init; } = true;
    public bool IsProc { get; init; }
    public ISharedImmediateTexture? Icon { get; init; }
}

public enum HotbarAssignCategory
{
    Actions = 0,
    Role = 1,
    Duties = 2,
    Performance = 3,
    Orders = 4,
    General = 5,
    MainCommands = 6,
    Extras = 7,
}

public enum HotbarAssignCommandKind
{
    Action = 0,
    GeneralAction = 1,
    MainCommand = 2,
    ExtraCommand = 3,
    BuddyAction = 4,
    PetAction = 5,
    Unknown23 = 6,
    Unknown28 = 7,
}

public enum HotbarOrderSection
{
    Companion = 0,
    Squadron = 1,
    Pets = 2,
}

public enum HotbarMainCommandSection
{
    Character = 0,
    Duty = 1,
    Logs = 2,
    Travel = 3,
    Party = 4,
    Social = 5,
    System = 6,
}

public sealed class HotbarAssignEntry
{
    public uint CommandId { get; init; }
    public HotbarAssignCommandKind CommandKind { get; init; } = HotbarAssignCommandKind.Action;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int RequiredLevel { get; init; }
    public string JobAbbrev { get; init; } = string.Empty;
    public string Affinity { get; init; } = string.Empty;
    public ISharedImmediateTexture? Icon { get; init; }
}

public sealed class LimitBreakViewModel
{
    public static LimitBreakViewModel Empty { get; } = new()
    {
        SegmentFill = new[] { 0f, 0f, 0f },
        MaxSegments = 3,
    };

    public IReadOnlyList<float> SegmentFill { get; init; } = Array.Empty<float>();
    public int MaxSegments { get; init; } = 3;
}

public readonly record struct HudLayoutRects(
    Vector2 Center,
    Vector2 OrbCenter,
    Vector2 Hotbar1Start,
    Vector2 Hotbar2Start,
    Vector2 LeftBuffStart,
    Vector2 RightDebuffStart,
    Vector2 LimitBreakStart,
    Vector2 MinimapCenter);
