using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class ActionCameraUiStateService
    {
        public bool IsDalamudOrPluginUiActive
        {
            get
            {
                var io = ImGui.GetIO();
                return io.WantCaptureMouse || io.WantTextInput || io.WantCaptureKeyboard;
            }
        }

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
