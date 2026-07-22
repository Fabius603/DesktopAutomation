using TaskAutomation.Makros;

namespace TaskAutomation.Tests.Makros;

public sealed class MakroTimelineAndGroupingTests
{
    [Fact]
    public void Timeline_UsesCumulativeDelaysAndTimeoutDurations()
    {
        MakroBefehl[] commands =
        [
            new KeyDownBefehl { Key = "A", DelayBeforeMicroseconds = 500_000 },
            new TimeoutBefehl { Duration = 1_000, DelayBeforeMicroseconds = 250_000 },
            new KeyUpBefehl { Key = "A", DelayBeforeMicroseconds = 100_000 }
        ];

        var timeline = MakroTimeline.Calculate(commands);

        Assert.Equal([500_000L, 750_000L, 1_850_000L], timeline.Select(item => item.ExecutionTimeMicroseconds));
        Assert.Equal(1_850_000L, MakroTimeline.GetTotalDurationMicroseconds(commands));
    }

    [Fact]
    public void Timeline_IncludesFinalTimeoutInTotalDuration()
    {
        MakroBefehl[] commands = [new TimeoutBefehl { Duration = 1_500, DelayBeforeMicroseconds = 250_000 }];

        Assert.Equal(1_750_000L, MakroTimeline.GetTotalDurationMicroseconds(commands));
    }

    [Fact]
    public void AutomaticGrouping_GroupsOnlyConsecutiveMovementRunsWithAtLeastTwoSteps()
    {
        MakroBefehl[] commands =
        [
            new MouseMoveAbsoluteBefehl(),
            new MouseMoveAbsoluteBefehl(),
            new MouseDownBefehl { Button = "Left" },
            new MouseMoveRelativeBefehl(),
            new KeyDownBefehl { Key = "A" },
            new MouseMoveRelativeBefehl(),
            new MouseMoveRelativeBefehl(),
            new MouseMoveRelativeBefehl()
        ];

        var groups = MakroGrouping.CreateAutomaticMovementGroups(commands, "Movement");

        Assert.Equal(2, groups.Count);
        Assert.Equal("Movement 1", groups[0].Title);
        Assert.Equal(commands[0].GroupId, commands[1].GroupId);
        Assert.Null(commands[3].GroupId);
        Assert.Equal(commands[5].GroupId, commands[6].GroupId);
        Assert.Equal(commands[6].GroupId, commands[7].GroupId);
        Assert.NotEqual(commands[0].GroupId, commands[5].GroupId);
        Assert.All(groups, group => Assert.True(group.IsAutomatic));
    }

    [Fact]
    public void AutomaticGrouping_DoesNotOverwriteExistingGroups()
    {
        MakroBefehl[] commands =
        [
            new MouseMoveAbsoluteBefehl { GroupId = "manual" },
            new MouseMoveAbsoluteBefehl(),
            new MouseMoveAbsoluteBefehl()
        ];

        var group = Assert.Single(MakroGrouping.CreateAutomaticMovementGroups(commands));

        Assert.Equal("manual", commands[0].GroupId);
        Assert.Equal(group.Id, commands[1].GroupId);
        Assert.Equal(group.Id, commands[2].GroupId);
    }

    [Fact]
    public void MoveGroupBefore_MovesTheWholeGroupWithoutChangingItsInternalOrder()
    {
        var first = new KeyDownBefehl { Key = "A" };
        var groupedOne = new MouseMoveAbsoluteBefehl { GroupId = "movement" };
        var groupedTwo = new MouseMoveRelativeBefehl { GroupId = "movement" };
        var anchor = new KeyUpBefehl { Key = "A" };
        MakroBefehl[] commands = [first, groupedOne, groupedTwo, anchor];

        var result = MakroGrouping.MoveGroupBefore(commands, "movement", first);

        Assert.Equal([groupedOne, groupedTwo, first, anchor], result);
    }

    [Fact]
    public void MoveGroupBefore_WithNoAnchorMovesTheWholeGroupToTheEnd()
    {
        var groupedOne = new MouseMoveAbsoluteBefehl { GroupId = "movement" };
        var middle = new KeyDownBefehl { Key = "A" };
        var groupedTwo = new MouseMoveRelativeBefehl { GroupId = "movement" };
        MakroBefehl[] commands = [groupedOne, middle, groupedTwo];

        var result = MakroGrouping.MoveGroupBefore(commands, "movement", null);

        Assert.Equal([middle, groupedOne, groupedTwo], result);
    }
}
