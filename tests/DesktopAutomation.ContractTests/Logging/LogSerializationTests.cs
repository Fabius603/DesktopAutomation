using System.Text.Json;
using TaskAutomation.Logging;

namespace TaskAutomation.Tests.Logging;

public sealed class LogSerializationTests
{
    [Fact]
    public void StepSummaries_RoundTripAndOlderRunsDefaultToNoSummaries()
    {
        var entry = new LogEvent
        {
            Context = new(StepId: "step", StepExecutionId: Guid.NewGuid()),
            Iteration = 8,
            Code = LogCodes.StepCompleted,
            Parameters = new() { ["Found"] = "False" }
        };
        var run = new LogRun
        {
            StepSummaries = [new("step", "Main", LogCodes.StepCompleted, "Completed", 8, 120,
            [new(1, 8)], entry)]
        };
        var restored = JsonSerializer.Deserialize<LogRun>(JsonSerializer.Serialize(run, LogRepository.JsonOptions), LogRepository.JsonOptions)!;
        var summary = Assert.Single(restored.StepSummaries);
        Assert.Equal(8, summary.Count);
        Assert.Equal(120, summary.TotalDurationMs);
        Assert.Equal(entry.Context, summary.LastEvent.Context);
        Assert.Equal(run.StepSummaries[0].Iterations, summary.Iterations);
        Assert.True(summary.IterationCoverageComplete);
        Assert.Empty(JsonSerializer.Deserialize<LogRun>("{\"SchemaVersion\":2}", LogRepository.JsonOptions)!.StepSummaries);
    }

    [Fact]
    public void EventRoundTrip_PreservesIdentityCorrelationAndMultilineDetails()
    {
        var entry = new LogEvent
        {
            Sequence = 42,
            Timestamp = DateTimeOffset.UtcNow,
            Code = LogCodes.StepFailed,
            Context = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "step-id", Guid.NewGuid(), Guid.NewGuid()),
            Source = LogSource.Job,
            Level = ExecutionLogLevel.Error,
            ProblemId = Guid.NewGuid(),
            Details = "first\nsecond",
            Phase = "End",
            Iteration = 3,
            DurationMs = 52,
            DiagnosticCode = LogCodes.AccessDenied,
            Parameters = new() { ["Reason"] = "Failed", ["Path"] = null }
        };
        var json = JsonSerializer.Serialize(entry, LogRepository.JsonOptions);
        var restored = JsonSerializer.Deserialize<LogEvent>(json, LogRepository.JsonOptions)!;
        Assert.Equal(LogEvent.CurrentSchema, restored.SchemaVersion);
        Assert.Equal(entry.Id, restored.Id);
        Assert.Equal(entry.Context, restored.Context);
        Assert.Equal(entry.Sequence, restored.Sequence);
        Assert.Equal(entry.Timestamp, restored.Timestamp);
        Assert.Equal(entry.Level, restored.Level);
        Assert.Equal(entry.Details, restored.Details);
        Assert.Equal(entry.Parameters, restored.Parameters);
        Assert.Equal(entry.DurationMs, restored.DurationMs);
        Assert.Equal(entry.DiagnosticCode, restored.DiagnosticCode);
    }

    [Fact]
    public void RunRoundTrip_PreservesSnapshotAndQualityWithoutJobDefinition()
    {
        var run = new LogRun
        {
            Id = Guid.NewGuid(),
            Name = "deleted job",
            SourceId = Guid.NewGuid(),
            InstanceId = Guid.NewGuid(),
            CatalogSequence = 31,
            Outcome = LogOutcome.WithErrors,
            ErrorCount = 2,
            StartedAt = DateTimeOffset.UtcNow.AddSeconds(-3),
            EndedAt = DateTimeOffset.UtcNow,
            Steps = [new("step-id", "save-image", 1, "End", false)],
            IsComplete = false,
            LostEntries = 1
        };
        var restored = JsonSerializer.Deserialize<LogRun>(JsonSerializer.Serialize(run, LogRepository.JsonOptions), LogRepository.JsonOptions)!;
        Assert.Equal(run.Steps, restored.Steps);
        Assert.Equal(run.Outcome, restored.Outcome);
        Assert.Equal(run.ErrorCount, restored.ErrorCount);
        Assert.Equal(run.Id, restored.Id);
        Assert.Equal(run.Name, restored.Name);
        Assert.False(restored.IsComplete);
        Assert.Equal(run.LostEntries, restored.LostEntries);
        Assert.Equal(run.CatalogSequence, restored.CatalogSequence);
    }
}
