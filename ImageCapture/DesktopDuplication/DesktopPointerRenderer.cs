using System.Drawing;
using System.Drawing.Imaging;
using SharpDX.DXGI;

namespace ImageCapture.DesktopDuplication;

/// <summary>DXGI positions describe the shape's top left, rather than its hotspot.</summary>
public static class DesktopPointerRenderer
{
    public static unsafe void Draw(Bitmap image, Point position, byte[] shape, OutputDuplicatePointerShapeInformation info)
    {
        int height = info.Type == (int)OutputDuplicatePointerShapeType.Monochrome ? info.Height / 2 : info.Height;
        if (info.Width <= 0 || height <= 0 || info.Pitch <= 0 || shape.Length < (long)info.Pitch * info.Height)
            throw new ArgumentException("Invalid DXGI pointer shape.", nameof(shape));
        bool monochrome = info.Type == (int)OutputDuplicatePointerShapeType.Monochrome;
        if (info.Type != (int)OutputDuplicatePointerShapeType.Color && info.Type != (int)OutputDuplicatePointerShapeType.MaskedColor && !monochrome)
            throw new ArgumentException("Unsupported DXGI pointer shape.", nameof(info));
        if (info.Pitch < (monochrome ? ((long)info.Width + 7) / 8 : (long)info.Width * 4))
            throw new ArgumentException("Invalid DXGI pointer pitch.", nameof(info));
        var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
        try
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < info.Width; x++)
                {
                    int dx = position.X + x, dy = position.Y + y;
                    if (dx < 0 || dy < 0 || dx >= image.Width || dy >= image.Height) continue;
                    byte* pixel = (byte*)data.Scan0 + dy * data.Stride + dx * 4;
                    if (info.Type == (int)OutputDuplicatePointerShapeType.Monochrome)
                    {
                        int bit = 0x80 >> (x & 7);
                        byte andMask = (shape[y * info.Pitch + x / 8] & bit) != 0 ? (byte)255 : (byte)0;
                        byte xorMask = (shape[(y + height) * info.Pitch + x / 8] & bit) != 0 ? (byte)255 : (byte)0;
                        for (int c = 0; c < 3; c++) pixel[c] = (byte)((pixel[c] & andMask) ^ xorMask);
                    }
                    else
                    {
                        int offset = y * info.Pitch + x * 4;
                        for (int c = 0; c < 3; c++)
                            pixel[c] = info.Type == (int)OutputDuplicatePointerShapeType.MaskedColor
                                ? shape[offset + 3] == 0 ? shape[offset + c] : (byte)(pixel[c] ^ shape[offset + c])
                                : (byte)((shape[offset + c] * shape[offset + 3] + pixel[c] * (255 - shape[offset + 3]) + 127) / 255);
                    }
                }
        }
        finally { image.UnlockBits(data); }
    }
}
