using System.Text.Json.Serialization;

namespace TaskAutomation.Contracts.Geometry;

/// <summary>A position expressed in whole pixels.</summary>
public readonly record struct PixelPoint(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y)
{
    public static PixelPoint Origin { get; } = new(0, 0);
    public static PixelPoint Empty => Origin;

    public PixelPoint Offset(int deltaX, int deltaY) => new(X + deltaX, Y + deltaY);
}

/// <summary>A size expressed in whole pixels.</summary>
public readonly record struct PixelSize(
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height)
{
    public static PixelSize Empty { get; } = new(0, 0);
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>A rectangular pixel region using an upper-left origin.</summary>
public readonly record struct PixelRegion(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height)
{
    public static PixelRegion Empty { get; } = new(0, 0, 0, 0);

    [JsonIgnore] public int Left => X;
    [JsonIgnore] public int Top => Y;
    [JsonIgnore] public int Right => X + Width;
    [JsonIgnore] public int Bottom => Y + Height;
    [JsonIgnore] public bool IsEmpty => Width <= 0 || Height <= 0;
    [JsonIgnore] public PixelPoint Location => new(X, Y);
    [JsonIgnore] public PixelSize Size => new(Width, Height);
    [JsonIgnore] public PixelPoint Center => new(X + Width / 2, Y + Height / 2);

    public PixelRegion Offset(int deltaX, int deltaY) => new(X + deltaX, Y + deltaY, Width, Height);

    public PixelRegion Inflate(int horizontal, int vertical) =>
        new(X - horizontal, Y - vertical, Width + horizontal * 2, Height + vertical * 2);

    public PixelRegion Intersect(PixelRegion other)
    {
        var left = Math.Max(Left, other.Left);
        var top = Math.Max(Top, other.Top);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        return right <= left || bottom <= top
            ? Empty
            : new PixelRegion(left, top, right - left, bottom - top);
    }

    public bool Contains(PixelPoint point) =>
        point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    public override string ToString() => $"{{X={X},Y={Y},Width={Width},Height={Height}}}";
}

/// <summary>Provides one compact display convention for pixel geometry.</summary>
public static class PixelGeometryFormatter
{
    public static string Format(PixelPoint point) => $"X {point.X} px  ·  Y {point.Y} px";

    public static string Format(PixelSize size) => $"{size.Width} × {size.Height} px";

    public static string Format(PixelRegion region) =>
        $"X {region.X} px  ·  Y {region.Y} px  ·  {region.Width} × {region.Height} px";
}
