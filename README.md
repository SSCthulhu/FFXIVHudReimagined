# FFXIV Hud Reimagined

A Dalamud plugin that replaces the default FFXIV HUD with a custom layout: center HP orb, MP ring, mirrored hotbars, status lanes, class gauge, limit break display, and an optional custom minimap.

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

## Version history

See git commits and tags. Bump the version in `FFXIVHudPlugin.csproj` for each release you test in-game.

## License

Add a license file when you choose one for this repository.
