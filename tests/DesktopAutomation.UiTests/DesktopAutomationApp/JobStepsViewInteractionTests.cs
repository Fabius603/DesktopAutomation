using DesktopAutomationApp.Behaviors;
using DesktopAutomationApp.Views;
using System.Windows.Media;
using System.Windows.Controls;
using System.Runtime.ExceptionServices;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepsViewInteractionTests
{
    [Fact]
    public void StepContextMenu_ActionsTargetTheClickedStepEvenWhenAnotherStepWasSelected()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var previous = new TimeoutStep();
                var clicked = new TimeoutStep();
                using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Steps = [previous, clicked] });
                vm.SetSelectedSteps([previous], vm.Steps);
                var menu = JobStepsView.CreateStepContextMenu(new Border(), clicked, vm);
                var actions = menu.Items.OfType<MenuItem>().ToArray();
                var delete = Assert.Single(actions, item => ReferenceEquals(item.Command, vm.DeleteStepCommand));
                Assert.Same(clicked, delete.CommandParameter);
                var toggle = Assert.Single(actions, item => ReferenceEquals(item.Command, vm.ToggleBreakpointCommand));
                Assert.Same(clicked, toggle.CommandParameter);
                Assert.Same(previous, vm.SelectedStep);
            }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }

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
    public void StepDoubleClick_FocusesInspectorOnlyForNonInteractiveStepsWithDetails()
    {
        Assert.True(JobStepsView.ShouldFocusStepInspector(isInteractiveControl: false, hasDetails: true));
        Assert.False(JobStepsView.ShouldFocusStepInspector(isInteractiveControl: true, hasDetails: true));
        Assert.False(JobStepsView.ShouldFocusStepInspector(isInteractiveControl: false, hasDetails: false));
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
