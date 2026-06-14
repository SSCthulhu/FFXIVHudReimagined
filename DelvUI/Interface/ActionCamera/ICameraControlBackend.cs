namespace DelvUI.Interface.ActionCamera
{
    internal interface ICameraControlBackend
    {
        string Name { get; }
        bool CanControl { get; }
        void Enable();
        void Disable();
        void Tick(float deltaX, float deltaY);
        ActionCameraBackendSnapshot GetSnapshot();
    }
}
