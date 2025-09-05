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
    public enum YoloGpuBackend { Cpu, DirectML, Cuda }

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
                var so = new SessionOptions
                {
                    GraphOptimizationLevel = _opt.Optimization
                };

                try
                {
                    switch (_opt.GpuBackend)
                    {
                        case YoloGpuBackend.DirectML:
                            so.AppendExecutionProvider_DML();
                            _logger.LogInformation("Using DirectML execution provider.");
                            break;

                        case YoloGpuBackend.Cuda:
#if WINDOWS
                            so.AppendExecutionProvider_CUDA();
                            _logger.LogInformation("Using CUDA execution provider.");
#else
                    _logger.LogWarning("CUDA backend requested but not supported on this platform. Falling back to CPU.");
#endif
                            break;

                        case YoloGpuBackend.Cpu:
                        default:
                            _logger.LogInformation("Using CPU execution provider.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GPU-Backend {backend} konnte nicht initialisiert werden – Fallback auf CPU.", _opt.GpuBackend);
                }

                return new InferenceSession(model.OnnxPath, so);
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

            // Input-Name ermitteln (zur Sicherheit)
            var inputName = session.InputMetadata.Keys.First(); // meist "images"
            var classId = IndexOfLabel(labels, objectName);
            if (classId < 0) return new DetectionResult { Success = false };

            // ROI clampen + Crop
            var full = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var useRoi = roi.HasValue ? Rectangle.Intersect(roi.Value, full) : full;
            if (useRoi.Width <= 0 || useRoi.Height <= 0) return new DetectionResult { Success = false };
            using var crop = bitmap.Clone(useRoi, bitmap.PixelFormat);

            // Preprocessing
            var inputSize = _opt.InputSize;
            var (tensor, scale, padX, padY) = LetterboxToTensor(crop, inputSize);

            // Inferenz
            var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };
            using var results = session.Run(inputs);
            var first = results.First();
            var outTensor = first.AsTensor<float>();
            var dims = outTensor.Dimensions.ToArray(); // z.B. [1,84,N] oder [1,N,84]
            var (isCHW, num, attrs) = InterpretDims(dims);

            // Decode + NMS für genau DIE gewünschte Klasse (classId)
            var dets = DecodeDetections(
                outTensor, isCHW, num, attrs, threshold,
                inputSize, scale, padX, padY,
                roiOffsetInImage: useRoi.Location,
                requiredClassId: classId);

            if (dets.Count == 0) return new DetectionResult { Success = false };

            var best = dets.OrderByDescending(d => d.Confidence).First();
            // Desktop-Koordinate: ohne globalen Offset == identisch zur Bildkoordinate
            // (falls du den globalen Offset kennst, addierst du ihn hier drauf)
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
        private static (DenseTensor<float> input, float scale, float padX, float padY) LetterboxToTensor(Bitmap bmp, int inputSize)
        {
            // Zielgröße
            int w0 = bmp.Width, h0 = bmp.Height;
            float r = Math.Min((float)inputSize / w0, (float)inputSize / h0);
            int nw = (int)Math.Round(w0 * r);
            int nh = (int)Math.Round(h0 * r);
            int padX = (inputSize - nw) / 2;
            int padY = (inputSize - nh) / 2;

            using var resized = new Bitmap(bmp, new Size(nw, nh));
            using var canvas = new Bitmap(inputSize, inputSize, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Black);
                g.DrawImage(resized, padX, padY, nw, nh);
            }

            // BGR/RGB → NCHW float
            var tensor = new DenseTensor<float>(new[] { 1, 3, inputSize, inputSize });
            var data = tensor.Buffer.Span;

            var bd = canvas.LockBits(new Rectangle(0, 0, inputSize, inputSize),
                                     System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                     canvas.PixelFormat);
            try
            {
                int stride = bd.Stride;
                unsafe
                {
                    byte* ptr = (byte*)bd.Scan0;
                    for (int y = 0; y < inputSize; y++)
                    {
                        byte* row = ptr + y * stride;
                        for (int x = 0; x < inputSize; x++)
                        {
                            int idx = x * 3;
                            byte b = row[idx + 0];
                            byte gch = row[idx + 1];
                            byte rch = row[idx + 2];

                            // NCHW: [0]=R, [1]=G, [2]=B
                            int baseIdx = y * inputSize + x;
                            data[0 * inputSize * inputSize + baseIdx] = rch / 255f;
                            data[1 * inputSize * inputSize + baseIdx] = gch / 255f;
                            data[2 * inputSize * inputSize + baseIdx] = b / 255f;
                        }
                    }
                }
            }
            finally
            {
                canvas.UnlockBits(bd);
            }

            return (tensor, r, padX, padY);
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
            int attrs,            // 4 + numClasses
            float threshold,
            int inputSize,
            float scale,
            float padX,
            float padY,
            Point roiOffsetInImage,
            int requiredClassId)
        {
            int numClasses = attrs - 4;
            var raw = new List<(RectangleF box, int cls, float conf)>(Math.Min(num, 1000));

            if (isCHW)
            {
                // [1, attrs, num]  -> (a,k)
                for (int k = 0; k < num; k++)
                {
                    float cx = oTensor[0, 0, k];
                    float cy = oTensor[0, 1, k];
                    float w = oTensor[0, 2, k];
                    float h = oTensor[0, 3, k];

                    // Klassenmax
                    int bestC = -1; float bestS = 0f;
                    for (int c = 0; c < numClasses; c++)
                    {
                        float s = oTensor[0, 4 + c, k];
                        if (s > bestS) { bestS = s; bestC = c; }
                    }
                    if (bestC != requiredClassId) continue;
                    if (bestS < threshold) continue;

                    var boxImg = ReverseLetterboxToImageSpace(cx, cy, w, h, inputSize, scale, padX, padY);
                    // ROI-Offset zurück auf Ursprungsbild
                    boxImg = new RectangleF(boxImg.X + roiOffsetInImage.X, boxImg.Y + roiOffsetInImage.Y, boxImg.Width, boxImg.Height);
                    raw.Add((boxImg, bestC, bestS));
                }
            }
            else
            {
                // [1, num, attrs] -> (k,a)
                for (int k = 0; k < num; k++)
                {
                    float cx = oTensor[0, k, 0];
                    float cy = oTensor[0, k, 1];
                    float w = oTensor[0, k, 2];
                    float h = oTensor[0, k, 3];

                    int bestC = -1; float bestS = 0f;
                    for (int c = 0; c < numClasses; c++)
                    {
                        float s = oTensor[0, k, 4 + c];
                        if (s > bestS) { bestS = s; bestC = c; }
                    }
                    if (bestC != requiredClassId) continue;
                    if (bestS < threshold) continue;

                    var boxImg = ReverseLetterboxToImageSpace(cx, cy, w, h, inputSize, scale, padX, padY);
                    boxImg = new RectangleF(boxImg.X + roiOffsetInImage.X, boxImg.Y + roiOffsetInImage.Y, boxImg.Width, boxImg.Height);
                    raw.Add((boxImg, bestC, bestS));
                }
            }

            // NMS
            var kept = Nms(raw, _opt.NmsIou);

            // Map auf IDetectionResult
            var list = new List<IDetectionResult>(kept.Count);
            foreach (var d in kept)
            {
                var center = new Point(
                    (int)Math.Round(d.box.X + d.box.Width / 2f),
                    (int)Math.Round(d.box.Y + d.box.Height / 2f));

                list.Add(new DetectionResult
                {
                    Success = true,
                    Confidence = d.conf,
                    BoundingBox = Rectangle.Round(d.box),
                    CenterPoint = center,
                });
            }
            return list;
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
    }
}

