# Aether UI Action Camera Integration Plan

This document is the implementation draft for integrating the Action Camera system from `ffxiv-dalamud-hud` into `Aether UI`.

Goal: ship a stable MVP first, then layer in soft-targeting and polish without destabilizing existing HUD features.

---

## 1) Product Scope

### In Scope
- Action camera runtime manager (toggle/hold unlock behavior).
- Cursor lock/show management.
- Reticle overlay.
- Safe gating during cutscenes, zoning, and incompatible UI states.
- Aether-native configuration and command integration.

### Out of Scope (Initial)
- New combat mechanics.
- Full backend experimentation in v1 (RMB latch path is the default).
- Large refactors to HUD element pipeline.

---

## 2) Architecture Fit in Aether UI

Action Camera must be a **parallel runtime subsystem**, not a `HudElement`.

- Keep `HudManager` responsible for HUD draw and actor assignment.
- Add a dedicated manager owned by `Plugin`.
- Run action-camera update before HUD draw, then draw overlay after HUD.
- Keep config in `ConfigurationManager` via `PluginConfigObject`.

This preserves Aether UI patterns and minimizes coupling to existing HUD components.

---

## 3) File and Class Plan

Create a new folder:

- `DelvUI/Interface/ActionCamera/`

New files:

- `ActionCameraManager.cs`
- `ActionCameraConfig.cs`
- `ActionCameraEnums.cs`
- `ActionCameraRuntimeState.cs`
- `ActionCameraBackendSnapshot.cs`
- `ActionCameraOverlay.cs`
- `ActionCameraInputManager.cs`
- `ActionCameraCursorManager.cs`
- `ActionCameraUiStateService.cs`
- `ActionCameraSoftTargetService.cs`
- `ActionCameraSoftTargetCandidate.cs`
- `ICameraControlBackend.cs`
- `RmbLatchCameraBackend.cs`
- `DirectCameraControlBackend.cs`
- `CameraController.cs`
- `ICameraProvider.cs`
- `FfxivClientStructsCameraProvider.cs`

Recommended namespace:

- `DelvUI.Interface.ActionCamera`

---

## 4) Plugin Wiring Plan

Update `Plugin`:

1. Add `IKeyState` service injection and static property.
2. Create private field:
   - `_actionCameraManager`
3. Initialize after `InputsHelper.Initialize()` and before `UiBuilder.Draw += Draw`.
4. In `Draw()`:
   - `UpdateJob()`
   - `ConfigurationManager.Instance.Draw()`
   - `NameplatesManager.Instance?.Update()`
   - `PartyManager.Instance?.Update()`
   - `_actionCameraManager?.Update()`
   - `_actionCameraManager?.LateUpdate()`
   - `_hudManager?.Draw(_jobId)`
   - `_actionCameraManager?.DrawOverlay()`
   - `InputsHelper.Instance.OnFrameEnd()`
5. Dispose before `InputsHelper` teardown (to guarantee cursor restore and backend disable).

Notes:
- Keep `UiBuilder.OverrideGameCursor = false`.
- `ActionCameraManager.Dispose()` must always unclip/show cursor even on exceptions.

---

## 5) Configuration Schema (Aether Native)

Add config type:

- `ActionCameraConfig : PluginConfigObject`

Attributes:

- `[Section("Action Camera")]`
- `[SubSection("General", 0)]`

Suggested fields:

- `public bool Enabled = false;` (inherited from `PluginConfigObject`; do not redeclare)
- `public ActionCameraUnlockMode UnlockMode = ActionCameraUnlockMode.Toggle;`
- `public int ToggleUnlockKey = (int)VirtualKey.CAPITAL;`
- `public int HoldUnlockKey = (int)VirtualKey.MENU;`
- `public bool UnlockOnUi = true;`
- `public bool EscAlwaysUnlock = true;`
- `public bool ReacquireOnToggle = true;`
- `public bool UnlockWhenConfigOpen = true;`
- `public bool EnableSoftTargetSuggestion = false;`
- `public bool AutoTarget = false;`
- `public float SoftTargetScreenRadius = 90f;`
- `public bool ShowReticle = true;`
- `public float ReticleSize = 16f;`
- `public PluginConfigColor ReticleColor = ...`
- `public ActionCameraBackendMode BackendMode = ActionCameraBackendMode.RmbLatch;`
- `public bool PreventRmbDisruption = true;`
- `public bool ShowDebugOverlay = false;`

Config-tree registration:

- Add `typeof(ActionCameraConfig)` to `ConfigurationManager.ConfigObjectTypes` near misc systems.

