using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Logging;

public static class LogRunSnapshots
{
    public static IReadOnlyList<LogStepSnapshot> Steps(Job job)
    {
        var snapshots = new List<LogStepSnapshot>();
        foreach (var (phase, steps) in new[] { ("Start", job.StartSteps), ("Main", job.Steps), ("End", job.EndSteps) })
            foreach (var step in steps)
                snapshots.Add(new(step.Id, BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition)
                    ? definition.Descriptor.TypeId : step.GetType().Name, snapshots.Count + 1, phase, step.IsEnabled));
        return snapshots;
    }
}
