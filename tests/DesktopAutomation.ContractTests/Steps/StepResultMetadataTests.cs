using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.Steps;

public sealed class StepResultMetadataTests
{
    [Fact]
    public void CaptureAndDetectionContracts_ExposeStableFrameProvenanceIds()
    {
        foreach (var type in new[] { typeof(DesktopDuplicationResult), typeof(CameraCaptureResult) })
        {
            var properties = StepResultMetadata.GetResultType(type.Name)!.Properties;
            Assert.Contains(properties, property => property.Name == "FrameVersion" && property.StableId == "frame_version");
            Assert.Contains(properties, property => property.Name == "FrameTimestamp" && property.StableId == "frame_timestamp");
        }
        foreach (var type in new[]
        {
            typeof(ColorDetectionResult), typeof(TemplateMatchingResult), typeof(KeyPointMatchingResult),
            typeof(YOLODetectionResult), typeof(OcrResult), typeof(PredictMovementResult)
        })
        {
            var properties = StepResultMetadata.GetResultType(type.Name)!.Properties;
            Assert.Contains(properties, property => property.Name == "SourceFrameVersion" && property.StableId == "source_frame_version");
            Assert.Contains(properties, property => property.Name == "SourceFrameTimestamp" && property.StableId == "source_frame_timestamp");
        }
        var movement = StepResultMetadata.GetResultType(nameof(KlickOnPoint3DResult))!;
        Assert.Contains(movement.Properties, property => property.Name == "MovementBlocked"
            && property.StableId == "click_on_point_3d.movement_blocked");
    }

    [Fact]
    public void WindowsStateResult_ExposesPayloadButHidesExecutionMarker()
    {
        var descriptor = StepResultMetadata.GetResultType(nameof(AudioVolumeQueryResult));
        Assert.NotNull(descriptor);
        Assert.Contains(descriptor.Properties, property => property.Name == nameof(AudioVolumeQueryResult.Percentage));
        Assert.DoesNotContain(descriptor.Properties, property => property.Name == nameof(StepResultBase.WasExecuted));
        Assert.DoesNotContain(descriptor.Properties, property => property.StableId == "is_available");
    }

