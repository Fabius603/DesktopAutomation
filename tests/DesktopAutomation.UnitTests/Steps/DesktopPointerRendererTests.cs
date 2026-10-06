using System.Drawing;
using System.Drawing.Imaging;
using ImageCapture.DesktopDuplication;
using SharpDX.DXGI;

namespace TaskAutomation.Tests.Steps;

public sealed class DesktopPointerRendererTests
{
    [Theory]
    [InlineData(OutputDuplicatePointerShapeType.Color, 255, 255)]
    [InlineData(OutputDuplicatePointerShapeType.Color, 0, 10)]
    [InlineData(OutputDuplicatePointerShapeType.Color, 128, 133)]
    [InlineData(OutputDuplicatePointerShapeType.MaskedColor, 0, 255)]
    [InlineData(OutputDuplicatePointerShapeType.MaskedColor, 255, 245)]
    public void ColorAndMaskedPointers_RespectAlphaMasksAndClipToDesktop(OutputDuplicatePointerShapeType type, int alpha, int expected)
    {
        using var image = new Bitmap(2, 2, PixelFormat.Format32bppRgb);
        image.SetPixel(0, 0, Color.FromArgb(10, 10, 10));
        var info = new OutputDuplicatePointerShapeInformation
        {
            Type = (int)type,
            Width = 1,
            Height = 1,
            Pitch = 4,
            HotSpot = new SharpDX.Mathematics.Interop.RawPoint(3, 4)
        };
        DesktopPointerRenderer.Draw(image, Point.Empty, [255, 255, 255, (byte)alpha], info);
        Assert.Equal(expected, image.GetPixel(0, 0).R);
        DesktopPointerRenderer.Draw(image, new(-1, -1), [255, 255, 255, (byte)alpha], info);
        Assert.Equal(expected, image.GetPixel(0, 0).R);
    }

    [Fact]
    public void MonochromePointer_AppliesAndXorMasks()
    {
        using var image = new Bitmap(4, 1, PixelFormat.Format32bppRgb);
        for (int x = 0; x < 4; x++) image.SetPixel(x, 0, Color.FromArgb(10, 10, 10));
        var info = new OutputDuplicatePointerShapeInformation { Type = 1, Width = 4, Height = 2, Pitch = 1 };
        DesktopPointerRenderer.Draw(image, Point.Empty, [0x30, 0x50], info);
        Assert.Equal(new[] { 0, 255, 10, 245 }, Enumerable.Range(0, 4).Select(x => (int)image.GetPixel(x, 0).R));
    }

    [Theory]
    [InlineData(DisplayModeRotation.Identity, 2, 3, 0, 0)]
    [InlineData(DisplayModeRotation.Rotate90, 3, 2, 2, 0)]
    [InlineData(DisplayModeRotation.Rotate180, 2, 3, 1, 2)]
    [InlineData(DisplayModeRotation.Rotate270, 3, 2, 0, 1)]
    public void RotatedDesktop_RotatesPixelsAndDimensions(DisplayModeRotation rotation, int width, int height, int redX, int redY)
    {
        using var image = new Bitmap(2, 3, PixelFormat.Format32bppRgb);
        image.SetPixel(0, 0, Color.Red);
        image.RotateFlip(DesktopImageRotation.ToRotateFlip(rotation));
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.Equal(Color.Red.ToArgb(), image.GetPixel(redX, redY).ToArgb());
    }
}
