namespace FFXIVHudPlugin;

/// <summary>
/// Applies mouse deltas to camera yaw/pitch through a camera provider.
/// </summary>
internal sealed class CameraController
{
    private const float PitchMin = -1.35f;
    private const float PitchMax = 1.35f;
    private const float BaseSensitivity = 0.00425f;
    private readonly ActionCameraConfiguration config;
    private readonly ICameraProvider cameraProvider;
    private float yaw;
    private float pitch;
    private bool initialized;
    private bool providerAvailable;
    private bool cameraWriteApplied;
    private bool cameraWritePersisted;
    private float readbackYaw;
    private float readbackPitch;

    public CameraController(ActionCameraConfiguration config, ICameraProvider cameraProvider)
    {
        this.config = config;
        this.cameraProvider = cameraProvider;
    }

    /// <summary>
    /// Last applied yaw.
    /// </summary>
    public float Yaw => this.yaw;

    /// <summary>
    /// Last applied pitch.
    /// </summary>
    public float Pitch => this.pitch;
    public bool ProviderAvailable => this.providerAvailable;
    public bool CameraWriteApplied => this.cameraWriteApplied;
    public bool CameraWritePersisted => this.cameraWritePersisted;
    public float ReadbackYaw => this.readbackYaw;
    public float ReadbackPitch => this.readbackPitch;

    /// <summary>
    /// Attempts to initialize camera baseline values.
    /// </summary>
    public void Enable()
    {
        this.initialized = this.cameraProvider.TryGetYawPitch(out this.yaw, out this.pitch);
        this.providerAvailable = this.cameraProvider.IsAvailable;
        this.cameraWriteApplied = false;
        this.cameraWritePersisted = false;
        this.readbackYaw = this.yaw;
        this.readbackPitch = this.pitch;
    }

    /// <summary>
    /// Clears cached camera baseline.
    /// </summary>
    public void Disable()
    {
        this.initialized = false;
        this.cameraWriteApplied = false;
        this.cameraWritePersisted = false;
    }

    /// <summary>
    /// Applies frame deltas to yaw and pitch.
    /// </summary>
    public bool Update(float deltaX, float deltaY)
    {
        this.providerAvailable = this.cameraProvider.IsAvailable;
        if (!this.providerAvailable)
        {
            this.initialized = false;
            this.cameraWriteApplied = false;
            this.cameraWritePersisted = false;
            return false;
        }

        if (!this.initialized && !this.cameraProvider.TryGetYawPitch(out this.yaw, out this.pitch))
        {
            return false;
        }

        this.initialized = true;

        this.yaw += deltaX * BaseSensitivity * this.config.HorizontalSensitivity;
        this.pitch -= deltaY * BaseSensitivity * this.config.VerticalSensitivity;
        this.pitch = Math.Clamp(this.pitch, PitchMin, PitchMax);

        this.cameraWriteApplied = this.cameraProvider.TrySetYawPitch(this.yaw, this.pitch);
        if (!this.cameraWriteApplied)
        {
            this.cameraWritePersisted = false;
            return false;
        }

        if (!this.cameraProvider.TryGetYawPitch(out this.readbackYaw, out this.readbackPitch))
        {
            this.cameraWritePersisted = false;
            return true;
        }

        this.cameraWritePersisted =
            MathF.Abs(this.readbackYaw - this.yaw) <= 0.02f &&
            MathF.Abs(this.readbackPitch - this.pitch) <= 0.02f;
        return true;
    }
}
