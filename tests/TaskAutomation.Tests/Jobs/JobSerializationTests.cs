using System.Text.Json;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobSerializationTests
{
    [Fact]
    public void RoundTrip_PreservesPolymorphicStepsBindingsAndWindowsParameters()
    {
        var job = new Job
        {
            Name = "Audio monitor", Repeating = true,
            StartSteps = [new ShowTextStep { Id = "start", Settings = new() { Text = "starting" } }],
            Steps =
            [
                new WindowsStateQueryStep { Id = "audio", Settings = new()
                    { QueryType = "audio.volume", Parameters = new(StringComparer.OrdinalIgnoreCase) { ["device"] = "default" } } },
                new ShowTextStep { Id = "display", Settings = new() { TextSource = ShowTextSource.TaskResult,
                    TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" } } }
            ],
            EndSteps = [new EndJobStep { Settings = new() { SkipEndSteps = true } }]
        };

        var json = JsonSerializer.Serialize(job);
        var restored = Assert.IsType<Job>(JsonSerializer.Deserialize<Job>(json));

        Assert.Equal(job.Id, restored.Id);
        Assert.True(restored.Repeating);
        Assert.IsType<ShowTextStep>(Assert.Single(restored.StartSteps));
        var audio = Assert.IsType<WindowsStateQueryStep>(restored.Steps[0]);
        Assert.Equal("audio.volume", audio.Settings.QueryType);
        Assert.Equal("default", audio.Settings.Parameters["DEVICE"]);
        var display = Assert.IsType<ShowTextStep>(restored.Steps[1]);
        Assert.Equal("audio", display.Settings.TextResult.SourceStepId);
        Assert.Equal("Percentage", display.Settings.TextResult.PropertyPath);
    }

    [Fact]
    public void Deserialize_LegacyConditionValue_RemainsEffectiveComparison()
    {
        const string json = """
            {"type":"if","id":"if","is_enabled":true,"settings":{"match_mode":"All","conditions":[{"source_step_id":"source","property_path":"Count","operator":"GreaterThan","comparison_value":"2"}]}}
            """;
        var step = Assert.IsType<IfStep>(JsonSerializer.Deserialize<JobStep>(json));
        var condition = Assert.Single(step.Settings.Conditions);
        Assert.Equal(ComparisonOperandKind.Literal, condition.EffectiveComparison.Kind);
        Assert.Equal("2", condition.EffectiveComparison.Value);
    }

    [Fact]
    public void NewJobStepIds_AreUniqueAndNonEmpty()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => new TimeoutStep().Id).ToArray();
        Assert.All(ids, id => Assert.True(Guid.TryParse(id, out _)));
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ActiveStepCount_ExcludesDisabledAndFlowControlStepsAcrossAllPhases()
    {
        var job = new Job
        {
            StartSteps = [new TimeoutStep()],
            Steps = [new IfStep(), new ShowTextStep { IsEnabled = false }, new ElseStep(), new EndIfStep(), new TimeoutStep()],
            EndSteps = [new TimeoutStep()]
        };
        Assert.Equal(3, job.ActiveStepCount);
    }
}
