using System.Text.Json;
using DesktopAutomationApp.Settings;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class UserPreferencesTests
{
    [Fact]
    public void LegacySettingsWithoutForceStopKey_DefaultToF10()
    {
        var preferences = JsonSerializer.Deserialize<UserPreferences>("{}");

        Assert.NotNull(preferences);
        Assert.Equal(0x79u, preferences.ForceStopVirtualKey);
    }

    [Fact]
    public void ConfiguredForceStopKey_RoundTrips()
    {
        var json = JsonSerializer.Serialize(new UserPreferences
        {
            ForceStopVirtualKey = 0x7A
        });

        var preferences = JsonSerializer.Deserialize<UserPreferences>(json);

        Assert.NotNull(preferences);
        Assert.Equal(0x7Au, preferences.ForceStopVirtualKey);
    }
}
