using System.Text.Json;
using DesktopAutomation.Application.Logging;
using TaskAutomation.Logging;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Logging;

public sealed class LogRetentionTests
{
    [Fact]
    public async Task HourlyCleanup_ExpiresIdleFilesAndRuns_PreservesCountersAndAcknowledgementsForRetainedRuns()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        using var attention = new LogAttentionService(repository, Path.Combine(directory.Path, "attention"));
        var run = Completed(repository, clock.GetUtcNow());
        var warning = repository.Append(new LogEvent { Level = ExecutionLogLevel.Warning, Context = new(run.Id) });
        await repository.FlushAsync();
        Assert.True(await attention.MarkSeenAsync(warning));
        AgeSegments(directory.Path, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromDays(31));
        await repository.FlushAsync();
        Assert.Empty(repository.ReadRuns());
        Assert.Empty(Directory.GetFiles(directory.Path, "*.events.jsonl"));
        var state = JsonSerializer.Deserialize<LogAttentionDocument>(File.ReadAllText(Path.Combine(directory.Path, "attention", "attention.json")), LogRepository.JsonOptions)!;
        Assert.Empty(state.Entries);
        Assert.Equal(2, repository.NextExecutionNumber(run.SourceId));
        using var restarted = new LogRepository(directory.Path, clock);
        Assert.Equal(2, restarted.NextExecutionNumber(run.SourceId));
    }

    [Fact]
    public async Task HourlyCleanup_KeepsBoundary_ThenExpiresWithoutNewEvents()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        repository.Append(new LogEvent());
        await repository.FlushAsync();
        AgeSegments(directory.Path, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromDays(30));
        await repository.FlushAsync();
        Assert.Single(Directory.GetFiles(directory.Path, "*.events.jsonl"));
        clock.Advance(TimeSpan.FromMinutes(59));
        await repository.FlushAsync();
        Assert.Single(Directory.GetFiles(directory.Path, "*.events.jsonl"));
        clock.Advance(TimeSpan.FromMinutes(1));
        await repository.FlushAsync();
        Assert.Empty(Directory.GetFiles(directory.Path, "*.events.jsonl"));
        repository.Append(new LogEvent { Message = "after cleanup" });
        await repository.FlushAsync();
        Assert.Equal("after cleanup", Assert.Single(repository.QueryAll(new()).Entries).Message);
    }

    [Fact]
    public async Task ActiveSharedSegment_IsProtected_ThenCleanupRunsImmediatelyOnCompletion()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        var completed = Completed(repository, clock.GetUtcNow());
        var active = repository.SaveRun(new LogRun { Id = Guid.NewGuid(), StartedAt = clock.GetUtcNow() });
        repository.Append(new LogEvent { Context = new(completed.Id) });
        repository.Append(new LogEvent { Context = new(active.Id) });
        await repository.FlushAsync();
        AgeSegments(directory.Path, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromDays(31));
        await repository.FlushAsync();
        Assert.Single(Directory.GetFiles(directory.Path, "*.events.jsonl"));
        Assert.Equal(2, repository.ReadRuns().Count);
        Assert.All(repository.ReadRuns(), run => Assert.True(run.IsComplete));
        repository.SaveRun(active with { EndedAt = clock.GetUtcNow(), Outcome = LogOutcome.Successful });
        await repository.FlushAsync();
        Assert.Empty(Directory.GetFiles(directory.Path, "*.events.jsonl"));
        Assert.False(Assert.Single(repository.ReadRuns()).IsComplete);
        using var restarted = new LogRepository(directory.Path, clock);
        Assert.False(Assert.Single(restarted.ReadRuns()).IsComplete);
        Assert.Contains("run.incomplete", restarted.Query(new(RunId: active.Id)).Issues);
    }

    [Fact]
    public async Task LockedSegment_ReportsFailureWithoutLosingNewWrites_AndRetriesNextHour()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        var run = Completed(repository, clock.GetUtcNow());
        repository.Append(new LogEvent { Context = new(run.Id) });
        await repository.FlushAsync();
        AgeSegments(directory.Path, clock.GetUtcNow());
        var path = Assert.Single(Directory.GetFiles(directory.Path, "*.events.jsonl"));
        // Rename the segment so unrelated new writes can use a fresh file.
        var lockedPath = Path.Combine(directory.Path, "locked.events.jsonl");
        File.Move(path, lockedPath);
        using (var locked = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            clock.Advance(TimeSpan.FromDays(31));
            repository.Append(new LogEvent { Message = "new event" });
            await repository.FlushAsync();
            Assert.Single(repository.ReadRuns());
            Assert.Contains("storage.retention-failed", repository.QueryAll(new()).Issues);
            Assert.Contains(repository.QueryAll(new()).Entries, entry => entry.Message == "new event");
            Assert.Equal(0, repository.ReadRuns()[0].LostEntries);
        }
        clock.Advance(TimeSpan.FromHours(1));
        await repository.FlushAsync();
        Assert.False(File.Exists(lockedPath));
        Assert.Empty(repository.ReadRuns());
        Assert.DoesNotContain("storage.retention-failed", repository.QueryAll(new()).Issues);
    }

    [Fact]
    public async Task QuotaCleanup_DeletesOldestFirst_AndAccountsForActualRemainingBytes()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        var oldest = Path.Combine(directory.Path, "oldest.events.jsonl");
        var middle = Path.Combine(directory.Path, "middle.events.jsonl");
        File.WriteAllText(oldest, JsonSerializer.Serialize(new LogEvent { Details = new string('a', 2 * 1024 * 1024) }, LogRepository.JsonOptions) + "\n");
        File.WriteAllText(middle, JsonSerializer.Serialize(new LogEvent { Details = new string('b', 512 * 1024) }, LogRepository.JsonOptions) + "\n");
        File.SetLastWriteTimeUtc(oldest, clock.GetUtcNow().AddHours(-2).UtcDateTime);
        File.SetLastWriteTimeUtc(middle, clock.GetUtcNow().AddHours(-1).UtcDateTime);
        var newest = Path.Combine(directory.Path, "newest.events.jsonl");
        // A sparse-size fixture tests the real 500 MiB default without materializing its contents.
        using (var fixture = new FileStream(newest, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            fixture.SetLength(499L * 1024 * 1024);
        File.SetLastWriteTimeUtc(newest, clock.GetUtcNow().UtcDateTime);
        using var locked = new FileStream(newest, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        clock.Advance(TimeSpan.FromHours(1));
        await repository.FlushAsync();
        Assert.False(File.Exists(oldest));
        Assert.True(File.Exists(middle));
        Assert.True(File.Exists(newest));
    }

    [Fact]
    public async Task CorruptSegmentAndLegacyFiles_ArePreserved_AndTimerStopsWithRepository()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        var corrupt = Path.Combine(directory.Path, "broken.events.jsonl");
        var legacy = Path.Combine(directory.Path, "legacy.log");
        using (var repository = new LogRepository(directory.Path, clock))
        {
            File.WriteAllText(corrupt, "{broken\n");
            File.WriteAllText(legacy, "historical log");
            File.SetLastWriteTimeUtc(corrupt, clock.GetUtcNow().AddDays(-31).UtcDateTime);
            clock.Advance(TimeSpan.FromHours(1));
            await repository.FlushAsync();
            Assert.True(File.Exists(corrupt));
            Assert.Contains("storage.retention-failed", repository.QueryAll(new()).Issues);
        }
        Assert.True(clock.AllTimersDisposed);
        clock.Advance(TimeSpan.FromDays(31));
        Assert.True(File.Exists(corrupt));
        Assert.Equal("historical log", File.ReadAllText(legacy));
    }

    [Fact]
    public async Task StartupPruning_RemovesOnlyAcknowledgementsOfExpiredRuns()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        var run = Completed(repository, clock.GetUtcNow());
        var warning = repository.Append(new LogEvent { Level = ExecutionLogLevel.Warning, Context = new(run.Id) });
        await repository.FlushAsync();
        var path = Path.Combine(directory.Path, "attention");
        Directory.CreateDirectory(path);
        var expired = Guid.NewGuid();
        File.WriteAllText(Path.Combine(path, "attention.json"), JsonSerializer.Serialize(new LogAttentionDocument
        {
            Entries = new()
            {
                ["expired"] = new(expired, LogAttentionState.Seen, ExecutionLogLevel.Warning, 1, ""),
                [$"{run.Id:D}:event:{warning.Id:D}"] = new(run.Id, LogAttentionState.Seen, ExecutionLogLevel.Warning, warning.Sequence, "")
            }
        }));
        using var attention = new LogAttentionService(repository, path);
        Assert.Equal(LogAttentionState.Seen, (await attention.DetailAsync(warning, run.Id)).Selected!.State);
        var state = JsonSerializer.Deserialize<LogAttentionDocument>(File.ReadAllText(Path.Combine(path, "attention.json")), LogRepository.JsonOptions)!;
        Assert.Equal(run.Id, Assert.Single(state.Entries).Value.RunId);
    }

    [Fact]
    public async Task AcknowledgementCleanupFailure_PreservesState_AndRetriesWithoutFurtherLogChanges()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        var attentionPath = Path.Combine(directory.Path, "attention");
        using var attention = new LogAttentionService(repository, attentionPath);
        var run = Completed(repository, clock.GetUtcNow());
        var warning = repository.Append(new LogEvent { Level = ExecutionLogLevel.Warning, Context = new(run.Id) });
        await repository.FlushAsync();
        Assert.True(await attention.MarkSeenAsync(warning));
        AgeSegments(directory.Path, clock.GetUtcNow());
        var statePath = Path.Combine(attentionPath, "attention.json");
        using (var locked = new FileStream(statePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            clock.Advance(TimeSpan.FromDays(31));
            await repository.FlushAsync();
            Assert.Empty(repository.ReadRuns());
            Assert.True(attention.CleanupFailed);
            Assert.Contains(run.Id.ToString(), File.ReadAllText(statePath));
        }
        clock.Advance(TimeSpan.FromHours(1));
        await repository.FlushAsync();
        Assert.False(attention.CleanupFailed);
        var document = JsonSerializer.Deserialize<LogAttentionDocument>(File.ReadAllText(statePath), LogRepository.JsonOptions)!;
        Assert.Empty(document.Entries);
    }

    [Fact]
    public async Task CleanupWithConcurrentCaptureAndReads_PreservesAllActiveEvidence()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        var run = repository.SaveRun(new LogRun { Id = Guid.NewGuid(), StartedAt = clock.GetUtcNow() });
        repository.Append(new LogEvent { Context = new(run.Id) });
        await repository.FlushAsync();
        AgeSegments(directory.Path, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromDays(31));
        await Task.WhenAll(Task.Run(() =>
        {
            for (var index = 0; index < 1000; index++) repository.Append(new LogEvent { Context = new(run.Id) });
        }), Task.Run(() =>
        {
            for (var index = 0; index < 10; index++) repository.QueryAll(new(RunId: run.Id));
        }));
        await repository.FlushAsync();
        var page = repository.QueryAll(new(RunId: run.Id));
        Assert.Equal(1001, page.Entries.Count);
        Assert.Equal(1001, page.Entries.Select(entry => entry.Id).Distinct().Count());
        Assert.True(Assert.Single(repository.ReadRuns()).IsComplete);
        Assert.Equal(0, repository.ReadRuns()[0].LostEntries);
    }

    [Fact]
    public async Task DailyRotation_PreventsRoutineNewEventsFromExtendingOldEvidenceIndefinitely()
    {
        using var directory = new TemporaryDirectory();
        var clock = new RetentionClock();
        using var repository = new LogRepository(directory.Path, clock);
        var old = repository.Append(new LogEvent { Message = "old" });
        await repository.FlushAsync();
        clock.Advance(TimeSpan.FromDays(29));
        var recent = repository.Append(new LogEvent { Message = "recent" });
        await repository.FlushAsync();
        Assert.Equal(2, Directory.GetFiles(directory.Path, "*.events.jsonl").Length);
        Assert.Contains(repository.QueryAll(new()).Entries, entry => entry.Id == old.Id);
        clock.Advance(TimeSpan.FromDays(2));
        await repository.FlushAsync();
        Assert.Single(Directory.GetFiles(directory.Path, "*.events.jsonl"));
        Assert.Equal(recent.Id, Assert.Single(repository.QueryAll(new()).Entries).Id);
    }

    private static LogRun Completed(LogRepository repository, DateTimeOffset stamp)
    {
        var source = Guid.NewGuid();
        return repository.SaveRun(new LogRun
        {
            Id = Guid.NewGuid(),
            SourceId = source,
            StartedAt = stamp,
            EndedAt = stamp,
            ExecutionNumber = repository.NextExecutionNumber(source),
            Outcome = LogOutcome.Successful
        });
    }
    private static void AgeSegments(string path, DateTimeOffset stamp)
    {
        foreach (var segment in Directory.GetFiles(path, "*.events.jsonl")) File.SetLastWriteTimeUtc(segment, stamp.UtcDateTime);
    }

}
