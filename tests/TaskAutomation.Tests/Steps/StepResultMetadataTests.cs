using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class StepResultMetadataTests
{
    [Fact]
    public void WindowsStateResult_ExposesPayloadButHidesExecutionMarker()
    {
        var descriptor = StepResultMetadata.GetResultType(nameof(WindowsStateQueryResult));
        Assert.NotNull(descriptor);
        Assert.Contains(descriptor.Properties, property => property.Name == nameof(WindowsStateQueryResult.Percentage));
        Assert.DoesNotContain(descriptor.Properties, property => property.Name == nameof(StepResultBase.WasExecuted));
    }

    [Fact]
    public void GetResultTypeForStep_FiltersWindowsPropertiesByCapability()
    {
        var audio = StepResultMetadata.GetResultTypeForStep(new WindowsStateQueryStep
            { Settings = new() { QueryType = "audio.volume" } });
        Assert.NotNull(audio);
        Assert.Contains(audio.Properties, property => property.Name == "Percentage");
        Assert.Contains(audio.Properties, property => property.Name == "IsMuted");
        Assert.DoesNotContain(audio.Properties, property => property.Name == "FreeSpaceGb");
    }

    [Fact]
    public void GetConditionSources_ReturnsOnlyEnabledPriorResultSteps()
    {
        var first = new WindowsStateQueryStep { Id = "first" };
        var disabled = new WindowsStateQueryStep { Id = "disabled", IsEnabled = false };
        var consumer = new IfStep();
        var later = new WindowsStateQueryStep { Id = "later" };
        var sources = StepResultMetadata.GetConditionSources([first, disabled, consumer, later], 2);
        var source = Assert.Single(sources);
        Assert.Same(first, source.Step);
    }

    [Fact]
    public void TryReadValue_ReadsScalarAndCollectionCount()
    {
        var result = new WindowsStateQueryResult { Percentage = 55, Items = ["a", "b"] };
        Assert.True(StepResultMetadata.TryGetProperty(nameof(WindowsStateQueryResult), "Percentage", out var percentage));
        Assert.True(StepResultMetadata.TryReadValue(result, percentage, out var scalar));
        Assert.Equal(55d, scalar);
        Assert.True(StepResultMetadata.TryGetProperty(nameof(WindowsStateQueryResult), "Items.Count", out var count));
        Assert.True(StepResultMetadata.TryReadValue(result, count, out var countValue));
        Assert.Equal(2, countValue);
    }

    [Fact]
    public void AreComparable_RequiresSameTypeEnumAndCardinality()
    {
        var scalar = new ResultPropertyDescriptor("a", "a", ResultPropertyType.Integer);
        var same = new ResultPropertyDescriptor("b", "b", ResultPropertyType.Integer);
        var collection = same with { Cardinality = ResultCardinality.Collection };
        Assert.True(StepResultMetadata.AreComparable(scalar, same));
        Assert.False(StepResultMetadata.AreComparable(scalar, collection));
        Assert.False(StepResultMetadata.AreComparable(scalar,
            new ResultPropertyDescriptor("c", "c", ResultPropertyType.Double)));
    }

    [Fact]
    public void ResultPropertyTree_GroupsNestedPaths()
    {
        var tree = ResultPropertyTree.Create([
            new("Process.Id", "Process / Id", ResultPropertyType.Integer),
            new("Process.Name", "Process / Name", ResultPropertyType.String)]);
        var process = Assert.Single(tree);
        Assert.Equal("Process", process.Segment);
        Assert.Equal(2, process.Children.Count);
    }
}
