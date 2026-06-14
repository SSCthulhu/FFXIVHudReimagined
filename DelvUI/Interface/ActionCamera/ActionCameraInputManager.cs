using Dalamud.Game.ClientState.Keys;
using Dalamud.Bindings.ImGui;
using System;
using System.Runtime.InteropServices;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class ActionCameraInputManager
    {
        private readonly ActionCameraConfig _config;
        private bool _toggleUnlockPrevious;
        private bool _toggleLockTargetPrevious;
        private bool _escPressed;
        private bool _escPressedPrevious;
        private bool _altHeld;
        private float _mouseDeltaX;
        private float _mouseDeltaY;

        public ActionCameraInputManager(ActionCameraConfig config)
        {
            _config = config;
        }

        public bool IsEscPressed => _escPressed;
        public float MouseDeltaX => _mouseDeltaX;
        public float MouseDeltaY => _mouseDeltaY;
        public bool IsAnyAltHeld => _altHeld;

        public void Update()
        {
            var io = ImGui.GetIO();
            _mouseDeltaX = io.MouseDelta.X;
            _mouseDeltaY = io.MouseDelta.Y;

            _altHeld = SafeIsKeyDown(VirtualKey.LMENU) ||
                       SafeIsKeyDown(VirtualKey.RMENU) ||
                       SafeIsKeyDown(VirtualKey.MENU);

            _escPressedPrevious = _escPressed;
            _escPressed = SafeIsKeyDown(VirtualKey.ESCAPE);
        }

        public bool IsHoldUnlockActive()
        {
            return IsConfiguredKeyDown(GetUnifiedUnlockKeybind());
        }

        public bool ConsumeToggleUnlockPressed()
        {
            bool nowPressed = IsConfiguredKeyDown(GetUnifiedUnlockKeybind());
            bool rising = nowPressed && !_toggleUnlockPrevious;
            _toggleUnlockPrevious = nowPressed;
            return rising;
        }

        public bool ConsumeEscPressedEdge()
        {
            return _escPressed && !_escPressedPrevious;
        }

        public bool ConsumeToggleLockTargetPressed()
        {
            bool nowPressed = IsConfiguredKeyDown(_config.LockTargetKeybind);
            bool rising = nowPressed && !_toggleLockTargetPrevious;
            _toggleLockTargetPrevious = nowPressed;
            return rising;
        }

        private static bool SafeIsKeyDown(VirtualKey key)
        {
            bool down = false;
            try
            {
                down = Plugin.KeyState[key];
            }
            catch (ArgumentException)
            {
                down = false;
            }

            // Fallback for edge cases where the high-level key state can miss
            // transitions while the game is in relative-input modes.
            if (!down)
            {
                down = (GetAsyncKeyState((int)key) & 0x8000) != 0;
            }

            return down;
        }

        private static bool IsConfiguredKeyDown(int keyCode)
        {
            var key = (VirtualKey)keyCode;

            return key switch
            {
                VirtualKey.MENU or VirtualKey.LMENU or VirtualKey.RMENU =>
                    SafeIsKeyDown(VirtualKey.MENU) || SafeIsKeyDown(VirtualKey.LMENU) || SafeIsKeyDown(VirtualKey.RMENU),

                VirtualKey.CONTROL or VirtualKey.LCONTROL or VirtualKey.RCONTROL =>
                    SafeIsKeyDown(VirtualKey.CONTROL) || SafeIsKeyDown(VirtualKey.LCONTROL) || SafeIsKeyDown(VirtualKey.RCONTROL),

                VirtualKey.SHIFT or VirtualKey.LSHIFT or VirtualKey.RSHIFT =>
                    SafeIsKeyDown(VirtualKey.SHIFT) || SafeIsKeyDown(VirtualKey.LSHIFT) || SafeIsKeyDown(VirtualKey.RSHIFT),

                _ => SafeIsKeyDown(key)
            };
        }

        private int GetUnifiedUnlockKeybind()
        {
            if (_config.UnlockKeybind != 0)
            {
                return _config.UnlockKeybind;
            }

            // Backward compatibility for older configs.
            if (_config.HoldUnlockKey != 0)
            {
                return _config.HoldUnlockKey;
            }

            if (_config.ToggleUnlockKey != 0)
            {
                return _config.ToggleUnlockKey;
            }

            return ActionCameraKeyDefaults.UnlockKeybind;
        }

        [DllImport("user32.dll", SetLastError = false)]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
