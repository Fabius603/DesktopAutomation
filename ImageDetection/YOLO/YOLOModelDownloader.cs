using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ImageDetection.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;

namespace ImageDetection.YOLO
{
    public class YOLOModelDownloader : IYOLOModelDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YOLOModelDownloader> _logger;

        // pro Zielpfad nur ein gleichzeitiger Download
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _pathLocks = new();

        public event EventHandler<ModelDownloadProgressEventArgs>? DownloadProgressChanged;

        public string ModelFolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopAutomation", "YOLOModels");

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

            Directory.CreateDirectory(ModelFolderPath);

            var onnxPath = Path.Combine(ModelFolderPath, modelKey + ".onnx");

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

                var model = new YOLOModel
                {
                    Id = modelKey,
                    Name = modelKey,
                    OnnxPath = onnxPath,
                    OnnxSizeBytes = new FileInfo(onnxPath).Length,
                    CreatedUtc = DateTime.UtcNow
                };

                await EnsureLabelsSidecarAsync(modelKey, model.OnnxPath, ct);

                return model;
            }
            finally
            {
                gate.Release();
                // Speicher aufräumen, falls keine weiteren Nutzer
                if (gate.CurrentCount == 1) _pathLocks.TryRemove(onnxPath, out _);
            }
        }

        public async Task<bool> UninstallModelAsync(string modelKey, CancellationToken ct = default)
        {
            try
            {
                var onnxPath = Path.Combine(ModelFolderPath, modelKey + ".onnx");
                var labelsPath = Path.Combine(ModelFolderPath, $"{modelKey}.labels.txt");

                bool wasDeleted = false;

                // ONNX-Datei löschen
                if (File.Exists(onnxPath))
                {
                    File.Delete(onnxPath);
                    wasDeleted = true;
                    _logger.LogInformation("ONNX-Datei gelöscht: {onnxPath}", onnxPath);
                }

                // Labels-Datei löschen
                if (File.Exists(labelsPath))
                {
                    File.Delete(labelsPath);
                    _logger.LogInformation("Labels-Datei gelöscht: {labelsPath}", labelsPath);
                }

                if (wasDeleted)
                {
                    _logger.LogInformation("Modell {modelKey} erfolgreich deinstalliert.", modelKey);
                }
                else
                {
                    _logger.LogWarning("Modell {modelKey} war nicht installiert.", modelKey);
                }

                return wasDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Deinstallieren des Modells {modelKey}.", modelKey);
                return false;
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

        private async Task EnsureLabelsSidecarAsync(string modelKey, string onnxPath, CancellationToken ct)
        {
            var dir = Path.GetDirectoryName(onnxPath)!;
            var sidecar = Path.Combine(dir, $"{modelKey}.labels.txt");

            if (File.Exists(sidecar))
            {
                _logger.LogDebug("Labels-Sidecar existiert bereits: {sidecar}", sidecar);
                return;
            }

            try
            {
                if (TryReadLabelsFromOnnx(onnxPath, out var labels) && labels.Length > 0)
                {
                    Directory.CreateDirectory(dir);
                    await File.WriteAllLinesAsync(sidecar, labels, ct);
                    _logger.LogInformation("Labels aus ONNX-Metadaten extrahiert ({count}) und geschrieben: {sidecar}",
                        labels.Length, sidecar);
                }
                else
                {
                    _logger.LogWarning("Keine Labels im ONNX gefunden. Bitte Labels separat bereitstellen. Modell: {modelKey}", modelKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Labels konnten nicht aus dem ONNX gelesen werden. Modell: {modelKey}", modelKey);
            }
        }

        private static bool TryReadLabelsFromOnnx(string onnxPath, out string[] labels)
        {
            labels = Array.Empty<string>();

            using var so = new SessionOptions(); // CPU reicht zum Lesen der Metadaten
            using var session = new InferenceSession(onnxPath, so);

            var meta = session.ModelMetadata?.CustomMetadataMap;
            if (meta is null || meta.Count == 0)
                return false;

            // Mögliche Schlüssel (Ultralytics nutzt i. d. R. "names")
            var candidateKeys = new[] { "names", "labels", "classes" };
            string? raw = null;
            string? keyFound = null;

            foreach (var k in candidateKeys)
            {
                if (meta.TryGetValue(k, out raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    keyFound = k;
                    break;
                }
            }

            if (raw is null)
            {
                // Seltener: keys wie names0=..., names1=... etc.
                var indexed = meta
                    .Where(kv => Regex.IsMatch(kv.Key, @"^names?\d+$"))
                    .OrderBy(kv =>
                    {
                        var m = Regex.Match(kv.Key, @"\d+");
                        return m.Success ? int.Parse(m.Value) : int.MaxValue;
                    })
                    .Select(kv => kv.Value?.Trim())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToArray();

                if (indexed.Length > 0)
                {
                    labels = indexed!;
                    return true;
                }

                return false;
            }

            // 1) JSON-Object {"0":"person",...}
            if (TryParseJsonDict(raw, out var dictObj))
            {
                labels = dictObj
                    .OrderBy(kv => TryInt(kv.Key))
                    .Select(kv => kv.Value.Trim())
                    .Where(v => v.Length > 0)
                    .ToArray();
                return labels.Length > 0;
            }

            // 2) JSON-Array ["person",...]
            if (TryParseJsonArray(raw, out var arr))
            {
                labels = arr
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToArray();
                return labels.Length > 0;
            }

            // 3) Fallback: Komma- oder Zeilen-getrennt
            var split = raw.Replace("\r", "")
                           .Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => s.Length > 0)
                           .ToArray();
            if (split.Length > 0)
            {
                labels = split;
                return true;
            }

            return false;

            // --- lokale Helfer ---
            static bool TryParseJsonDict(string s, out Dictionary<string, string> dict)
            {
                dict = new Dictionary<string, string>();

                // single quotes -> double quotes, keys ohne quotes -> quote keys
                var normalized = NormalizeToJsonObject(s);
                try
                {
                    var tmp = JsonSerializer.Deserialize<Dictionary<string, string>>(normalized);
                    if (tmp is null || tmp.Count == 0) return false;
                    dict = tmp;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            static bool TryParseJsonArray(string s, out string[] arr)
            {
                arr = Array.Empty<string>();

                var normalized = NormalizeToJsonArray(s);
                try
                {
                    var tmp = JsonSerializer.Deserialize<string[]>(normalized);
                    if (tmp is null || tmp.Length == 0) return false;
                    arr = tmp;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            static int TryInt(string k) => int.TryParse(k, out var i) ? i : int.MaxValue;

            // macht aus {0:'person',1:'bicycle'} → {"0":"person","1":"bicycle"}
            static string NormalizeToJsonObject(string s)
            {
                var t = s.Trim();

                // fehlende geschweifte Klammern ergänzen
                if (!t.StartsWith("{")) t = "{" + t;
                if (!t.EndsWith("}")) t = t + "}";

                // single quotes -> double quotes
                t = t.Replace('\'', '"');

                // unquoted keys (0: "x") → "0": "x"
                t = Regex.Replace(t, @"(?<pre>[\{\s,])(?<key>\d+)\s*:", "${pre}\"${key}\":");

                return t;
            }

            // macht aus ['person','bicycle'] → ["person","bicycle"]
            static string NormalizeToJsonArray(string s)
            {
                var t = s.Trim();

                if (!t.StartsWith("[")) t = "[" + t;
                if (!t.EndsWith("]")) t = t + "]";

                t = t.Replace('\'', '"');
                return t;
            }
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
