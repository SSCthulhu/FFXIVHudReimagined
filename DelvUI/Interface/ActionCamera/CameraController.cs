using System;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class CameraController
    {
        private const float PitchMin = -1.35f;
        private const float PitchMax = 1.35f;
        private const float BaseSensitivity = 0.00425f;
        private readonly ActionCameraConfig _config;
        private readonly ICameraProvider _cameraProvider;
        private float _yaw;
        private float _pitch;
        private bool _initialized;
        private bool _providerAvailable;
        private bool _cameraWriteApplied;
        private bool _cameraWritePersisted;
        private float _readbackYaw;
        private float _readbackPitch;

        public CameraController(ActionCameraConfig config, ICameraProvider cameraProvider)
        {
            _config = config;
            _cameraProvider = cameraProvider;
        }

        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public bool ProviderAvailable => _providerAvailable;
        public bool CameraWriteApplied => _cameraWriteApplied;
        public bool CameraWritePersisted => _cameraWritePersisted;
        public float ReadbackYaw => _readbackYaw;
        public float ReadbackPitch => _readbackPitch;

        public void Enable()
        {
            _initialized = _cameraProvider.TryGetYawPitch(out _yaw, out _pitch);
            _providerAvailable = _cameraProvider.IsAvailable;
            _cameraWriteApplied = false;
            _cameraWritePersisted = false;
            _readbackYaw = _yaw;
            _readbackPitch = _pitch;
        }

        public void Disable()
        {
            _initialized = false;
            _cameraWriteApplied = false;
            _cameraWritePersisted = false;
        }

        public bool Update(float deltaX, float deltaY)
        {
            _providerAvailable = _cameraProvider.IsAvailable;
            if (!_providerAvailable)
            {
                _initialized = false;
                _cameraWriteApplied = false;
                _cameraWritePersisted = false;
                return false;
            }

            if (!_initialized && !_cameraProvider.TryGetYawPitch(out _yaw, out _pitch))
            {
                return false;
            }

            _initialized = true;

            _yaw += deltaX * BaseSensitivity * _config.HorizontalSensitivity;
            _pitch -= deltaY * BaseSensitivity * _config.VerticalSensitivity;
            _pitch = Math.Clamp(_pitch, PitchMin, PitchMax);

            _cameraWriteApplied = _cameraProvider.TrySetYawPitch(_yaw, _pitch);
            if (!_cameraWriteApplied)
            {
                _cameraWritePersisted = false;
                return false;
            }

            if (!_cameraProvider.TryGetYawPitch(out _readbackYaw, out _readbackPitch))
            {
                _cameraWritePersisted = false;
                return true;
            }

            _cameraWritePersisted = MathF.Abs(_readbackYaw - _yaw) <= 0.02f &&
                                    MathF.Abs(_readbackPitch - _pitch) <= 0.02f;
            return true;
        }
    }
}
