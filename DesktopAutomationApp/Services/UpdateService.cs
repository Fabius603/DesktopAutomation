using System;
using System.Diagnostics;
using System.IO;
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

                // 2. Determine payload type; use extension OR magic bytes to avoid false negatives.
                var isZipByName = string.Equals(Path.GetExtension(downloadPath), ".zip", StringComparison.OrdinalIgnoreCase);
                var isZipPayload = isZipByName || IsZipFile(downloadPath);

                // 3. Write updater PS1 — waits for this process to exit, extracts/copies, relaunches
                var pid = Process.GetCurrentProcess().Id;
                var appDir = Path.GetDirectoryName(currentExe)!;
                var appDirEsc    = appDir.Replace("'", "''");
                var currentExeEsc = currentExe.Replace("'", "''");
                var tempDirEsc   = tempDir.Replace("'", "''");
                var downloadPathEsc = downloadPath.Replace("'", "''");
                var payloadIsZip = isZipPayload ? "$true" : "$false";
                var mainExeNameEsc = Path.GetFileName(currentExe).Replace("'", "''");
                var payloadFolderNameEsc = Path.GetFileNameWithoutExtension(downloadPath).Replace("'", "''");

                var script = $$"""
                    $targetPid = {{pid}}
                    $isZip = {{payloadIsZip}}
                    $downloadPath = '{{downloadPathEsc}}'
                    $stageDir = Join-Path '{{tempDirEsc}}' 'x'
                    $mainExeName = '{{mainExeNameEsc}}'
                    $payloadFolderName = '{{payloadFolderNameEsc}}'
                    $logPath = Join-Path '{{tempDirEsc}}' 'update.log'
                    $ErrorActionPreference = 'Stop'
                    try {
                        while ((Get-Process -Id $targetPid -ErrorAction SilentlyContinue) -ne $null) {
                            Start-Sleep -Milliseconds 300
                        }

                        # Move away from the app directory so Remove-Item on app folder is not blocked by current location.
                        Set-Location -LiteralPath ([System.IO.Path]::GetTempPath())

                        if (Test-Path $stageDir) { Remove-Item -LiteralPath $stageDir -Recurse -Force -ErrorAction SilentlyContinue }
                        New-Item -Path $stageDir -ItemType Directory -Force | Out-Null

                        if ($isZip) {
                            Expand-Archive -LiteralPath $downloadPath -DestinationPath $stageDir -Force
                        } else {
                            Copy-Item -LiteralPath $downloadPath -Destination (Join-Path $stageDir $mainExeName) -Force
                        }

                        $sourceDir = $stageDir
                        $entries = Get-ChildItem -LiteralPath $stageDir -Force
                        if ($entries.Count -eq 1 -and $entries[0].PSIsContainer) { $sourceDir = $entries[0].FullName }

                        $mainSourceExe = Join-Path $sourceDir $mainExeName
                        if (-not (Test-Path -LiteralPath $mainSourceExe)) {
                            throw "Main executable not found in update payload: $mainSourceExe"
                        }

                        $currentAppDir = '{{appDirEsc}}'
                        $parentDir = Split-Path $currentAppDir -Parent
                        $newDirName = if ($sourceDir -eq $stageDir) { $payloadFolderName } else { Split-Path $sourceDir -Leaf }
                        if ([string]::IsNullOrWhiteSpace($newDirName)) { $newDirName = 'DesktopAutomationApp' }
                        $newAppDir = Join-Path $parentDir $newDirName

                        # Stage installation folder first, then swap complete directory.
                        $installStage = Join-Path '{{tempDirEsc}}' 'install'
                        if (Test-Path -LiteralPath $installStage) {
                            Remove-Item -LiteralPath $installStage -Recurse -Force -ErrorAction SilentlyContinue
                        }
                        New-Item -Path $installStage -ItemType Directory -Force | Out-Null

                        $stagedRoot = Join-Path $installStage $newDirName
                        Copy-Item -LiteralPath $sourceDir -Destination $stagedRoot -Recurse -Force -ErrorAction Stop

                        if ((Test-Path -LiteralPath $newAppDir) -and ($newAppDir -ne $currentAppDir)) {
                            Remove-Item -LiteralPath $newAppDir -Recurse -Force -ErrorAction Stop
                        }

                        if (Test-Path -LiteralPath $currentAppDir) {
                            Remove-Item -LiteralPath $currentAppDir -Recurse -Force -ErrorAction Stop
                        }

                        Move-Item -LiteralPath $stagedRoot -Destination $newAppDir -Force

                        $newExePath = Join-Path $newAppDir $mainExeName
                        if (-not (Test-Path -LiteralPath $newExePath)) {
                            throw "Updated executable missing after folder swap: $newExePath"
                        }

                        Start-Process -FilePath $newExePath
                        Remove-Item -LiteralPath '{{tempDirEsc}}' -Recurse -Force -ErrorAction SilentlyContinue
                    }
                    catch {
                        try {
                            $err = "$(Get-Date -Format o) - $($_.Exception.Message)"
                            Add-Content -LiteralPath $logPath -Value $err
                        } catch { }
                        Remove-Item -LiteralPath '{{tempDirEsc}}' -Recurse -Force -ErrorAction SilentlyContinue
                    }
                    """;

                var scriptPath = Path.Combine(tempDir, "update.ps1");
                await File.WriteAllTextAsync(scriptPath, script);

                // 4. Launch updater (it waits for this process to exit, then installs + relaunches)
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetTempPath(),
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
