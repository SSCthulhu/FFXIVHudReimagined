using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Active FATEs from <see cref="FateManager"/> (not AgentMap.EventMarkers, which is mostly quests/sequential events).
/// </summary>
internal static class MinimapFateMarkers
{
    private const int MaxFatesToScan = 32;
    private const int MaxFateAreasPerFrame = 8;
    private const float PixelsPerYalmScale = 0.86f;
    private const float MinAreaRadiusPixels = 6f;
    private const float MaxAreaRadiusPixelsScale = 1.15f;

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
        List<MinimapFateArea> fateAreas,
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
                fateAreas,
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
        List<MinimapFateArea> fateAreas,
        int maxMarkers)
    {
        var agentMap = AgentMap.Instance();
        if (agentMap is null || agentMap->CurrentMapId == 0)
        {
            return 0;
        }

        var fateManager = FateManager.Instance();
        if (fateManager is null)
        {
            return 0;
        }

        ref var fates = ref fateManager->Fates;
        var count = (int)Math.Min(fates.LongCount, MaxFatesToScan);
        if (count <= 0 || fates.First == null)
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var collected = 0;
        var pixelsPerYalm = (contentHalf * PixelsPerYalmScale) / Math.Max(visibleRangeYalms, 1f);
        var maxAreaRadiusPixels = contentHalf * MaxAreaRadiusPixelsScale;

        for (var i = 0; i < count && markers.Count < maxMarkers; i++)
        {
            var fate = fates.First[i].Value;
            if (fate is null || !IsFateActive(fate, now))
            {
                continue;
            }

            if (!TryResolveFateWorldState(fate, out var worldX, out var worldZ, out var worldRadius))
            {
                continue;
            }

            if (!MinimapMarkerPlacement.TryGetMarkerScreenOffset(
                    worldX,
                    worldZ,
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

            if (fateAreas.Count < MaxFateAreasPerFrame && worldRadius > 0.5f)
            {
                var radiusPixels = Math.Clamp(
                    worldRadius * pixelsPerYalm,
                    MinAreaRadiusPixels,
                    maxAreaRadiusPixels);
                fateAreas.Add(new MinimapFateArea
                {
                    ScreenOffset = screenOffset,
                    RadiusPixels = radiusPixels,
                });
            }

            var iconId = fate->MapIconId != 0 ? fate->MapIconId : fate->IconId;
            if (iconId == 0)
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

    private static unsafe bool TryResolveFateWorldState(
        FateContext* fate,
        out float worldX,
        out float worldZ,
        out float worldRadius)
    {
        worldX = fate->Location.X;
        worldZ = fate->Location.Z;
        worldRadius = fate->Radius;

        if (!float.IsFinite(worldX) || !float.IsFinite(worldZ) || !float.IsFinite(worldRadius))
        {
            worldX = 0f;
            worldZ = 0f;
            worldRadius = 0f;
            return false;
        }

        if (MathF.Abs(worldX) < 0.01f && MathF.Abs(worldZ) < 0.01f)
        {
            Vector3 position = default;
            Vector3 radiusVector = default;
            if (!fate->TryGetPositionAndRadius(&position, &radiusVector))
            {
                return false;
            }

            worldX = position.X;
            worldZ = position.Z;
            if (worldRadius < 0.5f)
            {
                worldRadius = MathF.Max(radiusVector.X, MathF.Max(radiusVector.Y, radiusVector.Z));
            }
        }

        return float.IsFinite(worldX) && float.IsFinite(worldZ) && worldRadius > 0.5f;
    }

    private static unsafe bool IsFateActive(FateContext* fate, long nowUnix)
    {
        if (fate->State is FateState.Ended or FateState.Failed)
        {
            return false;
        }

        var startTime = fate->StartTimeEpoch;
        var duration = fate->Duration;
        if (startTime <= 0 || duration <= 0)
        {
            return fate->State is FateState.Running or FateState.Preparing or FateState.Ending;
        }

        var endTime = startTime + duration;
        return startTime <= nowUnix && nowUnix <= endTime;
    }
}
