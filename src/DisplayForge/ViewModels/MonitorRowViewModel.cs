using CommunityToolkit.Mvvm.ComponentModel;
using DisplayForge.Core.Models;
using DisplayForge.Core.Services;

namespace DisplayForge.ViewModels;

public partial class MonitorRowViewModel : ObservableObject
{
    private readonly ProfileMonitorEntry? _entry;
    private readonly Action<MonitorRowViewModel, bool, MonitorMatcher.StructuralFlagIntent>? _onProfileChanged;
    private bool _suppressNotify;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isPrimary;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private int _width;

    [ObservableProperty]
    private int _height;

    [ObservableProperty]
    private int _refreshRate;

    [ObservableProperty]
    private int _positionX;

    [ObservableProperty]
    private int _positionY;

    /// <summary>0, 90, 180, or 270.</summary>
    [ObservableProperty]
    private int _orientationDegrees;

    /// <summary>True when this row belongs to an editable profile (not live topology).</summary>
    public bool IsEditable => _entry is not null;

    public ProfileMonitorEntry? Entry => _entry;

    public static IReadOnlyList<int> OrientationOptions { get; } = [0, 90, 180, 270];

    private MonitorRowViewModel()
    {
    }

    private MonitorRowViewModel(
        ProfileMonitorEntry entry,
        Action<MonitorRowViewModel, bool, MonitorMatcher.StructuralFlagIntent> onProfileChanged)
    {
        _entry = entry;
        _onProfileChanged = onProfileChanged;
    }

    public static MonitorRowViewModel FromLive(MonitorInfo m) => new()
    {
        Name = m.DisplayLabel,
        IsPrimary = m.IsPrimary,
        IsEnabled = m.IsActive,
        Width = m.Width,
        Height = m.Height,
        RefreshRate = m.RefreshRate,
        PositionX = m.PositionX,
        PositionY = m.PositionY,
        OrientationDegrees = m.Orientation
    };

    public static MonitorRowViewModel FromProfile(
        ProfileMonitorEntry m,
        Action<MonitorRowViewModel, bool, MonitorMatcher.StructuralFlagIntent> onProfileChanged)
    {
        var row = new MonitorRowViewModel(m, onProfileChanged);
        row.SyncFromEntry();
        return row;
    }

    public void SyncFromEntry()
    {
        if (_entry is null)
            return;

        _suppressNotify = true;
        try
        {
            Name = string.IsNullOrWhiteSpace(_entry.FriendlyName) ? _entry.StableId : _entry.FriendlyName;
            IsPrimary = _entry.IsPrimary;
            IsEnabled = _entry.Enabled;
            Width = _entry.Width;
            Height = _entry.Height;
            RefreshRate = _entry.RefreshRate;
            PositionX = _entry.PositionX;
            PositionY = _entry.PositionY;
            OrientationDegrees = NormalizeOrientation(_entry.Orientation);
        }
        finally
        {
            _suppressNotify = false;
        }
    }

    partial void OnNameChanged(string value)
    {
        if (_suppressNotify || _entry is null)
            return;

        // Keep StableId; only update friendly display name when user renames.
        if (!string.IsNullOrWhiteSpace(value) && value != _entry.StableId)
            _entry.FriendlyName = value.Trim();

        NotifyGeometryChanged();
    }

    partial void OnIsPrimaryChanged(bool value)
    {
        if (_suppressNotify || _entry is null)
            return;

        if (value)
        {
            // Atomic intent: become exclusive primary AND enabled before any enforcement.
            // Do this before notify so enable+primary in one click cannot race.
            _entry.IsPrimary = true;
            _entry.Enabled = true;
            _suppressNotify = true;
            try
            {
                if (!IsEnabled)
                    IsEnabled = true;
            }
            finally
            {
                _suppressNotify = false;
            }

            NotifyStructuralChanged(MonitorMatcher.StructuralFlagIntent.SetPrimary);
            return;
        }

        _entry.IsPrimary = false;
        NotifyStructuralChanged(MonitorMatcher.StructuralFlagIntent.ClearPrimary);
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_suppressNotify || _entry is null)
            return;

