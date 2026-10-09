using TaskAutomation.Logging;
using TaskAutomation.Jobs;
namespace TaskAutomation.Tests.Logging;

public sealed class ExecutionLogLookupTests
{
    [Fact]
    public void CaptureAndCompletion_WorkWhenCatalogListingIsUnavailable()
    {
        using var directory = new TaskAutomation.Tests.TestDoubles.TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new ExecutionLogService(new CaptureRepository(repository));
        var job = new Job { Name = "lookup" };
        var run = logs.BeginJob(job.Id, job.Name);
        logs.InitializeRun(run, job, Guid.NewGuid());
        logs.Write(run, ExecutionLogLevel.Information, "observed");
        logs.Complete(run, true);
        Assert.Equal(LogOutcome.Successful, repository.GetRun(run.Id)!.Outcome);
        Assert.Contains(repository.Query(new(RunId: run.Id)).Entries, entry => entry.Message == "observed");
    }

    private sealed class CaptureRepository(LogRepository inner) : ILogRepository
    {
        public event EventHandler<LogEvent>? EntryWritten { add => inner.EntryWritten += value; remove => inner.EntryWritten -= value; }
        public event EventHandler<LogRun>? RunChanged { add => inner.RunChanged += value; remove => inner.RunChanged -= value; }
        public string DirectoryPath => inner.DirectoryPath;
        public LogPrivacy Privacy => inner.Privacy;
        public LogEvent Append(LogEvent entry) => inner.Append(entry);
        public LogRun SaveRun(LogRun run) => inner.SaveRun(run);
        public long NextExecutionNumber(Guid sourceId) => inner.NextExecutionNumber(sourceId);
        public LogRun? GetRun(Guid id) => inner.GetRun(id);
        public IReadOnlyList<LogRun> ReadRuns() => throw new InvalidOperationException("Catalog listing is unavailable.");
        public LogPage Query(LogQuery query, CancellationToken ct = default) => inner.Query(query, ct);
        public Task FlushAsync(CancellationToken ct = default) => inner.FlushAsync(ct);
    }
}
