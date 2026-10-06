using System.Globalization;
using System.Resources;
using System.Text.Json;
using TaskAutomation.Logging;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Logging;

public sealed class LogUiContractTests
{
    [Fact]
    public void RepeatedStepAverage_IsDerivedFromExistingStoredFactsWithoutChangingTheJsonShape()
    {
        var summary = new LogStepSummary("step", "Main", LogCodes.StepCompleted, null, 4, 10, [],
            new LogEvent { DurationMs = 1 });
        var json = JsonSerializer.Serialize(summary, LogRepository.JsonOptions);
        using var document = JsonDocument.Parse(json);
        Assert.DoesNotContain(document.RootElement.EnumerateObject(), property =>
            string.Equals(property.Name, nameof(LogStepSummary.AverageDurationMs), StringComparison.OrdinalIgnoreCase));
        var restored = JsonSerializer.Deserialize<LogStepSummary>(json, LogRepository.JsonOptions)!;
        Assert.Equal(2.5, restored.AverageDurationMs);
    }

    [Fact]
    public void EarlierV2Records_KeepSafeUnknownDefaultsWithoutInventedHistory()
    {
        var entry = JsonSerializer.Deserialize<LogEvent>("{\"SchemaVersion\":2,\"Code\":\"step.failed\"}", LogRepository.JsonOptions)!;
        Assert.Null(entry.Trigger);
        Assert.Null(entry.FlowEffect);
        Assert.Empty(entry.Paths);
        Assert.Empty(entry.RelatedInstanceIds);
        var run = JsonSerializer.Deserialize<LogRun>("{\"SchemaVersion\":2}", LogRepository.JsonOptions)!;
        Assert.Null(run.Trigger);
    }
    [Fact]
    public void StructuredUiFacts_RoundTripAndAllDisplayKeysHaveBothTranslations()
    {
        var entry = new LogEvent
        {
            Code = LogCodes.StepFailed,
            DiagnosticCode = LogCodes.FileUnavailable,
            Trigger = new("FileSystemEvent", "Created", "C:/incoming", Guid.NewGuid(), "Job", "target"),
            Paths = [new("TargetDirectory", "C:/out")],
            FlowEffect = new("StopPhase"),
            RelatedInstanceIds = [Guid.NewGuid()]
        };
        var restored = JsonSerializer.Deserialize<LogEvent>(JsonSerializer.Serialize(entry, LogRepository.JsonOptions), LogRepository.JsonOptions)!;
        Assert.Equal(entry.Trigger, restored.Trigger);
        Assert.Equal(entry.Paths, restored.Paths);
        Assert.Equal(entry.FlowEffect, restored.FlowEffect);
        Assert.Equal(entry.RelatedInstanceIds, restored.RelatedInstanceIds);
        var resources = new ResourceManager("DesktopAutomationApp.Resources.Strings", typeof(DesktopAutomationApp.App).Assembly);
        var keys = new HashSet<string> { LogPresentation.Event(entry).Title.Key, LogPresentation.Event(entry).Summary.Key,
            LogPresentation.Event(entry).AreaKey, "Log.Impact.Prevented", "Log.Impact.Stopped" };
        foreach (var area in Enum.GetValues<LogArea>()) keys.Add("Log.Area." + area);
        foreach (var definition in BuiltInStepDefinitions.Instance.Definitions)
        {
            var step = new LogStepExecution(new("id", definition.Descriptor.TypeId, 1, "Main", true), null, "Main", 1,
                StepLogOutcome.Successful, null, null, []);
            var display = LogPresentation.Step(step, []);
            keys.Add(display.TitleKey); keys.Add(display.Summary.Key);
        }
        foreach (var key in keys)
            foreach (var culture in new[] { CultureInfo.GetCultureInfo("de"), CultureInfo.GetCultureInfo("en") })
                Assert.False(string.IsNullOrWhiteSpace(resources.GetString(key, culture)), key + " / " + culture.Name);
    }
}
