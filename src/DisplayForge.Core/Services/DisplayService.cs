using System.Runtime.InteropServices;
using DisplayForge.Core.Models;
using DisplayForge.Core.Native;
using static DisplayForge.Core.Native.DisplayConfigNative;

namespace DisplayForge.Core.Services;

public sealed class DisplayService : IDisplayService
{
    public IReadOnlyList<MonitorInfo> GetCurrentMonitors(bool includeInactive = true)
    {
        if (!TryQuery(QDC_ALL_PATHS, out var paths, out var modes))
        {
            if (!TryQuery(QDC_ONLY_ACTIVE_PATHS, out paths, out modes))
                return [];
        }

        var results = new List<MonitorInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            var isActive = (path.flags & DISPLAYCONFIG_PATH_ACTIVE) != 0;
            if (!includeInactive && !isActive)
                continue;

            if (path.targetInfo.targetAvailable == 0 && !isActive)
                continue;

            var sourceName = GetSourceName(path.sourceInfo.adapterId, path.sourceInfo.id);
            var targetName = GetTargetName(path.targetInfo.adapterId, path.targetInfo.id);

            var devicePath = targetName.monitorDevicePath ?? string.Empty;
            var friendly = targetName.monitorFriendlyDeviceName ?? string.Empty;
            var gdi = sourceName.viewGdiDeviceName ?? string.Empty;
            var stableId = BuildStableId(targetName, devicePath, gdi);

            // Prefer the active path when duplicates exist for the same target.
            var key = !string.IsNullOrEmpty(stableId) ? stableId : $"{path.targetInfo.adapterId.LowPart}:{path.targetInfo.id}";
            if (seen.Contains(key))
            {
                if (!isActive)
                    continue;
                results.RemoveAll(r => string.Equals(r.StableId, key, StringComparison.OrdinalIgnoreCase));
            }

            seen.Add(key);

            int width = 0, height = 0, posX = 0, posY = 0, refresh = 0;
            var orientation = RotationToDegrees(path.targetInfo.rotation);
            var isPrimary = false;

            if (isActive && path.sourceInfo.modeInfoIdx != DISPLAYCONFIG_PATH_MODE_IDX_INVALID
                && path.sourceInfo.modeInfoIdx < modes.Length)
            {
                var mode = modes[path.sourceInfo.modeInfoIdx];
                if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Source)
                {
                    width = (int)mode.sourceMode.width;
                    height = (int)mode.sourceMode.height;
                    posX = mode.sourceMode.position.x;
                    posY = mode.sourceMode.position.y;
                    isPrimary = posX == 0 && posY == 0;
                }
            }

            if (path.targetInfo.refreshRate.Denominator != 0)
            {
                refresh = (int)Math.Round(
                    (double)path.targetInfo.refreshRate.Numerator / path.targetInfo.refreshRate.Denominator);
            }
            else if (isActive
                     && path.targetInfo.modeInfoIdx != DISPLAYCONFIG_PATH_MODE_IDX_INVALID
                     && path.targetInfo.modeInfoIdx < modes.Length)
            {
                var tMode = modes[path.targetInfo.modeInfoIdx];
                if (tMode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Target
                    && tMode.targetMode.targetVideoSignalInfo.vSyncFreq.Denominator != 0)
                {
                    refresh = (int)Math.Round(
                        (double)tMode.targetMode.targetVideoSignalInfo.vSyncFreq.Numerator
                        / tMode.targetMode.targetVideoSignalInfo.vSyncFreq.Denominator);
                }
            }

