using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Steps;

public sealed class StepInputContractRegistryTests
{
    [Fact]
    public void ProviderPolicy_DistinguishesDirectValuesReusableValuesAndStepResults()
    {
        var camera = StepInputContractRegistry.ForField(
            new CameraCaptureStepDefinition().Descriptor.Fields.Single());
        var predictionModel = StepInputContractRegistry.ForField(
            new PredictMovementStepDefinition().Descriptor.Fields.Single(field =>
                field.Id == PredictMovementStepDefinition.PredictionModelFieldId));
        var minimumConfidence = StepInputContractRegistry.ForField(
            new PredictMovementStepDefinition().Descriptor.Fields.Single(field =>
                field.Id == PredictMovementStepDefinition.MinimumConfidenceFieldId));
        var bounds = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "bounds")!;
        var padding = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "padding")!;
        var points = StepInputContractRegistry.Get(typeof(PredictMovementStep), "points")!;
        var clickPoints = StepInputContractRegistry.Get(typeof(KlickOnPointStep), "points")!;

        Assert.Empty(camera.AllowedProviderIds!);
        Assert.True(camera.AllowsDirectValue);
        Assert.Empty(predictionModel.AllowedProviderIds!);
        Assert.True(predictionModel.AllowsDirectValue);
        Assert.True(minimumConfidence.AllowsDirectValue);
        Assert.True(minimumConfidence.AllowsProvider(ValueProviderIds.JobVariable));
        Assert.True(minimumConfidence.AllowsProvider(ValueProviderIds.StepResult));
        Assert.False(minimumConfidence.AllowsProvider(ValueProviderIds.Secret));
        Assert.Equal([ValueProviderIds.StepResult], bounds.AllowedProviderIds);
        Assert.False(bounds.AllowsDirectValue);
        Assert.Equal([ValueProviderIds.JobVariable, ValueProviderIds.StepResult],
            padding.AllowedProviderIds!.OrderBy(value => value));
        Assert.Equal([ValueProviderIds.StepResult], points.AllowedProviderIds);
        Assert.False(points.AllowsDirectValue);
        Assert.True(clickPoints.AllowsDirectValue);
        Assert.True(clickPoints.AllowsProvider(ValueProviderIds.JobVariable));
        Assert.True(clickPoints.AllowsProvider(ValueProviderIds.StepResult));
        Assert.False(clickPoints.AllowsProvider(ValueProviderIds.Secret));
    }

    [Fact]
    public void EveryStepField_ExcludesSecretsAndKeepsFixedEnumsDirectOnly()
    {
        foreach (var definition in BuiltInStepDefinitions.Instance.Definitions)
        foreach (var field in definition.Descriptor.Fields)
        {
            var contract = StepInputContractRegistry.Resolve(definition.StepType, field);

            Assert.False(contract.AllowsProvider(ValueProviderIds.Secret),
                $"{definition.StepType.Name}.{field.Id} allows secrets.");
            Assert.True(contract.AllowsDirectValue
                        || contract.AllowsProvider(ValueProviderIds.JobVariable)
                        || contract.AllowsProvider(ValueProviderIds.StepResult),
                $"{definition.StepType.Name}.{field.Id} has no allowed value source.");
            if (field.ValueKind == TaskAutomation.Contracts.Steps.StepValueKind.Enum
                && field.Constraints?.AllowedValues is { Count: > 0 })
                Assert.Empty(contract.AllowedProviderIds!);
        }
    }

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
