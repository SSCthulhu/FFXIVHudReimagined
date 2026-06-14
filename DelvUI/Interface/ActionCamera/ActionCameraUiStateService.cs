using DelvUI.Config;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class ActionCameraUiStateService
    {
        public bool IsAetherSettingsOpen => ConfigurationManager.Instance.IsConfigWindowOpened;

        public bool IsPluginMouseCaptureRequested => ImGui.GetIO().WantCaptureMouse;

        public bool IsDalamudOrPluginUiActive =>
            IsAetherSettingsOpen || IsPluginMouseCaptureRequested;

        public bool IsUiFocused =>
            IsAddonVisible("Inventory") ||
            IsAddonVisible("InventoryLarge") ||
            IsAddonVisible("Character") ||
            IsAddonVisible("ItemSearch") ||
            IsAddonVisible("RetainerList") ||
            IsAddonVisible("RetainerTaskList") ||
            IsAddonVisible("ChatLogInput") ||
            IsAddonVisible("ChatInput");

        public bool IsMainMenuOpen =>
            IsAddonVisible("_MainCommand") ||
            IsAddonVisible("MainCommand");

        public unsafe bool TryOpenSystemMenu()
        {
            if (IsMainMenuOpen)
            {
                return true;
            }

            var agentHud = AgentHUD.Instance();
            if (agentHud is null)
            {
                return false;
            }

            return agentHud->HandleMainCommandOperation(
                MainCommandOperation.OpenSystemMenu,
                0u,
                -1,
                null);
        }

        private unsafe bool IsAddonVisible(string addonName)
        {
            var addon = Plugin.GameGui.GetAddonByName<AtkUnitBase>(addonName);
            if (addon is null)
            {
                return false;
            }

            return addon->IsVisible;
        }
    }
}
