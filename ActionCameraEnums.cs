using Dalamud.Game.ClientState.Keys;

namespace FFXIVHudPlugin;

/// <summary>
/// Action camera mouse unlock interaction modes.
/// </summary>
public enum ActionCameraUnlockMode
{
    Hold = 0,
    Toggle = 1,
}

/// <summary>
/// Action camera backend modes.
/// </summary>
public enum ActionCameraBackendMode
{
    RmbLatch = 0,
    DirectExperimental = 1,
}

/// <summary>
/// Runtime unlock reason for debug telemetry.
/// </summary>
public enum ActionCameraUnlockReason
{
    None = 0,
    Toggle = 1,
    Escape = 2,
    Ui = 3,
}

/// <summary>
/// Shared key presets for action camera settings UI.
/// </summary>
internal static class ActionCameraKeyPresets
{
    public static readonly VirtualKey[] HoldKeys =
    {
        VirtualKey.MENU,
    };

    public static readonly VirtualKey[] ToggleKeys =
    {
        VirtualKey.MENU,
    };

    public static string GetLabel(VirtualKey key) =>
        key == VirtualKey.MENU || key == VirtualKey.LMENU || key == VirtualKey.RMENU
            ? "ALT"
            : key.ToString();
}