Keybind UI:

- Aether config attributes do not include native keybind pickers today.
- Implement key pickers via `ManualDraw` methods in `ActionCameraConfig` and persist keys as `int`.

---

## 6) Command Integration

Extend `PluginCommand`:

- `/aetherui actioncam` toggles `ActionCameraConfig.Enabled`.
- `/aetherui actioncam on|off`
- `/aetherui actioncam debug on|off`

Mirror alias support for `/aui`.

Chat responses:

- Print clear state changes: `Action Camera is enabled/disabled`.

---

## 7) Compatibility Rules

### Aether HUD and Visibility
- Disable action camera when `ShowHUD` is false.
- Disable during cutscenes/zoning states used by `HudManager.ShouldBeVisible()`.
- Disable when local player is missing.

### Aether Config Window
- If config window is open and `UnlockWhenConfigOpen`, force unlocked state.

### InputsHelper Mouseover
- If `HUDOptionsConfig.MouseoverEnabled && MouseoverAutomaticMode` and action camera is active:
  - either temporarily disable soft-target suggestion, or
  - force unlock when cursor is over Aether interactive areas.
- Do not break existing mouseover action redirection behavior.

---

## 8) Backend Behavior Plan

### MVP Default
- Use `RmbLatchCameraBackend` as default production path.

### Backend Selection
- `ActionCameraManager` must respect `BackendMode`.
- If selected backend unavailable, fallback to RMB latch and write a warning log once.

### Sensitivity
- Do not expose non-functional sensitivity controls in MVP if backend ignores them.
- If kept in UI, annotate behavior:
  - RMB latch uses game look sensitivity.
  - direct backend uses plugin sensitivity.

---

## 9) Phase Plan

### Phase 0: Spike
- Add folder and compileable classes.
- Wire manager lifecycle into `Plugin`.
- Hardcoded enable flag in manager for smoke checks.
- Confirm safe dispose and cursor restoration.

Exit criteria:
- Builds cleanly.
- No stuck cursor after plugin unload/reload.

### Phase 1: MVP
- Add `ActionCameraConfig` and register config type.
- Implement toggle/hold unlock, ESC behavior, UI unlock.
- Add reticle overlay.
- Add command handlers.
- Add cutscene/zoning/config-window gates.

Exit criteria:
- Feature is usable with stable lock/unlock flow.
- No regressions to existing HUD draw or command behavior.

### Phase 2: Soft-Target + Polish
- Integrate `ActionCameraSoftTargetService`.
- Add debug overlay and diagnostics.
- Improve cursor centering for multi-monitor/windowed scenarios.
- Add conflict guardrails with `InputsHelper`.
- Add profile import/export verification for new config.

Exit criteria:
- Soft-target is stable and non-jittery.
- Known edge cases documented and recoverable.

---

## 10) Coding Sequence (Start Here)

1. Add `IKeyState` to `Plugin` constructor and static property.
2. Create `Interface/ActionCamera/` folder and stub classes:
   - `ActionCameraManager`, `ActionCameraConfig`, enums/runtime structs.
3. Register `ActionCameraConfig` in `ConfigurationManager.ConfigObjectTypes`.
4. Wire manager initialize/update/draw/dispose in `Plugin`.
5. Add `/aetherui actioncam` command branch.
6. Implement unlock FSM and cursor manager.
7. Add overlay.
8. Implement backend selection + fallback.
9. Add soft-target service.
10. Final cleanup, logs, and changelog entry.

---

## 11) Verification Checklist (No Automation)

- Toggle command works from both `/aetherui` and `/aui`.
- Action camera disables in cutscene, zoning, and no-player states.
- ESC unlock flow behaves predictably.
- Aether config open forces unlock when configured.
- HUD still draws correctly with action camera on/off.
- Mouseover mode remains usable and not permanently broken.
- Plugin unload/reload always restores cursor.

---

## 12) Known Risks and Mitigations

- **Cursor stuck/hidden**: enforce restore in `Dispose()` and exception paths.
- **Input conflicts**: integrate explicit rules with `InputsHelper`.
- **Patch breakage in structs**: isolate memory operations in `ICameraProvider`.
- **Dead settings**: hide or annotate backend/sensitivity options until active.

---

## 13) Definition of Done (MVP)

- Action camera can be enabled from config/command.
- Stable lock/unlock and cursor behavior.
- No regressions in Aether UI HUD rendering or command parsing.
- Clear docs for known limitations and recommended settings.
- Code ready for Phase 2 soft-target enhancement.
