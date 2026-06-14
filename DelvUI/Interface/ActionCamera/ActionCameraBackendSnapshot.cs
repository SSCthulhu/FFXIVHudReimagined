namespace DelvUI.Interface.ActionCamera
{
    public readonly record struct ActionCameraBackendSnapshot(
        bool CanControl,
        bool CameraWriteApplied,
        bool CameraWritePersisted,
        float Yaw,
        float Pitch,
        float ReadbackYaw,
        float ReadbackPitch);
}
