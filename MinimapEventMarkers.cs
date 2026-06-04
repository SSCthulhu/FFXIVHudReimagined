using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Quest and sequential event markers from <see cref="AgentMap.EventMarkers"/> (FATEs use <see cref="FateManager"/>).
/// </summary>
internal static class MinimapEventMarkers
{
    private const int MaxEventMarkersToScan = 96;
    private const uint PlayerMarkerIconId = 60443;

    public static unsafe int TryCollect(
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

    private static unsafe int TryCollectCore(
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
        var agentMap = AgentMap.Instance();
        if (agentMap is null || agentMap->CurrentMapId == 0)
        {
            return 0;
        }

        ref var eventMarkers = ref agentMap->EventMarkers;
        var count = (int)Math.Min(eventMarkers.LongCount, MaxEventMarkersToScan);
        if (count <= 0 || eventMarkers.First == null)
        {
            return 0;
        }

        var currentMapId = agentMap->CurrentMapId;
        var currentTerritoryId = (ushort)agentMap->CurrentTerritoryId;
        var collected = 0;
        var seen = new HashSet<(uint IconId, int X, int Z)>();

        for (var i = 0; i < count && markers.Count < maxMarkers; i++)
        {
            var marker = eventMarkers.First[i];
            var iconId = marker.IconId;
            if (iconId == 0 || iconId == PlayerMarkerIconId)
            {
                continue;
            }

            if (marker.MapId != 0 && marker.MapId != currentMapId)
            {
                continue;
            }

            if (marker.TerritoryTypeId != 0 && marker.TerritoryTypeId != currentTerritoryId)
            {
                continue;
            }

            var worldX = marker.Position.X;
            var worldZ = marker.Position.Z;
            if (!float.IsFinite(worldX) || !float.IsFinite(worldZ))
            {
                continue;
            }

            var cellX = (int)MathF.Round(worldX);
            var cellZ = (int)MathF.Round(worldZ);
            if (!seen.Add((iconId, cellX, cellZ)))
            {
                continue;
            }

            if (MinimapMarkerPlacement.TryAddIconMarker(
                    worldX,
                    worldZ,
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
