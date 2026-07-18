using DisplayForge.Core.Models;

namespace DisplayForge.Core.Services;

public interface IHotkeyService : IDisposable
{
    event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    /// <summary>
    /// Re-register all hotkeys. Returns failed registrations with reason.
    /// </summary>
    IReadOnlyList<HotkeyRegistrationResult> RegisterAll(
        IEnumerable<(int Id, HotkeyBinding Binding)> bindings);

    void UnregisterAll();

    bool IsSupported { get; }
}

public sealed class HotkeyPressedEventArgs : EventArgs
{
    public int Id { get; init; }
}

public sealed class HotkeyRegistrationResult
{
    public int Id { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string Display { get; init; } = string.Empty;
}
