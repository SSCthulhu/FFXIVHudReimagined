using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Lumina.Excel.Sheets;
using Dalamud.Plugin.Services;
using FFXIVHudPlugin.AetherPlates.Data;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace FFXIVHudPlugin.AetherPlates.Services;

public sealed class ObjectService
{
    private readonly IObjectTable objectTable;
    private readonly ITargetManager targetManager;
    private readonly IPartyList partyList;
    private readonly IClientState clientState;
    private readonly IDataManager dataManager;
    private readonly Dictionary<uint, (string Name, uint IconId, bool IsDebuff)> statusMetaCache = new();

    public ObjectService(
        IObjectTable objectTable,
        ITargetManager targetManager,
        IPartyList partyList,
        IClientState clientState,
        IDataManager dataManager)
    {
        this.objectTable = objectTable;
        this.targetManager = targetManager;
        this.partyList = partyList;
        this.clientState = clientState;
        this.dataManager = dataManager;
    }

    public IReadOnlyList<TrackedObject> BuildSnapshot()
    {
        var list = new List<TrackedObject>(384);
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer is null)
        {
            return list;
        }

        var targetId = this.targetManager.Target?.GameObjectId ?? 0;
        var focusId = this.targetManager.FocusTarget?.GameObjectId ?? 0;

        for (var i = 0; i < this.objectTable.Length; i++)
        {
            var obj = this.objectTable[i];
            if (obj is null)
            {
                continue;
            }

            if (!TryBuildTrackedObject(obj, localPlayer, targetId, focusId, out var tracked))
            {
                continue;
            }

            list.Add(tracked);
        }

