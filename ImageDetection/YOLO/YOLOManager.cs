using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using ImageDetection.Model;         // für YOLOModel DTO
using ImageHelperMethods;          // dein ScreenHelper
using ImageDetection.YOLO;         // dein YOLOModelDownloader

namespace ImageDetection.YOLO
{
    public enum YoloGpuBackend { Cpu, DirectML, Cuda, Auto }

    /// <summary>
    /// Default: erwartet eine Textdatei &lt;modelKey&gt;.labels.txt neben dem ONNX (eine Klasse pro Zeile).
    /// </summary>
    public sealed class LabelProvider : ILabelProvider
    {
        public IReadOnlyList<string> GetLabels(string modelKey, string onnxPath)
        {
            var dir = Path.GetDirectoryName(onnxPath)!;
            var sidecar = Path.Combine(dir, $"{modelKey}.labels.txt");
            if (!File.Exists(sidecar))
                throw new FileNotFoundException($"Labeldatei nicht gefunden: {sidecar}");
            return File.ReadAllLines(sidecar)
                       .Select(l => l.Trim())
                       .Where(l => !string.IsNullOrWhiteSpace(l))
                       .ToArray();
        }
    }

    public sealed class YoloManager : IYoloManager
    {
        private readonly IYOLOModelDownloader _downloader;
        private readonly ILogger<YoloManager> _logger;
        private readonly YoloManagerOptions _opt;
        private readonly ILabelProvider _labels;

        private readonly ConcurrentDictionary<string, Lazy<Task<(YOLOModel model, InferenceSession session, IReadOnlyList<string> labels)>>> _cache
            = new();

        public event Action<string, ModelDownloadStatus, int, string?>? DownloadProgressChanged;

        public YoloManager(
            IYOLOModelDownloader downloader,
            ILogger<YoloManager> logger,
            YoloManagerOptions? options = null,
            ILabelProvider? labelProvider = null)
        {
            _downloader = downloader;
            _logger = logger;
            _opt = options ?? new YoloManagerOptions();
            _labels = labelProvider ?? new LabelProvider();

            // Log verfügbare Execution Provider beim Start
            var providers = new List<string> { "CPU" };
            if (IsCudaAvailable()) providers.Add("CUDA");
            if (IsDirectMLAvailable()) providers.Add("DirectML");
            
            _logger.LogInformation("Available ONNX Runtime Execution Providers: {Providers}", 
                string.Join(", ", providers));
            
            // Automatische Backend-Auswahl falls Auto gewählt
            if (_opt.GpuBackend == YoloGpuBackend.Auto)
            {
                var recommended = GetRecommendedBackend();
                _logger.LogInformation("Auto backend selection: using {Backend}", recommended);
            }

            // Progress vom Downloader nach außen weiterreichen
            _downloader.DownloadProgressChanged += (sender, e)
                => DownloadProgressChanged?.Invoke(e.ModelName, e.Status, e.ProgressPercent, e.Message);
        }

        public async Task EnsureModelAsync(string modelKey, CancellationToken ct = default)
        {
            await GetOrCreateAsync(modelKey, ct).ConfigureAwait(false);
        }

        public bool HasSession(string modelKey)
        {
            if (_cache.TryGetValue(modelKey, out var lazy) && lazy.IsValueCreated)
            {
                var task = lazy.Value;
                return task.IsCompletedSuccessfully && task.Result.session != null;
            }
            return false;
        }

        public List<string> GetAvailableModels()
        {
            try
            {
                var manifest = ModelManifestProvider.LoadManifest();
                var availableModels = new List<string>();

                foreach (var modelKey in manifest.Models.Keys)
                {
                    var onnxPath = Path.Combine(_downloader.ModelFolderPath, modelKey + ".onnx");
                    if (File.Exists(onnxPath))
                    {
                        availableModels.Add(modelKey);
                    }
                }

                return availableModels.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Konnte Modell-Manifest nicht laden, um verfügbare Modelle zu ermitteln.");
                return new List<string>();
            }
        }

