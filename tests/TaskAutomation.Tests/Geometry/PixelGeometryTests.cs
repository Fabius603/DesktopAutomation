using System.Text.Json;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Geometry;

namespace TaskAutomation.Tests.Geometry;

public sealed class PixelGeometryTests
{
    [Fact]
    public void PixelRegion_ProvidesConsistentDerivedGeometry()
    {
        var region = new PixelRegion(10, 20, 30, 40);

        Assert.Equal(40, region.Right);
        Assert.Equal(60, region.Bottom);
        Assert.Equal(new PixelPoint(25, 40), region.Center);
        Assert.Equal(new PixelRegion(5, 14, 40, 52), region.Inflate(5, 6));
        Assert.Equal(new PixelRegion(20, 30, 20, 30),
            region.Intersect(new PixelRegion(20, 30, 50, 50)));
        Assert.True(region.Contains(new PixelPoint(10, 20)));
        Assert.False(region.Contains(new PixelPoint(40, 60)));
    }

    [Theory]
    [InlineData("{\"x\":1,\"y\":2,\"width\":3,\"height\":4}")]
    [InlineData("{\"X\":1,\"Y\":2,\"Width\":3,\"Height\":4}")]
    public void PixelRegion_ReadsExistingRoiJson(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var region = JsonSerializer.Deserialize<PixelRegion>(json, options);

        Assert.Equal(new PixelRegion(1, 2, 3, 4), region);
        Assert.Equal("{\"x\":1,\"y\":2,\"width\":3,\"height\":4}",
            JsonSerializer.Serialize(region));
    }

    [Fact]
    public void FrameworkAdapters_RoundTripWithoutChangingCoordinates()
    {
        var region = new PixelRegion(-100, 20, 300, 200);
        var point = new PixelPoint(-25, 80);

        Assert.Equal(region, region.ToDrawingRectangle().ToPixelRegion());
        Assert.Equal(region, region.ToOpenCvRect().ToPixelRegion());
        Assert.Equal(region, region.ToWpfRect().ToPixelRegion());
        Assert.Equal(point, point.ToDrawingPoint().ToPixelPoint());
        Assert.Equal(point, point.ToOpenCvPoint().ToPixelPoint());
        Assert.Equal(point, point.ToWpfPoint().ToPixelPoint());
    }

    [Fact]
    public void Formatter_UsesOnePixelGeometryDisplayConvention()
    {
        Assert.Equal("X 10 px  ·  Y 20 px",
            PixelGeometryFormatter.Format(new PixelPoint(10, 20)));
        Assert.Equal("300 × 200 px",
            PixelGeometryFormatter.Format(new PixelSize(300, 200)));
        Assert.Equal("X 10 px  ·  Y 20 px  ·  300 × 200 px",
            PixelGeometryFormatter.Format(new PixelRegion(10, 20, 300, 200)));
    }
}
