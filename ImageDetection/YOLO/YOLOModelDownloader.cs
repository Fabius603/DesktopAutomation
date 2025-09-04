using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageDetection.Model;
using Microsoft.Extensions.Logging;

namespace ImageDetection.YOLO
{
    public class YOLOModelDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YOLOModelDownloader> _logger;

        // pro Zielpfad nur ein gleichzeitiger Download
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _pathLocks = new();

        public event EventHandler<ModelDownloadProgressEventArgs>? DownloadProgressChanged;

        public YOLOModelDownloader(ILogger<YOLOModelDownloader> logger, HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            _logger = logger;
        }

        public async Task<YOLOModel> DownloadModelAsync(string modelKey, CancellationToken ct = default)
        {
            var manifest = ModelManifestProvider.LoadManifest();
            if (!manifest.Models.TryGetValue(modelKey, out var entry))
                throw new InvalidOperationException($"Model '{modelKey}' nicht im Manifest.");

            var appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DesktopAutomation", "YOLOModels");
            Directory.CreateDirectory(appDataRoot);

            var onnxPath = Path.Combine(appDataRoot, modelKey + ".onnx");

            // parallele Downloads auf denselben Pfad vermeiden
            var gate = _pathLocks.GetOrAdd(onnxPath, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);
            try
            {
                // Bereits vorhanden & Hash ok → fertig
                if (File.Exists(onnxPath) && await VerifySha256Async(onnxPath, entry.Sha256, ct))
                {
                    Report(modelKey, ModelDownloadStatus.Completed, 100, "Bereits vorhanden");
                    return new YOLOModel
                    {
                        Id = modelKey,
                        Name = modelKey,
                        OnnxPath = onnxPath,
                        OnnxSizeBytes = new FileInfo(onnxPath).Length,
                        CreatedUtc = DateTime.UtcNow
                    };
                }

                Report(modelKey, ModelDownloadStatus.DownloadingOnnx, 0);
                var progress = new Progress<int>(p => Report(modelKey, ModelDownloadStatus.DownloadingOnnx, p));

                await DownloadWithHashAsync(
                    http: _httpClient,
                    url: entry.Url,
                    target: onnxPath,
                    expectedSha256: entry.Sha256,
                    progress: progress,
                    ct: ct);

                Report(modelKey, ModelDownloadStatus.Completed, 100);

                return new YOLOModel
                {
                    Id = modelKey,
                    Name = modelKey,
                    OnnxPath = onnxPath,
                    OnnxSizeBytes = new FileInfo(onnxPath).Length,
                    CreatedUtc = DateTime.UtcNow
                };
            }
            finally
            {
                gate.Release();
                // Speicher aufräumen, falls keine weiteren Nutzer
                if (gate.CurrentCount == 1) _pathLocks.TryRemove(onnxPath, out _);
            }
        }

        private static async Task DownloadWithHashAsync(
            HttpClient http,
            string url,
            string target,
            string expectedSha256,
            IProgress<int>? progress,
            CancellationToken ct)
        {
            var dir = Path.GetDirectoryName(target)!;
            Directory.CreateDirectory(dir);

            var tmp = target + ".part";

            // alte .part sauber entfernen (mit kurzem Retry, falls AV/Indexing blockiert)
            await RetryIOAsync(async () =>
            {
                if (File.Exists(tmp))
                {
                    // exklusiv öffnen → wenn das klappt, hält kein anderer mehr den Handle
                    using (new FileStream(tmp, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1, false)) { }
                    File.Delete(tmp);
                }
                return 0;
            });

            // Download – Header separat laden, dann Stream
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            var total = resp.Content.Headers.ContentLength ?? -1L;

            // exklusiv schreiben; großer Buffer; async
            await RetryIOAsync(async () =>
            {
                await using var src = await resp.Content.ReadAsStreamAsync(ct);
                await using var dst = new FileStream(
                    tmp, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 128 * 1024, useAsync: true);

                var buffer = new byte[128 * 1024];
                long readTotal = 0;
                int read;
                while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                    readTotal += read;

                    if (total > 0 && progress is not null)
                    {
                        var pct = (int)Math.Clamp(readTotal * 100.0 / total, 0, 100);
                        progress.Report(pct);
                    }
                }

                await dst.FlushAsync(ct);
                return 0;
            });

            // Hash prüfen (neue Read-Session → keine Locks vom Schreibstream)
            var ok = await VerifySha256Async(tmp, expectedSha256, ct);
            if (!ok)
            {
                TryDeleteQuiet(tmp);
                throw new InvalidOperationException("Hash mismatch für heruntergeladenes Modell.");
            }

            // Ziel ersetzen – atomar, mit Retry gegen AV/Indexing
            await RetryIOAsync(async () =>
            {
                if (File.Exists(target)) File.Delete(target);
                File.Move(tmp, target);
                return 0;
            });

            // garantiert 100% melden, falls Content-Length unbekannt war
            progress?.Report(100);
        }

        private void Report(string modelName, ModelDownloadStatus status, int progressPercent, string? message = null)
        {
            try
            {
                DownloadProgressChanged?.Invoke(
                    this,
                    new ModelDownloadProgressEventArgs(
                        modelName: modelName,
                        status: status,
                        progressPercent: Math.Clamp(progressPercent, 0, 100),
                        message: message
                    ));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fehler beim Auslösen des DownloadProgressChanged-Events.");
            }
        }

        private static async Task<bool> VerifySha256Async(string path, string expected, CancellationToken ct)
        {
            using var sha = SHA256.Create();
            await using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
            var hash = Convert.ToHexString(await sha.ComputeHashAsync(s, ct)).ToLowerInvariant();
            return hash.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<T> RetryIOAsync<T>(Func<Task<T>> action, int attempts = 5, int delayMs = 200)
        {
            for (int i = 1; ; i++)
            {
                try { return await action(); }
                catch (IOException) when (i < attempts)
                {
                    await Task.Delay(delayMs * i);
                }
                catch (UnauthorizedAccessException) when (i < attempts)
                {
                    await Task.Delay(delayMs * i);
                }
            }
        }

        private static void TryDeleteQuiet(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }
    }
}
