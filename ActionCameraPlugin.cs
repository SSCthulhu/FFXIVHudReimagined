using Dalamud.Plugin.Services;

namespace FFXIVHudPlugin;

/// <summary>
/// Standalone action-camera feature lifecycle and per-frame orchestration.
/// </summary>
internal sealed class ActionCameraPlugin : IDisposable
{
    private readonly HudConfiguration rootConfig;
    private readonly ActionCameraConfiguration config;
    private readonly IClientState clientState;
    private readonly IObjectTable objectTable;
    private readonly InputManager inputManager;
    private readonly UiStateService uiStateService;
    private readonly CursorManager cursorManager;
    private readonly ITargetManager targetManager;
    private readonly CameraController cameraController;
    private readonly RmbLatchCameraBackend rmbLatchBackend;
    private readonly DirectCameraControlBackend directBackend;
    private readonly SoftTargetService softTargetService;
    private readonly IPluginLog log;
    private bool active;
    private bool lockedModeActive;
    private ActionCameraUnlockReason unlockReason;
    private bool pendingRelock;
    private bool previousConfigEnabled;
    private bool disposed;
    private bool suppressTargetOnNextRelock;
    private bool deferredSuppressTargetTap;
    private bool escForcedUnlockActive;
    private bool escMenuTemporaryUnlock;
    private bool escUnlockReleaseSeen;
    private bool escRelockRequested;
    private bool escOpenMenuRequested;
    private ActionCameraRuntimeState runtimeState;
    private long updateTick;
    private long lateUpdateTick;
    private float pendingDeltaX;
    private float pendingDeltaY;

    public ActionCameraPlugin(
        HudConfiguration rootConfig,
        IClientState clientState,
        IObjectTable objectTable,
        InputManager inputManager,
        UiStateService uiStateService,
        CursorManager cursorManager,
        ITargetManager targetManager,
        CameraController cameraController,
        RmbLatchCameraBackend rmbLatchBackend,
        DirectCameraControlBackend directBackend,
        SoftTargetService softTargetService,
        IPluginLog log)
    {
        this.rootConfig = rootConfig;
        this.config = rootConfig.ActionCamera;
        this.clientState = clientState;
        this.objectTable = objectTable;
        this.inputManager = inputManager;
        this.uiStateService = uiStateService;
        this.cursorManager = cursorManager;
        this.targetManager = targetManager;
        this.cameraController = cameraController;
        this.rmbLatchBackend = rmbLatchBackend;
        this.directBackend = directBackend;
        this.softTargetService = softTargetService;
        this.log = log;
    }

    /// <summary>
    /// Current action-camera runtime state for overlays and diagnostics.
    /// </summary>
    public ActionCameraRuntimeState RuntimeState => this.runtimeState;

    /// <summary>
    /// Toggles configured action camera mode.
    /// </summary>
    public void Toggle()
    {
        this.config.Enabled = !this.config.Enabled;
        this.rootConfig.Save();
        if (!this.config.Enabled)
        {
            this.SetActive(false);
            this.lockedModeActive = false;
            this.unlockReason = ActionCameraUnlockReason.None;
            this.pendingRelock = false;
            this.escForcedUnlockActive = false;
            this.escMenuTemporaryUnlock = false;
            this.escUnlockReleaseSeen = false;
            this.escRelockRequested = false;
            this.escOpenMenuRequested = false;
        }
        else
        {
            this.lockedModeActive = true;
            this.unlockReason = ActionCameraUnlockReason.None;
            this.pendingRelock = false;
            this.escForcedUnlockActive = false;
            this.escMenuTemporaryUnlock = false;
            this.escUnlockReleaseSeen = false;
            this.escRelockRequested = false;
            this.escOpenMenuRequested = false;
        }
    }

