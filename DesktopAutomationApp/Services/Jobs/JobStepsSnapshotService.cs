using System.Text.Json;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Services.Jobs;

internal sealed record SerializedJobStepsSnapshot(
    string StartStepsJson,
    string RunStepsJson,
    string EndStepsJson);

internal sealed record MaterializedJobStepsSnapshot(
    IReadOnlyList<JobStep> StartSteps,
    IReadOnlyList<JobStep> RunSteps,
    IReadOnlyList<JobStep> EndSteps);

internal static class JobStepsSnapshotService
{
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
        => Task.Run<IReadOnlyList<JobStep>>(() =>
        {
            var clones = Deserialize(Serialize(steps));
            if (newIds)
                foreach (var clone in clones)
                    clone.Id = Guid.NewGuid().ToString();
            return clones;
        }, cancellationToken);

    private static string Serialize(IReadOnlyList<JobStep> steps)
        => JsonSerializer.Serialize(steps);

    private static List<JobStep> Deserialize(string json)
        => JsonSerializer.Deserialize<List<JobStep>>(json) ?? [];
}
