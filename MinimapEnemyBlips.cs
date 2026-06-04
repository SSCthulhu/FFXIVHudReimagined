using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Hostile enemy blips that appear only while engaged by player/party.
/// </summary>
internal static class MinimapEnemyBlips
{
    public const uint EnemyBlipColor = 0xFF2020FF; // bright red (AARRGGBB -> ImGui ABGR)

    public static int TryCollect(
        IObjectTable objectTable,
        IPartyList partyList,
        ICharacter player,
        float contentHalf,
        Vector2 mapUvMin,
        Vector2 mapUvMax,
        int offsetX,
        int offsetY,
        uint sizeFactor,
        float visibleRangeYalms,
        float blipRadius,
        List<MinimapBlip> blips)
    {
        try
        {
            return TryCollectCore(
                objectTable,
                partyList,
                player,
                contentHalf,
                mapUvMin,
                mapUvMax,
                offsetX,
                offsetY,
                sizeFactor,
                visibleRangeYalms,
                blipRadius,
                blips);
        }
        catch
        {
            return 0;
        }
    }

    private static int TryCollectCore(
        IObjectTable objectTable,
        IPartyList partyList,
        ICharacter player,
        float contentHalf,
        Vector2 mapUvMin,
        Vector2 mapUvMax,
        int offsetX,
        int offsetY,
        uint sizeFactor,
        float visibleRangeYalms,
        float blipRadius,
        List<MinimapBlip> blips)
    {
        var engagedTargetIds = BuildEngagedTargetSet(objectTable, player, partyList);
        var playerObjectId = player.GameObjectId;
        var playerPosition = player.Position;
        var collected = 0;

        foreach (var obj in objectTable)
        {
            if (obj.ObjectKind != ObjectKind.BattleNpc || obj is not ICharacter enemy)
            {
                continue;
            }

            if (obj.GameObjectId == playerObjectId || enemy.CurrentHp == 0 || enemy.MaxHp == 0)
            {
                continue;
            }

            var isHostile = (enemy.StatusFlags & StatusFlags.Hostile) != 0;
            if (!isHostile)
            {
                continue;
            }

            // Engagement gate: enemy is actively in combat and has party/player as target.
            var enemyInCombat = (enemy.StatusFlags & StatusFlags.InCombat) != 0;
            if (!enemyInCombat || !engagedTargetIds.Contains(enemy.TargetObjectId))
            {
                continue;
            }

            var pos = enemy.Position;
            if (!float.IsFinite(pos.X) || !float.IsFinite(pos.Z))
            {
                continue;
            }

            if (!MinimapMarkerPlacement.TryGetMarkerScreenOffset(
                    pos.X,
                    pos.Z,
                    playerPosition,
                    offsetX,
                    offsetY,
                    sizeFactor,
                    visibleRangeYalms,
                    contentHalf,
                    mapUvMin,
                    mapUvMax,
                    out var screenOffset))
            {
                continue;
            }

            blips.Add(new MinimapBlip
            {
                ScreenOffset = screenOffset,
                Kind = MinimapBlipKind.Enemy,
                Color = EnemyBlipColor,
                Radius = blipRadius,
            });
            collected++;
        }

        return collected;
    }

    private static HashSet<ulong> BuildEngagedTargetSet(IObjectTable objectTable, ICharacter player, IPartyList partyList)
    {
        var ids = new HashSet<ulong>();
        ids.Add(player.GameObjectId);

        for (var i = 0; i < partyList.Length; i++)
        {
            var member = partyList[i];
            if (member is null || member.EntityId == 0)
            {
                continue;
            }

            ids.Add(member.GameObject?.GameObjectId ?? 0);
        }

        // Duty Support / Trust allies are battle NPCs with class jobs and positive HP.
        // They are often not represented in IPartyList, but enemies target them.
        var playerObjectId = player.GameObjectId;
        foreach (var obj in objectTable)
        {
            if (obj.ObjectKind != ObjectKind.BattleNpc || obj is not ICharacter ally)
            {
                continue;
            }

            if (obj.GameObjectId == playerObjectId || ally.ClassJob.RowId == 0 || ally.MaxHp == 0 || ally.CurrentHp == 0)
            {
                continue;
            }

            ids.Add(obj.GameObjectId);
        }

        ids.Remove(0);
        return ids;
    }
}
