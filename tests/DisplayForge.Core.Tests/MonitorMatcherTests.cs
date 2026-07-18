using DisplayForge.Core.Models;
using DisplayForge.Core.Services;

namespace DisplayForge.Core.Tests;

public class MonitorMatcherTests
{
    [Fact]
    public void FindMatch_PrefersStableId()
    {
        var live = new List<MonitorInfo>
        {
            new() { StableId = "DEL1234#0", GdiDeviceName = @"\\.\DISPLAY1", FriendlyName = "Dell" },
            new() { StableId = "LGD5678#0", GdiDeviceName = @"\\.\DISPLAY2", FriendlyName = "LG" }
        };

        var entry = new ProfileMonitorEntry
        {
            StableId = "LGD5678#0",
            GdiDeviceName = @"\\.\DISPLAY1",
            FriendlyName = "Dell"
        };

        var match = MonitorMatcher.FindMatch(entry, live);
        Assert.NotNull(match);
        Assert.Equal("LGD5678#0", match!.StableId);
    }

    [Fact]
    public void FindMatch_FallsBackToDevicePath()
    {
        var live = new List<MonitorInfo>
        {
            new() { StableId = "A", DevicePath = @"\\?\DISPLAY#ABC", GdiDeviceName = @"\\.\DISPLAY1" }
        };

        var entry = new ProfileMonitorEntry
        {
            StableId = "different",
            DevicePath = @"\\?\DISPLAY#ABC"
        };

        var match = MonitorMatcher.FindMatch(entry, live);
        Assert.NotNull(match);
        Assert.Equal("A", match!.StableId);
    }

    [Fact]
    public void FindMatch_SkipsAlreadyMatched()
    {
        var live = new List<MonitorInfo>
        {
            new() { StableId = "A", FriendlyName = "Monitor" },
            new() { StableId = "B", FriendlyName = "Monitor" }
        };

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
        var entry = new ProfileMonitorEntry { FriendlyName = "Monitor" };

        var match = MonitorMatcher.FindMatch(entry, live, used);
        Assert.NotNull(match);
        Assert.Equal("B", match!.StableId);
    }

    [Fact]
    public void NormalizePrimaryOrigin_ShiftsCoordinates()
    {
        var monitors = new List<ProfileMonitorEntry>
        {
            new() { IsPrimary = true, Enabled = true, PositionX = 1920, PositionY = 0, Width = 1920, Height = 1080 },
            new() { IsPrimary = false, Enabled = true, PositionX = 0, PositionY = 0, Width = 1920, Height = 1080 }
        };

        MonitorMatcher.NormalizePrimaryOrigin(monitors);

        Assert.Equal(0, monitors[0].PositionX);
        Assert.Equal(0, monitors[0].PositionY);
        Assert.Equal(-1920, monitors[1].PositionX);
        Assert.Equal(0, monitors[1].PositionY);
        Assert.True(monitors[0].IsPrimary);
        Assert.False(monitors[1].IsPrimary);
    }

    [Fact]
    public void EnforceEnabledAndPrimary_PrimaryOnDisabled_BecomesExclusivePrimary()
    {
        // Bug case: mark primary on a currently disabled monitor while another is primary.
        // Intent SetPrimary must win even when Enabled is still false on the entry.
        var a = new ProfileMonitorEntry
        {
            FriendlyName = "A",
            IsPrimary = true,
            Enabled = true,
            PositionX = 0,
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };
        var b = new ProfileMonitorEntry
        {
            FriendlyName = "B",
            IsPrimary = false,
            Enabled = false,
            PositionX = 1920,
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };

        var monitors = new List<ProfileMonitorEntry> { a, b };
        MonitorMatcher.EnforceEnabledAndPrimary(
            monitors,
            edited: b,
            intent: MonitorMatcher.StructuralFlagIntent.SetPrimary);

        Assert.False(a.IsPrimary);
        Assert.True(a.Enabled);
        Assert.True(b.IsPrimary);
        Assert.True(b.Enabled);
        Assert.Equal(0, b.PositionX);
        Assert.Equal(0, b.PositionY);
        Assert.Equal(-1920, a.PositionX);
    }

