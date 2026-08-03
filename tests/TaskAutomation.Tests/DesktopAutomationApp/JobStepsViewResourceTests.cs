namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepsViewResourceTests
{
    [Fact]
    public void JobStepsView_RegistersConvertersPreviouslyProvidedByLegacyTemplates()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));

        Assert.Contains("<conv:StepNumberConverter x:Key=\"StepNumberConverter\"/>", xaml);
        Assert.Contains("<conv:StepDisplayNameConverter x:Key=\"StepDisplayNameConverter\"/>", xaml);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DesktopAutomation.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
