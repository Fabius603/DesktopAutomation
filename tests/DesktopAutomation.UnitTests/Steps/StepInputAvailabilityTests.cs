using System.Drawing;
using System.Text.Json;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class StepInputAvailabilityTests
{
    [Fact]
    public void OptionalScalarSettingWithUnavailableSource_SkipsBeforeMaterialization()
    {
        var source = new ColorDetectionStep { Id = "source" };
        var consumer = new ShowTextStep { Settings = new() { Text = "text" } };
        var job = new Job { Steps = [source, consumer] };
        JobVariableInputMigration.Migrate(job);
        consumer.Inputs["font_size"] = ResultBinding.ForStepResult(source.Id, "confidence");
        var context = new PipelineContextStub(localValues: job.LocalValues);
        context.Results.RegisterStep(source);
        Assert.Throws<StepInputUnavailableException>(() => StepInputMaterializer.Materialize(consumer, context.Results));
        consumer.Inputs["font_size"] = ResultBinding.ForStepResult(source.Id, "nonexistent");
        Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(consumer, context.Results));
    }

    [Fact]
    public void OptionalKnownSourceWithoutResult_SkipsConsumerButBrokenReferenceFails()
    {
        var source = new ColorDetectionStep { Id = "source" };
        var consumer = new ShowOnDesktopStep { Settings = new() { DetectionsSource = ResultBinding.ForStepResult(source.Id, "point") } };
        var context = new PipelineContextStub();
        context.Results.RegisterStep(source);
        Assert.Throws<StepInputUnavailableException>(() => StepInputMaterializer.Materialize(consumer, context.Results));
        consumer.Settings.DetectionsSource = ResultBinding.ForStepResult("unknown", "point");
        Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(consumer, context.Results));
        consumer.Settings.DetectionsSource = ResultBinding.ForStepResult(source.Id, "nonexistent");
        Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(consumer, context.Results));
    }

    [Fact]
    public void RequiredKnownSourceWithoutResult_FailsConsumer()
    {
        var source = new DesktopDuplicationStep { Id = "capture" };
        var context = new PipelineContextStub();
        context.Results.RegisterStep(source);
        var consumer = new OcrStep { Settings = new() { ImageSource = ResultBinding.ForStepResult(source.Id, "image") } };
        Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(consumer, context.Results));
    }

    [Fact]
    public void SharedRectangleVariable_ProvidesDynamicRoiWithCaptureOffset()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Rectangle,
            Value = JsonSerializer.SerializeToNode(new PixelRegion(15, 25, 8, 6))
        };
        var context = new PipelineContextStub([variable]);
        var capture = new DesktopDuplicationResult { Bounds = new PixelRegion(10, 20, 20, 20), Offset = new PixelPoint(10, 20) };
        var binding = new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = variable.Id.ToString("D") };
        Assert.Equal(new PixelRegion(5, 5, 8, 6), DynamicRoiResolver.Resolve(binding, capture, context));
    }

    [Fact]
    public void RoiFeedback_IsValidBeforeItsProducerAndSurvivesOnlyAsGeometryState()
    {
        var captureStep = new DesktopDuplicationStep { Id = "capture" };
        var roi = new DynamicRoiStep { Id = "roi", Settings = new() { BoundsSource = ResultBinding.ForStepResult("ocr", "bounding_box") } };
        var consumer = new OcrStep
        {
            Id = "ocr",
            Settings = new()
            {
                ImageSource = ResultBinding.ForStepResult("capture", "image"),
                DynamicRoiSource = ResultBinding.ForStepResult("roi", "global_bounds")
            }
        };
        Assert.True(JobValidation.ValidateStep([captureStep, consumer, roi], consumer).IsValid);
        var context = new PipelineContextStub();
        context.Results.RegisterStep(captureStep); context.Results.RegisterStep(consumer); context.Results.RegisterStep(roi);
        using var image = new Bitmap(20, 20);
        var capture = new DesktopDuplicationResult { WasExecuted = true, Image = image, Bounds = new PixelRegion(0, 0, 20, 20) };
        context.Results.Set<DesktopDuplicationStep>(capture, "capture");
        Assert.NotNull(StepInputMaterializer.Materialize(consumer, context.Results));
        Assert.Null(DynamicRoiResolver.Resolve(consumer.Settings.DynamicRoiSource, capture, context));
        context.DynamicRoiStates[roi.Id] = new DynamicRoiState { GlobalBounds = new PixelRegion(2, 3, 5, 6) };
        Assert.Equal(new PixelRegion(2, 3, 5, 6), DynamicRoiResolver.Resolve(consumer.Settings.DynamicRoiSource, capture, context));
        Assert.Null(context.Results.GetRaw(roi.Id));
        context.DynamicRoiStates[roi.Id].GlobalBounds = null;
        Assert.Null(DynamicRoiResolver.Resolve(consumer.Settings.DynamicRoiSource, capture, context));
    }
}
