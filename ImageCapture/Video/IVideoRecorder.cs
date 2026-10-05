using System.Drawing;

namespace ImageCapture.Video;

public interface IVideoRecorder : IDisposable
{
    string OutputDirectory { get; set; }
    string FileName { get; set; }
    string? OutputFilePath { get; }
    long SubmittedFrameCount { get; }
    Task StartAsync(CancellationToken token);
    void AddFrame(Bitmap frame);
    Task StopAndSave();
}
