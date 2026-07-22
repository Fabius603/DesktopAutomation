using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class PredictMovementStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_MissingOrWrongSourceReturnsNotFound()
    {
        var result = await Execute(Step(), new PipelineContextStub());
        Assert.True(result.WasExecuted);
        Assert.False(result.Found);
    }

    [Fact]
    public async Task ExecuteAsync_InsufficientSamplesKeepsTrackButReturnsNotFound()
    {
        var context = new PipelineContextStub();
        var step = Step(minSamples: 3);
        AddSample(context, new Point(10, 10), DateTime.UtcNow.AddMilliseconds(-10));
        var result = await Execute(step, context);
        Assert.False(result.Found);
        Assert.True(context.PredictMovementStates.ContainsKey(step.Id));
    }

    [Fact]
    public async Task ExecuteAsync_LinearSamplesProducePredictionWithConfidenceAndBoundingBox()
    {
        var context = new PipelineContextStub();
        var step = Step(minSamples: 3);
        var now = DateTime.UtcNow;
        PredictMovementResult result = PredictMovementResult.Default;
        for (var index = 0; index < 3; index++)
        {
            AddSample(context, new Point(10 + index * 5, 20 + index * 2), now.AddMilliseconds(-30 + index * 10),
                new Rectangle(5 + index * 5, 15 + index * 2, 10, 10));
            result = await Execute(step, context);
        }
        Assert.True(result.Found);
        Assert.True(result.IsPredicted);
        Assert.NotNull(result.Point);
        Assert.NotNull(result.BoundingBox);
        Assert.True(result.Confidence > 0);
        Assert.NotNull(result.PredictedForUtc);
    }

    [Fact]
    public async Task ExecuteAsync_MinimumConfidenceCanRejectOtherwiseValidPrediction()
    {
        var context = new PipelineContextStub();
        var step = Step(minSamples: 2);
        step.Settings.MinimumConfidence = 1;
        var now = DateTime.UtcNow;
        AddSample(context, new Point(0, 0), now.AddMilliseconds(-20), confidence: .5);
        await Execute(step, context);
        AddSample(context, new Point(1, 1), now.AddMilliseconds(-10), confidence: .5);
        Assert.False((await Execute(step, context)).Found);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateCaptureTimestampDoesNotCreateSecondSample()
    {
        var context = new PipelineContextStub();
        var step = Step(minSamples: 2);
        var timestamp = DateTime.UtcNow.AddMilliseconds(-10);
        AddSample(context, new Point(0, 0), timestamp);
        await Execute(step, context);
        AddSample(context, new Point(10, 10), timestamp);
        Assert.False((await Execute(step, context)).Found);
    }

    private static PredictMovementStep Step(int minSamples = 2) => new() { Id = "predict", Settings = new()
        { MinSamples = minSamples, MaxSampleAgeMs = 1000, ResetDistanceThreshold = 100,
            PredictionModel = "Linear", PointsSource = new() { SourceStepId = "source", PropertyPath = "Point" } } };
    private static void AddSample(PipelineContextStub context, Point point, DateTime timestamp,
        Rectangle? box = null, double confidence = .9) => context.Results.Set<TemplateMatchingStep>(
        new TemplateMatchingResult { WasExecuted = true, Found = true, Point = point, BoundingBox = box,
            Confidence = confidence, SourceCaptureTimestampUtc = timestamp,
            AllDetections = [new DetectionItem { Center = point, BoundingBox = box, Confidence = confidence }] }, "source");
    private static async Task<PredictMovementResult> Execute(PredictMovementStep step, PipelineContextStub context) =>
        Assert.IsType<PredictMovementResult>(await new PredictMovementStepHandler().ExecuteAsync(step, context, default));
}
