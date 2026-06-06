namespace FFXIVHudPlugin.AetherPlates.Data;

public sealed record NameplateFrameSnapshot(
    long FrameNumber,
    DateTime UtcTimestamp,
    IReadOnlyList<TrackedObject> Objects)
{
    public static NameplateFrameSnapshot Empty { get; } = new(
        0,
        DateTime.UtcNow,
        Array.Empty<TrackedObject>());
}