        return list;
    }

    private bool TryBuildTrackedObject(
        IGameObject obj,
        IGameObject localPlayer,
        ulong targetId,
        ulong focusId,
        out TrackedObject tracked)
    {
        tracked = null!;
        var objectId = ResolveStableObjectId(obj);
        if (objectId == 0)
        {
            return false;
        }

        if (!ShouldTrackObject(obj))
        {
            return false;
        }

        var character = obj as ICharacter;
        var isCharacter = character is not null;
        var isPlayerCharacter = obj.GameObjectId == localPlayer.GameObjectId;
        var isPartyMember = IsPartyMember(obj.GameObjectId);
        var distance = Vector3.Distance(localPlayer.Position, obj.Position);
        var statusFlags = character?.StatusFlags ?? 0;

        var name = ResolveName(obj, character);
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var currentHp = character?.CurrentHp ?? 1u;
        var maxHp = character?.MaxHp ?? 1u;
        if (isCharacter && maxHp == 0 && obj.ObjectKind is not ObjectKind.EventNpc and not ObjectKind.Companion)
        {
            return false;
        }

        var statuses = character is IBattleChara battle
            ? BuildStatusIds(battle.StatusList)
            : Array.Empty<StatusSnapshot>();
        var cast = character is null
            ? new CastSnapshot(false, string.Empty, 0f, 0f, false)
            : BuildCastSnapshot(character, this.dataManager);

        var isFriendly = isPlayerCharacter ||
                         isPartyMember ||
                         (statusFlags & (StatusFlags.AllianceMember | StatusFlags.Friend)) != 0 ||
                         obj.ObjectKind == ObjectKind.Pc;
        var subKind = (BattleNpcSubKind)obj.SubKind;
        var isOwnedBattleCompanion = obj.ObjectKind == ObjectKind.BattleNpc &&
                                     (subKind == BattleNpcSubKind.Pet || subKind == BattleNpcSubKind.Buddy);
        var isHostile = obj.ObjectKind == ObjectKind.BattleNpc && !isFriendly && !isOwnedBattleCompanion;
        var enemyState = ResolveEnemyState(obj, character, isHostile);
        var shieldRatio = Math.Clamp((character?.ShieldPercentage ?? 0) / 100f, 0f, 1f);

        tracked = new TrackedObject(
            objectId,
            obj.EntityId,
            obj.Address,
            name,
            obj.ObjectKind,
            currentHp,
            maxHp,
            shieldRatio,
            obj.Position,
            character?.HitboxRadius ?? 0f,
            obj.IsTargetable,
            distance,
            character?.ClassJob.RowId ?? 0u,
            character?.Level ?? 0,
            targetId != 0 && targetId == obj.GameObjectId,
            focusId != 0 && focusId == obj.GameObjectId,
            isHostile,
            isFriendly,
            isPartyMember,
            (statusFlags & StatusFlags.AllianceMember) != 0,
            (statusFlags & StatusFlags.Friend) != 0,
            enemyState,
            obj.OwnerId,
            obj.SubKind,
            isPlayerCharacter,
            statuses,
            cast);
        return true;
    }

    private static bool ShouldTrackObject(IGameObject obj)
    {
        if (obj.ObjectKind == ObjectKind.Companion || obj.ObjectKind == ObjectKind.EventNpc)
        {
            return true;
        }

        if (obj.ObjectKind is ObjectKind.GatheringPoint or ObjectKind.Treasure or ObjectKind.EventObj)
        {
            return true;
        }

        return obj.IsTargetable;
    }

    private static ulong ResolveStableObjectId(IGameObject obj)
    {
        var rawId = unchecked((ulong)obj.GameObjectId);
        if (rawId != 0 && rawId != 0xE0000000)
        {
            return rawId;
        }

        if (obj.Address == nint.Zero)
        {
            return 0;
        }

        // Some world NPC/minion entries use pseudo IDs. Use address-based fallback identity so
        // nameplate tracking remains stable for rendering and category resolution.
        return 0xF000000000000000UL | ((ulong)obj.Address & 0x0FFFFFFFFFFFFFFFUL);
    }

    private static string ResolveName(IGameObject obj, ICharacter? character)
    {
        if (character is not null && !string.IsNullOrWhiteSpace(character.Name.TextValue))
        {
            return character.Name.TextValue;
        }

        var name = obj.Name.TextValue;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return obj.ObjectKind switch
        {
            ObjectKind.GatheringPoint => "Gathering Point",
            ObjectKind.Treasure => "Treasure",
            ObjectKind.EventObj => "Event Object",
            _ => string.Empty,
        };
    }

    private static unsafe EnemyNameplateState ResolveEnemyState(
        IGameObject obj,
        ICharacter? character,
        bool isHostile)
    {
        if (!isHostile || character is null || obj.ObjectKind != ObjectKind.BattleNpc)
        {
            return EnemyNameplateState.Unknown;
        }

        var chara = (Character*)obj.Address;
        if (chara == null)
        {
            return EnemyNameplateState.Unengaged;
        }

        return chara->GetNamePlateColorType() switch
        {
            7 => EnemyNameplateState.Unengaged,
            9 => EnemyNameplateState.Engaged,
            10 => EnemyNameplateState.Claimed,
            11 => EnemyNameplateState.Unclaimed,
            _ => EnemyNameplateState.Unengaged,
        };
    }

    private bool IsPartyMember(ulong gameObjectId)
    {
        for (var i = 0; i < this.partyList.Length; i++)
        {
            var member = this.partyList[i];
            if (member.GameObject?.GameObjectId == gameObjectId)
            {
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<StatusSnapshot> BuildStatusIds(StatusList statusList)
    {
        var values = new List<StatusSnapshot>(statusList.Length);
        for (var i = 0; i < statusList.Length; i++)
        {
            var status = statusList[i];
            if (status.StatusId != 0)
            {
                var meta = this.ResolveStatusMeta(status.StatusId);
                values.Add(new StatusSnapshot(
                    status.StatusId,
                    status.Param,
                    status.RemainingTime,
                    status.SourceId,
                    meta.IsDebuff,
                    meta.Name,
                    meta.IconId));
            }
        }

        return values;
    }

    private (string Name, uint IconId, bool IsDebuff) ResolveStatusMeta(uint statusId)
    {
        if (this.statusMetaCache.TryGetValue(statusId, out var cached))
        {
            return cached;
        }

        var sheet = this.dataManager.GetExcelSheet<Status>();
        if (sheet is not null && sheet.TryGetRow(statusId, out var row))
        {
            var value = (
                row.Name.ExtractText() ?? string.Empty,
                row.Icon,
                row.StatusCategory is 2 or 7);
            this.statusMetaCache[statusId] = value;
            return value;
        }

        var fallback = (string.Empty, 0u, false);
        this.statusMetaCache[statusId] = fallback;
        return fallback;
    }

    private static CastSnapshot BuildCastSnapshot(ICharacter character, IDataManager dataManager)
    {
        var isCasting = character is IBattleChara battleChara && battleChara.IsCasting;
        if (!isCasting || character is not IBattleChara caster)
        {
            return new CastSnapshot(false, string.Empty, 0f, 0f, false);
        }

        var actionName = ResolveCastActionName(caster, dataManager);
        var isInterruptible = caster.IsCastInterruptible;

        return new CastSnapshot(
            true,
            actionName,
            Math.Max(0f, caster.CurrentCastTime),
            Math.Max(0f, caster.TotalCastTime),
            isInterruptible);
    }

    private static unsafe string ResolveCastActionName(IBattleChara caster, IDataManager dataManager)
    {
        var actionId = caster.CastActionId;
        if (actionId == 0)
        {
            return string.Empty;
        }

        var actions = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        if (actions is null || !actions.TryGetRow(actionId, out var actionRow))
        {
            return string.Empty;
        }

        return actionRow.Name.ExtractText() ?? string.Empty;
    }

}
