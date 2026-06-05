namespace FFXIVHudPlugin;

/// <summary>
/// Serialized configuration for the standalone Action Camera module.
/// </summary>
[Serializable]
public sealed class ActionCameraConfiguration
{
    /// <summary>
    /// Enables action camera mode.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Horizontal sensitivity multiplier.
    /// </summary>
    public float HorizontalSensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Vertical sensitivity multiplier.
    /// </summary>
    public float VerticalSensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Backend mode used to drive camera behavior.
    /// </summary>
    public ActionCameraBackendMode BackendMode { get; set; } = ActionCameraBackendMode.RmbLatch;

    /// <summary>
    /// Unlock interaction mode.
    /// </summary>
    public ActionCameraUnlockMode UnlockMode { get; set; } = ActionCameraUnlockMode.Toggle;

    /// <summary>
    /// Hold-mode unlock key.
    /// </summary>
    public Dalamud.Game.ClientState.Keys.VirtualKey HoldUnlockKey { get; set; } =
        Dalamud.Game.ClientState.Keys.VirtualKey.LMENU;

    /// <summary>
    /// Toggle-mode key to switch between locked and unlocked states.
    /// </summary>
    public Dalamud.Game.ClientState.Keys.VirtualKey ToggleUnlockKey { get; set; } =
        Dalamud.Game.ClientState.Keys.VirtualKey.CAPITAL;

    /// <summary>
    /// Temporarily unlocks when game UI focus is detected.
    /// </summary>
    public bool UnlockOnUi { get; set; } = true;

    /// <summary>
    /// Escape forces unlock and release.
    /// </summary>
    public bool EscAlwaysUnlock { get; set; } = true;

    /// <summary>
    /// Relock immediately when toggle key is pressed in unlocked mode.
    /// </summary>
    public bool ReacquireOnToggle { get; set; } = true;

    /// <summary>
    /// Draws a center reticle while action camera is active.
    /// </summary>
    public bool ShowReticle { get; set; }

    /// <summary>
    /// Enables target suggestion behavior (future version placeholder).
    /// </summary>
    public bool AutoTarget { get; set; }

    /// <summary>
    /// Enables soft target suggestion scan at reticle center.
    /// </summary>
    public bool EnableSoftTargetSuggestion { get; set; }

    /// <summary>
    /// Maximum screen-space distance (pixels) from center reticle for soft target candidate matching.
    /// </summary>
    public float SoftTargetScreenRadius { get; set; } = 280f;

    /// <summary>
    /// Shows development debug overlay.
    /// </summary>
    public bool ShowDebugOverlay { get; set; }
}