    /// <summary>
    /// Per-frame update.
    /// </summary>
    public void Update()
    {
        try
        {
            this.updateTick++;
            this.inputManager.Update();
            var justEnabled = this.config.Enabled && !this.previousConfigEnabled;
            this.previousConfigEnabled = this.config.Enabled;

            if (!this.config.Enabled || !this.IsInWorld())
            {
                this.lockedModeActive = false;
                this.unlockReason = ActionCameraUnlockReason.None;
                this.pendingRelock = false;
                this.escForcedUnlockActive = false;
                this.escMenuTemporaryUnlock = false;
                this.escUnlockReleaseSeen = false;
                this.escRelockRequested = false;
                this.escOpenMenuRequested = false;
                this.SetActive(false);
                this.softTargetService.Update(false);
                var backendSnapshot = this.ActiveBackend.GetSnapshot();
                this.runtimeState = this.runtimeState with
                {
                    Active = false,
                    CursorLocked = false,
                    HoldUnlockHeld = this.inputManager.IsHoldUnlockActive(),
                    UiFocused = false,
                    MouseDeltaX = this.inputManager.MouseDeltaX,
                    MouseDeltaY = this.inputManager.MouseDeltaY,
                    ProviderAvailable = backendSnapshot.CanControl,
                    CameraWriteApplied = false,
                    CameraWritePersisted = false,
                    ReadbackYaw = backendSnapshot.ReadbackYaw,
                    ReadbackPitch = backendSnapshot.ReadbackPitch,
                    BackendName = this.ActiveBackend.Name,
                    IsLatched = this.rmbLatchBackend.IsLatched,
                    UnlockReason = this.unlockReason,
                    PendingRelock = this.pendingRelock,
                    UpdateTick = this.updateTick,
                    LateUpdateTick = this.lateUpdateTick,
                    LastError = this.runtimeState.LastError,
                    SoftTargetHasCandidate = this.softTargetService.Candidate.HasCandidate,
                    SoftTargetObjectId = this.softTargetService.Candidate.ObjectId,
                    SoftTargetScreenX = this.softTargetService.Candidate.ScreenPosition.X,
                    SoftTargetScreenY = this.softTargetService.Candidate.ScreenPosition.Y,
                    SoftTargetScore = this.softTargetService.Candidate.Score,
                    SoftTargetScannedCount = this.softTargetService.DebugScannedCount,
                    SoftTargetEnemyCandidateCount = this.softTargetService.DebugEnemyCandidateCount,
                    SoftTargetEngagedCandidateCount = this.softTargetService.DebugEngagedCandidateCount,
                    SoftTargetLastRejectedObjectId = this.softTargetService.DebugLastRejectedObjectId,
                    SoftTargetLastRejectReason = this.softTargetService.DebugLastRejectReason,
                };
                this.pendingDeltaX = 0f;
                this.pendingDeltaY = 0f;
                return;
            }

            if (justEnabled)
            {
                this.lockedModeActive = true;
                this.unlockReason = ActionCameraUnlockReason.None;
                this.pendingRelock = false;
                this.escForcedUnlockActive = false;
                this.escMenuTemporaryUnlock = false;
                this.escUnlockReleaseSeen = false;
                this.escRelockRequested = false;
                this.escOpenMenuRequested = false;
            }

            var holdUnlockHeld = this.inputManager.IsHoldUnlockActive();
            var uiFocused = this.config.UnlockOnUi && this.uiStateService.IsUiFocused;
            var mainMenuOpen = this.uiStateService.IsMainMenuOpen;
            var suppressSoftTargeting = false;
            var escPressedEdge = this.inputManager.ConsumeEscPressedEdge();

            if (this.config.EscAlwaysUnlock && this.inputManager.IsEscPressed)
            {
                this.lockedModeActive = false;
                this.unlockReason = ActionCameraUnlockReason.Escape;
                this.pendingRelock = true;
                this.suppressTargetOnNextRelock = true;
                this.escForcedUnlockActive = true;
                this.escMenuTemporaryUnlock = true;
                this.escUnlockReleaseSeen = false;
                this.escRelockRequested = false;
                suppressSoftTargeting = true;

                if (escPressedEdge)
                {
                    var hadTarget = this.targetManager.Target is not null || this.targetManager.SoftTarget is not null;
                    this.targetManager.Target = null;
                    this.targetManager.SoftTarget = null;
                    this.escOpenMenuRequested = hadTarget && !this.uiStateService.IsMainMenuOpen;
                }
            }

            if (this.escMenuTemporaryUnlock)
            {
                if (!this.inputManager.IsEscPressed)
                {
                    this.escUnlockReleaseSeen = true;
                }

                if (this.escUnlockReleaseSeen && escPressedEdge)
                {
                    this.escRelockRequested = true;
                }
            }

            var togglePressed = this.inputManager.ConsumeToggleUnlockPressed();
            if (togglePressed)
            {
                if (!this.lockedModeActive && !this.config.ReacquireOnToggle)
                {
                    this.pendingRelock = true;
                    this.suppressTargetOnNextRelock = true;
                }
                else
                {
                    if (!this.lockedModeActive)
                    {
                        this.suppressTargetOnNextRelock = true;
                    }
                    this.lockedModeActive = !this.lockedModeActive;
                    this.pendingRelock = false;
                    this.unlockReason = this.lockedModeActive
                        ? ActionCameraUnlockReason.None
                        : ActionCameraUnlockReason.Toggle;
                }

                this.escForcedUnlockActive = false;
                this.escMenuTemporaryUnlock = false;
                this.escUnlockReleaseSeen = false;
                this.escRelockRequested = false;
                this.escOpenMenuRequested = false;
            }

            if (this.config.UnlockMode == ActionCameraUnlockMode.Hold)
            {
                if (this.escForcedUnlockActive)
                {
                    // Stay unlocked after ESC in hold mode until user intentionally
                    // re-engages hold control by pressing the hold key once.
                    this.lockedModeActive = false;
                    this.unlockReason = ActionCameraUnlockReason.Escape;
                    if (holdUnlockHeld)
                    {
                        this.escForcedUnlockActive = false;
                    }
                }
                else
                {
                    if (holdUnlockHeld)
                    {
                        this.suppressTargetOnNextRelock = true;
                    }

                    this.lockedModeActive = !holdUnlockHeld;
                    this.unlockReason = this.lockedModeActive
                        ? ActionCameraUnlockReason.None
                        : ActionCameraUnlockReason.Toggle;
                }
            }

            if (uiFocused)
            {
                this.lockedModeActive = false;
                this.unlockReason = ActionCameraUnlockReason.Ui;
                this.pendingRelock = true;
                this.suppressTargetOnNextRelock = true;
                suppressSoftTargeting = true;
            }
            else if (this.pendingRelock && togglePressed)
            {
                this.lockedModeActive = true;
                this.unlockReason = ActionCameraUnlockReason.None;
                this.pendingRelock = false;
                this.suppressTargetOnNextRelock = true;
                this.escForcedUnlockActive = false;
                this.escMenuTemporaryUnlock = false;
                this.escUnlockReleaseSeen = false;
                this.escRelockRequested = false;
            }

            if (this.escMenuTemporaryUnlock &&
                (this.escRelockRequested && !this.inputManager.IsEscPressed))
            {
                this.lockedModeActive = true;
                this.unlockReason = ActionCameraUnlockReason.None;
                this.pendingRelock = false;
                this.suppressTargetOnNextRelock = true;
                this.escForcedUnlockActive = false;
                this.escMenuTemporaryUnlock = false;
                this.escUnlockReleaseSeen = false;
                this.escRelockRequested = false;
                this.escOpenMenuRequested = false;
            }

            if (this.escMenuTemporaryUnlock || this.escForcedUnlockActive || this.inputManager.IsEscPressed)
            {
                suppressSoftTargeting = true;
            }

            if (this.escOpenMenuRequested)
            {
                if (this.uiStateService.IsMainMenuOpen)
                {
                    this.escOpenMenuRequested = false;
                }
                else if (!this.inputManager.IsEscPressed)
                {
                    this.uiStateService.TryOpenSystemMenu();
                    this.escOpenMenuRequested = false;
                }
            }

            this.SetActive(this.lockedModeActive);
            if (this.deferredSuppressTargetTap && !this.inputManager.IsAnyAltHeld)
            {
                this.ClearCurrentTarget();
                this.deferredSuppressTargetTap = false;
                this.suppressTargetOnNextRelock = false;
            }
            this.softTargetService.Update(!suppressSoftTargeting);

            this.pendingDeltaX = this.inputManager.MouseDeltaX;
            this.pendingDeltaY = this.inputManager.MouseDeltaY;
            var snapshot = this.ActiveBackend.GetSnapshot();

            this.runtimeState = new ActionCameraRuntimeState(
                this.active,
                this.active,
                holdUnlockHeld,
                uiFocused,
                this.inputManager.MouseDeltaX,
                this.inputManager.MouseDeltaY,
                snapshot.Yaw,
                snapshot.Pitch,
                snapshot.CanControl,
                snapshot.CameraWriteApplied,
                snapshot.CameraWritePersisted,
                snapshot.ReadbackYaw,
                snapshot.ReadbackPitch,
                this.ActiveBackend.Name,
                this.rmbLatchBackend.IsLatched,
                this.unlockReason,
                this.pendingRelock,
                this.updateTick,
                this.lateUpdateTick,
                this.runtimeState.LastError,
                this.softTargetService.Candidate.HasCandidate,
                this.softTargetService.Candidate.ObjectId,
                this.softTargetService.Candidate.ScreenPosition.X,
                this.softTargetService.Candidate.ScreenPosition.Y,
                this.softTargetService.Candidate.Score,
                this.softTargetService.DebugScannedCount,
                this.softTargetService.DebugEnemyCandidateCount,
                this.softTargetService.DebugEngagedCandidateCount,
                this.softTargetService.DebugLastRejectedObjectId,
                this.softTargetService.DebugLastRejectReason);
        }
        catch (Exception ex)
        {
            this.runtimeState = this.runtimeState with
            {
                LastError = $"Update:{ex.GetType().Name}",
                UpdateTick = this.updateTick,
                LateUpdateTick = this.lateUpdateTick,
            };
        }
    }

