using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DisplayForge.Core.Services;
using DisplayForge.Resources;
using DisplayForge.Services;
using DisplayForge.ViewModels;
using DisplayForge.Views;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace DisplayForge;

public partial class App : Application
{
    private const string MutexName = "Local\\DisplayForge_SingleInstance_v1";
    private Mutex? _mutex;
    private ServiceProvider? _services;
    private TrayIconService? _tray;
    private MainWindow? _mainWindow;

    /// <summary>
    /// When true, closing the main window hides to the tray (process keeps running).
    /// When false, closing the window fully exits — used for <c>dotnet run</c> so the shell is not blocked.
    /// Override with <c>--tray-on-close</c> / <c>--exit-on-close</c>.
    /// </summary>
    public static bool MinimizeToTrayOnClose { get; private set; } = true;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Default: tray-on-close for normal launches; exit-on-close under dotnet CLI / debugger.
        MinimizeToTrayOnClose = ResolveMinimizeToTrayOnClose(e.Args);

        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            LogException("DispatcherUnhandledException", args.Exception);
            try
            {
                MessageBox.Show(
                    args.Exception.Message,
                    Strings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch
            {
                // ignore
            }
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                LogException("UnhandledException", ex);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
            LogException("UnobservedTaskException", args.Exception);
        };

        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(Strings.AlreadyRunning, Strings.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDisplayService, DisplayService>();
            services.AddSingleton<IProfileStore, JsonProfileStore>();
            services.AddSingleton<IHotkeyService, HotkeyService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<MainViewModel>();
            _services = services.BuildServiceProvider();

            var vm = _services.GetRequiredService<MainViewModel>();
            vm.Initialize();

            _mainWindow = new MainWindow(vm);
            MainWindow = _mainWindow;

            vm.RequestShowWindow += (_, _) =>
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            };

            // Create the tray indicator before honoring start-minimized.
            // Without a real tray icon, hiding the window would leave the user with nothing.
            var trayOk = false;
            try
            {
                _tray = new TrayIconService(vm);
                trayOk = true;
            }
            catch (Exception ex)
            {
                LogException("TrayIconService ctor", ex);
                // App remains usable without tray (window stays open).
            }

            // Without a tray there is no Exit menu — never orphan a hidden process.
            if (!trayOk)
                MinimizeToTrayOnClose = false;

            // Only honor start-minimized when the tray icon actually exists.
            if (trayOk && vm.Settings.StartMinimizedToTray)
            {
                // Do not Show() first — avoids a window flash when launching to tray.
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.Show();
            }
        }
        catch (Exception ex)
        {
            LogException("OnStartup", ex);
            MessageBox.Show(ex.ToString(), Strings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    /// <summary>
    /// Fully exit the application (tray, hotkeys, message loop). Safe to call multiple times.
    /// </summary>
    public void RequestExit()
    {
        try
        {
            if (_mainWindow is not null)
                _mainWindow.ForceClose();
        }
        catch
        {
            // ignore
        }

        try
        {
            Shutdown();
        }
        catch
        {
            // ignore
        }
    }

    private static bool ResolveMinimizeToTrayOnClose(string[] args)
    {
        // Explicit flags win.
        foreach (var arg in args)
        {
            if (arg is "--exit-on-close" or "/exit-on-close")
                return false;
            if (arg is "--tray-on-close" or "/tray-on-close")
                return true;
        }

        // Debugger / `dotnet run` should return the terminal when the window is closed.
        if (Debugger.IsAttached)
            return false;

        if (IsLaunchedFromDotnetCli())
            return false;

        return true;
    }

    /// <summary>
    /// True when this process is a child of <c>dotnet</c> (typical for <c>dotnet run</c>).
    /// </summary>
    private static bool IsLaunchedFromDotnetCli()
    {
        try
        {
            using var current = Process.GetCurrentProcess();
            var parentId = GetParentProcessId(current.Id);
            if (parentId <= 0)
                return false;

            using var parent = Process.GetProcessById(parentId);
            var name = parent.ProcessName;
            return name.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static int GetParentProcessId(int processId)
    {
        var entry = new PROCESSENTRY32
        {
            dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>()
        };

        var snapshot = CreateToolhelp32Snapshot(0x00000002 /* TH32CS_SNAPPROCESS */, 0);
        if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
            return 0;

        try
        {
            if (!Process32First(snapshot, ref entry))
                return 0;

            do
            {
                if (entry.th32ProcessID == (uint)processId)
                    return (int)entry.th32ParentProcessID;
            } while (Process32Next(snapshot, ref entry));
        }
        finally
        {
            CloseHandle(snapshot);
        }

        return 0;
    }

    private const int MaxPath = 260;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    protected override void OnExit(ExitEventArgs e)
    {
        try { _tray?.Dispose(); } catch { /* ignore */ }
        try
        {
            if (_services?.GetService<IHotkeyService>() is IDisposable hotkeys)
                hotkeys.Dispose();
        }
        catch { /* ignore */ }

        try { _services?.Dispose(); } catch { /* ignore */ }

        if (_mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch { /* ignore */ }
            try { _mutex.Dispose(); } catch { /* ignore */ }
        }

        base.OnExit(e);
    }

    private static void LogException(string source, Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DisplayForge");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "crash.log");
            File.AppendAllText(
                path,
                $"[{DateTime.Now:O}] {source}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // ignore logging failures
        }
    }
}
