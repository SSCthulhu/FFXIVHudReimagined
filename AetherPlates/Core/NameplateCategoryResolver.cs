using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.NamePlate;
using FFXIVHudPlugin.AetherPlates.Data;

namespace FFXIVHudPlugin.AetherPlates.Core;

internal static class NameplateCategoryResolver
{
    public static NameplateManager.NameplateCategory ResolveForTrackedObject(
        TrackedObject obj,
        ulong localPlayerId,
        NamePlateKind? nativeKind,
        IReadOnlyDictionary<ulong, TrackedObject> ownerLookup)
    {
        if (obj.Kind == ObjectKind.Companion)
        {
            return NameplateManager.NameplateCategory.Minion;
        }

        if (obj.Kind == ObjectKind.EventNpc)
        {
            return NameplateManager.NameplateCategory.Npc;
        }

        if (obj.Kind is ObjectKind.GatheringPoint or ObjectKind.Treasure or ObjectKind.EventObj)
        {
            return NameplateManager.NameplateCategory.Object;
        }

        if (obj.Kind == ObjectKind.HousingEventObject)
        {
            return NameplateManager.NameplateCategory.HousingFurniture;
        }

        var subKind = (BattleNpcSubKind)obj.SubKind;

        if (obj.Kind == ObjectKind.BattleNpc &&
            (subKind == BattleNpcSubKind.Pet || subKind == BattleNpcSubKind.Buddy))
        {
            return ResolveFriendlyBattleNpcCategory(obj, localPlayerId, ownerLookup);
        }

        if (obj.Kind == ObjectKind.BattleNpc &&
            (subKind == BattleNpcSubKind.Combatant || subKind == BattleNpcSubKind.BNpcPart))
        {
            return ResolveEnemyCategory(obj);
        }

        if (subKind == BattleNpcSubKind.NpcPartyMember)
        {
            // Duty Support / Trust actors should follow NPC category mapping.
            return NameplateManager.NameplateCategory.Npc;
        }

        if (obj.IsPlayerCharacter)
        {
            return NameplateManager.NameplateCategory.Self;
        }

        if (obj.IsPartyMember)
        {
            return NameplateManager.NameplateCategory.Party;
        }

        if (obj.IsAllianceMember)
        {
            return NameplateManager.NameplateCategory.Alliance;
        }

        if (obj.IsFriend)
        {
            return NameplateManager.NameplateCategory.Friend;
        }

        if (nativeKind.HasValue)
        {
            switch (nativeKind.Value)
            {
                case NamePlateKind.PlayerCharacter:
                    return NameplateManager.NameplateCategory.OtherPc;
                case NamePlateKind.BattleNpcEnemy:
                    if (obj.Kind == ObjectKind.BattleNpc && (subKind == BattleNpcSubKind.Pet || subKind == BattleNpcSubKind.Buddy))
                    {
                        return ResolveFriendlyBattleNpcCategory(obj, localPlayerId, ownerLookup);
                    }
                    return ResolveEnemyCategory(obj);
                case NamePlateKind.BattleNpcFriendly:
                    return ResolveFriendlyBattleNpcCategory(obj, localPlayerId, ownerLookup);
                case NamePlateKind.EventObject:
                case NamePlateKind.GatheringPoint:
                case NamePlateKind.Treasure:
                    return NameplateManager.NameplateCategory.Object;
                default:
                    return NameplateManager.NameplateCategory.UnknownFriendly;
            }
        }

        if (obj.IsHostile)
        {
            return ResolveEnemyCategory(obj);
        }

        return ResolveFriendlyBattleNpcCategory(obj, localPlayerId, ownerLookup);
    }

