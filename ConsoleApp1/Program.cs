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

class Program
{
    [STAThread]
    static async Task Main(string[] args)
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
}
