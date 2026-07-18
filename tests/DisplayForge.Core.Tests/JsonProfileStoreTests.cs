using DisplayForge.Core.Models;
using DisplayForge.Core.Services;

namespace DisplayForge.Core.Tests;

public class JsonProfileStoreTests
{
    [Fact]
    public void SaveAndLoad_ProfilesRoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "DisplayForgeTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonProfileStore(dir);
            var collection = new ProfileCollection
            {
                Profiles =
                [
                    new MonitorProfile
                    {
                        Name = "Work",
                        Hotkey = new HotkeyBinding
                        {
                            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
                            Key = "D1"
                        },
                        Monitors =
                        [
                            new ProfileMonitorEntry
                            {
                                StableId = "ABC",
                                Enabled = true,
                                IsPrimary = true,
                                Width = 2560,
                                Height = 1440,
                                RefreshRate = 144,
                                PositionX = 0,
                                PositionY = 0
                            }
                        ]
                    }
                ]
            };

            store.SaveProfiles(collection);
            var loaded = store.LoadProfiles();

            Assert.Single(loaded.Profiles);
            Assert.Equal("Work", loaded.Profiles[0].Name);
            Assert.Equal("D1", loaded.Profiles[0].Hotkey?.Key);
            Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, loaded.Profiles[0].Hotkey?.Modifiers);
            Assert.Equal(2560, loaded.Profiles[0].Monitors[0].Width);
            Assert.Equal("Ctrl+Alt+1", loaded.Profiles[0].Hotkey?.ToDisplayString());
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_SettingsRoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "DisplayForgeTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonProfileStore(dir);
            var settings = new AppSettings
            {
                Language = "ja",
                StartMinimizedToTray = false,
                ShowNotificationOnSwitch = true,
                HotkeysEnabled = true,
                ConfirmApplyFromUi = true,
                ConfirmApplyFromHotkey = false,
                ConfirmApplyTimeoutSeconds = 20,
                LastAppliedProfileId = "abc"
            };

            store.SaveSettings(settings);
            var loaded = store.LoadSettings();

            Assert.Equal("ja", loaded.Language);
            Assert.False(loaded.StartMinimizedToTray);
            Assert.Equal("abc", loaded.LastAppliedProfileId);
            Assert.True(loaded.ConfirmApplyFromUi);
            Assert.False(loaded.ConfirmApplyFromHotkey);
            Assert.Equal(20, loaded.ConfirmApplyTimeoutSeconds);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    [InlineData(0, 15)]
    [InlineData(-3, 15)]
    [InlineData(1, 1)]
    [InlineData(15, 15)]
    [InlineData(120, 120)]
    [InlineData(999, 120)]
    public void GetConfirmTimeoutSeconds_ClampsToRange(int input, int expected)
    {
        var settings = new AppSettings { ConfirmApplyTimeoutSeconds = input };
        Assert.Equal(expected, settings.GetConfirmTimeoutSeconds());
    }
}
