namespace FFXIVHudPlugin;

/// <summary>
/// Abstraction over game camera read/write access so game-version differences can be isolated.
/// </summary>
internal interface ICameraProvider
{
    /// <summary>
    /// Gets whether direct camera write access is currently available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Tries to read current camera yaw and pitch.
    /// </summary>
    bool TryGetYawPitch(out float yaw, out float pitch);

    /// <summary>
    /// Tries to apply camera yaw and pitch immediately.
    /// </summary>
    bool TrySetYawPitch(float yaw, float pitch);
}
