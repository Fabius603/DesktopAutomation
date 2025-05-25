using ImageCapture.Video;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg.Downloader;

public class StreamVideoRecorder : IDisposable
{
    // interner PST‐Frame
    private struct TimedFrame
    {
        public long TimestampMs;
        public byte[] RawFrame;
    }

    private readonly int width, height, fps;
    private readonly Task ffmpegInitTask;

    private readonly List<TimedFrame> buffer = new();
    private readonly object bufLock = new();

    private string outputPath, fileName, ffmpegPath;
    private Process ffmpegProcess;
    private Stream ffmpegInput;
    private Stopwatch stopwatch;
    private CancellationTokenSource cts;
    private Task writerTask;

    public StreamVideoRecorder(int width, int height, int fps, string ffmpegPath = "ffmpeg")
    {
        this.width = width;
        this.height = height;
        this.fps = fps;
        this.ffmpegPath = ffmpegPath;
        ffmpegInitTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        string bilderPfad = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.outputPath = bilderPfad + "\\ImageDetection";
        this.fileName = "output.mp4";
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
    }

    public async Task StartAsync()
    {
        await ffmpegInitTask;

        string outputFile = VideoHelper.GetUniqueFilePath(Path.Combine(outputPath, fileName));

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-y -f rawvideo -pix_fmt bgr24 -s {width}x{height} -r {fps} -i - -c:v libx264 -preset ultrafast -pix_fmt yuv420p \"{outputFile}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
        ffmpegProcess = Process.Start(psi);
        ffmpegInput = ffmpegProcess.StandardInput.BaseStream;

        // Start timebase
        stopwatch = Stopwatch.StartNew();
        cts = new CancellationTokenSource();
        writerTask = Task.Run(() => WriterLoopAsync(cts.Token));
    }

    /// <summary>
    /// Pusht einen neuen Frame (Bitmap wird als BGR24 sofort konvertiert).
    /// </summary>
    public void AddFrame(Bitmap bmp)
    {
        Task.Run(() =>
        {
            // Konvertieren & resizen ohne Lock
            using var mat = BitmapConverter.ToMat(bmp);
            Cv2.Resize(mat, mat, new OpenCvSharp.Size(width, height));
            byte[] rawFrame = new byte[mat.Total() * mat.ElemSize()];
            Marshal.Copy(mat.Data, rawFrame, 0, rawFrame.Length);

            var frame = new TimedFrame
            {
                TimestampMs = stopwatch.ElapsedMilliseconds,
                RawFrame = rawFrame
            };

            lock (bufLock)
            {
                buffer.Add(frame);
            }
        });
    }

    public void SetOutputPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(path));
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        outputPath = VideoHelper.GetUniqueFilePath(path);
    }

    public void SetFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("File name cannot be null or empty.", nameof(name));
        fileName = name;
    }

    private async Task WriterLoopAsync(CancellationToken token)
    {
        long frameDuration = 1000L / fps;
        long nextTargetTime = 0;
        byte[] lastFrame = null;

        try
        {
            while (!token.IsCancellationRequested)
            {
                long now = stopwatch.ElapsedMilliseconds;
                if (now < nextTargetTime)
                {
                    int delay = (int)(nextTargetTime - now);
                    if (delay > 1)
                        await Task.Delay(delay - 1, token);
                    while (stopwatch.ElapsedMilliseconds < nextTargetTime) { /* busy wait */ }
                }

                TimedFrame? selected = null;
                lock (bufLock)
                {
                    int index = buffer.FindIndex(f => f.TimestampMs >= nextTargetTime);
                    if (index >= 0)
                    {
                        selected = buffer[index];
                        buffer.RemoveRange(0, index + 1);
                    }
                    else if (buffer.Count > 0)
                    {
                        selected = buffer[^1]; // letzter Frame
                        buffer.Clear();
                    }
                }

                var currentFrame = selected?.RawFrame ?? lastFrame;
                if (currentFrame != null)
                {
                    ffmpegInput.Write(currentFrame, 0, currentFrame.Length);
                    lastFrame = currentFrame;
                }

                nextTargetTime += frameDuration;
            }
        }
        catch (OperationCanceledException)
        {
            // normaler Abbruch
        }
        finally
        {
            try
            {
                ffmpegInput.Flush();
                ffmpegInput.Close();
                await ffmpegProcess.WaitForExitAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Recorder] Fehler beim Beenden von ffmpeg: {ex.Message}");
            }
        }
    }

    public string GetOutputPath()
    {
        return outputPath;
    }


    /// <summary>
    /// Stoppt Capture und speichert Video (non-blocking).
    /// </summary>
    public async Task StopAndSave()
    {
        cts.Cancel();
        await writerTask;
    }

    public void Dispose()
    {
        StopAndSave();
        writerTask?.Wait();
        ffmpegProcess?.Dispose();
    }
}
