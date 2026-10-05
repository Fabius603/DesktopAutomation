using System.Windows;
using DesktopAutomationApp.Services.Jobs;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepDropPlacementTests
{
    [Fact]
    public void EmptyBranchHintOccupiesItsHeaderRowWithoutIntroducingAnotherInsertionSlot()
    {
        JobStep[] steps = [new IfStep(), new ElseStep(), new EndIfStep(), new TimeoutStep()];
        StepDropRow[] rows = [new(0, 0, 92), new(1, 100, 92), new(3, 224, 64)];
        var header = JobStepDropPlacement.Resolve(steps, rows, new Point(130, 150), []);
        var hint = JobStepDropPlacement.Resolve(steps, rows, new Point(130, 181), []);
        Assert.Equal(2, hint.Index);
        Assert.Equal(header.Index, hint.Index);
        Assert.Equal(header.X, hint.X);
    }

    [Theory]
    [InlineData(200, 3, 212)]
    [InlineData(110, 4, 236)]
    [InlineData(72, 5, 268)]
    public void HiddenClosingMarkersKeepNestedInsertionTargetsInTheirFooterLanes(double x, int index, double y)
    {
        JobStep[] steps = [new IfStep(), new IfStep(), new TimeoutStep(), new EndIfStep(), new EndIfStep(), new TimeoutStep()];
        StepDropRow[] rows = [new(0, 0, 64), new(1, 72, 64), new(2, 144, 64), new(5, 272, 64)];
        var target = JobStepDropPlacement.Resolve(steps, rows, new Point(x, 194), []);
        Assert.Equal(index, target.Index);
        Assert.Equal(y, target.Y);
    }

    [Fact]
    public void HoveringCollapsedHeaderBottom_TargetsAfterAllItsHiddenChildren()
    {
        var conditional = new IfStep();
        JobStep[] steps = [conditional, new TimeoutStep(), new ElseStep(), new TimeoutStep(), new EndIfStep(), new TimeoutStep()];
        StepDropRow[] rows = [new(0, 0, 64), new(5, 80, 64)];
        var target = JobStepDropPlacement.Resolve(steps, rows, new Point(200, 50), [conditional.Id]);
        Assert.Equal(5, target.Index);
        Assert.Equal(76, target.Y);
        Assert.Equal(StepListProjection.GutterWidth, target.X);
    }

    [Fact]
    public void DraggingNearBranchEnd_StaysInsideItsBranch()
    {
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new ElseStep(), new EndIfStep()];
        StepDropRow[] rows = [new(0, 0, 64), new(1, 72, 64), new(2, 144, 64), new(3, 216, 14)];
        var target = JobStepDropPlacement.Resolve(steps, rows, new Point(150, 131), []);
        Assert.Equal(2, target.Index);
        Assert.Equal(140, target.Y);
        Assert.Equal(StepListProjection.GutterWidth + StepListProjection.Indentation, target.X);
    }

    [Theory]
    [InlineData(200, 3, 2)]
    [InlineData(72, 5, 0)]
    public void HorizontalPosition_CanMoveOutOfNestedClosingBoundaries(double x, int index, int depth)
    {
        JobStep[] steps = [new IfStep(), new IfStep(), new TimeoutStep(), new EndIfStep(), new EndIfStep(), new TimeoutStep()];
        StepDropRow[] rows = [new(0, 0, 64), new(1, 72, 64), new(2, 144, 64), new(3, 216, 14), new(4, 238, 14), new(5, 260, 64)];
        var target = JobStepDropPlacement.Resolve(steps, rows, new Point(x, 194), []);
        Assert.Equal(index, target.Index);
        Assert.Equal(StepListProjection.GutterWidth + depth * StepListProjection.Indentation, target.X);
    }

    [Fact]
    public void EmptyPhase_HasAnInsertionTargetAtItsRoot()
    {
        var target = JobStepDropPlacement.Resolve([], [], new Point(300, 20), []);
        Assert.Equal(0, target.Index);
        Assert.Equal(StepListProjection.GutterWidth, target.X);
    }
    [Fact]
    public void EmptyElseBranch_RemainsADropTargetWithoutAnInsertCard()
    {
        JobStep[] steps = [new IfStep(), new ElseStep(), new EndIfStep()];
        StepDropRow[] rows = [new(0, 0, 64), new(1, 72, 64), new(2, 144, 14)];
        var target = JobStepDropPlacement.Resolve(steps, rows, new Point(130, 130), []);
        Assert.Equal(2, target.Index);
        Assert.Equal(StepListProjection.GutterWidth + StepListProjection.Indentation, target.X);
        Assert.True(target.Distance <= 30);
    }

}
