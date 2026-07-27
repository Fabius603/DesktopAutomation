using System.Drawing;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed record CameraDeviceInfo(string Id, string Name, int Index);

public sealed record CameraCaptureMode(int Width, int Height, double FramesPerSecond, string PixelFormat);

public sealed record CameraCaptureOptions(
    CameraQualityMode QualityMode,
    int Width = 0,
    int Height = 0,
    double FramesPerSecond = 0,
    string PixelFormat = "");

public sealed record CameraCaptureFrame(Bitmap Image, DateTime CaptureTimestampUtc);

public interface ICameraCaptureService : IDisposable
{
    IReadOnlyList<CameraDeviceInfo> GetAvailableCameras();
    IReadOnlyList<CameraCaptureMode> GetSupportedModes(string cameraId);
    Task<CameraCaptureFrame> CaptureAsync(
        string cameraId,
        CameraCaptureOptions options,
        CancellationToken cancellationToken);
}
