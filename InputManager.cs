using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

namespace FFXIVHudPlugin;

/// <summary>
/// Captures per-frame key and mouse input used by action camera.
/// </summary>
internal sealed class InputManager
{
    private bool toggleUnlockPrevious;
    private readonly IKeyState keyState;
    private readonly ActionCameraConfiguration config;
    private float mouseDeltaX;
    private float mouseDeltaY;
    private bool altHeld;
    private bool escPressed;
    private bool escPressedPrevious;

    public InputManager(IKeyState keyState, ActionCameraConfiguration config)
    {
        this.keyState = keyState;
        this.config = config;
    }

    public bool IsEscPressed => this.escPressed;

    /// <summary>
    /// Last raw horizontal mouse delta captured this frame.
    /// </summary>
    public float MouseDeltaX => this.mouseDeltaX;

    /// <summary>
    /// Last raw vertical mouse delta captured this frame.
    /// </summary>
    public float MouseDeltaY => this.mouseDeltaY;

    /// <summary>
    /// Reads current frame mouse deltas.
    /// </summary>
    public void Update()
    {
        var io = Dalamud.Bindings.ImGui.ImGui.GetIO();
        this.mouseDeltaX = io.MouseDelta.X;
        this.mouseDeltaY = io.MouseDelta.Y;
        this.altHeld =
            this.SafeIsKeyDown(VirtualKey.LMENU) ||
            this.SafeIsKeyDown(VirtualKey.RMENU) ||
            this.SafeIsKeyDown(VirtualKey.MENU);
        this.escPressedPrevious = this.escPressed;
        this.escPressed = this.SafeIsKeyDown(VirtualKey.ESCAPE);
    }

    /// <summary>
    /// Returns whether hold-unlock key is currently pressed.
    /// </summary>
    public bool IsHoldUnlockActive()
    {
        return this.SafeIsKeyDown(this.config.HoldUnlockKey);
    }

    /// <summary>
    /// Returns true only on toggle-unlock rising edge.
    /// </summary>
    public bool ConsumeToggleUnlockPressed()
    {
        var nowPressed = this.SafeIsKeyDown(this.config.ToggleUnlockKey);
        var rising = nowPressed && !this.toggleUnlockPrevious;
        this.toggleUnlockPrevious = nowPressed;
        return rising;
    }

    public bool IsAnyAltHeld => this.altHeld;

    public bool ConsumeEscPressedEdge() => this.escPressed && !this.escPressedPrevious;

    private bool SafeIsKeyDown(VirtualKey key)
    {
        try
        {
            return this.keyState[key];
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
