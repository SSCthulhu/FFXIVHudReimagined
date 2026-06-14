using Dalamud.Game.ClientState.Keys;
using Dalamud.Bindings.ImGui;
using DelvUI.Config;
using DelvUI.Config.Attributes;
using DelvUI.Helpers;
using System;
using System.Numerics;

namespace DelvUI.Interface.ActionCamera
{
    [Section("Action Camera")]
    [SubSection("General", 0)]
    public class ActionCameraConfig : PluginConfigObject
    {
        private bool _capturingUnlockKeybind = false;
        private int _pendingUnlockKeybind = ActionCameraKeyDefaults.UnlockKeybind;
        private bool _showUnlockConfirmModal = false;
        private bool _capturingLockTargetKeybind = false;
        private int _pendingLockTargetKeybind = ActionCameraKeyDefaults.LockTargetKeybind;
        private bool _showLockTargetConfirmModal = false;

        public ActionCameraUnlockMode UnlockMode = ActionCameraUnlockMode.Toggle;
        public bool UnlockOnUi = true;
        public bool UnlockWhenConfigOpen = true;
        public bool EscAlwaysUnlock = true;
        public bool ReacquireOnToggle = true;

        [Checkbox("Show Reticle", separator = true)]
        [Order(20, collapseWith = nameof(Enabled))]
        public bool ShowReticle = true;

        [DragFloat("Reticle Size", min = 2f, max = 30f, velocity = 0.2f)]
        [Order(21, collapseWith = nameof(ShowReticle))]
        public float ReticleSize = 6f;

        [ColorEdit4("Reticle Color")]
        [Order(22, collapseWith = nameof(ShowReticle))]
        public PluginConfigColor ReticleColor = new(new Vector4(1f, 1f, 1f, 0.8f));

        [Checkbox("Enable Soft Targeting", separator = true)]
        [Order(23, collapseWith = nameof(Enabled))]
        public bool EnableSoftTargetSuggestion = false;

        [Checkbox("Apply Soft Target to Actions")]
        [Order(24, collapseWith = nameof(EnableSoftTargetSuggestion))]
        public bool AutoTarget = true;

        [DragFloat("Soft Target Radius", min = 80f, max = 1200f, velocity = 1f)]
        [Order(25, collapseWith = nameof(EnableSoftTargetSuggestion))]
        public float SoftTargetScreenRadius = 280f;

        public ActionCameraBackendMode BackendMode = ActionCameraBackendMode.RmbLatch;
        public bool PreventRmbDisruption = true;

        [Checkbox("Restrict to Game Window", separator = true)]
        [Order(6, collapseWith = nameof(Enabled))]
        public bool RestrictToGameWindow = true;

        public bool ShowDebugOverlay = false;

        [ManualDraw]
        [ManualDrawPriority(44)]
        [ManualDrawParent(nameof(Enabled))]
        public bool DrawCameraSensitivityDisclaimer(ref bool changed)
        {
            ImGui.TextDisabled("Camera Sensitivity");
            ImGui.TextWrapped("Action camera turn speed follows FFXIV's native right-click camera behavior. Adjust turn speed using your mouse DPI and in-game camera settings.");
            return false;
        }

