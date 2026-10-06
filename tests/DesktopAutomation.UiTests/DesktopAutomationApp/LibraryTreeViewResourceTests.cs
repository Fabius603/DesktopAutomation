namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class LibraryTreeViewResourceTests
{
    [Fact]
    public void LibraryList_UsesTheSharedVerticalPixelScroller()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "Library", "LibraryTreeView.xaml"));

        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"", xaml);
        Assert.Contains("VirtualizingPanel.ScrollUnit=\"Pixel\"", xaml);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
