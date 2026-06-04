using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFXIVHudPlugin;

/// <summary>
/// Reads live minimap state from the native _NaviMap addon (still updated while hidden).
/// </summary>
internal static class MinimapNativeFrame
{
    public readonly struct Transform
    {
        public float Rotation { get; init; }
        public float ScaleX { get; init; }
        public float ScaleY { get; init; }
        public bool NorthLockedUp { get; init; }
        public float PlayerConeRotation { get; init; }
    }

    public static unsafe bool TryGetMapImageTransform(out Transform transform)
    {
        transform = default;
        if (!TryGetAddon(out var addon))
        {
            return false;
        }

        var mapImage = addon->MapImage;
        if (mapImage is null)
        {
            return false;
        }

        var node = (AtkResNode*)mapImage;
        transform = new Transform
        {
            Rotation = node->Rotation,
            ScaleX = node->ScaleX,
            ScaleY = node->ScaleY,
            NorthLockedUp = addon->NaviMap.NorthLockedUp,
            PlayerConeRotation = addon->NaviMap.PlayerConeRotation,
        };
        return true;
    }

    private static unsafe bool TryGetAddon(out AddonNaviMap* addon)
    {
        addon = null;
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return false;
        }

        addon = (AddonNaviMap*)stage->RaptureAtkUnitManager->GetAddonByName(NativeMinimapVisibility.AddonName, 1);
        return addon is not null;
    }
}
