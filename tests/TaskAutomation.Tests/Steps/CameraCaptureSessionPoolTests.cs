using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class CameraCaptureSessionPoolTests
{
    [Fact]
    public void GetOrAdd_ReusesSessionPerCameraIdAndSeparatesDifferentCameras()
    {
        using var pool = new CameraCaptureSessionPool();

        var first = pool.GetOrAdd("@device:pnp:camera-a");
        var sameIgnoringCase = pool.GetOrAdd("@DEVICE:PNP:CAMERA-A");
        var second = pool.GetOrAdd("@device:pnp:camera-b");

        Assert.Same(first, sameIgnoringCase);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task Gates_SerializeSameCameraButAllowDifferentCamerasInParallel()
    {
        using var pool = new CameraCaptureSessionPool();
        var first = pool.GetOrAdd("camera-a");
        var second = pool.GetOrAdd("camera-b");

        await first.Gate.WaitAsync();
        var sameCameraWait = first.Gate.WaitAsync();
        var otherCameraWait = second.Gate.WaitAsync();

        Assert.False(sameCameraWait.IsCompleted);
        Assert.True(otherCameraWait.IsCompletedSuccessfully);

        second.Gate.Release();
        first.Gate.Release();
        await sameCameraWait;
        first.Gate.Release();
    }
}
