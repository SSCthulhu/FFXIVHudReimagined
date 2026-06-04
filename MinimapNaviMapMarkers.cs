using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Reads marker positions from <see cref="Atk2DNaviMap.NaviMapMarkers"/> (game-maintained array).
/// Avoids walking the _NaviMap UI tree, which can crash when textures or nodes are mid-update.
/// </summary>
internal static class MinimapNaviMapMarkers
{
    private const uint PlayerMarkerIconId = 60443;

    // Fallback when PlayerPin is not available (native minimap center).
    private const float DefaultPlayerMapX = 72f;
    private const float DefaultPlayerMapY = 72f;

    public static bool TryCollect(
        float contentHalf,
        MinimapMarkerIconCache iconCache,
        List<MinimapIconMarker> markers)
    {
        try
        {
            return TryCollectCore(contentHalf, iconCache, markers);
        }
        catch
        {
            return false;
        }
    }

    private static unsafe bool TryCollectCore(
        float contentHalf,
        MinimapMarkerIconCache iconCache,
        List<MinimapIconMarker> markers)
    {
        var addon = TryGetAddon();
        if (addon is null || addon->UldManager.LoadedState != AtkLoadState.Loaded)
        {
            return false;
        }

        ref var naviMap = ref addon->NaviMap;
        var markersSpan = naviMap.NaviMapMarkers;
        if (markersSpan.IsEmpty)
        {
            return false;
        }

        var playerMapPosition = TryGetPlayerMapPosition(naviMap.PlayerPin, out var pinPosition)
            ? pinPosition
            : new Vector2(DefaultPlayerMapX, DefaultPlayerMapY);

        var nativeHalf = Math.Max(naviMap.Width, naviMap.Height) * 0.5f;
        if (nativeHalf < 1f)
        {
            nativeHalf = 100f;
        }

        var markerScale = naviMap.MarkerPositionScaling > 0.001f ? naviMap.MarkerPositionScaling : 1f;
        var screenScale = (contentHalf * MinimapRenderer.BlipClipScaleForMarkers) / nativeHalf;
        var limit = Math.Min(markersSpan.Length, MinimapLayout.MaxNativeMarkersPerFrame);
        var collected = false;

        for (var i = 0; i < limit; i++)
        {
            ref readonly var entry = ref markersSpan[i];
            var iconId = entry.IconId != 0 ? entry.IconId : entry.SecondaryIconId;
            if (iconId == 0 || iconId == PlayerMarkerIconId)
            {
                continue;
            }

            if (entry.X == 0 && entry.Y == 0)
            {
                continue;
            }

            if (!iconCache.TryGetDrawableIcon(iconId, out var texture))
            {
                continue;
            }

            var iconMapPosition = new Vector2(entry.X * markerScale, -(entry.Y * markerScale));
            var mapDelta = iconMapPosition - playerMapPosition;
            markers.Add(new MinimapIconMarker
            {
                UsesNativeScreenOffset = true,
                ScreenOffset = mapDelta * screenScale,
                IconId = iconId,
                Size = MinimapLayout.NativeMarkerIconSize,
                Texture = texture,
            });
            collected = true;
        }

        return collected;
    }

    private static unsafe bool TryGetPlayerMapPosition(AtkComponentNode* playerPin, out Vector2 position)
    {
        position = default;
        if (playerPin is null)
        {
            return false;
        }

        ref readonly var resNode = ref playerPin->AtkResNode;
        position = new Vector2(resNode.X, -resNode.Y);
        return true;
    }

    private static unsafe AddonNaviMap* TryGetAddon()
    {
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return null;
        }

        return (AddonNaviMap*)stage->RaptureAtkUnitManager->GetAddonByName(NativeMinimapVisibility.AddonName, 1);
    }
}
