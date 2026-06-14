using Dalamud.Game.ClientState.Keys;

namespace DelvUI.Interface.ActionCamera
{
    public enum ActionCameraUnlockMode
    {
        Hold = 0,
        Toggle = 1
    }

    public enum ActionCameraBackendMode
    {
        RmbLatch = 0,
        DirectExperimental = 1
    }

    public enum ActionCameraUnlockReason
    {
        None = 0,
        Toggle = 1,
        Escape = 2,
        Ui = 3
    }

    internal static class ActionCameraKeyDefaults
    {
        public const int UnlockKeybind = (int)VirtualKey.LMENU;
        public const int LockTargetKeybind = (int)VirtualKey.X;
    }
}
