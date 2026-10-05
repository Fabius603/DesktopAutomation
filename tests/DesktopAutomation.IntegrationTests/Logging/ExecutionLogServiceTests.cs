using TaskAutomation.Logging;
using TaskAutomation.Orchestration;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Logging;

public sealed class ExecutionLogServiceTests
{
    [Fact]
    public void BeginWriteComplete_TracksSessionEntriesDurationAndStatus()
    {
        using var directory = new TemporaryDirectory();
        using var service = new ExecutionLogService(directory.Path);
        var jobId = Guid.NewGuid();
        var session = service.BeginJob(jobId, "job", new JobStartContext(JobStartSource.Manual));
        service.Write(session, ExecutionLogLevel.Information, "step", "details", "s1", "TimeoutStep", 12,
            JobExecutionState.RunningSteps);
        service.Complete(session, success: true);
        Assert.Equal(jobId, session.SourceId);
        Assert.False(session.IsRunning);
        Assert.NotNull(session.DurationMs);
        Assert.Equal(JobExecutionState.RunningSteps, session.JobState);
        var entries = service.ReadEntries(session.Id);
        Assert.Contains(entries, entry => entry.Message == "step" && entry.StepId == "s1" && entry.DurationMs == 12);
        Assert.Equal("Ausführung beendet.", entries.Last().Message);
    }

    [Theory]
    [InlineData(false, false, ExecutionLogLevel.Error, "fehlerhaft")]
    [InlineData(false, true, ExecutionLogLevel.Information, "gestoppt")]
    public void Complete_DistinguishesFailureAndCancellation(bool success, bool cancelled,
        ExecutionLogLevel expectedLevel, string messagePart)
    {
        using var directory = new TemporaryDirectory();
        using var service = new ExecutionLogService(directory.Path);
        var session = service.BeginJob(Guid.NewGuid(), "job");
        service.Complete(session, success, cancelled: cancelled);
        var last = service.ReadEntries(session.Id).Last();
        Assert.Equal(expectedLevel, last.Level);
        Assert.Contains(messagePart, last.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConcurrentWrites_AreAllAvailableInMemoryAndPersistedAfterDispose()
    {
        using var directory = new TemporaryDirectory();
        Guid sessionId;
        using (var service = new ExecutionLogService(directory.Path))
        {
            var session = service.BeginJob(Guid.NewGuid(), "parallel");
            sessionId = session.Id;
            await Task.WhenAll(Enumerable.Range(0, 200).Select(index => Task.Run(() =>
                service.Write(session, ExecutionLogLevel.Debug, $"entry-{index}"))));
            Assert.Equal(200, service.ReadEntries(session.Id).Count(entry => entry.Message.StartsWith("entry-")));
            service.Complete(session, true);
        }
        using var reloaded = new ExecutionLogService(directory.Path);
        reloaded.ReloadSessions();
        var sessionReloaded = Assert.Single(reloaded.Sessions, session => session.Id == sessionId);
        Assert.Contains(reloaded.ReadEntries(sessionReloaded.Id), entry => entry.Message == "entry-199");
    }

    [Fact]
    public async Task ReloadSessions_OnlyDiscoversNewSchemaAndPagesRuns()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "20260101-120000-000_old.log"), "historical");
        using (var repository = new LogRepository(directory.Path))
        {
            for (var index = 0; index < 55; index++)
                repository.SaveRun(new LogRun
                {
                    Id = Guid.NewGuid(),
                    Source = LogSource.Job,
                    Name = $"job-{index:D2}",
                    StartedAt = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero).AddMinutes(index),
                    EndedAt = new DateTimeOffset(2026, 10, 5, 13, 0, 0, TimeSpan.Zero),
                    Outcome = LogOutcome.Successful
                });
            await repository.FlushAsync();
        }
        using var service = new ExecutionLogService(directory.Path);
        service.ReloadSessions(50);
        Assert.Equal(50, service.Sessions.Count);
        Assert.True(service.HasMoreSessions);
        Assert.Equal("job-54", service.Sessions[0].Name);
        service.ReloadSessions(100);
        Assert.Equal(55, service.Sessions.Count);
        Assert.False(service.HasMoreSessions);
    }

    [Fact]
    public void ReadEntries_RespectsMaximumAndUnknownSessionIsEmpty()
    {
        using var directory = new TemporaryDirectory();
        using var service = new ExecutionLogService(directory.Path);
        var session = service.BeginJob(Guid.NewGuid(), "tail");
        for (var i = 0; i < 10; i++) service.Write(session, ExecutionLogLevel.Debug, i.ToString());
        Assert.Equal(["7", "8", "9"], service.ReadEntries(session.Id, 3).Select(entry => entry.Message));
        Assert.Empty(service.ReadEntries(Guid.NewGuid()));
    }

    [Fact]
    public async Task ReadEntriesAsync_HonorsPreCancelledToken()
    {
        using var directory = new TemporaryDirectory();
        using var service = new ExecutionLogService(directory.Path);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReadEntriesAsync(Guid.NewGuid(), cancellationToken: cts.Token));
    }
}
