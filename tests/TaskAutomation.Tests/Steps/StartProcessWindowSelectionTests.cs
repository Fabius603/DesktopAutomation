using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class StartProcessWindowSelectionTests
{
    [Fact]
    public void SelectWindowCandidate_PrefersNewWindowOwnedByStartedProcess()
    {
        var selected = StartProcessStepHandler.SelectWindowCandidate(
        [
            Candidate(1, 10, 100, wasPresentBefore: true),
            Candidate(2, 20, 50, wasPresentBefore: false)
        ], launchedProcessId: 20, allowExistingWindow: false);

        Assert.Equal(new IntPtr(2), selected);
    }

    [Fact]
    public void SelectWindowCandidate_AllowsForegroundWindowAfterLauncherDelegatesAndExits()
    {
        var selected = StartProcessStepHandler.SelectWindowCandidate(
        [
            Candidate(1, 10, 500, wasPresentBefore: true),
            Candidate(2, 11, 100, wasPresentBefore: true, isForeground: true)
        ], launchedProcessId: 20, allowExistingWindow: true);

        Assert.Equal(new IntPtr(2), selected);
    }

    [Fact]
    public void SelectWindowCandidate_DoesNotMoveUnrelatedExistingWindowDuringInitialSearch()
    {
        var selected = StartProcessStepHandler.SelectWindowCandidate(
            [Candidate(1, 10, 500, wasPresentBefore: true)],
            launchedProcessId: 20,
            allowExistingWindow: false);

        Assert.Equal(IntPtr.Zero, selected);
    }

    private static StartProcessStepHandler.WindowSearchCandidate Candidate(
        long handle,
        uint processId,
        long area,
        bool wasPresentBefore,
        bool isForeground = false) =>
        new(new IntPtr(handle), processId, area, wasPresentBefore, isForeground);
}
