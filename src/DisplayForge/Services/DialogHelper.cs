using System.Windows;
using System.Windows.Interop;
using Application = System.Windows.Application;

namespace DisplayForge.Services;

/// <summary>
/// Safe owner / placement for dialogs opened while the main window may be
/// hidden or never shown (tray-only startup).
/// </summary>
internal static class DialogHelper
{
    /// <summary>
    /// Attach a usable owner when possible; otherwise center on the screen and
    /// show in the taskbar so the dialog is still reachable from the tray.
    /// </summary>
    public static void PrepareDialog(Window dialog, Window? preferredOwner = null)
    {
        preferredOwner ??= Application.Current?.MainWindow;

        if (preferredOwner is not null && CanBeOwner(preferredOwner))
        {
            dialog.Owner = preferredOwner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return;
        }

        // Setting Owner to a never-shown window throws InvalidOperationException.
        dialog.Owner = null;
        dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        dialog.ShowInTaskbar = true;
        dialog.Topmost = true;
    }

    public static bool CanBeOwner(Window window)
    {
        try
        {
            // HWND exists only after the window has been shown at least once.
            return new WindowInteropHelper(window).Handle != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }
}
