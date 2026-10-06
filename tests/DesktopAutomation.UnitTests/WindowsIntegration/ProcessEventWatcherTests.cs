using System.Management;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.WindowsIntegration;

public sealed class ProcessEventWatcherTests
{
    [Theory]
    [InlineData(true, "Creation")]
    [InlineData(false, "Deletion")]
    public void DeniedTrace_RegistersEquivalentIntrinsicEvent(bool started, string eventKind)
    {
        var fallbackReported = false;
        var result = ProcessEventWatcher.StartWithFallback(started, query =>
        {
            if (query.Contains("Trace")) throw new UnauthorizedAccessException();
            return query;
        }, () => fallbackReported = true);
        Assert.True(fallbackReported);
        Assert.Contains($"__Instance{eventKind}Event", result);
        Assert.Contains("TargetInstance ISA 'Win32_Process'", result);
    }

    [Fact]
    public void UnavailableWmi_RemainsAFailureInsteadOfReportingSuccessfulFallback()
    {
        var fallbackReported = false;
        Assert.Throws<ManagementException>(() => ProcessEventWatcher.StartWithFallback<object>(true,
            _ => throw new ManagementException("WMI unavailable"), () => fallbackReported = true));
        Assert.False(fallbackReported);
        Assert.Throws<InvalidOperationException>(() => ProcessEventWatcher.StartWithFallback<object>(false, query =>
        {
            if (query.Contains("Trace")) throw new UnauthorizedAccessException();
            throw new InvalidOperationException("Fallback unavailable");
        }, () => fallbackReported = true));
        Assert.False(fallbackReported);
    }
}
