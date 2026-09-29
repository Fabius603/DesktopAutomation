using TaskAutomation.Makros;

namespace TaskAutomation.Tests.Makros;

public sealed class MakroSnapshotServiceTests
{
    [Fact]
    public void CloneCommand_PreservesPersistedMeaningAndCreatesIndependentObject()
    {
        var source = new TimeoutBefehl
        {
            Id = "command",
            GroupId = "group",
            Duration = 250
        };

        var clone = Assert.IsType<TimeoutBefehl>(MakroSnapshotService.CloneCommand(source));

        Assert.NotSame(source, clone);
        Assert.Equal(source.Id, clone.Id);
        Assert.Equal(source.GroupId, clone.GroupId);
        Assert.Equal(source.Duration, clone.Duration);
    }

    [Fact]
    public void CloneGroup_PreservesPersistedMeaningAndCreatesIndependentObject()
    {
        var source = new MakroGruppe { Id = "group", Title = "Example", IsAutomatic = true };

        var clone = MakroSnapshotService.CloneGroup(source);

        Assert.NotSame(source, clone);
        Assert.Equal(source.Id, clone.Id);
        Assert.Equal(source.Title, clone.Title);
        Assert.Equal(source.IsAutomatic, clone.IsAutomatic);
    }
}
