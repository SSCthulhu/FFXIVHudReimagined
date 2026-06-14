using System.Numerics;

namespace DelvUI.Interface.ActionCamera
{
    public readonly record struct ActionCameraSoftTargetCandidate(
        bool HasCandidate,
        uint ObjectId,
        Vector2 ScreenPosition,
        float Score);
}
