using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFXIVHudPlugin;

/// <summary>
/// Detects when game UI interaction should temporarily suspend action camera.
/// </summary>
internal sealed class UiStateService
{
    private readonly IGameGui gameGui;

    public UiStateService(IGameGui gameGui)
    {
        this.gameGui = gameGui;
    }

    /// <summary>
    /// True when a common mouse-driven game UI is visible.
    /// </summary>
    public bool IsUiFocused =>
        this.IsAddonVisible("Inventory") ||
        this.IsAddonVisible("InventoryLarge") ||
        this.IsAddonVisible("Character") ||
        this.IsAddonVisible("ItemSearch") ||
        this.IsAddonVisible("RetainerList") ||
        this.IsAddonVisible("RetainerTaskList") ||
        this.IsAddonVisible("ChatLogInput") ||
        this.IsAddonVisible("ChatInput");

    /// <summary>
    /// True while the Escape main command menu is visible.
    /// </summary>
    public bool IsMainMenuOpen =>
        this.IsAddonVisible("_MainCommand") ||
        this.IsAddonVisible("MainCommand");

    private unsafe bool IsAddonVisible(string addonName)
    {
        var addon = this.gameGui.GetAddonByName<AtkUnitBase>(addonName);
        if (addon is null)
        {
            return false;
        }

        return addon->IsVisible;
    }
}
