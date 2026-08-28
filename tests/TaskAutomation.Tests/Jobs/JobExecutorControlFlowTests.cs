using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.WindowsIntegration;
using TaskAutomation.Orchestration;
using TaskAutomation.Logging;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobExecutorControlFlowTests
{
    [Fact]
    public async Task ExecuteJob_LoadsReferencedSecretOnlyForRuntimeResolution()
    {
        var builder = new JobExecutorTestBuilder();
        var secret = builder.Secrets.Add("API token", "top-secret");
        var job = new Job
        {
            Name = "secret input",
            Steps =
            [
                new ShowTextStep
                {
                    Settings = new ShowTextSettings
                    {
                        TextSource = ShowTextSource.TaskResult,
                        TextResult = new ResultBinding
                        {
                            ProviderId = ValueProviderIds.Secret,
                            SourceId = secret.Id.ToString("D")
                        }
                    }
                }
            ]
        };
        builder.WithJobs(job);

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        Assert.Equal("top-secret", Assert.Single(builder.Overlay.TextCalls).Text);
    }

    [Fact]
    public async Task ExecuteJob_ConditionReadsJobVariableReference()
    {
        var variable = new JobVariable
        {
            Name = "Feature enabled",
            ValueKind = ResultValueKind.Boolean,
            Value = System.Text.Json.Nodes.JsonValue.Create(true)
        };
        var condition = new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D"),
            Operator = ConditionOperator.IsTrue
        };
        var job = new Job
        {
            Name = "variable condition",
            Variables = [variable],
            Steps =
            [
                new IfStep { Settings = new() { Conditions = [condition] } },
                Text("enabled"),
                new ElseStep(),
                Text("disabled"),
                new EndIfStep()
            ]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job);

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        Assert.Equal(["enabled"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_ConditionReadsSelectedVariableSubproperty()
    {
        var variable = new JobVariable
        {
            Name = "Area",
            ValueKind = ResultValueKind.Rectangle,
            Value = System.Text.Json.JsonSerializer.SerializeToNode(
                new TaskAutomation.Contracts.Geometry.PixelRegion(7, 8, 9, 10))
        };
        var condition = new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D"),
            ValuePath = "X",
            Operator = ConditionOperator.Equals,
            ComparisonValue = "7"
        };
        var job = new Job
        {
            Name = "compound variable condition",
            Variables = [variable],
            Steps =
            [
                new IfStep { Settings = new() { Conditions = [condition] } },
                Text("matched"),
                new EndIfStep()
            ]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job);

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        Assert.Equal(["matched"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_ConditionComparesAgainstPrivateLocalValue()
    {
        var actual = new JobVariable
        {
            Name = "Current count",
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create(7)
        };
        var expected = new LocalValue
        {
            Name = "Comparison value",
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create(7)
        };
        var condition = new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = actual.Id.ToString("D"),
            Operator = ConditionOperator.Equals,
            Comparison = new ComparisonOperand
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.LocalValue,
                SourceId = expected.Id.ToString("D")
            }
        };
        var job = new Job
        {
            Name = "local comparison",
            Variables = [actual],
            LocalValues = [expected],
            Steps =
            [
                new IfStep { Settings = new() { Conditions = [condition] } },
                Text("equal"),
                new ElseStep(),
                Text("different"),
                new EndIfStep()
            ]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job);

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        Assert.Equal(["equal"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_UserChoiceConditionComparesStableIdAndSelectsNamedBranch()
    {
        var choice = new UserChoiceStep
        {
            Id = "choice",
            Settings = new()
            {
                Title = "Environment",
                Question = "Choose",
                Options =
                [
                    new() { Id = "dev-id", Label = "Development" },
                    new() { Id = "prod-id", Label = "Production" }
                ]
            }
        };
        var condition = new StepCondition
        {
            SourceStepId = choice.Id,
            PropertyId = "selected_option_id",
            PropertyPath = nameof(UserChoiceResult.SelectedOptionId),
            Operator = ConditionOperator.Equals,
            Comparison = new() { Value = "prod-id" }
        };
        var job = new Job
        {
            Name = "choice branch",
            Steps =
            [
                choice,
                new IfStep { Settings = new() { Conditions = [condition] } },
                Text("production"),
                new ElseStep(),
                Text("development"),
                new EndIfStep()
            ]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithUserChoice("prod-id");

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        Assert.Equal(["production"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Theory]
    [InlineData(true, "if")]
    [InlineData(false, "else")]
    public async Task ExecuteJob_ChoosesIfOrElseFromCurrentWindowsState(bool muted, string expected)
    {
        var audio = new WindowsStateQueryStep { Id = "audio", Settings = new() { QueryType = "audio.volume" } };
        var job = new Job { Name = "branch", Steps = [audio,
            new IfStep { Settings = Settings(ConditionOperator.IsTrue) }, Text("if"), new ElseStep(), Text("else"), new EndIfStep()] };
        var builder = new JobExecutorTestBuilder().WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = muted });
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal([expected], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_FirstMatchingElseIfWinsAndLaterBranchesAreSkipped()
    {
        var audio = new WindowsStateQueryStep { Id = "audio", Settings = new() { QueryType = "audio.volume" } };
        var job = new Job { Name = "elseif", Steps = [audio,
            new IfStep { Settings = Settings(ConditionOperator.IsFalse) }, Text("if"),
            new ElseIfStep { Settings = Settings(ConditionOperator.IsTrue) }, Text("elseif"),
            new ElseStep(), Text("else"), new EndIfStep()] };
        var builder = new JobExecutorTestBuilder().WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal(["elseif"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_WindowsSettingRunsOnlyInsideSelectedBranch()
    {
        var audio = new WindowsStateQueryStep
        {
            Id = "audio",
            Settings = new() { QueryType = "audio.volume" }
        };
        var skipped = new WindowsSettingChangeStep
        {
            Settings = new()
            {
                SettingId = "audio.master_volume",
                Parameters = new() { ["value"] = "20" }
            }
        };
        var executed = new WindowsSettingChangeStep
        {
            Settings = new()
            {
                SettingId = "audio.mute",
                Parameters = new() { ["state"] = "on" }
            }
        };
        var job = new Job
        {
            Name = "setting branch",
            Steps =
            [
                audio,
                new IfStep { Settings = Settings(ConditionOperator.IsFalse) },
                skipped,
                new ElseStep(),
                executed,
                new EndIfStep()
            ]
        };
        var builder = new JobExecutorTestBuilder()
            .WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        var change = Assert.Single(builder.WindowsSettings.Changes);
        Assert.Equal("audio.mute", change.SettingId);
        Assert.Equal("on", change.Parameters["state"]);
    }

    [Fact]
    public async Task ExecuteJob_RepeatingConditionUsesFreshResultEachIteration()
    {
        using var cts = new CancellationTokenSource();
        var audio = new WindowsStateQueryStep { Id = "audio", Settings = new() { QueryType = "audio.volume" } };
        var job = new Job { Name = "fresh", Repeating = true, Steps = [audio,
            new IfStep { Settings = Settings(ConditionOperator.IsTrue) }, Text("muted"),
            new ElseStep(), Text("audible"), new EndIfStep()] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithWindowsStates(
            new AudioVolumeQueryResult { IsMuted = false }, new AudioVolumeQueryResult { IsMuted = true });
        builder.Overlay.OnShowText = _ => { if (builder.Overlay.TextCalls.Count == 2) cts.Cancel(); };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id, cts.Token);
        Assert.Equal(["audible", "muted"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task Debugger_StoresStructuredIfEvaluationWithActualAndExpectedValues()
    {
        var audio = new WindowsStateQueryStep
        {
            Id = "audio",
            Settings = new() { QueryType = "audio.volume" }
        };
        var ifStep = new IfStep
        {
            Id = "if",
            Settings = Settings(ConditionOperator.IsTrue)
        };
        var job = new Job
        {
            Name = "debug condition",
            Steps = [audio, ifStep, Text("muted"), new EndIfStep()]
        };
        var builder = new JobExecutorTestBuilder()
            .WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });
        using var executor = await builder.BuildAsync();
        using var cancellation = new JobExecutionCancellation(CancellationToken.None);
        var session = new JobDebugSession(Guid.NewGuid(), job);

        var execution = executor.ExecuteJob(job.Id, JobStartContext.Unknown, cancellation, session);
        session.Continue();
        await execution;

        var evaluation = session.GetSnapshot(ifStep.Id)!.ConditionEvaluation;
        Assert.NotNull(evaluation);
        Assert.Equal(ConditionDebugState.Met, evaluation.State);
        Assert.True(evaluation.BranchExecuted);
        var condition = Assert.Single(evaluation.Conditions);
        Assert.Equal(ConditionDebugState.Met, condition.State);
        Assert.Equal("true", condition.ActualValue);
        Assert.Equal("Festwert true", condition.ExpectedValue);
        Assert.Same(ifStep.Settings.Conditions[0], condition.Definition);
    }

    [Fact]
    public async Task Debugger_DistinguishesSkippedElseIfFromFalseCondition()
    {
        var audio = new WindowsStateQueryStep
        {
            Id = "audio",
            Settings = new() { QueryType = "audio.volume" }
        };
        var ifStep = new IfStep { Settings = Settings(ConditionOperator.IsTrue) };
        var elseIf = new ElseIfStep
        {
            Id = "else-if",
            Settings = Settings(ConditionOperator.IsFalse)
        };
        var job = new Job
        {
            Name = "skipped else-if",
            Steps = [audio, ifStep, Text("if"), elseIf, Text("else-if"), new EndIfStep()]
        };
        var builder = new JobExecutorTestBuilder()
            .WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });
        using var executor = await builder.BuildAsync();
        using var cancellation = new JobExecutionCancellation(CancellationToken.None);
        var session = new JobDebugSession(Guid.NewGuid(), job);

        var execution = executor.ExecuteJob(job.Id, JobStartContext.Unknown, cancellation, session);
        session.Continue();
        await execution;

        var evaluation = session.GetSnapshot(elseIf.Id)!.ConditionEvaluation;
        Assert.NotNull(evaluation);
        Assert.Equal(ConditionDebugState.NotEvaluated, evaluation.State);
        Assert.Equal(ConditionDebugState.NotEvaluated, Assert.Single(evaluation.Conditions).State);
        Assert.False(evaluation.BranchExecuted);
    }

    [Fact]
    public async Task ExecuteJob_DoesNotMatchElseIfFromDefaultResultOfSkippedStep()
    {
        var enabled = new JobVariable
        {
            Name = "Enabled",
            ValueKind = ResultValueKind.Boolean,
            Value = System.Text.Json.Nodes.JsonValue.Create(false)
        };
        var skippedSource = new WindowsStateQueryStep
        {
            Id = "skipped-audio",
            Settings = new() { QueryType = "audio.volume" }
        };
        var job = new Job
        {
            Name = "skipped condition source",
            Variables = [enabled],
            Steps =
            [
                new IfStep
                {
                    Settings = new()
                    {
                        Conditions =
                        [
                            new StepCondition
                            {
                                ProviderId = ValueProviderIds.JobVariable,
                                SourceId = enabled.Id.ToString("D"),
                                Operator = ConditionOperator.IsTrue
                            }
                        ]
                    }
                },
                skippedSource,
                new ElseIfStep
                {
                    Settings = new()
                    {
                        Conditions =
                        [
                            new StepCondition
                            {
                                SourceStepId = skippedSource.Id,
                                PropertyPath = "IsMuted",
                                Operator = ConditionOperator.IsFalse
                            }
                        ]
                    }
                },
                Text("default-result-branch"),
                new ElseStep(),
                Text("fallback"),
                new EndIfStep()
            ]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job);

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        Assert.Equal(["fallback"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task Debugger_ShortCircuitsRemainingAnyConditions()
    {
        var audio = new WindowsStateQueryStep
        {
            Id = "audio",
            Settings = new() { QueryType = "audio.volume" }
        };
        var ifStep = new IfStep
        {
            Id = "if",
            Settings = new()
            {
                MatchMode = ConditionMatchMode.Any,
                Conditions =
                [
                    new StepCondition
                    {
                        SourceStepId = audio.Id,
                        PropertyPath = "IsMuted",
                        Operator = ConditionOperator.IsTrue
                    },
                    new StepCondition
                    {
                        SourceStepId = audio.Id,
                        PropertyPath = "IsMuted",
                        Operator = ConditionOperator.IsFalse
                    }
                ]
            }
        };
        var job = new Job
        {
            Name = "short circuit",
            Steps = [audio, ifStep, Text("matched"), new EndIfStep()]
        };
        var builder = new JobExecutorTestBuilder()
            .WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });
        using var executor = await builder.BuildAsync();
        using var cancellation = new JobExecutionCancellation(CancellationToken.None);
        var session = new JobDebugSession(Guid.NewGuid(), job);

        var execution = executor.ExecuteJob(job.Id, JobStartContext.Unknown, cancellation, session);
        session.Continue();
        await execution;

        var evaluation = session.GetSnapshot(ifStep.Id)!.ConditionEvaluation!;
        Assert.Equal(ConditionDebugState.Met, evaluation.Conditions[0].State);
        Assert.Equal(ConditionDebugState.NotEvaluated, evaluation.Conditions[1].State);
    }

    private static IfConditionSettings Settings(ConditionOperator op) => new() { Conditions = [new StepCondition
        { SourceStepId = "audio", PropertyPath = "IsMuted", Operator = op }] };
    private static ShowTextStep Text(string text) => new() { Settings = new() { Text = text, ClearOnJobEnd = false } };
}
