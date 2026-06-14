using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class ActionCameraCursorManager : IDisposable
    {
        private readonly ActionCameraConfig _config;
        private IntPtr _gameWindowHandle = IntPtr.Zero;
        private bool _hidden;
        private bool _clipped;
        private bool _captured;
        private bool _disposed;

        public ActionCameraCursorManager(ActionCameraConfig config)
        {
            _config = config;
        }

        public void Hide()
        {
            if (_hidden)
            {
                return;
            }

            while (ShowCursor(false) >= 0)
            {
            }

            _hidden = true;
        }

        public void Show()
        {
            if (!_hidden)
            {
                return;
            }

            while (ShowCursor(true) < 0)
            {
            }

            _hidden = false;
        }

        public void Lock()
        {
            if (_config.RestrictToGameWindow && TryLockToGameWindow())
            {
                return;
            }

            ReleaseMouseCapture();
            LockToMonitorCenter();
        }

        public void Unlock()
        {
            ReleaseMouseCapture();

            if (!_clipped)
            {
                return;
            }

            ClipCursor(IntPtr.Zero);
            _clipped = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Unlock();
            Show();
        }

        public bool IsGameWindowForeground()
        {
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            var gameWindow = GetGameWindowHandle();
            return gameWindow != IntPtr.Zero && foreground == gameWindow;
        }

        private bool TryLockToGameWindow()
        {
            if (!TryGetGameClientCenterPoint(out var center))
            {
                return false;
            }

            CenterCursor(center);
            EnsureCursorAtCenter(center);

            var clipRect = new RECT
            {
                Left = center.X,
                Top = center.Y,
                Right = center.X + 1,
                Bottom = center.Y + 1
            };

            AcquireMouseCapture();
            if (ClipCursor(ref clipRect))
            {
                _clipped = true;
                return true;
            }

            return false;
        }

        private void LockToMonitorCenter()
        {
            if (!TryGetMonitorCenterPoint(out var center))
            {
                return;
            }

            CenterCursor(center);

            var clipRect = new RECT
            {
                Left = center.X,
                Top = center.Y,
                Right = center.X + 1,
                Bottom = center.Y + 1
            };

            if (ClipCursor(ref clipRect))
            {
                _clipped = true;
            }
        }

        private void AcquireMouseCapture()
        {
            var hwnd = GetGameWindowHandle();
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            if (SetCapture(hwnd) != IntPtr.Zero)
            {
                _captured = true;
            }
        }

        private void ReleaseMouseCapture()
        {
            if (!_captured)
            {
                return;
            }

            ReleaseCapture();
            _captured = false;
        }

        private static void CenterCursor(POINT point)
        {
            SetCursorPos(point.X, point.Y);
        }

        private void EnsureCursorAtCenter(POINT center)
        {
            if (!GetCursorPos(out var position))
            {
                return;
            }

            if (position.X != center.X || position.Y != center.Y)
            {
                CenterCursor(center);
            }
        }

        private IntPtr GetGameWindowHandle()
        {
            if (_gameWindowHandle != IntPtr.Zero && IsWindow(_gameWindowHandle))
            {
                return _gameWindowHandle;
            }

            ulong processId = (ulong)Process.GetCurrentProcess().Id;
            IntPtr handle = IntPtr.Zero;

            do
            {
                handle = FindWindowExW(IntPtr.Zero, handle, "FFXIVGAME", null);
                if (handle == IntPtr.Zero)
                {
                    break;
                }

                ulong windowProcessId = 0;
                GetWindowThreadProcessId(handle, ref windowProcessId);
                if (windowProcessId == processId)
                {
                    _gameWindowHandle = handle;
                    return _gameWindowHandle;
                }
            } while (handle != IntPtr.Zero);

            _gameWindowHandle = IntPtr.Zero;
            return IntPtr.Zero;
        }

        private bool TryGetGameClientScreenRect(out RECT rect)
        {
            rect = default;
            var hwnd = GetGameWindowHandle();
            if (hwnd == IntPtr.Zero || IsIconic(hwnd))
            {
                return false;
            }

            if (!GetClientRect(hwnd, out rect))
            {
                return false;
            }

            if (rect.Right <= 0 || rect.Bottom <= 0)
            {
                return false;
            }

            MapWindowPoints(hwnd, IntPtr.Zero, ref rect, 2);
            return rect.Right > rect.Left && rect.Bottom > rect.Top;
        }

        private bool TryGetGameClientCenterPoint(out POINT point)
        {
            point = default;
            if (!TryGetGameClientScreenRect(out var rect))
            {
                return false;
            }

            point.X = rect.Left + ((rect.Right - rect.Left) / 2);
            point.Y = rect.Top + ((rect.Bottom - rect.Top) / 2);
            return true;
        }

        private static bool TryGetMonitorCenterPoint(out POINT point)
        {
            point = default;
            int width = GetSystemMetrics(0);
            int height = GetSystemMetrics(1);
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            point.X = width / 2;
            point.Y = height / 2;
            return true;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ClipCursor(ref RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ClipCursor(IntPtr lpRect);

        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref RECT lpRect, uint cPoints);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll", EntryPoint = "FindWindowExW", SetLastError = true)]
        private static extern IntPtr FindWindowExW(IntPtr hWndParent, IntPtr hWndChildAfter, [MarshalAs(UnmanagedType.LPWStr)] string? lpszClass, [MarshalAs(UnmanagedType.LPWStr)] string? lpszWindow);

        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
        private static extern ulong GetWindowThreadProcessId(IntPtr hWnd, ref ulong id);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
