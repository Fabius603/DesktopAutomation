using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TaskAutomation.Logging;

namespace DesktopAutomation.Application.Logging;

public sealed record LogExportResult(string Path, int EventCount, bool IsComplete, IReadOnlyList<string> Issues);

public sealed class LogExportService(ILogRepository repository, LogQueryService queries)
{
    private static void Write(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
    public async Task<LogExportResult> ExportAsync(LogQuery query, string destination, string applicationVersion,
        CancellationToken ct = default)
    {
        destination = Path.GetFullPath(destination);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var issues = new List<string>();
        try
        {
            try { await repository.FlushAsync(ct).ConfigureAwait(false); }
            catch (IOException) { issues.Add("storage.flush-failed"); }
            return await Task.Run(() =>
            {
                var entries = queries.ReadAll(query, ct, out _, out var readIssues).Select(repository.Privacy.Sanitize).ToArray();
                issues.AddRange(readIssues);
                var runs = repository.ReadRuns().Where(run => entries.Any(entry => entry.Context.RunId == run.Id)
                    || query.RunId == run.Id).Select(repository.Privacy.Sanitize).ToArray();
                if (runs.Any(run => !run.IsComplete)) issues.Add("run.incomplete");
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    Write(archive, "manifest.json", JsonSerializer.Serialize(new
                    {
                        SchemaVersion = LogEvent.CurrentSchema,
                        ApplicationVersion = applicationVersion,
                        CreatedAt = DateTimeOffset.UtcNow,
                        Complete = issues.Count == 0,
                        SearchScope = "Retained schema-v2 events",
                        Query = query with { Search = repository.Privacy.Sanitize(query.Search) },
                        LastExportedSequence = entries.LastOrDefault()?.Sequence,
                        ActiveRuns = runs.Where(run => run.EndedAt is null).Select(run => run.Id).ToArray(),
                        EventCount = entries.Length,
                        Issues = issues
                    }, LogRepository.JsonOptions));
                    Write(archive, "runs.json", JsonSerializer.Serialize(runs, LogRepository.JsonOptions));
                    using var json = new StreamWriter(archive.CreateEntry("events.jsonl").Open(), new UTF8Encoding(false));
                    foreach (var entry in entries) { ct.ThrowIfCancellationRequested(); json.WriteLine(JsonSerializer.Serialize(entry, LogRepository.JsonOptions)); }
                    json.Dispose();
                    using var report = new StreamWriter(archive.CreateEntry("report.txt").Open(), new UTF8Encoding(false));
                    report.WriteLine($"DesktopAutomation {applicationVersion} — schema v2");
                    report.WriteLine(issues.Count == 0 ? "Complete within retained search scope" : "Partial: " + string.Join(", ", issues));
                    foreach (var run in runs) report.WriteLine($"{run.Name} #{run.ExecutionNumber}: {run.Outcome}; {run.DurationMs} ms; errors={run.ErrorCount}; warnings={run.WarningCount}");
                    foreach (var entry in entries) { ct.ThrowIfCancellationRequested(); report.WriteLine($"{entry.Timestamp:O} [{entry.Level}] {entry.Code}: {entry.Message}"); }
                }
                ct.ThrowIfCancellationRequested();
                File.Move(temporary, destination, false);
                return new LogExportResult(destination, entries.Length, issues.Count == 0, issues);
            }, ct).ConfigureAwait(false);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
