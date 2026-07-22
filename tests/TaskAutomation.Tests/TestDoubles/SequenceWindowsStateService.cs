using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed class SequenceWindowsStateService(params WindowsStateSnapshot[] snapshots) : IWindowsSystemStateService
{
    private readonly Queue<WindowsStateSnapshot> _snapshots = new(snapshots);
    public List<WindowsStateQuery> Queries { get; } = [];

    public Task<WindowsStateSnapshot> QueryAsync(WindowsStateQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Queries.Add(query);
        if (_snapshots.Count == 0) throw new InvalidOperationException("Keine weitere Testantwort konfiguriert.");
        return Task.FromResult(_snapshots.Dequeue());
    }
}
