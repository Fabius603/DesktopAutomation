using System.Text.Json;

namespace TaskAutomation.Jobs;

public sealed record SerializedJobStepsSnapshot(
    string StartStepsJson,
    string RunStepsJson,
    string EndStepsJson);

public sealed record MaterializedJobStepsSnapshot(
    IReadOnlyList<JobStep> StartSteps,
    IReadOnlyList<JobStep> RunSteps,
    IReadOnlyList<JobStep> EndSteps);

public sealed record JobStepGraph(IReadOnlyList<JobStep> Steps, IReadOnlyList<LocalValue> LocalValues);

public static class JobStepsSnapshotService
{
    public static Job CaptureExecution(Job source) =>
        JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(source))
        ?? throw new JsonException("Job execution snapshot could not be materialized.");

    public static Job CloneJob(Job source, string name)
    {
        var copy = JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(source))
            ?? throw new JsonException("Job copy could not be materialized.");
        copy.Id = Guid.NewGuid();
        copy.Name = name;
        return copy;
    }

    public static Task<JobStepGraph> CaptureGraphAsync(Job job, IReadOnlyList<JobStep> steps,
        CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        var source = new Job { Steps = steps.ToList(), LocalValues = job.LocalValues };
        var ids = ValueReferenceUsageInspector.Find(source)
            .Where(usage => usage.Reference.ProviderId == ValueProviderIds.LocalValue)
            .Select(usage => usage.Reference.SourceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new JobStepGraph(Deserialize(Serialize(steps)),
            CloneValues(job.LocalValues.Where(value => ids.Contains(value.Id.ToString("D")))));
    }, cancellationToken);

    public static Task<JobStepGraph> CloneGraphAsync(JobStepGraph source,
        CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        var clones = Deserialize(Serialize(source.Steps));
        var stepIds = source.Steps.Zip(clones).ToDictionary(pair => pair.First.Id,
            pair => Guid.NewGuid().ToString("D"), StringComparer.OrdinalIgnoreCase);
        foreach (var step in clones) step.Id = stepIds[step.Id];
        var locals = CloneValues(source.LocalValues);
        var localIds = locals.ToDictionary(value => value.Id, _ => Guid.NewGuid());
        var usages = ValueReferenceUsageInspector.Find(new Job { Steps = clones, LocalValues = locals });
        foreach (var value in locals)
        {
            value.Id = localIds[value.Id];
            if (stepIds.TryGetValue(value.OwnerStepId, out var owner)) value.OwnerStepId = owner;
            if (value.EnumTypeName is { } enumType)
                foreach (var (oldStep, newStep) in stepIds)
                    if (enumType == Steps.UserChoiceEnumContract.ForStep(oldStep))
                        value.EnumTypeName = Steps.UserChoiceEnumContract.ForStep(newStep);
        }
        foreach (var usage in usages)
            usage.UpdateReference(reference =>
            {
                if (reference.ProviderId == ValueProviderIds.LocalValue
                    && Guid.TryParse(reference.SourceId, out var id) && localIds.TryGetValue(id, out var mapped))
                    reference.SourceId = mapped.ToString("D");
                else if (reference is ResultBinding binding && binding.TryGetStepResult(out var result)
                         && stepIds.TryGetValue(result.StepId, out var stepId))
                {
                    binding.ProviderId = ValueProviderIds.StepResult;
                    binding.SourceId = StepResultSourceIdCodec.Create(stepId, result.PropertyId);
                    binding.LegacySourceStepId = null;
                    binding.LegacyPropertyId = null;
                }
            });
        return new JobStepGraph(clones, locals);
    }, cancellationToken);

    private static List<LocalValue> CloneValues(IEnumerable<LocalValue> values) =>
        JsonSerializer.Deserialize<List<LocalValue>>(JsonSerializer.Serialize(values)) ?? [];

    public static Task<SerializedJobStepsSnapshot> SerializeAsync(
        IReadOnlyList<JobStep> startSteps,
        IReadOnlyList<JobStep> runSteps,
        IReadOnlyList<JobStep> endSteps,
        CancellationToken cancellationToken = default)
        => Task.Run(() => new SerializedJobStepsSnapshot(
            Serialize(startSteps),
            Serialize(runSteps),
            Serialize(endSteps)), cancellationToken);

    public static Task<MaterializedJobStepsSnapshot> DeserializeAsync(
        SerializedJobStepsSnapshot snapshot,
        CancellationToken cancellationToken = default)
        => Task.Run(() => new MaterializedJobStepsSnapshot(
            Deserialize(snapshot.StartStepsJson),
            Deserialize(snapshot.RunStepsJson),
            Deserialize(snapshot.EndStepsJson)), cancellationToken);

    public static Task<IReadOnlyList<JobStep>> CloneAsync(
        IReadOnlyList<JobStep> steps,
        bool newIds,
        CancellationToken cancellationToken = default)
        => Task.Run<IReadOnlyList<JobStep>>(async () =>
        {
            var clones = Deserialize(Serialize(steps));
            if (newIds)
                return (await CloneGraphAsync(new JobStepGraph(clones, []), cancellationToken)).Steps;
            return clones;
        }, cancellationToken);

    private static string Serialize(IReadOnlyList<JobStep> steps)
        => JsonSerializer.Serialize(steps);

    private static List<JobStep> Deserialize(string json)
        => JsonSerializer.Deserialize<List<JobStep>>(json) ?? [];
}
