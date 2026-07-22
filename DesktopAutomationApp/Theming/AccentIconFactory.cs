using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace DesktopAutomationApp.Theming;

/// <summary>Creates a deliberately simple app icon in the selected accent color.</summary>
internal static class AccentIconFactory
{
    public static AccentIconSet Create(System.Windows.Media.Color accent)
    {
        using var bitmap = DrawIcon(256, System.Drawing.Color.FromArgb(accent.R, accent.G, accent.B));
        return new AccentIconSet(CreateTrayIcon(bitmap));
    }

    private static Bitmap DrawIcon(int size, System.Drawing.Color accent)
    {
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(System.Drawing.Color.Transparent);

        var scale = size / 256f;
        using var accentBrush = new SolidBrush(accent);
        using var whiteBrush = new SolidBrush(System.Drawing.Color.White);
        using var whitePen = new System.Drawing.Pen(System.Drawing.Color.White, 12 * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        using var background = RoundedRectangle(12 * scale, 12 * scale, 232 * scale, 232 * scale, 54 * scale);
        graphics.FillPath(accentBrush, background);

        // A compact robot head: one silhouette, two eyes and one antenna.
        graphics.DrawLine(whitePen, 128 * scale, 88 * scale, 128 * scale, 61 * scale);
        graphics.FillEllipse(whiteBrush, 119 * scale, 48 * scale, 18 * scale, 18 * scale);

        using var face = RoundedRectangle(58 * scale, 82 * scale, 140 * scale, 108 * scale, 30 * scale);
        graphics.FillPath(whiteBrush, face);
        graphics.FillEllipse(accentBrush, 89 * scale, 123 * scale, 20 * scale, 20 * scale);
        graphics.FillEllipse(accentBrush, 147 * scale, 123 * scale, 20 * scale, 20 * scale);

        return bitmap;
    }

    private static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Icon CreateTrayIcon(Bitmap source)
    {
        using var resized = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(resized))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawImage(source, 0, 0, resized.Width, resized.Height);
        }

        var handle = resized.GetHicon();
        try
        {
            using var borrowedIcon = Icon.FromHandle(handle);
            return (Icon)borrowedIcon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
}

internal sealed record AccentIconSet(Icon TrayIcon) : IDisposable
{
    public void Dispose() => TrayIcon.Dispose();
}