    /// <summary>
    /// Applies camera movement as late as possible in frame to reduce overwrite conflicts.
    /// </summary>
    public void LateUpdate()
    {
        this.lateUpdateTick++;
        if (!this.active)
        {
            this.runtimeState = this.runtimeState with
            {
                UpdateTick = this.updateTick,
                LateUpdateTick = this.lateUpdateTick,
            };
            return;
        }

        try
        {
            this.ActiveBackend.Tick(this.pendingDeltaX, this.pendingDeltaY);
        }
        catch (Exception ex)
        {
            this.runtimeState = this.runtimeState with
            {
                LastError = $"LateTick:{ex.GetType().Name}",
                UpdateTick = this.updateTick,
                LateUpdateTick = this.lateUpdateTick,
            };
            return;
        }

        var snapshot = this.ActiveBackend.GetSnapshot();
        if (!snapshot.CanControl)
        {
            this.log.Verbose("ActionCamera: camera update skipped (provider unavailable).");
        }

        this.runtimeState = this.runtimeState with
        {
            Yaw = snapshot.Yaw,
            Pitch = snapshot.Pitch,
            ProviderAvailable = snapshot.CanControl,
            CameraWriteApplied = snapshot.CameraWriteApplied,
            CameraWritePersisted = snapshot.CameraWritePersisted,
            ReadbackYaw = snapshot.ReadbackYaw,
            ReadbackPitch = snapshot.ReadbackPitch,
            BackendName = this.ActiveBackend.Name,
            IsLatched = this.rmbLatchBackend.IsLatched,
            UnlockReason = this.unlockReason,
            PendingRelock = this.pendingRelock,
            UpdateTick = this.updateTick,
            LateUpdateTick = this.lateUpdateTick,
            LastError = this.runtimeState.LastError,
        };

        this.pendingDeltaX = 0f;
        this.pendingDeltaY = 0f;
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.SetActive(false);
        this.cursorManager.Dispose();
    }