    [Fact]
    public void EnforceEnabledAndPrimary_EnableThenSetPrimary_IntentWins()
    {
        // User enables B, then checks Primary — two-step simultaneous intent.
        var a = new ProfileMonitorEntry
        {
            FriendlyName = "A",
            IsPrimary = true,
            Enabled = true,
            PositionX = 0,
            PositionY = 0,
            Width = 2560,
            Height = 1440
        };
        var b = new ProfileMonitorEntry
        {
            FriendlyName = "B",
            IsPrimary = false,
            Enabled = false,
            PositionX = 0, // inactive capture often sits on origin
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };
        var monitors = new List<ProfileMonitorEntry> { a, b };

        b.Enabled = true;
        MonitorMatcher.EnforceEnabledAndPrimary(
            monitors, edited: b, intent: MonitorMatcher.StructuralFlagIntent.None);
        Assert.True(a.IsPrimary);
        Assert.False(b.IsPrimary);

        MonitorMatcher.EnforceEnabledAndPrimary(
            monitors, edited: b, intent: MonitorMatcher.StructuralFlagIntent.SetPrimary);
        Assert.False(a.IsPrimary);
        Assert.True(b.IsPrimary);
        Assert.True(b.Enabled);
        Assert.Equal(0, b.PositionX);
        // Former primary must not stay stacked on origin.
        Assert.True(a.PositionX != 0 || a.PositionY != 0);
    }

    [Fact]
    public void EnforceEnabledAndPrimary_EnableAlone_DoesNotStealPrimary()
    {
        var a = new ProfileMonitorEntry
        {
            FriendlyName = "A",
            IsPrimary = true,
            Enabled = true,
            PositionX = 0,
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };
        var b = new ProfileMonitorEntry
        {
            FriendlyName = "B",
            IsPrimary = false,
            Enabled = false,
            PositionX = 1920,
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };
        var monitors = new List<ProfileMonitorEntry> { a, b };

        b.Enabled = true;
        MonitorMatcher.EnforceEnabledAndPrimary(
            monitors, edited: b, intent: MonitorMatcher.StructuralFlagIntent.None);

        Assert.True(a.IsPrimary);
        Assert.False(b.IsPrimary);
        Assert.True(b.Enabled);
    }

    [Fact]
    public void EnforceEnabledAndPrimary_DisablingPrimary_AssignsAnother()
    {
        var a = new ProfileMonitorEntry
        {
            FriendlyName = "A",
            IsPrimary = false, // cleared when disabled
            Enabled = false,
            PositionX = 0,
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };
        var b = new ProfileMonitorEntry
        {
            FriendlyName = "B",
            IsPrimary = false,
            Enabled = true,
            PositionX = 1920,
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };

        var monitors = new List<ProfileMonitorEntry> { a, b };
        MonitorMatcher.EnforceEnabledAndPrimary(monitors, edited: a);

        Assert.False(a.Enabled);
        Assert.False(a.IsPrimary);
        Assert.True(b.Enabled);
        Assert.True(b.IsPrimary);
        Assert.Equal(0, b.PositionX);
    }

    [Fact]
    public void EnforceEnabledAndPrimary_CannotDisableLastMonitor()
    {
        var a = new ProfileMonitorEntry
        {
            FriendlyName = "A",
            IsPrimary = false,
            Enabled = false,
            PositionX = 0,
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };

        var monitors = new List<ProfileMonitorEntry> { a };
        MonitorMatcher.EnforceEnabledAndPrimary(monitors, edited: a);

        Assert.True(a.Enabled);
        Assert.True(a.IsPrimary);
    }

    [Fact]
    public void EnforceEnabledAndPrimary_PrimaryOnEnabled_ClearsOthersEvenAtOrigin()
    {
        var a = new ProfileMonitorEntry
        {
            FriendlyName = "A",
            IsPrimary = true,
            Enabled = true,
            PositionX = 0,
            PositionY = 0,
            Width = 2560,
            Height = 1440
        };
        var b = new ProfileMonitorEntry
        {
            FriendlyName = "B",
            IsPrimary = true,
            Enabled = true,
            PositionX = -1920,
            PositionY = 0,
            Width = 1920,
            Height = 1080
        };

        var monitors = new List<ProfileMonitorEntry> { a, b };
        MonitorMatcher.EnforceEnabledAndPrimary(
            monitors,
            edited: b,
            intent: MonitorMatcher.StructuralFlagIntent.SetPrimary);

        Assert.False(a.IsPrimary);
        Assert.True(b.IsPrimary);
        Assert.Equal(0, b.PositionX);
        Assert.Equal(1920, a.PositionX);
    }
}
