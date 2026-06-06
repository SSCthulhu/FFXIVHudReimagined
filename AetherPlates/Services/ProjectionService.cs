using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Services;

public sealed unsafe class ProjectionService : IProjectionService
{
    public bool WorldToScreen(Vector3 world, out Vector2 screen)
    {
        screen = default;
        var sceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance();
        if (sceneCameraManager is null || sceneCameraManager->CurrentCamera is null)
        {
            return false;
        }

        var worldCopy = world;
        if (!sceneCameraManager->CurrentCamera->WorldToScreen(worldCopy, out var screenNative))
        {
            return false;
        }

        screen = new Vector2(screenNative.X, screenNative.Y);
        return true;
    }
}
