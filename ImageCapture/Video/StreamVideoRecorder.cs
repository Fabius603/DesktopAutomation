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

public class StreamVideoRecorder : IVideoRecorder
{
    // interner PST‐Frame
    private struct TimedFrame
    {
        public long TimestampMs;
        public byte[] RawFrame;
    }

    private readonly int width, height, fps;
    private static readonly Lazy<Task> SharedInitialization = new(() => FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official));
    private readonly Task? ffmpegInitTask;
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    private TimedFrame? _latestFrame = null;
    private readonly object _frameLock = new();

    private string _outputDirectory, _fileName, ffmpegPath;
    private Process ffmpegProcess;
    private Stream ffmpegInput;
    private Stopwatch stopwatch;
    private CancellationTokenSource cts;
    private Task writerTask;
    private readonly SemaphoreSlim stopLock = new(1, 1);
    private bool isStarted;
    private bool isStopped;
    private long submittedFrameCount;

    public string? OutputFilePath { get; private set; }
    public long SubmittedFrameCount => Interlocked.Read(ref submittedFrameCount);

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Output directory cannot be null or empty.", nameof(value));
            Directory.CreateDirectory(value);
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

    public StreamVideoRecorder(int width, int height, int fps, string ffmpegPath = "ffmpeg", Task? initialization = null,
        Func<ProcessStartInfo, Process?>? startProcess = null)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        if (fps <= 0)
            throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be greater than zero.");

        this.width = width;
        this.height = height;
        this.fps = fps;
        this.ffmpegPath = ffmpegPath;
        ffmpegInitTask = initialization ?? (File.Exists(ffmpegPath) ? Task.CompletedTask : null);
        _startProcess = startProcess ?? Process.Start;
        string bilderPfad = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.OutputDirectory = bilderPfad + "\\ImageCapture";
        this.FileName = "output.mp4";
    }

    /// <summary>
    /// Startet die Aufnahme und WriterLoop im Hintergrund.
    /// </summary>
    public async Task StartAsync(CancellationToken token)
    {
        // Warte auf ffmpeg-Pfad
        token.ThrowIfCancellationRequested();
        await (ffmpegInitTask ?? SharedInitialization.Value).WaitAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (isStarted) throw new InvalidOperationException("Recording has already started.");

        string outputFile = VideoHelper.GetUniqueFilePath(Path.Combine(OutputDirectory, FileName));
        OutputFilePath = outputFile;

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-y -f rawvideo -pix_fmt bgr24 -s {width}x{height} -r {fps} -i - -c:v libx264 -preset ultrafast -pix_fmt yuv420p \"{outputFile}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
        token.ThrowIfCancellationRequested();
        ffmpegProcess = _startProcess(psi)
            ?? throw new InvalidOperationException("ffmpeg process could not be started.");
        ffmpegInput = ffmpegProcess.StandardInput.BaseStream;

        // Starte Timebase + CTS + WriterLoop
        stopwatch = Stopwatch.StartNew();
        cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        writerTask = Task.Run(() => WriterLoopAsync(cts.Token), CancellationToken.None);
        isStarted = true;
    }

    /// <summary>
    /// Pusht einen neuen Frame (Bitmap wird als BGR24 sofort konvertiert).
    /// </summary>
    public void AddFrame(Bitmap bmp)
    {
        ArgumentNullException.ThrowIfNull(bmp);
        if (!isStarted || isStopped) throw new InvalidOperationException("Recording is not active.");
        if (writerTask.IsFaulted) writerTask.GetAwaiter().GetResult();
        {
            using var mat = BitmapConverter.ToMat(bmp);
            if (mat.Empty() || mat.Data == IntPtr.Zero)
                throw new InvalidOperationException("Frame could not be converted.");

            // Ensure BGR format for FFmpeg
            using var bgrMat = new Mat();
            if (mat.Channels() == 4) // BGRA or RGBA
            {
                Cv2.CvtColor(mat, bgrMat, ColorConversionCodes.BGRA2BGR);
            }
            else if (mat.Channels() == 1) // Grayscale
            {
                Cv2.CvtColor(mat, bgrMat, ColorConversionCodes.GRAY2BGR);
            }
            else if (mat.Channels() == 3) // Already BGR or RGB
            {
                // Assume it's already BGR since most Windows bitmaps are
                mat.CopyTo(bgrMat);
            }
            else
            {
                // Fallback: try to convert to BGR
                mat.CopyTo(bgrMat);
            }

            // Only resize if dimensions don't match
            if (bgrMat.Width != width || bgrMat.Height != height)
            {
                Cv2.Resize(bgrMat, bgrMat, new OpenCvSharp.Size(width, height));
            }

            int length = (int)(bgrMat.Total() * bgrMat.ElemSize());
            if (length <= 0)
                throw new InvalidOperationException("Frame contains no pixels.");

            byte[] raw = new byte[length];
            Marshal.Copy(bgrMat.Data, raw, 0, length);

            var frame = new TimedFrame
            {
                TimestampMs = stopwatch.ElapsedMilliseconds,
                RawFrame = raw
            };

            lock (_frameLock)
            {
                _latestFrame = frame;
            }
            Interlocked.Increment(ref submittedFrameCount);
        }
    }

    private async Task WriterLoopAsync(CancellationToken token)
    {
        var frameDuration = Math.Max(1, 1000L / fps);
        var nextTargetTime = stopwatch.ElapsedMilliseconds;
        byte[]? lastFrame = null;
        Exception? failure = null;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var wait = nextTargetTime - stopwatch.ElapsedMilliseconds;
                if (wait > 0) await Task.Delay((int)wait, token).ConfigureAwait(false);
                TimedFrame? frame;
                lock (_frameLock) { frame = _latestFrame; _latestFrame = null; }
                var data = frame?.RawFrame ?? lastFrame;
                if (data is not null)
                {
                    await ffmpegInput.WriteAsync(data, token).ConfigureAwait(false);
                    lastFrame = data;
                }
                nextTargetTime += frameDuration;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error) { failure = error; }
        finally
        {
            try
            {
                TimedFrame? pending;
                lock (_frameLock) { pending = _latestFrame; _latestFrame = null; }
                var final = pending?.RawFrame ?? lastFrame;
                if (failure is null && final is not null)
                    await ffmpegInput.WriteAsync(final, CancellationToken.None).ConfigureAwait(false);
                await ffmpegInput.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception error) { failure ??= error; }
            finally { ffmpegInput.Dispose(); }
            await ffmpegProcess.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            if (ffmpegProcess.ExitCode != 0)
                failure = new InvalidOperationException($"FFmpeg exited with code {ffmpegProcess.ExitCode}.", failure);
        }
        if (failure is not null) throw new IOException("Video recording could not be saved.", failure);
        if (submittedFrameCount > 0 && (OutputFilePath is null || !File.Exists(OutputFilePath) || new FileInfo(OutputFilePath).Length == 0))
            throw new IOException("FFmpeg did not create the output file.");
    }

    /// <summary>
    /// Stoppt Capture und speichert Video (non-blocking).
    /// </summary>
    public Task StopAndSave() => StopAndSave(CancellationToken.None);
    public async Task StopAndSave(CancellationToken token)
    {
        if (!isStarted || writerTask == null || cts == null)
            return;

        using var stopRegistration = token.Register(() =>
        {
            try { if (ffmpegProcess is { HasExited: false }) ffmpegProcess.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        });
        await stopLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!isStopped)
            {
                isStopped = true;
                cts.Cancel();
            }
            await writerTask.ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
        }
        finally
        {
            stopLock.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            StopAndSave().GetAwaiter().GetResult();
        }
        catch
        {
            // Dispose darf keine Job-Ausführung nachträglich abbrechen.
        }
        cts?.Dispose();
        ffmpegProcess?.Dispose();
        stopLock.Dispose();
    }
}
