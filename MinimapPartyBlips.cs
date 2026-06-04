using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Party and alliance members on the minimap using the same map-texture placement as native markers.
/// </summary>
internal static class MinimapPartyBlips
{
    private const uint FallbackBlipColor = 0xFF66D4FF;

    public static int TryCollect(
        IPartyList partyList,
        IObjectTable objectTable,
        IClientState clientState,
        IDataManager dataManager,
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
                partyList,
                objectTable,
                clientState,
                dataManager,
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
        IPartyList partyList,
        IObjectTable objectTable,
        IClientState clientState,
        IDataManager dataManager,
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
        var playerEntityId = player.EntityId;
        var playerObjectId = player.GameObjectId;
        var playerPosition = player.Position;
        var collected = 0;
        var seenEntities = new HashSet<uint>();

        for (var i = 0; i < partyList.Length; i++)
        {
            var member = partyList[i];
            if (member is null)
            {
                continue;
            }

            if (member.EntityId == 0 || member.EntityId == playerEntityId)
            {
                continue;
            }

            if (!TryAddBlip(
                    member.ClassJob.RowId,
                    member.Position,
                    playerPosition,
                    offsetX,
                    offsetY,
                    sizeFactor,
                    visibleRangeYalms,
                    blipRadius,
                    contentHalf,
                    mapUvMin,
                    mapUvMax,
                    dataManager,
                    blips))
            {
                continue;
            }

            seenEntities.Add(member.EntityId);
            collected++;
        }

        // Duty Support / Trust NPC allies are often not exposed in IPartyList.
        // Fall back to friendly-like battle NPCs with valid class jobs in the same territory.
        foreach (var obj in objectTable)
        {
            if (obj.ObjectKind != ObjectKind.BattleNpc || obj is not ICharacter character)
            {
                continue;
            }

            if (obj.GameObjectId == playerObjectId || character.EntityId == 0 || character.EntityId == playerEntityId)
            {
                continue;
            }

            if (!seenEntities.Add(character.EntityId))
            {
                continue;
            }

            if (character.ClassJob.RowId == 0 || character.MaxHp == 0 || character.CurrentHp == 0)
            {
                continue;
            }

            if (!TryAddBlip(
                    character.ClassJob.RowId,
                    character.Position,
                    playerPosition,
                    offsetX,
                    offsetY,
                    sizeFactor,
                    visibleRangeYalms,
                    blipRadius,
                    contentHalf,
                    mapUvMin,
                    mapUvMax,
                    dataManager,
                    blips))
            {
                continue;
            }

            collected++;
        }

        return collected;
    }

    private static bool TryAddBlip(
        uint classJobId,
        Vector3 worldPosition,
        Vector3 playerPosition,
        int offsetX,
        int offsetY,
        uint sizeFactor,
        float visibleRangeYalms,
        float blipRadius,
        float contentHalf,
        Vector2 mapUvMin,
        Vector2 mapUvMax,
        IDataManager dataManager,
        List<MinimapBlip> blips)
    {
        if (!float.IsFinite(worldPosition.X) || !float.IsFinite(worldPosition.Z))
        {
            return false;
        }

        if (!MinimapMarkerPlacement.TryGetMarkerScreenOffset(
                worldPosition.X,
                worldPosition.Z,
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
            return false;
        }

        var color = FallbackBlipColor;
        if (classJobId != 0 && MinimapRoleColor.TryResolveArgb(dataManager, classJobId, out var roleColor))
        {
            color = roleColor;
        }

        blips.Add(new MinimapBlip
        {
            ScreenOffset = screenOffset,
            Kind = MinimapBlipKind.Party,
            Color = color,
            Radius = blipRadius,
        });
        return true;
    }
}
