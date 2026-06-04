using FFXIVClientStructs.FFXIV.Client.UI;

using FFXIVClientStructs.FFXIV.Component.GUI;



namespace FFXIVHudPlugin;



/// <summary>

/// Hides the native minimap visuals while _NaviMap keeps updating map/marker data.

/// </summary>

internal static class NativeMinimapVisibility

{

    public const string AddonName = "_NaviMap";

    private const int IconsRootNodeIndex = 2;



    private static bool? defaultVisible;

    private static byte? defaultAlpha;

    private static bool? defaultIconsVisible;

    private static bool? defaultMapImageVisible;

    private static bool? defaultMaskVisible;



    public static unsafe void Apply(bool hideNativeMinimap)

    {

        var stage = AtkStage.Instance();

        if (stage is null)

        {

            return;

        }



        var addon = (AddonNaviMap*)stage->RaptureAtkUnitManager->GetAddonByName(AddonName, 1);

        if (addon is null)

        {

            return;

        }



        CaptureDefaultsIfNeeded(addon);



        if (hideNativeMinimap)

        {

            // Keep visible with alpha 0 so map/marker data and player indicator assets keep updating.
            addon->IsVisible = true;

            addon->SetAlpha(0);

            SetIconsSubtreeVisible(addon, false);

            SetResNodeVisible((AtkResNode*)addon->MapImage, false);

            SetResNodeVisible((AtkResNode*)addon->Mask, false);

            SetResNodeVisible(addon->NaviMap.CompassFrame, false);

            return;

        }



        addon->IsVisible = defaultVisible ?? true;

        addon->SetAlpha(defaultAlpha ?? (byte)255);

        SetIconsSubtreeVisible(addon, defaultIconsVisible ?? true);

        SetResNodeVisible((AtkResNode*)addon->MapImage, defaultMapImageVisible ?? true);

        SetResNodeVisible((AtkResNode*)addon->Mask, defaultMaskVisible ?? true);

        SetResNodeVisible(addon->NaviMap.CompassFrame, true);

    }



    private static unsafe void CaptureDefaultsIfNeeded(AddonNaviMap* addon)

    {

        if (!defaultVisible.HasValue)

        {

            defaultVisible = addon->IsVisible;

            defaultAlpha = addon->Alpha;

            defaultIconsVisible = IsIconsRootVisible(addon);

            defaultMapImageVisible = IsResNodeVisible((AtkResNode*)addon->MapImage);

            defaultMaskVisible = IsResNodeVisible((AtkResNode*)addon->Mask);

        }

    }



    private static unsafe void SetIconsSubtreeVisible(AddonNaviMap* addon, bool visible)

    {

        SetIconsRootVisible(addon, visible);



        var iconsRoot = (AtkComponentNode*)addon->UldManager.NodeList[IconsRootNodeIndex];

        if (iconsRoot is null)

        {

            return;

        }



        var component = iconsRoot->Component;

        if (component is null)

        {

            return;

        }



        ref var uld = ref component->UldManager;

        for (var i = 0; i < uld.NodeListCount; i++)

        {

            var node = uld.NodeList[i];

            if (node is null)

            {

                continue;

            }



            SetResNodeVisible(node, visible);

        }

    }



    private static unsafe void SetIconsRootVisible(AddonNaviMap* addon, bool visible)

    {

        var iconsRoot = (AtkComponentNode*)addon->UldManager.NodeList[IconsRootNodeIndex];

        if (iconsRoot is null)

        {

            return;

        }



        SetResNodeVisible((AtkResNode*)iconsRoot, visible);

    }



    private static unsafe bool IsIconsRootVisible(AddonNaviMap* addon)

    {

        var iconsRoot = (AtkComponentNode*)addon->UldManager.NodeList[IconsRootNodeIndex];

        if (iconsRoot is null)

        {

            return true;

        }



        return iconsRoot->AtkResNode.IsVisible();

    }



    private static unsafe void SetResNodeVisible(AtkResNode* node, bool visible)

    {

        if (node is null)

        {

            return;

        }



        if (visible)

        {

            node->NodeFlags |= NodeFlags.Visible;

        }

        else

        {

            node->NodeFlags &= ~NodeFlags.Visible;

        }

    }



    private static unsafe bool IsResNodeVisible(AtkResNode* node)

    {

        return node is not null && node->IsVisible();

    }

}


