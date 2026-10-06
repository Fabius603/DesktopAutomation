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

    private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!, "..", "..", ".."));
}
