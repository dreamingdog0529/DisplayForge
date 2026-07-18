using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Input;
using DisplayForge.Core.Models;
using DisplayForge.Core.Services;

namespace DisplayForge.Services;

/// <summary>
/// Global hotkeys via RegisterHotKey, using a message-only HwndSource.
/// </summary>
public sealed class HotkeyService : IHotkeyService
{
    private const int WmHotkey = 0x0312;
    private HwndSource? _source;
    private readonly Dictionary<int, HotkeyBinding> _registered = new();
    private bool _disposed;

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public bool IsSupported => OperatingSystem.IsWindows();

    public IReadOnlyList<HotkeyRegistrationResult> RegisterAll(
        IEnumerable<(int Id, HotkeyBinding Binding)> bindings)
    {
        EnsureWindow();
        UnregisterAll();

        var results = new List<HotkeyRegistrationResult>();
        foreach (var (id, binding) in bindings)
        {
            if (binding.IsEmpty)
            {
                results.Add(new HotkeyRegistrationResult
                {
                    Id = id,
                    Success = true,
                    Display = string.Empty
                });
                continue;
            }

            var modifiers = ToNativeModifiers(binding.Modifiers);
            if (!TryParseKey(binding.Key, out var virtualKey))
            {
                results.Add(new HotkeyRegistrationResult
                {
                    Id = id,
                    Success = false,
                    Error = $"Unknown key: {binding.Key}",
                    Display = binding.ToDisplayString()
                });
                continue;
            }

            var ok = RegisterHotKey(_source!.Handle, id, modifiers, virtualKey);
            if (ok)
                _registered[id] = binding.Clone();

            results.Add(new HotkeyRegistrationResult
            {
                Id = id,
                Success = ok,
                Error = ok ? null : $"RegisterHotKey failed (error {Marshal.GetLastWin32Error()})",
                Display = binding.ToDisplayString()
            });
        }

        return results;
    }

    public void UnregisterAll()
    {
        if (_source is null)
            return;

        foreach (var id in _registered.Keys.ToList())
            UnregisterHotKey(_source.Handle, id);
        _registered.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        UnregisterAll();
        _source?.Dispose();
        _source = null;
    }

    private void EnsureWindow()
    {
        if (_source is not null)
            return;

        var parameters = new HwndSourceParameters("DisplayForgeHotkeys")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = unchecked((int)0x80000000) // WS_POPUP
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey)
        {
            var id = wParam.ToInt32();
            HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs { Id = id });
            handled = true;
        }

        return IntPtr.Zero;
    }

    internal static uint ToNativeModifiers(HotkeyModifiers modifiers)
    {
        // MOD_ALT=1, MOD_CONTROL=2, MOD_SHIFT=4, MOD_WIN=8, MOD_NOREPEAT=0x4000
        uint result = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Alt)) result |= 0x0001;
        if (modifiers.HasFlag(HotkeyModifiers.Control)) result |= 0x0002;
        if (modifiers.HasFlag(HotkeyModifiers.Shift)) result |= 0x0004;
        if (modifiers.HasFlag(HotkeyModifiers.Win)) result |= 0x0008;
        result |= 0x4000;
        return result;
    }

    internal static bool TryParseKey(string keyName, out uint virtualKey)
    {
        virtualKey = 0;
        if (string.IsNullOrWhiteSpace(keyName))
            return false;

        if (!Enum.TryParse<Key>(keyName, ignoreCase: true, out var key))
            return false;

        virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        return virtualKey != 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
