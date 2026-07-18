using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayForge.Core.Models;
using DisplayForge.Services;

namespace DisplayForge.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;

    public AppSettings Settings { get; }

    public IReadOnlyList<LanguageOption> Languages { get; }

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    public SettingsViewModel(AppSettings settings, ILocalizationService localization)
    {
        Settings = settings;
        _localization = localization;

        Languages = localization.GetLanguageOptions();

        SelectedLanguage = Languages.FirstOrDefault(l =>
                               string.Equals(l.Code, settings.Language, StringComparison.OrdinalIgnoreCase))
                           ?? Languages[0];
    }

    [ObservableProperty]
    private bool _dialogResult;

    [RelayCommand]
    private void Save()
    {
        if (SelectedLanguage is not null)
            Settings.Language = SelectedLanguage.Code;
        DialogResult = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
    }
}
