using System.Text.Json;
using DesktopAutomation.Application.Settings;

namespace DesktopAutomation.ContractTests.Settings;

public sealed class UserPreferencesTests
{
    [Fact]
    public void MissingProperties_UseBackwardCompatibleDefaults()
    {
        var preferences = JsonSerializer.Deserialize<UserPreferences>("{}");

        Assert.NotNull(preferences);
        Assert.Equal("de-DE", preferences.Culture);
        Assert.Equal(AppThemeMode.System, preferences.ThemeMode);
        Assert.Equal(UserPreferences.DefaultForceStopVirtualKey, preferences.ForceStopVirtualKey);
        Assert.NotNull(preferences.ExpandedLibraryFolders);
        Assert.False(preferences.NavigationCollapsed);
        Assert.False(preferences.StepInspectorCollapsed);
    }

    [Fact]
    public void ExistingProperties_RoundTripWithoutChangingTheirMeaning()
    {
        var json = JsonSerializer.Serialize(new UserPreferences
        {
            Culture = "en-US",
            ThemeMode = AppThemeMode.Black,
            ForceStopVirtualKey = 0x7A,
            NavigationCollapsed = true,
            StepInspectorCollapsed = true
        });

        var preferences = JsonSerializer.Deserialize<UserPreferences>(json);

        Assert.NotNull(preferences);
        Assert.Equal("en-US", preferences.Culture);
        Assert.Equal(AppThemeMode.Black, preferences.ThemeMode);
        Assert.Equal(0x7Au, preferences.ForceStopVirtualKey);
        Assert.True(preferences.NavigationCollapsed);
        Assert.True(preferences.StepInspectorCollapsed);
    }
}
