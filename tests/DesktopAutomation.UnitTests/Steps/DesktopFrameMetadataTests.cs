using System.Diagnostics;
using ImageCapture.DesktopDuplication;

namespace TaskAutomation.Tests.Steps;

public sealed class DesktopFrameMetadataTests
{
    [Fact]
    public void Update_PointerOnlyAndRepeatedFramesPreserveImageIdentityAndTime()
    {
        var utcOrigin = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        const long origin = 1_000;
        var metadata = new DesktopFrameMetadata(origin, utcOrigin);
        Assert.False(metadata.Update(0));
        Assert.Equal(0, metadata.FrameVersion);
        Assert.True(metadata.Update(origin + Stopwatch.Frequency));
        var version = metadata.FrameVersion;
        var timestamp = metadata.FrameTimestamp;
        Assert.Equal(utcOrigin.AddSeconds(1), metadata.CaptureTimestampUtc);

        Assert.False(metadata.Update(0));
        Assert.False(metadata.Update(timestamp));
        Assert.False(metadata.Update(timestamp - 1));
        Assert.Equal(version, metadata.FrameVersion);
        Assert.Equal(timestamp, metadata.FrameTimestamp);
        Assert.Equal(utcOrigin.AddSeconds(1), metadata.CaptureTimestampUtc);

        Assert.True(metadata.Update(origin + 2 * Stopwatch.Frequency));
        Assert.True(metadata.FrameVersion > version);
        Assert.Equal(utcOrigin.AddSeconds(2), metadata.CaptureTimestampUtc);
    }
}
