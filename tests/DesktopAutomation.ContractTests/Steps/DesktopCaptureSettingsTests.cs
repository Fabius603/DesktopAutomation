using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class DesktopCaptureSettingsTests
{
    [Fact]
    public void LegacySettings_KeepMonitorCursorAndNewSafeDefaults()
    {
        var settings = JsonSerializer.Deserialize<DesktopDuplicationSettings>("""{"desktop_idx":2,"capture_cursor":true}""")!;
        Assert.Equal(2, settings.DesktopIdx);
        Assert.True(settings.CaptureCursor);
        Assert.True(settings.WaitForNewFrame);
        Assert.True(settings.AllowCachedFallback);
        Assert.Equal(250, settings.TimeoutMilliseconds);
        Assert.Equal(string.Empty, settings.MonitorDeviceName);
    }

    [Fact]
    public void CaptureOptions_SurviveUnifiedJobSerializationAndMaterialization()
    {
        var step = new DesktopDuplicationStep
        {
            Settings = new()
            {
                DesktopIdx = 2,
                CaptureCursor = true,
                MonitorDeviceName = "DISPLAY-A",
                WaitForNewFrame = false,
                AllowCachedFallback = false,
                TimeoutMilliseconds = 1234
            }
        };
        var job = new Job { Steps = [step] };
        JobVariableInputMigration.Migrate(job);
        var options = new JsonSerializerOptions();
        JobJsonSerialization.Configure(options);
        var json = JsonSerializer.Serialize(job, options);
        var restored = JsonSerializer.Deserialize<Job>(json, options)!;
        JobVariableInputMigration.Migrate(restored);
        var capture = Assert.IsType<DesktopDuplicationStep>(StepInputMaterializer.Materialize(restored.Steps[0],
            new JobResultStore(restored.Variables, localValues: restored.LocalValues)));
        Assert.Equal(2, capture.Settings.DesktopIdx);
        Assert.True(capture.Settings.CaptureCursor);
        Assert.Equal("DISPLAY-A", capture.Settings.MonitorDeviceName);
        Assert.False(capture.Settings.WaitForNewFrame);
        Assert.False(capture.Settings.AllowCachedFallback);
        Assert.Equal(1234, capture.Settings.TimeoutMilliseconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60001)]
    public void CaptureDefinition_RejectsUnboundedOrInvalidTimeout(int timeout)
    {
        var definition = new DesktopDuplicationStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[DesktopDuplicationStepDefinition.TimeoutFieldId] = JsonValue.Create(timeout);
        Assert.Contains(definition.ValidateDraft(draft), issue => issue.FieldId == DesktopDuplicationStepDefinition.TimeoutFieldId);
    }

    [Fact]
    public void MonitorPicker_DeclaresItsIdentityCompanionAndAllOptionsRoundTrip()
    {
        var definition = new DesktopDuplicationStepDefinition();
        var step = new DesktopDuplicationStep
        {
            Settings = new()
            {
                MonitorDeviceName = "DISPLAY-A",
                WaitForNewFrame = false,
                TimeoutMilliseconds = 1234,
                AllowCachedFallback = false
            }
        };
        var result = Assert.IsType<DesktopDuplicationStep>(definition.ApplyDraft(definition.CreateDraft(step)));
        Assert.Equal(step.Settings.MonitorDeviceName, result.Settings.MonitorDeviceName);
        Assert.False(result.Settings.WaitForNewFrame);
        Assert.False(result.Settings.AllowCachedFallback);
        Assert.Equal(1234, result.Settings.TimeoutMilliseconds);
        Assert.Equal(DesktopDuplicationStepDefinition.MonitorDeviceNameFieldId,
            definition.Descriptor.Fields.Single(field => field.Id == DesktopDuplicationStepDefinition.DesktopIndexFieldId).MonitorDeviceNameFieldId);
    }
}
