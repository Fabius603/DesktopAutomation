using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TaskAutomation.Logging;

namespace DesktopAutomation.Application.Logging;

public sealed record LogExportSelection(IReadOnlyList<Guid> RunIds, IReadOnlyList<Guid> EventIds, long SnapshotSequence);

public sealed record LogExportResult(string Path, int EventCount, bool IsComplete, IReadOnlyList<string> Issues);

public sealed class LogExportService(ILogRepository repository, LogQueryService queries)
{
    private static void Write(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
    public Task<LogExportResult> ExportAsync(LogQuery query, string destination, string applicationVersion,
        CancellationToken ct = default) => ExportCoreAsync(query, null, destination, applicationVersion, ct);
    public Task<LogExportResult> ExportRunsAsync(RunQuery query, string destination, string applicationVersion,
        CancellationToken ct = default) => ExportCoreAsync(new(), query, destination, applicationVersion, ct);
    public Task<LogExportResult> ExportSelectionAsync(LogExportSelection selection, string destination,
        string applicationVersion, CancellationToken ct = default)
        => ExportCoreAsync(new(SnapshotSequence: selection.SnapshotSequence), null, destination, applicationVersion, ct, selection);

    private async Task<LogExportResult> ExportCoreAsync(LogQuery query, RunQuery? runQuery, string destination,
        string applicationVersion, CancellationToken ct, LogExportSelection? selection = null)
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
                LogRun[]? selected = null;
                if (runQuery is not null)
                {
                    var snapshot = Math.Min(runQuery.SnapshotSequence, repository.SnapshotSequence);
                    runQuery = runQuery with { SnapshotSequence = snapshot, Offset = 0 };
                    selected = queries.SelectRuns(runQuery, snapshot, ct);
                    query = query with { SnapshotSequence = snapshot };
                }
                var retained = queries.ReadAll(query, ct, out _, out var readIssues);
                var ids = selected?.Select(run => run.Id).ToHashSet();
                var selectedRuns = selection?.RunIds.ToHashSet();
                var selectedEvents = selection?.EventIds.ToHashSet();
                var entries = retained.Where(entry => ids is null || entry.Context.RunId is { } id && ids.Contains(id))
                    .Where(entry => selection is null || selectedEvents!.Contains(entry.Id) || entry.Context.RunId is { } runId && selectedRuns!.Contains(runId))
                    .Select(repository.Privacy.Sanitize).ToArray();
                issues.AddRange(readIssues);
                var runs = (selected ?? repository.ReadRuns().Where(run => entries.Any(entry => entry.Context.RunId == run.Id)
                    || query.RunId == run.Id || selectedRuns != null && selectedRuns.Contains(run.Id)).ToArray()).Select(repository.Privacy.Sanitize).ToArray();
                if (selectedEvents is not null && selectedEvents.Except(entries.Select(entry => entry.Id)).Any()) issues.Add("selection.events-unavailable");
                if (selectedRuns is not null && selectedRuns.Except(runs.Select(run => run.Id)).Any()) issues.Add("selection.runs-unavailable");
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
                        RunQuery = runQuery is null ? null : runQuery with { Search = repository.Privacy.Sanitize(runQuery.Search) },
                        RunIds = runs.Select(run => run.Id).ToArray(),
                        Selection = selection,
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
                    foreach (var run in runs)
                    {
                        report.WriteLine($"{run.Name} #{run.ExecutionNumber}: {run.Outcome}; {run.DurationMs} ms; errors={run.ErrorCount}; warnings={run.WarningCount}");
                        foreach (var summary in run.StepSummaries)
                            report.WriteLine($"  step={summary.StepId}; {summary.Code}; repetitions={summary.Count}; total={summary.TotalDurationMs} ms; lastIteration={summary.LastEvent.Iteration}; details=last observation");
                    }
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
