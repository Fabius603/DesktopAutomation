using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class CollectionSelectionRulesTests
{
    [Fact]
    public void Move_PreservesRelativeOrderForContiguousAndSeparateSelections()
    {
        Assert.Equal([1, 2, 0, 3, 4], CollectionSelectionRules.MoveOrder(5, [1, 2], -1));
        Assert.Equal([0, 2, 1, 4, 3], CollectionSelectionRules.MoveOrder(5, [1, 3], 1));
        Assert.Null(CollectionSelectionRules.MoveOrder(5, [0, 2], -1));
        Assert.Null(CollectionSelectionRules.MoveOrder(5, [2, 4], 1));
        Assert.Null(CollectionSelectionRules.MoveOrder(5, [], 1));
    }

    [Fact]
    public void BatchLimits_RejectEntireOperationRatherThanPartialMutation()
    {
        Assert.False(CollectionSelectionRules.CanRemove(3, 2, 2));
        Assert.True(CollectionSelectionRules.CanRemove(4, 2, 2));
        Assert.False(CollectionSelectionRules.CanDuplicate(17, 2, 18));
        Assert.True(CollectionSelectionRules.CanDuplicate(16, 2, 18));
        Assert.False(CollectionSelectionRules.CanRemove(2, 0, 0));
    }
}
