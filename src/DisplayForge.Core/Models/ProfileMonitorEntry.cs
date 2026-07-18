namespace DisplayForge.Core.Models;

/// <summary>
/// One monitor's configuration stored inside a profile.
/// </summary>
public sealed class ProfileMonitorEntry
{
    /// <summary>Stable identity: preferably manufacturer+product+serial from EDID.</summary>
    public string StableId { get; set; } = string.Empty;

    /// <summary>GDI device name such as \\.\DISPLAY1 (may change across sessions).</summary>
    public string GdiDeviceName { get; set; } = string.Empty;

    /// <summary>Device path from DisplayConfig (more stable than GDI name).</summary>
    public string DevicePath { get; set; } = string.Empty;

    public string FriendlyName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public bool IsPrimary { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int RefreshRate { get; set; }

    /// <summary>0, 90, 180, or 270 degrees.</summary>
    public int Orientation { get; set; }

    public int PositionX { get; set; }

    public int PositionY { get; set; }

    public int BitsPerPixel { get; set; } = 32;

    public uint AdapterLuidHigh { get; set; }

    public uint AdapterLuidLow { get; set; }

    public uint SourceId { get; set; }

    public uint TargetId { get; set; }

    public ProfileMonitorEntry Clone() => new()
    {
        StableId = StableId,
        GdiDeviceName = GdiDeviceName,
        DevicePath = DevicePath,
        FriendlyName = FriendlyName,
        Enabled = Enabled,
        IsPrimary = IsPrimary,
        Width = Width,
        Height = Height,
        RefreshRate = RefreshRate,
        Orientation = Orientation,
        PositionX = PositionX,
        PositionY = PositionY,
        BitsPerPixel = BitsPerPixel,
        AdapterLuidHigh = AdapterLuidHigh,
        AdapterLuidLow = AdapterLuidLow,
        SourceId = SourceId,
        TargetId = TargetId
    };
}
