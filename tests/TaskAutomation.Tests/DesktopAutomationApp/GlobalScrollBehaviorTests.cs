using System.Runtime.CompilerServices;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class GlobalScrollBehaviorTests
{
    [Fact]
    public void JobDebugger_DoesNotOverrideTheGlobalMouseWheelBehavior()
    {
        var repositoryRoot = RepositoryRoot();
        var viewDirectory = Path.Combine(
            repositoryRoot, "DesktopAutomationApp", "Views", "JobsView");

        var xaml = File.ReadAllText(Path.Combine(viewDirectory, "JobStepsView.xaml"));
        var codeBehind = File.ReadAllText(Path.Combine(viewDirectory, "JobStepsView.xaml.cs"));

        Assert.DoesNotContain("PreviewMouseWheel=", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DebugTree_PreviewMouseWheel", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void JobLogLists_UsePixelScrolling()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "LogsView", "ExecutionLogsView.xaml"));

        Assert.Equal(2, CountOccurrences(
            xaml, "VirtualizingStackPanel.ScrollUnit=\"Pixel\""));
    }

    [Theory]
    [InlineData("ExecutionLogsView.xaml")]
    [InlineData("AutomationLogsView.xaml")]
    [InlineData("ApplicationLogsView.xaml")]
    public void LogPages_FollowTheLatestEntry(string fileName)
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "LogsView", fileName));

        Assert.Contains("LogTailFollowBehavior.IsEnabled=\"True\"", xaml, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        for (var index = 0; (index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0;
             index += search.Length)
            count++;
        return count;
    }

    private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!, "..", "..", ".."));
}
