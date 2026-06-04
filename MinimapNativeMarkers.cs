using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Step 1: read only <see cref="AgentMap.MiniMapMarkers"/> (same list the native minimap uses).
/// </summary>
internal static class MinimapNativeMarkers
{
    public static unsafe void TryCollect(
        Vector3 playerPosition,
        float visibleRangeYalms,
        int offsetX,
        int offsetY,
        uint sizeFactor,
        MinimapMarkerIconCache iconCache,
        List<MinimapIconMarker> markers)
    {
        var agentMap = AgentMap.Instance();
        if (agentMap is null || agentMap->CurrentMapId == 0)
        {
            return;
        }

        var playerTex = MinimapMapMath.WorldToMapTextureCoords(playerPosition, offsetX, offsetY, sizeFactor);
        var maxTexDistance = visibleRangeYalms * (sizeFactor / 100f);

        var markerCount = Math.Min(agentMap->MiniMapMarkerCount, agentMap->MiniMapMarkers.Length);
        markerCount = Math.Min(markerCount, MinimapLayout.MaxNativeMarkersPerFrame);

        for (var i = 0; i < markerCount; i++)
        {
            ref readonly var entry = ref agentMap->MiniMapMarkers[i];
            var iconId = entry.MapMarker.IconId;
            if (iconId == 0)
            {
                continue;
            }

            var markerWorld = new Vector3(entry.MapMarker.X / 16f, 0f, entry.MapMarker.Y / 16f);
            if (!IsPlausibleWorldCoordinate(markerWorld.X, markerWorld.Z, playerPosition, visibleRangeYalms))
            {
                continue;
            }

            var markerTex = MinimapMapMath.WorldToMapTextureCoords(markerWorld, offsetX, offsetY, sizeFactor);
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
                MapTextureDelta = texDelta,
                IconId = iconId,
                Size = MinimapLayout.NativeMarkerIconSize,
                Texture = texture,
            });
        }
    }

    private static bool IsPlausibleWorldCoordinate(
        float worldX,
        float worldZ,
        Vector3 playerPosition,
        float visibleRangeYalms)
    {
        if (!float.IsFinite(worldX) || !float.IsFinite(worldZ))
        {
            return false;
        }

        var maxDelta = Math.Max(visibleRangeYalms * 4f, 400f);
        return MathF.Abs(worldX - playerPosition.X) <= maxDelta &&
               MathF.Abs(worldZ - playerPosition.Z) <= maxDelta;
    }
}
