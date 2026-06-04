using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Loads the native minimap player pin and vision cone textures from _NaviMap.
/// </summary>
public sealed class MinimapPlayerIndicatorCache
{
    private readonly ITextureProvider textureProvider;
    private MinimapPlayerIndicatorAssets cachedAssets;
    private string cachedConePath = string.Empty;
    private string cachedPinPath = string.Empty;
    private uint cachedPinIconId;

    public MinimapPlayerIndicatorCache(ITextureProvider textureProvider)
    {
        this.textureProvider = textureProvider;
    }

    public MinimapPlayerIndicatorAssets GetAssets()
    {
        if (this.TryRefreshFromNativeAddon(out var assets))
        {
            this.cachedAssets = assets;
            return assets;
        }

        if (this.TryLoadFromCachedPaths(out assets))
        {
            this.cachedAssets = assets;
            return assets;
        }

        return this.cachedAssets;
    }

    private unsafe bool TryRefreshFromNativeAddon(out MinimapPlayerIndicatorAssets assets)
    {
        assets = default;
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

        ref var naviMap = ref addon->NaviMap;
        var coneNode = naviMap.PlayerCone;
        if (coneNode is null && addon->PlayerCone is not null)
        {
            coneNode = addon->PlayerCone->GetAsAtkImageNode();
        }

        if (coneNode is null)
        {
            return false;
        }

        if (!this.TryResolveCone(coneNode, out var coneTexture, out var coneUvMin, out var coneUvMax, out var coneSize, out var conePath))
        {
            return false;
        }

        if (!this.TryResolvePin(naviMap.PlayerPin, out var pinTexture, out var pinUvMin, out var pinUvMax, out var pinSize, out var pinPath, out var pinIconId))
        {
            return false;
        }

        this.cachedConePath = conePath;
        this.cachedPinPath = pinPath;
        this.cachedPinIconId = pinIconId;

        var nativeMapSize = naviMap.Width > 0 ? naviMap.Width : (ushort)200;
        assets = new MinimapPlayerIndicatorAssets
        {
            IsValid = true,
            PinTexture = pinTexture,
            ConeTexture = coneTexture,
            PinUvMin = pinUvMin,
            PinUvMax = pinUvMax,
            ConeUvMin = coneUvMin,
            ConeUvMax = coneUvMax,
            PinSize = pinSize,
            ConeSize = coneSize,
            NativeMapSize = nativeMapSize,
            NativeConeRotation = naviMap.PlayerConeRotation,
            NativePinRotation = naviMap.PlayerPinRotation,
        };
        return true;
    }

    private bool TryLoadFromCachedPaths(out MinimapPlayerIndicatorAssets assets)
    {
        assets = default;
        ISharedImmediateTexture? coneTexture = null;
        ISharedImmediateTexture? pinTexture = null;

        if (!string.IsNullOrWhiteSpace(this.cachedConePath))
        {
            coneTexture = this.textureProvider.GetFromGame(this.cachedConePath);
        }

        if (!string.IsNullOrWhiteSpace(this.cachedPinPath))
        {
            pinTexture = this.textureProvider.GetFromGame(this.cachedPinPath);
        }
        else if (this.cachedPinIconId != 0)
        {
            pinTexture = this.textureProvider.GetFromGameIcon(new GameIconLookup(this.cachedPinIconId));
        }

        if (coneTexture is null || pinTexture is null)
        {
            return false;
        }

        assets = this.cachedAssets with
        {
            IsValid = true,
            ConeTexture = coneTexture,
            PinTexture = pinTexture,
        };
        return assets.IsValid;
    }

    private static unsafe AtkImageNode* ResolvePinImageNode(AtkComponentNode* pinComponentNode)
    {
        if (pinComponentNode is null)
        {
            return null;
        }

        var iconComponent = pinComponentNode->GetAsAtkComponentIcon();
        if (iconComponent is not null && iconComponent->IconImage is not null)
        {
            return iconComponent->IconImage;
        }

        return pinComponentNode->GetAsAtkImageNode();
    }

