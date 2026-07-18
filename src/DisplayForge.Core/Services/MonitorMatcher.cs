using DisplayForge.Core.Models;

namespace DisplayForge.Core.Services;

/// <summary>
/// Matches profile monitor entries to currently connected monitors.
/// </summary>
public static class MonitorMatcher
{
    public static MonitorInfo? FindMatch(
        ProfileMonitorEntry entry,
        IReadOnlyList<MonitorInfo> liveMonitors,
        HashSet<string>? alreadyMatchedStableIds = null)
    {
        alreadyMatchedStableIds ??= [];

        // 1. Exact stable id
        var match = liveMonitors.FirstOrDefault(m =>
            !alreadyMatchedStableIds.Contains(m.StableId)
            && !string.IsNullOrEmpty(entry.StableId)
            && string.Equals(m.StableId, entry.StableId, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return match;

        // 2. Device path
        match = liveMonitors.FirstOrDefault(m =>
            !alreadyMatchedStableIds.Contains(m.StableId)
            && !string.IsNullOrEmpty(entry.DevicePath)
            && !string.IsNullOrEmpty(m.DevicePath)
            && string.Equals(m.DevicePath, entry.DevicePath, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return match;

        // 3. GDI device name
        match = liveMonitors.FirstOrDefault(m =>
            !alreadyMatchedStableIds.Contains(m.StableId)
            && !string.IsNullOrEmpty(entry.GdiDeviceName)
            && string.Equals(m.GdiDeviceName, entry.GdiDeviceName, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return match;

        // 4. Friendly name (last resort)
        if (!string.IsNullOrWhiteSpace(entry.FriendlyName))
        {
            match = liveMonitors.FirstOrDefault(m =>
                !alreadyMatchedStableIds.Contains(m.StableId)
                && string.Equals(m.FriendlyName, entry.FriendlyName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return null;
    }

    /// <summary>
    /// Why a structural (Enabled / Primary) edit was made. Used so "make primary"
    /// still wins when enable+primary are applied together and intermediate state is messy.
    /// </summary>
    public enum StructuralFlagIntent
    {
        /// <summary>No explicit primary intent (enable/disable only, or generic repair).</summary>
        None = 0,

        /// <summary>User asked for this monitor to become the exclusive primary (implies enable).</summary>
        SetPrimary = 1,

        /// <summary>User cleared primary on this monitor (another enabled monitor will take over).</summary>
        ClearPrimary = 2
    }

    /// <summary>
    /// Enforce profile invariants for Enabled / Primary after a structural edit:
    /// <list type="bullet">
    /// <item>At least one monitor stays enabled</item>
    /// <item>Disabled monitors cannot be primary</item>
    /// <item>Exactly one primary among enabled monitors</item>
    /// <item>
    /// When <paramref name="intent"/> is <see cref="StructuralFlagIntent.SetPrimary"/>,
    /// or <paramref name="edited"/> is marked primary, that entry becomes the exclusive
    /// primary and is force-enabled — even if it was disabled before the edit.
    /// </item>
    /// </list>
    /// Then normalizes coordinates so the primary is at (0,0) and not overlapped.
    /// </summary>
    /// <param name="monitors">All profile monitor entries.</param>
    /// <param name="edited">
    /// The entry the user just changed. Used to honour an explicit primary selection
    /// and to restore enable if the user tried to disable the last monitor.
    /// </param>
    /// <param name="intent">
    /// Explicit user intent. Prefer <see cref="StructuralFlagIntent.SetPrimary"/> when the
    /// user checked Primary (including on a previously disabled row).
    /// </param>
    public static void EnforceEnabledAndPrimary(
        IList<ProfileMonitorEntry> monitors,
        ProfileMonitorEntry? edited = null,
        StructuralFlagIntent intent = StructuralFlagIntent.None)
    {
        if (monitors.Count == 0)
            return;

        // At least one monitor must stay enabled.
        if (monitors.All(m => !m.Enabled))
        {
            var restore = edited is not null && monitors.Any(m => ReferenceEquals(m, edited))
                ? edited
                : monitors[0];
            restore.Enabled = true;
        }

        var editedInList = edited is not null && monitors.Any(m => ReferenceEquals(m, edited));

        // User explicitly chose this monitor as primary — always wins, force-enable.
        // Also honour entry.IsPrimary when intent is None (e.g. loaded/partial state repair).
        var makeEditedPrimary = editedInList
            && (intent == StructuralFlagIntent.SetPrimary
                || (intent != StructuralFlagIntent.ClearPrimary && edited!.IsPrimary));

        if (makeEditedPrimary)
        {
            edited!.Enabled = true;
            edited.IsPrimary = true;
            foreach (var m in monitors)
                m.IsPrimary = ReferenceEquals(m, edited);
        }
        else if (intent == StructuralFlagIntent.ClearPrimary && editedInList)
        {
            edited!.IsPrimary = false;
        }

        // Disabled monitors cannot be primary.
        foreach (var m in monitors)
        {
            if (!m.Enabled)
                m.IsPrimary = false;
        }

        // Exactly one primary among enabled monitors.
        var enabled = monitors.Where(m => m.Enabled).ToList();
        if (enabled.Count == 0)
            return;

        var primaries = enabled.Where(m => m.IsPrimary).ToList();
        if (primaries.Count == 0)
        {
            // Do NOT promote the monitor that was merely enabled — only fill a missing primary.
            // Prefer an entry that was primary before disable, else the first enabled.
            var pick = enabled[0];
            pick.IsPrimary = true;
            foreach (var m in monitors.Where(x => !ReferenceEquals(x, pick)))
                m.IsPrimary = false;
        }
        else if (primaries.Count > 1)
        {
            // Prefer edited when it is one of the primaries; otherwise the first.
            var keep = editedInList && primaries.Any(p => ReferenceEquals(p, edited))
                ? edited!
                : primaries[0];
            foreach (var m in monitors)
                m.IsPrimary = ReferenceEquals(m, keep);
        }

        NormalizePrimaryOrigin(monitors);
    }

    /// <summary>
    /// Normalize positions so the primary monitor is at (0,0), and shift siblings that
    /// would otherwise stack on the same origin (common when re-enabling an inactive capture).
    /// </summary>
    public static void NormalizePrimaryOrigin(IList<ProfileMonitorEntry> monitors)
    {
        var primary = monitors.FirstOrDefault(m => m.IsPrimary && m.Enabled)
                      ?? monitors.FirstOrDefault(m => m.Enabled);
        if (primary is null)
            return;

        var ox = primary.PositionX;
        var oy = primary.PositionY;
        if (ox != 0 || oy != 0)
        {
            foreach (var m in monitors)
            {
                m.PositionX -= ox;
                m.PositionY -= oy;
            }
        }

        primary.PositionX = 0;
        primary.PositionY = 0;
        primary.IsPrimary = true;
        foreach (var m in monitors.Where(x => !ReferenceEquals(x, primary)))
            m.IsPrimary = false;

        // Inactive captures often sit at (0,0). After promotion, push siblings off the primary rect.
        SeparateOverlappingWithPrimary(monitors, primary);
    }

    /// <summary>
    /// Place enabled non-primary monitors that still overlap the primary's top-left
    /// to the right of the primary (simple pack; keeps extend topology valid).
    /// </summary>
    private static void SeparateOverlappingWithPrimary(
        IList<ProfileMonitorEntry> monitors,
        ProfileMonitorEntry primary)
    {
        var primaryW = Math.Max(primary.Width, 640);
        var cursorX = primaryW;

        foreach (var m in monitors)
        {
            if (ReferenceEquals(m, primary) || !m.Enabled)
                continue;

            // Overlap primary origin (or exact same position).
            if (m.PositionX == 0 && m.PositionY == 0)
            {
                m.PositionX = cursorX;
                m.PositionY = 0;
                cursorX += Math.Max(m.Width, 640);
            }
        }
    }
}
