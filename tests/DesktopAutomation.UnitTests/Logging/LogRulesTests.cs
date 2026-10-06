using TaskAutomation.Logging;

namespace TaskAutomation.Tests.Logging;

public sealed class LogRulesTests
{
    [Fact]
    public void FileDiagnostic_UsesObservedOutputPathInsteadOfConfiguredTemplate()
    {
        var step = new TaskAutomation.Jobs.SaveImageStep { Settings = new() { SavePath = "template", FileName = "planned.png" } };
        var result = new TaskAutomation.Steps.SaveImageResult { FilePath = "actual/output.png" };
        Assert.Equal("actual/output.png", StepLogEvents.Create(step, LogCodes.StepCompleted, "End", result: result).Parameters["Path"]);
        var error = new FileNotFoundException("missing", "actual/missing.png");
        Assert.Equal("actual/missing.png", StepLogEvents.Create(step, LogCodes.StepFailed, "End", error: error).Parameters["Path"]);
    }
    [Fact]
    public void SameException_KeptAcrossScopeExitHasOneProblemIdentity()
    {
        var error = new IOException("failure");
        var scope = Guid.NewGuid();
        Assert.Equal(scope, LogDiagnostics.ProblemId(error, scope));
        Assert.Equal(scope, LogDiagnostics.ProblemId(error));
        Assert.NotEqual(scope, LogDiagnostics.ProblemId(new IOException("failure")));
    }

    [Fact]
    public void ResultSummary_ExcludesRecognizedTextAndKeepsUsefulOutcome()
    {
        var result = StepLogEvents.Result(new TaskAutomation.Steps.OcrResult { Text = "sensitive", Found = true, WordCount = 3 });
        Assert.Equal("True", result["Found"]);
        Assert.Equal("3", result["WordCount"]);
        Assert.DoesNotContain("sensitive", result.Values);
    }
    [Theory]
    [InlineData(LogOutcome.Successful, 0, 0, LogOutcome.Successful)]
    [InlineData(LogOutcome.Successful, 0, 1, LogOutcome.WithWarnings)]
    [InlineData(LogOutcome.Successful, 1, 0, LogOutcome.WithErrors)]
    [InlineData(LogOutcome.Failed, 1, 0, LogOutcome.Failed)]
    [InlineData(LogOutcome.Stopped, 1, 1, LogOutcome.Stopped)]
    public void FinalOutcome_SeparatesCompletionFromQuality(LogOutcome requested, int errors, int warnings, LogOutcome expected)
        => Assert.Equal(expected, LogOutcomeRules.Complete(requested, errors, warnings));

    [Fact]
    public void Timeline_DistinguishesInactiveBranchesUnreachedStepsAndExecutedCleanup()
    {
        var run = new LogRun
        {
            Id = Guid.NewGuid(),
            EndedAt = DateTimeOffset.UtcNow,
            Outcome = LogOutcome.Failed,
            CompletionReason = "StepFailed",
            Steps = [new("s1", "type", 1, "Main", true), new("s2", "type", 2, "Main", true),
                new("s3", "type", 3, "End", true)]
        };
        var events = new[] {
            new LogEvent { Sequence = 1, Code = LogCodes.StepSkipped, Context = new(run.Id, StepId: "s1"), Phase = "Main", Iteration = 1,
                Parameters = new() { ["Reason"] = "InactiveBranch" } },
            new LogEvent { Sequence = 2, Code = LogCodes.StepCompleted, Context = new(run.Id, StepId: "s3", StepExecutionId: Guid.NewGuid()), Phase = "End" }
        };
        var steps = LogTimeline.Build(run, events);
        Assert.Equal(new[] { StepLogOutcome.Skipped, StepLogOutcome.NotExecuted, StepLogOutcome.Successful }, steps.Select(step => step.Outcome));
        Assert.Equal("InactiveBranch", steps[0].Reason);
    }

    [Fact]
    public void Timeline_IncompleteRunNeverClaimsMissingStepWasNotExecuted()
    {
        var run = new LogRun
        {
            IsComplete = false,
            Outcome = LogOutcome.Interrupted,
            Steps = [new("s1", "type", 1, "Main", true)]
        };
        Assert.Equal(StepLogOutcome.Unknown, Assert.Single(LogTimeline.Build(run, [])).Outcome);
    }

    [Fact]
    public void Diagnostic_UnknownCauseDoesNotOfferUnprovenPathAction()
    {
        var result = LogDiagnostics.Describe(new LogEvent { Details = "directory-like text", Parameters = new() { ["Path"] = "Z:\\missing" } });
        Assert.Equal(LogCodes.Unexpected, result.Code);
        Assert.DoesNotContain(result.Actions, action => action.Kind == "CheckPath");
    }

    [Fact]
    public void PathActions_ExposeCapturedAbsolutePathsAndRejectInvalidOrMaskedValues()
    {
        var actions = LogDiagnostics.PathActions(new LogEvent
        {
            Paths = [new("TargetFile", "C:\\output\\image.png"),
            new("TargetDirectory", "C:\\output"), new("Source", "[redacted]"), new("Other", "relative\\file"), new("Other", "C:\\bad\0path")]
        });
        Assert.Equal(2, actions.Count);
        Assert.All(actions, action => Assert.Equal("CheckPath", action.Kind));
        Assert.Contains(actions, action => action.Path == "C:\\output\\image.png");
    }
}
