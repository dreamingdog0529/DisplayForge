using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace DisplayForge.Services;

/// <summary>
/// Keeps the tray <see cref="ContextMenu"/> inside the monitor work area
/// (taskbar / multi-monitor edges).
/// </summary>
internal static class TrayMenuPlacement
{
    private const uint MonitorDefaultToNearest = 2;

    /// <summary>
    /// Call just before the menu opens: place AbsolutePoint near the cursor,
    /// opening upward/left when near the bottom-right tray corner, clamped to work area.
    /// </summary>
    public static void PlaceNearCursor(ContextMenu menu)
    {
        if (!GetCursorPos(out var cursorPx))
            return;

        // Measure in DIPs before open.
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
        menu.PlacementTarget = null;
        menu.PlacementRectangle = Rect.Empty;
        menu.HorizontalOffset = 0;
        menu.VerticalOffset = 0;

        menu.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var sizeDip = menu.DesiredSize;
        if (sizeDip.Width <= 0 || sizeDip.Height <= 0)
        {
            // Fallback estimate so we still clamp something reasonable.
            sizeDip = new Size(220, Math.Max(40, menu.Items.Count * 28.0));
        }

        // Use the DPI of the monitor under the cursor (not primary).
        var dpi = GetDpiForCursor(cursorPx);
        var sizePx = new Size(sizeDip.Width * dpi.DpiX / 96.0, sizeDip.Height * dpi.DpiY / 96.0);

        var work = GetWorkAreaPixels(cursorPx);

        // Prefer above-left of cursor (tray is usually bottom/right).
        var left = cursorPx.X;
        var top = cursorPx.Y - (int)Math.Ceiling(sizePx.Height);

        if (left + sizePx.Width > work.Right)
            left = work.Right - (int)Math.Ceiling(sizePx.Width);
        if (left < work.Left)
            left = work.Left;

        if (top + sizePx.Height > work.Bottom)
            top = work.Bottom - (int)Math.Ceiling(sizePx.Height);
        if (top < work.Top)
            top = work.Top;

        // AbsolutePoint offsets are in DIPs (device-independent).
        menu.HorizontalOffset = left * 96.0 / dpi.DpiX;
        menu.VerticalOffset = top * 96.0 / dpi.DpiY;
    }

    /// <summary>
    /// After open, nudge if the rendered popup still spills outside (DPI / measure mismatch).
    /// </summary>
    public static void ClampOpened(ContextMenu menu)
    {
        try
        {
            menu.UpdateLayout();
            if (menu.ActualWidth <= 0 || menu.ActualHeight <= 0)
                return;

            var topLeftPx = menu.PointToScreen(new Point(0, 0));
            var bottomRightPx = menu.PointToScreen(new Point(menu.ActualWidth, menu.ActualHeight));

            var work = GetWorkAreaPixels(new POINT
            {
                X = (int)topLeftPx.X,
                Y = (int)topLeftPx.Y
            });

            double dxPx = 0;
            double dyPx = 0;

            if (bottomRightPx.X > work.Right)
                dxPx -= bottomRightPx.X - work.Right;
            if (topLeftPx.X + dxPx < work.Left)
                dxPx += work.Left - (topLeftPx.X + dxPx);

            if (bottomRightPx.Y > work.Bottom)
                dyPx -= bottomRightPx.Y - work.Bottom;
            if (topLeftPx.Y + dyPx < work.Top)
                dyPx += work.Top - (topLeftPx.Y + dyPx);

            if (Math.Abs(dxPx) < 0.5 && Math.Abs(dyPx) < 0.5)
                return;

            var source = PresentationSource.FromVisual(menu);
            var fromDevice = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            var deltaDip = fromDevice.Transform(new Point(dxPx, dyPx));

            menu.HorizontalOffset += deltaDip.X;
            menu.VerticalOffset += deltaDip.Y;
        }
        catch
        {
            // Placement is best-effort; never break menu open.
        }
    }

    private static (int DpiX, int DpiY) GetDpiForCursor(POINT cursorPx)
    {
        try
        {
            var mon = MonitorFromPoint(cursorPx, MonitorDefaultToNearest);
            if (mon != IntPtr.Zero && GetDpiForMonitor(mon, 0 /* MDT_EFFECTIVE_DPI */, out var dpiX, out var dpiY) == 0)
                return ((int)dpiX, (int)dpiY);
        }
        catch
        {
            // Fall through.
        }

        // System DPI fallback.
        try
        {
            using var src = new HwndSource(new HwndSourceParameters());
            var m = src.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            return ((int)Math.Round(96 * m.M11), (int)Math.Round(96 * m.M22));
        }
        catch
        {
            return (96, 96);
        }
    }

    private static RECT GetWorkAreaPixels(POINT pt)
    {
        var mon = MonitorFromPoint(pt, MonitorDefaultToNearest);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (mon != IntPtr.Zero && GetMonitorInfo(mon, ref info))
            return info.rcWork;

        return new RECT
        {
            Left = (int)SystemParameters.VirtualScreenLeft,
            Top = (int)SystemParameters.VirtualScreenTop,
            Right = (int)(SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth),
            Bottom = (int)(SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
}
