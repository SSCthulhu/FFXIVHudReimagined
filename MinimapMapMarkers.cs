using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Map markers from <see cref="AgentMap.MapMarkers"/> (separate from MiniMapMarkers/EventMarkers).
/// Some quest objectives and map-linked markers only appear in this container.
/// </summary>
internal static class MinimapMapMarkers
{
    private const uint PlayerMarkerIconId = 60443;

    public static int TryCollect(
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

        var markerCount = Math.Min(agentMap->MapMarkerCount, agentMap->MapMarkers.Length);
        if (markerCount <= 0)
        {
            return 0;
        }

        var seen = new HashSet<(uint IconId, int X, int Z)>();
        var collected = 0;

        for (var i = 0; i < markerCount && markers.Count < maxMarkers; i++)
        {
            ref readonly var entry = ref agentMap->MapMarkers[i];
            var iconId = entry.MapMarker.IconId;
            if (iconId == 0 || iconId == PlayerMarkerIconId)
            {
                continue;
            }

            var worldX = entry.MapMarker.X / 16f;
            var worldZ = entry.MapMarker.Y / 16f;
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
