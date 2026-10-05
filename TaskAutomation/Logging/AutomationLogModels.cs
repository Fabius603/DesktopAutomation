using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using TaskAutomation.Automations;
using Common.ApplicationData;

namespace TaskAutomation.Logging;

public sealed class AutomationLog
{
    internal AutomationLog(Guid automationId, string name, string filePath, DateTimeOffset createdAt, DateTimeOffset lastEntryAt)
    {
        AutomationId = automationId;
        Name = name;
        FilePath = filePath;
        CreatedAt = createdAt;
        LastEntryAt = lastEntryAt;
    }

    public Guid AutomationId { get; }
    public string Name { get; internal set; }
    public string FilePath { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastEntryAt { get; internal set; }
}

public sealed class AutomationLogEntry
{
    public Guid Id { get; init; }
    public long Sequence { get; init; }
    public Guid AutomationId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public ExecutionLogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
}

public interface IAutomationLogService
{
    event EventHandler<AutomationLogEntry>? EntryWritten;
    event EventHandler? LogsChanged;
    IReadOnlyList<AutomationLog> Logs { get; }
    void Synchronize(IEnumerable<AutomationDefinition> automations);
    void Record(Guid automationId, LogEvent entry);
    void Write(Guid automationId, ExecutionLogLevel level, string message, string? details = null);
    IReadOnlyList<AutomationLogEntry> ReadEntries(Guid automationId, int maxEntries = 3000);
    Task<IReadOnlyList<AutomationLogEntry>> ReadEntriesAsync(Guid automationId, int maxEntries = 3000,
        CancellationToken cancellationToken = default);
}

