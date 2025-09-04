using ImageCapture.DesktopDuplication;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SharpDX;
using SharpDX.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using ImageCapture.ProcessDuplication;
using ImageCapture.Video;
using System.Diagnostics;
using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;
using Common.Logging;
using TaskAutomation.Hotkeys;
using TaskAutomation.Orchestration;
using DesktopOverlay;
using GameOverlay.Drawing;
using ImageDetection.YOLO;
using ImageDetection.Model;
using Microsoft.ML.OnnxRuntime;
using System.Drawing;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        //await Download();
        await Execute();
    }

    static async Task Download()
    {
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<YOLOModelDownloader>();

        var downloader = new YOLOModelDownloader(logger, new HttpClient());

        downloader.DownloadProgressChanged += (e, sender) =>
        {
            Console.WriteLine($"[{sender.Status}] {sender.ModelName}: {sender.ProgressPercent}% {sender.Message}");
        };

        var cts = new CancellationTokenSource();

        try
        {
            var model = await downloader.DownloadModelAsync("yolov8n", cts.Token);
            Console.WriteLine($"Fertig: {model.Name} @ {model.OnnxPath}, Größe: {model.OnnxSizeBytes} Bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download fehlgeschlagen: {ex.Message}");
        }
    }

    static async Task Execute()
    {
        var modelKey = "yolov8n";
        var className = "person";
        var imagePath = @"C:\Users\schlieper\OneDrive - Otto Künnecke GmbH\Pictures\trash\person.jpg";
        var threshold = 0.25f;

        if (!System.IO.File.Exists(imagePath))
        {
            Console.Error.WriteLine($"Bild nicht gefunden: {imagePath}");
        }

        var backend = YoloGpuBackend.DirectML;

        using var loggerFactory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            b.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "HH:mm:ss ";
            });
        });

        var logger = loggerFactory.CreateLogger<Program>();

        // YoloManager konfigurieren
        var downloaderLogger = loggerFactory.CreateLogger<YOLOModelDownloader>();
        var downloader = new YOLOModelDownloader(downloaderLogger);

        var options = new YoloManagerOptions
        {
            GpuBackend = backend,
            Optimization = GraphOptimizationLevel.ORT_ENABLE_ALL,
            InputSize = 640,     // ggf. an dein Modell anpassen
            NmsIou = 0.45f
        };

        using var manager = new YoloManager(
            downloader,
            loggerFactory.CreateLogger<YoloManager>(),
            options);

        // Download-/Lade-Progress loggen
        manager.DownloadProgressChanged += (model, status, pct, msg) =>
        {
            Console.WriteLine($"[Download] {model}: {status} {pct}% {(string.IsNullOrEmpty(msg) ? "" : "- " + msg)}");
        };

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var swTotal = Stopwatch.StartNew();

        try
        {
            // Modell sicherstellen (Download / Session bauen)
            var swModel = Stopwatch.StartNew();
            await manager.EnsureModelAsync(modelKey, cts.Token);
            swModel.Stop();
            logger.LogInformation("Modell bereit in {ms} ms.", swModel.ElapsedMilliseconds);

            // Bild laden
            using var bmp = new Bitmap(imagePath);

            // Inferenz
            var swInfer = Stopwatch.StartNew();
            var result = await manager.DetectAsync(
                modelKey: modelKey,
                objectName: className,
                bitmap: bmp,
                threshold: threshold,
                roi: null,
                ct: cts.Token);
            swInfer.Stop();

            swTotal.Stop();

            if (result is null || !result.Success)
            {
                Console.WriteLine($"Kein Treffer für Klasse \"{className}\". " +
                                  $"Infer: {swInfer.ElapsedMilliseconds} ms | Total: {swTotal.ElapsedMilliseconds} ms");
            }

            Console.WriteLine(
                $"Conf: {result.Confidence:F3} | " +
                $"Box: {result.BoundingBox} | " +
                $"Center: {result.CenterPoint} | " +
                $"Infer: {swInfer.ElapsedMilliseconds} ms | " +
                $"Total: {swTotal.ElapsedMilliseconds} ms");

        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Abgebrochen.");
        }
        catch (Exception ex)
        {
            swTotal.Stop();
            logger.LogError(ex, "Fehler beim Testlauf.");
        }
    }
}
