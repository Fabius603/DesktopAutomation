using TaskAutomation.Automations;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.Jobs;

public sealed class DefinitionCopiesTests
{
    [Fact]
    public void JobCopy_IsIndependentAndPreservesInternalStepIdentities()
    {
        var step = new TimeoutStep();
        var source = new Job { Name = "source", Steps = [step] };
        var copy = JobStepsSnapshotService.CloneJob(source, "copy");
        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal("copy", copy.Name);
        Assert.Equal(step.Id, Assert.Single(copy.Steps).Id);
        Assert.NotSame(step, copy.Steps[0]);
        copy.Steps.Clear();
        Assert.Single(source.Steps);
    }

    [Fact]
    public void AutomationCopy_IsDisabledWithIndependentTriggerAndNoExecutionHistory()
    {
        var source = new AutomationDefinition { Active = true, LastRunAt = DateTimeOffset.Now };
        var copy = AutomationCopies.Clone(source, "copy");
        Assert.NotEqual(source.Id, copy.Id);
        Assert.False(copy.Active);
        Assert.Null(copy.LastRunAt);
        Assert.NotSame(source.Trigger, copy.Trigger);
        Assert.NotSame(source.Action, copy.Action);
        Assert.True(source.Active);
        Assert.NotNull(source.LastRunAt);
    }
}
