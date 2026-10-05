using System.Text.Json;
using TaskAutomation.Logging;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Logging;

public sealed class StepLogResultCoverageTests
{
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
