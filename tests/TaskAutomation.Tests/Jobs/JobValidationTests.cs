using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobValidationTests
{
    [Fact]
    public void ValidateJob_EmptyJob_IsValid() => Assert.True(JobValidation.ValidateJob(new Job()).IsValid);

    [Fact]
    public void ValidateStep_DisabledInvalidStep_IsAllowed()
    {
        var step = new ShowTextStep { IsEnabled = false, Settings = new() { Text = "", FontSize = -1 } };
        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.01)]
    public void ValidateStep_ShowTextOpacityOutsideUnitRange_IsInvalid(double opacity)
    {
        var step = new ShowTextStep { Settings = new() { Text = "x", Opacity = (float)opacity } };
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_WindowsQueryRequiredParameterMissing_IsInvalid()
    {
        var step = new WindowsStateQueryStep { Settings = new() { QueryType = "filesystem.path" } };
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_WindowsQueryRequiredParameterPresent_IsValid()
    {
        var step = new WindowsStateQueryStep { Settings = new() { QueryType = "filesystem.path",
            Parameters = new(StringComparer.OrdinalIgnoreCase) { ["path"] = "C:\\temp" } } };
        Assert.True(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateStep_UnknownWindowsQuery_IsInvalid()
    {
        var step = new WindowsStateQueryStep { Settings = new() { QueryType = "unknown.query" } };
        Assert.False(JobValidation.ValidateStep([step], step).IsValid);
    }

    [Fact]
    public void ValidateJob_CompleteIfElseStructure_IsValid()
    {
        var source = AudioStep("audio");
        var condition = Condition("audio", "IsMuted", ConditionOperator.IsTrue);
        var job = new Job { Steps = [source, new IfStep { Settings = new() { Conditions = [condition] } },
            new ShowTextStep { Settings = new() { Text = "muted" } }, new ElseStep(),
            new ShowTextStep { Settings = new() { Text = "audible" } }, new EndIfStep()] };
        Assert.True(JobValidation.ValidateJob(job).IsValid);
    }

    [Fact]
    public void ValidateJob_IfWithoutEndIf_IsInvalid()
    {
        var source = AudioStep("audio");
        var @if = new IfStep { Settings = new() { Conditions = [Condition("audio", "IsMuted", ConditionOperator.IsTrue)] } };
        var result = JobValidation.ValidateJob(new Job { Steps = [source, @if] });
        Assert.False(result.IsValid);
        Assert.Contains(result.Steps, item => item.Step == @if && item.Error!.Contains("EndIf"));
    }

    [Fact]
    public void ValidateJob_ElseWithoutIf_IsInvalid() =>
        Assert.False(JobValidation.ValidateJob(new Job { Steps = [new ElseStep()] }).IsValid);

    [Fact]
    public void ValidateJob_ElseIfAfterElse_IsInvalid()
    {
        var source = AudioStep("audio");
        var settings = new IfConditionSettings { Conditions = [Condition("audio", "IsMuted", ConditionOperator.IsTrue)] };
        Assert.False(JobValidation.ValidateJob(new Job { Steps = [source, new IfStep { Settings = settings }, new ElseStep(),
            new ElseIfStep { Settings = settings }, new EndIfStep()] }).IsValid);
    }

    [Fact]
    public void ValidateStep_ResultBindingToLaterStep_IsInvalid()
    {
        var display = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" } } };
        var source = AudioStep("audio");
        Assert.False(JobValidation.ValidateStep([display, source], display).IsValid);
    }

    [Fact]
    public void ValidateStep_ResultBindingToPriorCompatibleStep_IsValid()
    {
        var source = AudioStep("audio");
        var display = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" } } };
        Assert.True(JobValidation.ValidateStep([source, display], display).IsValid);
    }

    [Fact]
    public void RemoveInvalidSourceSelections_RemovesMissingButPreservesTemporarilyInvalidReferences()
    {
        var source = AudioStep("audio");
        var missing = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "gone", PropertyPath = "Text" } } };
        var later = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "audio", PropertyPath = "Text" } } };
        JobValidation.RemoveInvalidSourceSelections([missing, later, source]);
        Assert.Equal(string.Empty, missing.Settings.TextResult.SourceStepId);
        Assert.Equal("audio", later.Settings.TextResult.SourceStepId);
    }

    private static WindowsStateQueryStep AudioStep(string id) => new() { Id = id, Settings = new() { QueryType = "audio.volume" } };
    private static StepCondition Condition(string id, string path, ConditionOperator op) => new()
        { SourceStepId = id, PropertyPath = path, Operator = op };
}