    public static NameplateManager.NameplateCategory ResolveForNativeHandler(
        NamePlateKind nativeKind,
        ulong gameObjectId,
        IGameObject? gameObject,
        ICharacter? playerCharacter,
        ulong localPlayerId,
        uint localPlayerEntityId,
        nint localPlayerAddress,
        IReadOnlyDictionary<ulong, TrackedObject> ownerLookup)
    {
        if (localPlayerId != 0 && gameObjectId == localPlayerId)
        {
            return NameplateManager.NameplateCategory.Self;
        }

        if (playerCharacter is not null)
        {
            if ((localPlayerId != 0 && playerCharacter.GameObjectId == localPlayerId) ||
                (localPlayerEntityId != 0 && playerCharacter.EntityId == localPlayerEntityId))
            {
                return NameplateManager.NameplateCategory.Self;
            }
        }

        if (localPlayerAddress != nint.Zero)
        {
            if ((gameObject is not null && gameObject.Address == localPlayerAddress) ||
                (playerCharacter is not null && playerCharacter.Address == localPlayerAddress))
            {
                return NameplateManager.NameplateCategory.Self;
            }
        }

        var tracked = ownerLookup.TryGetValue(gameObjectId, out var fromLookup)
            ? fromLookup
            : null;

        if (tracked is not null)
        {
            return ResolveForTrackedObject(tracked, localPlayerId, nativeKind, ownerLookup);
        }

        if (nativeKind == NamePlateKind.PlayerCharacter)
        {
            return NameplateManager.NameplateCategory.OtherPc;
        }

        if (nativeKind == NamePlateKind.BattleNpcEnemy)
        {
            if (gameObject is IBattleNpc npc &&
                (npc.BattleNpcKind == BattleNpcSubKind.Pet || npc.BattleNpcKind == BattleNpcSubKind.Buddy))
            {
                return NameplateManager.NameplateCategory.OtherPet;
            }

            return NameplateManager.NameplateCategory.EnemyUnengaged;
        }

        if (nativeKind == NamePlateKind.BattleNpcFriendly)
        {
            return NameplateManager.NameplateCategory.Npc;
        }

        return NameplateManager.NameplateCategory.UnknownFriendly;
    }

    private static NameplateManager.NameplateCategory ResolveFriendlyBattleNpcCategory(
        TrackedObject obj,
        ulong localPlayerId,
        IReadOnlyDictionary<ulong, TrackedObject> ownerLookup)
    {
        var subKind = (BattleNpcSubKind)obj.SubKind;
        var isCompanion = subKind == BattleNpcSubKind.Buddy;
        var isPet = subKind == BattleNpcSubKind.Pet;
        if (subKind == BattleNpcSubKind.NpcPartyMember)
        {
            return NameplateManager.NameplateCategory.Npc;
        }

        if (obj.OwnerId != 0 && ownerLookup.TryGetValue(obj.OwnerId, out var owner))
        {
            if (owner.IsPlayerCharacter || owner.ObjectId == localPlayerId)
            {
                if (isCompanion)
                {
                    return NameplateManager.NameplateCategory.SelfCompanion;
                }

                if (isPet)
                {
                    return NameplateManager.NameplateCategory.SelfPet;
                }

                return NameplateManager.NameplateCategory.Npc;
            }

            if (owner.IsPartyMember)
            {
                if (isCompanion)
                {
                    return NameplateManager.NameplateCategory.PartyCompanion;
                }

                if (isPet)
                {
                    return NameplateManager.NameplateCategory.PartyPet;
                }

                return NameplateManager.NameplateCategory.Npc;
            }

            if (owner.IsAllianceMember)
            {
                return isCompanion || isPet
                    ? NameplateManager.NameplateCategory.AlliancePet
                    : NameplateManager.NameplateCategory.Npc;
            }

            if (owner.IsFriend)
            {
                if (isCompanion)
                {
                    return NameplateManager.NameplateCategory.FriendCompanion;
                }

                if (isPet)
                {
                    return NameplateManager.NameplateCategory.FriendPet;
                }

                return NameplateManager.NameplateCategory.Npc;
            }

            if (isCompanion)
            {
                return NameplateManager.NameplateCategory.OtherCompanion;
            }

            if (isPet)
            {
                return NameplateManager.NameplateCategory.OtherPet;
            }

            return NameplateManager.NameplateCategory.Npc;
        }

        if (obj.OwnerId != 0)
        {
            if (obj.OwnerId == localPlayerId)
            {
                if (isCompanion)
                {
                    return NameplateManager.NameplateCategory.SelfCompanion;
                }

                if (isPet)
                {
                    return NameplateManager.NameplateCategory.SelfPet;
                }

                return NameplateManager.NameplateCategory.Npc;
            }

            if (isCompanion)
            {
                return NameplateManager.NameplateCategory.OtherCompanion;
            }

            if (isPet)
            {
                return NameplateManager.NameplateCategory.OtherPet;
            }
        }

        return NameplateManager.NameplateCategory.Npc;
    }

    private static NameplateManager.NameplateCategory ResolveEnemyCategory(TrackedObject obj)
    {
        return obj.EnemyState switch
        {
            EnemyNameplateState.Engaged => NameplateManager.NameplateCategory.EnemyEngaged,
            EnemyNameplateState.Claimed => NameplateManager.NameplateCategory.EnemyClaimed,
            EnemyNameplateState.Unclaimed => NameplateManager.NameplateCategory.EnemyUnclaimed,
            EnemyNameplateState.Feast => NameplateManager.NameplateCategory.EnemyFeast,
            EnemyNameplateState.FeastPet => NameplateManager.NameplateCategory.EnemyFeastPet,
            _ => NameplateManager.NameplateCategory.EnemyUnengaged,
        };
    }
}