        _entry.Enabled = value;
        if (!value)
        {
            // Disabled cannot be primary — keep Entry and VM in sync.
            _entry.IsPrimary = false;
            if (IsPrimary)
                SetIsPrimarySilently(false);

            NotifyStructuralChanged(MonitorMatcher.StructuralFlagIntent.None);
            return;
        }

        // Enabling a row that is already marked primary (e.g. checked Primary first in UI,
        // or re-enable after disable while primary flag was restored) must re-assert exclusive primary.
        var intent = _entry.IsPrimary || IsPrimary
            ? MonitorMatcher.StructuralFlagIntent.SetPrimary
            : MonitorMatcher.StructuralFlagIntent.None;

        if (intent == MonitorMatcher.StructuralFlagIntent.SetPrimary)
        {
            _entry.IsPrimary = true;
            if (!IsPrimary)
                SetIsPrimarySilently(true);
        }

        NotifyStructuralChanged(intent);
    }

    private void SetIsEnabledSilently(bool value)
    {
        _suppressNotify = true;
        try
        {
            IsEnabled = value;
        }
        finally
        {
            _suppressNotify = false;
        }
    }

    private void SetIsPrimarySilently(bool value)
    {
        _suppressNotify = true;
        try
        {
            IsPrimary = value;
        }
        finally
        {
            _suppressNotify = false;
        }
    }

    partial void OnWidthChanged(int value)
    {
        if (_suppressNotify || _entry is null)
            return;

        _entry.Width = Math.Clamp(value, 0, 16384);
        NotifyGeometryChanged();
    }

    partial void OnHeightChanged(int value)
    {
        if (_suppressNotify || _entry is null)
            return;

        _entry.Height = Math.Clamp(value, 0, 16384);
        NotifyGeometryChanged();
    }

    partial void OnRefreshRateChanged(int value)
    {
        if (_suppressNotify || _entry is null)
            return;

        _entry.RefreshRate = Math.Clamp(value, 0, 1000);
        NotifyGeometryChanged();
    }

    partial void OnPositionXChanged(int value)
    {
        if (_suppressNotify || _entry is null)
            return;

        _entry.PositionX = value;
        NotifyGeometryChanged();
    }

    partial void OnPositionYChanged(int value)
    {
        if (_suppressNotify || _entry is null)
            return;

        _entry.PositionY = value;
        NotifyGeometryChanged();
    }

    partial void OnOrientationDegreesChanged(int value)
    {
        if (_suppressNotify || _entry is null)
            return;

        _entry.Orientation = NormalizeOrientation(value);
        NotifyGeometryChanged();
    }

    /// <summary>
    /// Update position without persisting (used while dragging in the layout editor).
    /// </summary>
    public void SetPositionSilently(int x, int y)
    {
        _suppressNotify = true;
        try
        {
            PositionX = x;
            PositionY = y;
            if (_entry is not null)
            {
                _entry.PositionX = x;
                _entry.PositionY = y;
            }
        }
        finally
        {
            _suppressNotify = false;
        }
    }

    /// <summary>
    /// Persist a position change after a silent drag sequence.
    /// </summary>
    public void CommitPositionChange()
    {
        if (_entry is null)
            return;

        NotifyGeometryChanged();
    }

    private void NotifyStructuralChanged(MonitorMatcher.StructuralFlagIntent intent) =>
        _onProfileChanged?.Invoke(this, true, intent);

    private void NotifyGeometryChanged() =>
        _onProfileChanged?.Invoke(this, false, MonitorMatcher.StructuralFlagIntent.None);

    private static int NormalizeOrientation(int degrees) => (degrees % 360) switch
    {
        90 or -270 => 90,
        180 or -180 => 180,
        270 or -90 => 270,
        _ => 0
    };
}
