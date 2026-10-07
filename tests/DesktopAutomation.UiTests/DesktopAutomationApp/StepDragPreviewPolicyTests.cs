using DesktopAutomationApp.Behaviors;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class StepDragPreviewPolicyTests
{
    [Theory]
    [InlineData(29, false, true)]
    [InlineData(31, false, false)]
    [InlineData(31, true, true)]
    [InlineData(45, true, false)]
    [InlineData(double.NaN, true, false)]
    [InlineData(double.PositiveInfinity, false, false)]
    public void Snap_RequiresProximityAndRetainsAnAcquiredTarget(double distance, bool retaining, bool expected)
        => Assert.Equal(expected, StepDragPreviewPolicy.CanSnap(distance, retaining));

    [Fact]
    public void EdgeScrolling_StartsGentlyStopsImmediatelyAndReversesWithoutMomentum()
    {
        var first = StepDragPreviewPolicy.EaseScrollVelocity(0, 360, 0.016);
        var next = StepDragPreviewPolicy.EaseScrollVelocity(first, 360, 0.016);
        Assert.InRange(first, 1, 60);
        Assert.InRange(next, first + 1, 360);
        Assert.Equal(0, StepDragPreviewPolicy.EaseScrollVelocity(next, 0, 0.016));
        Assert.Equal(-first, StepDragPreviewPolicy.EaseScrollVelocity(next, -360, 0.016));
        Assert.Equal(0, StepDragPreviewPolicy.ScrollVelocity(double.NaN, 600));
        Assert.Equal(0, StepDragPreviewPolicy.ScrollVelocity(0, double.PositiveInfinity));
    }

    [Fact]
    public void EdgeScrolling_AcceleratesSmoothlyTowardBothEdgesAndStopsInTheMiddle()
    {
        Assert.Equal(0, StepDragPreviewPolicy.ScrollVelocity(300, 600));
        Assert.Equal(0, StepDragPreviewPolicy.ScrollVelocity(-1, 600));
        Assert.Equal(0, StepDragPreviewPolicy.ScrollVelocity(601, 600));
        Assert.Equal(-360, StepDragPreviewPolicy.ScrollVelocity(0, 600));
        Assert.Equal(360, StepDragPreviewPolicy.ScrollVelocity(600, 600));
        Assert.True(StepDragPreviewPolicy.ScrollVelocity(10, 600) < StepDragPreviewPolicy.ScrollVelocity(40, 600));
        Assert.True(StepDragPreviewPolicy.ScrollVelocity(590, 600) > StepDragPreviewPolicy.ScrollVelocity(560, 600));
        Assert.Equal(-StepDragPreviewPolicy.ScrollVelocity(10, 600), StepDragPreviewPolicy.ScrollVelocity(590, 600));
        Assert.Equal(0, StepDragPreviewPolicy.ScrollVelocity(0, 0));
    }
}
