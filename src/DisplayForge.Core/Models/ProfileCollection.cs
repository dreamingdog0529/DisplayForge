namespace DisplayForge.Core.Models;

public sealed class ProfileCollection
{
    public int Version { get; set; } = 1;

    public List<MonitorProfile> Profiles { get; set; } = [];
}
