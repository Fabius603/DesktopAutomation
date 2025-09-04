using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Http;
using ImageDetection.Model;
using Microsoft.Extensions.Logging;

namespace ImageDetection.YOLO
{
    public class YOLOModelDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YOLOModelDownloader> _logger;
        public event EventHandler<ModelDownloadProgressEventArgs>? DownloadProgressChanged;

        public YOLOModelDownloader(ILogger<YOLOModelDownloader> logger, HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            _logger = logger;
        }

        private static async Task DownloadWithHashAsync(HttpClient http, string url, string target, string expectedSha256, IProgress<int>? progress, CancellationToken ct)
        {
            var tmp = target + ".part";
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            var total = resp.Content.Headers.ContentLength ?? -1;
            await using var net = await resp.Content.ReadAsStreamAsync(ct);
            await using var file = File.Create(tmp);

            var buffer = new byte[64 * 1024];
            long readTotal = 0;
            int read;
            while ((read = await net.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), ct);
                readTotal += read;
                if (total > 0 && progress != null)
                {
                    var pct = (int)Math.Clamp((readTotal * 100.0 / total), 0, 100);
                    progress.Report(pct);
                }
            }

            await file.FlushAsync(ct);

            await using var check = File.OpenRead(tmp);
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(await sha.ComputeHashAsync(check, ct)).ToLowerInvariant();

            if (!hash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(tmp);
                throw new InvalidOperationException($"Hash mismatch for model. Expected {expectedSha256}, got {hash}.");
            }

            if (File.Exists(target)) File.Delete(target);
            File.Move(tmp, target);
        }

        public async Task<YOLOModel> DownloadModelAsync(string modelKey, CancellationToken ct = default)
        {
            var manifest = ModelManifestProvider.LoadEmbedded();
            if (!manifest.Models.TryGetValue(modelKey, out var entry))
                throw new InvalidOperationException($"Model '{modelKey}' nicht im Manifest.");

            var appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopAutomation", "YOLOModels");
            Directory.CreateDirectory(appDataRoot);
            var onnxPath = Path.Combine(appDataRoot, modelKey + ".onnx");

            // Wenn bereits vorhanden & Hash stimmt → fertig
            if (File.Exists(onnxPath) && await VerifySha256Async(onnxPath, entry.Sha256, ct))
            {
                Report(modelKey, ModelDownloadStatus.Completed, 100, "Bereits vorhanden");
                return new YOLOModel { Id = modelKey, Name = modelKey, OnnxPath = onnxPath, OnnxSizeBytes = new FileInfo(onnxPath).Length, CreatedUtc = DateTime.UtcNow };
            }

            Report(modelKey, ModelDownloadStatus.ConvertingToOnnx, 0);
            var progress = new Progress<int>(p => Report(modelKey, ModelDownloadStatus.ConvertingToOnnx, p));

            await DownloadWithHashAsync(_httpClient, entry.Url, onnxPath, entry.Sha256, progress, ct);

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
            await using var s = File.OpenRead(path);
            var hash = Convert.ToHexString(await sha.ComputeHashAsync(s, ct)).ToLowerInvariant();
            return hash.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
