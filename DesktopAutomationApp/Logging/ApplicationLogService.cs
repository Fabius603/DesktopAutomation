using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;
using TaskAutomation.Logging;

namespace DesktopAutomationApp.Logging;

public sealed class ApplicationLogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public ExecutionLogLevel Level { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
    public string TimestampText => Timestamp.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("de-DE"));
}

public interface IApplicationLogService
{
    event EventHandler<ApplicationLogEntry>? EntryWritten;
    string LogDirectory { get; }
    IReadOnlyList<ApplicationLogEntry> ReadEntries(int maxEntries = 5000);
}

public sealed partial class ApplicationLogService : IApplicationLogService, ILogEventSink
{
    private readonly object _gate = new();
    private readonly List<ApplicationLogEntry> _liveEntries = new();

    public ApplicationLogService(string logDirectory)
    {
        LogDirectory = logDirectory;
        Directory.CreateDirectory(LogDirectory);
    }

    public event EventHandler<ApplicationLogEntry>? EntryWritten;
    public string LogDirectory { get; }

    public void Emit(LogEvent logEvent)
    {
        var entry = new ApplicationLogEntry
        {
            Timestamp = logEvent.Timestamp,
            Level = MapLevel(logEvent.Level),
            Source = logEvent.Properties.TryGetValue("SourceContext", out var source)
                ? source.ToString().Trim('"')
                : string.Empty,
            Message = logEvent.RenderMessage(CultureInfo.CurrentCulture),
            Details = logEvent.Exception?.ToString()
        };
        lock (_gate)
        {
            _liveEntries.Add(entry);
            if (_liveEntries.Count > 5000) _liveEntries.RemoveRange(0, _liveEntries.Count - 5000);
        }
        EntryWritten?.Invoke(this, entry);
    }

    public IReadOnlyList<ApplicationLogEntry> ReadEntries(int maxEntries = 5000)
    {
        var entries = new List<ApplicationLogEntry>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(LogDirectory, "desktop-automation-*.log")
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                ApplicationLogEntry? pending = null;
                var pendingDetails = new List<string>();
                foreach (var line in File.ReadLines(file))
                {
                    var match = LogLineRegex().Match(line);
                    if (!match.Success)
                    {
                        if (pending != null && !string.IsNullOrWhiteSpace(line)) pendingDetails.Add(line);
                        continue;
                    }
                    if (pending != null) entries.Add(WithDetails(pending, pendingDetails));
                    pending = null;
                    pendingDetails.Clear();
                    if (!DateTimeOffset.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss.fff",
                            CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var timestamp)) continue;
                    pending = new ApplicationLogEntry
                    {
                        Timestamp = timestamp,
                        Level = ParseLevel(match.Groups[2].Value),
                        Source = match.Groups[3].Value,
                        Message = match.Groups[4].Value
                    };
                }
                if (pending != null) entries.Add(WithDetails(pending, pendingDetails));
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        lock (_gate)
        {
            var latestFileTimestamp = entries.Count == 0 ? DateTimeOffset.MinValue : entries.Max(entry => entry.Timestamp);
            entries.AddRange(_liveEntries.Where(entry => entry.Timestamp > latestFileTimestamp));
        }
        return entries.OrderBy(entry => entry.Timestamp).TakeLast(maxEntries).ToArray();
    }

    private static ExecutionLogLevel MapLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose or LogEventLevel.Debug => ExecutionLogLevel.Debug,
        LogEventLevel.Warning => ExecutionLogLevel.Warning,
        LogEventLevel.Error or LogEventLevel.Fatal => ExecutionLogLevel.Error,
        _ => ExecutionLogLevel.Information
    };

    private static ApplicationLogEntry WithDetails(ApplicationLogEntry entry, List<string> details) => new()
    {
        Timestamp = entry.Timestamp,
        Level = entry.Level,
        Source = entry.Source,
        Message = entry.Message,
        Details = details.Count == 0 ? null : string.Join(Environment.NewLine, details)
    };

    private static ExecutionLogLevel ParseLevel(string level) => level switch
    {
        "DBG" or "VRB" => ExecutionLogLevel.Debug,
        "WRN" => ExecutionLogLevel.Warning,
        "ERR" or "FTL" => ExecutionLogLevel.Error,
        _ => ExecutionLogLevel.Information
    };

    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(\w{3})\] (\S*)\s?(.*)$")]
    private static partial Regex LogLineRegex();
}