            results.Add(new MonitorInfo
            {
                StableId = stableId,
                GdiDeviceName = gdi,
                DevicePath = devicePath,
                FriendlyName = friendly,
                IsActive = isActive,
                IsPrimary = isPrimary && isActive,
                Width = width,
                Height = height,
                RefreshRate = refresh,
                Orientation = orientation,
                PositionX = posX,
                PositionY = posY,
                AdapterLuidHigh = unchecked((uint)path.sourceInfo.adapterId.HighPart),
                AdapterLuidLow = path.sourceInfo.adapterId.LowPart,
                SourceId = path.sourceInfo.id,
                TargetId = path.targetInfo.id
            });
        }

        return results
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.PositionX)
            .ThenBy(m => m.PositionY)
            .ToList();
    }

    public MonitorProfile CaptureCurrentAsProfile(string name)
    {
        var monitors = GetCurrentMonitors(includeInactive: true);
        var entries = monitors
            .Where(m => m.IsActive || !string.IsNullOrEmpty(m.DevicePath))
            .Select(m => m.ToProfileEntry())
            .ToList();

        // Only active monitors should be enabled in a fresh capture of the "current" layout.
        // Inactive but detected monitors are stored as disabled so they can be re-enabled later.
        foreach (var e in entries)
        {
            // IsActive was mapped to Enabled in ToProfileEntry.
        }

        MonitorMatcher.NormalizePrimaryOrigin(entries);

        return new MonitorProfile
        {
            Name = name,
            Monitors = entries,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public ApplyProfileResult ApplyProfile(MonitorProfile profile, int maxAttempts = 5)
    {
        if (profile.Monitors.Count == 0)
            return ApplyProfileResult.Fail("Profile has no monitors.");

        var desired = profile.Monitors.Select(m => m.Clone()).ToList();
        MonitorMatcher.NormalizePrimaryOrigin(desired);

        var enabledDesired = desired.Where(m => m.Enabled).ToList();
        if (enabledDesired.Count == 0)
            return ApplyProfileResult.Fail("Profile would disable all monitors.");

        if (!enabledDesired.Any(m => m.IsPrimary))
            enabledDesired[0].IsPrimary = true;

        var live = GetCurrentMonitors(includeInactive: true);
        var matched = new List<(ProfileMonitorEntry Entry, MonitorInfo Live)>();
        var missing = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in enabledDesired)
        {
            var liveMatch = MonitorMatcher.FindMatch(entry, live, used);
            if (liveMatch is null)
            {
                missing.Add(string.IsNullOrWhiteSpace(entry.FriendlyName)
                    ? entry.StableId
                    : entry.FriendlyName);
                continue;
            }

            used.Add(liveMatch.StableId);
            matched.Add((entry, liveMatch));
        }

        if (matched.Count == 0)
        {
            return ApplyProfileResult.Fail(
                "No monitors from the profile are currently connected.",
                missing: missing);
        }

        // Rebuild topology from ALL_PATHS so we can activate the right targets.
        if (!TryQuery(QDC_ALL_PATHS, out var allPaths, out var allModes))
            return ApplyProfileResult.Fail("QueryDisplayConfig failed.");

        var newPaths = new List<DISPLAYCONFIG_PATH_INFO>();
        var newModes = new List<DISPLAYCONFIG_MODE_INFO>();
        var sourceModeIndex = new Dictionary<(uint low, int high, uint id), uint>();
        var usedSources = new HashSet<(uint low, int high, uint id)>();
        (uint low, int high, uint id)? primarySourceKey = null;

        // Ensure primary is first for source id stability when possible.
        matched = matched
            .OrderByDescending(m => m.Entry.IsPrimary)
            .ThenBy(m => m.Entry.PositionX)
            .ThenBy(m => m.Entry.PositionY)
            .ToList();

        foreach (var (entry, liveInfo) in matched)
        {
            // Prefer a free source so re-enabled monitors join as Extend, not accidental Clone.
            // Profile SourceId is a hint (from last capture); live SourceId may be stale when OFF.
            var pathIndex = FindBestPathIndex(
                allPaths,
                liveInfo,
                usedSources,
                preferredSourceId: entry.SourceId);
            if (pathIndex < 0)
            {
                missing.Add(liveInfo.DisplayLabel);
                continue;
            }

            var path = allPaths[pathIndex];
            // Virtual-mode packed indices are not used; keep plain mode indexes.
            path.flags = (path.flags & ~DISPLAYCONFIG_PATH_SUPPORT_VIRTUAL_MODE) | DISPLAYCONFIG_PATH_ACTIVE;

            var sourceKey = (
                path.sourceInfo.adapterId.LowPart,
                path.sourceInfo.adapterId.HighPart,
                path.sourceInfo.id);
            usedSources.Add(sourceKey);

            // Track the source actually assigned to the primary (not the stale live SourceId of an OFF monitor).
            if (entry.IsPrimary)
                primarySourceKey = sourceKey;

            if (!sourceModeIndex.TryGetValue(sourceKey, out var srcModeIdx))
            {
                srcModeIdx = (uint)newModes.Count;
                sourceModeIndex[sourceKey] = srcModeIdx;

                var sourceMode = new DISPLAYCONFIG_MODE_INFO
                {
                    infoType = DISPLAYCONFIG_MODE_INFO_TYPE.Source,
                    id = path.sourceInfo.id,
                    adapterId = path.sourceInfo.adapterId,
                    sourceMode = new DISPLAYCONFIG_SOURCE_MODE
                    {
                        width = (uint)Math.Max(entry.Width, 640),
                        height = (uint)Math.Max(entry.Height, 480),
                        pixelFormat = DISPLAYCONFIG_PIXELFORMAT.PixelFormat32Bpp,
                        position = new POINTL
                        {
                            x = entry.IsPrimary ? 0 : entry.PositionX,
                            y = entry.IsPrimary ? 0 : entry.PositionY
                        }
                    }
                };
                newModes.Add(sourceMode);
            }
            else
            {
                // Clone path sharing a source: keep existing source mode (clone topology). Rare for extend profiles.
            }

            path.sourceInfo.modeInfoIdx = sourceModeIndex[sourceKey];

            // Target mode: inactive (re-enabled) paths often have INVALID mode indexes.
            // Copy existing mode when present; otherwise query the preferred mode / synthesize.
            path.targetInfo.modeInfoIdx = ResolveTargetModeIndex(
                path,
                allModes,
                newModes,
                entry);

            path.targetInfo.rotation = DegreesToRotation(entry.Orientation);
            if (entry.RefreshRate > 0)
            {
                path.targetInfo.refreshRate = new DISPLAYCONFIG_RATIONAL
                {
                    Numerator = (uint)entry.RefreshRate,
                    Denominator = 1
                };
            }
            else if (path.targetInfo.refreshRate.Denominator == 0)
            {
                path.targetInfo.refreshRate = new DISPLAYCONFIG_RATIONAL
                {
                    Numerator = 60,
                    Denominator = 1
                };
            }

            // Scaling must be set for newly activated paths; Identity/Preferred both work well.
            if (path.targetInfo.scaling == 0)
                path.targetInfo.scaling = DISPLAYCONFIG_SCALING.Preferred;

            path.targetInfo.statusFlags |= DISPLAYCONFIG_TARGET_IN_USE;
            path.sourceInfo.statusFlags |= DISPLAYCONFIG_SOURCE_IN_USE;

            newPaths.Add(path);
        }

        if (newPaths.Count == 0)
            return ApplyProfileResult.Fail("Could not build display paths for the profile.", missing: missing);

        // Force every source mode so the primary is exactly at (0,0).
        EnsurePrimaryAtOrigin(newModes, primarySourceKey);

        var pathArray = newPaths.ToArray();
        var modeArray = newModes.ToArray();

        // Prefer saving to database so the topology sticks across logon; fall back without it.
        uint[] flagSets =
        [
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES | SDC_SAVE_TO_DATABASE | SDC_ALLOW_PATH_ORDER_CHANGES,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES | SDC_ALLOW_PATH_ORDER_CHANGES,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES | SDC_SAVE_TO_DATABASE,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES
        ];

        int lastError = -1;
        for (var attempt = 0; attempt < Math.Max(1, maxAttempts); attempt++)
        {
            foreach (var applyFlags in flagSets)
            {
                lastError = SetDisplayConfig(
                    (uint)pathArray.Length,
                    pathArray,
                    (uint)modeArray.Length,
                    modeArray,
                    applyFlags);

                if (lastError == ERROR_SUCCESS)
                {
                    var msg = missing.Count > 0
                        ? $"Applied with {missing.Count} missing monitor(s)."
                        : "Profile applied.";
                    return new ApplyProfileResult
                    {
                        Success = true,
                        Message = msg,
                        MissingMonitors = missing
                    };
                }
            }

            Thread.Sleep(200 + attempt * 150);
        }

        return ApplyProfileResult.Fail(
            $"SetDisplayConfig failed (error {lastError}).",
            win32Error: lastError,
            missing: missing);
    }

    private static void EnsurePrimaryAtOrigin(
        List<DISPLAYCONFIG_MODE_INFO> modes,
        (uint low, int high, uint id)? primarySourceKey)
    {
        int primaryModeIndex = -1;

        // Prefer the source we actually assigned to the primary path (re-enabled monitors
        // often have a stale Live.SourceId of 0 that is not the path we selected).
        if (primarySourceKey is { } key)
        {
            for (var i = 0; i < modes.Count; i++)
            {
                var mode = modes[i];
                if (mode.infoType != DISPLAYCONFIG_MODE_INFO_TYPE.Source)
                    continue;

                if (mode.id == key.id
                    && mode.adapterId.LowPart == key.low
                    && mode.adapterId.HighPart == key.high)
                {
                    primaryModeIndex = i;
                    break;
                }
            }
        }

        // Fallback: first source mode (matched list is ordered with primary first when built).
        if (primaryModeIndex < 0)
        {
            for (var i = 0; i < modes.Count; i++)
            {
                if (modes[i].infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Source)
                {
                    primaryModeIndex = i;
                    break;
                }
            }
        }

        if (primaryModeIndex < 0)
            return;

        var primarySm = modes[primaryModeIndex].sourceMode;
        var dx = primarySm.position.x;
        var dy = primarySm.position.y;

        // Always force primary source to (0,0) and shift siblings by the same delta.
        for (var j = 0; j < modes.Count; j++)
        {
            if (modes[j].infoType != DISPLAYCONFIG_MODE_INFO_TYPE.Source)
                continue;

            var m = modes[j];
            var s = m.sourceMode;
            if (j == primaryModeIndex)
            {
                s.position.x = 0;
                s.position.y = 0;
            }
            else
            {
                s.position.x -= dx;
                s.position.y -= dy;
            }

            m.sourceMode = s;
            modes[j] = m;
        }
    }

    /// <summary>
    /// Pick the best ALL_PATHS entry for a live monitor.
    /// Prefers free sources (Extend), then active path, then profile/live source id hints.
    /// </summary>
    private static int FindBestPathIndex(
        DISPLAYCONFIG_PATH_INFO[] paths,
        MonitorInfo live,
        HashSet<(uint low, int high, uint id)> usedSources,
        uint preferredSourceId)
    {
        var candidates = new List<int>();

        for (var i = 0; i < paths.Length; i++)
        {
            var p = paths[i];
            if (p.targetInfo.targetAvailable == 0 && (p.flags & DISPLAYCONFIG_PATH_ACTIVE) == 0)
                continue;

            if (IsPathForLiveTarget(p, live))
                candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            // Fallback: device path / GDI name (adapter/target ids can shift after driver resets)
            for (var i = 0; i < paths.Length; i++)
            {
                var p = paths[i];
                if (p.targetInfo.targetAvailable == 0 && (p.flags & DISPLAYCONFIG_PATH_ACTIVE) == 0)
                    continue;

                var target = GetTargetName(p.targetInfo.adapterId, p.targetInfo.id);
                if (!string.IsNullOrEmpty(live.DevicePath)
                    && string.Equals(target.monitorDevicePath, live.DevicePath, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(i);
                    continue;
                }

                var source = GetSourceName(p.sourceInfo.adapterId, p.sourceInfo.id);
                if (!string.IsNullOrEmpty(live.GdiDeviceName)
                    && string.Equals(source.viewGdiDeviceName, live.GdiDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(i);
                }
            }
        }

        if (candidates.Count == 0)
            return -1;

        int Score(int index)
        {
            var p = paths[index];
            var sourceKey = (
                p.sourceInfo.adapterId.LowPart,
                p.sourceInfo.adapterId.HighPart,
                p.sourceInfo.id);
            var score = 0;

            // Highest priority: source not already taken (needed for Extend).
            if (!usedSources.Contains(sourceKey))
                score += 1000;

            // Prefer currently active path for monitors that are already on.
            if ((p.flags & DISPLAYCONFIG_PATH_ACTIVE) != 0)
                score += 200;

            // Profile source id (from capture) is the best hint for re-enable.
            if (preferredSourceId != DISPLAYCONFIG_SOURCE_ID_INVALID
                && p.sourceInfo.id == preferredSourceId)
                score += 100;

            // Live source id (may be wrong for OFF monitors — often the first path = source 0).
            if (live.IsActive && p.sourceInfo.id == live.SourceId)
                score += 50;

            // Prefer paths that already have a target mode index (less work to re-activate).
            if (p.targetInfo.modeInfoIdx != DISPLAYCONFIG_PATH_MODE_IDX_INVALID)
                score += 10;

            // Stable ordering: lower source id as weak tie-breaker.
            score -= (int)(p.sourceInfo.id & 0xFF);
            return score;
        }

        return candidates.OrderByDescending(Score).First();
    }

    private static bool IsPathForLiveTarget(DISPLAYCONFIG_PATH_INFO p, MonitorInfo live)
    {
        if (p.targetInfo.id != live.TargetId)
            return false;

        // Target adapter should match; source adapter is usually the same GPU LUID.
        return p.targetInfo.adapterId.LowPart == live.AdapterLuidLow
               && unchecked((uint)p.targetInfo.adapterId.HighPart) == live.AdapterLuidHigh
               || p.sourceInfo.adapterId.LowPart == live.AdapterLuidLow
               && unchecked((uint)p.sourceInfo.adapterId.HighPart) == live.AdapterLuidHigh;
    }

    /// <summary>
    /// Ensure the path has a valid target mode in <paramref name="newModes"/> and return its index.
    /// Inactive monitors commonly have INVALID mode indexes in QDC_ALL_PATHS.
    /// </summary>
    private static uint ResolveTargetModeIndex(
        DISPLAYCONFIG_PATH_INFO path,
        DISPLAYCONFIG_MODE_INFO[] allModes,
        List<DISPLAYCONFIG_MODE_INFO> newModes,
        ProfileMonitorEntry entry)
    {
        if (path.targetInfo.modeInfoIdx != DISPLAYCONFIG_PATH_MODE_IDX_INVALID
            && path.targetInfo.modeInfoIdx < allModes.Length
            && allModes[path.targetInfo.modeInfoIdx].infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Target)
        {
            var existing = allModes[path.targetInfo.modeInfoIdx];
            // Ensure the mode's target id matches; ALL_PATHS can share indices oddly after topology changes.
            if (existing.id == path.targetInfo.id
                || existing.adapterId.LowPart == path.targetInfo.adapterId.LowPart)
            {
                if (entry.RefreshRate > 0)
                {
                    var tvi = existing.targetMode.targetVideoSignalInfo;
                    tvi.vSyncFreq.Numerator = (uint)entry.RefreshRate;
                    tvi.vSyncFreq.Denominator = 1;
                    var tm = existing.targetMode;
                    tm.targetVideoSignalInfo = tvi;
                    existing.targetMode = tm;
                }

                // Align active size with requested resolution when we have a known size.
                if (entry.Width > 0 && entry.Height > 0)
                {
                    var tvi = existing.targetMode.targetVideoSignalInfo;
                    if (tvi.activeSize.cx == 0 || tvi.activeSize.cy == 0)
                    {
                        tvi.activeSize.cx = (uint)entry.Width;
                        tvi.activeSize.cy = (uint)entry.Height;
                        if (tvi.totalSize.cx == 0 || tvi.totalSize.cy == 0)
                        {
                            tvi.totalSize.cx = (uint)entry.Width;
                            tvi.totalSize.cy = (uint)entry.Height;
                        }

                        var tm = existing.targetMode;
                        tm.targetVideoSignalInfo = tvi;
                        existing.targetMode = tm;
                    }
                }

                var idx = (uint)newModes.Count;
                newModes.Add(existing);
                return idx;
            }
        }

        // Preferred mode from the driver — works for monitors that are connected but currently OFF.
        if (TryGetPreferredTargetMode(path.targetInfo.adapterId, path.targetInfo.id, out var preferred))
        {
            var mode = new DISPLAYCONFIG_MODE_INFO
            {
                infoType = DISPLAYCONFIG_MODE_INFO_TYPE.Target,
                id = path.targetInfo.id,
                adapterId = path.targetInfo.adapterId,
                targetMode = preferred.targetMode
            };

            if (entry.RefreshRate > 0)
            {
                var tvi = mode.targetMode.targetVideoSignalInfo;
                tvi.vSyncFreq.Numerator = (uint)entry.RefreshRate;
                tvi.vSyncFreq.Denominator = 1;
                var tm = mode.targetMode;
                tm.targetVideoSignalInfo = tvi;
                mode.targetMode = tm;
            }

            var idx = (uint)newModes.Count;
            newModes.Add(mode);
            return idx;
        }

        // Last resort: synthesize a basic target mode from the profile entry.
        {
            var width = (uint)Math.Max(entry.Width > 0 ? entry.Width : 1920, 640);
            var height = (uint)Math.Max(entry.Height > 0 ? entry.Height : 1080, 480);
            var refresh = (uint)Math.Max(entry.RefreshRate > 0 ? entry.RefreshRate : 60, 1);

            var mode = new DISPLAYCONFIG_MODE_INFO
            {
                infoType = DISPLAYCONFIG_MODE_INFO_TYPE.Target,
                id = path.targetInfo.id,
                adapterId = path.targetInfo.adapterId,
                targetMode = new DISPLAYCONFIG_TARGET_MODE
                {
                    targetVideoSignalInfo = new DISPLAYCONFIG_VIDEO_SIGNAL_INFO
                    {
                        pixelRate = (ulong)width * height * refresh,
                        hSyncFreq = new DISPLAYCONFIG_RATIONAL { Numerator = refresh * height, Denominator = 1 },
                        vSyncFreq = new DISPLAYCONFIG_RATIONAL { Numerator = refresh, Denominator = 1 },
                        activeSize = new DISPLAYCONFIG_2DREGION { cx = width, cy = height },
                        totalSize = new DISPLAYCONFIG_2DREGION { cx = width, cy = height },
                        videoStandard = 0,
                        scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING.Progressive
                    }
                }
            };

            var idx = (uint)newModes.Count;
            newModes.Add(mode);
            return idx;
        }
    }

    private static bool TryGetPreferredTargetMode(
        LUID adapterId,
        uint targetId,
        out DISPLAYCONFIG_TARGET_PREFERRED_MODE preferred)
    {
        preferred = new DISPLAYCONFIG_TARGET_PREFERRED_MODE
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE,
                size = Marshal.SizeOf<DISPLAYCONFIG_TARGET_PREFERRED_MODE>(),
                adapterId = adapterId,
                id = targetId
            }
        };

        return DisplayConfigGetDeviceInfo(ref preferred) == ERROR_SUCCESS
               && preferred.width > 0
               && preferred.height > 0;
    }

    private static bool TryQuery(
        uint flags,
        out DISPLAYCONFIG_PATH_INFO[] paths,
        out DISPLAYCONFIG_MODE_INFO[] modes)
    {
        paths = [];
        modes = [];

        var err = GetDisplayConfigBufferSizes(flags, out var pathCount, out var modeCount);
        if (err != ERROR_SUCCESS)
            return false;

        paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
        err = QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
        if (err != ERROR_SUCCESS)
            return false;

        if (pathCount != paths.Length)
            Array.Resize(ref paths, (int)pathCount);
        if (modeCount != modes.Length)
            Array.Resize(ref modes, (int)modeCount);

        return true;
    }

    private static DISPLAYCONFIG_SOURCE_DEVICE_NAME GetSourceName(LUID adapterId, uint sourceId)
    {
        var name = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                size = Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                adapterId = adapterId,
                id = sourceId
            },
            viewGdiDeviceName = string.Empty
        };
        _ = DisplayConfigGetDeviceInfo(ref name);
        return name;
    }

    private static DISPLAYCONFIG_TARGET_DEVICE_NAME GetTargetName(LUID adapterId, uint targetId)
    {
        var name = new DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                size = Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                adapterId = adapterId,
                id = targetId
            },
            monitorFriendlyDeviceName = string.Empty,
            monitorDevicePath = string.Empty
        };
        _ = DisplayConfigGetDeviceInfo(ref name);
        return name;
    }

    private static string BuildStableId(
        DISPLAYCONFIG_TARGET_DEVICE_NAME target,
        string devicePath,
        string gdiName)
    {
        // Prefer EDID manufacturer + product + connector instance
        if (target.edidManufactureId != 0 || target.edidProductCodeId != 0)
        {
            var mfg = DecodeEdidManufacturer(target.edidManufactureId);
            return $"{mfg}{target.edidProductCodeId:X4}#{target.connectorInstance}";
        }

        if (!string.IsNullOrWhiteSpace(devicePath))
        {
            // Extract a short stable fragment from the device path
            var parts = devicePath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            var monitorPart = parts.FirstOrDefault(p => p.StartsWith("DISPLAY", StringComparison.OrdinalIgnoreCase)
                                                       || p.Contains('#')
                                                       || p.Length > 4);
            if (!string.IsNullOrEmpty(monitorPart))
                return monitorPart;
            return devicePath;
        }

        return string.IsNullOrEmpty(gdiName) ? Guid.NewGuid().ToString("N")[..8] : gdiName;
    }

    private static string DecodeEdidManufacturer(ushort id)
    {
        // EDID manufacturer is 3 letters packed into 15 bits.
        if (id == 0)
            return "UNK";

        var c1 = (char)('A' + ((id >> 10) & 0x1F) - 1);
        var c2 = (char)('A' + ((id >> 5) & 0x1F) - 1);
        var c3 = (char)('A' + (id & 0x1F) - 1);
        if (c1 is < 'A' or > 'Z' || c2 is < 'A' or > 'Z' || c3 is < 'A' or > 'Z')
            return $"M{id:X4}";
        return new string([c1, c2, c3]);
    }

    private static int RotationToDegrees(DISPLAYCONFIG_ROTATION rotation) => rotation switch
    {
        DISPLAYCONFIG_ROTATION.Rotate90 => 90,
        DISPLAYCONFIG_ROTATION.Rotate180 => 180,
        DISPLAYCONFIG_ROTATION.Rotate270 => 270,
        _ => 0
    };

    private static DISPLAYCONFIG_ROTATION DegreesToRotation(int degrees) => (degrees % 360) switch
    {
        90 => DISPLAYCONFIG_ROTATION.Rotate90,
        180 => DISPLAYCONFIG_ROTATION.Rotate180,
        270 => DISPLAYCONFIG_ROTATION.Rotate270,
        _ => DISPLAYCONFIG_ROTATION.Identity
    };
}
