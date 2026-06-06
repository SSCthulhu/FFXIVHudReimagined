using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;

namespace FFXIVHudPlugin.AetherPlates.Data;

public enum EnemyNameplateState : byte
{
    Unknown = 0,
    Unengaged = 1,
    Engaged = 2,
    Claimed = 3,
    Unclaimed = 4,
    Feast = 5,
    FeastPet = 6,
}

public sealed record TrackedObject(
    ulong ObjectId,
    uint EntityId,
    nint Address,
    string Name,
    ObjectKind Kind,
    uint CurrentHp,
    uint MaxHp,
    float ShieldRatio,
    Vector3 Position,
    float Height,
    bool Targetable,
    float Distance,
    uint JobId,
    int Level,
    bool IsTarget,
    bool IsFocusTarget,
    bool IsHostile,
    bool IsFriendly,
    bool IsPartyMember,
    bool IsAllianceMember,
    bool IsFriend,
    EnemyNameplateState EnemyState,
    ulong OwnerId,
    byte SubKind,
    bool IsPlayerCharacter,
    IReadOnlyList<StatusSnapshot> Statuses,
    CastSnapshot CastInfo);

public readonly record struct StatusSnapshot(
    uint StatusId,
    ushort StackCount,
    float RemainingTime,
    uint SourceId,
    bool IsDebuff,
    string Name,
    uint IconId);

public readonly record struct CastSnapshot(
    bool IsCasting,
    string ActionName,
    float CurrentTime,
    float TotalTime,
    bool IsInterruptible);
