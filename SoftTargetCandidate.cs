using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Immutable soft-target candidate snapshot for overlay/debug.
/// </summary>
public readonly record struct SoftTargetCandidate(
    bool HasCandidate,
    uint ObjectId,
    Vector2 ScreenPosition,
    float Score);