    private unsafe bool TryResolveCone(
        AtkImageNode* imageNode,
        out ISharedImmediateTexture? texture,
        out Vector2 uvMin,
        out Vector2 uvMax,
        out Vector2 size,
        out string texturePath)
    {
        if (TryResolveImageNode(imageNode, out texture, out uvMin, out uvMax, out size, out texturePath))
        {
            return true;
        }

        texture = null;
        uvMin = Vector2.Zero;
        uvMax = Vector2.One;
        size = Vector2.Zero;
        texturePath = string.Empty;
        return false;
    }

    private unsafe bool TryResolvePin(
        AtkComponentNode* pinComponentNode,
        out ISharedImmediateTexture? texture,
        out Vector2 uvMin,
        out Vector2 uvMax,
        out Vector2 size,
        out string texturePath,
        out uint iconId)
    {
        texturePath = string.Empty;
        iconId = 0;

        var pinImageNode = ResolvePinImageNode(pinComponentNode);
        if (pinImageNode is not null && TryResolveImageNode(pinImageNode, out texture, out uvMin, out uvMax, out size, out texturePath))
        {
            return true;
        }

        if (pinComponentNode is not null)
        {
            var iconComponent = pinComponentNode->GetAsAtkComponentIcon();
            if (iconComponent is not null && iconComponent->IconId != 0)
            {
                iconId = iconComponent->IconId;
                texture = this.textureProvider.GetFromGameIcon(new GameIconLookup(iconId));
                if (texture is not null)
                {
                    var resNode = (AtkResNode*)pinComponentNode;
                    size = new Vector2(
                        resNode->Width * Math.Max(resNode->ScaleX, 0.01f),
                        resNode->Height * Math.Max(resNode->ScaleY, 0.01f));
                    uvMin = Vector2.Zero;
                    uvMax = Vector2.One;
                    return true;
                }
            }
        }

        texture = null;
        uvMin = Vector2.Zero;
        uvMax = Vector2.One;
        size = Vector2.Zero;
        return false;
    }

    private unsafe bool TryResolveImageNode(
        AtkImageNode* imageNode,
        out ISharedImmediateTexture? texture,
        out Vector2 uvMin,
        out Vector2 uvMax,
        out Vector2 size,
        out string texturePath)
    {
        texture = null;
        uvMin = Vector2.Zero;
        uvMax = Vector2.One;
        size = Vector2.Zero;
        texturePath = string.Empty;

        if (imageNode is null)
        {
            return false;
        }

        var resNode = (AtkResNode*)imageNode;
        size = new Vector2(
            resNode->Width * Math.Max(resNode->ScaleX, 0.01f),
            resNode->Height * Math.Max(resNode->ScaleY, 0.01f));

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

        ref var atkTexture = ref part.UldAsset->AtkTexture;
        var resource = atkTexture.Resource;
        if (resource is null || resource->TexFileResourceHandle is null)
        {
            return false;
        }

        texturePath = NormalizeGamePath(resource->TexFileResourceHandle->FileName.ToString());
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return false;
        }

        texture = this.textureProvider.GetFromGame(texturePath);
        if (texture is null)
        {
            return false;
        }

        var texWidth = Math.Max(atkTexture.GetTextureWidth(), 1u);
        var texHeight = Math.Max(atkTexture.GetTextureHeight(), 1u);
        uvMin = new Vector2(part.U / (float)texWidth, part.V / (float)texHeight);
        uvMax = new Vector2((part.U + part.Width) / (float)texWidth, (part.V + part.Height) / (float)texHeight);
        return true;
    }

    private static string NormalizeGamePath(string path)
    {
        var trimmed = path.Trim().Replace('\\', '/');
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return trimmed.TrimStart('/');
    }
}
