namespace DelvUI.Interface.ActionCamera
{
    internal sealed class DirectCameraControlBackend : ICameraControlBackend
    {
        private readonly CameraController _cameraController;

        public DirectCameraControlBackend(CameraController cameraController)
        {
            _cameraController = cameraController;
        }

        public string Name => "DirectExperimental";
        public bool CanControl => _cameraController.ProviderAvailable;
        public void Enable() => _cameraController.Enable();
        public void Disable() => _cameraController.Disable();
        public void Tick(float deltaX, float deltaY) => _cameraController.Update(deltaX, deltaY);

        public ActionCameraBackendSnapshot GetSnapshot()
        {
            return new ActionCameraBackendSnapshot(
                _cameraController.ProviderAvailable,
                _cameraController.CameraWriteApplied,
                _cameraController.CameraWritePersisted,
                _cameraController.Yaw,
                _cameraController.Pitch,
                _cameraController.ReadbackYaw,
                _cameraController.ReadbackPitch);
        }
    }
}
