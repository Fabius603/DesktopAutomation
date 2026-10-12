using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Steps;

public sealed class InputContractRegistryTests
{
    [Fact]
    public void DirectoryFields_KeepTheirTextPathShapeWithoutDisplayTextConversion()
    {
        var field = InputContractRegistry.ForField(new("directory", "", TaskAutomation.Contracts.Steps.StepValueKind.DirectoryPath));
        Assert.True(field.IsShapeCompatible(ResultValueKind.Text, ResultCardinality.Single, InputCompatibilityProfile.NewSelection));
        Assert.False(field.IsShapeCompatible(ResultValueKind.Integer, ResultCardinality.Single, InputCompatibilityProfile.NewSelection));
        Assert.False(field.IsShapeCompatible(ResultValueKind.Text, ResultCardinality.Collection, InputCompatibilityProfile.NewSelection));
    }

    [Theory]
    [InlineData(ResultValueKind.Integer, TaskAutomation.Contracts.Steps.StepValueKind.Integer)]
    [InlineData(ResultValueKind.Number, TaskAutomation.Contracts.Steps.StepValueKind.Number)]
    [InlineData(ResultValueKind.Boolean, TaskAutomation.Contracts.Steps.StepValueKind.Boolean)]
    [InlineData(ResultValueKind.Color, TaskAutomation.Contracts.Steps.StepValueKind.Color)]
    [InlineData(ResultValueKind.FilePath, TaskAutomation.Contracts.Steps.StepValueKind.FilePath)]
    public void FieldAndNestedMemberContracts_AgreeOnTypesCardinalityAndLegacyShapes(
        ResultValueKind kind, TaskAutomation.Contracts.Steps.StepValueKind fieldKind)
    {
        var field = InputContractRegistry.ForField(new("value", "", fieldKind));
        var member = InputContractRegistry.Member("value", kind);
        foreach (var sourceKind in Enum.GetValues<ResultValueKind>())
            foreach (var cardinality in Enum.GetValues<ResultCardinality>())
            {
                var property = new ResultPropertyDescriptor("source", "Source", sourceKind, Cardinality: cardinality);
                Assert.Equal(field.IsShapeCompatible(sourceKind, cardinality, InputCompatibilityProfile.ExistingBinding), member.EvaluateSource(ValueProviderIds.LocalValue, property, InputCompatibilityProfile.ExistingBinding).IsAccepted);
            }
        Assert.True(field.IsShapeCompatible(kind, ResultCardinality.OptionalSingle, InputCompatibilityProfile.NewSelection));
        Assert.False(field.IsShapeCompatible(kind, ResultCardinality.Collection, InputCompatibilityProfile.NewSelection));
        if (kind is ResultValueKind.Color or ResultValueKind.FilePath)
            Assert.False(field.IsShapeCompatible(ResultValueKind.Text, ResultCardinality.Single, InputCompatibilityProfile.NewSelection));
    }

    [Fact]
    public void EveryPlainTextFieldAndNestedTextMember_AcceptsDisplayableValuesAndCollections()
    {
        var fields = BuiltInStepDefinitions.Instance.Definitions.SelectMany(definition => definition.Descriptor.Fields)
            .Where(field => field.ValueKind is TaskAutomation.Contracts.Steps.StepValueKind.Text
                or TaskAutomation.Contracts.Steps.StepValueKind.MultilineText).ToArray();
        Assert.NotEmpty(fields);
        foreach (var field in fields)
        {
            var contract = InputContractRegistry.ForField(field);
            foreach (var kind in new[] { ResultValueKind.Boolean, ResultValueKind.Integer, ResultValueKind.Number,
                ResultValueKind.Point, ResultValueKind.ResultObject, ResultValueKind.JobReference, ResultValueKind.MacroReference })
                foreach (var cardinality in Enum.GetValues<ResultCardinality>())
                    Assert.True(contract.IsShapeCompatible(kind, cardinality, InputCompatibilityProfile.NewSelection), field.Id);
            Assert.False(contract.IsShapeCompatible(ResultValueKind.Image, ResultCardinality.Single, InputCompatibilityProfile.NewSelection));
        }
        foreach (var schemaId in new[] { InputSchemaIds.ProcessTarget, InputSchemaIds.YoloSelection,
            InputSchemaIds.UserChoiceOption })
        {
            Assert.True(InputContractRegistry.TryGetSchema(schemaId, out var schema));
            foreach (var member in schema.Members.Values.Where(member => member.TargetKind == ResultValueKind.Text))
                Assert.True(member.EvaluateSource(ValueProviderIds.LocalValue, new ResultPropertyDescriptor("number", "Number", ResultValueKind.Integer, Cardinality: ResultCardinality.Collection), InputCompatibilityProfile.ExistingBinding).IsAccepted);
        }
    }

