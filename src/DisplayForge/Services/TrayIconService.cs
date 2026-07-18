using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DisplayForge.Resources;
using DisplayForge.ViewModels;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MenuItem = System.Windows.Controls.MenuItem;

namespace DisplayForge.Services;

/// <summary>
/// System tray (notification-area) indicator. Owns a single TaskbarIcon;
/// left or right click opens an icon menu for profile switch / settings.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly MainViewModel _vm;
    private TaskbarIcon? _icon;
    private bool _disposed;

    public TrayIconService(MainViewModel vm)
    {
        _vm = vm;

        _vm.RequestRebuildTrayMenu += (_, _) =>
            SafeDispatch(RebuildMenu);

        _vm.RequestBalloon += (_, msg) =>
            SafeDispatch(() => ShowBalloonSafe(msg));

        CreateTrayIcon();
    }

    public void RebuildMenu()
    {
        if (_disposed || _icon is null)
            return;

        try
        {
            var menu = new ContextMenu();
            // Keep the tray menu inside the monitor work area (avoids off-screen near edges).
            menu.Opened += OnTrayMenuOpened;

            // Profile list (icon + optional check for the last-applied one)
            foreach (var profile in _vm.Profiles)
            {
                var item = new MenuItem
                {
                    Header = string.IsNullOrEmpty(profile.HotkeyDisplay)
                        ? profile.Name
                        : $"{profile.Name}  ({profile.HotkeyDisplay})",
                    IsChecked = profile.IsLastApplied,
                    Tag = profile.Id,
                    Icon = CreateMenuIcon(
                        profile.IsLastApplied ? TrayGlyph.Check : TrayGlyph.Monitor,
                        profile.IsLastApplied
                            ? Color.FromRgb(0x2F, 0x85, 0x5A)
                            : Color.FromRgb(0x4B, 0x55, 0x63))
                };
                item.Click += (_, _) => _vm.ApplyProfileById((string)item.Tag);
                menu.Items.Add(item);
            }

            if (_vm.Profiles.Count == 0)
            {
                menu.Items.Add(new MenuItem
                {
                    Header = Strings.EmptyStateNoProfiles.Replace("\n", " ").Trim(),
                    IsEnabled = false,
                    Icon = CreateMenuIcon(TrayGlyph.Monitor, Color.FromRgb(0x9C, 0xA3, 0xAF))
                });
            }

            menu.Items.Add(new Separator());

            var open = new MenuItem
            {
                Header = Strings.ShowMainWindow,
                Icon = CreateMenuIcon(TrayGlyph.Window, Color.FromRgb(0x37, 0x41, 0x51))
            };
            open.Click += (_, _) => _vm.ShowWindowCommand.Execute(null);
            menu.Items.Add(open);

            var settings = new MenuItem
            {
                Header = Strings.Settings,
                Icon = CreateMenuIcon(TrayGlyph.Settings, Color.FromRgb(0x37, 0x41, 0x51))
            };
            settings.Click += (_, _) => _vm.OpenSettingsCommand.Execute(null);
            menu.Items.Add(settings);

            menu.Items.Add(new Separator());

            var exit = new MenuItem
            {
                Header = Strings.Exit,
                Icon = CreateMenuIcon(TrayGlyph.Exit, Color.FromRgb(0xB9, 0x1C, 0x1C))
            };
            exit.Click += (_, _) => _vm.ExitAppCommand.Execute(null);
            menu.Items.Add(exit);

            _icon.ContextMenu = menu;
            _icon.ToolTipText = BuildTooltipText();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RebuildMenu: {ex}");
        }
    }

    private static void OnTrayMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu)
        {
            // Re-place with live cursor + clamp after layout for multi-monitor / DPI.
            TrayMenuPlacement.PlaceNearCursor(menu);
            TrayMenuPlacement.ClampOpened(menu);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            if (_icon is not null)
            {
                _icon.TrayMouseDoubleClick -= OnTrayDoubleClick;
                _icon.Dispose(); // disposes the Icon it owns — do not dispose Icon separately
                _icon = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tray dispose: {ex}");
        }
    }

    private string BuildTooltipText()
    {
        var active = _vm.Profiles.FirstOrDefault(p => p.IsLastApplied);
        if (active is null)
            return Strings.TrayTooltip;

        return $"{Strings.AppName} — {active.Name}";
    }

    private void ShowBalloonSafe(string msg)
    {
        if (_disposed || _icon is null || string.IsNullOrWhiteSpace(msg))
            return;

        try
        {
            _icon.ShowNotification(Strings.AppName, msg);
        }
        catch (Exception ex)
        {
            // After primary-monitor switches the native tray handle can be invalid.
            // Notification is optional; never crash the app for it.
            System.Diagnostics.Debug.WriteLine($"ShowNotification: {ex.Message}");
        }
    }

    private void CreateTrayIcon()
    {
        if (_disposed || _icon is not null)
            return;

        // Fresh Icon every time. TaskbarIcon takes ownership and will Dispose it.
        var icon = CreateIcon();
        try
        {
            _icon = new TaskbarIcon
            {
                ToolTipText = Strings.TrayTooltip,
                Icon = icon,
                Visibility = Visibility.Visible,
                // Left or right click on the indicator opens the profile / settings menu.
                MenuActivation = PopupActivationMode.LeftOrRightClick
            };
            _icon.TrayMouseDoubleClick += OnTrayDoubleClick;

            // TaskbarIcon created purely in code is not in the visual tree, so setting
            // Visibility alone never registers the Win32 notify icon. ForceCreate is required.
            // Efficiency mode is left off — this app needs snappy global hotkeys.
            _icon.ForceCreate(enablesEfficiencyMode: false);

            if (!_icon.IsCreated)
                throw new InvalidOperationException("TaskbarIcon.ForceCreate did not create a tray icon.");

            RebuildMenu();
        }
        catch
        {
            // If TaskbarIcon construction failed before taking ownership, free the icon.
            try
            {
                if (_icon is not null)
                {
                    try { _icon.Dispose(); } catch { /* ignore */ }
                    _icon = null;
                }
                else
                {
                    icon.Dispose();
                }
            }
            catch
            {
                /* ignore */
            }

            throw;
        }
    }

    private void OnTrayDoubleClick(object? sender, RoutedEventArgs e) =>
        _vm.ShowWindowCommand.Execute(null);

    private static void SafeDispatch(Action action)
    {
        var app = Application.Current;
        if (app is null)
            return;

        try
        {
            if (app.Dispatcher.CheckAccess())
                action();
            else
                app.Dispatcher.BeginInvoke(action);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SafeDispatch: {ex}");
        }
    }

    private static Icon CreateIcon()
    {
        // Lucide monitor-cog (ISC). Multi-size ICO generated from Assets/monitor-cog.svg.
        var info = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
        if (info?.Stream is null)
            throw new InvalidOperationException("App icon resource Assets/app.ico was not found.");

        using (info.Stream)
        using (var ms = new MemoryStream())
        {
            info.Stream.CopyTo(ms);
            ms.Position = 0;
            // Pick a tray-friendly size; Clone so the MemoryStream can be released.
            using var temp = new Icon(ms, 32, 32);
            return (Icon)temp.Clone();
        }
    }

    /// <summary>16×16 stroke icons for tray menu items (no external assets).</summary>
    private static FrameworkElement CreateMenuIcon(TrayGlyph glyph, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();

        var canvas = new Canvas
        {
            Width = 16,
            Height = 16,
            SnapsToDevicePixels = true
        };

        void AddPath(string data, double thickness = 1.5, bool fill = false)
        {
            var path = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(data),
                Stroke = brush,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Fill = fill ? brush : Brushes.Transparent
            };
            canvas.Children.Add(path);
        }

        switch (glyph)
        {
            case TrayGlyph.Monitor:
                // Rounded monitor body + stand (Lucide-inspired)
                AddPath("M2,3.5 H14 A1.5,1.5 0 0 1 15.5,5 V10.5 A1.5,1.5 0 0 1 14,12 H2 A1.5,1.5 0 0 1 0.5,10.5 V5 A1.5,1.5 0 0 1 2,3.5 Z");
                AddPath("M6,14 H10 M8,12 V14");
                break;

            case TrayGlyph.Check:
                AddPath("M3,8.5 L6.5,12 L13,4.5", thickness: 1.8);
                break;

            case TrayGlyph.Settings:
                // Simple gear: outer circle + hub + a few teeth as arcs
                AddPath("M8,2.5 V4.2 M8,11.8 V13.5 M2.5,8 H4.2 M11.8,8 H13.5 M4.1,4.1 L5.3,5.3 M10.7,10.7 L11.9,11.9 M11.9,4.1 L10.7,5.3 M5.3,10.7 L4.1,11.9");
                AddPath("M8,8 m-2.4,0 a2.4,2.4 0 1,0 4.8,0 a2.4,2.4 0 1,0 -4.8,0");
                break;

            case TrayGlyph.Window:
                AddPath("M2.5,3.5 H13.5 A1,1 0 0 1 14.5,4.5 V12.5 A1,1 0 0 1 13.5,13.5 H2.5 A1,1 0 0 1 1.5,12.5 V4.5 A1,1 0 0 1 2.5,3.5 Z");
                AddPath("M1.5,6.5 H14.5");
                break;

            case TrayGlyph.Exit:
                AddPath("M6,3.5 H3.5 A1,1 0 0 0 2.5,4.5 V11.5 A1,1 0 0 0 3.5,12.5 H6");
                AddPath("M9,8 H14.5 M12,5.5 L14.5,8 L12,10.5", thickness: 1.6);
                break;
        }

        return canvas;
    }

    private enum TrayGlyph
    {
        Monitor,
        Check,
        Settings,
        Window,
        Exit
    }
}
