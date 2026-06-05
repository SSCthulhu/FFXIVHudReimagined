namespace FFXIVHudPlugin;

/// <summary>
/// Experimental direct camera backend using camera memory writes.
/// </summary>
internal sealed class DirectCameraControlBackend : ICameraControlBackend
{
    private readonly CameraController cameraController;

    public DirectCameraControlBackend(CameraController cameraController)
    {
        this.cameraController = cameraController;
    }

    public string Name => "DirectExperimental";

    public bool CanControl => this.cameraController.ProviderAvailable;

    public void Enable() => this.cameraController.Enable();

    public void Disable() => this.cameraController.Disable();

    public void Tick(float deltaX, float deltaY) => this.cameraController.Update(deltaX, deltaY);

    public ActionCameraBackendSnapshot GetSnapshot() =>
        new(
            this.cameraController.ProviderAvailable,
            this.cameraController.CameraWriteApplied,
            this.cameraController.CameraWritePersisted,
            this.cameraController.Yaw,
            this.cameraController.Pitch,
            this.cameraController.ReadbackYaw,
            this.cameraController.ReadbackPitch);
}
