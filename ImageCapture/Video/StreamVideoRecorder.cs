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

    private TimedFrame? _latestFrame = null;
    private readonly object _frameLock = new();

    private string _outputDirectory, _fileName, ffmpegPath;
    private Process ffmpegProcess;
    private Stream ffmpegInput;
    private Stopwatch stopwatch;
    private CancellationTokenSource cts;
    private Task writerTask;

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Output directory cannot be null or empty.", nameof(value));
            if (!Directory.Exists(value))
            {
                Directory.CreateDirectory(value);
            }
            _outputDirectory = value;
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("File name cannot be null or empty.", nameof(value));

            _fileName = value;
        }
    }

    public StreamVideoRecorder(int width, int height, int fps, string ffmpegPath = "ffmpeg")
    {
        this.width = width;
        this.height = height;
        this.fps = fps;
        this.ffmpegPath = ffmpegPath;
        ffmpegInitTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        string bilderPfad = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.OutputDirectory = bilderPfad + "\\ImageCapture";
        this.FileName = "output.mp4";
    }

    public async Task StartAsync()
    {
        await ffmpegInitTask;

        string outputFile = VideoHelper.GetUniqueFilePath(Path.Combine(OutputDirectory, FileName));

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
        if (bmp == null || bmp.Width == 0 || bmp.Height == 0)
            return;

        try
        {
            using var mat = BitmapConverter.ToMat(bmp);
            if (mat.Empty() || mat.Data == IntPtr.Zero)
                return;

            Cv2.Resize(mat, mat, new OpenCvSharp.Size(width, height));

            int length = (int)(mat.Total() * mat.ElemSize());
            if (length <= 0)
                return;

            byte[] raw = new byte[length];
            Marshal.Copy(mat.Data, raw, 0, length);

            var frame = new TimedFrame
            {
                TimestampMs = stopwatch.ElapsedMilliseconds,
                RawFrame = raw
            };

            lock (_frameLock)
            {
                _latestFrame = frame;
            }
        }
        catch
        {
            // bei Fehlern einfach überspringen
        }
    }

    private async Task WriterLoopAsync(CancellationToken token)
    {
        long frameDuration = 1000L / fps;
        long nextTargetTime = stopwatch.ElapsedMilliseconds;
        byte[] lastFrame = null;

        try
        {
            while (!token.IsCancellationRequested)
            {
                long now = stopwatch.ElapsedMilliseconds;
                long wait = nextTargetTime - now;
                if (wait > 2)
                    await Task.Delay((int)(wait - 2), token);
                while (stopwatch.ElapsedMilliseconds < nextTargetTime) {}

                TimedFrame? slotFrame;
                lock (_frameLock)
                {
                    slotFrame = _latestFrame;
                    _latestFrame = null;      
                }

                var toWrite = slotFrame?.RawFrame ?? lastFrame;
                if (toWrite != null)
                {
                    ffmpegInput.Write(toWrite, 0, toWrite.Length);
                    lastFrame = toWrite;
                }

                nextTargetTime += frameDuration;
            }
        }
        catch (OperationCanceledException)
        {
            // normaler Stopp
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
