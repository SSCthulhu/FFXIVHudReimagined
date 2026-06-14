namespace DelvUI.Interface.ActionCamera
{
    internal interface ICameraProvider
    {
        bool IsAvailable { get; }
        bool TryGetYawPitch(out float yaw, out float pitch);
        bool TrySetYawPitch(float yaw, float pitch);
    }
}
