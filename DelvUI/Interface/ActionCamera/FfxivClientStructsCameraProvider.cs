using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using System.Numerics;
using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using SceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class FfxivClientStructsCameraProvider : ICameraProvider
    {
        public unsafe bool IsAvailable => TryGetGameplayCamera(out _) || TryGetSceneCamera(out _);

        public unsafe bool TryGetYawPitch(out float yaw, out float pitch)
        {
            yaw = 0f;
            pitch = 0f;

            if (TryGetGameplayCamera(out var gameplayCamera))
            {
                yaw = gameplayCamera->DirH;
                pitch = gameplayCamera->DirV;
                return float.IsFinite(yaw) && float.IsFinite(pitch);
            }

            if (!TryGetSceneCamera(out var sceneCamera))
            {
                return false;
            }

            var delta = new Vector3(
                sceneCamera->LookAtVector.X - sceneCamera->Position.X,
                sceneCamera->LookAtVector.Y - sceneCamera->Position.Y,
                sceneCamera->LookAtVector.Z - sceneCamera->Position.Z);

            float horizontalLength = MathF.Sqrt((delta.X * delta.X) + (delta.Z * delta.Z));
            if (horizontalLength < 0.0001f)
            {
                return false;
            }

            yaw = MathF.Atan2(delta.X, delta.Z);
            pitch = MathF.Atan2(delta.Y, horizontalLength);
            return float.IsFinite(yaw) && float.IsFinite(pitch);
        }

        public unsafe bool TrySetYawPitch(float yaw, float pitch)
        {
            if (!float.IsFinite(yaw) || !float.IsFinite(pitch))
            {
                return false;
            }

            if (TryGetGameplayCamera(out var gameplayCamera))
            {
                if (float.IsFinite(gameplayCamera->DirVMin) &&
                    float.IsFinite(gameplayCamera->DirVMax) &&
                    gameplayCamera->DirVMin < gameplayCamera->DirVMax)
                {
                    pitch = Math.Clamp(pitch, gameplayCamera->DirVMin, gameplayCamera->DirVMax);
                }

                gameplayCamera->DirH = yaw;
                gameplayCamera->DirV = pitch;
                gameplayCamera->InputDeltaH = 0f;
                gameplayCamera->InputDeltaV = 0f;
                gameplayCamera->InputDeltaHAdjusted = 0f;
                gameplayCamera->InputDeltaVAdjusted = 0f;
                gameplayCamera->UpdateState();
                return true;
            }

            if (!TryGetSceneCamera(out var sceneCamera))
            {
                return false;
            }

            var direction = new Vector3(
                MathF.Sin(yaw) * MathF.Cos(pitch),
                MathF.Sin(pitch),
                MathF.Cos(yaw) * MathF.Cos(pitch));

            if (direction.LengthSquared() < 0.0001f)
            {
                return false;
            }

            var current = new Vector3(
                sceneCamera->LookAtVector.X - sceneCamera->Position.X,
                sceneCamera->LookAtVector.Y - sceneCamera->Position.Y,
                sceneCamera->LookAtVector.Z - sceneCamera->Position.Z);

            float distance = current.Length();
            if (distance < 0.1f)
            {
                distance = 5f;
            }

            direction = Vector3.Normalize(direction) * distance;
            sceneCamera->LookAtVector.X = sceneCamera->Position.X + direction.X;
            sceneCamera->LookAtVector.Y = sceneCamera->Position.Y + direction.Y;
            sceneCamera->LookAtVector.Z = sceneCamera->Position.Z + direction.Z;
            return true;
        }

        private static unsafe bool TryGetGameplayCamera(out GameCamera* camera)
        {
            camera = null;
            var manager = CameraManager.Instance();
            if (manager is null)
            {
                return false;
            }

            camera = manager->GetActiveCamera();
            if (camera is null)
            {
                camera = manager->Camera;
            }

            return camera is not null;
        }

        private static unsafe bool TryGetSceneCamera(out SceneCamera* camera)
        {
            camera = null;
            var manager = SceneCameraManager.Instance();
            if (manager is null)
            {
                return false;
            }

            camera = manager->CurrentCamera;
            return camera is not null;
        }
    }
}
