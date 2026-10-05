using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;

namespace TaskAutomation.Tests.Jobs;

public sealed class ControlFlowEditRulesTests
{
    [Fact]
    public void DroppingAfterCollapsedAlternative_SkipsOnlyItsBody()
    {
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new ElseIfStep(), new IfStep(), new TimeoutStep(),
            new EndIfStep(), new ElseStep(), new TimeoutStep(), new EndIfStep()];
        Assert.Equal(new ControlFlowInsertionTarget(6, 1), ControlFlowEditRules.ResolveInsertionTarget(steps, 2, true, 1, afterWholeBlock: true));
        Assert.Equal(new ControlFlowInsertionTarget(8, 1), ControlFlowEditRules.ResolveInsertionTarget(steps, 6, true, 1, afterWholeBlock: true));
    }

    [Theory]
    [InlineData(2, 2, 3, 4, 5)]
    [InlineData(6, 6, 7)]
    public void DeletingAlternative_RemovesOnlyItsBodyIncludingNestedBlocks(int branch, params int[] expected)
    {
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new ElseIfStep(), new IfStep(),
            new TimeoutStep(), new EndIfStep(), new ElseStep(), new TimeoutStep(), new EndIfStep(), new TimeoutStep()];
        var deletion = ControlFlowEditRules.ExpandDeletionSelection(steps, [branch]);
        Assert.Equal(expected, deletion);
        var remaining = steps.Where((_, index) => !deletion.Contains(index)).ToArray();
        Assert.True(ControlFlowStructureAnalyzer.Analyze(remaining).IsValid);
        Assert.Same(steps[0], remaining[0]);
        Assert.Contains(steps[8], remaining);
        // Moving an alternative still retains the complete block as its editing unit.
        Assert.Equal(Enumerable.Range(0, 9), ControlFlowEditRules.ExpandSelection(steps, [branch]));
    }

    [Fact]
    public void DeletingEmptyAndMultipleBranches_PreservesIfItsBodyAndEnd()
    {
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new ElseIfStep(), new ElseIfStep(),
            new TimeoutStep(), new ElseStep(), new EndIfStep()];
        Assert.Equal([2], ControlFlowEditRules.ExpandDeletionSelection(steps, [2]));
        Assert.Equal([2, 3, 4, 5], ControlFlowEditRules.ExpandDeletionSelection(steps, [2, 3, 5]));
        Assert.Equal(Enumerable.Range(0, 7), ControlFlowEditRules.ExpandDeletionSelection(steps, [0, 5]));
    }

    [Fact]
    public void DeletingDamagedBranches_DoesNotInferUnrelatedContents()
    {
        Assert.Equal([0], ControlFlowEditRules.ExpandDeletionSelection([new ElseStep(), new TimeoutStep()], [0]));
        Assert.Equal([1], ControlFlowEditRules.ExpandDeletionSelection([new IfStep(), new ElseStep(), new TimeoutStep()], [1]));
        Assert.Empty(ControlFlowEditRules.ExpandDeletionSelection([new TimeoutStep()], [-1, 5]));
    }

    [Theory]
    [InlineData(-1, 4)]
    [InlineData(0, 1)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    public void AddingAfterFocus_PreservesTheRequestedRowAndAppendsWithoutFocus(int focus, int expected)
    {
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new EndIfStep(), new TimeoutStep()];
        Assert.Equal(expected, ControlFlowEditRules.ResolveAddInsertionIndex(steps, focus));
    }

    [Fact]
    public void AlternativeInsertion_UsesTheNearestBlockAndAlwaysPlacesElseIfBeforeElse()
    {
        JobStep[] steps = [new IfStep(), new IfStep(), new ElseIfStep(), new ElseStep(), new EndIfStep(),
            new ElseIfStep(), new EndIfStep()];
        Assert.Equal(3, ControlFlowEditRules.ResolveBranchInsertionIndex(steps, 2, elseIf: true));
        Assert.Null(ControlFlowEditRules.ResolveBranchInsertionIndex(steps, 2, elseIf: false));
        Assert.Equal(6, ControlFlowEditRules.ResolveBranchInsertionIndex(steps, 0, elseIf: false));
        Assert.Equal(6, ControlFlowEditRules.ResolveBranchInsertionIndex(steps, 5, elseIf: true));
    }

    [Fact]
    public void AlternativeInsertion_RejectsOrphansIncompleteAndMisorderedBlocks()
    {
        Assert.Null(ControlFlowEditRules.ResolveBranchInsertionIndex([new TimeoutStep()], 0, true));
        Assert.Null(ControlFlowEditRules.ResolveBranchInsertionIndex([new IfStep(), new TimeoutStep()], 0, false));
        Assert.Null(ControlFlowEditRules.ResolveBranchInsertionIndex([new IfStep(), new ElseStep(), new ElseIfStep(), new EndIfStep()], 0, true));
    }

    [Theory]
    [InlineData(1, false, 0, 1, 0)]
    [InlineData(1, true, 2, 2, 1)]
    [InlineData(2, false, 0, 2, 1)]
    [InlineData(2, true, 3, 3, 2)]
    [InlineData(3, true, 2, 4, 2)]
    [InlineData(3, true, 1, 5, 1)]
    [InlineData(3, true, 0, 5, 1)]
    [InlineData(5, false, 2, 5, 1)]
    [InlineData(5, true, 2, 6, 1)]
    [InlineData(6, true, 1, 7, 1)]
    [InlineData(6, true, 0, 8, 0)]
    public void DropTargets_RespectBranchBoundariesAndOnlyOutdentAcrossClosingMarkers(
        int row, bool after, int depth, int expectedIndex, int expectedDepth)
    {
        JobStep[] steps = [new TimeoutStep(), new IfStep(), new IfStep(), new TimeoutStep(), new EndIfStep(),
            new ElseStep(), new TimeoutStep(), new EndIfStep(), new TimeoutStep()];
        var target = ControlFlowEditRules.ResolveInsertionTarget(steps, row, after, depth);
        Assert.Equal(new ControlFlowInsertionTarget(expectedIndex, expectedDepth), target);
    }

    [Fact]
    public void DroppingAfterCollapsedBlock_SkipsItsHiddenChildren()
    {
        JobStep[] steps = [new IfStep(), new IfStep(), new TimeoutStep(), new EndIfStep(), new ElseStep(), new TimeoutStep(), new EndIfStep()];
        Assert.Equal(new ControlFlowInsertionTarget(7, 0), ControlFlowEditRules.ResolveInsertionTarget(steps, 0, true, 2, afterWholeBlock: true));
        Assert.Equal(new ControlFlowInsertionTarget(4, 1), ControlFlowEditRules.ResolveInsertionTarget(steps, 1, true, 2, afterWholeBlock: true));
    }

    [Fact]
    public void DropIntoEmptyElseBranch_MovesTheWholeNestedBlock()
    {
        JobStep[] source = [new IfStep(), new TimeoutStep(), new EndIfStep()];
        JobStep[] target = [new IfStep(), new ElseStep(), new EndIfStep()];
        var slot = ControlFlowEditRules.ResolveInsertionTarget(target, 1, true, 1);
        Assert.True(ControlFlowEditRules.TryMove(source, target, [0], slot.Index, out var remaining, out var moved));
        Assert.Empty(remaining);
        Assert.Equal(target.Take(2).Concat(source).Append(target[2]), moved);
    }

    [Fact]
    public void MovingOuterBlockIntoItself_IsRejected()
    {
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new EndIfStep(), new TimeoutStep()];
        Assert.False(ControlFlowEditRules.TryMove(steps, steps, [0], 1, out _, out _));
        Assert.True(ControlFlowEditRules.TryMove(steps, steps, [0], 4, out _, out var moved));
        Assert.Equal([steps[3], steps[0], steps[1], steps[2]], moved);
    }

    [Fact]
    public void CrossPhaseMove_PreservesNestedBranchesAndTheirIds()
    {
        JobStep[] source = [new IfStep(), new IfStep(), new TimeoutStep(), new EndIfStep(), new EndIfStep()];
        JobStep[] target = [new TimeoutStep()];
        Assert.True(ControlFlowEditRules.TryMove(source, target, [0], 1, out var remaining, out var moved));
        Assert.Empty(remaining);
        Assert.Equal(target.Concat(source), moved);
    }

    [Fact]
    public void RemovingOuterCondition_PreservesNestedConditionsAndBranchOrder()
    {
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new IfStep(), new TimeoutStep(),
            new ElseStep(), new TimeoutStep(), new EndIfStep(), new ElseStep(), new TimeoutStep(), new EndIfStep()];
        var remove = ControlFlowEditRules.RemoveConditionMarkers(steps, 0);
        var remaining = steps.Where((_, index) => !remove.Contains(index)).ToArray();
        Assert.Equal([0, 7, 9], remove);
        Assert.True(ControlFlowStructureAnalyzer.Analyze(remaining).IsValid);
        Assert.Same(steps[2], remaining[1]);
        Assert.Same(steps[4], remaining[3]);
        Assert.Same(steps[8], remaining[^1]);
    }

    [Fact]
    public void SelectingOverlappingBlocks_ProducesOneCompleteOrderedUnit()
    {
        JobStep[] steps = [new TimeoutStep(), new IfStep(), new IfStep(), new TimeoutStep(),
            new EndIfStep(), new ElseStep(), new TimeoutStep(), new EndIfStep(), new TimeoutStep()];
        Assert.Equal(Enumerable.Range(1, 7), ControlFlowEditRules.ExpandSelection(steps, [2, 1, 4]));
    }

    [Fact]
    public void DamagedCondition_DoesNotConsumeUnrelatedSteps()
    {
        JobStep[] steps = [new IfStep(), new TimeoutStep()];
        Assert.Equal([0], ControlFlowEditRules.ExpandSelection(steps, [0]));
        Assert.Empty(ControlFlowEditRules.RemoveConditionMarkers(steps, 0));
    }
}
