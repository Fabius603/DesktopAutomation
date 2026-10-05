using Serilog.Core;
using Serilog.Events;
using System.Globalization;

namespace TaskAutomation.Logging;

public sealed class ApplicationLogEntry
{
    public Guid Id { get; init; }
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public ExecutionLogLevel Level { get; init; }
    public string Source { get; init; } = "";
    public string Message { get; init; } = "";
    public string? Details { get; init; }
    public string TimestampText => Timestamp.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
}
public interface IApplicationLogService
{
    event EventHandler<ApplicationLogEntry>? EntryWritten;
    string LogDirectory { get; }
    IReadOnlyList<ApplicationLogEntry> ReadEntries(int maxEntries = 5000);
}

/// <summary>Serilog adapter. Structured properties and ambient run/step identity survive persistence.</summary>
public sealed class ApplicationLogService : IApplicationLogService, ILogEventSink, IDisposable
{
    private readonly ILogRepository _repository;
    public ApplicationLogService(ILogRepository repository) { _repository = repository; _repository.EntryWritten += OnEntry; }
    public event EventHandler<ApplicationLogEntry>? EntryWritten;
    public string LogDirectory => _repository.DirectoryPath;
    public void Emit(Serilog.Events.LogEvent logEvent)
    {
        var context = LogAmbient.Current;
        var parameters = new Dictionary<string, string?>();
        // Only approved primitive metadata is persisted. Arbitrary destructured objects may contain secrets.
        foreach (var key in new[] { "JobName", "StepType", "InstanceId", "JobId", "Reason", "Path", "ErrorCode" })
            if (logEvent.Properties.TryGetValue(key, out var value) && value is ScalarValue scalar)
                parameters[key] = Convert.ToString(scalar.Value, CultureInfo.InvariantCulture);
        var source = logEvent.Properties.TryGetValue("SourceContext", out var sourceValue) ? sourceValue.ToString().Trim('"') : "Application";
        _repository.Append(new LogEvent
        {
            Source = LogSource.Application,
            SourceName = source,
            Area = source,
            Timestamp = logEvent.Timestamp,
            Context = context,
            Phase = context.StepId is not null ? StepLogScope.CurrentPhase : null,
            Iteration = context.StepId is not null ? StepLogScope.CurrentIteration : null,
            Level = MapLevel(logEvent.Level),
            DiagnosticCode = logEvent.Exception is { } error ? LogDiagnostics.Code(error) : null,
            ProblemId = logEvent.Exception is null ? null : LogDiagnostics.ProblemId(logEvent.Exception, context.StepExecutionId),
            Message = context.RunId.HasValue ? RenderRunMessage(logEvent) : logEvent.RenderMessage(CultureInfo.CurrentCulture),
            Details = logEvent.Exception is { } failure && context.RunId.HasValue
                ? LogDiagnostics.ExceptionDetails(failure) : logEvent.Exception?.ToString(),
            Parameters = parameters
        });
    }
    private static string RenderRunMessage(Serilog.Events.LogEvent entry)
    {
        // Runtime string properties can contain materialized inputs or output. Keep only
        // public identities and approved paths; never render arbitrary objects or collections.
        var properties = entry.Properties.Select(property => new LogEventProperty(property.Key,
            property.Value is ScalarValue scalar && (scalar.Value is null
                || scalar.Value is not string && scalar.Value.GetType().IsPrimitive
                || scalar.Value is Guid or DateTime or DateTimeOffset or TimeSpan or Enum or decimal
                || scalar.Value is string && property.Key is ("StepType" or "JobName" or "SourceContext" or "Reason" or "ErrorCode"
                    or "Path" or "TemplatePath" or "Script" or "Exe" or "WorkingDir" or "WorkingDirectory" or "Program"))
                ? property.Value : new ScalarValue("[omitted]")));
        return new Serilog.Events.LogEvent(entry.Timestamp, entry.Level, null, entry.MessageTemplate, properties)
            .RenderMessage(CultureInfo.CurrentCulture);
    }
    public IReadOnlyList<ApplicationLogEntry> ReadEntries(int maxEntries = 5000)
        => _repository.Query(new(Source: LogSource.Application, PageSize: Math.Clamp(maxEntries, 1, 10000)))
            .Entries.OrderBy(entry => entry.Sequence).Select(ToEntry).ToArray();
    private static ExecutionLogLevel MapLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Debug or LogEventLevel.Verbose => ExecutionLogLevel.Debug,
        LogEventLevel.Warning => ExecutionLogLevel.Warning,
        LogEventLevel.Error or LogEventLevel.Fatal => ExecutionLogLevel.Error,
        _ => ExecutionLogLevel.Information
    };
    private static ApplicationLogEntry ToEntry(LogEvent entry) => new()
    {
        Id = entry.Id,
        Sequence = entry.Sequence,
        Timestamp = entry.Timestamp,
        Level = entry.Level,
        Source = entry.Area,
        Message = entry.Message,
        Details = entry.Details
    };
    private void OnEntry(object? sender, LogEvent entry) { if (entry.Source == LogSource.Application) EntryWritten?.Invoke(this, ToEntry(entry)); }
    public void Dispose() => _repository.EntryWritten -= OnEntry;
}
