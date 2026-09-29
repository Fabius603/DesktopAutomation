using ImageDetection.YOLO;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace TaskAutomation.Tests.ImageDetection;

public sealed class YoloModelCompatibilityTests
{
    [Theory]
    [InlineData("Search Field")]
    [InlineData("Search Bar")]
    public void ResolveRequestedClassIds_GroupsEquivalentScreenParserSearchClasses(string requestedClass)
    {
        string[] labels = ["Button", "Search Field", "Text", "Search Bar"];

        var result = YoloModelCompatibility.ResolveRequestedClassIds(labels, requestedClass);

        Assert.Equal([1, 3], result);
    }

    [Fact]
    public void ResolveRequestedClassIds_KeepsUnrelatedClassExact()
    {
        string[] labels = ["Button", "Search Field", "Text", "Search Bar"];

        var result = YoloModelCompatibility.ResolveRequestedClassIds(labels, "Button");

        Assert.Equal([0], result);
    }

    [Fact]
    public void ResolveRequestedClassIds_ReturnsEmptyForUnknownClass()
    {
        string[] labels = ["Button", "Search Field"];

        Assert.Empty(YoloModelCompatibility.ResolveRequestedClassIds(labels, "Person"));
    }

    [Theory]
    [InlineData(640)]
    [InlineData(1280)]
    public void ResolveSquareInputSize_ReturnsStaticModelSize(int size)
    {
        var result = YoloModelCompatibility.ResolveSquareInputSize(
            [1, 3, size, size],
            TensorElementType.Float,
            640);

        Assert.Equal(size, result);
    }

    [Fact]
    public void ResolveSquareInputSize_UsesFallbackForDynamicSpatialDimensions()
    {
        var result = YoloModelCompatibility.ResolveSquareInputSize(
            [1, 3, -1, -1],
            TensorElementType.Float16,
            960);

        Assert.Equal(960, result);
    }

    [Theory]
    [InlineData(1, 3, 720, 1280)]
    [InlineData(1, 1, 640, 640)]
    public void ResolveSquareInputSize_RejectsUnsupportedShape(int batch, int channels, int height, int width)
    {
        Assert.Throws<NotSupportedException>(() =>
            YoloModelCompatibility.ResolveSquareInputSize(
                [batch, channels, height, width],
                TensorElementType.Float,
                640));
    }

    [Theory]
    [InlineData(1, 84, 8400, 80)]
    [InlineData(1, 8400, 84, 80)]
    [InlineData(1, 59, 33600, 55)]
    public void ValidateDetectionOutput_AcceptsDetectionLayouts(
        int batch,
        int first,
        int second,
        int classCount)
    {
        YoloModelCompatibility.ValidateDetectionOutput(
            [batch, first, second],
            classCount);
    }

    [Theory]
    [InlineData(1, 300, 6, 80)]
    [InlineData(1, 116, 8400, 80)]
    [InlineData(1, 84, 8400, 55)]
    public void ValidateDetectionOutput_RejectsMismatchedTaskOrLabels(
        int batch,
        int first,
        int second,
        int classCount)
    {
        Assert.Throws<NotSupportedException>(() =>
            YoloModelCompatibility.ValidateDetectionOutput(
                [batch, first, second],
                classCount));
    }
}
