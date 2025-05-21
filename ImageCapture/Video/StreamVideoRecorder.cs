using ImageCapture.Video;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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

    public StreamedVideoRecorder(int width, int height, int fps, string outputPath, string ffmpegPath = "ffmpeg")
    {
        this.width = width;
        this.height = height;
        this.fps = fps;
        this.outputPath = VideoHelper.GetUniqueFilePath(outputPath);
        this.ffmpegPath = ffmpegPath;
    }

    public void Start()
    {
        if (isStarted) throw new InvalidOperationException("Recorder already started.");
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
        if (!isStarted) throw new InvalidOperationException("Recorder not started.");

        bitmap = ResizeBitmap(bitmap);

        if (bitmap.Width != width || bitmap.Height != height)
            throw new ArgumentException("Bitmap size does not match initialized resolution.");

        byte[] rawFrame = BitmapToBgr24(bitmap);
        ffmpegInput.Write(rawFrame, 0, rawFrame.Length);
    }

    public void StopAndSaveAsync()
    {
        _ = Task.Run(async () =>
        {
            if (!isStarted || ffmpegProcess == null)
                return;

            try
            {
                await ffmpegInput.FlushAsync();
                ffmpegInput.Close();
                await ffmpegProcess.WaitForExitAsync();
                ffmpegProcess.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Stoppen des Recorders: {ex.Message}");
            }
            finally
            {
                ffmpegInput = null;
                ffmpegProcess = null;
                isStarted = false;
            }
        });
    }


    private Bitmap ResizeBitmap(Bitmap source)
    {
        var resized = new Bitmap(width, height);
        using var g = Graphics.FromImage(resized);
        g.DrawImage(source, 0, 0, width, height);
        return resized;
    }

    private static byte[] BitmapToBgr24(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        int byteCount = data.Stride * data.Height;
        byte[] buffer = new byte[byteCount];
        Marshal.Copy(data.Scan0, buffer, 0, byteCount);
        bmp.UnlockBits(data);
        return buffer;
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
