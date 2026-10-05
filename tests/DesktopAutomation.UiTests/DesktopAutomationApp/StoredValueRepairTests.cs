using System.Text.Json;
using System.Text.Json.Nodes;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class StoredValueRepairTests
{
    [Fact]
    public void ConditionRowUsesTheLiveCatalogForNewCompoundVariableMembers()
    {
        var catalog = new ValueReferenceSourceCatalog([]);
        var rows = new System.Collections.ObjectModel.ObservableCollection<ConditionRowViewModel>();
        var row = new ConditionRowViewModel(rows, [], sourceCatalog: catalog);
        rows.Add(row);
        var variable = new JobVariable
        {
            Name = "Flags",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonNode.Parse("{\"ready\":true}")
        };
        catalog.AddVariable(variable);
        row.LoadFrom(new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D"),
            ValuePath = "ready",
            Operator = ConditionOperator.IsTrue
        });
        Assert.Equal(ResultValueKind.Boolean, row.SelectedProperty!.DataType);
        Assert.True(row.IsValid);
        Assert.Equal("ready", row.ToCondition().ValuePath);
    }

    [Fact]
    public void TwiceNestedConditionReopensStoredChoiceAndSharedVariableValues()
    {
        var choice = new UserChoiceStepDefinition().CreateDefaultStep();
        var flag = new JobVariable
        {
            Name = "Ready",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Boolean,
            Value = JsonValue.Create(true)
        };
        var outer = new IfStep
        {
            Settings = new()
            {
                Conditions = [new() {
            ProviderId = ValueProviderIds.JobVariable, SourceId = flag.Id.ToString("D"), Operator = ConditionOperator.IsTrue }]
            }
        };
        var inner = new IfStep
        {
            Settings = new()
            {
                Conditions = [new() {
            SourceStepId = choice.Id, PropertyPath = "SelectedOptionId", Operator = ConditionOperator.Equals,
            ComparisonValue = choice.Settings.Options[0].Id }, new() {
            ProviderId = ValueProviderIds.JobVariable, SourceId = flag.Id.ToString("D"), Operator = ConditionOperator.IsTrue }]
            }
        };
        var job = new Job
        {
            Name = "nested values",
            Variables = [flag],
            Steps = [choice, outer, inner, new TimeoutStep(), new EndIfStep(), new EndIfStep()]
        };
        JobVariableInputMigration.Migrate(job);
        var options = new JsonSerializerOptions();
        JobJsonSerialization.Configure(options);
        job = JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(job, options), options)!;
        var persisted = JsonSerializer.Serialize(job, options);
        using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(job);
        vm.SelectedStep = vm.Steps[2];
        var editor = vm.SelectedGeneratedEditor!;
        var conditions = editor.Fields.Single(field => field.UsesConditionEditor).ConditionEditor!;
        Assert.True(conditions.IsValid);
        Assert.Equal(choice.Settings.Options[0].Id, conditions.Conditions[0].ComparisonField!.SelectedEnumValue);
        Assert.Equal(flag.Id.ToString("D"), conditions.Conditions[1].ToCondition().SourceId);
        Assert.True(editor.TryCreateWorkingStep(out _), editor.ValidationError + "\n" + JsonSerializer.Serialize(conditions.ToValue()));
        vm.SelectedStep = vm.Steps[0];
        vm.SelectedStep = vm.Steps[2];
        Assert.True(vm.SelectedGeneratedEditor!.TryCreateWorkingStep(out _), vm.InlineEditorValidationError);
        Assert.Equal(persisted, JsonSerializer.Serialize(job, options));
    }

    [Fact]
    public void MalformedCollectionsStayIntactUntilContentIsEdited()
    {
        var options = new GeneratedUserChoiceOptionsEditorViewModel(JsonNode.Parse("[42,99]"));
        var points = new GeneratedPointEntryListEditorViewModel(JsonNode.Parse("[null]"), []);
        var axes = new GeneratedAxisExpressionListEditorViewModel(JsonNode.Parse("[42]"));
        Assert.Equal("[42,99]", options.ToNode()!.ToJsonString());
        Assert.Equal("[null]", points.ToNode()!.ToJsonString());
        Assert.Equal("[42]", axes.ToNode()!.ToJsonString());
        options.Options[0].Label = "A";
        Assert.NotEqual("[42,99]", options.ToNode()!.ToJsonString());
    }

    [Fact]
    public void ExplicitlyEnteringTheDisplayedDefaultRepairsAnInvalidValue()
    {
        var variable = new JobVariable { ValueKind = ResultValueKind.Integer, Value = JsonValue.Create("broken") };
        var editor = new JobVariableEditorViewModel(variable, _ => { });
        editor.IntegerValue = 0;
        Assert.False(editor.HasInvalidValue);
        Assert.Equal(0, variable.Value!.GetValue<int>());
    }

    [Fact]
    public void TextOverlayPreservesUnknownFontColorUntilAnExplicitReplacement()
    {
        var row = new TextOverlayRowViewModel([], [], StepInputContractRegistry.Get(typeof(VideoCreationStep), "text")!, null,
            settings: new TextResultOverlaySettings { FontColor = "not-a-color" });
        Assert.Equal("not-a-color", row.ToSettings().FontColor);
        row.FontColor = System.Windows.Media.Colors.White;
        Assert.True(ColorValueRules.TryParse(row.ToSettings().FontColor, out _));
    }

    [Fact]
    public void SourcePickerKeepsEqualIdsFromDifferentProvidersIndependent()
    {
        var variable = new JobVariable { ValueKind = ResultValueKind.Integer, Value = JsonValue.Create(3) };
        var local = new LocalValue { Id = variable.Id, ValueKind = variable.ValueKind, Value = JsonValue.Create(8) };
        var sources = new ValueReferenceSourceCatalog([], [variable, local]);
        var contract = StepInputContractRegistry.ForField(new TimeoutStepDefinition().Descriptor.Fields.Single());
        var picker = new ValueReferencePickerViewModel(sources, contract, selectDefault: false);
        picker.Load(new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = variable.Id.ToString() });
        Assert.Same(variable, picker.SelectedJobVariable);
        picker.Load(new ResultBinding { ProviderId = ValueProviderIds.LocalValue, SourceId = local.Id.ToString() });
        Assert.Same(local, picker.SelectedJobVariable);
        sources.AddVariable(variable);
        picker.Load(new ResultBinding { ProviderId = ValueProviderIds.LocalValue, SourceId = local.Id.ToString() });
        Assert.Same(local, picker.SelectedJobVariable);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("LessThan ")]
    [InlineData("future-operator")]
    public void AxisEditorPreservesUnknownOperatorUntilTheUserChoosesAReplacement(string token)
    {
        var editor = new GeneratedAxisExpressionListEditorViewModel(JsonSerializer.SerializeToNode(
            new[] { new StepAxisExpressionValue("X", token, 7) }));
        var expression = Assert.Single(editor.Expressions);
        Assert.Null(expression.Operator);
        Assert.True(expression.HasInvalidToken);
        Assert.Equal(token, editor.ToNode()!.Deserialize<StepAxisExpressionValue[]>()![0].Operator);
        expression.Operator = PointAxisOperator.GreaterThan;
        Assert.False(expression.HasInvalidToken);
        Assert.Equal("GreaterThan", editor.ToNode()!.Deserialize<StepAxisExpressionValue[]>()![0].Operator);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("Manual ")]
    [InlineData("future-source")]
    public void PointEditorPreservesUnknownSourceUntilExplicitSelection(string token)
    {
        var editor = new GeneratedPointEntryListEditorViewModel(JsonSerializer.SerializeToNode(
            new[] { new StepPointEntryValue(token, 3, 4, null) }), []);
        var point = Assert.Single(editor.Points);
        Assert.Null(point.SelectedSourceOption);
        Assert.True(point.HasInvalidToken);
        Assert.Equal(token, editor.ToNode()!.Deserialize<StepPointEntryValue[]>()![0].Source);
        point.SelectedSourceOption = point.SourceOptions.Single(option => option.Value == "Manual");
        Assert.False(point.HasInvalidToken);
        Assert.Equal("Manual", editor.ToNode()!.Deserialize<StepPointEntryValue[]>()![0].Source);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("Automatic ")]
    [InlineData("future-quality")]
    public async Task CameraEditorPreservesUnknownQualityEvenWhenTheCameraIsUnavailable(string token)
    {
        var selection = new StepCameraSelectionValue("missing-camera", "saved name", token, 800, 600, 25, "RGB");
        var editor = new GeneratedCameraEditorViewModel(JsonSerializer.SerializeToNode(selection), new NoOpCameraCaptureService());
        await editor.Initialization;
        Assert.Null(editor.SelectedQuality);
        Assert.True(editor.HasInvalidQualityToken);
        Assert.Equal(selection, editor.ToValue());
    }

    [Theory]
    [InlineData(ResultValueKind.Integer, "\"broken\"")]
    [InlineData(ResultValueKind.Boolean, "42")]
    [InlineData(ResultValueKind.Color, "\"not-a-color\"")]
    [InlineData(ResultValueKind.Point, "[1,2]")]
    [InlineData(ResultValueKind.Rectangle, "{\"x\":\"bad\"}")]
    public void OpeningAnInvalidVariableDoesNotModifyItAndResetRequiresAnExplicitAction(ResultValueKind kind, string json)
    {
        var value = new JobVariable { ValueKind = kind, Value = JsonNode.Parse(json) };
        var changes = 0;
        var editor = new JobVariableEditorViewModel(value, _ => changes++);
        Assert.Equal(json, value.Value!.ToJsonString());
        Assert.True(editor.HasInvalidValue);
        Assert.Equal(0, changes);
        editor.ResetInvalidValueCommand.Execute(null);
        Assert.False(editor.HasInvalidValue);
        Assert.True(changes > 0);
    }

    [Fact]
    public void ExistingSecretSelectionIsPreservedAndMaskedWithoutEnablingNewSecretSources()
    {
        var id = Guid.NewGuid().ToString("D");
        var source = new ValueProviderSourceDescriptor(ValueProviderIds.Secret, id, "Token", "hidden secret description",
            ResultValueKind.Text, ResultCardinality.Single, IsSensitive: true);
        var picker = new ValueReferencePickerViewModel([], StepInputContractRegistry.Get(typeof(ShowTextStep), "text")!,
            false, providerSources: [source]);
        picker.Load(new ResultBinding { ProviderId = ValueProviderIds.Secret, SourceId = id });
        Assert.True(picker.IsConfigured);
        Assert.True(picker.IsSecretSource);
        Assert.False(picker.CanUseSecrets);
        Assert.Equal(id, picker.ToBinding().SourceId);
        Assert.Equal("••••••••", picker.SelectedPreviewValue);
    }

    [Fact]
    public void CollectionVariableUsesJsonEditorAndExplicitResetKeepsCollectionShape()
    {
        var variable = new JobVariable
        {
            ValueKind = ResultValueKind.Integer,
            Cardinality = ResultCardinality.Collection,
            Value = JsonNode.Parse("[1,\"bad\"]")
        };
        var editor = new JobVariableEditorViewModel(variable, _ => { });
        Assert.True(editor.IsResultObject);
        Assert.False(editor.IsInteger);
        Assert.Equal("[1,\"bad\"]", variable.Value!.ToJsonString());
        editor.ResetInvalidValueCommand.Execute(null);
        Assert.IsType<JsonArray>(variable.Value);
        Assert.False(editor.HasInvalidValue);
    }
}