    [Fact]
    public void WindowsStateResults_RemoveRedundantAliasesAndKeepStableSemanticIds()
    {
        var fileSystem = StepResultMetadata.GetResultType(nameof(FileSystemPathQueryResult));
        var process = StepResultMetadata.GetResultType(nameof(ProcessRunningQueryResult));
        var foreground = StepResultMetadata.GetResultType(nameof(ForegroundWindowQueryResult));
        var update = StepResultMetadata.GetResultType(nameof(WindowsUpdateStatusQueryResult));
        var idle = StepResultMetadata.GetResultType(nameof(InputIdleQueryResult));
        var lifecycle = StepResultMetadata.GetResultType(nameof(SystemLifecycleQueryResult));

        Assert.DoesNotContain(fileSystem!.Properties, property => property.Name == "IsActive");
        Assert.DoesNotContain(process!.Properties, property => property.Name == "IsActive");
        Assert.DoesNotContain(foreground!.Properties, property => property.Name == "IsActive");
        Assert.DoesNotContain(update!.Properties, property => property.Name == "IsActive");
        Assert.Equal("value", Assert.Single(idle!.Properties,
            property => property.Name == "IdleMilliseconds").StableId);
        Assert.Equal("percentage", Assert.Single(idle.Properties,
            property => property.Name == "IdleSeconds").StableId);
        Assert.Equal("text", Assert.Single(lifecycle!.Properties,
            property => property.Name == "OperatingSystemVersion").StableId);
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
    public void UserChoiceContract_UsesStableIdsWithConfiguredDisplayNames()
    {
        var step = new UserChoiceStep
        {
            Settings = new()
            {
                Options =
                [
                    new() { Id = "first-id", Label = "First" },
                    new() { Id = "second-id", Label = "Second" }
                ]
            }
        };

        var contract = StepResultMetadata.GetResultTypeForStep(step);
        var selected = Assert.Single(contract!.Properties,
            property => property.StableId == "selected_option_id");

        Assert.Equal(ResultValueKind.Enum, selected.DataType);
        Assert.Equal(["first-id", "second-id"], selected.EnumValues);
        Assert.Equal("Second", selected.EnumDisplayNames!["second-id"]);

        step.Settings.Options.Reverse();
        var reordered = StepResultMetadata.GetResultTypeForStep(step);
        var reorderedSelected = Assert.Single(reordered!.Properties,
            property => property.StableId == "selected_option_id");
        Assert.Equal("Second", reorderedSelected.EnumDisplayNames!["second-id"]);
    }

    [Fact]
    public void EveryStaticEnumResultProperty_ExposesItsValues()
    {
        var enumProperties = StepResultMetadata.ResultTypes
            .SelectMany(resultType => resultType.Properties.Select(property => (resultType.TypeName, Property: property)))
            .Where(item => item.Property.DataType == ResultValueKind.Enum)
            .ToArray();

        Assert.NotEmpty(enumProperties);
        Assert.All(enumProperties, item =>
            Assert.True(item.Property.EnumValues is { Count: > 0 },
                $"{item.TypeName}.{item.Property.Name} has no enum values."));
    }

    [Fact]
    public void TryReadValue_ReadsScalarAndCollectionCount()
    {
        var result = new NetworkConnectivityQueryResult { Count = 2, Items = ["a", "b"] };
        Assert.True(StepResultMetadata.TryGetProperty(nameof(NetworkConnectivityQueryResult), "Count", out var percentage));
        Assert.True(StepResultMetadata.TryReadValue(result, percentage, out var scalar));
        Assert.Equal(2L, scalar);
        Assert.True(StepResultMetadata.TryGetProperty(nameof(NetworkConnectivityQueryResult), "Items.Count", out var count));
        Assert.True(StepResultMetadata.TryReadValue(result, count, out var countValue));
        Assert.Equal(2, countValue);
    }

    [Fact]
    public void ResultPropertyContract_UsesExplicitStableId()
    {
        var descriptor = StepResultMetadata.GetResultType(nameof(AudioVolumeQueryResult));
        var percentage = Assert.Single(descriptor!.Properties, property => property.Name == "Percentage");
        Assert.Equal("volume_percentage", percentage.StableId);
    }

    [Fact]
    public void EveryWindowsStateQuery_HasAConcreteResultContract()
    {
        var queries = new WindowsCapabilityCatalog().Capabilities.Where(capability => capability.SupportsStateQuery);
        Assert.All(queries, query =>
        {
            Assert.False(string.IsNullOrWhiteSpace(query.ResultTypeName));
            var contract = WindowsQueryResultRegistry.GetContract(query.Id);
            Assert.NotNull(contract);
            Assert.Equal(query.ResultTypeName, contract.TypeName);
            Assert.NotEmpty(contract.Properties);
            Assert.Equal(query.ResultTypeName,
                WindowsQueryResultRegistry.Create(query.Id, new WindowsStateSnapshot()).GetType().Name);
        });
    }

    [Fact]
    public void EveryResultContract_HasUniqueStablePropertyIds()
    {
        Assert.All(StepResultMetadata.ResultTypes, contract =>
        {
            var ids = contract.Properties.Select(property => property.StableId).ToArray();
            Assert.Equal(ids.Length, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        });
    }

    [Fact]
    public void AreComparable_RequiresSameTypeEnumAndCardinality()
    {
        var scalar = new ResultPropertyDescriptor("a", "a", ResultValueKind.Integer);
        var same = new ResultPropertyDescriptor("b", "b", ResultValueKind.Integer);
        var collection = same with { Cardinality = ResultCardinality.Collection };
        Assert.True(StepResultMetadata.AreComparable(scalar, same));
        Assert.False(StepResultMetadata.AreComparable(scalar, collection));
        Assert.False(StepResultMetadata.AreComparable(scalar,
            new ResultPropertyDescriptor("c", "c", ResultValueKind.Number)));

        var typedEnum = new ResultPropertyDescriptor(
            "mode", "mode", ResultValueKind.Enum, EnumTypeName: "ChoiceMode");
        var sameEnum = new ResultPropertyDescriptor(
            "other", "other", ResultValueKind.Enum, EnumTypeName: "ChoiceMode");
        var differentEnum = new ResultPropertyDescriptor(
            "other", "other", ResultValueKind.Enum, EnumTypeName: "WindowMode");
        var untypedEnum = new ResultPropertyDescriptor("value", "value", ResultValueKind.Enum);
        Assert.True(StepResultMetadata.AreComparable(typedEnum, sameEnum));
        Assert.False(StepResultMetadata.AreComparable(typedEnum, differentEnum));
        Assert.False(StepResultMetadata.AreComparable(typedEnum, untypedEnum));
        Assert.False(StepResultMetadata.AreComparable(untypedEnum, typedEnum));
    }

    [Fact]
    public void ResultPropertyTree_GroupsNestedPaths()
    {
        var tree = ResultPropertyTree.Create([
            new("Process.Id", "Process / Id", ResultValueKind.Integer),
            new("Process.Name", "Process / Name", ResultValueKind.Text)]);
        var process = Assert.Single(tree);
        Assert.Equal("Process", process.Segment);
        Assert.Equal(2, process.Children.Count);
    }
}
