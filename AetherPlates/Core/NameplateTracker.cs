using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Services;

namespace FFXIVHudPlugin.AetherPlates.Core;

public sealed class NameplateTracker
{
    private readonly ObjectService objectService;
    private NameplateFrameSnapshot currentFrame = NameplateFrameSnapshot.Empty;
    private long frameCounter;

    public NameplateTracker(ObjectService objectService)
    {
        this.objectService = objectService;
    }

    public NameplateFrameSnapshot CurrentFrame => this.currentFrame;

    public void Update()
    {
        this.frameCounter++;
        this.currentFrame = new NameplateFrameSnapshot(
            this.frameCounter,
            DateTime.UtcNow,
            this.objectService.BuildSnapshot());
    }
}
