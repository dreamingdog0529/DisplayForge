namespace DisplayForge.Core.Models;

public sealed class MonitorProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "New Profile";

    public HotkeyBinding? Hotkey { get; set; }

    public List<ProfileMonitorEntry> Monitors { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public MonitorProfile Clone(bool newId = true) => new()
    {
        Id = newId ? Guid.NewGuid().ToString("N") : Id,
        Name = newId ? $"{Name} (copy)" : Name,
        Hotkey = Hotkey?.Clone(),
        Monitors = Monitors.Select(m => m.Clone()).ToList(),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