    private bool IsInWorld() =>
        this.clientState.IsLoggedIn &&
        this.objectTable.LocalPlayer is not null;

    private void SetActive(bool shouldBeActive)
    {
        if (this.active == shouldBeActive)
        {
            if (this.active)
            {
                if (this.suppressTargetOnNextRelock)
                {
                    if (this.inputManager.IsAnyAltHeld)
                    {
                        this.deferredSuppressTargetTap = true;
                    }
                    else
                    {
                        this.ClearCurrentTarget();
                        this.suppressTargetOnNextRelock = false;
                        this.deferredSuppressTargetTap = false;
                    }
                }
                this.cursorManager.Lock();
                this.cursorManager.Hide();
            }
            else
            {
                this.cursorManager.Unlock();
                this.cursorManager.Show();
            }

            return;
        }

        this.active = shouldBeActive;
        if (this.active)
        {
            if (this.suppressTargetOnNextRelock)
            {
                if (this.inputManager.IsAnyAltHeld)
                {
                    this.deferredSuppressTargetTap = true;
                }
                else
                {
                    this.ClearCurrentTarget();
                    this.suppressTargetOnNextRelock = false;
                    this.deferredSuppressTargetTap = false;
                }
            }
            this.cursorManager.Hide();
            this.cursorManager.Lock();
            this.ActiveBackend.Enable();
        }
        else
        {
            this.ActiveBackend.Disable();
            this.cursorManager.Unlock();
            this.cursorManager.Show();
        }
    }

    private ICameraControlBackend ActiveBackend =>
        this.rmbLatchBackend;

    private void ClearCurrentTarget()
    {
        this.targetManager.Target = null;
    }

}
