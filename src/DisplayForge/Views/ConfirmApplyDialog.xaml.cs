using System.Windows;
using System.Windows.Threading;
using DisplayForge.Resources;

namespace DisplayForge.Views;

/// <summary>
/// Windows-style "Keep these display settings?" prompt with auto-revert countdown.
/// DialogResult is true when the user keeps changes; false on revert or timeout.
/// </summary>
public partial class ConfirmApplyDialog : Window
{
    public const int DefaultTimeoutSeconds = 15;

    private readonly DispatcherTimer _timer;
    private int _remainingSeconds;
    private bool _completed;

    public ConfirmApplyDialog(int timeoutSeconds = DefaultTimeoutSeconds)
    {
        InitializeComponent();

        _remainingSeconds = Math.Max(1, timeoutSeconds);

        Title = Strings.ConfirmKeepSettingsTitle;
        MessageText.Text = Strings.ConfirmKeepSettingsMessage;
        BtnKeep.Content = Strings.KeepChanges;
        BtnRevert.Content = Strings.RevertChanges;
        UpdateCountdownText();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Activate();
        BtnKeep.Focus();
        _timer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            Complete(keep: false);
            return;
        }

        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        CountdownText.Text = string.Format(Strings.ConfirmKeepSettingsCountdown, _remainingSeconds);
    }

    private void Keep_Click(object sender, RoutedEventArgs e) => Complete(keep: true);

    private void Revert_Click(object sender, RoutedEventArgs e) => Complete(keep: false);

    private void Complete(bool keep)
    {
        if (_completed)
            return;

        _completed = true;
        _timer.Stop();
        DialogResult = keep;
    }
}
