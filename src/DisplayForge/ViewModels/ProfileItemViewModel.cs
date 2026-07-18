using CommunityToolkit.Mvvm.ComponentModel;
using DisplayForge.Core.Models;

namespace DisplayForge.ViewModels;

public partial class ProfileItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _hotkeyDisplay = string.Empty;

    [ObservableProperty]
    private bool _isLastApplied;

    public MonitorProfile Model { get; private set; }

    public ProfileItemViewModel(MonitorProfile model)
    {
        Model = model;
        SyncFromModel();
    }

    public void SyncFromModel()
    {
        Id = Model.Id;
        Name = Model.Name;
        HotkeyDisplay = Model.Hotkey?.ToDisplayString() ?? string.Empty;
    }

    public void ReplaceModel(MonitorProfile model)
    {
        Model = model;
        SyncFromModel();
    }
}
