using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayForge.Core.Models;
using DisplayForge.Core.Services;
using DisplayForge.Resources;
using DisplayForge.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace DisplayForge.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDisplayService _displayService;
    private readonly IProfileStore _profileStore;
    private readonly IHotkeyService _hotkeyService;
    private readonly ILocalizationService _localization;
    private ProfileCollection _collection = new();
    private AppSettings _settings = new();
    private bool _exitRequested;
    private bool _applyInProgress;

    public event EventHandler? RequestShowWindow;
    public event EventHandler? RequestRebuildTrayMenu;
    public event EventHandler<string>? RequestBalloon;
    public event EventHandler? LanguageChanged;

    public ObservableCollection<ProfileItemViewModel> Profiles { get; } = [];
    public ObservableCollection<MonitorRowViewModel> CurrentMonitors { get; } = [];
    public ObservableCollection<MonitorRowViewModel> SelectedProfileMonitors { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplySelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(OverwriteFromCurrentCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameSelectedCommand))]
    private ProfileItemViewModel? _selectedProfile;

    private bool HasSelectedProfile => SelectedProfile is not null;

    [ObservableProperty]
    private string _statusText = Strings.StatusReady;

    [ObservableProperty]
    private string _selectedHotkeyDisplay = string.Empty;

    [ObservableProperty]
    private string _windowTitle = Strings.AppName;

    public AppSettings Settings => _settings;

    public bool ExitRequested
    {
        get => _exitRequested;
        private set => _exitRequested = value;
    }

    public MainViewModel(
        IDisplayService displayService,
        IProfileStore profileStore,
        IHotkeyService hotkeyService,
        ILocalizationService localization)
    {
        _displayService = displayService;
        _profileStore = profileStore;
        _hotkeyService = hotkeyService;
        _localization = localization;

        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
    }

    public void Initialize()
    {
        _settings = _profileStore.LoadSettings();
        _settings.Normalize();
        _localization.ApplyLanguage(_settings.Language);
        RefreshLocalizedStrings();

        _collection = _profileStore.LoadProfiles();
        ReloadProfileList();
        RefreshCurrentMonitors();
        RegisterHotkeys();
        StatusText = Strings.StatusReady;
        RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshLocalizedStrings()
    {
        WindowTitle = Strings.AppName;
        OnPropertyChanged(nameof(StatusText));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSelectedProfileChanged(ProfileItemViewModel? value)
    {
        SelectedProfileMonitors.Clear();
        if (value is null)
        {
            SelectedHotkeyDisplay = string.Empty;
            return;
        }

        SelectedHotkeyDisplay = value.Model.Hotkey?.ToDisplayString() ?? string.Empty;
        foreach (var m in value.Model.Monitors)
            SelectedProfileMonitors.Add(MonitorRowViewModel.FromProfile(m, OnProfileMonitorEdited));
    }

    /// <summary>
    /// Persist profile edits. When <paramref name="structural"/> is true (Primary/Enabled),
    /// enforce exclusivity and re-sync all rows.
    /// </summary>
    private void OnProfileMonitorEdited(
        MonitorRowViewModel source,
        bool structural,
        MonitorMatcher.StructuralFlagIntent intent)
    {
        if (SelectedProfile is null)
            return;

        var rows = SelectedProfileMonitors.Where(r => r.IsEditable).ToList();
        if (rows.Count == 0)
            return;

        if (structural)
        {
            // Entry + explicit intent are the source of truth.
            // SetPrimary must win even when the row was disabled (enable+primary together).
            MonitorMatcher.EnforceEnabledAndPrimary(
                SelectedProfile.Model.Monitors,
                edited: source.Entry,
                intent: intent);

            foreach (var row in rows)
                row.SyncFromEntry();
        }
        else if (source.Entry is not null)
        {
            source.Entry.Orientation = source.OrientationDegrees is 90 or 180 or 270
                ? source.OrientationDegrees
                : 0;
        }

        SelectedProfile.Model.UpdatedAt = DateTimeOffset.UtcNow;
        PersistProfiles();
        StatusText = string.Format(Strings.StatusSaved, SelectedProfile.Name);
    }

    [RelayCommand]
    private void RefreshCurrentMonitors()
    {
        CurrentMonitors.Clear();
        foreach (var m in _displayService.GetCurrentMonitors(includeInactive: true))
            CurrentMonitors.Add(MonitorRowViewModel.FromLive(m));
    }

    /// <summary>
    /// Show large identification numbers on each physical monitor (Windows Settings "Identify" style).
    /// </summary>
    [RelayCommand]
    private void IdentifyMonitors()
    {
        try
        {
            var monitors = _displayService.GetCurrentMonitors(includeInactive: false);
            Views.MonitorIdentifyWindow.ShowIdentify(monitors);
            StatusText = Strings.StatusIdentifyShown;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Strings.StatusFailed, ex.Message);
        }
    }

    [RelayCommand]
    private void CreateFromCurrent()
    {
        var index = _collection.Profiles.Count + 1;
        var name = string.Format(Strings.NewProfileName, index);
        var profile = _displayService.CaptureCurrentAsProfile(name);
        _collection.Profiles.Add(profile);
        PersistProfiles();
        ReloadProfileList(profile.Id);
        StatusText = string.Format(Strings.StatusSaved, profile.Name);
        RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void OverwriteFromCurrent()
    {
        if (SelectedProfile is null)
            return;

        var captured = _displayService.CaptureCurrentAsProfile(SelectedProfile.Name);
        captured.Id = SelectedProfile.Id;
        captured.Hotkey = SelectedProfile.Model.Hotkey?.Clone();
        captured.CreatedAt = SelectedProfile.Model.CreatedAt;
        captured.UpdatedAt = DateTimeOffset.UtcNow;

        var idx = _collection.Profiles.FindIndex(p => p.Id == captured.Id);
        if (idx >= 0)
            _collection.Profiles[idx] = captured;

        PersistProfiles();
        SelectedProfile.ReplaceModel(captured);
        OnSelectedProfileChanged(SelectedProfile);
        StatusText = string.Format(Strings.StatusSaved, captured.Name);
        RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void ApplySelected()
    {
        if (SelectedProfile is null)
            return;
        ApplyProfile(SelectedProfile.Model, showBalloon: true, fromHotkey: false);
    }

    public void ApplyProfileById(string profileId, bool showBalloon = true)
    {
        var profile = _collection.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null)
            return;
        ApplyProfile(profile, showBalloon, fromHotkey: false);
    }

    private void ApplyProfile(MonitorProfile profile, bool showBalloon, bool fromHotkey)
    {
        if (_applyInProgress)
            return;

        _applyInProgress = true;
        try
        {
            var needConfirm = fromHotkey
                ? _settings.ConfirmApplyFromHotkey
                : _settings.ConfirmApplyFromUi;

            // Snapshot current layout only when we may need to roll back.
            MonitorProfile? rollback = null;
            if (needConfirm)
            {
                try
                {
                    rollback = _displayService.CaptureCurrentAsProfile("__rollback");
                }
                catch
                {
                    // Continue without rollback if capture fails; still try to apply.
                }
            }

            ApplyProfileResult result;
            try
            {
                // Work on a clone so we can normalize primary=(0,0) without mutating the saved model mid-UI.
                var working = profile.Clone(newId: false);
                MonitorMatcher.NormalizePrimaryOrigin(working.Monitors);
                result = _displayService.ApplyProfile(working);
            }
            catch (Exception ex)
            {
                StatusText = string.Format(Strings.StatusFailed, ex.Message);
                if (showBalloon && _settings.ShowNotificationOnSwitch)
                    RequestBalloon?.Invoke(this, StatusText);
                return;
            }

            if (!result.Success)
            {
                StatusText = string.Format(Strings.StatusFailed, result.Message);
                if (showBalloon && _settings.ShowNotificationOnSwitch)
                    RequestBalloon?.Invoke(this, StatusText);
                return;
            }

            // Brief UI refresh so the dialog lands on a sensible layout.
            try
            {
                RefreshCurrentMonitors();
            }
            catch
            {
                // Query can fail mid-switch; ignore.
            }

            if (needConfirm && rollback is not null)
            {
                var kept = ConfirmKeepDisplaySettings(_settings.GetConfirmTimeoutSeconds());
                if (!kept)
                {
                    RevertToProfile(rollback);
                    if (showBalloon && _settings.ShowNotificationOnSwitch)
                        RequestBalloon?.Invoke(this, StatusText);
                    return;
                }
            }

            _settings.LastAppliedProfileId = profile.Id;
            _profileStore.SaveSettings(_settings);
            foreach (var p in Profiles)
                p.IsLastApplied = p.Id == profile.Id;

            var msg = string.Format(Strings.StatusApplied, profile.Name);
            if (result.MissingMonitors.Count > 0)
                msg += " " + string.Format(Strings.MissingMonitors, string.Join(", ", result.MissingMonitors));

            StatusText = msg;

            try
            {
                RefreshCurrentMonitors();
            }
            catch
            {
                // ignore
            }

            RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);

            // Balloon is best-effort; tray may be invalid right after topology changes.
            if (showBalloon && _settings.ShowNotificationOnSwitch)
                RequestBalloon?.Invoke(this, msg);
        }
        finally
        {
            _applyInProgress = false;
        }
    }

    /// <summary>
    /// Shows the keep/revert countdown dialog. Returns true if the user keeps changes.
    /// </summary>
    private static bool ConfirmKeepDisplaySettings(int timeoutSeconds)
    {
        var dialog = new Views.ConfirmApplyDialog(timeoutSeconds);
        // CenterScreen + Topmost (set in XAML) so the prompt remains reachable after topology changes.
        return dialog.ShowDialog() == true;
    }

    /// <summary>
    /// Re-applies a previously captured layout without a second confirmation prompt.
    /// </summary>
    private void RevertToProfile(MonitorProfile rollback)
    {
        try
        {
            MonitorMatcher.NormalizePrimaryOrigin(rollback.Monitors);
            var revertResult = _displayService.ApplyProfile(rollback);
            StatusText = revertResult.Success
                ? Strings.StatusReverted
                : string.Format(Strings.StatusRevertFailed, revertResult.Message);
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Strings.StatusRevertFailed, ex.Message);
        }

        try
        {
            RefreshCurrentMonitors();
        }
        catch
        {
            // ignore
        }

        RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void DuplicateSelected()
    {
        if (SelectedProfile is null)
            return;

        var copy = SelectedProfile.Model.Clone(newId: true);
        // Clear hotkey on duplicate to avoid conflicts
        copy.Hotkey = null;
        _collection.Profiles.Add(copy);
        PersistProfiles();
        ReloadProfileList(copy.Id);
        RegisterHotkeys();
        RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void DeleteSelected()
    {
        if (SelectedProfile is null)
            return;

        var name = SelectedProfile.Name;
        var confirm = MessageBox.Show(
            string.Format(Strings.ConfirmDelete, name),
            Strings.ConfirmDeleteTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        _collection.Profiles.RemoveAll(p => p.Id == SelectedProfile.Id);
        PersistProfiles();
        ReloadProfileList();
        RegisterHotkeys();
        StatusText = Strings.StatusDeleted;
        RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void RenameSelected()
    {
        if (SelectedProfile is null)
            return;

        var dialog = new Views.InputDialog(Strings.Rename, Strings.RenamePrompt, SelectedProfile.Name);
        DialogHelper.PrepareDialog(dialog);
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResultText))
        {
            SelectedProfile.Model.Name = dialog.ResultText.Trim();
            SelectedProfile.Model.UpdatedAt = DateTimeOffset.UtcNow;
            SelectedProfile.SyncFromModel();
            PersistProfiles();
            RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool TrySetHotkey(HotkeyBinding? binding, out string error)
    {
        error = string.Empty;
        if (SelectedProfile is null)
        {
            error = Strings.NoProfileSelected;
            return false;
        }

        if (binding is not null && !binding.IsEmpty)
        {
            var conflict = _collection.Profiles.FirstOrDefault(p =>
                p.Id != SelectedProfile.Id
                && p.Hotkey is not null
                && p.Hotkey.EqualsBinding(binding));
            if (conflict is not null)
            {
                error = string.Format(Strings.HotkeyConflict, binding.ToDisplayString());
                return false;
            }
        }

        SelectedProfile.Model.Hotkey = binding is null || binding.IsEmpty ? null : binding.Clone();
        SelectedProfile.Model.UpdatedAt = DateTimeOffset.UtcNow;
        SelectedProfile.SyncFromModel();
        SelectedHotkeyDisplay = SelectedProfile.HotkeyDisplay;
        PersistProfiles();
        RegisterHotkeys();
        return true;
    }

    [RelayCommand]
    private void ClearHotkey()
    {
        TrySetHotkey(null, out _);
    }

    [RelayCommand]
    private void ShowWindow() => RequestShowWindow?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_settings.Clone(), _localization);
        var window = new Views.SettingsWindow(vm);
        // Tray-only / never-shown main window cannot be Owner (throws and aborts open).
        DialogHelper.PrepareDialog(
            window,
            Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault());

        if (window.ShowDialog() == true)
        {
            _settings = vm.Settings;
            _settings.Normalize();
            _profileStore.SaveSettings(_settings);
            _localization.ApplyLanguage(_settings.Language);
            RefreshLocalizedStrings();
            RegisterHotkeys();
            StatusText = Strings.StatusReady;
            RequestRebuildTrayMenu?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void ExitApp()
    {
        if (ExitRequested)
            return;

        ExitRequested = true;
        try
        {
            _hotkeyService.UnregisterAll();
        }
        catch
        {
            // ignore — still shut down
        }

        // Prefer App.RequestExit so the main window is force-closed under OnExplicitShutdown.
        if (Application.Current is App app)
            app.RequestExit();
        else
            Application.Current?.Shutdown();
    }

    private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Hotkey id is index into profile list (stable registration order)
            if (e.Id < 0 || e.Id >= _collection.Profiles.Count)
                return;
            var profile = _collection.Profiles[e.Id];
            ApplyProfile(profile, showBalloon: true, fromHotkey: true);
        });
    }

    private void RegisterHotkeys()
    {
        if (!_settings.HotkeysEnabled)
        {
            _hotkeyService.UnregisterAll();
            return;
        }

        var bindings = _collection.Profiles
            .Select((p, index) => (Id: index, Binding: p.Hotkey ?? new HotkeyBinding()))
            .Where(x => !x.Binding.IsEmpty)
            .Select(x => (x.Id, x.Binding));

        var results = _hotkeyService.RegisterAll(bindings);
        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Count > 0)
        {
            var first = failures[0];
            StatusText = string.Format(
                Strings.HotkeyRegisterFailed,
                first.Display,
                first.Error ?? "unknown");
        }
    }

    private void ReloadProfileList(string? selectId = null)
    {
        selectId ??= SelectedProfile?.Id ?? _settings.LastAppliedProfileId;
        Profiles.Clear();
        foreach (var p in _collection.Profiles)
        {
            var item = new ProfileItemViewModel(p)
            {
                IsLastApplied = p.Id == _settings.LastAppliedProfileId
            };
            Profiles.Add(item);
        }

        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == selectId)
                          ?? Profiles.FirstOrDefault();
    }

    private void PersistProfiles()
    {
        _profileStore.SaveProfiles(_collection);
    }
}
