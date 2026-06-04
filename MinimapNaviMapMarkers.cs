using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Draws markers from <see cref="AgentMap.MiniMapMarkers"/> using map-texture deltas
/// converted through the same UV window as the scrolling minimap image.
/// </summary>
internal static class MinimapNaviMapMarkers
{
    private const uint PlayerMarkerIconId = 60443;

    public static unsafe bool IsAddonLoaded()
    {
        return AgentMap.Instance() is not null;
    }

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
        List<MinimapIconMarker> markers)
    {
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
        if (agentMap is null || agentMap->CurrentMapId == 0)
        {
            return 0;
        }

        var playerTex = MinimapMapMath.WorldToMapTextureCoords(playerPosition, offsetX, offsetY, sizeFactor);
        var maxTexDistance = visibleRangeYalms * (sizeFactor / 100f);
        var markerCount = Math.Min(agentMap->MiniMapMarkerCount, agentMap->MiniMapMarkers.Length);
        markerCount = Math.Min(markerCount, MinimapLayout.MaxNativeMarkersPerFrame);
        var collected = 0;

        for (var i = 0; i < markerCount; i++)
        {
            ref readonly var entry = ref agentMap->MiniMapMarkers[i];
            var iconId = entry.MapMarker.IconId;
            if (iconId == 0 || iconId == PlayerMarkerIconId)
            {
                continue;
            }

            var markerWorldX = entry.MapMarker.X / 16f;
            var markerWorldZ = entry.MapMarker.Y / 16f;
            if (!float.IsFinite(markerWorldX) || !float.IsFinite(markerWorldZ))
            {
                continue;
            }

            var markerTex = MinimapMapMath.WorldToMapTextureCoords(
                new Vector3(markerWorldX, 0f, markerWorldZ),
                offsetX,
                offsetY,
                sizeFactor);
            var texDelta = markerTex - playerTex;
            if (texDelta.Length() > maxTexDistance)
            {
                continue;
            }

            if (!iconCache.TryGetDrawableIcon(iconId, out var texture))
            {
                continue;
            }

            markers.Add(new MinimapIconMarker
            {
                UsesNativeScreenOffset = true,
                ScreenOffset = MinimapMapMath.MapTextureDeltaToScreenOffset(
                    texDelta,
                    mapUvMin,
                    mapUvMax,
                    contentHalf),
                IconId = iconId,
                Size = markerIconSize,
                Texture = texture,
            });
            collected++;
        }

        return collected;
    }
}
