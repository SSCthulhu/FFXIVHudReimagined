using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Camera heading on the north-up minimap (same convention as player.Rotation: 0 ≈ +Z / south).
/// </summary>
internal static class MinimapCameraHeading
{
    public static unsafe bool TryGetMapYaw(out float yaw)
    {
        yaw = 0f;
        var cameraManager = CameraManager.Instance();
        if (cameraManager is null)
        {
            return false;
        }

        var camera = cameraManager->CurrentCamera;
        if (camera is null)
        {
            return false;
        }

        if (TryGetYawFromViewMatrix(camera->ViewMatrix, out yaw))
        {
            return true;
        }

        return TryGetYawFromLookVectors(camera, out yaw);
    }

    /// <summary>
    /// Horizontal camera azimuth from the live view matrix (in-game camera yaw convention).
    /// </summary>
    private static bool TryGetYawFromViewMatrix(
        FFXIVClientStructs.FFXIV.Common.Math.Matrix4x4 viewMatrix,
        out float yaw)
    {
        yaw = 0f;
        return TryWorldDirectionToYaw(viewMatrix.M13, viewMatrix.M33, out yaw);
    }

    private static unsafe bool TryGetYawFromLookVectors(Camera* camera, out float yaw)
    {
        yaw = 0f;
        var lookAt = camera->LookAtVector;
        var position = camera->Position;
        var delta = new Vector3(lookAt.X - position.X, 0f, lookAt.Z - position.Z);
        if (TryWorldDirectionToYaw(delta.X, delta.Z, out yaw))
        {
            return true;
        }

        return TryWorldDirectionToYaw(lookAt.X, lookAt.Z, out yaw);
    }

    private static bool TryWorldDirectionToYaw(float worldX, float worldZ, out float yaw)
    {
        yaw = 0f;
        var lengthSquared = (worldX * worldX) + (worldZ * worldZ);
        if (lengthSquared < 0.0001f)
        {
            return false;
        }

        yaw = MathF.Atan2(worldX, worldZ);
        return !float.IsNaN(yaw) && !float.IsInfinity(yaw);
    }
}
