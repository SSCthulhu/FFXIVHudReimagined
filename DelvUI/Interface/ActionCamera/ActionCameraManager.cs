using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using DelvUI.Config;
using System;

namespace DelvUI.Interface.ActionCamera
{
    public sealed class ActionCameraManager : IDisposable
    {
        public static ActionCameraManager? Instance { get; private set; }

        private readonly ActionCameraConfig _config;
        private readonly ActionCameraInputManager _inputManager;
        private readonly ActionCameraUiStateService _uiStateService;
        private readonly ActionCameraCursorManager _cursorManager;
        private readonly ActionCameraSoftTargetService _softTargetService;
        private readonly CameraController _cameraController;
        private readonly RmbLatchCameraBackend _rmbLatchBackend;
        private readonly DirectCameraControlBackend _directBackend;
        private bool _active;
        private bool _lockedModeActive;
        private bool _pendingRelock;
        private bool _disposed;
        private ActionCameraUnlockReason _unlockReason;
        private ActionCameraRuntimeState _runtimeState;
        private long _updateTick;
        private long _lateUpdateTick;
        private float _pendingDeltaX;
        private float _pendingDeltaY;
        private bool _previousEnabledState;

        public ActionCameraManager()
        {
            Instance = this;
            _config = ConfigurationManager.Instance.GetConfigObject<ActionCameraConfig>();
            _inputManager = new ActionCameraInputManager(_config);
            _uiStateService = new ActionCameraUiStateService();
            _cursorManager = new ActionCameraCursorManager();
            _softTargetService = new ActionCameraSoftTargetService(_config);

            ICameraProvider provider = new FfxivClientStructsCameraProvider();
            _cameraController = new CameraController(_config, provider);
            _rmbLatchBackend = new RmbLatchCameraBackend(_config);
            _directBackend = new DirectCameraControlBackend(_cameraController);

            _runtimeState = new ActionCameraRuntimeState(
                false,
                false,
                false,
                false,
                0f,
                0f,
                0f,
                0f,
                false,
                false,
                false,
                0f,
                0f,
                ActiveBackend.Name,
                false,
                ActionCameraUnlockReason.None,
                false,
                0,
                0,
                string.Empty);
        }

        public ActionCameraRuntimeState RuntimeState => _runtimeState;
        public bool HasLockedTarget => _softTargetService.HasLockedTarget;

        public bool IsLockedTarget(IGameObject? actor)
        {
            if (!HasLockedTarget || actor == null)
            {
                return false;
            }

            return (uint)actor.GameObjectId == _softTargetService.LockedObjectId;
        }

        public void Update()
        {
            try
            {
                _config.EnforceLockedDefaults();
                _updateTick++;
                _inputManager.Update();

                if (!ShouldRun())
                {
                    _lockedModeActive = false;
                    _unlockReason = ActionCameraUnlockReason.None;
                    _pendingRelock = false;
                    _previousEnabledState = _config.Enabled;
                    SetActive(false);
                    _softTargetService.ClearAll();
                    _pendingDeltaX = 0f;
                    _pendingDeltaY = 0f;
                    UpdateRuntimeSnapshot(false, false);
                    return;
                }

                if (!_cursorManager.IsGameWindowForeground())
                {
                    _lockedModeActive = false;
                    _unlockReason = ActionCameraUnlockReason.Ui;
                    _pendingRelock = true;
                    SetActive(false);
                    _softTargetService.ClearAll();
                    _pendingDeltaX = 0f;
                    _pendingDeltaY = 0f;
                    UpdateRuntimeSnapshot(false, true);
                    return;
                }

                if (_config.Enabled && !_previousEnabledState)
                {
                    _lockedModeActive = true;
                    _pendingRelock = false;
                    _unlockReason = ActionCameraUnlockReason.None;
                }

                _previousEnabledState = _config.Enabled;

                bool holdUnlockHeld = _inputManager.IsHoldUnlockActive();
                bool gameUiFocused = _config.UnlockOnUi && _uiStateService.IsUiFocused;
                bool pluginUiActive = _uiStateService.IsDalamudOrPluginUiActive;
                bool uiFocused = gameUiFocused || pluginUiActive;
                bool togglePressed = _inputManager.ConsumeToggleUnlockPressed();
                bool lockTargetPressed = _inputManager.ConsumeToggleLockTargetPressed();
                _inputManager.ConsumeEscPressedEdge();

                if (_config.EscAlwaysUnlock && _inputManager.IsEscPressed)
                {
                    _lockedModeActive = false;
                    _unlockReason = ActionCameraUnlockReason.Escape;
                    _pendingRelock = true;
                }
                else if (_config.UnlockMode == ActionCameraUnlockMode.Hold)
                {
                    _lockedModeActive = !holdUnlockHeld;
                    _unlockReason = _lockedModeActive ? ActionCameraUnlockReason.None : ActionCameraUnlockReason.Toggle;
                }
                else if (togglePressed)
                {
                    if (!_lockedModeActive && !_config.ReacquireOnToggle)
                    {
                        _pendingRelock = true;
                    }
                    else
                    {
                        _lockedModeActive = !_lockedModeActive;
                        _pendingRelock = false;
                        _unlockReason = _lockedModeActive ? ActionCameraUnlockReason.None : ActionCameraUnlockReason.Toggle;
                    }
                }

                if (uiFocused || (_config.UnlockWhenConfigOpen && ConfigurationManager.Instance.IsConfigWindowOpened))
                {
                    _lockedModeActive = false;
                    _unlockReason = ActionCameraUnlockReason.Ui;
                    _pendingRelock = true;
                }

                SetActive(_lockedModeActive);
                if (lockTargetPressed && _active)
                {
                    _softTargetService.ToggleLockedTarget();
                }
                _softTargetService.Update(_active && _config.EnableSoftTargetSuggestion);

                _pendingDeltaX = _inputManager.MouseDeltaX;
                _pendingDeltaY = _inputManager.MouseDeltaY;
                UpdateRuntimeSnapshot(holdUnlockHeld, uiFocused);
            }
            catch (Exception ex)
            {
                _runtimeState = _runtimeState with
                {
                    LastError = $"Update:{ex.GetType().Name}",
                    UpdateTick = _updateTick,
                    LateUpdateTick = _lateUpdateTick
                };
            }
        }

