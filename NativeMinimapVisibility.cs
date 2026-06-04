using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFXIVHudPlugin;

/// <summary>
/// Shows or hides _NaviMap the same way as other native HUD addons (AtkUnitBase.IsVisible).
/// </summary>
internal static class NativeMinimapVisibility
{
    public const string AddonName = "_NaviMap";

    public static unsafe void Apply(bool hideNativeMinimap)
    {
        SetVisible(!hideNativeMinimap);
    }

    public static unsafe void SetVisible(bool visible)
    {
        var addon = TryGetAddon();
        if (addon is null)
        {
            return;
        }

        addon->IsVisible = visible;
        if (!visible)
        {
            return;
        }

        if (addon->Alpha < 255)
        {
            addon->SetAlpha(255);
        }
    }

    public static unsafe AddonNaviMap* TryGetAddon()
    {
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return null;
        }

        return (AddonNaviMap*)stage->RaptureAtkUnitManager->GetAddonByName(AddonName, 1);
    }
}
