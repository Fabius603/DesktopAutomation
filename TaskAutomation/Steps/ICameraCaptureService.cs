using System.Drawing;

namespace TaskAutomation.Steps;

public sealed record CameraDeviceInfo(string Id, string Name, int Index);

public sealed record CameraCaptureFrame(Bitmap Image, DateTime CaptureTimestampUtc);

public interface ICameraCaptureService : IDisposable
{
    IReadOnlyList<CameraDeviceInfo> GetAvailableCameras();
    Task<CameraCaptureFrame> CaptureAsync(string cameraId, CancellationToken cancellationToken);
}
