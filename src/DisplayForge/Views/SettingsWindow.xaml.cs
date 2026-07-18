using System.Windows;
using DisplayForge.Resources;
using DisplayForge.ViewModels;

namespace DisplayForge.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        ApplyLabels();
    }

    private void ApplyLabels()
    {
        Title = Strings.Settings;
        LblLanguage.Text = Strings.Language;
        LblGeneralSection.Text = Strings.GeneralSection;
        ChkStartMinimized.Content = Strings.StartMinimized;
        ChkNotify.Content = Strings.ShowNotifications;
        ChkHotkeys.Content = Strings.HotkeysEnabled;
        LblConfirmSection.Text = Strings.ConfirmApplySection;
        LblConfirmHint.Text = Strings.ConfirmApplyHint;
        ChkConfirmUi.Content = Strings.ConfirmApplyFromUi;
        ChkConfirmHotkey.Content = Strings.ConfirmApplyFromHotkey;
        LblConfirmTimeout.Text = Strings.ConfirmApplyTimeoutSeconds;
        BtnSave.Content = Strings.Save;
        BtnCancel.Content = Strings.Cancel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedLanguage is not null)
            _vm.Settings.Language = _vm.SelectedLanguage.Code;
        _vm.Settings.Normalize();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
