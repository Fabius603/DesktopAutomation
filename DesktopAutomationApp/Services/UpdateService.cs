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
                // 1. Download — keep original filename so we can detect type
                var fileName = Uri.TryCreate(assetDownloadUrl, UriKind.Absolute, out var uri)
                    ? Path.GetFileName(uri.LocalPath)
                    : "update.bin";
                if (string.IsNullOrEmpty(fileName)) fileName = "update.bin";
                var downloadPath = Path.Combine(tempDir, fileName);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("DesktopAutomation", GetCurrentVersion()));
                client.Timeout = TimeSpan.FromMinutes(5);

                using var response = await client.GetAsync(assetDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                await using var fs = File.Create(downloadPath);
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

                // 2. Extract (ZIP) or copy directly (single-exe release)
                var extractDir = Path.Combine(tempDir, "x");
                Directory.CreateDirectory(extractDir);

                if (IsZipFile(downloadPath))
                {
                    ZipFile.ExtractToDirectory(downloadPath, extractDir);
                }
                else
                {
                    // Single-file release: rename to match current exe name
                    var destName = Path.GetFileName(currentExe);
                    File.Copy(downloadPath, Path.Combine(extractDir, destName), overwrite: true);
                }

                // 3. If the ZIP extracted into exactly one sub-folder, treat that as the source root
                var sourceDir = extractDir;
                var topEntries = Directory.GetFileSystemEntries(extractDir);
                if (topEntries.Length == 1 && Directory.Exists(topEntries[0]))
                    sourceDir = topEntries[0];

                // 4. Write updater PS1 — waits for this process to exit, copies ALL files, relaunches
                var pid = Process.GetCurrentProcess().Id;
                var appDir = Path.GetDirectoryName(currentExe)!;
                var sourceDirEsc = sourceDir.Replace("'", "''");
                var appDirEsc    = appDir.Replace("'", "''");
                var currentExeEsc = currentExe.Replace("'", "''");
                var tempDirEsc   = tempDir.Replace("'", "''");

                var script = $"""
                    $pid = {pid}
                    $limit = 60
                    $elapsed = 0
                    while ((Get-Process -Id $pid -ErrorAction SilentlyContinue) -ne $null) {{
                        Start-Sleep -Milliseconds 300
                        $elapsed += 0.3
                        if ($elapsed -ge $limit) {{ break }}
                    }}
                    Get-ChildItem -LiteralPath '{sourceDirEsc}' -Recurse -File | ForEach-Object {{
                        $rel  = $_.FullName.Substring('{sourceDirEsc}'.Length).TrimStart('\','/')
                        $dest = Join-Path '{appDirEsc}' $rel
                        $destDir = Split-Path $dest -Parent
                        if (-not (Test-Path $destDir)) {{ New-Item $destDir -ItemType Directory -Force | Out-Null }}
                        Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
                    }}
                    Start-Process -FilePath '{currentExeEsc}'
                    Remove-Item -LiteralPath '{tempDirEsc}' -Recurse -Force -ErrorAction SilentlyContinue
                    """;

                var scriptPath = Path.Combine(tempDir, "update.ps1");
                await File.WriteAllTextAsync(scriptPath, script);

                // 5. Launch updater (it blocks until this process exits, then copies + relaunches)
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
                try { Directory.Delete(tempDir, recursive: true); } catch { }
                return false;
            }
        }

        /// <summary>Checks the ZIP magic bytes (PK\x03\x04) to distinguish ZIP from raw exe.</summary>
        private static bool IsZipFile(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                var header = new byte[4];
                if (fs.Read(header, 0, 4) < 4) return false;
                return header[0] == 0x50 && header[1] == 0x4B &&
                       header[2] == 0x03 && header[3] == 0x04;
            }
            catch { return false; }
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
