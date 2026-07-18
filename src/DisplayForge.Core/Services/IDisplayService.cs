using DisplayForge.Core.Models;

namespace DisplayForge.Core.Services;

public interface IDisplayService
{
    IReadOnlyList<MonitorInfo> GetCurrentMonitors(bool includeInactive = true);

    MonitorProfile CaptureCurrentAsProfile(string name);

    ApplyProfileResult ApplyProfile(MonitorProfile profile, int maxAttempts = 5);
}
