using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Live gathering nodes from the object table (standing at / near an active node).
/// </summary>
internal static class MinimapGatheringPointMarkers
{
    private const int MaxGatheringPointsPerFrame = 24;
    private const float PositionDedupeGridYalms = 3f;

    public static int TryCollect(
        IObjectTable objectTable,
        IDataManager dataManager,
        float contentHalf,
        Vector2 mapUvMin,
        Vector2 mapUvMax,
        Vector3 playerPosition,
        int offsetX,
        int offsetY,
        uint sizeFactor,
        float visibleRangeYalms,
        float markerIconSize,
        MinimapMarkerIconCache iconCache,
        List<MinimapIconMarker> markers,
        int maxMarkers)
    {
        if (maxMarkers <= 0 || markers.Count >= maxMarkers)
        {
            return 0;
        }

        try
        {
            return TryCollectCore(
                objectTable,
                dataManager,
                contentHalf,
                mapUvMin,
                mapUvMax,
                playerPosition,
                offsetX,
                offsetY,
                sizeFactor,
                visibleRangeYalms,
                markerIconSize,
                iconCache,
                markers,
                maxMarkers);
        }
        catch
        {
            return 0;
        }
    }

    private static int TryCollectCore(
        IObjectTable objectTable,
        IDataManager dataManager,
        float contentHalf,
        Vector2 mapUvMin,
        Vector2 mapUvMax,
        Vector3 playerPosition,
        int offsetX,
        int offsetY,
        uint sizeFactor,
        float visibleRangeYalms,
        float markerIconSize,
        MinimapMarkerIconCache iconCache,
        List<MinimapIconMarker> markers,
        int maxMarkers)
    {
        var gatheringSheet = dataManager.GetExcelSheet<GatheringPoint>();
        if (gatheringSheet is null)
        {
            return 0;
        }

        var collected = 0;
        var seenCells = new HashSet<(int X, int Z)>();
        var maxWorldDistance = visibleRangeYalms + 8f;
        var maxWorldDistanceSq = maxWorldDistance * maxWorldDistance;

        foreach (var obj in objectTable)
        {
            if (collected >= MaxGatheringPointsPerFrame || markers.Count >= maxMarkers)
            {
                break;
            }

            if (obj.ObjectKind != ObjectKind.GatheringPoint || !obj.IsTargetable)
            {
                continue;
            }

            var delta = obj.Position - playerPosition;
            delta.Y = 0f;
            if (delta.LengthSquared() > maxWorldDistanceSq)
            {
                continue;
            }

            var cellX = (int)MathF.Round(obj.Position.X / PositionDedupeGridYalms);
            var cellZ = (int)MathF.Round(obj.Position.Z / PositionDedupeGridYalms);
            if (!seenCells.Add((cellX, cellZ)))
            {
                continue;
            }

            var pointRow = gatheringSheet.GetRow(obj.BaseId);
            var iconId = (uint)(pointRow.GatheringPointBase.ValueNullable?.GatheringType.ValueNullable?.IconMain ?? 0);
            if (iconId == 0)
            {
                continue;
            }

            if (MinimapMarkerPlacement.TryAddIconMarker(
                    obj.Position.X,
                    obj.Position.Z,
                    iconId,
                    playerPosition,
                    offsetX,
                    offsetY,
                    sizeFactor,
                    visibleRangeYalms,
                    contentHalf,
                    mapUvMin,
                    mapUvMax,
                    markerIconSize,
                    iconCache,
                    markers))
            {
                collected++;
            }
        }

        return collected;
    }
}
