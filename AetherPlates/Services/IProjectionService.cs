using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Services;

public interface IProjectionService
{
    bool WorldToScreen(Vector3 world, out Vector2 screen);
}
