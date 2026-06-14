using System;
using System.Runtime.InteropServices;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class RmbLatchCameraBackend : ICameraControlBackend
    {
        private const int VKeyRButton = 0x02;
        private readonly ActionCameraConfig _config;
        private bool _enabled;
        private bool _latchedRmb;
        private bool _previousPhysicalRmbDown;
        private float _yaw;
        private float _pitch;

        public RmbLatchCameraBackend(ActionCameraConfig config)
        {
            _config = config;
        }

        public string Name => "RmbLatch";
        public bool CanControl => true;
        public bool IsLatched => _latchedRmb;

        public void Enable()
        {
            _enabled = true;
            _previousPhysicalRmbDown = IsPhysicalRightMouseDown();
            EnsureLatched();
        }

        public void Disable()
        {
            ReleaseLatch();
            _enabled = false;
            _previousPhysicalRmbDown = false;
        }

        public void Tick(float deltaX, float deltaY)
        {
            if (!_enabled)
            {
                return;
            }

            MaintainLatchState();

            _yaw += deltaX * 0.00425f;
            _pitch = Math.Clamp(_pitch - (deltaY * 0.00425f), -1.35f, 1.35f);
        }

        public ActionCameraBackendSnapshot GetSnapshot()
        {
            return new ActionCameraBackendSnapshot(
                true,
                _latchedRmb,
                _latchedRmb,
                _yaw,
                _pitch,
                _yaw,
                _pitch);
        }

        private void EnsureLatched()
        {
            if (_latchedRmb)
            {
                return;
            }

            mouse_event(MouseEventfRightDown, 0, 0, 0, 0);
            _latchedRmb = true;
        }

        private void MaintainLatchState()
        {
            bool physicalRmbDown = IsPhysicalRightMouseDown();
            if (!_config.PreventRmbDisruption)
            {
                _previousPhysicalRmbDown = physicalRmbDown;
                return;
            }

            if (_previousPhysicalRmbDown && !physicalRmbDown)
            {
                RelatchPulse();
            }

            _previousPhysicalRmbDown = physicalRmbDown;
        }

        private void RelatchPulse()
        {
            mouse_event(MouseEventfRightUp, 0, 0, 0, 0);
            mouse_event(MouseEventfRightDown, 0, 0, 0, 0);
            _latchedRmb = true;
        }

        private static bool IsPhysicalRightMouseDown()
        {
            return (GetAsyncKeyState(VKeyRButton) & 0x8000) != 0;
        }

        private void ReleaseLatch()
        {
            if (!_latchedRmb)
            {
                return;
            }

            mouse_event(MouseEventfRightUp, 0, 0, 0, 0);
            _latchedRmb = false;
        }

        private const uint MouseEventfRightDown = 0x0008;
        private const uint MouseEventfRightUp = 0x0010;

        [DllImport("user32.dll", SetLastError = false)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, nuint dwExtraInfo);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
