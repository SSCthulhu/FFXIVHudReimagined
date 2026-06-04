using Dalamud.Game.Command;

namespace FFXIVHudPlugin;

internal static class PluginCommands
{
    public const string MainCommand = "/ffxivhud";

    public static CommandInfo CreateCommand(Action toggleConfig) =>
        new((_, _) => toggleConfig())
        {
            HelpMessage = "Toggle the FFXIV Hud Reimagined configuration window.",
            ShowInHelp = true
        };
}
