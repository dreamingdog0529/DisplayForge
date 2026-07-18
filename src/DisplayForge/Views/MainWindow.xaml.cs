using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DisplayForge.Core.Models;
using DisplayForge.Resources;
using DisplayForge.ViewModels;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using TextBox = System.Windows.Controls.TextBox;

namespace DisplayForge.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _forceClose;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        HotkeyBox.HotkeyCaptured += OnHotkeyCaptured;
        vm.LanguageChanged += (_, _) => ApplyLabels();
        vm.PropertyChanged += OnViewModelPropertyChanged;
        ApplyLabels();
        SyncHotkeyBox();

        Closing += OnClosing;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SelectedHotkeyDisplay)
            or nameof(MainViewModel.SelectedProfile))
        {
            SyncHotkeyBox();
        }
    }

    private void SyncHotkeyBox()
    {
        HotkeyBox.Hotkey = _vm.SelectedProfile?.Model.Hotkey?.Clone();
        HotkeyBox.Text = _vm.SelectedHotkeyDisplay;
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void ApplyLabels()
    {
        BtnNewProfile.Content = Strings.NewProfile;
        BtnNewProfile.ToolTip = Strings.NewProfileHint;
        BtnOverwrite.Content = Strings.SaveToProfile;
        BtnOverwrite.ToolTip = Strings.SaveToProfileHint;
        BtnApply.Content = Strings.Apply;
        BtnApply.ToolTip = Strings.ApplyHint;
        BtnDuplicate.Content = Strings.Duplicate;
        BtnRename.Content = Strings.Rename;
        BtnDelete.Content = Strings.Delete;
        BtnSettings.Content = Strings.Settings;
        BtnRefresh.Content = Strings.RefreshMonitors;
        BtnClearHotkey.Content = Strings.ClearHotkey;
        BtnIdentify.Content = Strings.IdentifyMonitors;
        BtnIdentify.ToolTip = Strings.IdentifyMonitorsHint;
        LblAppTagline.Text = Strings.AppTagline;
        LblProfiles.Text = Strings.Profiles;
        LblSelectedSection.Text = Strings.SelectedProfileSection;
        LblHotkey.Text = Strings.Hotkey;
        LblHotkeyHint.Text = Strings.HotkeyHint;
        LblProfileMonitors.Text = Strings.ProfileMonitors;
        LblProfileMonitorsHint.Text = Strings.ProfileMonitorsHint;
        LblCurrentMonitors.Text = Strings.CurrentMonitors;
        LblLayoutEditor.Text = Strings.LayoutEditor;
        LblLayoutEditorHint.Text = Strings.LayoutEditorHint;
        LblEmptyState.Text = Strings.EmptyStateNoProfiles;
        ProfileLayoutEditor.RefreshLabels();
        HotkeyBox.ToolTip = Strings.HotkeyHint;
        SyncHotkeyBox();

        ColPName.Header = Strings.Name;
        ColPPrimary.Header = Strings.Primary;
        ColPEnabled.Header = Strings.Enabled;
        ColPWidth.Header = Strings.Width;
        ColPHeight.Header = Strings.Height;
        ColPHz.Header = Strings.Refresh;
        ColPPosX.Header = Strings.PosX;
        ColPPosY.Header = Strings.PosY;
        ColPOri.Header = Strings.Orientation;

        ColCName.Header = Strings.Name;
        ColCPrimary.Header = Strings.Primary;
        ColCEnabled.Header = Strings.Enabled;
        ColCWidth.Header = Strings.Width;
        ColCHeight.Header = Strings.Height;
        ColCHz.Header = Strings.Refresh;
        ColCPosX.Header = Strings.PosX;
        ColCPosY.Header = Strings.PosY;
        ColCOri.Header = Strings.Orientation;
    }

    private void OnHotkeyCaptured(object? sender, HotkeyBinding? binding)
    {
        if (!_vm.TrySetHotkey(binding, out var error))
        {
            MessageBox.Show(error, Strings.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            SyncHotkeyBox();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_forceClose || _vm.ExitRequested)
            return;

        // Under `dotnet run` (and similar), exit fully so the shell is not left blocked.
        // Normal desktop launches keep the tray-resident behavior.
        if (!App.MinimizeToTrayOnClose)
        {
            // Cancel this close and exit asynchronously — calling Close/Shutdown
            // re-entrantly from inside Closing is unsafe.
            e.Cancel = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_vm.ExitRequested)
                    _vm.ExitAppCommand.Execute(null);
            }));
            return;
        }

        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// Select all text when starting to edit a numeric cell so typing replaces the value easily.
    /// </summary>
    private void ProfileMonitorsGrid_OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.EditingElement is TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
            // Commit on Enter
            textBox.KeyDown -= EditingTextBox_OnKeyDown;
            textBox.KeyDown += EditingTextBox_OnKeyDown;
        }
    }

    private void EditingTextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox)
        {
            ProfileMonitorsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ProfileMonitorsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            e.Handled = true;
        }
    }
}
