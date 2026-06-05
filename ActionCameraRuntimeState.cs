namespace FFXIVHudPlugin;

/// <summary>
/// Lightweight runtime state shared between action camera components.
/// </summary>
public readonly record struct ActionCameraRuntimeState(
    bool Active,
    bool CursorLocked,
    bool HoldUnlockHeld,
    bool UiFocused,
    float MouseDeltaX,
    float MouseDeltaY,
    float Yaw,
    float Pitch,
    bool ProviderAvailable,
    bool CameraWriteApplied,
    bool CameraWritePersisted,
    float ReadbackYaw,
    float ReadbackPitch,
    string BackendName,
    bool IsLatched,
    ActionCameraUnlockReason UnlockReason,
    bool PendingRelock,
    long UpdateTick,
    long LateUpdateTick,
    string LastError,
    bool SoftTargetHasCandidate,
    uint SoftTargetObjectId,
    float SoftTargetScreenX,
    float SoftTargetScreenY,
    float SoftTargetScore);
