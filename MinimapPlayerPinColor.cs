using Dalamud.Plugin.Services;

namespace FFXIVHudPlugin;

internal static class MinimapPlayerPinColor
{
    public static uint Resolve(HudConfiguration config, uint classJobId, IDataManager dataManager)
    {
        if (!config.MinimapUseRolePinColor)
        {
            return config.MinimapPlayerPinColor;
        }

        if (MinimapRoleColor.TryResolveArgb(dataManager, classJobId, out var roleColor))
        {
            return roleColor;
        }

        return config.MinimapPlayerPinColor;
    }
}
