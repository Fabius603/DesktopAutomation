using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class ResultBindingResolverTests
{
    private readonly JobResultStore _store = new();

    [Fact]
    public void Resolve_WhenBindingMissing_ReturnsNotConfigured() =>
        Assert.Equal(ResultResolutionStatus.NotConfigured, ResultBindingResolver.Resolve<object>(_store, null).Status);

    [Fact]
    public void Resolve_WhenSourceMissing_ReturnsSourceNotExecuted()
    {
        var result = ResultBindingResolver.Resolve<object>(_store, Binding("missing", "Text"));
        Assert.Equal(ResultResolutionStatus.SourceNotExecuted, result.Status);
    }

    [Fact]
    public void Resolve_WhenSourceWasNotExecuted_ReturnsSourceNotExecuted()
    {
        _store.Set<WindowsStateQueryStep>(new ClipboardContentQueryResult { WasExecuted = false, Text = "old" }, "source");
        Assert.Equal(ResultResolutionStatus.SourceNotExecuted,
            ResultBindingResolver.Resolve<object>(_store, Binding("source", "Text")).Status);
    }

    [Fact]
    public void Resolve_ReadsPropertyCaseInsensitively()
    {
        _store.Set<WindowsStateQueryStep>(new AudioVolumeQueryResult { WasExecuted = true, Percentage = 72.5 }, "source");
        var result = ResultBindingResolver.Resolve<object>(_store, Binding("SOURCE", "percentage"));
        Assert.Equal(ResultResolutionStatus.Success, result.Status);
        Assert.Equal(72.5, Assert.IsType<double>(result.FirstOrDefault));
    }

    [Fact]
    public void Resolve_ReadsPropertyByStableIdWithoutLegacyPath()
    {
        _store.Set<WindowsStateQueryStep>(
            new AudioVolumeQueryResult { WasExecuted = true, Percentage = 42.5 }, "source");
        var result = ResultBindingResolver.Resolve<double>(_store, new ResultBinding
        {
            SourceStepId = "source",
            PropertyId = "volume_percentage"
        });
        Assert.Equal(ResultResolutionStatus.Success, result.Status);
        Assert.Equal(42.5, result.FirstOrDefault);
    }

    [Fact]
    public void Resolve_WhenPropertyMissing_ReturnsPropertyNotFound()
    {
        _store.Set<WindowsStateQueryStep>(new AudioVolumeQueryResult { WasExecuted = true }, "source");
        Assert.Equal(ResultResolutionStatus.PropertyNotFound,
            ResultBindingResolver.Resolve<object>(_store, Binding("source", "DoesNotExist")).Status);
    }

    [Fact]
    public void Resolve_WhenValueNull_ReturnsValueIsNull()
    {
        _store.Set<TestStep>(new NullableResult { WasExecuted = true }, "source");
        Assert.Equal(ResultResolutionStatus.ValueIsNull,
            ResultBindingResolver.Resolve<object>(_store, Binding("source", "Value")).Status);
    }

    [Fact]
    public void Resolve_WhenRequestedTypeDoesNotMatch_ReturnsTypeMismatch()
    {
        _store.Set<WindowsStateQueryStep>(new ClipboardContentQueryResult { WasExecuted = true, Text = "abc" }, "source");
        Assert.Equal(ResultResolutionStatus.TypeMismatch,
            ResultBindingResolver.Resolve<Point>(_store, Binding("source", "Text")).Status);
    }

    [Fact]
    public void Resolve_FlattensCollections()
    {
        _store.Set<TestStep>(new CollectionResult { WasExecuted = true, Values = [1, 2, 3] }, "source");
        var result = ResultBindingResolver.Resolve<int>(_store, Binding("source", "Values"));
        Assert.Equal([1, 2, 3], result.Values);
    }

    [Fact]
    public void Resolve_EmptyCollection_ReturnsEmptyCollection()
    {
        _store.Set<TestStep>(new CollectionResult { WasExecuted = true, Values = [] }, "source");
        Assert.Equal(ResultResolutionStatus.EmptyCollection,
            ResultBindingResolver.Resolve<int>(_store, Binding("source", "Values")).Status);
    }

    [Fact]
    public void TryReadPath_ProjectsMembersFromCollection()
    {
        var source = new Parent([new Child("A"), new Child("B")]);
        Assert.True(ResultBindingResolver.TryReadPath(source, "Children[].Name", out var value));
        Assert.Equal(["A", "B"], Assert.IsType<List<object?>>(value).Cast<string>());
    }

    private static ResultBinding Binding(string id, string path) => new() { SourceStepId = id, PropertyPath = path };
    private sealed class TestStep : JobStep;
    private sealed record NullableResult : StepResultBase { public string? Value { get; init; } }
    private sealed record CollectionResult : StepResultBase { public IReadOnlyList<int> Values { get; init; } = []; }
    private sealed record Parent(IReadOnlyList<Child> Children);
    private sealed record Child(string Name);
}
