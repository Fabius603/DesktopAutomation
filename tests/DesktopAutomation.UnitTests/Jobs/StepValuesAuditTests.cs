using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Jobs;

public sealed class StepValuesAuditTests
{
    [Theory]
    [InlineData("yolo_detection", "roi", "{\"enabled\":\"bad\"}")]
    [InlineData("video_creation", "overlay", "{\"text_results\":[42]}")]
    [InlineData("show_on_desktop", "overlay", "{\"text_results\":null}")]
    [InlineData("focus_process", "process_target", "{\"process_source\":42}")]
    [InlineData("point_comparison", "points", "[null]")]
    public void InvalidSpecializedShapesAreReportedWithoutFallingBackToEmptyValues(string typeId, string fieldId, string json)
    {
        var definition = BuiltInStepDefinitions.Instance.Definitions.Single(item => item.Descriptor.TypeId == typeId);
        var draft = definition.CreateDraft(); draft.Values[fieldId] = JsonNode.Parse(json);
        Assert.Contains(definition.ValidateDraft(draft), issue => issue.FieldId == fieldId);
    }

    [Theory]
    [InlineData("#123", true)]
    [InlineData("#80112233", true)]
    [InlineData("#A123", true)]
    [InlineData("Red", true)]
    [InlineData("not-a-color", false)]
    public void ColorValuesHaveOneValidationAndExecutionParser(string token, bool valid)
    {
        var variable = new JobVariable { ValueKind = ResultValueKind.Color, Value = JsonValue.Create(token) };
        Assert.Equal(valid, JobVariableValueRules.IsValid(variable));
        Assert.Equal(valid, ColorValueRules.TryParse(token, out _));
    }

