namespace DisplayForge.Core.Models;

/// <summary>
/// Live monitor information returned by <see cref="Services.IDisplayService"/>.
/// </summary>
public sealed class MonitorInfo
{
    public string StableId { get; init; } = string.Empty;
    public string GdiDeviceName { get; init; } = string.Empty;
    public string DevicePath { get; init; } = string.Empty;
    public string FriendlyName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsPrimary { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int RefreshRate { get; init; }
    public int Orientation { get; init; }
    public int PositionX { get; init; }
    public int PositionY { get; init; }
    public uint AdapterLuidHigh { get; init; }
    public uint AdapterLuidLow { get; init; }
    public uint SourceId { get; init; }
    public uint TargetId { get; init; }

    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(FriendlyName)
            ? (string.IsNullOrWhiteSpace(GdiDeviceName) ? StableId : GdiDeviceName)
            : FriendlyName;

    public ProfileMonitorEntry ToProfileEntry() => new()
    {
        StableId = StableId,
        GdiDeviceName = GdiDeviceName,
        DevicePath = DevicePath,
        FriendlyName = FriendlyName,
        Enabled = IsActive,
        IsPrimary = IsPrimary,
        Width = Width,
        Height = Height,
        RefreshRate = RefreshRate,
        Orientation = Orientation,
        PositionX = PositionX,
        PositionY = PositionY,
        AdapterLuidHigh = AdapterLuidHigh,
        AdapterLuidLow = AdapterLuidLow,
        SourceId = SourceId,
        TargetId = TargetId
    };
}
