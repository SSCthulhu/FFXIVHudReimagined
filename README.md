# Aether UI

Aether UI is a Dalamud plugin that provides a standalone customizable HUD with nameplate parity, action camera controls, and profile-safe backend behavior.

## Requirements

- Final Fantasy XIV with [Dalamud](https://github.com/goatcorp/Dalamud) installed
- .NET SDK matching the project target (see `FFXIVHudPlugin.csproj`)

## Build

```bash
dotnet build -c Debug
```

Copy or symlink the output from `bin/Debug/` into your Dalamud dev plugins folder, then reload in-game with `/xlreload`.

## Configuration

Open the plugin config from the Dalamud plugin installer or use the in-game command defined in `PluginCommands.cs`.

## Experimental Repo Install (No Build Needed)

For testers, install directly from Dalamud's Custom Plugin Repositories without cloning or building.

- Add this URL in `/xlsettings` -> `Experimental` -> `Custom Plugin Repositories`:
  - `https://raw.githubusercontent.com/SSCthulhu/FFXIVHudReimagined/main/pluginmaster.json`
- Open Dalamud plugin installer, refresh plugin lists, and install `Aether UI`.

### Publishing updates for testers

1. Build/release the latest `AetherUI.zip` asset from the Aether UI source plugin.
2. Commit and push to `main`.
3. Create and push a matching git tag (example: `v2.7.0.1-aether.0`):
   - `git tag v2.7.0.1-aether.0`
   - `git push origin v2.7.0.1-aether.0`
4. Publish the `AetherUI.zip` release asset.
5. Dalamud users get install/update from the same experimental repo URL.

For the full repeatable process, use `PUBLISH_CHECKLIST.md`.

## Action Camera Plugin (Standalone Module)

This repository now includes an isolated Action Camera feature that does not alter existing HUD logic.

- **Command**: `/actioncam` toggles action camera mode.
- **Settings tab**: `Action Camera` tab in the existing config window.
- **Direct camera path**: uses `FFXIVClientStructs` scene camera look-vector writes through `ICameraProvider`.
- **Fallback strategy**: camera access is abstracted behind `ICameraProvider` for game-version-safe replacement.

### Behavior

- Locks and hides cursor while action camera is active.
- Reads raw mouse delta each frame and applies yaw/pitch updates.
- Holding Alt temporarily releases cursor (configurable).
- UI visibility can auto-release cursor (configurable).
- Pressing Escape disables action camera until reactivated (right click by default, configurable).
- Separate horizontal and vertical sensitivity (`0.1` to `5.0`).
- Optional center reticle and debug overlay.

### Architecture

- `ActionCameraPlugin`: lifecycle/state orchestration.
- `InputManager`: key and mouse delta capture.
- `UiStateService`: common UI-focus detection.
- `CursorManager`: show/hide, lock/unlock, recenter.
- `CameraController`: yaw/pitch integration and clamping.
- `ICameraProvider` / `FfxivClientStructsCameraProvider`: direct camera reads/writes.
- `ActionCameraOverlay`: optional reticle/debug rendering.

## Version history

See git commits and tags. Keep pluginmaster `AssemblyVersion` aligned with the released Aether UI plugin version.

## License

Add a license file when you choose one for this repository.
