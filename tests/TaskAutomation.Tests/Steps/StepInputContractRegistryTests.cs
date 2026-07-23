using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class StepInputContractRegistryTests
{
    [Fact]
    public void ShowOnDesktop_PrefersDetectionsWithBoundingBoxesOverPoints()
    {
        var contract = Assert.IsType<StepInputDescriptor>(
            StepInputContractRegistry.Get(typeof(ShowOnDesktopStep), "detections"));
        var result = Assert.IsType<ResultTypeDescriptor>(
            StepResultMetadata.GetResultType(nameof(YOLODetectionResult)));
        Assert.Contains(result.Properties, property =>
            property.Name == nameof(YOLODetectionResult.BoundingBox) &&
            property.DataType == ResultValueKind.Rectangle);
        var bestBoundingBox = Assert.Single(result.Properties, property =>
            property.Name == nameof(YOLODetectionResult.BoundingBox));
        var allBoundingBoxes = Assert.Single(result.Properties, property =>
            property.Name == "AllDetections[].BoundingBox");
        Assert.True(contract.Accepts(bestBoundingBox));
        Assert.True(contract.Accepts(allBoundingBoxes));

        var preferred = contract.FindPreferredProperty(result.Properties);

        Assert.NotNull(preferred);
        Assert.Equal(nameof(YOLODetectionResult.AllDetections), preferred.Name);
        Assert.Equal(ResultValueKind.Detection, preferred.DataType);
        Assert.Equal(ResultCardinality.Collection, preferred.Cardinality);
    }

    [Fact]
    public void ShowImage_AcceptsBestBoundingBoxAndBoundingBoxCollection()
    {
        var contract = Assert.IsType<StepInputDescriptor>(
            StepInputContractRegistry.Get(typeof(ShowImageStep), "detections"));
        var result = Assert.IsType<ResultTypeDescriptor>(
            StepResultMetadata.GetResultType(nameof(YOLODetectionResult)));
        var bestBoundingBox = Assert.Single(result.Properties, property =>
            property.Name == nameof(YOLODetectionResult.BoundingBox));
        var allBoundingBoxes = Assert.Single(result.Properties, property =>
            property.Name == "AllDetections[].BoundingBox");

        Assert.True(contract.Accepts(bestBoundingBox));
        Assert.True(contract.Accepts(allBoundingBoxes));
    }

    [Fact]
    public void VideoCreation_AcceptsBestBoundingBoxAndBoundingBoxCollection()
    {
        var contract = Assert.IsType<StepInputDescriptor>(
            StepInputContractRegistry.Get(typeof(VideoCreationStep), "detections"));
        var result = Assert.IsType<ResultTypeDescriptor>(
            StepResultMetadata.GetResultType(nameof(YOLODetectionResult)));
        var bestBoundingBox = Assert.Single(result.Properties, property =>
            property.Name == nameof(YOLODetectionResult.BoundingBox));
        var allBoundingBoxes = Assert.Single(result.Properties, property =>
            property.Name == "AllDetections[].BoundingBox");

        Assert.True(contract.Accepts(bestBoundingBox));
        Assert.True(contract.Accepts(allBoundingBoxes));
    }
}
