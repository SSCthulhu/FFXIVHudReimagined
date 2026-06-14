using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class ActionCameraCursorManager : IDisposable
    {
        private IntPtr _gameWindowHandle = IntPtr.Zero;
        private bool _hidden;
        private bool _clipped;
        private bool _disposed;

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
            CenterCursor();
            if (!TryGetCenterRect(out var rect))
            {
                return;
            }

            if (ClipCursor(ref rect))
            {
                _clipped = true;
            }
        }

        public void Unlock()
        {
            if (!_clipped)
            {
                return;
            }

            ClipCursor(IntPtr.Zero);
            _clipped = false;
        }

        public void CenterCursor()
        {
            if (!TryGetCenterPoint(out var point))
            {
                return;
            }

            SetCursorPos(point.X, point.Y);
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

        private static bool TryGetCenterRect(out RECT rect)
        {
            rect = default;
            if (!TryGetCenterPoint(out var point))
            {
                return false;
            }

            rect.Left = point.X;
            rect.Top = point.Y;
            rect.Right = point.X + 1;
            rect.Bottom = point.Y + 1;
            return true;
        }

        private static bool TryGetCenterPoint(out POINT point)
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

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindow(IntPtr hWnd);

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
