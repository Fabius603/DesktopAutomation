using DesktopAutomationApp.Services.Jobs;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepsSnapshotServiceTests
{
    [Fact]
    public async Task Snapshot_RoundTripsPolymorphicStepsAndAllSections()
    {
        var start = new JobStep[] { new TimeoutStep { Id = "start" } };
        var run = new JobStep[]
        {
            new IfStep { Id = "if" },
            new TemplateMatchingStep { Id = "template" },
            new EndIfStep { Id = "end-if" }
        };
        var end = new JobStep[] { new EndJobStep { Id = "end" } };

        var serialized = await JobStepsSnapshotService.SerializeAsync(start, run, end);
        var restored = await JobStepsSnapshotService.DeserializeAsync(serialized);

        Assert.IsType<TimeoutStep>(Assert.Single(restored.StartSteps));
        Assert.Collection(restored.RunSteps,
            step => Assert.IsType<IfStep>(step),
            step => Assert.IsType<TemplateMatchingStep>(step),
            step => Assert.IsType<EndIfStep>(step));
        Assert.IsType<EndJobStep>(Assert.Single(restored.EndSteps));
        Assert.Equal(["start", "if", "template", "end-if", "end"],
            restored.StartSteps.Concat(restored.RunSteps).Concat(restored.EndSteps).Select(step => step.Id));
    }

    [Fact]
    public async Task CloneWithNewIds_ProducesIndependentObjectsAndFreshIds()
    {
        var source = new JobStep[]
        {
            new TimeoutStep { Id = "one", Settings = new TimeoutSettings { DelayMs = 123 } },
            new EndJobStep { Id = "two" }
        };

        var first = await JobStepsSnapshotService.CloneAsync(source, newIds: true);
        var second = await JobStepsSnapshotService.CloneAsync(source, newIds: true);

        Assert.All(first, clone => Assert.DoesNotContain(clone.Id, source.Select(step => step.Id)));
        Assert.Empty(first.Select(step => step.Id).Intersect(second.Select(step => step.Id)));
        Assert.False(ReferenceEquals(source[0], first[0]));
        Assert.Equal(123, Assert.IsType<TimeoutStep>(first[0]).Settings.DelayMs);
    }

    [Fact]
    public async Task Snapshot_RoundTripsEveryConcreteJobStepType()
    {
        var stepTypes = typeof(JobStep).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(JobStep).IsAssignableFrom(type))
            .OrderBy(type => type.FullName)
            .ToArray();
        var steps = stepTypes
            .Select((type, index) =>
            {
                var step = Assert.IsAssignableFrom<JobStep>(Activator.CreateInstance(type));
                step.Id = $"step-{index}";
                return step;
            })
            .ToArray();

        var snapshot = await JobStepsSnapshotService.SerializeAsync([], steps, []);
        var restored = await JobStepsSnapshotService.DeserializeAsync(snapshot);

        Assert.Equal(stepTypes, restored.RunSteps.Select(step => step.GetType()));
        Assert.Equal(steps.Select(step => step.Id), restored.RunSteps.Select(step => step.Id));
    }
}
