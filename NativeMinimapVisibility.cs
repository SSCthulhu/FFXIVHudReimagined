using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFXIVHudPlugin;

/// <summary>
/// Hides the native minimap when the plugin draws its own ImGui minimap.
/// </summary>
internal static class NativeMinimapVisibility
{
    public const string AddonName = "_NaviMap";

    private static bool? defaultVisible;

    public static unsafe void Apply(bool hideNativeMinimap)
    {
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return;
        }

        var addon = stage->RaptureAtkUnitManager->GetAddonByName(AddonName, 1);
        if (addon is null)
        {
            return;
        }

        CaptureDefaultsIfNeeded(addon);
        addon->IsVisible = hideNativeMinimap ? false : defaultVisible ?? true;
    }

    private static unsafe void CaptureDefaultsIfNeeded(AtkUnitBase* addon)
    {
        if (!defaultVisible.HasValue)
        {
            defaultVisible = addon->IsVisible;
        }
    }
}
