using System.Runtime.InteropServices;

namespace FFXIVHudPlugin;

/// <summary>
/// Handles OS cursor locking, visibility, and centering for action camera.
/// </summary>
internal sealed class CursorManager : IDisposable
{
    private bool hidden;
    private bool clipped;
    private bool disposed;

    /// <summary>
    /// Hides the OS cursor.
    /// </summary>
    public void Hide()
    {
        if (this.hidden)
        {
            return;
        }

        while (ShowCursor(false) >= 0)
        {
        }

        this.hidden = true;
    }

    /// <summary>
    /// Shows the OS cursor.
    /// </summary>
    public void Show()
    {
        if (!this.hidden)
        {
            return;
        }

        while (ShowCursor(true) < 0)
        {
        }

        this.hidden = false;
    }

    /// <summary>
    /// Centers and clips the cursor to a 1x1 rectangle.
    /// </summary>
    public void Lock()
    {
        this.CenterCursor();
        if (!this.TryGetCenterRect(out var rect))
        {
            return;
        }

        if (ClipCursor(ref rect))
        {
            this.clipped = true;
        }
    }

    /// <summary>
    /// Releases any cursor clipping.
    /// </summary>
    public void Unlock()
    {
        if (!this.clipped)
        {
            return;
        }

        ClipCursor(IntPtr.Zero);
        this.clipped = false;
    }

    /// <summary>
    /// Centers the cursor to current monitor midpoint.
    /// </summary>
    public void CenterCursor()
    {
        if (!this.TryGetCenterPoint(out var point))
        {
            return;
        }

        SetCursorPos(point.X, point.Y);
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.Unlock();
        this.Show();
    }

    private bool TryGetCenterRect(out RECT rect)
    {
        rect = default;
        if (!this.TryGetCenterPoint(out var point))
        {
            return false;
        }

        rect.Left = point.X;
        rect.Top = point.Y;
        rect.Right = point.X + 1;
        rect.Bottom = point.Y + 1;
        return true;
    }

    private bool TryGetCenterPoint(out POINT point)
    {
        point = default;
        var width = GetSystemMetrics(0);
        var height = GetSystemMetrics(1);
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
