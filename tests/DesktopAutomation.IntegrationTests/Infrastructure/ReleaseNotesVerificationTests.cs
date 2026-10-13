using System.Diagnostics;
using System.Text.Json;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Infrastructure;

public sealed class ReleaseNotesVerificationTests
{
    [Theory]
    [InlineData("1.6.2", "1.6.2", true)]
    [InlineData("1.6.2", "1.6.3", true)]
    [InlineData("1.6.2", "1.6.1", false)]
    [InlineData("1.6.2", "1.6.4", false)]
    [InlineData("1.6.2", "1.7.0", false)]
    [InlineData("2.0.0", "2.0.0", true)]
    [InlineData("2.0.0", "2.0.1", true)]
    public async Task VersionCheck_AcceptsReleaseAndNextPatchButRejectsOtherVersions(
        string headVersion, string newestVersion, bool valid)
    {
        var result = await Run(headVersion, [newestVersion]);

        Assert.True((result.ExitCode == 0) == valid, result.Output);
        if (!valid) Assert.Contains("Newest release-note version must be", result.Output);
    }

    [Theory]
    [InlineData("1.6.1", true)]
    [InlineData("1.6.2", false)]
    [InlineData("1.6.3", false)]
    public async Task ReleaseCommit_StillRequiresStrictlyDescendingHistory(string olderVersion, bool valid)
    {
        var result = await Run("1.6.2", ["1.6.2", olderVersion]);

        Assert.True((result.ExitCode == 0) == valid, result.Output);
        if (!valid) Assert.Contains("Release notes must be newest first", result.Output);
    }

    [Fact]
    public async Task ReleaseCommit_StillRequiresBothLanguages()
    {
        var result = await Run("1.6.2", ["1.6.2"], english: "");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("without German and English text", result.Output);
    }

    private static async Task<(int ExitCode, string Output)> Run(
        string headVersion, string[] versions, string english = "Example fix")
    {
        using var directory = new TemporaryDirectory();
        var resources = Path.Combine(directory.Path, "DesktopAutomationApp", "Resources");
        Directory.CreateDirectory(resources);
        File.WriteAllText(Path.Combine(resources, "ReleaseNotes.json"), JsonSerializer.Serialize(
            versions.Select(version => new
            {
                version,
                date = "2026-10-12",
                sections = new[] { new
                {
                    title = new { de = "Änderungen", en = "Changes" },
                    changes = new[] { new { category = "Fixed", de = "Beispielkorrektur", en = english } }
                } }
            })));
        File.Copy(Path.Combine(RepositoryRoot(), "eng", "checks", "release-notes.ps1"),
            Path.Combine(directory.Path, "check.ps1"));
        // Control the Git boundary without modifying the real repository or creating commits.
        File.WriteAllText(Path.Combine(directory.Path, "run.ps1"), $$"""
            function global:git {
                $global:LASTEXITCODE = 0
                '<Project><PropertyGroup><Version>{{headVersion}}</Version></PropertyGroup></Project>'
            }
            & (Join-Path $PSScriptRoot 'check.ps1') -RepositoryRoot $PSScriptRoot -ArtifactsRoot $PSScriptRoot
            """);
        var start = new ProcessStartInfo("powershell")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(directory.Path, "run.ps1") })
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

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "eng", "verify.ps1"))) return directory.FullName;
        throw new DirectoryNotFoundException("Repository verification entrypoint not found.");
    }
}