    [Theory]
    [InlineData(typeof(ShowImageStep))]
    [InlineData(typeof(ShowOnDesktopStep))]
    [InlineData(typeof(SaveImageStep))]
    [InlineData(typeof(VideoCreationStep))]
    public void OverlayText_AcceptsLocalDisplayableValuesWithoutRelaxingOtherInputs(Type stepType)
    {
        var text = InputContractRegistry.Find(stepType, "text")!;
        Assert.True(text.EvaluateSource(ValueProviderIds.LocalValue, new ResultPropertyDescriptor("text", "Text", ResultValueKind.Text), InputCompatibilityProfile.NewSelection).IsAccepted);
        Assert.False(text.EvaluateSource(ValueProviderIds.LocalValue, new ResultPropertyDescriptor("image", "Image", ResultValueKind.Image), InputCompatibilityProfile.NewSelection).IsAccepted);
        Assert.False(text.AllowsProvider(ValueProviderIds.Secret));
        Assert.False(InputContractRegistry.Find(stepType, "detections")!.AllowsDirectValue);
        if (InputContractRegistry.Find(stepType, "image") is { } image)
            Assert.False(image.AllowsDirectValue);
    }

    [Fact]
    public void ProviderPolicy_DistinguishesDirectValuesReusableValuesAndStepResults()
    {
        var camera = InputContractRegistry.ForField(
            new CameraCaptureStepDefinition().Descriptor.Fields.Single());
        var predictionModel = InputContractRegistry.ForField(
            new PredictMovementStepDefinition().Descriptor.Fields.Single(field =>
                field.Id == PredictMovementStepDefinition.PredictionModelFieldId));
        var minimumConfidence = InputContractRegistry.ForField(
            new PredictMovementStepDefinition().Descriptor.Fields.Single(field =>
                field.Id == PredictMovementStepDefinition.MinimumConfidenceFieldId));
        var bounds = InputContractRegistry.Find(typeof(DynamicRoiStep), "bounds")!;
        var padding = InputContractRegistry.Find(typeof(DynamicRoiStep), "padding")!;
        var points = InputContractRegistry.Find(typeof(PredictMovementStep), "points")!;
        var clickPoints = InputContractRegistry.Find(typeof(KlickOnPointStep), "points")!;
        var overlayDetections = InputContractRegistry.Find(typeof(ShowOnDesktopStep), "detections")!;
        var overlayText = InputContractRegistry.Find(typeof(ShowOnDesktopStep), "text")!;

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
        Assert.False(clickPoints.AllowsDirectValue);
        Assert.Contains(ValueProviderIds.LocalValue, clickPoints.LegacyAllowedProviderIds);
        Assert.True(clickPoints.AllowsProvider(ValueProviderIds.JobVariable));
        Assert.True(clickPoints.AllowsProvider(ValueProviderIds.StepResult));
        Assert.False(clickPoints.AllowsProvider(ValueProviderIds.Secret));
        Assert.True(overlayDetections.AllowsProvider(ValueProviderIds.JobVariable));
        Assert.True(overlayDetections.AllowsProvider(ValueProviderIds.StepResult));
        Assert.True(overlayText.AllowsProvider(ValueProviderIds.JobVariable));
        Assert.True(overlayText.AllowsProvider(ValueProviderIds.StepResult));
    }

