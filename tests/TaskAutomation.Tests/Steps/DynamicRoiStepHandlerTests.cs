using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class DynamicRoiStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ValidDetectionAppliesPaddingAndUpdatesState()
    {
        var context = Context(new Rectangle(10, 20, 30, 40), .8);
        var step = Step(padding: 5, minimumConfidence: .5);
        var result = await Execute(step, context);
        Assert.True(result.RoiUpdated);
        Assert.False(result.RoiReset);
        Assert.Equal(new Rectangle(5, 15, 40, 50), result.GlobalBounds);
        Assert.Equal(result.GlobalBounds, context.DynamicRoiStates[step.Id].GlobalBounds);
        Assert.Equal(0, result.ConsecutiveMisses);
    }

    [Fact]
    public async Task ExecuteAsync_LowConfidenceCountsAsMissAndPreservesExistingRoi()
    {
        var context = Context(new Rectangle(1, 1, 2, 2), .2);
        var step = Step(minimumConfidence: .5, resetAfterMisses: 2);
        context.DynamicRoiStates[step.Id] = new DynamicRoiState { GlobalBounds = new Rectangle(9, 9, 9, 9) };
        var result = await Execute(step, context);
        Assert.False(result.RoiUpdated);
        Assert.Equal(new Rectangle(9, 9, 9, 9), result.GlobalBounds);
        Assert.Equal(1, result.ConsecutiveMisses);
    }

    [Fact]
    public async Task ExecuteAsync_ResetsRoiAtConfiguredMissLimit()
    {
        var context = new PipelineContextStub();
        var step = Step(resetAfterMisses: 2);
        context.DynamicRoiStates[step.Id] = new DynamicRoiState { GlobalBounds = new Rectangle(1, 1, 2, 2), ConsecutiveMisses = 1,
            RoiUsesSinceFullSearch = 5 };
        var result = await Execute(step, context);
        Assert.True(result.RoiReset);
        Assert.Null(result.GlobalBounds);
        Assert.Equal(0, result.ConsecutiveMisses);
        Assert.Equal(0, context.DynamicRoiStates[step.Id].RoiUsesSinceFullSearch);
    }

    [Fact]
    public async Task ExecuteAsync_ResetDisabledAccumulatesMisses()
    {
        var context = new PipelineContextStub();
        var step = Step(resetAfterMisses: 0);
        Assert.Equal(1, (await Execute(step, context)).ConsecutiveMisses);
        Assert.Equal(2, (await Execute(step, context)).ConsecutiveMisses);
    }

    private static PipelineContextStub Context(Rectangle bounds, double confidence)
    {
        var context = new PipelineContextStub();
        context.Results.Set<TemplateMatchingStep>(new TemplateMatchingResult { WasExecuted = true, Found = true,
            BoundingBox = bounds, Confidence = confidence }, "source");
        return context;
    }
    private static DynamicRoiStep Step(int padding = 0, double minimumConfidence = 0, int resetAfterMisses = 3) => new()
    { Id = "roi", Settings = new() { Padding = padding, MinimumConfidence = minimumConfidence, ResetAfterMisses = resetAfterMisses,
        BoundsSource = new() { SourceStepId = "source", PropertyPath = "BoundingBox" } } };
    private static async Task<DynamicRoiResult> Execute(DynamicRoiStep step, PipelineContextStub context) =>
        Assert.IsType<DynamicRoiResult>(await new DynamicRoiStepHandler().ExecuteAsync(step, context, default));
}
