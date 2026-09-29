using DesktopAutomationApp.Behaviors;
using DesktopAutomationApp.Views;
using System.Windows.Media;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepsViewInteractionTests
{
    [Fact]
    public void InspectorSelectionIndicator_ReplacesFrozenTemplateTransformWithMutableCopy()
    {
        var frozenTransform = new TranslateTransform(42, 0);
        frozenTransform.Freeze();

        var mutableTransform = JobStepsView.EnsureMutableTranslateTransform(frozenTransform);

        Assert.NotSame(frozenTransform, mutableTransform);
        Assert.False(mutableTransform.IsFrozen);
        Assert.Equal(42, mutableTransform.X);
        mutableTransform.BeginAnimation(TranslateTransform.XProperty, null);
        mutableTransform.X = 84;
        Assert.Equal(84, mutableTransform.X);
    }

    [Fact]
    public void StepDoubleClick_OpensDetailsOnlyForNonInteractiveStepsWithDetails()
    {
        Assert.True(JobStepsView.ShouldOpenStepDetails(isInteractiveControl: false, hasDetails: true));
        Assert.False(JobStepsView.ShouldOpenStepDetails(isInteractiveControl: true, hasDetails: true));
        Assert.False(JobStepsView.ShouldOpenStepDetails(isInteractiveControl: false, hasDetails: false));
    }

    [Fact]
    public void DragPreviewOrder_MovesSelectedStepsIntoTheVisibleTargetSlot()
    {
        var order = StepDragDrop.BuildPreviewOrder(
            itemCount: 6,
            movingIndices: [1, 2],
            targetIndex: 5);

        Assert.Equal([0, 3, 4, 1, 2, 5], order);
    }

    [Fact]
    public void DragPreviewHitZone_KeepsTargetOverGhostAndChangesOnlyAcrossItemMidpoint()
    {
        Assert.Equal(4, StepDragDrop.ResolvePreviewHitZoneTarget(
            pointerY: 130,
            top: 100,
            bottom: 160,
            beforeIndex: 1,
            afterIndex: 2,
            holdsCurrentTarget: true,
            currentTargetIndex: 4));
        Assert.Equal(1, StepDragDrop.ResolvePreviewHitZoneTarget(
            pointerY: 120,
            top: 100,
            bottom: 160,
            beforeIndex: 1,
            afterIndex: 2,
            holdsCurrentTarget: false,
            currentTargetIndex: 4));
        Assert.Equal(2, StepDragDrop.ResolvePreviewHitZoneTarget(
            pointerY: 140,
            top: 100,
            bottom: 160,
            beforeIndex: 1,
            afterIndex: 2,
            holdsCurrentTarget: false,
            currentTargetIndex: 4));
    }
}