    [Fact]
    public void EveryStepField_ExcludesSecretsAndKeepsFixedEnumsDirectOnly()
    {
        foreach (var definition in BuiltInStepDefinitions.Instance.Definitions)
            foreach (var field in definition.Descriptor.Fields)
            {
                var contract = InputContractRegistry.Resolve(definition.StepType, field);

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
    public void ColorAndFileFields_UseTypedVariablesAndKeepExistingTextReferencesCompatible()
    {
        var colorField = new ShowTextStepDefinition().Descriptor.Fields.Single(field =>
            field.Id == ShowTextStepDefinition.FontColorFieldId);
        var fileField = new StartProcessStepDefinition().Descriptor.Fields.Single(field =>
            field.Id == StartProcessStepDefinition.ExecutablePathFieldId);
        var color = InputContractRegistry.ForField(colorField);
        var file = InputContractRegistry.ForField(fileField);

        Assert.Equal(ResultValueKind.Color, Assert.Single(color.AcceptedShapes).ValueKind);
        Assert.Equal(ResultValueKind.FilePath, Assert.Single(file.AcceptedShapes).ValueKind);
        Assert.True(color.IsShapeCompatible(ResultValueKind.Text, ResultCardinality.Single, InputCompatibilityProfile.ExistingBinding));
        Assert.True(file.IsShapeCompatible(ResultValueKind.Text, ResultCardinality.Single, InputCompatibilityProfile.ExistingBinding));
        Assert.False(color.IsShapeCompatible(ResultValueKind.Text, ResultCardinality.Single, InputCompatibilityProfile.NewSelection));
        Assert.False(file.IsShapeCompatible(ResultValueKind.Text, ResultCardinality.Single, InputCompatibilityProfile.NewSelection));
    }

    [Fact]
    public void ShowOnDesktop_PrefersDetectionsWithBoundingBoxesOverPoints()
    {
        var contract = Assert.IsType<InputContractNode>(
            InputContractRegistry.Find(typeof(ShowOnDesktopStep), "detections"));
        var result = Assert.IsType<ResultTypeDescriptor>(
            StepResultMetadata.GetResultType(nameof(YOLODetectionResult)));
        Assert.Contains(result.Properties, property =>
            property.Name == nameof(YOLODetectionResult.BoundingBox) &&
            property.DataType == ResultValueKind.Rectangle);
        var bestBoundingBox = Assert.Single(result.Properties, property =>
            property.Name == nameof(YOLODetectionResult.BoundingBox));
        var allBoundingBoxes = Assert.Single(result.Properties, property =>
            property.Name == "AllDetections[].BoundingBox");
        Assert.True(contract.IsShapeCompatible(bestBoundingBox, InputCompatibilityProfile.ExistingBinding));
        Assert.True(contract.IsShapeCompatible(allBoundingBoxes, InputCompatibilityProfile.ExistingBinding));

        var preferred = contract.FindPreferredProperty(result.Properties);

        Assert.NotNull(preferred);
        Assert.Equal(nameof(YOLODetectionResult.AllDetections), preferred.Name);
        Assert.Equal(ResultValueKind.Detection, preferred.DataType);
        Assert.Equal(ResultCardinality.Collection, preferred.Cardinality);
    }

    [Fact]
    public void ShowImage_AcceptsBestBoundingBoxAndBoundingBoxCollection()
    {
        var contract = Assert.IsType<InputContractNode>(
            InputContractRegistry.Find(typeof(ShowImageStep), "detections"));
        var result = Assert.IsType<ResultTypeDescriptor>(
            StepResultMetadata.GetResultType(nameof(YOLODetectionResult)));
        var bestBoundingBox = Assert.Single(result.Properties, property =>
            property.Name == nameof(YOLODetectionResult.BoundingBox));
        var allBoundingBoxes = Assert.Single(result.Properties, property =>
            property.Name == "AllDetections[].BoundingBox");

        Assert.True(contract.IsShapeCompatible(bestBoundingBox, InputCompatibilityProfile.ExistingBinding));
        Assert.True(contract.IsShapeCompatible(allBoundingBoxes, InputCompatibilityProfile.ExistingBinding));
    }

    [Fact]
    public void VideoCreation_AcceptsBestBoundingBoxAndBoundingBoxCollection()
    {
        var contract = Assert.IsType<InputContractNode>(
            InputContractRegistry.Find(typeof(VideoCreationStep), "detections"));
        var result = Assert.IsType<ResultTypeDescriptor>(
            StepResultMetadata.GetResultType(nameof(YOLODetectionResult)));
        var bestBoundingBox = Assert.Single(result.Properties, property =>
            property.Name == nameof(YOLODetectionResult.BoundingBox));
        var allBoundingBoxes = Assert.Single(result.Properties, property =>
            property.Name == "AllDetections[].BoundingBox");

        Assert.True(contract.IsShapeCompatible(bestBoundingBox, InputCompatibilityProfile.ExistingBinding));
        Assert.True(contract.IsShapeCompatible(allBoundingBoxes, InputCompatibilityProfile.ExistingBinding));
    }
}
