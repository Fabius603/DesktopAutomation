using System.Drawing;
using System.Drawing.Imaging;
using ImageDetection.Model;
using ImageDetection.YOLO;

namespace TaskAutomation.Tests.ImageDetection;

public sealed class YoloImagePreprocessorTests
{
    [Fact]
    public void Preprocess_UsesGrayLetterboxPaddingAndBilinearInterpolation()
    {
        using var source = new Bitmap(2, 1, PixelFormat.Format32bppArgb);
        source.SetPixel(0, 0, Color.Red);
        source.SetPixel(1, 0, Color.Blue);
        using var buffers = new YoloBuffers(4);

        var transform = YoloImagePreprocessor.Preprocess(
            source,
            new Rectangle(0, 0, source.Width, source.Height),
            buffers);

        Assert.Equal(2f, transform.Scale);
        Assert.Equal(0f, transform.PadX);
        Assert.Equal(1f, transform.PadY);

        var plane = 16;
        var gray = 114f / 255f;
        Assert.Equal(gray, buffers.Input[0], 5);
        Assert.Equal(gray, buffers.Input[plane], 5);
        Assert.Equal(gray, buffers.Input[2 * plane], 5);

        var blendedPixel = 1 * 4 + 1;
        Assert.InRange(buffers.Input[blendedPixel], 0.70f, 0.80f);
        Assert.InRange(buffers.Input[2 * plane + blendedPixel], 0.20f, 0.30f);
    }

    [Fact]
    public void Preprocess_ReusedBuffersContainTheNewestFrame()
    {
        using var first = new Bitmap(2, 2, PixelFormat.Format32bppArgb);
        using var second = new Bitmap(2, 2, PixelFormat.Format32bppArgb);
        using var buffers = new YoloBuffers(2);
        using (var graphics = Graphics.FromImage(first)) graphics.Clear(Color.Red);
        using (var graphics = Graphics.FromImage(second)) graphics.Clear(Color.Blue);

        YoloImagePreprocessor.Preprocess(first, new Rectangle(0, 0, 2, 2), buffers);
        var firstInput = buffers.Input.ToArray();
        YoloImagePreprocessor.Preprocess(second, new Rectangle(0, 0, 2, 2), buffers);

        Assert.NotEqual(firstInput, buffers.Input);
        Assert.All(buffers.Input.Take(4), value => Assert.InRange(value, 0f, .01f));
        Assert.All(buffers.Input.Skip(8).Take(4), value => Assert.InRange(value, .99f, 1f));
    }

    [Fact]
    public async Task InferenceGate_SerializesSharedBufferUse()
    {
        using var buffers = new YoloBuffers(2);
        await buffers.InferenceGate.WaitAsync();
        var secondEntered = false;
        var waiter = Task.Run(async () =>
        {
            await buffers.InferenceGate.WaitAsync();
            secondEntered = true;
            buffers.InferenceGate.Release();
        });

        await Task.Delay(50);
        Assert.False(secondEntered);
        buffers.InferenceGate.Release();
        await waiter;
        Assert.True(secondEntered);
    }
}
