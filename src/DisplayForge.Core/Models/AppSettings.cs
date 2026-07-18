namespace DisplayForge.Core.Models;

public sealed class AppSettings
{
    public const int DefaultConfirmTimeoutSeconds = 15;
    public const int MinConfirmTimeoutSeconds = 1;
    public const int MaxConfirmTimeoutSeconds = 120;

    public string Language { get; set; } = "auto";

    /// <summary>If true, start with the main window hidden (tray only). Default false so first launch is visible.</summary>
    public bool StartMinimizedToTray { get; set; } = false;

    public bool ShowNotificationOnSwitch { get; set; } = true;

    public bool HotkeysEnabled { get; set; } = true;

    /// <summary>
    /// After applying from the main window or tray menu, show the keep/revert countdown dialog.
    /// </summary>
    public bool ConfirmApplyFromUi { get; set; } = true;

    /// <summary>
    /// After applying via global hotkey, show the keep/revert countdown dialog.
    /// </summary>
    public bool ConfirmApplyFromHotkey { get; set; } = true;

    /// <summary>
    /// Seconds before auto-revert when the confirmation dialog is shown.
    /// Clamped to <see cref="MinConfirmTimeoutSeconds"/>–<see cref="MaxConfirmTimeoutSeconds"/>.
    /// </summary>
    public int ConfirmApplyTimeoutSeconds { get; set; } = DefaultConfirmTimeoutSeconds;

    public string? LastAppliedProfileId { get; set; }

    /// <summary>Returns a sanitized timeout within the allowed range.</summary>
    public int GetConfirmTimeoutSeconds() =>
        Math.Clamp(
            ConfirmApplyTimeoutSeconds <= 0
                ? DefaultConfirmTimeoutSeconds
                : ConfirmApplyTimeoutSeconds,
            MinConfirmTimeoutSeconds,
            MaxConfirmTimeoutSeconds);

    public void Normalize()
    {
        ConfirmApplyTimeoutSeconds = GetConfirmTimeoutSeconds();
    }

    public AppSettings Clone() => new()
    {
        Language = Language,
        StartMinimizedToTray = StartMinimizedToTray,
        ShowNotificationOnSwitch = ShowNotificationOnSwitch,
        HotkeysEnabled = HotkeysEnabled,
        ConfirmApplyFromUi = ConfirmApplyFromUi,
        ConfirmApplyFromHotkey = ConfirmApplyFromHotkey,
        ConfirmApplyTimeoutSeconds = ConfirmApplyTimeoutSeconds,
        LastAppliedProfileId = LastAppliedProfileId
    };
}
