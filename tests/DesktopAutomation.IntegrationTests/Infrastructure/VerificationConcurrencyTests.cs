using System.Diagnostics;
using System.Text.Json;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Infrastructure;

public sealed class VerificationConcurrencyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task FullVerification_SerializesSharedArtifacts_AndReleasesLockAfterSuccessOrFailure(int firstCheckExitCode)
    {
        using var directory = new TemporaryDirectory();
        var eng = Path.Combine(directory.Path, "eng");
        Directory.CreateDirectory(Path.Combine(eng, "checks"));
        File.Copy(Path.Combine(RepositoryRoot(), "eng", "verify.ps1"), Path.Combine(eng, "verify.ps1"));
        File.WriteAllText(Path.Combine(eng, "initialize-dotnet.ps1"), "param([string]$RepositoryRoot)\n");
        File.WriteAllText(Path.Combine(eng, "test-manifest.json"), """
            {"checks":[{"name":"Controlled check","script":"checks/controlled.ps1"}]}
            """);
        File.WriteAllText(Path.Combine(eng, "checks", "controlled.ps1"), $$"""
            param([string]$RepositoryRoot, [string]$ArtifactsRoot)
            $first = @(Get-ChildItem -LiteralPath $RepositoryRoot -Filter 'called-*').Count -eq 0
            [IO.File]::WriteAllText((Join-Path $RepositoryRoot "called-$PID"), 'called')
            [IO.File]::WriteAllText((Join-Path $ArtifactsRoot 'sentinel'), 'preserved during wait')
            if ($first) {
                Write-Host 'controlled-ready'
                $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
                while (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot 'release'))) {
                    if ([DateTimeOffset]::UtcNow -gt $deadline) { exit 9 }
                    Start-Sleep -Milliseconds 20
                }
                exit {{firstCheckExitCode}}
            }
            exit 0
            """);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var first = Start(directory.Path);
        Process? second = null;
        try
        {
            await ReadUntil(first, "controlled-ready", timeout.Token);
            second = Start(directory.Path);
            await ReadUntil(second, "Waiting for another repository verification", timeout.Token);
            Assert.Equal("preserved during wait", File.ReadAllText(Path.Combine(directory.Path, "artifacts", "verify", "sentinel")));
            Assert.Single(Directory.GetFiles(directory.Path, "called-*"));
            File.WriteAllText(Path.Combine(directory.Path, "release"), "release");
            await Task.WhenAll(first.WaitForExitAsync(timeout.Token), second.WaitForExitAsync(timeout.Token));
            Assert.Equal(firstCheckExitCode == 0 ? 0 : 1, first.ExitCode);
            Assert.True(second.ExitCode == 0, await second.StandardError.ReadToEndAsync());
            Assert.Equal(2, Directory.GetFiles(directory.Path, "called-*").Length);
            using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.Path, "artifacts", "verify", "summary.json")));
            Assert.Equal(0, summary.RootElement.GetProperty("checks")[0].GetProperty("exitCode").GetInt32());
        }
        finally
        {
            Stop(first);
            if (second is not null) { Stop(second); second.Dispose(); }
        }
    }

    private static Process Start(string root)
    {
        var start = new ProcessStartInfo("powershell")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(root, "eng", "verify.ps1"), "-Mode", "Full" })
            start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    private static async Task ReadUntil(Process process, string marker, CancellationToken ct)
    {
        while (await process.StandardOutput.ReadLineAsync().WaitAsync(ct) is { } line)
            if (line.Contains(marker, StringComparison.Ordinal)) return;
        throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
    }

    private static void Stop(Process process)
    {
        if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(); }
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "eng", "verify.ps1"))) return directory.FullName;
        throw new DirectoryNotFoundException("Repository verification entrypoint not found.");
    }
}
