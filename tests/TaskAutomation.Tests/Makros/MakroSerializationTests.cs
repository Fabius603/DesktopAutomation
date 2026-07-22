using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskAutomation.Makros;
using TaskAutomation.Hotkeys;

namespace TaskAutomation.Tests.Makros;

public sealed class MakroSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void VersionTwo_RoundTripPreservesModeTimingAndEnvironment()
    {
        var macro = new Makro
        {
            Name = "precise",
            RecordingSettings = new() { Mode = MakroRecordingMode.MotionFaithfulRelative, MinimumIntervalMicroseconds = 500 },
            RecordedEnvironment = new() { VirtualDesktopX = -1920, VirtualDesktopWidth = 3840, VirtualDesktopHeight = 1080,
                RecordedAtUtc = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc) },
            Gruppen = new ObservableCollection<MakroGruppe>
            {
                new() { Id = "movement-1", Title = "Navigation", IsAutomatic = true }
            },
            Befehle = new ObservableCollection<MakroBefehl>
            {
                new MouseMoveRelativeBefehl { DeltaX = 3, DeltaY = -2, DelayBeforeMicroseconds = 875, GroupId = "movement-1" }
            }
        };
        var json = JsonSerializer.Serialize(macro, Options);
        var restored = JsonSerializer.Deserialize<Makro>(json, Options)!;
        Assert.Equal(Makro.CurrentFormatVersion, restored.FormatVersion);
        Assert.Equal(MakroRecordingMode.MotionFaithfulRelative, restored.RecordingSettings.Mode);
        Assert.Equal(0x78u, restored.RecordingSettings.RecordingHotkeyVirtualKey);
        Assert.Equal(-1920, restored.RecordedEnvironment!.VirtualDesktopX);
        var group = Assert.Single(restored.Gruppen);
        Assert.Equal(("movement-1", "Navigation", true), (group.Id, group.Title, group.IsAutomatic));
        var move = Assert.IsType<MouseMoveRelativeBefehl>(Assert.Single(restored.Befehle));
        Assert.Equal(875, move.DelayBeforeMicroseconds);
        Assert.Equal(group.Id, move.GroupId);
    }

    [Fact]
    public void LegacyMacroWithoutNewPropertiesLoadsWithSafeDefaults()
    {
        var restored = JsonSerializer.Deserialize<Makro>("""{"id":"00000000-0000-0000-0000-000000000001","name":"legacy","commands":[{"type":"mouse_move_absolute","x":10,"y":20}]}""", Options)!;
        Assert.Equal(Makro.CurrentFormatVersion, restored.FormatVersion);
        Assert.Equal(MakroRecordingMode.ScreenAccurateAbsolute, restored.RecordingSettings.Mode);
        Assert.Null(Assert.Single(restored.Befehle).DelayBeforeMicroseconds);
    }

    [Fact]
    public void ExplicitNullCollectionsAndSettingsAreNormalized()
    {
        var restored = JsonSerializer.Deserialize<Makro>("""{"name":"legacy","commands":null,"groups":null,"recordingSettings":null}""", Options)!;
        Assert.NotNull(restored.Befehle);
        Assert.NotNull(restored.Gruppen);
        Assert.NotNull(restored.RecordingSettings);
        Assert.Empty(restored.Befehle);
    }

    [Fact]
    public void ValidationRejectsNegativePreciseTimingAndInvalidRecordingLimits()
    {
        var macro = new Makro
        {
            Name = "invalid",
            Befehle = new ObservableCollection<MakroBefehl>
            {
                new MouseMoveRelativeBefehl { DelayBeforeMicroseconds = -1 }
            }
        };
        Assert.Equal(MakroValidationError.CommandTimingInvalid, MakroValidation.Validate(macro).Error);
        macro.Befehle[0].DelayBeforeMicroseconds = 0;
        macro.RecordingSettings.MinimumIntervalMicroseconds = -1;
        Assert.Equal(MakroValidationError.RecordingSettingsInvalid, MakroValidation.Validate(macro).Error);
        macro.RecordingSettings.MinimumIntervalMicroseconds = 1_000;
        macro.RecordingSettings.RecordingHotkeyVirtualKey = 0x79;
        Assert.Equal(MakroValidationError.RecordingSettingsInvalid, MakroValidation.Validate(macro).Error);
    }

    [Theory]
    [InlineData(null, false, "")]
    [InlineData(0L, true, "+0 \u00B5s")]
    [InlineData(875L, true, "+875 \u00B5s")]
    public void PreciseTiming_HasConsistentStepDisplay(long? microseconds, bool visible, string expected)
    {
        var command = new MouseMoveRelativeBefehl { DelayBeforeMicroseconds = microseconds };
        Assert.Equal(visible, command.HasPreciseDelay);
        Assert.Equal(expected, command.DelayBeforeDisplay);
    }

    [Theory]
    [InlineData(999L, "+999 \u00B5s")]
    [InlineData(1_000L, "+1 ms")]
    [InlineData(1_500L, "+1.5 ms")]
    [InlineData(1_000_000L, "+1 s")]
    [InlineData(90_000_000L, "+1.5 min")]
    [InlineData(3_600_000_000L, "+1 h")]
    public void PreciseTiming_UsesLargestSensibleUnit(long microseconds, string expected)
    {
        var command = new MouseMoveRelativeBefehl { DelayBeforeMicroseconds = microseconds };

        Assert.Equal(UseCurrentDecimalSeparator(expected), command.DelayBeforeDisplay);
    }

    [Theory]
    [InlineData(999L, "999 ms")]
    [InlineData(1_000L, "1 s")]
    [InlineData(90_000L, "1.5 min")]
    [InlineData(3_600_000L, "1 h")]
    public void TimeoutDuration_UsesLargestSensibleUnit(int milliseconds, string expected)
    {
        var command = new TimeoutBefehl { Duration = milliseconds };

        Assert.Equal(UseCurrentDecimalSeparator(expected), command.DurationDisplay);
    }

    private static string UseCurrentDecimalSeparator(string value)
        => value.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
}
