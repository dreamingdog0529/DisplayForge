using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using DisplayForge.Core.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace DisplayForge.Views;

/// <summary>
/// Captures a global-style hotkey chord.
/// Primary path: WPF <see cref="OnPreviewKeyDown"/> (reliable for Ctrl/Alt/Shift).
/// Optional low-level keyboard hook: improves Win+key capture when the shell would
/// otherwise steal the chord before WPF sees it.
/// </summary>
public sealed class HotkeyCaptureBox : TextBox
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12; // Alt
    private const int VkShift = 0x10;
    private const int VkEscape = 0x1B;
    private const int VkBack = 0x08;
    private const int VkDelete = 0x2E;

    public static readonly DependencyProperty HotkeyProperty =
        DependencyProperty.Register(
            nameof(Hotkey),
            typeof(HotkeyBinding),
            typeof(HotkeyCaptureBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnHotkeyChanged));

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc;
    private bool _capturing;
    private bool _appliedFromHook;

    public HotkeyBinding? Hotkey
    {
        get => (HotkeyBinding?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public event EventHandler<HotkeyBinding?>? HotkeyCaptured;

    public HotkeyCaptureBox()
    {
        IsReadOnly = true;
        AcceptsReturn = false;
        AcceptsTab = false;
        Focusable = true;
        GotKeyboardFocus += (_, _) => StartHook();
        LostKeyboardFocus += (_, _) => StopHook();
        Unloaded += (_, _) => StopHook();
    }

    private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotkeyCaptureBox box)
            box.Text = (e.NewValue as HotkeyBinding)?.ToDisplayString() ?? string.Empty;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        e.Handled = true;

        // Hook already applied a binding for this physical key — avoid double-fire.
        if (_appliedFromHook)
        {
            _appliedFromHook = false;
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        // Key.ImeProcessed / DeadCharProcessed etc. — resolve real key when present.
        if (key == Key.ImeProcessed || key == Key.DeadCharProcessed)
            key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (IsModifierKey(key))
            return;

        if (key is Key.Escape or Key.Back or Key.Delete)
        {
            ApplyCapture(null);
            return;
        }

        var modifiers = ReadWpfModifiers();
        // Require at least one modifier for safety (matches global RegisterHotKey policy).
        if (modifiers == HotkeyModifiers.None)
            return;

        if (key is Key.None or Key.System)
            return;

        var keyName = key.ToString();
        if (string.IsNullOrEmpty(keyName))
            return;

        ApplyCapture(new HotkeyBinding
        {
            Kind = HotkeyActionKind.ApplyProfile,
            Modifiers = modifiers,
            Key = keyName
        });
    }

    private void StartHook()
    {
        if (_hookId != IntPtr.Zero)
            return;

        // Keep delegate alive for the native hook lifetime (GC must not collect it).
        _hookProc = HookCallback;
        var hMod = ResolveHookModuleHandle();
        _hookId = SetWindowsHookEx(WhKeyboardLl, _hookProc, hMod, 0);
        if (_hookId == IntPtr.Zero)
        {
            // Fallback: some hosts accept NULL module for LL hooks.
            _hookId = SetWindowsHookEx(WhKeyboardLl, _hookProc, IntPtr.Zero, 0);
        }

        _capturing = _hookId != IntPtr.Zero;
        // Capture still works via OnPreviewKeyDown when the hook cannot be installed.
    }

    private static IntPtr ResolveHookModuleHandle()
    {
        // Prefer the process executable module (works for WH_KEYBOARD_LL in most hosts).
        try
        {
            using var process = Process.GetCurrentProcess();
            var module = process.MainModule;
            if (module?.ModuleName is { Length: > 0 } name)
            {
                var handle = GetModuleHandle(name);
                if (handle != IntPtr.Zero)
                    return handle;
            }
        }
        catch
        {
            // MainModule can throw under restricted process access.
        }

        return GetModuleHandle(null);
    }

    private void StopHook()
    {
        if (_hookId == IntPtr.Zero)
            return;

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _hookProc = null;
        _capturing = false;
        _appliedFromHook = false;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _capturing)
        {
            var msg = wParam.ToInt32();
            if (msg is WmKeyDown or WmSysKeyDown)
            {
                // Focus check on the installing (UI) thread; LL hooks are delivered there.
                if (!IsKeyboardFocused)
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);

                var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                if (TryHandleVirtualKey(info.vkCode))
                {
                    // Eat non-modifier chords so Win+key does not open Start / switch tasks.
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool TryHandleVirtualKey(int vk)
    {
        // Let pure modifiers through so the OS keeps consistent key state.
        // Returning false means "do not eat" — CallNextHookEx will run.
        if (IsModifierVk(vk))
            return false;

        if (vk is VkEscape or VkBack or VkDelete)
        {
            _appliedFromHook = true;
            Dispatcher.BeginInvoke(() =>
            {
                ApplyCapture(null);
                _appliedFromHook = false;
            });
            return true;
        }

        var modifiers = ReadNativeModifiers();
        if (modifiers == HotkeyModifiers.None)
            return false;

        if (!TryVirtualKeyToWpfKey(vk, out var keyName))
            return false;

        var binding = new HotkeyBinding
        {
            Kind = HotkeyActionKind.ApplyProfile,
            Modifiers = modifiers,
            Key = keyName
        };

        _appliedFromHook = true;
        Dispatcher.BeginInvoke(() =>
        {
            ApplyCapture(binding);
            // Clear after a tick so a residual PreviewKeyDown (if any) is ignored.
            Dispatcher.BeginInvoke(() => _appliedFromHook = false);
        });

        return true;
    }

    private void ApplyCapture(HotkeyBinding? binding)
    {
        if (binding is null || binding.IsEmpty)
        {
            Hotkey = null;
            Text = string.Empty;
            HotkeyCaptured?.Invoke(this, null);
            return;
        }

        Hotkey = binding;
        Text = binding.ToDisplayString();
        HotkeyCaptured?.Invoke(this, binding);
    }

    private static HotkeyModifiers ReadWpfModifiers()
    {
        var modifiers = HotkeyModifiers.None;
        var kmod = Keyboard.Modifiers;
        if (kmod.HasFlag(ModifierKeys.Control)) modifiers |= HotkeyModifiers.Control;
        if (kmod.HasFlag(ModifierKeys.Alt)) modifiers |= HotkeyModifiers.Alt;
        if (kmod.HasFlag(ModifierKeys.Shift)) modifiers |= HotkeyModifiers.Shift;
        if (kmod.HasFlag(ModifierKeys.Windows)
            || Keyboard.IsKeyDown(Key.LWin)
            || Keyboard.IsKeyDown(Key.RWin))
        {
            modifiers |= HotkeyModifiers.Win;
        }

        return modifiers;
    }

    private static HotkeyModifiers ReadNativeModifiers()
    {
        var modifiers = HotkeyModifiers.None;
        if (IsKeyDown(VkControl)) modifiers |= HotkeyModifiers.Control;
        if (IsKeyDown(VkMenu)) modifiers |= HotkeyModifiers.Alt;
        if (IsKeyDown(VkShift)) modifiers |= HotkeyModifiers.Shift;
        if (IsKeyDown(VkLWin) || IsKeyDown(VkRWin)) modifiers |= HotkeyModifiers.Win;
        return modifiers;
    }

    private static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin
            or Key.System;

    private static bool IsModifierVk(int vk) =>
        vk is VkControl or VkMenu or VkShift or VkLWin or VkRWin
            or 0xA0 or 0xA1 // L/R Shift
            or 0xA2 or 0xA3 // L/R Ctrl
            or 0xA4 or 0xA5; // L/R Alt

    private static bool TryVirtualKeyToWpfKey(int vk, out string keyName)
    {
        keyName = string.Empty;
        try
        {
            var key = KeyInterop.KeyFromVirtualKey(vk);
            if (key is Key.None or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System)
            {
                return false;
            }

            keyName = key.ToString();
            return !string.IsNullOrEmpty(keyName);
        }
        catch
        {
            return false;
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
