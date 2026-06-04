using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFXIVHudPlugin;

/// <summary>
/// Resolves the live map image texture from _NaviMap when AgentMap paths are empty or not yet loaded.
/// </summary>
internal static class MinimapNativeMapTexture
{
    public static unsafe bool TryGetMapImagePath(out string texturePath, out bool addonLoaded)
    {
        texturePath = string.Empty;
        addonLoaded = false;
        try
        {
            var stage = AtkStage.Instance();
            if (stage is null)
            {
                return false;
            }

            var addon = (AddonNaviMap*)stage->RaptureAtkUnitManager->GetAddonByName(NativeMinimapVisibility.AddonName, 1);
            if (addon is null)
            {
                return false;
            }

            addonLoaded = true;
            if (addon->UldManager.LoadedState != AtkLoadState.Loaded || addon->MapImage is null)
            {
                return false;
            }

            return TryResolveImagePath(addon->MapImage, out texturePath);
        }
        catch
        {
            return false;
        }
    }

    public static unsafe bool TryGetTexture(ITextureProvider textureProvider, out ISharedImmediateTexture? texture)
    {
        texture = null;
        try
        {
            return TryGetTextureCore(textureProvider, out texture);
        }
        catch
        {
            return false;
        }
    }

    private static unsafe bool TryGetTextureCore(ITextureProvider textureProvider, out ISharedImmediateTexture? texture)
    {
        texture = null;
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return false;
        }

        var addon = (AddonNaviMap*)stage->RaptureAtkUnitManager->GetAddonByName(NativeMinimapVisibility.AddonName, 1);
        if (addon is null || addon->UldManager.LoadedState != AtkLoadState.Loaded)
        {
            return false;
        }

        var mapImage = addon->MapImage;
        if (mapImage is null)
        {
            return false;
        }

        if (!TryResolveImagePath(mapImage, out var texturePath) || string.IsNullOrWhiteSpace(texturePath))
        {
            return false;
        }

        texture = textureProvider.GetFromGame(texturePath);
        if (texture is null)
        {
            return false;
        }

        var wrap = texture.GetWrapOrEmpty();
        return wrap.Handle != 0 && wrap.Width > 0 && wrap.Height > 0;
    }

    private static unsafe bool TryResolveImagePath(AtkImageNode* imageNode, out string texturePath)
    {
        texturePath = string.Empty;
        if (imageNode is null)
        {
            return false;
        }

        var partsList = imageNode->PartsList;
        if (partsList is null || partsList->PartCount == 0)
        {
            return false;
        }

        var partId = imageNode->PartId;
        if (partId >= partsList->PartCount)
        {
            return false;
        }

        ref var part = ref partsList->Parts[partId];
        if (part.UldAsset is null)
        {
            return false;
        }

        var resource = part.UldAsset->AtkTexture.Resource;
        if (resource is null || resource->TexFileResourceHandle is null)
        {
            return false;
        }

        texturePath = resource->TexFileResourceHandle->FileName.ToString();
        return texturePath.Length > 0;
    }
}
