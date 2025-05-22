using ImageCapture.Video;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xabe.FFmpeg.Downloader;

public class StreamedVideoRecorder : IDisposable
{
    private Process ffmpegProcess;
    private Stream ffmpegInput;
    private bool isStarted = false;

    private readonly int width;
    private readonly int height;
    private readonly int fps;
    private readonly string outputPath;
    private readonly string ffmpegPath;

    private readonly Task ffmpegInitTask;

    public StreamedVideoRecorder(int width, int height, int fps, string outputPath, string ffmpegPath = "ffmpeg")
    {
        this.width = width;
        this.height = height;
        this.fps = fps;
        this.outputPath = VideoHelper.GetUniqueFilePath(outputPath);
        this.ffmpegPath = ffmpegPath;
        this.ffmpegInitTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
    }

    public async Task Start()
    {
        if (isStarted) throw new InvalidOperationException("Recorder already started.");

        await ffmpegInitTask;

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-y -f rawvideo -pix_fmt bgr24 -s {width}x{height} -r {fps} -i - -c:v libx264 -preset ultrafast -pix_fmt yuv420p \"{outputPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        ffmpegProcess = new Process();
        ffmpegProcess.StartInfo = psi;
        ffmpegProcess.Start();

        ffmpegInput = ffmpegProcess.StandardInput.BaseStream;
        isStarted = true;

        // Optional: Fehlerausgabe überwachen
        _ = Task.Run(() => {
            string error = ffmpegProcess.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine("[ffmpeg stderr] " + error);
        });
    }

    public void AddFrame(Bitmap bitmap)
    {
        if (!isStarted || ffmpegInput == null || ffmpegProcess?.HasExited == true)
            return;

        using var mat = BitmapConverter.ToMat(bitmap); // Schnell, kein GDI+
        Cv2.Resize(mat, mat, new OpenCvSharp.Size(width, height)); // Wenn nötig

        if (mat.Type() != MatType.CV_8UC3)
            throw new ArgumentException("Bitmap must be BGR24 compatible");

        // Direktes Schreiben der rohen Daten
        byte[] rawFrame = new byte[mat.Total() * mat.ElemSize()];
        Marshal.Copy(mat.Data, rawFrame, 0, rawFrame.Length);

        ffmpegInput.Write(rawFrame, 0, rawFrame.Length);
    }

    public async Task StopAndSaveAsync()
    {
        if (!isStarted || ffmpegProcess == null)
            return;

        try
        {
            await ffmpegInput.FlushAsync();
            ffmpegInput.Close();
            await ffmpegProcess.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Stoppen des Recorders: {ex.Message}");
        }
        finally
        {
            ffmpegInput = null;
            ffmpegProcess?.Dispose();
            ffmpegProcess = null;
            isStarted = false;
        }
    }


    public void Dispose()
    {
        if (ffmpegInput != null)
        {
            ffmpegInput.Dispose();
            ffmpegInput = null;
        }
        if (ffmpegProcess != null)
        {
            ffmpegProcess.Dispose();
            ffmpegProcess = null;
        }
    }
}
