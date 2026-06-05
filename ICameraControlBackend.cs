namespace FFXIVHudPlugin;

/// <summary>
/// Backend interface for action camera control mechanics.
/// </summary>
internal interface ICameraControlBackend
{
    /// <summary>
    /// Human-readable backend name for diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// True when backend is available this frame.
    /// </summary>
    bool CanControl { get; }

    /// <summary>
    /// Called when action camera enters locked mode.
    /// </summary>
    void Enable();

    /// <summary>
    /// Called when action camera exits locked mode.
    /// </summary>
    void Disable();

    /// <summary>
    /// Per-frame update while locked.
    /// </summary>
    void Tick(float deltaX, float deltaY);

    /// <summary>
    /// Returns backend runtime diagnostics.
    /// </summary>
    ActionCameraBackendSnapshot GetSnapshot();
}
