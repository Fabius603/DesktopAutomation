using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Logging;

public sealed class LogUiMeaningTests
{
    [Theory]
    [InlineData(4, 10, 1L, 2.5)]
    [InlineData(200, 400, 2L, 2d)]
    [InlineData(5, 0, 0L, 0d)]
    [InlineData(0, 0, 0L, null)]
    [InlineData(5, 0, null, null)]
    public void RepeatedStepDuration_AveragesAllMeasurementsAndKeepsUnknownDistinctFromZero(
        long count, long total, long? lastDuration, double? expected)
    {
        var summary = new LogStepSummary("step", "Main", LogCodes.StepCompleted, null, count, total, [],
            new LogEvent { DurationMs = lastDuration });
        Assert.Equal(expected, summary.AverageDurationMs);
    }

    [Theory]
    [InlineData("StopPhase")]
    [InlineData("EndJob")]
    [InlineData("NextIteration")]
    public void Timeline_UsesExplicitFlowEvidenceAndDoesNotBlameCleanup(string effect)
    {
        var execution = Guid.NewGuid();
        var run = new LogRun
        {
            EndedAt = DateTimeOffset.UtcNow,
            CompletionReason = "StepFailed",
            Steps = [new("cause", "script_execution", 1, "Main", true), new("after", "show_text", 2, "Main", true), new("cleanup", "show_text", 3, "End", true)]
        };
        var cause = new LogEvent
        {
            Source = LogSource.Job,
            Code = LogCodes.StepFailed,
            Context = new(StepId: "cause", StepExecutionId: execution),
            Phase = "Main",
            Iteration = 1,
            FlowEffect = new(effect),
            Parameters = new() { ["Reason"] = "Exception" }
        };
        var timeline = LogTimeline.Build(run, [cause]);
        var after = Assert.Single(timeline, step => step.Step.Id == "after");
        Assert.Equal("cause", after.Cause!.StepId);
        Assert.Equal(execution, after.Cause.ExecutionId);
        Assert.Null(Assert.Single(timeline, step => step.Step.Id == "cleanup").Cause);
        Assert.Equal("Log.Impact.Prevented", LogPresentation.Step(after, run.Steps).Impact!.Key);
        Assert.All(LogTimeline.Build(run with { IsComplete = false }, [cause]), step => Assert.Null(step.Cause));
        Assert.All(LogTimeline.Build(run, [cause with { FlowEffect = null }]), step => Assert.Null(step.Cause));
    }
    [Fact]
    public void Paths_SeparateNormalizedDirectoryAndActualUniqueOutputFile()
    {
        var step = new SaveImageStep { Settings = new() { SavePath = "relative-folder", FileName = "image.png" } };
        var start = StepLogPaths.Capture(step);
        Assert.Equal(Path.GetFullPath("relative-folder"), Assert.Single(start, path => path.Kind == "TargetDirectory").Value);
        var result = new SaveImageResult { FilePath = Path.GetFullPath("relative-folder/image-2.png") };
        Assert.Equal(result.FilePath, Assert.Single(StepLogPaths.Capture(step, result), path => path.Kind == "TargetFile").Value);
        var literalName = new SaveImageResult { FilePath = Path.GetFullPath("relative-folder/%TEMP%.png") };
        Assert.Equal(literalName.FilePath, Assert.Single(StepLogPaths.Capture(step, literalName), path => path.Kind == "TargetFile").Value);
        Assert.Null(StepLogPaths.Normalize("invalid\0path"));
        step.Settings.FileName = "invalid\0file";
        Assert.DoesNotContain(StepLogPaths.Capture(step), path => path.Kind == "TargetFile");
    }
    [Fact]
    public void Summary_UsesSafeFactsAndDoesNotTreatFailedQueriesAsAbsence()
    {
        var entry = new LogEvent { Code = LogCodes.StepCompleted, Parameters = new() { ["ResultKind"] = nameof(OcrResult), ["WordCount"] = "7", ["Text"] = "must not expose" } };
        var display = LogPresentation.Event(entry);
        Assert.Equal("Log.Summary.Ocr", display.Summary.Key);
        Assert.DoesNotContain("must not expose", display.Summary.Arguments.Values);
        var failedQuery = entry with { Parameters = new() { ["Status"] = "Unavailable", ["Exists"] = "False" } };
        Assert.Equal("Log.Summary.Unsuccessful", LogPresentation.Summary(failedQuery).Key);
        Assert.Equal("Log.Summary.Dispatched", LogPresentation.Summary(entry with { Parameters = new() { ["CompletionScope"] = "Dispatch" } }).Key);
    }
}
