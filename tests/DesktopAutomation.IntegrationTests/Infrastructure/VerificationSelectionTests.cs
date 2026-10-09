using System.Diagnostics;
using System.Text.Json;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Infrastructure;

public sealed class VerificationSelectionTests
{
    [Fact]
    public async Task Focused_RunIncludesPrerequisitesOnceAndForwardsFilterOnlyToTests()
    {
        using var fixture = new VerificationFixture();
        var result = await fixture.Run("-Mode", "Focused", "-Checks", "selected,restore,selected",
            "-TestFilter", "FullyQualifiedName~Example&Category!=Slow");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Equal(["restore:", "build:", "selected:FullyQualifiedName~Example&Category!=Slow"], fixture.Calls());
        using var summary = JsonDocument.Parse(File.ReadAllText(fixture.SummaryPath));
        Assert.Equal("Focused", summary.RootElement.GetProperty("mode").GetString());
        Assert.Equal(3, summary.RootElement.GetProperty("checks").GetArrayLength());
        Assert.Equal("FullyQualifiedName~Example&Category!=Slow", summary.RootElement.GetProperty("testFilter").GetString());
    }

    [Fact]
    public async Task Focused_StaticCheckDoesNotRunBuildOrTests()
    {
        using var fixture = new VerificationFixture();
        var result = await fixture.Run("-Mode", "Focused", "-Checks", "static");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Equal(["static:"], fixture.Calls());
    }

    [Theory]
    [InlineData("missing-selection")]
    [InlineData("unknown-check")]
    [InlineData("full-selection")]
    [InlineData("full-filter")]
    [InlineData("static-filter")]
    public async Task Invalid_RequestFailsBeforeTouchingArtifacts(string scenario)
    {
        using var fixture = new VerificationFixture();
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.SummaryPath)!);
        File.WriteAllText(fixture.SummaryPath, "previous result");
        string[] arguments = scenario switch
        {
            "missing-selection" => ["-Mode", "Focused"],
            "unknown-check" => ["-Mode", "Focused", "-Checks", "missing"],
            "full-selection" => ["-Mode", "Full", "-Checks", "static"],
            "full-filter" => ["-Mode", "Full", "-TestFilter", "Example"],
            _ => ["-Mode", "Focused", "-Checks", "static", "-TestFilter", "Example"]
        };

        var result = await fixture.Run(arguments);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Empty(fixture.Calls());
        Assert.Equal("previous result", File.ReadAllText(fixture.SummaryPath));
    }

    [Fact]
    public async Task Focused_FailedPrerequisiteStopsDependentTestsAndReportsFailure()
    {
        using var fixture = new VerificationFixture();
        fixture.FailBuild();
        var result = await fixture.Run("-Mode", "Focused", "-Checks", "selected");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(["restore:", "build:"], fixture.Calls());
        using var summary = JsonDocument.Parse(File.ReadAllText(fixture.SummaryPath));
        Assert.Equal(7, summary.RootElement.GetProperty("checks")[1].GetProperty("exitCode").GetInt32());
    }

    [Fact]
    public async Task Default_FullRunExecutesEveryCheckWithoutRestrictions()
    {
        using var fixture = new VerificationFixture();
        var result = await fixture.Run();

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Equal(["static:", "restore:", "build:", "selected:", "unselected:"], fixture.Calls());
        using var summary = JsonDocument.Parse(File.ReadAllText(fixture.SummaryPath));
        Assert.Equal("Full", summary.RootElement.GetProperty("mode").GetString());
    }

    private sealed class VerificationFixture : IDisposable
    {
        private readonly TemporaryDirectory directory = new();
        private readonly string eng;

        public VerificationFixture()
        {
            eng = Path.Combine(directory.Path, "eng");
            Directory.CreateDirectory(Path.Combine(eng, "checks"));
            File.Copy(Path.Combine(RepositoryRoot(), "eng", "verify.ps1"), Path.Combine(eng, "verify.ps1"));
            File.WriteAllText(Path.Combine(eng, "initialize-dotnet.ps1"), "param([string]$RepositoryRoot)\n");
            File.WriteAllText(Path.Combine(eng, "test-manifest.json"), """
                {"checks":[
                  {"name":"Static","script":"checks/static.ps1"},
                  {"name":"Restore","script":"checks/restore.ps1"},
                  {"name":"Build","script":"checks/build.ps1","prerequisites":["restore"]},
                  {"name":"Selected","script":"checks/selected.ps1","prerequisites":["build"],"acceptsTestFilter":true},
                  {"name":"Unselected","script":"checks/unselected.ps1","prerequisites":["build"],"acceptsTestFilter":true}
                ]}
                """);
            foreach (var id in new[] { "static", "restore", "build", "selected", "unselected" })
                WriteCheck(id, 0);
        }

        public string SummaryPath => Path.Combine(directory.Path, "artifacts", "verify", "summary.json");
        public string[] Calls() => File.Exists(Path.Combine(directory.Path, "calls"))
            ? File.ReadAllLines(Path.Combine(directory.Path, "calls")) : [];
        public void FailBuild() => WriteCheck("build", 7);

        public async Task<(int ExitCode, string Output)> Run(params string[] arguments)
        {
            var start = new ProcessStartInfo("powershell")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(eng, "verify.ps1") }.Concat(arguments))
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                return (process.ExitCode, await output + await error);
            }
            finally
            {
                if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(); }
            }
        }

        private void WriteCheck(string id, int exitCode) => File.WriteAllText(Path.Combine(eng, "checks", id + ".ps1"), $$"""
            param([string]$RepositoryRoot, [string]$ArtifactsRoot{{(id is "selected" or "unselected" ? ", [string]$TestFilter = ''" : "")}})
            [IO.File]::AppendAllText((Join-Path $RepositoryRoot 'calls'), '{{id}}:' + $TestFilter + [Environment]::NewLine)
            exit {{exitCode}}
            """);

        public void Dispose() => directory.Dispose();
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "eng", "verify.ps1"))) return directory.FullName;
        throw new DirectoryNotFoundException("Repository verification entrypoint not found.");
    }
}
