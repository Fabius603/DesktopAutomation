using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopAutomationApp.Services
{
    public interface IUpdateService
    {
        /// <summary>Checks GitHub Releases for a newer version.</summary>
        Task<UpdateCheckResult> CheckForUpdateAsync();

        /// <summary>
        /// Downloads the release asset ZIP, extracts the new exe, writes an updater
        /// script that replaces the running exe after exit and relaunches it.
        /// Returns false if the asset URL is missing or extraction fails.
        /// </summary>
        Task<bool> DownloadAndInstallAsync(string assetDownloadUrl, IProgress<int>? progress = null);
    }

    public sealed record UpdateCheckResult(
        bool HasUpdate,
        string LatestTag,
        string HtmlUrl,
        string AssetDownloadUrl,
        string CurrentVersion);

    public sealed class UpdateService : IUpdateService
    {
        private const string ApiUrl = "https://api.github.com/repos/Fabius603/DesktopAutomation/releases/latest";

        public async Task<UpdateCheckResult> CheckForUpdateAsync()
        {
            var currentVersion = GetCurrentVersion();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("DesktopAutomation", currentVersion));
            client.Timeout = TimeSpan.FromSeconds(10);

            using var response = await client.GetAsync(ApiUrl);
            if (!response.IsSuccessStatusCode)
                return new UpdateCheckResult(false, string.Empty, string.Empty, string.Empty, currentVersion);

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagEl) ||
                !root.TryGetProperty("html_url", out var urlEl))
                return new UpdateCheckResult(false, string.Empty, string.Empty, string.Empty, currentVersion);

            var tag = tagEl.GetString() ?? string.Empty;
            var url = urlEl.GetString() ?? string.Empty;

            // First asset's browser_download_url (the ZIP)
            var assetUrl = string.Empty;
            if (root.TryGetProperty("assets", out var assetsEl) &&
                assetsEl.ValueKind == JsonValueKind.Array &&
                assetsEl.GetArrayLength() > 0)
            {
                var first = assetsEl[0];
                if (first.TryGetProperty("browser_download_url", out var dlEl))
                    assetUrl = dlEl.GetString() ?? string.Empty;
            }

            var hasUpdate = IsNewer(tag, currentVersion);
            return new UpdateCheckResult(hasUpdate, tag, url, assetUrl, currentVersion);
        }

        public async Task<bool> DownloadAndInstallAsync(string assetDownloadUrl, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(assetDownloadUrl)) return false;

            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExe)) return false;

            var tempDir = Path.Combine(Path.GetTempPath(), $"DA_update_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. Download ZIP
                var zipPath = Path.Combine(tempDir, "update.zip");
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("DesktopAutomation", GetCurrentVersion()));
                client.Timeout = TimeSpan.FromMinutes(5);

                using var response = await client.GetAsync(assetDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                await using var fs = File.Create(zipPath);
                await using var dl = await response.Content.ReadAsStreamAsync();

                var buffer = new byte[81920];
                long downloaded = 0;
                int read;
                while ((read = await dl.ReadAsync(buffer)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read));
                    downloaded += read;
                    if (totalBytes > 0)
                        progress?.Report((int)(downloaded * 100 / totalBytes));
                }

                // 2. Extract
                var extractDir = Path.Combine(tempDir, "x");
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                // 3. Find new exe (single-file publish — usually just one .exe)
                var newExe = FindExe(extractDir);
                if (newExe is null) return false;

                // 4. Write updater PS1 — waits for this process to exit, copies ALL files, relaunches
                var pid = Process.GetCurrentProcess().Id;
                var appDir = Path.GetDirectoryName(currentExe)!;
                var script = $$"""
                    $pid = {{pid}}
                    $limit = 60
                    $elapsed = 0
                    while ((Get-Process -Id $pid -ErrorAction SilentlyContinue) -ne $null) {
                        Start-Sleep -Milliseconds 300
                        $elapsed += 0.3
                        if ($elapsed -ge $limit) { break }
                    }
                    Get-ChildItem -Path '{{extractDir}}' -Recurse -File | ForEach-Object {
                        $rel  = $_.FullName.Substring('{{extractDir}}'.Length).TrimStart('\','/')
                        $dest = Join-Path '{{appDir}}' $rel
                        $destDir = Split-Path $dest -Parent
                        if (-not (Test-Path $destDir)) { New-Item $destDir -ItemType Directory -Force | Out-Null }
                        Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
                    }
                    Start-Process -FilePath '{{currentExe}}'
                    Remove-Item -LiteralPath '{{tempDir}}' -Recurse -Force -ErrorAction SilentlyContinue
                    """;

                var scriptPath = Path.Combine(tempDir, "update.ps1");
                await File.WriteAllTextAsync(scriptPath, script);

                // 5. Launch updater then let the caller shut down the app
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });

                return true;
            }
            catch
            {
                // Clean up on failure, but don't rethrow — caller shows error
                try { Directory.Delete(tempDir, recursive: true); } catch { }
                return false;
            }
        }

        private static string? FindExe(string dir)
        {
            // Prefer a file named DesktopAutomationApp.exe; fall back to any .exe
            var preferred = Directory.GetFiles(dir, "DesktopAutomationApp.exe", SearchOption.AllDirectories);
            if (preferred.Length > 0) return preferred[0];
            var any = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories);
            return any.Length > 0 ? any[0] : null;
        }

        private static string GetCurrentVersion()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return ver is null ? "0.0.0" : $"{ver.Major}.{ver.Minor}.{ver.Build}";
        }

        private static bool IsNewer(string tag, string current)
        {
            var stripped = tag.TrimStart('v', 'V');
            return Version.TryParse(stripped, out var remote)
                && Version.TryParse(current, out var local)
                && remote > local;
        }
    }
}
