using Dalamud.Interface.Textures;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    internal readonly struct MinimapBlip
    {
        public Vector2 ScreenOffset { get; init; }
        public MinimapBlipKind Kind { get; init; }
        public uint Color { get; init; }
        public float Radius { get; init; }
    }

    internal enum MinimapBlipKind
    {
        Party = 0,
        Enemy = 1,
    }

    internal readonly struct MinimapFateArea
    {
        public Vector2 ScreenOffset { get; init; }
        public float RadiusPixels { get; init; }
    }

    internal readonly struct MinimapIconMarker
    {
        public Vector2 ScreenOffset { get; init; }
        public bool UsesNativeScreenOffset { get; init; }
        public Vector2 MapTextureDelta { get; init; }
        public uint IconId { get; init; }
        public float Size { get; init; }
        public ISharedImmediateTexture? Texture { get; init; }
    }

    internal readonly struct MinimapPlayerIndicatorAssets
    {
        public bool IsValid { get; init; }
        public ISharedImmediateTexture? ConeTexture { get; init; }
        public ISharedImmediateTexture? PinTexture { get; init; }
        public Vector2 ConeUvMin { get; init; }
        public Vector2 ConeUvMax { get; init; }
        public Vector2 PinUvMin { get; init; }
        public Vector2 PinUvMax { get; init; }
        public Vector2 ConeSize { get; init; }
        public Vector2 PinSize { get; init; }
        public float NativeMapSize { get; init; }
        public float NativeConeRotation { get; init; }
        public float NativePinRotation { get; init; }
    }

    internal sealed class MinimapSnapshot
    {
        public static MinimapSnapshot Empty { get; } = new();

        public bool IsActive { get; init; }
        public float PlayerYaw { get; init; }
        public float CameraMapYaw { get; init; }
        public bool HasCameraMapYaw { get; init; }
        public string MapTitle { get; init; } = string.Empty;
        public IReadOnlyList<MinimapBlip> Blips { get; init; } = Array.Empty<MinimapBlip>();
        public IReadOnlyList<MinimapIconMarker> IconMarkers { get; init; } = Array.Empty<MinimapIconMarker>();
        public IReadOnlyList<MinimapFateArea> FateAreas { get; init; } = Array.Empty<MinimapFateArea>();
        public ISharedImmediateTexture? MapTexture { get; init; }
        public Vector2 MapUvMin { get; init; }
        public Vector2 MapUvMax { get; init; }
        public bool HasMapTexture { get; init; }
        public MinimapPlayerIndicatorAssets PlayerIndicator { get; init; }
        public bool HasNativeMapFrame { get; init; }
        public float NativeMapImageRotation { get; init; }
        public float NativeMapImageScaleX { get; init; } = 1f;
        public float NativeMapImageScaleY { get; init; } = 1f;
        public bool NativeNorthLockedUp { get; init; }
        public float NativePlayerConeRotation { get; init; }
        public float VisibleRangeYalms { get; init; }
        public uint PlayerClassJobId { get; init; }
        public uint PlayerPinFillColor { get; init; }
    }

    internal sealed class MinimapDiagnosticReport
    {
        public string Text { get; init; } = string.Empty;
        public DateTime CapturedUtc { get; init; } = DateTime.UtcNow;
    }
}
