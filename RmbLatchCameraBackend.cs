using System.Runtime.InteropServices;

namespace FFXIVHudPlugin;

/// <summary>
/// Reliable backend that latches RMB camera behavior while action camera is locked.
/// </summary>
internal sealed class RmbLatchCameraBackend : ICameraControlBackend
{
    private bool enabled;
    private bool latchedRmb;
    private float yaw;
    private float pitch;

    public RmbLatchCameraBackend() { }

    public string Name => "RmbLatch";

    public bool CanControl => true;

    public void Enable()
    {
        this.enabled = true;
        this.EnsureLatched();
    }

    public void Disable()
    {
        this.ReleaseLatch();
        this.enabled = false;
    }

    public void Tick(float deltaX, float deltaY)
    {
        if (!this.enabled)
        {
            return;
        }

        this.EnsureLatched();

        // This backend intentionally tracks pseudo yaw/pitch for diagnostics while relying
        // on RMB-latch semantics for actual game camera behavior.
        this.yaw += deltaX * 0.00425f;
        this.pitch = Math.Clamp(this.pitch - (deltaY * 0.00425f), -1.35f, 1.35f);
    }

    public ActionCameraBackendSnapshot GetSnapshot() =>
        new(
            true,
            this.latchedRmb,
            this.latchedRmb,
            this.yaw,
            this.pitch,
            this.yaw,
            this.pitch);

    /// <summary>
    /// True while plugin-generated RMB latch is active.
    /// </summary>
    public bool IsLatched => this.latchedRmb;

    private void EnsureLatched()
    {
        if (this.latchedRmb)
        {
            return;
        }

        mouse_event(MouseEventfRightDown, 0, 0, 0, 0);
        this.latchedRmb = true;
    }

    private void ReleaseLatch()
    {
        if (!this.latchedRmb)
        {
            return;
        }

        mouse_event(MouseEventfRightUp, 0, 0, 0, 0);
        this.latchedRmb = false;
    }

    private const uint MouseEventfRightDown = 0x0008;
    private const uint MouseEventfRightUp = 0x0010;

    [DllImport("user32.dll", SetLastError = false)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, nuint dwExtraInfo);
}
