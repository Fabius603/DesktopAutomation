using System.Text.Json;
using TaskAutomation.Logging;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Logging;

public sealed class StepLogResultCoverageTests
{
    [Fact]
    public void CaptureAndDetectionSummaries_RetainFrameOrderingEvidence()
    {
        var capture = StepLogResults.Summarize(new DesktopDuplicationResult { FrameVersion = 7, FrameTimestamp = 1234 });
        var detection = StepLogResults.Summarize(new ColorDetectionResult { SourceFrameVersion = 7, SourceFrameTimestamp = 1234 });
        Assert.Equal("7", capture["FrameVersion"]);
        Assert.Equal("1234", capture["FrameTimestamp"]);
        Assert.Equal("7", detection["SourceFrameVersion"]);
        Assert.Equal("1234", detection["SourceFrameTimestamp"]);
    }

    [Fact]
    public void Suppressed3DMovement_ReportsBlockWithoutFabricatingAppliedMovement()
    {
        var summary = StepLogResults.Summarize(new KlickOnPoint3DResult
        {
            WasExecuted = true,
            Success = true,
            MovementBlocked = true,
            DeltaX = 120,
            DeltaY = -40
        });
        Assert.Equal("True", summary["MovementBlocked"]);
        Assert.True(string.IsNullOrEmpty(summary["AppliedDeltaX"]));
        Assert.True(string.IsNullOrEmpty(summary["AppliedDeltaY"]));
    }

    [Fact]
    public void AllBuiltInResultContracts_HaveAnApprovedLogSummary()
    {
        var resultTypes = typeof(StepResultBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(StepResultBase).IsAssignableFrom(type)).ToArray();
        Assert.NotEmpty(resultTypes);
        foreach (var type in resultTypes)
        {
            Assert.True(StepLogResults.Supports(type), $"Missing privacy-preserving logging contract for {type.Name}");
            var result = Activator.CreateInstance(type);
            var summary = StepLogResults.Summarize(result);
            Assert.Equal("True", summary["SummarySupported"]);
            Assert.Equal(type.Name, summary["ResultKind"]);
            Assert.Contains("WasExecuted", summary.Keys);
        }
    }

    [Fact]
    public void BranchDecision_RoundTripContainsOnlyDecisionFacts()
    {
        var entry = new LogEvent
        {
            BranchDecision = new(true, false, "ConditionNotMet", "All",
            [new(1, "NotMet", "Contains"), new(2, "NotEvaluated", "Equals")])
        };
        var restored = JsonSerializer.Deserialize<LogEvent>(JsonSerializer.Serialize(entry, LogRepository.JsonOptions), LogRepository.JsonOptions)!;
        Assert.Equal(entry.BranchDecision!.Conditions, restored.BranchDecision!.Conditions);
        Assert.False(restored.BranchDecision.BranchActive);
        Assert.Equal("ConditionNotMet", restored.BranchDecision.Reason);
        Assert.Equal("All", restored.BranchDecision.MatchMode);
    }
}
