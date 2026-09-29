using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using OpenCvPoint = OpenCvSharp.Point;
using OpenCvRect = OpenCvSharp.Rect;
using TaskAutomation.Contracts.Geometry;

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

}
