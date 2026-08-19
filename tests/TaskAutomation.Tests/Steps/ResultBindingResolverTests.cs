using System.Drawing;
using System.Text.Json.Nodes;
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
    public void Resolve_ReadsTypedJobVariableThroughProviderReference()
    {
        var variable = new JobVariable
        {
            Id = Guid.NewGuid(),
            Name = "Timeout",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(45)
        };
        var store = new JobResultStore([variable]);
        var binding = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        };

        var result = ResultBindingResolver.Resolve<int>(store, binding);

        Assert.Equal(ResultResolutionStatus.Success, result.Status);
        Assert.Equal(45, result.FirstOrDefault);
    }

    [Fact]
    public void Resolve_ReadsSecretThroughProviderReference()
    {
        var secretId = Guid.NewGuid();
        var descriptor = new ValueProviderSourceDescriptor(
            ValueProviderIds.Secret,
            secretId.ToString("D"),
            "API token",
            string.Empty,
            ResultValueKind.Text,
            ResultCardinality.Single,
            IsSensitive: true);
        var store = new JobResultStore(
            secrets: new Dictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)>
            {
                [secretId] = (descriptor, "top-secret")
            });

        var result = ResultBindingResolver.Resolve<string>(store, new ResultBinding
        {
            ProviderId = ValueProviderIds.Secret,
            SourceId = secretId.ToString("D")
        });

        Assert.Equal(ResultResolutionStatus.Success, result.Status);
        Assert.Equal("top-secret", result.FirstOrDefault);
    }

    [Fact]
    public void Resolve_LoadsImageJobVariableAndReleasesFileOnDispose()
    {
        var path = Path.Combine(Path.GetTempPath(), $"job-variable-{Guid.NewGuid():N}.png");
        using (var source = new Bitmap(2, 3))
            source.Save(path);

        try
        {
            var variable = new JobVariable
            {
                Id = Guid.NewGuid(),
                Name = "Image",
                ValueKind = ResultValueKind.Image,
                Value = JsonValue.Create(path)
            };
            var store = new JobResultStore([variable]);

            var result = ResultBindingResolver.Resolve<Bitmap>(store, new ResultBinding
            {
                ProviderId = ValueProviderIds.JobVariable,
                SourceId = variable.Id.ToString("D")
            });

            Assert.Equal(ResultResolutionStatus.Success, result.Status);
            Assert.Equal(new Size(2, 3), Assert.IsType<Bitmap>(result.FirstOrDefault).Size);

            store.DisposeAndClear();
            File.Delete(path);
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
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
