using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFXIVHudPlugin;

/// <summary>
/// Keeps the native _NaviMap north-lock state aligned with plugin config so map rotation math stays in sync.
/// </summary>
internal static class MinimapNativeNorthLock
{
    public static unsafe void Apply(bool northLocked)
    {
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return;
        }

        var addon = (AddonNaviMap*)stage->RaptureAtkUnitManager->GetAddonByName(NativeMinimapVisibility.AddonName, 1);
        if (addon is null)
        {
            return;
        }

        ref var naviMap = ref addon->NaviMap;
        if (naviMap.NorthLockedUp != northLocked)
        {
            naviMap.NorthLockedUp = northLocked;
        }

        var lockNorthCheckbox = addon->LockNorthCheckbox;
        if (lockNorthCheckbox is not null && lockNorthCheckbox->IsChecked != northLocked)
        {
            lockNorthCheckbox->SetChecked(northLocked);
        }
    }
}
