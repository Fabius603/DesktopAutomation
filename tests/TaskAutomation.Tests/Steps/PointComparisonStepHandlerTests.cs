using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class PointComparisonStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_NoResolvedPointsReturnsFalseWithZeroCounts()
    {
        var result = await Execute(new PointComparisonSettings());
        Assert.False(result.Matches);
        Assert.Equal(0, result.MatchCount);
        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData(110, 90, true)]
    [InlineData(111, 90, false)]
    [InlineData(100, 111, false)]
    public async Task ExecuteAsync_OffsetComparisonIncludesToleranceBoundary(int x, int y, bool expected)
    {
        var settings = new PointComparisonSettings { Mode = PointComparisonMode.Offset,
            Points = [Manual(x, y)], OffsetSettings = new() { ReferenceX = 100, ReferenceY = 100, OffsetX = 10, OffsetY = 10 } };
        Assert.Equal(expected, (await Execute(settings)).Matches);
    }

    [Fact]
    public async Task ExecuteAsync_AnyAndAllRequirementsProduceDifferentResults()
    {
        var settings = new PointComparisonSettings { Mode = PointComparisonMode.Offset,
            Points = [Manual(0, 0), Manual(100, 100)], OffsetSettings = new() { ReferenceX = 0, ReferenceY = 0, OffsetX = 1, OffsetY = 1 } };
        settings.MatchRequirement = PointMatchRequirement.Any;
        Assert.True((await Execute(settings)).Matches);
        settings.MatchRequirement = PointMatchRequirement.All;
        var all = await Execute(settings);
        Assert.False(all.Matches);
        Assert.Equal(1, all.MatchCount);
        Assert.Equal(2, all.TotalCount);
    }

    [Fact]
    public async Task ExecuteAsync_ExpressionSupportsAndOrAndBothAxes()
    {
        var settings = new PointComparisonSettings { Mode = PointComparisonMode.Expression, Points = [Manual(10, 20)],
            ExpressionSettings = new() { CombineMode = ExpressionCombineMode.And, Expressions = [
                new() { Axis = "X", Operator = PointAxisOperator.GreaterThanOrEqual, Value = 10 },
                new() { Axis = "Y", Operator = PointAxisOperator.LessThan, Value = 21 }] } };
        Assert.True((await Execute(settings)).Matches);
        settings.ExpressionSettings.Expressions[1].Value = 20;
        Assert.False((await Execute(settings)).Matches);
        settings.ExpressionSettings.CombineMode = ExpressionCombineMode.Or;
        Assert.True((await Execute(settings)).Matches);
    }

    [Fact]
    public async Task ExecuteAsync_CollectsPointsFromJobResult()
    {
        var context = new PipelineContextStub();
        context.Results.Set<TemplateMatchingStep>(new TemplateMatchingResult { WasExecuted = true,
            AllDetections = [new() { Center = new(1, 1) }, new() { Center = new(2, 2) }] }, "source");
        var settings = new PointComparisonSettings { Points = [new PointEntry { Source = PointEntrySource.JobResult,
            PointsSource = new() { SourceStepId = "source", PropertyPath = "AllDetections[].Center" } }],
            OffsetSettings = new() { ReferenceX = 1, ReferenceY = 1, OffsetX = 0, OffsetY = 0 }, MatchRequirement = PointMatchRequirement.Any };
        var result = await Execute(settings, context);
        Assert.True(result.Matches);
        Assert.Equal(2, result.TotalCount);
    }

    private static PointEntry Manual(int x, int y) => new() { ManualX = x, ManualY = y };
    private static async Task<PointComparisonResult> Execute(PointComparisonSettings settings, PipelineContextStub? context = null) =>
        Assert.IsType<PointComparisonResult>(await new PointComparisonStepHandler().ExecuteAsync(
            new PointComparisonStep { Settings = settings }, context ?? new PipelineContextStub(), default));
}
