using System.Runtime.InteropServices;

namespace FFXIVHudPlugin;

/// <summary>
/// Reliable backend that latches RMB camera behavior while action camera is locked.
/// </summary>
internal sealed class RmbLatchCameraBackend : ICameraControlBackend
{
    private const int VKeyRButton = 0x02;

    private readonly ActionCameraConfiguration config;
    private bool enabled;
    private bool latchedRmb;
    private bool previousPhysicalRmbDown;
    private float yaw;
    private float pitch;

    public RmbLatchCameraBackend(ActionCameraConfiguration config)
    {
        this.config = config;
    }

    public string Name => "RmbLatch";

    public bool CanControl => true;

    public void Enable()
    {
        this.enabled = true;
        this.previousPhysicalRmbDown = this.IsPhysicalRightMouseDown();
        this.EnsureLatched();
    }

    public void Disable()
    {
        this.ReleaseLatch();
        this.enabled = false;
        this.previousPhysicalRmbDown = false;
    }

    public void Tick(float deltaX, float deltaY)
    {
        if (!this.enabled)
        {
            return;
        }

        this.MaintainLatchState();

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

    private void MaintainLatchState()
    {
        var physicalRmbDown = this.IsPhysicalRightMouseDown();
        if (!this.config.PreventRmbDisruption)
        {
            this.previousPhysicalRmbDown = physicalRmbDown;
            return;
        }

        if (this.previousPhysicalRmbDown && !physicalRmbDown)
        {
            this.RelatchPulse();
        }

        this.previousPhysicalRmbDown = physicalRmbDown;
    }

    private void RelatchPulse()
    {
        // A physical RMB-up can clear camera-hold state even though we think we're latched.
        // Emit a deterministic up->down pulse so latch state is always restored after release.
        mouse_event(MouseEventfRightUp, 0, 0, 0, 0);
        mouse_event(MouseEventfRightDown, 0, 0, 0, 0);
        this.latchedRmb = true;
    }

    private bool IsPhysicalRightMouseDown() => (GetAsyncKeyState(VKeyRButton) & 0x8000) != 0;

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

    [DllImport("user32.dll", SetLastError = false)]
    private static extern short GetAsyncKeyState(int vKey);
}
