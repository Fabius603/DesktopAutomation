using DesktopAutomationApp.Logging;
using TaskAutomation.Logging;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Logging;

public sealed class ApplicationLogServiceTests
{
    [Fact]
    public void ReadEntries_ReturnsOnlyNewestRequestedEntriesAcrossRolledFiles()
    {
        using var directory = new TemporaryDirectory();
        WriteLog(directory.Path, "desktop-automation-20260101.log", 0, 4_000);
        WriteLog(directory.Path, "desktop-automation-20260102.log", 4_000, 2_000);
        var service = new ApplicationLogService(directory.Path, new LogFileStorageService());

        var entries = service.ReadEntries(5_000);

        Assert.Equal(5_000, entries.Count);
        Assert.Equal("entry-1000", entries[0].Message);
        Assert.Equal("entry-5999", entries[^1].Message);
    }

    [Fact]
    public void ReadEntries_PreservesMultilineExceptionDetailsFromTheTail()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "desktop-automation-20260101.log");
        File.WriteAllLines(path,
        [
            "2026-01-01 10:00:00.000 [ERR] Source Failure",
            "System.InvalidOperationException: broken",
            "   at Example.Run()",
            "2026-01-01 10:00:01.000 [INF] Source Recovered"
        ]);
        var service = new ApplicationLogService(directory.Path, new LogFileStorageService());

        var entries = service.ReadEntries();

        Assert.Equal(2, entries.Count);
        Assert.Contains("InvalidOperationException", entries[0].Details);
        Assert.Contains("Example.Run", entries[0].Details);
    }

    private static void WriteLog(string directory, string fileName, int start, int count)
    {
        var firstTimestamp = new DateTime(2026, 1, 1, 0, 0, 0);
        File.WriteAllLines(Path.Combine(directory, fileName), Enumerable.Range(start, count).Select(index =>
            $"{firstTimestamp.AddSeconds(index):yyyy-MM-dd HH:mm:ss.fff} [INF] Source entry-{index}"));
    }
}
