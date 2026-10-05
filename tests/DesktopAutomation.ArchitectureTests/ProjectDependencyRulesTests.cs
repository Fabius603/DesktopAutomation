using System.Xml.Linq;

namespace DesktopAutomation.ArchitectureTests;

public sealed class ProjectDependencyRulesTests
{
    [Fact]
    public void LoggingSchemaAndRules_LiveBelowPresentation()
    {
        Assert.Equal("TaskAutomation.Contracts", typeof(TaskAutomation.Logging.LogEvent).Assembly.GetName().Name);
        Assert.Equal("TaskAutomation", typeof(TaskAutomation.Logging.LogOutcomeRules).Assembly.GetName().Name);
        Assert.Equal("TaskAutomation", typeof(TaskAutomation.Logging.ApplicationLogService).Assembly.GetName().Name);
        Assert.Equal("DesktopAutomation.Application", typeof(DesktopAutomation.Application.Logging.LogQueryService).Assembly.GetName().Name);
    }
    [Fact]
    public void SharedProjects_DoNotReferencePresentationProject()
    {
        var root = FindRepositoryRoot();
        var sharedProjects = new[]
        {
            "TaskAutomation.Contracts/TaskAutomation.Contracts.csproj",
            "TaskAutomation/TaskAutomation.csproj",
            "DesktopAutomation.Application/DesktopAutomation.Application.csproj"
        };

        foreach (var relativePath in sharedProjects)
        {
            var references = ProjectReferences(Path.Combine(root, relativePath));
            Assert.DoesNotContain(references,
                reference => reference.EndsWith("DesktopAutomationApp.csproj", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void SharedContracts_DoNotEnableDesktopUiFrameworks()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(root,
            "TaskAutomation.Contracts", "TaskAutomation.Contracts.csproj"));
        var properties = document.Descendants().Where(element =>
            element.Name.LocalName is "UseWPF" or "UseWindowsForms");

        Assert.Empty(properties);
    }

    [Fact]
    public void TaskAutomation_DoesNotEnableWpf()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(root,
            "TaskAutomation", "TaskAutomation.csproj"));

        Assert.DoesNotContain(document.Descendants(), element =>
            element.Name.LocalName == "UseWPF"
            && string.Equals(element.Value, "true", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PresentationProject_DeclaresWpfAsPrimaryUiFramework()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(root,
            "DesktopAutomationApp", "DesktopAutomationApp.csproj"));

        Assert.Equal("true", document.Descendants()
            .Single(element => element.Name.LocalName == "UseWPF").Value,
            ignoreCase: true);
    }

    [Fact]
    public void CoreProjects_ReferenceOnlyDocumentedLowerLayers()
    {
        var root = FindRepositoryRoot();
        var allowedReferences = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["TaskAutomation.Contracts/TaskAutomation.Contracts.csproj"] = [],
            ["Common.JsonRepository/Common.JsonRepository.csproj"] = [],
            ["TaskAutomation/TaskAutomation.csproj"] =
            [
                "TaskAutomation.Contracts.csproj",
                "Common.JsonRepository.csproj",
                "Common.Logging.csproj",
                "ImageCapture.csproj",
                "ImageDetection.csproj",
                "ImageHelperMethods.csproj"
            ],
            ["DesktopAutomation.Application/DesktopAutomation.Application.csproj"] =
            [
                "Common.JsonRepository.csproj",
                "TaskAutomation.csproj"
            ]
        };

        foreach (var (project, allowed) in allowedReferences)
        {
            var actual = ProjectReferences(Path.Combine(root, project))
                .Select(Path.GetFileName)
                .Order(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(allowed.Order(StringComparer.OrdinalIgnoreCase), actual,
                StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TestProjects_DoNotCompileProductionSourcesAsLinkedFiles()
    {
        var root = FindRepositoryRoot();
        var testProjects = Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.csproj",
            SearchOption.AllDirectories);

        foreach (var project in testProjects)
        {
            var linkedProductionSources = XDocument.Load(project).Descendants()
                .Where(element => element.Name.LocalName == "Compile")
                .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
                .Where(include => include.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    && include.Contains("..\\..\\", StringComparison.Ordinal)
                    && !include.Contains("TestInfrastructure", StringComparison.OrdinalIgnoreCase));

            Assert.Empty(linkedProductionSources);
        }
    }

    private static IReadOnlyList<string> ProjectReferences(string projectPath) =>
        XDocument.Load(projectPath).Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .ToArray();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DesktopAutomation.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("DesktopAutomation.sln was not found above the test output directory.");
    }
}
