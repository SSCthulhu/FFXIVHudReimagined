using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    internal static class NativeMinimapVisibility
    {
        public const string AddonName = "_NaviMap";

        public static unsafe void SetVisible(bool visible)
        {
            var addon = TryGetAddon();
            if (addon is null)
            {
                return;
            }

            addon->IsVisible = visible;
            if (visible && addon->Alpha < 255)
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

    internal static class MinimapNativeNorthLock
    {
        public static unsafe bool TryGetCurrent(out bool northLocked)
        {
            northLocked = false;
            var addon = NativeMinimapVisibility.TryGetAddon();
            if (addon is null)
            {
                return false;
            }

            northLocked = addon->NaviMap.NorthLockedUp;
            return true;
        }

        public static unsafe void Apply(bool northLocked)
        {
            var addon = NativeMinimapVisibility.TryGetAddon();
            if (addon is null)
            {
                return;
            }

            ref var naviMap = ref addon->NaviMap;
            if (naviMap.NorthLockedUp != northLocked)
            {
                naviMap.NorthLockedUp = northLocked;
            }

            if (!addon->IsVisible)
            {
                return;
            }

            var checkbox = addon->LockNorthCheckbox;
            if (checkbox is not null && checkbox->IsChecked != northLocked)
            {
                checkbox->SetChecked(northLocked);
            }
        }
    }

    internal static class MinimapNativeFrame
    {
        internal readonly struct Transform
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
            var addon = NativeMinimapVisibility.TryGetAddon();
            if (addon is null || addon->MapImage is null)
            {
                return false;
            }

            var node = (AtkResNode*)addon->MapImage;
            transform = new Transform
            {
                Rotation = node->Rotation,
                ScaleX = node->ScaleX,
                ScaleY = node->ScaleY,
                NorthLockedUp = addon->NaviMap.NorthLockedUp,
                PlayerConeRotation = addon->NaviMap.PlayerConeRotation
            };
            return true;
        }
    }

    internal static class MinimapNativeMapTexture
    {
        public static unsafe bool TryGetMapImagePath(out string texturePath)
        {
            texturePath = string.Empty;
            var addon = NativeMinimapVisibility.TryGetAddon();
            if (addon is null || addon->UldManager.LoadedState != AtkLoadState.Loaded || addon->MapImage is null)
            {
                return false;
            }

            var imageNode = addon->MapImage;
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

            texturePath = part.UldAsset->AtkTexture.Resource->TexFileResourceHandle->FileName.ToString();
            return !string.IsNullOrWhiteSpace(texturePath);
        }

        public static bool TryGetTexture(ITextureProvider textureProvider, out ISharedImmediateTexture? texture)
        {
            texture = null;
            if (!TryGetMapImagePath(out var path))
            {
                return false;
            }

            texture = textureProvider.GetFromGame(path);
            if (texture is null)
            {
                return false;
            }

            var wrap = texture.GetWrapOrEmpty();
            return wrap.Handle != 0 && wrap.Width > 0 && wrap.Height > 0;
        }
    }

    internal static class MinimapCameraHeading
    {
        public static unsafe bool TryGetMapYaw(out float yaw)
        {
            yaw = 0f;
            var cameraManager = CameraManager.Instance();
            if (cameraManager is null || cameraManager->CurrentCamera is null)
            {
                return false;
            }

            var camera = cameraManager->CurrentCamera;
            return TryWorldDirectionToYaw(camera->ViewMatrix.M13, camera->ViewMatrix.M33, out yaw);
        }

        private static bool TryWorldDirectionToYaw(float worldX, float worldZ, out float yaw)
        {
            yaw = 0f;
            var lenSq = (worldX * worldX) + (worldZ * worldZ);
            if (lenSq < 0.0001f)
            {
                return false;
            }

            yaw = MathF.Atan2(worldX, worldZ);
            return !float.IsNaN(yaw) && !float.IsInfinity(yaw);
        }
    }
}
