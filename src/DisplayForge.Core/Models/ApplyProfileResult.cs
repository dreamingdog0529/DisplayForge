namespace DisplayForge.Core.Models;

public sealed class ApplyProfileResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> MissingMonitors { get; init; } = [];
    public int Win32Error { get; init; }

    public static ApplyProfileResult Ok(string message = "") =>
        new() { Success = true, Message = message };

    public static ApplyProfileResult Fail(string message, int win32Error = 0, IReadOnlyList<string>? missing = null) =>
        new()
        {
            Success = false,
            Message = message,
            Win32Error = win32Error,
            MissingMonitors = missing ?? []
        };
}