        public void LateUpdate()
        {
            _lateUpdateTick++;

            if (!_active)
            {
                _runtimeState = _runtimeState with
                {
                    UpdateTick = _updateTick,
                    LateUpdateTick = _lateUpdateTick
                };
                return;
            }

            try
            {
                ActiveBackend.Tick(_pendingDeltaX, _pendingDeltaY);
                var snapshot = ActiveBackend.GetSnapshot();
                _runtimeState = _runtimeState with
                {
                    Yaw = snapshot.Yaw,
                    Pitch = snapshot.Pitch,
                    ProviderAvailable = snapshot.CanControl,
                    CameraWriteApplied = snapshot.CameraWriteApplied,
                    CameraWritePersisted = snapshot.CameraWritePersisted,
                    ReadbackYaw = snapshot.ReadbackYaw,
                    ReadbackPitch = snapshot.ReadbackPitch,
                    BackendName = ActiveBackend.Name,
                    IsLatched = _rmbLatchBackend.IsLatched,
                    UpdateTick = _updateTick,
                    LateUpdateTick = _lateUpdateTick
                };
            }
            catch (Exception ex)
            {
                _runtimeState = _runtimeState with
                {
                    LastError = $"LateTick:{ex.GetType().Name}",
                    UpdateTick = _updateTick,
                    LateUpdateTick = _lateUpdateTick
                };
            }
            finally
            {
                _pendingDeltaX = 0f;
                _pendingDeltaY = 0f;
            }
        }

        public void DrawOverlay()
        {
            ActionCameraOverlay.Draw(_config, _runtimeState);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            SetActive(false);
            _softTargetService.ClearAll();
            _cursorManager.Dispose();
            Instance = null;
        }

        private bool ShouldRun()
        {
            if (!_config.Enabled || Plugin.ObjectTable.LocalPlayer == null || !ConfigurationManager.Instance.ShowHUD)
            {
                return false;
            }

            return !(Plugin.Condition[ConditionFlag.WatchingCutscene] ||
                     Plugin.Condition[ConditionFlag.WatchingCutscene78] ||
                     Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                     Plugin.Condition[ConditionFlag.CreatingCharacter] ||
                     Plugin.Condition[ConditionFlag.BetweenAreas] ||
                     Plugin.Condition[ConditionFlag.BetweenAreas51]);
        }

        private void SetActive(bool shouldBeActive)
        {
            if (_active == shouldBeActive)
            {
                if (_active)
                {
                    _cursorManager.Lock();
                    _cursorManager.Hide();
                }
                else
                {
                    _cursorManager.Unlock();
                    _cursorManager.Show();
                }

                return;
            }

            _active = shouldBeActive;
            if (_active)
            {
                _cursorManager.Hide();
                _cursorManager.Lock();
                ActiveBackend.Enable();
            }
            else
            {
                ActiveBackend.Disable();
                _softTargetService.ClearAll();
                _cursorManager.Unlock();
                _cursorManager.Show();
            }
        }

        private ICameraControlBackend ActiveBackend =>
            _config.BackendMode == ActionCameraBackendMode.DirectExperimental ? _directBackend : _rmbLatchBackend;

        private void UpdateRuntimeSnapshot(bool holdUnlockHeld, bool uiFocused)
        {
            var snapshot = ActiveBackend.GetSnapshot();
            _runtimeState = new ActionCameraRuntimeState(
                _active,
                _active,
                holdUnlockHeld,
                uiFocused,
                _inputManager.MouseDeltaX,
                _inputManager.MouseDeltaY,
                snapshot.Yaw,
                snapshot.Pitch,
                snapshot.CanControl,
                snapshot.CameraWriteApplied,
                snapshot.CameraWritePersisted,
                snapshot.ReadbackYaw,
                snapshot.ReadbackPitch,
                ActiveBackend.Name,
                _rmbLatchBackend.IsLatched,
                _unlockReason,
                _pendingRelock,
                _updateTick,
                _lateUpdateTick,
                _runtimeState.LastError);
        }
    }
}
