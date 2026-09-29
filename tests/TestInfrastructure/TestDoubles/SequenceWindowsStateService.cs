using TaskAutomation.WindowsIntegration;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed class SequenceWindowsStateService(params WindowsStateQueryResult[] results) : IWindowsSystemStateService
{
    private readonly Queue<WindowsStateQueryResult> _results = new(results);
    public List<WindowsStateQuery> Queries { get; } = [];

    public Task<WindowsStateQueryResult> QueryAsync(WindowsStateQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Queries.Add(query);
        if (_results.Count == 0) throw new InvalidOperationException("Keine weitere Testantwort konfiguriert.");
        return Task.FromResult(_results.Dequeue());
    }
}
