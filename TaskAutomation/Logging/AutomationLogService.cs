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
    void Write(Guid automationId, ExecutionLogLevel level, string message, string? details = null);
    IReadOnlyList<AutomationLogEntry> ReadEntries(Guid automationId, int maxEntries = 3000);
}

public sealed class AutomationLogService : IAutomationLogService
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, AutomationLog> _logs = new();
    private readonly string _directory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public AutomationLogService()
    {
        _directory = AppPaths.AutomationLogsDirectory;
        Directory.CreateDirectory(_directory);
    }

    public event EventHandler<AutomationLogEntry>? EntryWritten;
    public event EventHandler? LogsChanged;

    public IReadOnlyList<AutomationLog> Logs
    {
        get { lock (_gate) return _logs.Values.OrderBy(log => log.Name).ToArray(); }
    }

    public void Synchronize(IEnumerable<AutomationDefinition> automations)
    {
        var definitions = automations.ToArray();
        var activeIds = definitions.Select(automation => automation.Id).ToHashSet();
        lock (_gate)
        {
            foreach (var removedId in _logs.Keys.Where(id => !activeIds.Contains(id)).ToArray())
                _logs.Remove(removedId);

            foreach (var automation in definitions)
            {
                var filePath = Path.Combine(_directory, $"{automation.Id:N}.log");
                if (_logs.TryGetValue(automation.Id, out var current))
                {
                    current.Name = automation.Name;
                    continue;
                }

                var exists = File.Exists(filePath);
                var createdAt = exists ? File.GetCreationTime(filePath) : DateTimeOffset.Now;
                var lastAt = exists ? File.GetLastWriteTime(filePath) : createdAt;
                _logs[automation.Id] = new AutomationLog(automation.Id, automation.Name, filePath, createdAt, lastAt);
            }
        }

        LogsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Write(Guid automationId, ExecutionLogLevel level, string message, string? details = null)
    {
        AutomationLog? log;
        AutomationLogEntry entry;
        lock (_gate)
        {
            if (!_logs.TryGetValue(automationId, out log)) return;
            entry = new AutomationLogEntry
            {
                AutomationId = automationId,
                Timestamp = DateTimeOffset.Now,
                Level = level,
                Message = message,
                Details = details
            };
            try
            {
                File.AppendAllText(log.FilePath, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine, Encoding.UTF8);
                log.LastEntryAt = entry.Timestamp;
            }
            catch (IOException) { return; }
            catch (UnauthorizedAccessException) { return; }
        }
        EntryWritten?.Invoke(this, entry);
    }

    public IReadOnlyList<AutomationLogEntry> ReadEntries(Guid automationId, int maxEntries = 3000)
    {
        string? filePath;
        lock (_gate) filePath = _logs.GetValueOrDefault(automationId)?.FilePath;
        if (filePath == null || !File.Exists(filePath)) return Array.Empty<AutomationLogEntry>();
        try
        {
            return File.ReadLines(filePath)
                .Select(line =>
                {
                    try { return JsonSerializer.Deserialize<AutomationLogEntry>(line.TrimStart('\uFEFF'), JsonOptions); }
                    catch (JsonException) { return null; }
                })
                .Where(entry => entry != null)
                .Cast<AutomationLogEntry>()
                .TakeLast(maxEntries)
                .ToArray();
        }
        catch (IOException) { return Array.Empty<AutomationLogEntry>(); }
        catch (UnauthorizedAccessException) { return Array.Empty<AutomationLogEntry>(); }
    }
}
