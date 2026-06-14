using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    internal sealed class MinimapPlayerIndicatorCache
    {
        private MinimapPlayerIndicatorAssets _cachedAssets;
        private string _cachedConePath = string.Empty;
        private string _cachedPinPath = string.Empty;
        private uint _cachedPinIconId;

        public MinimapPlayerIndicatorAssets GetAssets()
        {
            if (TryRefreshFromNativeAddon(out var assets))
            {
                _cachedAssets = assets;
                return assets;
            }

            if (TryLoadFromCachedPaths(out assets))
            {
                _cachedAssets = assets;
                return assets;
            }

            return _cachedAssets;
        }

        private unsafe bool TryRefreshFromNativeAddon(out MinimapPlayerIndicatorAssets assets)
        {
            assets = default;
            var addon = NativeMinimapVisibility.TryGetAddon();
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

            if (!TryResolveImageNode(coneNode, out var coneTexture, out var coneUvMin, out var coneUvMax, out var coneSize, out var conePath))
            {
                return false;
            }

            var pinImageNode = ResolvePinImageNode(naviMap.PlayerPin);
            if (!TryResolveImageNode(pinImageNode, out var pinTexture, out var pinUvMin, out var pinUvMax, out var pinSize, out var pinPath))
            {
                return false;
            }

            _cachedConePath = conePath;
            _cachedPinPath = pinPath;
            _cachedPinIconId = 0;

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
                NativePinRotation = naviMap.PlayerPinRotation
            };
            return true;
        }

        private bool TryLoadFromCachedPaths(out MinimapPlayerIndicatorAssets assets)
        {
            assets = default;
            ISharedImmediateTexture? coneTexture = null;
            ISharedImmediateTexture? pinTexture = null;

            if (!string.IsNullOrWhiteSpace(_cachedConePath))
            {
                coneTexture = Plugin.TextureProvider.GetFromGame(_cachedConePath);
            }

            if (!string.IsNullOrWhiteSpace(_cachedPinPath))
            {
                pinTexture = Plugin.TextureProvider.GetFromGame(_cachedPinPath);
            }
            else if (_cachedPinIconId != 0)
            {
                pinTexture = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(_cachedPinIconId));
            }

            if (coneTexture is null || pinTexture is null)
            {
                return false;
            }

            assets = _cachedAssets with
            {
                IsValid = true,
                ConeTexture = coneTexture,
                PinTexture = pinTexture
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
            if (partsList is null || partsList->PartCount == 0 || imageNode->PartId >= partsList->PartCount)
            {
                return false;
            }

            ref var part = ref partsList->Parts[imageNode->PartId];
            if (part.UldAsset is null || part.UldAsset->AtkTexture.Resource is null || part.UldAsset->AtkTexture.Resource->TexFileResourceHandle is null)
            {
                return false;
            }

            texturePath = part.UldAsset->AtkTexture.Resource->TexFileResourceHandle->FileName.ToString().Trim().Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                return false;
            }

            texture = Plugin.TextureProvider.GetFromGame(texturePath);
            if (texture is null)
            {
                return false;
            }

            var atkTexture = part.UldAsset->AtkTexture;
            var texWidth = Math.Max(atkTexture.GetTextureWidth(), 1u);
            var texHeight = Math.Max(atkTexture.GetTextureHeight(), 1u);
            uvMin = new Vector2(part.U / (float)texWidth, part.V / (float)texHeight);
            uvMax = new Vector2((part.U + part.Width) / (float)texWidth, (part.V + part.Height) / (float)texHeight);
            return true;
        }
    }
}
