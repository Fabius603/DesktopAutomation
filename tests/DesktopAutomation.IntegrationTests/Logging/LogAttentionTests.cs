using System.IO.Compression;
using System.Text.Json;
using Common.JsonRepository;
using DesktopAutomation.Application.Logging;
using TaskAutomation.Logging;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Logging;

public sealed class LogAttentionTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
    private static LogRun Run(LogRepository repository) => repository.SaveRun(new LogRun
    {
        Id = Guid.NewGuid(),
        InstanceId = Guid.NewGuid(),
        SourceId = Guid.NewGuid(),
        Source = LogSource.Job,
        Name = "attention",
        StartedAt = Stamp,
        EndedAt = Stamp.AddSeconds(1),
        Outcome = LogOutcome.WithWarnings
    });
    private static LogEvent Problem(LogRepository repository, LogRun run, Guid? id = null, ExecutionLogLevel level = ExecutionLogLevel.Warning)
        => repository.Append(new LogEvent { Timestamp = Stamp, Level = level, Code = LogCodes.Message, Context = new(run.Id), ProblemId = id ?? Guid.NewGuid() });
    [Fact]
    public async Task SeenPersists_DuplicatesStaySeen_SeverityEscalationAndNewProblemsReopen()
    {
        using var directory = new TemporaryDirectory(); using var repository = new LogRepository(directory.Path);
        var run = Run(repository); var warning = Problem(repository, run); var path = Path.Combine(directory.Path, "read-state");
        using (var attention = new LogAttentionService(repository, path)) Assert.True(await attention.MarkSeenAsync(warning));
        using var reopened = new LogAttentionService(repository, path); var queries = new LogQueryService(repository, reopened);
        Assert.Equal(0, (await queries.OverviewAsync()).NewProblemRunCount);
        Assert.Equal(1, (await queries.OverviewAsync()).ProblemRunCount);
        Problem(repository, run, warning.ProblemId);
        Assert.Equal(0, (await queries.OverviewAsync()).NewProblemRunCount);
        var error = Problem(repository, run, warning.ProblemId, ExecutionLogLevel.Error);
        Assert.Equal(1, (await queries.OverviewAsync()).NewProblemRunCount);
        Assert.True(await reopened.MarkSeenAsync(warning));
        Assert.Equal(1, (await queries.OverviewAsync()).NewProblemRunCount);
        Assert.True(await reopened.MarkSeenAsync(error));
        Assert.Equal(0, (await queries.OverviewAsync()).NewProblemRunCount);
        Problem(repository, run);
        Assert.Equal(1, (await queries.OverviewAsync()).NewProblemRunCount);
        Assert.Equal(LogOutcome.WithErrors, repository.ReadRuns().Single().Outcome);
    }
    [Fact]
    public async Task ViewingOneProblem_DoesNotAcknowledgeOtherProblems_ResolveAndReopenAreExplicit()
    {
        using var directory = new TemporaryDirectory(); using var repository = new LogRepository(directory.Path);
        using var attention = new LogAttentionService(repository, Path.Combine(directory.Path, "read-state"));
        var run = Run(repository); var one = Problem(repository, run); var two = Problem(repository, run);
        await attention.MarkSeenAsync(one);
        var scope = await attention.ScopeAsync(new());
        Assert.Equal(1, scope.Count(problem => problem.State == LogAttentionState.New));
        var detail = await attention.DetailAsync(one, run.Id); Assert.Equal(LogAttentionState.Seen, detail.Selected!.State);
        Assert.Equal(two.Id, detail.Next!.EventId);
        Assert.Equal(two.Id, (await attention.NextNewAsync(run.Id))!.EventId);
        Assert.True(await attention.ChangeAsync([detail.Selected], LogAttentionState.Resolved));
        await attention.MarkSeenAsync(one);
        Assert.Equal(LogAttentionState.Resolved, (await attention.DetailAsync(one, run.Id)).Selected!.State);
        Assert.True(await attention.ChangeAsync([(await attention.DetailAsync(one, run.Id)).Selected!], LogAttentionState.New));
        Assert.Equal(LogAttentionState.New, (await attention.DetailAsync(one, run.Id)).Selected!.State);
        Assert.Equal(LogAttentionState.New, (await attention.DetailAsync(two, run.Id)).Selected!.State);
    }
    [Fact]
    public async Task CorrelatedSources_ShareAcknowledgement_SeparateRunsRemainIndependent()
    {
        using var directory = new TemporaryDirectory(); using var repository = new LogRepository(directory.Path);
        using var attention = new LogAttentionService(repository, Path.Combine(directory.Path, "read-state"));
        var one = Run(repository); var two = Run(repository); var identity = Guid.NewGuid();
        var job = Problem(repository, one, identity); var otherRun = Problem(repository, two, identity);
        var diagnostic = repository.Append(new LogEvent
        {
            Timestamp = Stamp,
            Source = LogSource.Application,
            Level = ExecutionLogLevel.Warning,
            Context = new(InstanceId: one.InstanceId),
            ProblemId = identity
        });
        Assert.True(await attention.MarkSeenAsync(diagnostic));
        Assert.Equal(LogAttentionState.Seen, (await attention.DetailAsync(job, one.Id)).Selected!.State);
        Assert.Equal(LogAttentionState.New, (await attention.DetailAsync(otherRun, two.Id)).Selected!.State);
        Assert.Equal(1, (await new LogQueryService(repository, attention).OverviewAsync()).NewProblemRunCount);
        Assert.Equal(2, (await attention.ScopeAsync(new())).Count);
    }
    [Fact]
    public async Task MissingEvidenceSummary_RequiresExplicitAcknowledgement_ChangedSummaryReopens()
    {
        using var directory = new TemporaryDirectory(); using var repository = new LogRepository(directory.Path);
        using var attention = new LogAttentionService(repository, Path.Combine(directory.Path, "read-state"));
        var run = repository.SaveRun(Run(repository) with { IsComplete = false });
        var warning = Problem(repository, run); await attention.MarkSeenAsync(warning);
        var queries = new LogQueryService(repository, attention);
        Assert.Equal(1, (await queries.OverviewAsync()).NewProblemRunCount);
        var summary = (await attention.DetailAsync(warning, run.Id)).Summary!;
        Assert.True(summary.IsSummary); Assert.Equal(LogAttentionState.New, summary.State);
        await attention.ChangeAsync([summary], LogAttentionState.Seen);
        Assert.Equal(0, (await queries.OverviewAsync()).NewProblemRunCount);
        repository.SaveRun(repository.ReadRuns().Single() with { Outcome = LogOutcome.Interrupted });
        Assert.Equal(1, (await queries.OverviewAsync()).NewProblemRunCount);
    }
    [Fact]
    public async Task BulkAcknowledgement_OnlyConfirmsCapturedCheckpoint_ConcurrentChangesAreNotLost()
    {
        using var directory = new TemporaryDirectory(); using var repository = new LogRepository(directory.Path);
        using var attention = new LogAttentionService(repository, Path.Combine(directory.Path, "read-state"));
        var one = Run(repository); var two = Run(repository); var first = Problem(repository, one); var second = Problem(repository, two);
        var checkpoint = repository.Query(new()).SnapshotSequence;
        var captured = await attention.ScopeAsync(new(SnapshotSequence: checkpoint));
        var late = Problem(repository, one);
        await Task.WhenAll(attention.ChangeAsync(captured, LogAttentionState.Seen), attention.MarkSeenAsync(second));
        Assert.Equal(LogAttentionState.Seen, (await attention.DetailAsync(first, one.Id)).Selected!.State);
        Assert.Equal(LogAttentionState.Seen, (await attention.DetailAsync(second, two.Id)).Selected!.State);
        Assert.Equal(LogAttentionState.New, (await attention.DetailAsync(late, one.Id)).Selected!.State);
        Assert.Equal(1, (await new LogQueryService(repository, attention).OverviewAsync()).NewProblemRunCount);
    }
    [Fact]
    public async Task UnwritableAcknowledgement_DoesNotPublishSeenState()
    {
        using var directory = new TemporaryDirectory(); using var repository = new LogRepository(directory.Path);
        var run = Run(repository); var warning = Problem(repository, run);
        var blocked = Path.Combine(directory.Path, "blocked"); await File.WriteAllTextAsync(blocked, "file, not a directory");
        using var attention = new LogAttentionService(repository, blocked);
        Assert.False(await attention.MarkSeenAsync(warning));
        Assert.Equal(LogAttentionState.New, (await attention.DetailAsync(warning, run.Id)).Selected!.State);
        Assert.True(attention.ReadFailed);
    }
    [Fact]
    public async Task CorruptAcknowledgement_IsExplicit_AndSuccessfulWriteRecovers()
    {
        using var directory = new TemporaryDirectory(); using var repository = new LogRepository(directory.Path);
        var run = Run(repository); var warning = Problem(repository, run); var path = Path.Combine(directory.Path, "read-state");
        Directory.CreateDirectory(path); await File.WriteAllTextAsync(JsonRepositoryPath.ForKey(path, "attention"), "invalid-json");
        using var attention = new LogAttentionService(repository, path);
        Assert.Equal(LogAttentionState.New, (await attention.DetailAsync(warning, run.Id)).Selected!.State);
        Assert.True(attention.ReadFailed);
        Assert.True(await attention.MarkSeenAsync(warning));
        Assert.False(attention.ReadFailed);
        Assert.Equal(LogAttentionState.Seen, (await attention.DetailAsync(warning, run.Id)).Selected!.State);
    }
    [Fact]
    public async Task NewProblemFilterAndExport_ShareMembership_QualityAndEvidenceRemainUnchanged()
    {
        using var directory = new TemporaryDirectory(); using var repository = new LogRepository(directory.Path);
        using var attention = new LogAttentionService(repository, Path.Combine(directory.Path, "read-state"));
        var seenRun = Run(repository); var unseenRun = Run(repository);
        var warning = Problem(repository, seenRun); Problem(repository, unseenRun); await attention.MarkSeenAsync(warning);
        var queries = new LogQueryService(repository, attention);
        var query = new RunQuery(OnlyNewProblems: true); var page = await queries.RunsAsync(query);
        Assert.Equal(unseenRun.Id, Assert.Single(page.Runs).Id);
        Assert.Equal(2, (await queries.RunsAsync(new(OnlyProblems: true))).Total);
        var zip = Path.Combine(directory.Path, "new.zip"); await new LogExportService(repository, queries).ExportRunsAsync(query, zip, "test");
        using var archive = ZipFile.OpenRead(zip); using var reader = new StreamReader(archive.GetEntry("runs.json")!.Open());
        var exported = JsonSerializer.Deserialize<LogRun[]>(await reader.ReadToEndAsync(), LogRepository.JsonOptions)!;
        Assert.Equal(unseenRun.Id, Assert.Single(exported).Id);
        Assert.DoesNotContain(archive.Entries, item => item.FullName.Contains("attention", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, repository.Query(new(MinimumLevel: ExecutionLogLevel.Warning)).MatchCount);
    }
}
