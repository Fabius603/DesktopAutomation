using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using OpenCvPoint = OpenCvSharp.Point;
using OpenCvRect = OpenCvSharp.Rect;
using TaskAutomation.Contracts.Geometry;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;

namespace TaskAutomation.Geometry;

public static class PixelGeometryAdapters
{
    public static PixelPoint ToPixelPoint(this DrawingPoint point) => new(point.X, point.Y);
    public static DrawingPoint ToDrawingPoint(this PixelPoint point) => new(point.X, point.Y);
    public static PixelRegion ToPixelRegion(this DrawingRectangle rectangle) =>
        new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    public static DrawingRectangle ToDrawingRectangle(this PixelRegion region) =>
        new(region.X, region.Y, region.Width, region.Height);

    public static PixelPoint ToPixelPoint(this OpenCvPoint point) => new(point.X, point.Y);
    public static OpenCvPoint ToOpenCvPoint(this PixelPoint point) => new(point.X, point.Y);
    public static PixelRegion ToPixelRegion(this OpenCvRect rectangle) =>
        new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    public static OpenCvRect ToOpenCvRect(this PixelRegion region) =>
        new(region.X, region.Y, region.Width, region.Height);

    public static PixelPoint ToPixelPoint(this WpfPoint point) => new((int)Math.Round(point.X), (int)Math.Round(point.Y));
    public static WpfPoint ToWpfPoint(this PixelPoint point) => new(point.X, point.Y);
    public static PixelRegion ToPixelRegion(this WpfRect rectangle) =>
        new((int)Math.Round(rectangle.X), (int)Math.Round(rectangle.Y),
            (int)Math.Round(rectangle.Width), (int)Math.Round(rectangle.Height));
    public static WpfRect ToWpfRect(this PixelRegion region) =>
        new(region.X, region.Y, region.Width, region.Height);
}