    [Theory]
    [InlineData(ValueProviderIds.JobVariable, false)]
    [InlineData(ValueProviderIds.LocalValue, true)]
    public void StoredSourceIdentityIncludesItsProvider(string provider, bool valid)
    {
        var value = new LocalValue { ValueKind = ResultValueKind.Integer, Value = JsonValue.Create(5) };
        var step = new TimeoutStep();
        var field = new TimeoutStepDefinition().Descriptor.Fields.Single().Id;
        step.Inputs[field] = new ResultBinding { ProviderId = provider, SourceId = value.Id.ToString("D") };
        var job = new Job { Steps = [step], LocalValues = [value] };

        Assert.Equal(valid, JobValidation.ValidateJob(job).IsValid);
        var results = new JobResultStore(localValues: [value]);
        if (valid) Assert.NotNull(StepInputMaterializer.Materialize(step, results));
        else Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(step, results));
    }

    [Fact]
    public void EqualIdsAcrossProvidersAreIndependentWhileDuplicatesWithinAProviderAreRejected()
    {
        var value = new JobVariable { ValueKind = ResultValueKind.Integer, Value = JsonValue.Create(3) };
        var local = new LocalValue { Id = value.Id, ValueKind = value.ValueKind, Value = JsonValue.Create(8) };
        var job = new Job { Variables = [value], LocalValues = [local] };
        Assert.True(JobValidation.ValidateJob(job).IsValid);
        var store = new JobResultStore([value], localValues: [local]);
        Assert.Equal(3, store.ReadProvider(ValueProviderIds.JobVariable, value.Id.ToString()).Value);
        Assert.Equal(8, store.ReadProvider(ValueProviderIds.LocalValue, value.Id.ToString()).Value);
        job.Variables.Add(new JobVariable { Id = value.Id });
        Assert.Contains("StepValidation.DuplicateValueId", JobValidation.ValidateJob(job).Errors!);
        Assert.Throws<ArgumentException>(() => new JobResultStore(job.Variables));
    }

    [Fact]
    public void DuplicateStepIdsAndIncorrectLocalOwnershipAreReported()
    {
        var step = new TimeoutStep();
        var local = new LocalValue
        {
            OwnerStepId = "another-step",
            InputPath = "wrong-field",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(2)
        };
        step.Inputs[new TimeoutStepDefinition().Descriptor.Fields.Single().Id] = Reference(local);
        var job = new Job { Steps = [step, new TimeoutStep { Id = step.Id }], LocalValues = [local] };
        var result = JobValidation.ValidateJob(job);
        Assert.Contains("StepValidation.DuplicateStepId", result.Errors!);
        Assert.Contains("StepValidation.LocalValueOwnership", result.Errors!);
    }

    [Fact]
    public void InactiveInvalidEnumDoesNotPreventMinimizeButRemainsPersisted()
    {
        var step = new FocusProcessStep { Settings = new() { Action = FocusProcessAction.Minimize } };
        step.Settings.Target.ProcessName = "test-process";
        var job = new Job { Steps = [step] };
        JobVariableInputMigration.Migrate(job);
        var inactive = job.LocalValues.Single(value => value.InputPath == FocusProcessStepDefinition.WindowModeFieldId);
        inactive.Value = JsonValue.Create("unknown-mode");
        Assert.True(JobValidation.ValidateJob(job).IsValid);

        var resolved = Assert.IsType<FocusProcessStep>(StepInputMaterializer.Materialize(step,
            new JobResultStore(localValues: job.LocalValues)));
        Assert.Equal(FocusProcessAction.Minimize, resolved.Settings.Action);
        Assert.Equal("unknown-mode", inactive.Value.GetValue<string>());
        var action = job.LocalValues.Single(value => value.InputPath == FocusProcessStepDefinition.ActionFieldId);
        action.Value = JsonValue.Create(nameof(FocusProcessAction.BringToFront));
        Assert.False(JobValidation.ValidateJob(job).IsValid);
        Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(step,
            new JobResultStore(localValues: job.LocalValues)));
    }

    [Theory]
    [InlineData("[42,99]")]
    [InlineData("[null,null]")]
    [InlineData("[{\"id\":\"a\",\"label\":\"A\"},42]")]
    public void MalformedUserChoiceOptionsAreRejectedAndCanStillBeReopened(string json)
    {
        var step = new UserChoiceStep();
        var local = new LocalValue
        {
            ValueKind = ResultValueKind.ResultObject,
            Cardinality = ResultCardinality.Collection,
            Value = JsonNode.Parse(json)
        };
        step.Inputs[UserChoiceStepDefinition.OptionsFieldId] = Reference(local);
        var job = new Job { Steps = [step], LocalValues = [local] };
        Assert.False(JobValidation.ValidateJob(job).IsValid);
        Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(step,
            new JobResultStore(localValues: [local])));
        var options = new JsonSerializerOptions();
        JobJsonSerialization.Configure(options);
        var reopened = JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(job, options), options)!;
        Assert.Equal(json, reopened.LocalValues.Single().Value!.ToJsonString());
        Assert.False(JobValidation.ValidateJob(reopened).IsValid);
    }

    [Fact]
    public void ProviderOnlyCollectionItemOverrideIsValidatedAndComposed()
    {
        var step = new UserChoiceStep();
        var root = new LocalValue
        {
            ValueKind = ResultValueKind.ResultObject,
            Cardinality = ResultCardinality.Collection,
            Value = JsonNode.Parse("[{\"id\":\"a\",\"label\":\"old\",\"value\":\"a\"},{\"id\":\"b\",\"label\":\"B\",\"value\":\"b\"}]")
        };
        var item = new JobVariable
        {
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonNode.Parse("{\"id\":\"a\",\"label\":\"new\",\"value\":\"a\"}")
        };
        var binding = Reference(root);
        binding.SchemaId = ValueBindingSchemaRegistry.UserChoiceOptions;
        binding.Items = [Reference(item)];
        step.Inputs[UserChoiceStepDefinition.OptionsFieldId] = binding;
        var job = new Job { Steps = [step], Variables = [item], LocalValues = [root] };
        Assert.True(JobValidation.ValidateJob(job).IsValid);
        Assert.Equal("new", Assert.IsType<UserChoiceStep>(StepInputMaterializer.Materialize(step,
            new JobResultStore([item], localValues: [root]))).Settings.Options[0].Label);

        item.ValueKind = ResultValueKind.Text;
        item.Value = JsonValue.Create("bad");
        Assert.False(JobValidation.ValidateJob(job).IsValid);
        Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(step,
            new JobResultStore([item], localValues: [root])));
    }

    [Fact]
    public void IncompatibleSchemaIsNotSilentlyRewrittenByMigration()
    {
        var step = new UserChoiceStep();
        step.Inputs[UserChoiceStepDefinition.OptionsFieldId] = new ResultBinding
        {
            SchemaId = ValueBindingSchemaRegistry.Roi,
            Items = [new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = Guid.NewGuid().ToString() }]
        };
        var job = new Job { Steps = [step] };
        JobVariableInputMigration.Migrate(job);
        Assert.Equal(ValueBindingSchemaRegistry.Roi, step.Inputs[UserChoiceStepDefinition.OptionsFieldId].SchemaId);
        Assert.False(JobValidation.ValidateJob(job).IsValid);
    }

    [Theory]
    [InlineData(ResultValueKind.Integer, "[1,2,3]", typeof(int))]
    [InlineData(ResultValueKind.Number, "[1.5,2]", typeof(double))]
    [InlineData(ResultValueKind.Boolean, "[true,false]", typeof(bool))]
    [InlineData(ResultValueKind.Text, "[\"a\",\"b\"]", typeof(string))]
    public void PrimitiveCollectionsHaveMatchingStoredAndRuntimeShapes(ResultValueKind kind, string json, Type elementType)
    {
        var value = new JobVariable { ValueKind = kind, Cardinality = ResultCardinality.Collection, Value = JsonNode.Parse(json) };
        Assert.True(JobVariableValueRules.IsValid(value));
        var resolved = ValueReferenceResolver.Resolve(new JobResultStore([value]), Reference(value));
        Assert.True(resolved.IsSuccess);
        Assert.Equal(kind, resolved.Descriptor!.ValueKind);
        Assert.Equal(ResultCardinality.Collection, resolved.Descriptor.Cardinality);
        Assert.Equal(elementType, resolved.Value!.GetType().GetElementType());
    }

    [Fact]
    public void JsonArrayPropertyHasMatchingAuthoringProviderAndTypedResolution()
    {
        var value = new JobVariable { ValueKind = ResultValueKind.ResultObject, Value = JsonNode.Parse("{\"scores\":[1,2]}") };
        var property = JobVariablePropertyMetadata.GetProperties(value).Single(item => item.Name == "scores");
        Assert.Equal(ResultValueKind.Integer, property.DataType);
        Assert.Equal(ResultCardinality.Collection, property.Cardinality);
        var binding = Reference(value); binding.ValuePath = "scores";
        var store = new JobResultStore([value]);
        var resolved = ValueReferenceResolver.Resolve(store, binding);
        Assert.Equal(property.DataType, resolved.Descriptor!.ValueKind);
        Assert.Equal(property.Cardinality, resolved.Descriptor.Cardinality);
        Assert.Equal([1, 2], ResultBindingResolver.Resolve<int>(store, binding).Values);
    }

    [Fact]
    public void MixedNumericJsonArrayDoesNotLoseIntegerElementsWhenReadAsNumbers()
    {
        var value = new JobVariable { ValueKind = ResultValueKind.ResultObject, Value = JsonNode.Parse("{\"scores\":[1.5,2]}") };
        var property = JobVariablePropertyMetadata.GetProperties(value).Single(item => item.Name == "scores");
        Assert.Equal(ResultValueKind.Number, property.DataType);
        var binding = Reference(value); binding.ValuePath = "scores";
        var resolved = ResultBindingResolver.Resolve<double>(new JobResultStore([value]), binding);
        Assert.True(resolved.IsSuccess);
        Assert.Equal([1.5, 2d], resolved.Values);
    }

    [Fact]
    public void InvalidMixedCollectionDoesNotReturnOnlyItsMatchingElements()
    {
        var value = new JobVariable { ValueKind = ResultValueKind.ResultObject, Value = JsonNode.Parse("{\"scores\":[1,\"bad\"]}") };
        var binding = Reference(value); binding.ValuePath = "scores";
        var resolved = ResultBindingResolver.Resolve<int>(new JobResultStore([value]), binding);
        Assert.Equal(ResultResolutionStatus.TypeMismatch, resolved.Status);
        Assert.Empty(resolved.Values);
    }

    [Theory]
    [InlineData(JobVariableScope.StepValue)]
    [InlineData(JobVariableScope.Shared)]
    public void AllowedOverlayJobVariableRemainsValidRegardlessOfLegacyScope(JobVariableScope scope)
    {
        var value = new JobVariable { Scope = scope, ValueKind = ResultValueKind.Text, Value = JsonValue.Create("Status") };
        var step = new ShowOnDesktopStep
        {
            Settings = new() { Overlay = new() { TextResults = [new() { Result = Reference(value) }] } }
        };
        Assert.True(JobValidation.ValidateCandidate([], step, variables: [value]).IsValid);
        var materialized = Assert.IsType<ShowOnDesktopStep>(StepInputMaterializer.Materialize(step, new JobResultStore([value])));
        Assert.Equal(value.Id.ToString("D"), Assert.Single(materialized.Settings.Overlay.TextResults).Result.SourceId);
    }

    [Fact]
    public void PersistedSecretTextRemainsReadableWithoutEnablingNewSecretSelections()
    {
        var id = Guid.NewGuid();
        var descriptor = new ValueProviderSourceDescriptor(ValueProviderIds.Secret, id.ToString("D"), "Token", null,
            ResultValueKind.Text, ResultCardinality.Single, IsSensitive: true);
        var step = new ShowTextStep
        {
            Settings = new()
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new() { ProviderId = ValueProviderIds.Secret, SourceId = id.ToString("D") }
            }
        };
        var job = new Job { Steps = [step] };
        JobVariableInputMigration.Migrate(job);
        Assert.True(JobValidation.ValidateCandidate([], step, providerSources: [descriptor], variables: job.LocalValues).IsValid);
        var contract = StepInputContractRegistry.Get(typeof(ShowTextStep), "text")!;
        Assert.False(contract.AllowsProvider(ValueProviderIds.Secret));
        Assert.True(contract.AcceptsSource(ValueProviderIds.Secret, descriptor.ToResultProperty()));
        Assert.False(contract.AcceptsSource(ValueProviderIds.Secret, descriptor.ToResultProperty(), includeLegacy: false));
        var results = new JobResultStore(secrets: new Dictionary<Guid, (ValueProviderSourceDescriptor, string)>
        { [id] = (descriptor, "runtime token") }, localValues: job.LocalValues);
        var materialized = Assert.IsType<ShowTextStep>(StepInputMaterializer.Materialize(step, results));
        Assert.Equal("runtime token", ResultBindingResolver.Resolve<string>(results, materialized.Settings.TextResult).FirstOrDefault);
    }

    [Fact]
    public void PersistedNestedSecretUsesTheSameSourcePolicyInAuthoringAndExecution()
    {
        var id = Guid.NewGuid();
        var descriptor = new ValueProviderSourceDescriptor(ValueProviderIds.Secret, id.ToString("D"), "Window title", null,
            ResultValueKind.Text, ResultCardinality.Single, IsSensitive: true);
        var step = new ActiveProcessStep(); step.Settings.Target.ProcessName = "notepad";
        ValueBindingTree.Set(step.Inputs, "process_target.window_title_contains",
            new() { ProviderId = ValueProviderIds.Secret, SourceId = id.ToString("D") });
        var job = new Job { Steps = [step] };
        JobVariableInputMigration.Migrate(job);
        Assert.True(JobValidation.ValidateCandidate([], step, variables: job.LocalValues, providerSources: [descriptor]).IsValid);
        var results = new JobResultStore(secrets: new Dictionary<Guid, (ValueProviderSourceDescriptor, string)>
        { [id] = (descriptor, "resolved title") }, localValues: job.LocalValues);
        var materialized = Assert.IsType<ActiveProcessStep>(StepInputMaterializer.Materialize(step, results));
        Assert.Equal("resolved title", materialized.Settings.Target.WindowTitleContains);
    }

    [Fact]
    public void DynamicEnumMetadataMatchesGenericAndProviderReads()
    {
        var choice = new UserChoiceStep { Settings = new() { Options = [new() { Id = "a", Label = "A" }, new() { Id = "b", Label = "B" }] } };
        var store = new JobResultStore(); store.RegisterStep(choice);
        store.Set<UserChoiceStep>(new UserChoiceResult { WasExecuted = true, SelectedOptionId = "b" }, choice.Id);
        var binding = ResultBinding.ForStepResult(choice.Id, "selected_option_id");
        var generic = ValueReferenceResolver.Resolve(store, binding);
        var provider = store.ReadProvider(ValueProviderIds.StepResult, binding.SourceId);
        var contract = StepResultMetadata.GetResultTypeForStep(choice)!.Properties.Single(property => property.StableId == "selected_option_id");
        Assert.Equal(contract.EnumTypeName, generic.Descriptor!.EnumTypeName);
        Assert.Equal(generic.Descriptor.EnumTypeName, provider.Descriptor!.EnumTypeName);
        Assert.Equal(contract.EnumValues, generic.Descriptor.EnumValues);
        Assert.Equal("b", ResultBindingResolver.Resolve<string>(store, binding).FirstOrDefault);
    }

    [Fact]
    public void FixedStepEnumRejectsAResultProviderEvenWhenItsTokenMatches()
    {
        var choice = new UserChoiceStep
        {
            Settings = new()
            {
                Options =
            [new() { Id = "Minimize", Label = "Minimize" }, new() { Id = "BringToFront", Label = "Show" }]
            }
        };
        var focus = new FocusProcessStep(); focus.Settings.Target.ProcessName = "test-process";
        focus.Inputs[FocusProcessStepDefinition.ActionFieldId] = ResultBinding.ForStepResult(choice.Id, "selected_option_id");
        Assert.False(JobValidation.ValidateCandidate([choice], focus).IsValid);
        var results = new JobResultStore(); results.RegisterStep(choice);
        results.Set<UserChoiceStep>(new UserChoiceResult { WasExecuted = true, SelectedOptionId = "Minimize" }, choice.Id);
        Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(focus, results));
    }

    [Fact]
    public void ContextAwareConvenienceValidationUsesTheSameStoredValues()
    {
        var step = new TimeoutStep();
        var value = new JobVariable { ValueKind = ResultValueKind.Integer, Value = JsonValue.Create(5) };
        step.Inputs[new TimeoutStepDefinition().Descriptor.Fields.Single().Id] = Reference(value);
        var job = new Job { Steps = [step], Variables = [value] };
        Assert.True(JobValidation.IsStepAllowed(job, step));
        Assert.True(JobValidation.CanConfirm([], step, job.Variables));
        value.Value = JsonValue.Create("broken");
        Assert.False(JobValidation.IsStepAllowed(job, step));
        Assert.False(JobValidation.CanConfirm([], step, job.Variables));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReopenedEndJobRespectsPersistedSkipEndStepsInBothPhases(bool startPhase)
    {
        var end = new EndJobStep { Settings = new() { SkipEndSteps = true } };
        var job = new Job { Name = "persisted stop", EndSteps = [new ShowTextStep { Settings = new() { Text = "cleanup" } }] };
        if (startPhase) job.StartSteps = [end]; else job.Steps = [end];
        JobVariableInputMigration.Migrate(job);
        var options = new JsonSerializerOptions(); JobJsonSerialization.Configure(options);
        job = JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(job, options), options)!;
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Empty(builder.Overlay.TextCalls);
        Assert.True(Assert.Single(builder.Logs.Completions).Success);
    }

    private static ResultBinding Reference(JobVariable value) => new()
    {
        ProviderId = JobValueSources.ProviderFor(value),
        SourceId = value.Id.ToString("D")
    };
}
