using Dalamud.Game.Command;

namespace FFXIVHudPlugin;

internal static class PluginCommands
{
    public const string MainCommand = "/ffxivhud";
    public const string ActionCameraCommand = "/actioncam";

    public static CommandInfo CreateCommand(Action toggleConfig) =>
        new((_, _) => toggleConfig())
        {
            HelpMessage = "Toggle the FFXIV Hud Reimagined configuration window.",
            ShowInHelp = true
        };

    public static CommandInfo CreateActionCameraCommand(Action toggleActionCamera) =>
        new((_, _) => toggleActionCamera())
        {
            HelpMessage = "Toggle Action Camera mode.",
            ShowInHelp = true
        };
}
