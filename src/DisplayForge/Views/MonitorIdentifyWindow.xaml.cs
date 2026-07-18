using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using DisplayForge.Core.Models;
using DisplayForge.Resources;

namespace DisplayForge.Views;

/// <summary>
/// Full-monitor overlay showing a large identification number (Windows Settings "Identify" style).
/// </summary>
public partial class MonitorIdentifyWindow : Window
{
    private static readonly List<MonitorIdentifyWindow> ActiveOverlays = [];
    private static DispatcherTimer? _autoCloseTimer;

    private const uint SwpShowWindow = 0x0040;
    private const uint SwpNoActivate = 0x0010;
    private static readonly IntPtr HwndTopmost = new(-1);

    public MonitorIdentifyWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += (_, _) => CloseAll();
        KeyDown += (_, e) =>
        {
            if (e.Key is System.Windows.Input.Key.Escape or System.Windows.Input.Key.Enter or System.Windows.Input.Key.Space)
                CloseAll();
        };
    }

    /// <summary>
    /// Show identification numbers on all active monitors for a few seconds.
    /// </summary>
    public static void ShowIdentify(IReadOnlyList<MonitorInfo> monitors, int durationSeconds = 4)
    {
        CloseAll();

        var active = monitors
            .Where(m => m.IsActive && m.Width > 0 && m.Height > 0)
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.PositionX)
            .ThenBy(m => m.PositionY)
            .ToList();

        if (active.Count == 0)
            return;

        for (var i = 0; i < active.Count; i++)
        {
            var m = active[i];
            var number = i + 1;
            var win = new MonitorIdentifyWindow();
            win.NumberText.Text = number.ToString();
            win.LabelText.Text = BuildLabel(m);

            // Create HWND before positioning in physical pixels.
            win.Show();
            var hwnd = new WindowInteropHelper(win).Handle;
            SetWindowPos(
                hwnd,
                HwndTopmost,
                m.PositionX,
                m.PositionY,
                m.Width,
                m.Height,
                SwpShowWindow | SwpNoActivate);

            ActiveOverlays.Add(win);
        }

        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Clamp(durationSeconds, 1, 30))
        };
        _autoCloseTimer.Tick += (_, _) => CloseAll();
        _autoCloseTimer.Start();
    }

    public static void CloseAll()
    {
        if (_autoCloseTimer is not null)
        {
            _autoCloseTimer.Stop();
            _autoCloseTimer = null;
        }

        foreach (var w in ActiveOverlays.ToList())
        {
            try
            {
                w.Close();
            }
            catch
            {
                // ignore
            }
        }

        ActiveOverlays.Clear();
    }

    private static string BuildLabel(MonitorInfo m)
    {
        var name = string.IsNullOrWhiteSpace(m.FriendlyName)
            ? (string.IsNullOrWhiteSpace(m.GdiDeviceName) ? m.StableId : m.GdiDeviceName)
            : m.FriendlyName;

        var res = m.Width > 0 && m.Height > 0 ? $"{m.Width}×{m.Height}" : string.Empty;
        if (m.IsPrimary)
            return string.IsNullOrEmpty(res)
                ? $"{name} ({Strings.Primary})"
                : $"{name}  ·  {res}  ·  {Strings.Primary}";
        return string.IsNullOrEmpty(res) ? name : $"{name}  ·  {res}";
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
