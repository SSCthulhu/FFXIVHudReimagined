using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Player-placed map flag from <see cref="AgentMap.FlagMapMarkers"/>.
/// </summary>
internal static class MinimapFlagMarkers
{
    /// <summary>Default map flag icon when the marker record has IconId 0.</summary>
    private const uint DefaultFlagIconId = 0xEC91;

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
                markers);
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
        List<MinimapIconMarker> markers)
    {
        var agentMap = AgentMap.Instance();
        if (agentMap is null || agentMap->CurrentMapId == 0 || agentMap->FlagMarkerCount == 0)
        {
            return 0;
        }

        ref readonly var flag = ref agentMap->FlagMapMarkers[0];
        if (flag.MapId != agentMap->CurrentMapId)
        {
            return 0;
        }

        var iconId = flag.MapMarker.IconId;
        if (iconId == 0)
        {
            iconId = DefaultFlagIconId;
        }

        return MinimapMarkerPlacement.TryAddIconMarker(
            flag.XFloat,
            flag.YFloat,
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
            markers)
            ? 1
            : 0;
    }
}
