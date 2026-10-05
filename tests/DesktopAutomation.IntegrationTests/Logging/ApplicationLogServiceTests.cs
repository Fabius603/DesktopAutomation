using Serilog.Events;
using Serilog.Parsing;
using TaskAutomation.Logging;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Logging;

public sealed class ApplicationLogServiceTests
{
    [Fact]
    public async Task RunMessages_KeepMetadataButOmitArbitraryInputsAndObjects()
    {
        const string privateValue = "unregistered-private-output-431";
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var service = new ApplicationLogService(repository);
        using (LogAmbient.Push(new(RunId: Guid.NewGuid(), StepExecutionId: Guid.NewGuid())))
            service.Emit(new Serilog.Events.LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error,
                new InvalidOperationException(privateValue),
                new MessageTemplateParser().Parse("Read {Text} {Arguments} {Payload} at {Path} ({Count})"),
                [new("Text", new ScalarValue(privateValue)), new("Arguments", new ScalarValue(privateValue)),
                 new("Payload", new SequenceValue([new ScalarValue(privateValue)])),
                 new("Path", new ScalarValue("C:/approved/output.txt")), new("Count", new ScalarValue(7))]));
        await repository.FlushAsync();
        var entry = Assert.Single(service.ReadEntries());
        Assert.DoesNotContain(privateValue, entry.Message + entry.Details);
        Assert.Contains("C:/approved/output.txt", entry.Message);
        Assert.Contains("7", entry.Message);
        using var reloaded = new LogRepository(directory.Path);
        Assert.DoesNotContain(privateValue, Assert.Single(reloaded.Query(new()).Entries).Message);
    }

    [Fact]
    public async Task StructuredEntries_PreserveMultilineDetailsAndIgnoreHistoricalText()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "desktop-automation-old.log"), "2026-01-01 [ERR] historical");
        using var repository = new LogRepository(directory.Path);
        using var service = new ApplicationLogService(repository);
        Assert.Empty(service.ReadEntries());
        var run = Guid.NewGuid();
        using (LogAmbient.Push(new(run, run, StepId: "s1", StepExecutionId: Guid.NewGuid())))
            service.Emit(new Serilog.Events.LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error,
                new DirectoryNotFoundException("missing folder"), new MessageTemplateParser().Parse("Could not save"), []));
        await repository.FlushAsync();
        var entry = Assert.Single(service.ReadEntries());
        Assert.Contains("DirectoryNotFoundException", entry.Details);
        var structured = Assert.Single(repository.Query(new(Source: LogSource.Application)).Entries);
        Assert.Equal(run, structured.Context.RunId);
        Assert.Equal("s1", structured.Context.StepId);
        Assert.Equal(LogCodes.FileUnavailable, structured.DiagnosticCode);
    }

    [Fact]
    public async Task ReloadAndLiveEventsAtSameTimestampRemainDistinct()
    {
        using var directory = new TemporaryDirectory();
        var timestamp = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        using (var repository = new LogRepository(directory.Path))
        {
            repository.Append(new TaskAutomation.Logging.LogEvent { Timestamp = timestamp, Source = LogSource.Application, Message = "first" });
            await repository.FlushAsync();
        }
        using var reloaded = new LogRepository(directory.Path);
        using var service = new ApplicationLogService(reloaded);
        service.Emit(new Serilog.Events.LogEvent(timestamp, LogEventLevel.Information, null,
            new MessageTemplateParser().Parse("second"), []));
        Assert.Equal(new[] { "first", "second" }, service.ReadEntries().Select(entry => entry.Message));
    }
}
