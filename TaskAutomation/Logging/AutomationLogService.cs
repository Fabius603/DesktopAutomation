using TaskAutomation.Automations;

namespace TaskAutomation.Logging;

public sealed class AutomationLogService : IAutomationLogService, IDisposable
{
    private readonly ILogRepository _repository;
    private readonly Dictionary<Guid, string> _names = new();
    private readonly object _gate = new();
    public AutomationLogService(ILogRepository repository) { _repository = repository; _repository.EntryWritten += OnEntry; }
    public event EventHandler<AutomationLogEntry>? EntryWritten;
    public event EventHandler? LogsChanged;
    public IReadOnlyList<AutomationLog> Logs
    {
        get
        {
            var events = new List<LogEvent>();
            var query = new LogQuery(Source: LogSource.Automation, PageSize: 10000);
            while (true)
            {
                var page = _repository.Query(query);
                events.AddRange(page.Entries.Where(entry => entry.SourceId.HasValue));
                if (page.NextBeforeSequence is null) break;
                query = query with { BeforeSequence = page.NextBeforeSequence.Value, SnapshotSequence = page.SnapshotSequence };
            }
            lock (_gate) return events.GroupBy(entry => entry.SourceId!.Value).Select(group =>
                new AutomationLog(group.Key, _names.GetValueOrDefault(group.Key) ?? group.First().SourceName,
                    _repository.DirectoryPath, group.Min(entry => entry.Timestamp), group.Max(entry => entry.Timestamp))).ToArray();
        }
    }
    public void Synchronize(IEnumerable<AutomationDefinition> automations)
    {
        lock (_gate) foreach (var automation in automations) _names[automation.Id] = automation.Name;
        LogsChanged?.Invoke(this, EventArgs.Empty);
    }
    public void Write(Guid automationId, ExecutionLogLevel level, string message, string? details = null)
        => Record(automationId, new LogEvent { Level = level, Message = message, Details = details });
    public void Record(Guid automationId, LogEvent entry)
    {
        string name;
        lock (_gate) name = _names.GetValueOrDefault(automationId) ?? automationId.ToString();
        _repository.Append(entry with
        {
            Source = LogSource.Automation,
            SourceId = automationId,
            SourceName = name,
            Area = "Automation",
            Context = entry.Context with { AutomationId = automationId }
        });
    }
    public IReadOnlyList<AutomationLogEntry> ReadEntries(Guid automationId, int maxEntries = 3000)
        => _repository.Query(new(Source: LogSource.Automation, SourceId: automationId, PageSize: Math.Clamp(maxEntries, 1, 10000)))
            .Entries.OrderBy(entry => entry.Sequence).Select(ToEntry).ToArray();
    public Task<IReadOnlyList<AutomationLogEntry>> ReadEntriesAsync(Guid automationId, int maxEntries = 3000, CancellationToken cancellationToken = default)
        => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); return ReadEntries(automationId, maxEntries); }, cancellationToken);
    private static AutomationLogEntry ToEntry(LogEvent entry) => new()
    {
        Id = entry.Id,
        Sequence = entry.Sequence,
        AutomationId = entry.SourceId ?? Guid.Empty,
        Timestamp = entry.Timestamp,
        Level = entry.Level,
        Message = entry.Message,
        Details = entry.Details
    };
    private void OnEntry(object? sender, LogEvent entry)
    {
        if (entry.Source != LogSource.Automation) return;
        EntryWritten?.Invoke(this, ToEntry(entry)); LogsChanged?.Invoke(this, EventArgs.Empty);
    }
    public void Dispose() => _repository.EntryWritten -= OnEntry;
}
