using System.Drawing;
using System.Drawing.Imaging;
using ImageDetection.Model;

namespace ImageDetection.YOLO;

public static class YoloImagePreprocessor
{
    private const float PaddingValue = 114f / 255f;

    public static (float Scale, float PadX, float PadY) Preprocess(
        Bitmap source,
        Rectangle sourceRectangle,
        YoloBuffers buffers)
    {
        var inputSize = buffers.Size;
        var sourceWidth = sourceRectangle.Width;
        var sourceHeight = sourceRectangle.Height;
        var scale = Math.Min(
            (float)inputSize / sourceWidth,
            (float)inputSize / sourceHeight);
        var resizedWidth = (int)Math.Round(sourceWidth * scale);
        var resizedHeight = (int)Math.Round(sourceHeight * scale);
        var padX = (inputSize - resizedWidth) / 2;
        var padY = (inputSize - resizedHeight) / 2;
        var scaleX = (float)resizedWidth / sourceWidth;
        var scaleY = (float)resizedHeight / sourceHeight;

        var bitmapData = source.LockBits(
            sourceRectangle,
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            unsafe
            {
                var sourceBase = (byte*)bitmapData.Scan0;
                var sourceStride = bitmapData.Stride;
                var plane = inputSize * inputSize;
                var destination = buffers.Input;

                for (var targetY = 0; targetY < inputSize; targetY++)
                {
                    var rowOffset = targetY * inputSize;
                    if (targetY < padY || targetY >= padY + resizedHeight)
                    {
                        FillPadding(destination, plane, rowOffset, inputSize);
                        continue;
                    }

                    var sourceY = (targetY - padY + 0.5f) / scaleY - 0.5f;
                    var y0 = Math.Clamp((int)Math.Floor(sourceY), 0, sourceHeight - 1);
                    var y1 = Math.Min(y0 + 1, sourceHeight - 1);
                    var yWeight = Math.Clamp(sourceY - y0, 0f, 1f);
                    var row0 = sourceBase + y0 * sourceStride;
                    var row1 = sourceBase + y1 * sourceStride;

                    for (var targetX = 0; targetX < inputSize; targetX++)
                    {
                        var destinationIndex = rowOffset + targetX;
                        if (targetX < padX || targetX >= padX + resizedWidth)
                        {
                            destination[destinationIndex] = PaddingValue;
                            destination[plane + destinationIndex] = PaddingValue;
                            destination[2 * plane + destinationIndex] = PaddingValue;
                            continue;
                        }

                        var sourceX = (targetX - padX + 0.5f) / scaleX - 0.5f;
                        var x0 = Math.Clamp((int)Math.Floor(sourceX), 0, sourceWidth - 1);
                        var x1 = Math.Min(x0 + 1, sourceWidth - 1);
                        var xWeight = Math.Clamp(sourceX - x0, 0f, 1f);

                        var pixel00 = row0 + x0 * 4;
                        var pixel10 = row0 + x1 * 4;
                        var pixel01 = row1 + x0 * 4;
                        var pixel11 = row1 + x1 * 4;

                        destination[destinationIndex] = InterpolateChannel(
                            pixel00[2], pixel10[2], pixel01[2], pixel11[2],
                            xWeight, yWeight);
                        destination[plane + destinationIndex] = InterpolateChannel(
                            pixel00[1], pixel10[1], pixel01[1], pixel11[1],
                            xWeight, yWeight);
                        destination[2 * plane + destinationIndex] = InterpolateChannel(
                            pixel00[0], pixel10[0], pixel01[0], pixel11[0],
                            xWeight, yWeight);
                    }
                }
            }
        }
        finally
        {
            source.UnlockBits(bitmapData);
        }

        return (scale, padX, padY);
    }

    private static void FillPadding(float[] destination, int plane, int offset, int length)
    {
        Array.Fill(destination, PaddingValue, offset, length);
        Array.Fill(destination, PaddingValue, plane + offset, length);
        Array.Fill(destination, PaddingValue, 2 * plane + offset, length);
    }

    private static float InterpolateChannel(
        byte topLeft,
        byte topRight,
        byte bottomLeft,
        byte bottomRight,
        float xWeight,
        float yWeight)
    {
        var top = topLeft + (topRight - topLeft) * xWeight;
        var bottom = bottomLeft + (bottomRight - bottomLeft) * xWeight;
        return (top + (bottom - top) * yWeight) / 255f;
    }
}