        public List<string> GetClassesForModel(string modelKey)
        {
            try
            {                
                var onnxPath = Path.Combine(_downloader.ModelFolderPath, modelKey + ".onnx");
                
                if (!File.Exists(onnxPath))
                {
                    _logger.LogWarning("ONNX-Datei für Modell {modelKey} nicht gefunden: {onnxPath}", modelKey, onnxPath);
                    return new List<string>();
                }

                var labelList = _labels.GetLabels(modelKey, onnxPath);
                return labelList.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fehler beim Laden der Labels für Modell {modelKey}", modelKey);
                return new List<string>();
            }
        }

        private Task<InferenceSession> CreateSessionAsync(YOLOModel model, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                // Helper zum Erstellen + Loggen
                InferenceSession CreateWith(SessionOptions so, string label)
                {
                    var s = new InferenceSession(model.OnnxPath, so);
                    _logger.LogInformation("ONNXRuntime: using {label}", label);
                    return s;
                }

                // CPU direkt, falls ausdrücklich gewünscht
                if (_opt.GpuBackend == YoloGpuBackend.Cpu)
                {
                    var soCpu = new SessionOptions { GraphOptimizationLevel = _opt.Optimization };
                    _logger.LogInformation("CPU requested explicitly.");
                    return CreateWith(soCpu, "CPUExecutionProvider");
                }

                // Auto-Modus: Empfohlenes Backend verwenden
                var targetBackend = _opt.GpuBackend;
                if (targetBackend == YoloGpuBackend.Auto)
                {
                    targetBackend = GetRecommendedBackend();
                }

                Exception? lastCudaError = null;
                Exception? lastDirectMLError = null;

                // 1) CUDA versuchen (bei Auto/Cuda und wenn verfügbar)
                if ((targetBackend == YoloGpuBackend.Cuda || targetBackend == YoloGpuBackend.Auto) &&
                    IsCudaAvailable())
                {
                    try
                    {
                        var soCuda = new SessionOptions { GraphOptimizationLevel = _opt.Optimization };
                        soCuda.AppendExecutionProvider_CUDA();
                        _logger.LogInformation("Using CUDAExecutionProvider (detected as available)");
                        return CreateWith(soCuda, "CUDAExecutionProvider");
                    }
                    catch (Exception ex)
                    {
                        lastCudaError = ex;
                        _logger.LogWarning(ex, "CUDA EP failed despite being detected as available");
                    }
                }
                else if (targetBackend == YoloGpuBackend.Cuda)
                {
                    _logger.LogWarning("CUDA requested but not available on this system");
                }

                // 2) DirectML versuchen (bei Auto/DirectML und wenn verfügbar)
                if ((targetBackend == YoloGpuBackend.DirectML || targetBackend == YoloGpuBackend.Auto) &&
                    IsDirectMLAvailable())
                {
                    try
                    {
                        _logger.LogInformation("Using DirectML GPU acceleration");
                        var session = DirectMLHelper.CreateDirectMLSession(model.OnnxPath, _opt.Optimization, _logger);
                        _logger.LogInformation("DirectML session created successfully");
                        return session;
                    }
                    catch (Exception ex)
                    {
                        lastDirectMLError = ex;
                        _logger.LogWarning(ex, "DirectML failed, trying fallback options");
                        
                        // Versuche CUDA als Fallback für DirectML
                        if (IsCudaAvailable() && lastCudaError == null)
                        {
                            try
                            {
                                _logger.LogInformation("Trying CUDA as fallback for DirectML failure");
                                var soCuda = new SessionOptions { GraphOptimizationLevel = _opt.Optimization };
                                soCuda.AppendExecutionProvider_CUDA();
                                return CreateWith(soCuda, "CUDAExecutionProvider (DirectML fallback)");
                            }
                            catch (Exception cudaFallbackEx)
                            {
                                _logger.LogWarning(cudaFallbackEx, "CUDA fallback also failed");
                                lastCudaError = cudaFallbackEx;
                            }
                        }
                    }
                }
                else if (targetBackend == YoloGpuBackend.DirectML)
                {
                    _logger.LogWarning("DirectML requested but not available on this system");
                }

                // 3) CPU (Fallback)
                try
                {
                    var soCpu = new SessionOptions { GraphOptimizationLevel = _opt.Optimization };
                    _logger.LogInformation("Using CPUExecutionProvider (fallback)");
                    return CreateWith(soCpu, "CPUExecutionProvider");
                }
                catch (Exception cpuEx)
                {
                    var allErrors = new List<Exception>();
                    if (lastCudaError != null) allErrors.Add(lastCudaError);
                    if (lastDirectMLError != null) allErrors.Add(lastDirectMLError);
                    allErrors.Add(cpuEx);

                    throw new InvalidOperationException(
                        "Konnte keine ONNX Runtime Session erstellen (alle Execution Provider fehlgeschlagen). " +
                        "Bitte überprüfen Sie die Installation der ONNX Runtime.",
                        new AggregateException(allErrors));
                }
            }, ct);
        }

        public async Task<IDetectionResult?> DetectAsync(
            string modelKey,
            string objectName,
            Bitmap bitmap,
            float threshold,
            Rectangle? roi = null,
            CancellationToken ct = default)
        {
            var (model, session, labels) = await GetOrCreateAsync(modelKey, ct).ConfigureAwait(false);

            // Input-Name holen ohne LINQ
            string? inputName = null;
            foreach (var k in session.InputMetadata.Keys) { inputName = k; break; }
            if (inputName is null)
                return new DetectionResult { Success = false };

            int classId = IndexOfLabel(labels, objectName);
            if (classId < 0)
                return new DetectionResult { Success = false };

            // ROI clampen
            var full = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var useRoi = roi.HasValue ? Rectangle.Intersect(roi.Value, full) : full;
            if (useRoi.Width <= 0 || useRoi.Height <= 0)
                return new DetectionResult { Success = false };

            // InputSize stride-ausrichten (z. B. 32)
            int align = 32;
            var inputSize = Math.Max(align, ((_opt.InputSize + (align - 1)) / align) * align);

            // Preprocessing: direkt ROI -> Letterbox -> Tensor
            var (tensor, scale, padX, padY) = LetterboxToTensor(bitmap, useRoi, inputSize);

            // Inferenz
            var input = NamedOnnxValue.CreateFromTensor(inputName, tensor); // bei manchen ORT-Versionen NICHT IDisposable
            using var results = session.Run(new[] { input });
            using var first = results.First();
            var outTensor = first.AsTensor<float>();

            // C#12: Span vermeiden
            int[] dims = outTensor.Dimensions.ToArray();
            var (isCHW, num, attrs) = InterpretDims(dims);

            // Nur Top-1 der gewünschten Klasse dekodieren
            var bestList = DecodeDetections(
                outTensor, isCHW, num, attrs, threshold,
                inputSize, scale, padX, padY,
                roiOffsetInImage: useRoi.Location,
                requiredClassId: classId);

            if (bestList.Count == 0)
                return new DetectionResult { Success = false };

            var best = bestList[0]; // DecodeDetections liefert bereits nur Top-1
            return new DetectionResult
            {
                Success = true,
                Confidence = best.Confidence,
                BoundingBox = Rectangle.Round(best.BoundingBox!.Value),
                CenterPoint = best.CenterPoint,
            };
        }

        // ------------ Cache & Initialisierung ------------

        private Task<(YOLOModel model, InferenceSession session, IReadOnlyList<string> labels)> GetOrCreateAsync(string modelKey, CancellationToken ct)
        {
            var lazy = _cache.GetOrAdd(modelKey, key => new Lazy<Task<(YOLOModel, InferenceSession, IReadOnlyList<string>)>>(
                () => CreateAsync(key, ct), LazyThreadSafetyMode.ExecutionAndPublication));
            return lazy.Value;
        }

        private async Task<(YOLOModel model, InferenceSession session, IReadOnlyList<string> labels)> CreateAsync(string modelKey, CancellationToken ct)
        {
            var model = await _downloader.DownloadModelAsync(modelKey, ct).ConfigureAwait(false);

            var session = await CreateSessionAsync(model, ct).ConfigureAwait(false);
            var labelList = _labels.GetLabels(modelKey, model.OnnxPath);

            return (model, session, labelList);
        }

        // ------------ Utilities ------------

        private static int IndexOfLabel(IReadOnlyList<string> labels, string name)
        {
            for (int i = 0; i < labels.Count; i++)
                if (string.Equals(labels[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary> Letterbox-Resize (Proportionen halten) → NCHW Float32 [0..1] </summary>
        private static (DenseTensor<float> input, float scale, float padX, float padY)
        LetterboxToTensor(Bitmap src, Rectangle srcRect, int inputSize)
        {
            // Skalierung & Padding berechnen
            int w0 = srcRect.Width, h0 = srcRect.Height;
            float scale = Math.Min((float)inputSize / w0, (float)inputSize / h0);
            int nw = (int)Math.Round(w0 * scale);
            int nh = (int)Math.Round(h0 * scale);
            float padX = (inputSize - nw) * 0.5f;
            float padY = (inputSize - nh) * 0.5f;

            // Canvas erzeugen und ROI direkt skaliert zeichnen
            using var canvas = new Bitmap(inputSize, inputSize, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Black);
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                var dst = new Rectangle((int)padX, (int)padY, nw, nh);
                g.DrawImage(src, dst, srcRect, GraphicsUnit.Pixel);
            }

            // Tensor anlegen (NCHW)
            var tensor = new DenseTensor<float>(new[] { 1, 3, inputSize, inputSize });

            // Canvas -> Tensor kopieren
            var bd = canvas.LockBits(new Rectangle(0, 0, inputSize, inputSize),
                                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                    canvas.PixelFormat);
            try
            {
                unsafe
                {
                    byte* basePtr = (byte*)bd.Scan0;
                    int stride = bd.Stride;
                    int plane = inputSize * inputSize;
                    var buf = tensor.Buffer.Span;

                    for (int y = 0; y < inputSize; y++)
                    {
                        byte* row = basePtr + y * stride;
                        int rowBase = y * inputSize;
                        for (int x = 0; x < inputSize; x++)
                        {
                            int idx = x * 3;
                            byte b = row[idx + 0];
                            byte gch = row[idx + 1];
                            byte rch = row[idx + 2];

                            int p = rowBase + x;
                            buf[0 * plane + p] = rch * (1f / 255f);
                            buf[1 * plane + p] = gch * (1f / 255f);
                            buf[2 * plane + p] = b   * (1f / 255f);
                        }
                    }
                }
            }
            finally
            {
                canvas.UnlockBits(bd);
            }

            return (tensor, scale, padX, padY);
        }

        private static (bool isCHW, int num, int attrs) InterpretDims(int[] dims)
        {
            // (1, 84, N)  -> isCHW = true,  num=N, attrs=84
            // (1, N, 84)  -> isCHW = false, num=N, attrs=84
            if (dims.Length != 3 || dims[0] != 1)
                throw new NotSupportedException($"Unerwartete Output-Shape: [{string.Join(",", dims)}]");

            bool chw;
            int num, attrs;
            if (dims[1] < dims[2]) { chw = true; attrs = dims[1]; num = dims[2]; }
            else { chw = false; num = dims[1]; attrs = dims[2]; }

            if (attrs < 5) throw new NotSupportedException("YOLO-Ausgabe mit <5 Attributen wird nicht unterstützt.");
            return (chw, num, attrs);
        }

        private List<IDetectionResult> DecodeDetections(
            Tensor<float> oTensor,
            bool isCHW,
            int num,              // Anzahl Kandidaten
            int attrs,            // 4(+1) + numClasses
            float threshold,
            int inputSize,
            float scale,
            float padX,
            float padY,
            Point roiOffsetInImage,
            int requiredClassId)
        {
            // Layout: [cx,cy,w,h,(obj?), class0..classN-1]
            int numClasses = attrs - 4;
            bool hasObj = (attrs == numClasses + 5);
            int baseCls = hasObj ? 5 : 4;

            float bestS = -1f;
            RectangleF bestBox = default;
            bool found = false;

            if (isCHW)
            {
                // [1, attrs, num]
                int clsOff = baseCls + requiredClassId;
                for (int k = 0; k < num; k++)
                {
                    float cx = oTensor[0, 0, k];
                    float cy = oTensor[0, 1, k];
                    float w  = oTensor[0, 2, k];
                    float h  = oTensor[0, 3, k];

                    float s = oTensor[0, clsOff, k];
                    if (hasObj) s *= oTensor[0, 4, k]; // obj * class
                    if (s < threshold) continue;

                    if (s > bestS)
                    {
                        var boxImg = ReverseLetterboxToImageSpace(cx, cy, w, h, inputSize, scale, padX, padY);
                        bestBox = new RectangleF(boxImg.X + roiOffsetInImage.X, boxImg.Y + roiOffsetInImage.Y, boxImg.Width, boxImg.Height);
                        bestS = s;
                        found = true;
                    }
                }
            }
            else
            {
                // [1, num, attrs]
                int clsOff = baseCls + requiredClassId;
                for (int k = 0; k < num; k++)
                {
                    float cx = oTensor[0, k, 0];
                    float cy = oTensor[0, k, 1];
                    float w  = oTensor[0, k, 2];
                    float h  = oTensor[0, k, 3];

                    float s = oTensor[0, k, clsOff];
                    if (hasObj) s *= oTensor[0, k, 4];
                    if (s < threshold) continue;

                    if (s > bestS)
                    {
                        var boxImg = ReverseLetterboxToImageSpace(cx, cy, w, h, inputSize, scale, padX, padY);
                        bestBox = new RectangleF(boxImg.X + roiOffsetInImage.X, boxImg.Y + roiOffsetInImage.Y, boxImg.Width, boxImg.Height);
                        bestS = s;
                        found = true;
                    }
                }
            }

            if (!found) return new List<IDetectionResult>(0);

            var center = new Point(
                (int)Math.Round(bestBox.X + bestBox.Width * 0.5f),
                (int)Math.Round(bestBox.Y + bestBox.Height * 0.5f));

            return new List<IDetectionResult>(1)
            {
                new DetectionResult
                {
                    Success = true,
                    Confidence = bestS,
                    BoundingBox = Rectangle.Round(bestBox),
                    CenterPoint = center,
                }
            };
        }

        private static RectangleF ReverseLetterboxToImageSpace(
            float cx, float cy, float w, float h,
            int inputSize, float scale, float padX, float padY)
        {
            float x1 = (cx - w / 2f - padX) / scale;
            float y1 = (cy - h / 2f - padY) / scale;
            float x2 = (cx + w / 2f - padX) / scale;
            float y2 = (cy + h / 2f - padY) / scale;

            if (x2 < x1) (x1, x2) = (x2, x1);
            if (y2 < y1) (y1, y2) = (y2, y1);

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        private static List<(RectangleF box, int cls, float conf)> Nms(
            List<(RectangleF box, int cls, float conf)> dets, float iouThresh)
        {
            var result = new List<(RectangleF, int, float)>(dets.Count);
            var order = dets.OrderByDescending(d => d.conf).ToList();

            while (order.Count > 0)
            {
                var best = order[0];
                result.Add(best);
                order.RemoveAt(0);
                order.RemoveAll(d => IoU(best.box, d.box) > iouThresh);
            }
            return result;
        }

        private static float IoU(RectangleF a, RectangleF b)
        {
            float x1 = Math.Max(a.Left, b.Left);
            float y1 = Math.Max(a.Top, b.Top);
            float x2 = Math.Min(a.Right, b.Right);
            float y2 = Math.Min(a.Bottom, b.Bottom);

            float inter = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            if (inter <= 0) return 0f;

            float union = a.Width * a.Height + b.Width * b.Height - inter;
            if (union <= 0) return 0f;

            return inter / union;
        }

        public void Dispose()
        {
            foreach (var kv in _cache)
            {
                if (kv.Value.IsValueCreated)
                {
                    try
                    {
                        var t = kv.Value.Value;
                        if (t.IsCompletedSuccessfully)
                        {
                            t.Result.session.Dispose();
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            GC.SuppressFinalize(this);
        }

        public bool UnloadModel(string modelKey)
        {
            if (_cache.TryRemove(modelKey, out var lazy) && lazy.IsValueCreated)
            {
                try
                {
                    var task = lazy.Value;
                    if (task.IsCompletedSuccessfully)
                    {
                        task.Result.session.Dispose();
                        return true;
                    }
                }
                catch
                {
                    // optional: loggen
                }
            }
            return false;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        // Einfache GPU-Backend-Erkennung
        private static bool IsCudaAvailable()
        {
            try
            {
                using var options = new SessionOptions();
                options.AppendExecutionProvider_CUDA();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsDirectMLAvailable()
        {
            try
            {
                using var options = new SessionOptions();
                options.AppendExecutionProvider_DML();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static YoloGpuBackend GetRecommendedBackend()
        {
            if (IsCudaAvailable())
                return YoloGpuBackend.Cuda;
            if (IsDirectMLAvailable())
                return YoloGpuBackend.DirectML;
            return YoloGpuBackend.Cpu;
        }
    }
}