        [ManualDraw]
        [ManualDrawPriority(45)]
        [ManualDrawParent(nameof(Enabled))]
        public bool DrawKeybindSectionHeader(ref bool changed)
        {
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.95f, 0.85f, 0.35f, 1f), "Keybind");
            ImGui.Spacing();
            return false;
        }

        [ManualDraw]
        [ManualDrawPriority(50)]
        [ManualDrawParent(nameof(Enabled))]
        public bool DrawKeybinds(ref bool changed)
        {
            EnforceLockedDefaults();

            if (UnlockKeybind == 0)
            {
                UnlockKeybind = HoldUnlockKey != 0 ? HoldUnlockKey : (ToggleUnlockKey != 0 ? ToggleUnlockKey : ActionCameraKeyDefaults.UnlockKeybind);
            }
            HoldUnlockKey = UnlockKeybind;
            ToggleUnlockKey = UnlockKeybind;

            int unlockMode = (int)UnlockMode;
            if (ImGui.Combo("Unlock Mode", ref unlockMode, new[] { "Hold", "Toggle" }, 2))
            {
                UnlockMode = (ActionCameraUnlockMode)unlockMode;
                changed = true;
            }

            string keybindLabel = GetFriendlyKeyLabel((VirtualKey)UnlockKeybind);
            ImGui.TextDisabled("Unlock Keybind");
            ImGui.TextColored(new Vector4(0.95f, 0.95f, 0.95f, 1f), keybindLabel);
            ImGui.SameLine();

            if (ImGui.Button("Change..."))
            {
                _capturingUnlockKeybind = true;
            }

            if (_capturingUnlockKeybind)
            {
                ImGui.TextWrapped("Press any key to set your unlock keybind...");
                int capturedKey = TryCaptureAnyVirtualKey();
                if (capturedKey >= 0)
                {
                    _pendingUnlockKeybind = capturedKey;
                    _capturingUnlockKeybind = false;
                    _showUnlockConfirmModal = true;
                }
            }

            if (_showUnlockConfirmModal)
            {
                string[] lines = new string[]
                {
                    $"Apply new unlock keybind: {GetFriendlyKeyLabel((VirtualKey)_pendingUnlockKeybind)}?",
                    "This keybind is used for both Hold and Toggle modes."
                };

                var (didConfirm, didClose) = ImGuiHelper.DrawConfirmationModal("Apply Unlock Keybind?", lines);
                if (didConfirm)
                {
                    UnlockKeybind = _pendingUnlockKeybind;
                    HoldUnlockKey = UnlockKeybind;
                    ToggleUnlockKey = UnlockKeybind;
                    changed = true;
                }

                if (didClose)
                {
                    _showUnlockConfirmModal = false;
                }
            }

            ImGui.Spacing();
            string lockTargetKeybindLabel = LockTargetKeybind > 0 ? GetFriendlyKeyLabel((VirtualKey)LockTargetKeybind) : "Not Set";
            ImGui.TextDisabled("Lock Target Keybind");
            ImGui.TextColored(new Vector4(0.95f, 0.95f, 0.95f, 1f), lockTargetKeybindLabel);
            ImGui.SameLine();

            if (ImGui.Button("Change Lock Key..."))
            {
                _capturingLockTargetKeybind = true;
            }

            if (_capturingLockTargetKeybind)
            {
                ImGui.TextWrapped("Press any key to set your lock target keybind...");
                int capturedKey = TryCaptureAnyVirtualKey();
                if (capturedKey >= 0)
                {
                    _pendingLockTargetKeybind = capturedKey;
                    _capturingLockTargetKeybind = false;
                    _showLockTargetConfirmModal = true;
                }
            }

            if (_showLockTargetConfirmModal)
            {
                string[] lines = new string[]
                {
                    $"Apply new lock target keybind: {GetFriendlyKeyLabel((VirtualKey)_pendingLockTargetKeybind)}?",
                    "Press this key to lock or unlock the current action-camera target."
                };

                var (didConfirm, didClose) = ImGuiHelper.DrawConfirmationModal("Apply Lock Target Keybind?", lines);
                if (didConfirm)
                {
                    LockTargetKeybind = _pendingLockTargetKeybind;
                    changed = true;
                }

                if (didClose)
                {
                    _showLockTargetConfirmModal = false;
                }
            }

            return false;
        }

        [ManualDraw]
        [ManualDrawPriority(51)]
        [ManualDrawParent(nameof(Enabled))]
        public bool DrawUsageSectionHeader(ref bool changed)
        {
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text("Usage");
            ImGui.Spacing();
            return false;
        }

        [ManualDraw]
        [ManualDrawPriority(52)]
        [ManualDrawParent(nameof(Enabled))]
        public bool DrawUsageHint(ref bool changed)
        {
            EnforceLockedDefaults();
            ImGui.TextWrapped("Tip: In Toggle mode, camera starts locked when enabled and unlocks when you press the toggle key.");
            ImGui.TextWrapped("Lock Target: Press the lock target keybind to lock onto your current action-camera target. Press again to unlock.");
            ImGui.TextWrapped("Note: If 'Unlock when Aether UI settings open' is enabled, action camera unlocks while this settings window is open.");
            ImGui.TextWrapped("Safety: Action Camera now auto-releases mouse lock whenever FFXIV is not the active foreground window.");
            ImGui.TextWrapped("Restrict to Game Window anchors the cursor to the FFXIV viewport center while locked, keeping camera control inside the game window.");
            return false;
        }

        public int UnlockKeybind = ActionCameraKeyDefaults.UnlockKeybind;
        public int LockTargetKeybind = ActionCameraKeyDefaults.LockTargetKeybind;
        public int HoldUnlockKey = ActionCameraKeyDefaults.UnlockKeybind;
        public int ToggleUnlockKey = ActionCameraKeyDefaults.UnlockKeybind;

        public new static ActionCameraConfig DefaultConfig() => new()
        {
            Enabled = false,
            RestrictToGameWindow = true
        };

        public void EnforceLockedDefaults()
        {
            UnlockOnUi = true;
            UnlockWhenConfigOpen = true;
            EscAlwaysUnlock = true;
            ReacquireOnToggle = true;
            BackendMode = ActionCameraBackendMode.RmbLatch;
            PreventRmbDisruption = true;
            ShowDebugOverlay = false;
            if (SoftTargetScreenRadius < 80f)
            {
                SoftTargetScreenRadius = 80f;
            }
            else if (SoftTargetScreenRadius > 1200f)
            {
                SoftTargetScreenRadius = 1200f;
            }
        }

        private static string GetFriendlyKeyLabel(VirtualKey key)
        {
            return key switch
            {
                VirtualKey.MENU or VirtualKey.LMENU or VirtualKey.RMENU => "Alt",
                VirtualKey.CAPITAL => "Caps Lock",
                VirtualKey.LCONTROL or VirtualKey.RCONTROL or VirtualKey.CONTROL => "Ctrl",
                VirtualKey.LSHIFT or VirtualKey.RSHIFT or VirtualKey.SHIFT => "Shift",
                _ => key.ToString()
            };
        }

        private static int TryCaptureAnyVirtualKey()
        {
            for (int i = 1; i < 256; i++)
            {
                if (i is 0x01 or 0x02 or 0x04 or 0x05 or 0x06)
                {
                    // Avoid capturing mouse buttons as unlock keys.
                    continue;
                }

                var key = (VirtualKey)i;
                try
                {
                    if (Plugin.KeyState[key])
                    {
                        return i;
                    }
                }
                catch (ArgumentException)
                {
                    // Ignore invalid/unhandled key enum entries.
                }
            }

            return -1;
        }
    }
}
