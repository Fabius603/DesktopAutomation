using System.Globalization;
using System.Resources;
using System.Text.Json;
using System.Text.Json.Nodes;
using DesktopAutomationApp.Converters;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services.Jobs;
using DesktopAutomationApp.ViewModels;
using OpenCvSharp;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class StepDefinitionCatalogTests
{
    [Fact]
    public void RoiPicker_OffersLaterFeedbackProducerWithoutOfferingOtherForwardResults()
    {
        var capture = new DesktopDuplicationStep();
        var consumer = new OcrStep();
        var feedback = new DynamicRoiStep();
        var laterCapture = new DesktopDuplicationStep();
        var viewModel = new AddJobStepDialogViewModel(new ControllableJobExecutor([]), [capture],
            allJobSteps: [capture, consumer, feedback, laterCapture], cameraCaptureService: new CameraDefinitionTestService());
        Assert.True(viewModel.TryLoadGeneratedStep(consumer));
        var editor = viewModel.GeneratedEditor!;
        var roi = Assert.Single(editor.Fields, field => field.Descriptor.Id == "roi").RoiEditor!;
        roi.DetectionDynamicRoiSource.Load(ResultBinding.ForStepResult(feedback.Id, "global_bounds"));
        Assert.False(roi.DetectionDynamicRoiSource.HasMissingReference);
        var image = Assert.Single(editor.Fields, field => field.Descriptor.Id == "image_source").InputReferenceEditor!.Picker;
        image.Load(ResultBinding.ForStepResult(laterCapture.Id, "image"));
        Assert.True(image.HasMissingReference);
    }

    [Fact]
    public void FeedbackSourceCatalog_SharesVariableUpdatesWithRegularPickers()
    {
        var parent = new ValueReferenceSourceCatalog([]);
        var roi = new DynamicRoiStep();
        var child = parent.WithAdditionalSources([new SourceStepItem(roi.Id, "ROI", StepResultMetadata.GetResultTypeForStep(roi)!)]);
        var variable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Rectangle,
            Value = JsonSerializer.SerializeToNode(new TaskAutomation.Contracts.Geometry.PixelRegion(0, 0, 10, 10))
        };
        parent.AddVariable(variable);
        var picker = new ValueReferencePickerViewModel(child, StepInputContractRegistry.Get(typeof(OcrStep), "dynamicRoi")!, false);
        picker.Load(new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = variable.Id.ToString("D") });
        Assert.False(picker.HasMissingReference);
        Assert.Same(variable, picker.SelectedJobVariable);
    }

    [Fact]
    public void MovementThreshold_IsAdvancedDefaultsToTenAndSurvivesEditing()
    {
        var definition = new KlickOnPoint3DStepDefinition();
        var field = Assert.Single(definition.Descriptor.Fields,
            field => field.Id == KlickOnPoint3DStepDefinition.MovementThresholdFieldId);
        Assert.True(field.Advanced);
        Assert.Equal(10, field.DefaultValue!.GetValue<int>());
        var draft = definition.CreateDraft();
        Assert.Equal(10, draft.Values[field.Id]!.GetValue<int>());
        draft.Values[field.Id] = JsonValue.Create(17);
        var step = Assert.IsType<KlickOnPoint3DStep>(definition.ApplyDraft(draft));
        Assert.Equal(17, step.Settings.MovementThresholdPixels);
        Assert.Equal(17, definition.CreateDraft(step).Values[field.Id]!.GetValue<int>());
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    public void MovementThreshold_RejectsInvalidPixelValues(string json)
    {
        var definition = new KlickOnPoint3DStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[KlickOnPoint3DStepDefinition.MovementThresholdFieldId] = JsonNode.Parse(json);
        Assert.Contains(definition.ValidateDraft(draft), issue => issue.FieldId == KlickOnPoint3DStepDefinition.MovementThresholdFieldId);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("999")]
    [InlineData("Automatic ")]
    public void CameraQuality_RejectsNonTokenValues(string token)
    {
        var definition = new CameraCaptureStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[CameraCaptureStepDefinition.CameraFieldId] = JsonSerializer.SerializeToNode(
            new StepCameraSelectionValue("camera", "Camera", token, 640, 480, 30, "RGB"));
        Assert.Contains(definition.ValidateDraft(draft), issue => issue.Code == "StepValidation.Invalid");
        Assert.Throws<InvalidOperationException>(() => definition.ApplyDraft(draft));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("999")]
    [InlineData("LessThan ")]
    public void PointExpression_RejectsNonTokenOperators(string token)
    {
        var definition = new PointComparisonStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[PointComparisonStepDefinition.ModeFieldId] = JsonValue.Create("Expression");
        draft.Values[PointComparisonStepDefinition.ExpressionsFieldId] = JsonSerializer.SerializeToNode(
            new[] { new StepAxisExpressionValue("X", token, 0) });
        Assert.Contains(definition.ValidateDraft(draft), issue =>
            issue.FieldId == PointComparisonStepDefinition.ExpressionsFieldId);
        Assert.Throws<InvalidOperationException>(() => definition.ApplyDraft(draft));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("\" \"")]
    public void OptionalEnum_RejectsNonStringAndWhitespaceValues(string json)
    {
        var field = new StepFieldDescriptor("mode", "label", StepValueKind.Enum,
            Options: [new("known", "label")]);
        var descriptor = new StepDescriptor("test", "test", "test", "test", null, [field], new([], [], []));
        var draft = new StepDraft("test");
        draft.Values[field.Id] = JsonNode.Parse(json);
        Assert.NotEmpty(StepDescriptorDraftValidator.Validate(descriptor, draft, StepValidationContext.FullyResolved()));
    }

    [Fact]
    public void NumberFields_AcceptIntegerJsonAndRejectOverflowWithoutThrowing()
    {
        var field = new StepFieldDescriptor("number", "label", StepValueKind.Number);
        var descriptor = new StepDescriptor("test", "test", "test", "test", null, [field], new([], [], []));
        var draft = new StepDraft("test");
        draft.Values[field.Id] = JsonValue.Create(12);
        Assert.Empty(StepDescriptorDraftValidator.Validate(descriptor, draft, StepValidationContext.FullyResolved()));
        Assert.Equal(12, DefinitionValueReader.Number(draft, field.Id));
        draft.Values[field.Id] = JsonValue.Create(double.MaxValue);
        Assert.NotEmpty(StepDescriptorDraftValidator.Validate(descriptor, draft, StepValidationContext.FullyResolved()));
    }

    [Fact]
    public void OptionalEnumField_AllowsAnEmptyValue()
    {
        var field = new StepFieldDescriptor(
            "mode",
            "Ui.Common.Value",
            StepValueKind.Enum,
            Required: false,
            Options: [new StepFieldOptionDescriptor("known", "Ui.Common.Value")]);
        var descriptor = new StepDescriptor(
            "test", "test", "test", "test", null,
            [field],
            new StepPresentationDescriptor([], [], []));
        var draft = new StepDraft(descriptor.TypeId);
        draft.Values[field.Id] = JsonValue.Create(string.Empty);

        var issues = StepDescriptorDraftValidator.Validate(
            descriptor, draft, StepValidationContext.FullyResolved());

        Assert.Empty(issues);
    }

    [Fact]
    public void Catalog_RejectsEnumOptionsWithoutLabelKeys()
    {
        var field = new StepFieldDescriptor(
            "mode",
            "Ui.Common.Value",
            StepValueKind.Enum,
            Required: true,
            DefaultValue: JsonValue.Create("known"),
            Options: [new StepFieldOptionDescriptor("known", string.Empty)]);
        var descriptor = new StepDescriptor(
            "test", "test", "test", "test", null,
            [field],
            new StepPresentationDescriptor([], [], []));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new StepDefinitionCatalog(
                [new DescriptorOverrideDefinition(new TimeoutStepDefinition(), descriptor)]));

        Assert.Contains("label keys", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltInEnumOptions_HaveGermanAndEnglishLabels()
    {
        var resources = new ResourceManager(
            "DesktopAutomationApp.Resources.Strings",
            typeof(LocalizationService).Assembly);
        var options = BuiltInStepDefinitions.Instance.Definitions
            .SelectMany(definition => definition.Descriptor.Fields)
            .Where(field => field.ValueKind == StepValueKind.Enum)
            .SelectMany(field => field.Options ?? [])
            .ToArray();

        Assert.NotEmpty(options);
        Assert.All(options, option =>
        {
            Assert.False(string.IsNullOrWhiteSpace(resources.GetString(
                option.LabelKey, CultureInfo.GetCultureInfo("de-DE"))), option.LabelKey);
            Assert.False(string.IsNullOrWhiteSpace(resources.GetString(
                option.LabelKey, CultureInfo.GetCultureInfo("en-US"))), option.LabelKey);
        });
    }

    [Fact]
    public void ConditionDisplay_FormatsLocalTimestampComparisonAsLiteral()
    {
        var timestamp = new LocalValue
        {
            Name = "Comparison timestamp",
            ValueKind = ResultValueKind.DateTime,
            Value = JsonValue.Create(new DateTime(2026, 8, 24, 14, 35, 12))
        };
        var source = new FileSystemOperationStep { Id = "source" };
        var condition = new StepCondition
        {
            SourceStepId = source.Id,
            PropertyPath = "CompletedAtUtc",
            Operator = ConditionOperator.GreaterThan,
            Comparison = new ComparisonOperand
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.LocalValue,
                SourceId = timestamp.Id.ToString("D")
            }
        };

        var text = ConditionDisplayFormatter.Format(condition, new JobStep[] { source }, [timestamp]);

        Assert.Contains(Loc.Get("Ui.Step.IfEditor.LiteralValue"), text);
        Assert.DoesNotContain(Loc.Get("Step.Unknown"), text);
        Assert.DoesNotContain(timestamp.Name, text);
    }

    [Fact]
    public void ConditionDisplay_UsesUserChoiceLabelInsteadOfTheInternalOptionId()
    {
        var source = new UserChoiceStep
        {
            Id = "choice",
            Settings = new UserChoiceSettings
            {
                Options =
                [
                    new UserChoiceOption { Id = "mode-prod", Label = "Production", Value = "prod" },
                    new UserChoiceOption { Id = "mode-test", Label = "Test", Value = "test" }
                ]
            }
        };
        var comparison = new LocalValue
        {
            Name = "Selected choice",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("mode-prod")
        };
        var condition = new StepCondition
        {
            SourceStepId = source.Id,
            PropertyId = "selected_option_id",
            PropertyPath = nameof(UserChoiceResult.SelectedOptionId),
            Operator = ConditionOperator.Equals,
            Comparison = new ComparisonOperand
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.LocalValue,
                SourceId = comparison.Id.ToString("D")
            }
        };

        var text = ConditionDisplayFormatter.Format(
            condition, new JobStep[] { source }, [comparison]);
        var ifStep = new IfStep
        {
            Settings = new IfConditionSettings { Conditions = [condition] }
        };
        var details = new JobStepDetailsProvider().GetDetails(
            ifStep, new JobStep[] { source, ifStep }, [comparison]);
        var detail = Assert.Single(details.Groups.SelectMany(group => group.Items),
            item => item.Name.Contains("1.", StringComparison.Ordinal));

        Assert.Contains("Production", text);
        Assert.DoesNotContain("mode-prod", text);
        Assert.Contains("Production", detail.Value);
        Assert.DoesNotContain("mode-prod", detail.Value);
    }

    [Fact]
    public void FileSystemEditor_UsesSingleDirectoryFieldsWithoutSourceModeDropdowns()
    {
        var definition = new FileSystemOperationStepDefinition();
        var fields = definition.Descriptor.Fields;
        var section = definition.Descriptor.Presentation.EditorSections.Single(candidate => candidate.Id == "general");

        Assert.DoesNotContain(fields, field => field.Id is FileSystemOperationStepDefinition.SourceModeFieldId
            or FileSystemOperationStepDefinition.SourceResultFieldId
            or FileSystemOperationStepDefinition.TargetModeFieldId
            or FileSystemOperationStepDefinition.TargetResultFieldId);
        Assert.Empty(section.EditorNodes!.OfType<StepChoiceGroupDescriptor>());
        Assert.All(fields.Where(field => field.Id is FileSystemOperationStepDefinition.SourcePathFieldId
                or FileSystemOperationStepDefinition.TargetPathFieldId), field =>
            Assert.Equal(StepEditorHints.DirectoryPicker, field.EditorHint));
    }

    [Fact]
    public void ChoiceGroupContract_SupportsArbitraryBranchCounts()
    {
        var group = new StepChoiceGroupDescriptor("mode",
        [
            new("first", "First", [new StepFieldNodeDescriptor("a")]),
            new("second", "Second", [new StepFieldNodeDescriptor("b")]),
            new("third", "Third", [new StepFieldNodeDescriptor("c")])
        ]);

        Assert.Equal(3, group.Branches.Count);
    }

    [Fact]
    public void ShowTextEditor_UsesOneUnifiedTextFieldWithoutSourceDropdown()
    {
        var definition = new ShowTextStepDefinition();
        var fields = definition.Descriptor.Fields;
        var general = definition.Descriptor.Presentation.EditorSections.Single(section => section.Id == "general");

        Assert.DoesNotContain(fields, field => field.Id is ShowTextStepDefinition.TextSourceFieldId
            or ShowTextStepDefinition.TextFieldId);
        Assert.Contains(fields, field => field.Id == ShowTextStepDefinition.TextResultFieldId);
        Assert.Empty(general.EditorNodes?.OfType<StepChoiceGroupDescriptor>() ?? []);
    }

    [Fact]
    public void Catalog_RejectsInvalidChoiceGroupStructure()
    {
        var pointComparison = new PointComparisonStepDefinition();
        var general = pointComparison.Descriptor.Presentation.EditorSections[0];
        var invalidGroup = new StepChoiceGroupDescriptor(PointComparisonStepDefinition.ReferenceSourceFieldId,
        [
            new("Manual", "Ui.Step.IfEditor.LiteralValue", [new StepFieldNodeDescriptor(PointComparisonStepDefinition.ReferenceXFieldId)]),
            new("Manual", "Ui.Step.IfEditor.JobResultValue", [new StepFieldNodeDescriptor("missing")])
        ]);
        var invalid = new DescriptorOverrideDefinition(pointComparison, pointComparison.Descriptor with
        {
            TypeId = "point_comparison_invalid_choice_group",
            Presentation = pointComparison.Descriptor.Presentation with
            {
                EditorSections = [general with { EditorNodes = [invalidGroup] }]
            }
        });

        Assert.Contains("choice group", Assert.Throws<InvalidOperationException>(() =>
            new StepDefinitionCatalog([invalid])).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PointComparison_UsesTheReferenceSourceInsideThePointEditor()
    {
        var definition = new PointComparisonStepDefinition();
        var field = definition.Descriptor.Fields.Single(candidate =>
            candidate.Id == PointComparisonStepDefinition.ReferenceSourceFieldId);
        var offset = definition.Descriptor.Presentation.EditorSections.Single(section => section.Id == "offset");

        Assert.Null(field.EditorHint);
        Assert.Equal(2, field.Options?.Count);
        var point = Assert.IsType<StepPointFieldPairDescriptor>(offset.EditorNodes![0]);
        Assert.Equal(PointComparisonStepDefinition.ReferenceSourceFieldId, point.SourceFieldId);
        Assert.Equal(PointComparisonStepDefinition.ReferencePointsFieldId, point.ReferenceFieldId);
    }

    [Fact]
    public void ShowTextField_IsARequiredDirectCapableValueReference()
    {
        var field = Assert.Single(new ShowTextStepDefinition().Descriptor.Fields,
            candidate => candidate.Id == ShowTextStepDefinition.TextResultFieldId);
        var contract = StepInputContractRegistry.Resolve(typeof(ShowTextStep), field);

        Assert.True(field.Required);
        Assert.True(field.AllowsDirectValue);
        Assert.True(contract.AllowsDirectValue);
        Assert.True(contract.AllowsProvider(ValueProviderIds.JobVariable));
        Assert.True(contract.AllowsProvider(ValueProviderIds.StepResult));
        Assert.False(contract.AllowsProvider(ValueProviderIds.Secret));
    }

    [Fact]
    public void PointEntry_CanSwitchBackFromResultToManualInput()
    {
        var owner = new System.Collections.ObjectModel.ObservableCollection<PointEntryViewModel>();
        var point = new PointEntryViewModel(owner, []);

        point.IsJobResult = true;
        point.IsJobResult = false;

        Assert.True(point.IsManual);
        Assert.False(point.IsJobResult);
    }

    [Fact]
    public void PointComparison_ShowsOneWholePointSourceOrTheReferenceCoordinates()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService());
        viewModel.SelectedType = "PointComparison";
        var nodes = viewModel.GeneratedEditor!.Sections.Single(section => section.Descriptor.Id == "offset").Nodes;

        var manualPoint = Assert.IsType<GeneratedStepPointFieldPairViewModel>(
            nodes[0]);
        Assert.Equal(PointComparisonStepDefinition.ReferenceXFieldId, manualPoint.XField.Descriptor.Id);
        Assert.Equal(PointComparisonStepDefinition.ReferenceYFieldId, manualPoint.YField.Descriptor.Id);
        Assert.True(manualPoint.HasLabel);
        Assert.NotEmpty(manualPoint.Label);
        Assert.NotNull(manualPoint.WholeValueSource);
        Assert.True(manualPoint.WholeValueSource!.ShowsIndividualValues);

        manualPoint.WholeValueSource.UseStepResultCommand.Execute(null);
        Assert.False(manualPoint.WholeValueSource.ShowsIndividualValues);
        Assert.True(manualPoint.WholeValueSource.UsesStepResult);
        Assert.False(manualPoint.WholeValueSource.UsesJobVariable);
        Assert.False(manualPoint.WholeValueSource.UsesIndividualValues);
        manualPoint.WholeValueSource.UseIndividualValuesCommand.Execute(null);
        Assert.True(manualPoint.WholeValueSource.ShowsIndividualValues);
        Assert.False(manualPoint.WholeValueSource.UsesStepResult);
        Assert.False(manualPoint.WholeValueSource.UsesJobVariable);
        manualPoint.WholeValueSource.UseJobVariableCommand.Execute(null);
        Assert.True(manualPoint.WholeValueSource.UsesJobVariable);
        Assert.False(manualPoint.WholeValueSource.UsesStepResult);
        Assert.False(manualPoint.WholeValueSource.UsesIndividualValues);
    }

    [Fact]
    public void PointComparison_CanBeCreatedWithoutOptionalReferenceSource()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService());
        viewModel.SelectedType = "PointComparison";

        Assert.True(viewModel.GeneratedEditor!.TryCreateStep(out var created),
            viewModel.GeneratedEditor.ValidationError);
        Assert.IsType<PointComparisonStep>(created);
    }

    [Fact]
    public void PointComparison_CannotBeCreatedWhileVisibleVariablePickerHasNoSelection()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService());
        viewModel.SelectedType = "PointComparison";
        var point = Assert.IsType<GeneratedStepPointFieldPairViewModel>(
            viewModel.GeneratedEditor!.Sections.Single(section => section.Descriptor.Id == "offset").Nodes[0]);

        point.WholeValueSource!.UseJobVariableCommand.Execute(null);

        Assert.False(viewModel.GeneratedEditor.TryCreateStep(out var created));
        Assert.Null(created);
        Assert.NotEmpty(viewModel.GeneratedEditor.ValidationError);

        point.WholeValueSource.UseIndividualValuesCommand.Execute(null);

        Assert.True(viewModel.GeneratedEditor.TryCreateStep(out created),
            viewModel.GeneratedEditor.ValidationError);
    }

    [Fact]
    public void ShowTextDefinition_RequiresItsUnifiedTextBinding()
    {
        var definition = new ShowTextStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[ShowTextStepDefinition.TextResultFieldId] = null;

        Assert.Contains(definition.ValidateDraft(draft), issue =>
            issue.FieldId == ShowTextStepDefinition.TextResultFieldId
            && issue.Code == "StepValidation.Required");
    }

    [Fact]
    public void PointComparison_ReferenceCoordinatesHideOutsideOffsetMode()
    {
        var editor = new GeneratedStepEditorViewModel(new PointComparisonStepDefinition());
        var mode = editor.Fields.Single(field => field.Descriptor.Id == PointComparisonStepDefinition.ModeFieldId);
        var point = Assert.IsType<GeneratedStepPointFieldPairViewModel>(
            editor.Sections.Single(section => section.Descriptor.Id == "offset").Nodes[0]);

        Assert.True(point.XField.IsVisible);
        Assert.True(point.YField.IsVisible);

        mode.SelectedEnumOption = mode.EnumOptions.Single(option => option.Value == "Expression");

        Assert.False(point.XField.IsVisible);
        Assert.False(point.YField.IsVisible);
    }

    [Fact]
    public void AddStepDialog_InitializesEditorForDefaultStepType()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [],
            cameraCaptureService: new CameraDefinitionTestService());

        Assert.Equal("DesktopDuplication", viewModel.SelectedType);
        Assert.NotNull(viewModel.GeneratedEditor);
        Assert.Equal(
            new DesktopDuplicationStepDefinition().Descriptor.Fields.Select(field => field.Id),
            viewModel.GeneratedEditor.Fields.Select(field => field.Descriptor.Id));
    }

    [Fact]
    public void AddStepPicker_ConfirmsDefaultStepWithoutRequiringValidEditorInput()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [],
            cameraCaptureService: new CameraDefinitionTestService())
        {
            IsPickerOnly = true,
            SelectedType = "ScriptExecution"
        };
        bool? closeResult = null;
        viewModel.RequestClose += result => closeResult = result;

        viewModel.ConfirmCommand.Execute(null);

        Assert.True(closeResult);
        Assert.IsType<ScriptExecutionStep>(viewModel.CreatedStep);
        Assert.False(JobValidation.ValidateCandidate([], viewModel.CreatedStep).IsValid);
    }

    [Fact]
    public void AddStepDialog_CreatesJobAndMacroExecutionStepsFromDirectPickers()
    {
        var job = new Job { Id = Guid.NewGuid(), Name = "Child job" };
        var macro = new Makro { Id = Guid.NewGuid(), Name = "Cleanup" };
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([job], [macro]),
            [],
            cameraCaptureService: new CameraDefinitionTestService());

        viewModel.SelectedType = "JobExecution";
        var jobField = viewModel.GeneratedEditor!.Fields.Single(field =>
            field.Descriptor.Id == JobExecutionStepDefinition.JobFieldId);
        Assert.True(jobField.InputReferenceEditor!.Picker.IsStepValue);
        Assert.Equal(job.Id.ToString("D"), jobField.SelectedChoice!.Value.Id);
        viewModel.CreateStep();
        var jobStep = Assert.IsType<JobExecutionStep>(viewModel.CreatedStep);
        Assert.Equal(job.Id, jobStep.Settings.JobId);
        Assert.Equal(job.Name, jobStep.Settings.JobName);

        viewModel.SelectedType = "MakroExecution";
        var macroField = Assert.Single(viewModel.GeneratedEditor!.Fields);
        Assert.True(macroField.InputReferenceEditor!.Picker.IsStepValue);
        Assert.Equal(macro.Id.ToString("D"), macroField.SelectedChoice!.Value.Id);
        viewModel.CreateStep();
        var macroStep = Assert.IsType<MakroExecutionStep>(viewModel.CreatedStep);
        Assert.Equal(macro.Id, macroStep.Settings.MakroId);
        Assert.Equal(macro.Name, macroStep.Settings.MakroName);
    }

    [Fact]
    public void AddStepDialog_CreatesEditableLocalStepValueForLiteralField()
    {
        var createdVariables = new List<JobVariable>();
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [],
            cameraCaptureService: new CameraDefinitionTestService(),
            jobVariableCreated: createdVariables.Add);
        viewModel.SelectedType = "Timeout";
        var field = Assert.Single(viewModel.GeneratedEditor!.Fields);

        Assert.True(field.IsInlineStepValue);
        Assert.True(field.ShowsDirectInput);
        Assert.False(field.ShowsInputSourcePicker);
        Assert.True(field.ShowsInputSourceSelector);
        field.IntegerValue = 2500;
        viewModel.ConfirmCommand.Execute(null);

        var variable = Assert.Single(createdVariables);
        Assert.Equal(JobVariableScope.StepValue, variable.Scope);
        Assert.Equal(2500, variable.Value!.GetValue<int>());
        var step = Assert.IsType<TimeoutStep>(viewModel.CreatedStep);
        var binding = Assert.Contains(TimeoutStepDefinition.DelayFieldId, step.Inputs);
        Assert.Equal(variable.Id.ToString("D"), binding.SourceId);
    }

    [Fact]
    public void AddStepDialog_DoesNotCommitReplacedAutomaticStepValue()
    {
        var shared = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Shared delay",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(500)
        };
        var createdVariables = new List<JobVariable>();
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [],
            cameraCaptureService: new CameraDefinitionTestService(),
            jobVariables: [shared],
            jobVariableCreated: createdVariables.Add);
        viewModel.SelectedType = "Timeout";
        var picker = Assert.Single(viewModel.GeneratedEditor!.Fields).InputReferenceEditor!.Picker;
        picker.UseJobVariableCommand.Execute(null);
        var sharedNode = picker.SelectionTree.Single(node => node.DisplayName == shared.Name);

        var field = Assert.Single(viewModel.GeneratedEditor.Fields);
        field.UseVariableCommand.Execute(null);
        Assert.True(field.ShowsInputSourcePicker);
        Assert.False(field.ShowsDirectInput);
        sharedNode.SelectCommand!.Execute(null);
        Assert.True(field.UsesExternalInputReference);
        viewModel.ConfirmCommand.Execute(null);

        Assert.Empty(createdVariables);
        var step = Assert.IsType<TimeoutStep>(viewModel.CreatedStep);
        Assert.Equal(shared.Id.ToString("D"), step.Inputs[TimeoutStepDefinition.DelayFieldId].SourceId);
    }

    [Fact]
    public void AddStepDialog_CanReturnFromVariableToIndependentDirectValue()
    {
        var shared = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Shared delay",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(500)
        };
        var createdVariables = new List<JobVariable>();
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService(),
            jobVariables: [shared], jobVariableCreated: createdVariables.Add);
        viewModel.SelectedType = "Timeout";
        var field = Assert.Single(viewModel.GeneratedEditor!.Fields);
        field.UseVariableCommand.Execute(null);
        var sharedNode = field.InputReferenceEditor!.Picker.SelectionTree
            .Single(node => node.DisplayName == shared.Name);
        sharedNode.SelectCommand!.Execute(null);

        field.UseDirectValueCommand.Execute(null);
        field.IntegerValue = 1750;
        viewModel.ConfirmCommand.Execute(null);

        Assert.True(field.ShowsDirectInput);
        Assert.False(field.UsesExternalInputReference);
        var direct = Assert.Single(createdVariables);
        Assert.Equal(JobVariableScope.StepValue, direct.Scope);
        Assert.Equal(1750, direct.Value!.GetValue<int>());
        Assert.Equal(direct.Id.ToString("D"), Assert.IsType<TimeoutStep>(viewModel.CreatedStep)
            .Inputs[TimeoutStepDefinition.DelayFieldId].SourceId);
    }

    [Fact]
    public void AddStepDialog_ShowsOnlySourcePickerWhenFieldDisallowsDirectValues()
    {
        var original = new TimeoutStepDefinition();
        var descriptor = original.Descriptor with
        {
            Fields = original.Descriptor.Fields
                .Select(field => field with { AllowsDirectValue = false })
                .ToArray()
        };
        var catalog = new StepDefinitionCatalog(
            [new DescriptorOverrideDefinition(original, descriptor)]);
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService(),
            stepDefinitionCatalog: catalog);
        viewModel.SelectedType = "Timeout";

        var field = Assert.Single(viewModel.GeneratedEditor!.Fields);

        Assert.False(field.SupportsDirectValue);
        Assert.False(field.ShowsDirectInput);
        Assert.True(field.ShowsInputSourcePicker);
        Assert.False(field.UseDirectValueCommand.CanExecute(null));
    }

    [Fact]
    public void AddStepDialog_AllowsDirectFilePathValuesByDefault()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService());
        viewModel.SelectedType = "TemplateMatching";
        var field = viewModel.GeneratedEditor!.Fields.Single(candidate =>
            candidate.Descriptor.Id == TemplateMatchingStepDefinition.TemplatePathFieldId);

        Assert.True(field.SupportsDirectValue);
        Assert.True(field.ShowsDirectInput);
        Assert.False(field.ShowsInputSourcePicker);
    }

    [Fact]
    public void AddStepDialog_UsesTheFieldSpecificSourcePolicy()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService());

        viewModel.SelectedType = "CameraCapture";
        var camera = Assert.Single(viewModel.GeneratedEditor!.Fields);
        Assert.True(camera.SupportsDirectValue);
        Assert.True(camera.InputReferenceEditor!.Picker.IsDirectSource);
        Assert.True(camera.InputReferenceEditor.Picker.IsConfigured);
        Assert.False(camera.InputReferenceEditor.Picker.CanUseJobVariables);
        Assert.False(camera.InputReferenceEditor.Picker.CanUseSecrets);
        Assert.False(camera.ShowsInputSourceSelector);

        viewModel.SelectedType = "DynamicRoi";
        var bounds = viewModel.GeneratedEditor!.Fields.Single(field =>
            field.Descriptor.Id == DynamicRoiStepDefinition.BoundsSourceFieldId);
        var padding = viewModel.GeneratedEditor.Fields.Single(field =>
            field.Descriptor.Id == DynamicRoiStepDefinition.PaddingSourceFieldId);
        Assert.False(bounds.InputReferenceEditor!.Picker.CanUseJobVariables);
        Assert.False(bounds.InputReferenceEditor.Picker.CanUseSecrets);
        Assert.True(bounds.InputReferenceEditor.Picker.IsStepResultSource);
        Assert.False(bounds.ShowsInputSourceSelector);
        Assert.True(padding.InputReferenceEditor!.Picker.CanUseJobVariables);
        Assert.True(padding.InputReferenceEditor.Picker.CanUseStepResults);
        Assert.False(padding.InputReferenceEditor.Picker.CanUseSecrets);
        Assert.True(padding.InputReferenceEditor.Picker.IsDirectSource);
        Assert.True(padding.ShowsInputSourceSelector);
        Assert.Equal("0", padding.InputText);
        Assert.Equal(0, padding.IntegerValue);
        Assert.False(padding.UsesValueReferencePicker);
        Assert.True(padding.UsesTextInput);

        viewModel.SelectedType = "PredictMovement";
        var points = viewModel.GeneratedEditor!.Fields.Single(field =>
            field.Descriptor.Id == PredictMovementStepDefinition.PointsSourceFieldId);
        var model = viewModel.GeneratedEditor.Fields.Single(field =>
            field.Descriptor.Id == PredictMovementStepDefinition.PredictionModelFieldId);
        var confidence = viewModel.GeneratedEditor.Fields.Single(field =>
            field.Descriptor.Id == PredictMovementStepDefinition.MinimumConfidenceFieldId);
        Assert.False(points.InputReferenceEditor!.Picker.CanUseJobVariables);
        Assert.False(points.InputReferenceEditor.Picker.CanUseSecrets);
        Assert.True(model.SupportsDirectValue);
        Assert.False(model.InputReferenceEditor!.Picker.CanUseJobVariables);
        Assert.False(model.InputReferenceEditor.Picker.CanUseSecrets);
        Assert.True(model.InputReferenceEditor.Picker.IsDirectSource);
        Assert.True(confidence.InputReferenceEditor!.Picker.CanUseJobVariables);
        Assert.False(confidence.InputReferenceEditor.Picker.CanUseSecrets);
        Assert.True(confidence.InputReferenceEditor.Picker.IsDirectSource);

        foreach (var stepType in new[] { "KlickOnPoint", "KlickOnPoint3D" })
        {
            viewModel.SelectedType = stepType;
            var point = viewModel.GeneratedEditor!.Fields.Single(field =>
                field.Descriptor.Id == (stepType == "KlickOnPoint"
                    ? KlickOnPointStepDefinition.PointsSourceFieldId
                    : KlickOnPoint3DStepDefinition.PointsSourceFieldId));
            Assert.False(point.SupportsDirectValue);
            Assert.False(point.UseDirectValueCommand.CanExecute(null));
            Assert.True(point.InputReferenceEditor!.Picker.IsStepResultSource);
            Assert.True(point.ShowsInputSourcePicker);
            Assert.False(point.ShowsDirectInput);
            Assert.True(point.InputReferenceEditor.Picker.CanUseJobVariables);
            Assert.True(point.InputReferenceEditor.Picker.CanUseStepResults);
            Assert.False(point.InputReferenceEditor.Picker.CanUseSecrets);
        }

        viewModel.SelectedType = "FileSystemOperation";
        foreach (var id in new[]
                 {
                     FileSystemOperationStepDefinition.SourcePathFieldId,
                     FileSystemOperationStepDefinition.TargetPathFieldId
                 })
        {
            var path = viewModel.GeneratedEditor!.Fields.Single(field => field.Descriptor.Id == id);
            Assert.True(path.UsesDirectoryPicker);
            Assert.True(path.InputReferenceEditor!.Picker.IsDirectSource);
            Assert.True(path.InputReferenceEditor.Picker.CanUseJobVariables);
            Assert.True(path.InputReferenceEditor.Picker.CanUseStepResults);
            Assert.False(path.InputReferenceEditor.Picker.CanUseSecrets);
        }

        viewModel.SelectedType = "YOLODetection";
        var yolo = viewModel.GeneratedEditor!.Fields.Single(field => field.YoloEditor is not null).YoloEditor!;
        foreach (var field in new[] { yolo.ModelField!, yolo.ClassField! })
        {
            Assert.True(field.InputReferenceEditor!.Picker.IsDirectSource);
            Assert.False(field.InputReferenceEditor.Picker.CanUseJobVariables);
            Assert.False(field.InputReferenceEditor.Picker.CanUseStepResults);
            Assert.False(field.InputReferenceEditor.Picker.CanUseSecrets);
        }
    }

    [Fact]
    public void ValueReferencePicker_DoesNotUseDisallowedLegacyBindingAsDefaultSource()
    {
        var legacyDirectValue = new JobVariable
        {
            Scope = JobVariableScope.StepValue,
            ValueKind = ResultValueKind.Rectangle,
            Value = new JsonObject { ["x"] = 1, ["y"] = 2, ["width"] = 3, ["height"] = 4 }
        };
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "bounds")!;
        var picker = new ValueReferencePickerViewModel(
            [], contract, false, [legacyDirectValue], context: new ValueReferencePickerContext(
                "Dynamic region", "Detection result", CreateStepValue: () => legacyDirectValue));

        picker.Load(new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = legacyDirectValue.Id.ToString("D")
        });

        Assert.True(picker.HasMissingReference);
        Assert.True(picker.IsStepResultSource);
        Assert.False(picker.UseDirectValueCommand.CanExecute(null));
    }

    [Fact]
    public void AddStepDialog_EveryFieldStartsWithAnEnabledAllowedSourceKind()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService());
        var stepTypes = AddJobStepDialogViewModel.CreateStepTypeItems(BuiltInStepDefinitions.Instance)
            .Cast<AddJobStepDialogViewModel.StepTypeItem>()
            .Select(item => item.Name)
            .ToArray();

        foreach (var stepType in stepTypes)
        {
            viewModel.SelectedType = stepType;
            foreach (var field in viewModel.GeneratedEditor!.Fields)
            {
                var picker = field.InputReferenceEditor!.Picker;
                var hasAllowedSource = picker.CanUseDirectValue
                                       || picker.CanUseJobVariables
                                       || picker.CanUseStepResults
                                       || picker.CanUseSecrets
                                       || picker.CanUseExternalProviders;
                Assert.True(hasAllowedSource,
                    $"{stepType}.{field.Descriptor.Id} has no enabled source kind.");

                var activeSourceIsAllowed = picker.ActiveSourceKind switch
                {
                    StepInputSourceKind.Direct => picker.CanUseDirectValue,
                    StepInputSourceKind.JobVariable => picker.CanUseJobVariables,
                    StepInputSourceKind.StepResult => picker.CanUseStepResults,
                    StepInputSourceKind.Secret => picker.CanUseSecrets,
                    StepInputSourceKind.ExternalProvider => picker.CanUseExternalProviders,
                    _ => false
                };
                Assert.True(activeSourceIsAllowed,
                    $"{stepType}.{field.Descriptor.Id} starts with disabled source {picker.ActiveSourceKind}.");
            }
        }
    }

    [Fact]
    public void AddStepDialog_KeepsSelectionWhenSearchHasNoMatches()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [],
            cameraCaptureService: new CameraDefinitionTestService());
        var originalEditor = viewModel.GeneratedEditor;

        viewModel.StepTypeSearchText = "step-type-that-does-not-exist";
        Assert.Empty(viewModel.StepTypeItems.Cast<AddJobStepDialogViewModel.StepTypeItem>());

        viewModel.SelectedType = null!;

        Assert.Equal("DesktopDuplication", viewModel.SelectedType);
        Assert.Same(originalEditor, viewModel.GeneratedEditor);
    }

    [Fact]
    public void AddStepDialog_ListsEverySelectableStepTypeOnlyOnce()
    {
        var items = AddJobStepDialogViewModel.CreateStepTypeItems(BuiltInStepDefinitions.Instance)
            .Cast<AddJobStepDialogViewModel.StepTypeItem>()
            .ToArray();

        Assert.Equal(items.Length, items.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(items, item => item.Name == "CameraCapture");
        Assert.Single(items, item => item.Name == "FileSystemOperation");
        Assert.Single(items, item => item.Name == "ShowImage");
        Assert.Single(items, item => item.Name == "KlickOnPoint3D");
        Assert.Single(items, item => item.Name == "UserChoice");
        Assert.Single(items, item => item.Name == "PointComparison");
        Assert.DoesNotContain(items, item => item.Name == "ProcessDuplication");
        Assert.DoesNotContain(items, item => item.Name == "ElseIf");
        Assert.DoesNotContain(items, item => item.Name == "Else");
        Assert.DoesNotContain(items, item => item.Name == "EndIf");
        Assert.DoesNotContain(items, item => item.Name == "ShowText");
        Assert.Single(items, item => item.Name == "ShowOnDesktop");
    }

    [Fact]
    public void AddStepDialog_FiltersStepTypesByVisibleText()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [],
            cameraCaptureService: new CameraDefinitionTestService());
        var allItems = viewModel.StepTypeItems
            .Cast<AddJobStepDialogViewModel.StepTypeItem>()
            .ToArray();
        var expected = allItems.Single(item => item.Name == "CameraCapture");

        viewModel.StepTypeSearchText = expected.DisplayLabel;

        var filteredItems = viewModel.StepTypeItems
            .Cast<AddJobStepDialogViewModel.StepTypeItem>()
            .ToArray();
        Assert.Contains(filteredItems, item => item.Name == expected.Name);
        Assert.All(filteredItems, item => Assert.True(
            item.DisplayLabel.Contains(expected.DisplayLabel, StringComparison.CurrentCultureIgnoreCase)
            || item.Category.Contains(expected.DisplayLabel, StringComparison.CurrentCultureIgnoreCase)
            || item.Description.Contains(expected.DisplayLabel, StringComparison.CurrentCultureIgnoreCase)));

        viewModel.StepTypeSearchText = string.Empty;
        Assert.Equal(allItems.Length, viewModel.StepTypeItems.Cast<object>().Count());
    }

    [Fact]
    public void SummaryProvider_RendersDefinitionSummaryItems()
    {
        var provider = new JobStepDetailsProvider();
        var step = new StartProcessStep
        {
            Settings = new StartProcessSettings
            {
                ExecutablePath = @"C:\Tools\worker.exe",
                WaitForExit = true
            }
        };

        var summary = provider.GetSummary(step, new JobStep[] { step });

        Assert.Contains("worker.exe", summary);
        Assert.DoesNotContain(@"C:\Tools", summary);
        Assert.Contains("·", summary);
    }

    [Fact]
    public void SummaryProvider_RendersProcessNameAndWindowTitleFromSharedTarget()
    {
        var provider = new JobStepDetailsProvider();
        var step = new ActiveProcessStep
        {
            Settings = new ActiveProcessSettings
            {
                Target = new ProcessTargetSettings
                {
                    ProcessName = "notepad",
                    WindowTitleContains = "Editor"
                }
            }
        };

        var summary = provider.GetSummary(step, new JobStep[] { step });

        Assert.Contains("notepad", summary);
        Assert.Contains("Editor", summary);
    }

    [Fact]
    public void DetailsProvider_RendersJobVariableReferenceByName()
    {
        var variable = new JobVariable
        {
            Name = "Greeting",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = JsonValue.Create("Hello")
        };
        var step = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new ResultBinding
                {
                    ProviderId = ValueProviderIds.JobVariable,
                    SourceId = variable.Id.ToString("D")
                }
            }
        };

        var details = new JobStepDetailsProvider().GetDetails(
            step,
            new JobStep[] { step },
            new[] { variable });

        var item = Assert.Single(details.Groups.SelectMany(group => group.Items),
            item => item.SourceLabel?.Contains(variable.Name) == true);
        Assert.Contains("Hello", item.Value);
        Assert.DoesNotContain(variable.Name, item.Value);
        Assert.Contains(Loc.Get("Ui.Job.Variables.Scope.Shared"), item.SourceLabel);
        Assert.Null(item.UsageText);
        Assert.False(item.IsWarning);
    }

    [Fact]
    public void DetailsProvider_RendersLocalValueWithoutInternalProviderName()
    {
        var local = new LocalValue
        {
            Name = "show_text_text_result",
            ValueKind = ResultValueKind.Text,
            Value = JsonValue.Create("Hello from the step")
        };
        var step = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new ResultBinding
                {
                    ProviderId = ValueProviderIds.LocalValue,
                    SourceId = local.Id.ToString("D")
                }
            }
        };

        var details = new JobStepDetailsProvider().GetDetails(
            step,
            new JobStep[] { step },
            [],
            [ValueProviderSourceDescriptor.FromVariable(local)],
            [local]);

        var item = Assert.Single(details.Groups.SelectMany(group => group.Items),
            item => item.Value.Contains("Hello from the step"));
        Assert.Null(item.SourceLabel);
        Assert.DoesNotContain(ValueProviderIds.LocalValue, item.Value);
        Assert.DoesNotContain(local.Name, item.Value);
    }

    [Fact]
    public void DetailsProvider_RendersSelectedVariablePropertyValueAsPrimaryText()
    {
        var variable = new JobVariable
        {
            Name = "Area",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Rectangle,
            Value = JsonNode.Parse("""{"x":10,"y":20,"width":300,"height":200}""")
        };
        var step = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new ResultBinding
                {
                    ProviderId = ValueProviderIds.JobVariable,
                    SourceId = variable.Id.ToString("D"),
                    ValuePath = "Center.X"
                }
            }
        };

        var details = new JobStepDetailsProvider().GetDetails(
            step, new JobStep[] { step }, [variable]);

        var item = Assert.Single(details.Groups.SelectMany(group => group.Items),
            item => item.SourceLabel?.Contains(variable.Name) == true);
        Assert.Equal("160", item.Value);
        Assert.Contains("›", item.SourceLabel);
        Assert.DoesNotContain("width", item.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DetailsProvider_LocalizesDirectEnumValuesAndCollapsesAdvancedGroup()
    {
        var local = new LocalValue
        {
            Name = "start_process_window_mode",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create(nameof(StartProcessWindowMode.ApplicationDefault))
        };
        var step = new StartProcessStep();
        step.Inputs[StartProcessStepDefinition.WindowModeFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = local.Id.ToString("D")
        };

        var details = new JobStepDetailsProvider().GetDetails(
            step,
            new JobStep[] { step },
            [],
            [ValueProviderSourceDescriptor.FromVariable(local)],
            [local]);

        var advanced = Assert.Single(details.Groups,
            group => group.Title == Loc.Get("Ui.Job.Steps.DetailsAdvanced"));
        Assert.False(advanced.IsExpandedByDefault);
        var windowMode = Assert.Single(advanced.Items,
            item => item.Name == Loc.Get("Ui.Step.Settings.WindowMode"));
        Assert.Equal(Loc.Get("Enum.StartProcessWindowMode.ApplicationDefault"), windowMode.Value);
        Assert.DoesNotContain(nameof(StartProcessWindowMode.ApplicationDefault), windowMode.Value);
    }

    [Fact]
    public void DetailsProvider_MarksMissingVariableSourceAsWarning()
    {
        var step = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new ResultBinding
                {
                    ProviderId = ValueProviderIds.JobVariable,
                    SourceId = Guid.NewGuid().ToString("D")
                }
            }
        };

        var details = new JobStepDetailsProvider().GetDetails(step, new JobStep[] { step }, []);
        var item = Assert.Single(details.Groups.SelectMany(group => group.Items),
            item => item.IsWarning);

        Assert.Equal(Loc.Get("Ui.Job.Steps.DetailsSourceMissing"), item.SourceLabel);
    }

    [Fact]
    public void DetailsProvider_RendersConditionJobVariableReferencesByName()
    {
        var variable = new JobVariable { Name = "Enabled", ValueKind = ResultValueKind.Boolean };
        var reference = new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D"),
            Operator = ConditionOperator.Equals,
            Comparison = new ComparisonOperand
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.JobVariable,
                SourceId = variable.Id.ToString("D")
            }
        };
        var step = new IfStep { Settings = new IfConditionSettings { Conditions = [reference] } };

        var details = new JobStepDetailsProvider().GetDetails(
            step,
            new JobStep[] { step },
            new[] { variable });
        var condition = Assert.Single(details.Groups.SelectMany(group => group.Items),
            item => item.Name.Contains("1.", StringComparison.Ordinal));

        Assert.Equal(2, condition.Value.Split(variable.Name, StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain(variable.Id.ToString("D"), condition.Value);
    }

    [Fact]
    public void DetailsProvider_RendersDirectConditionValueAsReadableConditionRows()
    {
        var source = new UserChoiceStep
        {
            Id = "choice",
            Settings = new UserChoiceSettings { Title = "Choose mode" }
        };
        var settings = new IfConditionSettings
        {
            MatchMode = ConditionMatchMode.All,
            Conditions =
            [
                new StepCondition
                {
                    SourceStepId = source.Id,
                    PropertyPath = "SelectedValue",
                    Operator = ConditionOperator.Equals,
                    ComparisonValue = "automatic"
                }
            ]
        };
        var directValue = new LocalValue
        {
            Name = "if_conditions",
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonSerializer.SerializeToNode(settings)
        };
        var step = new IfStep { Id = "if" };
        step.Inputs[IfStepDefinition.ConditionsFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = directValue.Id.ToString("D")
        };

        var details = new JobStepDetailsProvider().GetDetails(
            step,
            new JobStep[] { source, step },
            [],
            [ValueProviderSourceDescriptor.FromVariable(directValue)],
            [directValue]);

        var items = details.Groups.SelectMany(group => group.Items).ToArray();
        Assert.Contains(items, item => item.Name == Loc.Get("Ui.Step.Settings.ConditionMatchMode")
                                      && item.Value == Loc.Get("Ui.Step.Settings.AllAND"));
        var condition = Assert.Single(items, item => item.Name.Contains("1.", StringComparison.Ordinal));
        Assert.Contains("automatic", condition.Value);
        Assert.DoesNotContain("source_step_id", condition.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{", condition.Value);
    }

    [Fact]
    public void VisualOverlayDefinitions_DeclarePortableCapabilitiesAndContracts()
    {
        var imageOptions = Assert.Single(new ShowImageStepDefinition().Descriptor.Fields,
            field => field.EditorHint == StepEditorHints.VisualOverlay).VisualOverlayOptions;
        var desktopOptions = Assert.Single(new ShowOnDesktopStepDefinition().Descriptor.Fields,
            field => field.EditorHint == StepEditorHints.VisualOverlay).VisualOverlayOptions;

        Assert.NotNull(imageOptions);
        Assert.Equal("detections", imageOptions.DetectionInputContractId);
        Assert.Equal("text", imageOptions.TextInputContractId);
        Assert.False(imageOptions.SupportsDesktopPlacement);
        Assert.NotNull(desktopOptions);
        Assert.True(desktopOptions.SupportsDesktopPlacement);
    }

    [Fact]
    public void TimeoutDefinition_DescribesPortableEditorAndPresentation()
    {
        var definition = new TimeoutStepDefinition();

        var json = JsonSerializer.Serialize(definition.Descriptor);

        Assert.Contains("delay_ms", json);
        Assert.DoesNotContain("System.Windows", json);
        Assert.Equal("timeout", definition.Descriptor.TypeId);
        Assert.Equal(
            [TimeoutStepDefinition.DelayFieldId],
            definition.Descriptor.Presentation.DetailFieldIds);
    }

    [Fact]
    public void TimeoutDefinition_RoundTripsExistingStepWithoutChangingItsIdentity()
    {
        var definition = new TimeoutStepDefinition();
        var existing = new TimeoutStep
        {
            Id = "existing-timeout",
            IsEnabled = false,
            IsBreakpoint = true,
            Settings = new TimeoutSettings { DelayMs = 2750 }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<TimeoutStep>(definition.ApplyDraft(draft, existing));

        Assert.Same(existing, updated);
        Assert.Equal("existing-timeout", updated.Id);
        Assert.False(updated.IsEnabled);
        Assert.True(updated.IsBreakpoint);
        Assert.Equal(2750, updated.Settings.DelayMs);
    }

    [Fact]
    public void TimeoutDefinition_RejectsNegativeDelayWithFieldSpecificIssue()
    {
        var definition = new TimeoutStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[TimeoutStepDefinition.DelayFieldId] = System.Text.Json.Nodes.JsonValue.Create(-1);

        var issue = Assert.Single(definition.ValidateDraft(draft));

        Assert.Equal("StepValidation.Minimum", issue.Code);
        Assert.Equal(TimeoutStepDefinition.DelayFieldId, issue.FieldId);
    }

    [Fact]
    public void BlockInputDefinition_RoundTripsExistingStepAndDescribesLimits()
    {
        var definition = new BlockInputStepDefinition();
        var existing = new BlockInputStep
        {
            Id = "existing-block",
            IsEnabled = false,
            IsBreakpoint = true,
            Settings = new BlockInputSettings { SafetyTimeoutSeconds = 75 }
        };

        var field = Assert.Single(definition.Descriptor.Fields);
        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<BlockInputStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal(BlockInputStepDefinition.SafetyTimeoutFieldId, field.Id);
        Assert.Equal(1m, field.Constraints?.Minimum);
        Assert.Equal(3600m, field.Constraints?.Maximum);
        Assert.Same(existing, updated);
        Assert.Equal("existing-block", updated.Id);
        Assert.False(updated.IsEnabled);
        Assert.True(updated.IsBreakpoint);
        Assert.Equal(75, updated.Settings.SafetyTimeoutSeconds);
    }

    [Theory]
    [InlineData(0, "StepValidation.Minimum")]
    [InlineData(3601, "StepValidation.Maximum")]
    public void BlockInputDefinition_RejectsTimeoutOutsideSupportedRange(int timeout, string expectedCode)
    {
        var definition = new BlockInputStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[BlockInputStepDefinition.SafetyTimeoutFieldId] =
            System.Text.Json.Nodes.JsonValue.Create(timeout);

        var issue = Assert.Single(definition.ValidateDraft(draft));

        Assert.Equal(expectedCode, issue.Code);
        Assert.Equal(BlockInputStepDefinition.SafetyTimeoutFieldId, issue.FieldId);
    }

    [Fact]
    public void UnblockInputDefinition_RepresentsParameterlessEditor()
    {
        var definition = new UnblockInputStepDefinition();

        var draft = definition.CreateDraft();
        var created = definition.ApplyDraft(draft);

        Assert.Empty(definition.Descriptor.Fields);
        Assert.Equal("Ui.Step.Settings.UnblockInputDescription",
            definition.Descriptor.Presentation.EditorDescriptionKey);
        Assert.Empty(draft.Values);
        Assert.IsType<UnblockInputStep>(created);
        Assert.Empty(definition.ValidateDraft(draft));
    }

    [Fact]
    public void EndJobDefinition_RoundTripsSkipEndStepsAndDescribesBooleanEditor()
    {
        var definition = new EndJobStepDefinition();
        var existing = new EndJobStep
        {
            Id = "existing-end",
            Settings = new EndJobSettings { SkipEndSteps = true }
        };

        var field = Assert.Single(definition.Descriptor.Fields);
        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<EndJobStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal(EndJobStepDefinition.SkipEndStepsFieldId, field.Id);
        Assert.Equal(StepValueKind.Boolean, field.ValueKind);
        Assert.Equal("Ui.Step.Settings.EndJobDescription",
            definition.Descriptor.Presentation.EditorDescriptionKey);
        Assert.Same(existing, updated);
        Assert.True(updated.Settings.SkipEndSteps);
    }

    [Fact]
    public void EndJobDefinition_RejectsNonBooleanDraftValue()
    {
        var definition = new EndJobStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[EndJobStepDefinition.SkipEndStepsFieldId] =
            System.Text.Json.Nodes.JsonValue.Create("invalid");

        var issue = Assert.Single(definition.ValidateDraft(draft));

        Assert.Equal("StepValidation.Boolean", issue.Code);
        Assert.Equal(EndJobStepDefinition.SkipEndStepsFieldId, issue.FieldId);
    }

    [Fact]
    public void EndJobDefinition_UsesFalseForMissingOptionalValue()
    {
        var definition = new EndJobStepDefinition();
        var draft = new StepDraft(definition.Descriptor.TypeId);

        var created = Assert.IsType<EndJobStep>(definition.ApplyDraft(draft));

        Assert.False(created.Settings.SkipEndSteps);
        Assert.Empty(definition.ValidateDraft(draft));
    }

    [Fact]
    public void ContinueJobDefinition_RepresentsParameterlessEditor()
    {
        var definition = new ContinueJobStepDefinition();

        var draft = definition.CreateDraft();

        Assert.Empty(definition.Descriptor.Fields);
        Assert.Equal("Ui.Step.Settings.ContinueJobDescription",
            definition.Descriptor.Presentation.EditorDescriptionKey);
        Assert.IsType<ContinueJobStep>(definition.ApplyDraft(draft));
        Assert.Empty(definition.ValidateDraft(draft));
    }

    [Fact]
    public void DesktopDuplicationDefinition_RoundTripsSettingsAndRequestsMonitorPicker()
    {
        var definition = new DesktopDuplicationStepDefinition();
        var existing = new DesktopDuplicationStep
        {
            Id = "existing-capture",
            Settings = new DesktopDuplicationSettings
            {
                DesktopIdx = 2,
                CaptureCursor = true
            }
        };

        var monitorField = definition.Descriptor.Fields.Single(field =>
            field.Id == DesktopDuplicationStepDefinition.DesktopIndexFieldId);
        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<DesktopDuplicationStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal(StepEditorHints.MonitorPicker, monitorField.EditorHint);
        Assert.Equal(0m, monitorField.Constraints?.Minimum);
        Assert.Same(existing, updated);
        Assert.Equal(2, updated.Settings.DesktopIdx);
        Assert.True(updated.Settings.CaptureCursor);
    }

    [Fact]
    public void DesktopDuplicationDefinition_RejectsNegativeMonitorIndex()
    {
        var definition = new DesktopDuplicationStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[DesktopDuplicationStepDefinition.DesktopIndexFieldId] =
            System.Text.Json.Nodes.JsonValue.Create(-1);

        var issue = Assert.Single(definition.ValidateDraft(draft));

        Assert.Equal("StepValidation.Minimum", issue.Code);
        Assert.Equal(DesktopDuplicationStepDefinition.DesktopIndexFieldId, issue.FieldId);
    }

    [Fact]
    public void DesktopDuplicationDefinition_DefaultsMissingOptionalCursorToFalse()
    {
        var definition = new DesktopDuplicationStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values.Remove(DesktopDuplicationStepDefinition.CaptureCursorFieldId);

        var created = Assert.IsType<DesktopDuplicationStep>(definition.ApplyDraft(draft));

        Assert.False(created.Settings.CaptureCursor);
        Assert.Empty(definition.ValidateDraft(draft));
    }

    [Fact]
    public void ScriptExecutionDefinition_RoundTripsSettingsAndDescribesAdvancedFileEditor()
    {
        var definition = new ScriptExecutionStepDefinition();
        var existing = new ScriptExecutionStep
        {
            Id = "existing-script",
            Settings = new ScriptExecutionSettings
            {
                ScriptPath = @"C:\scripts\sample.ps1",
                Arguments = "-Verbose",
                WaitForExit = true
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<ScriptExecutionStep>(definition.ApplyDraft(draft, existing));
        var pathField = definition.Descriptor.Fields.Single(field =>
            field.Id == ScriptExecutionStepDefinition.ScriptPathFieldId);
        var advanced = definition.Descriptor.Presentation.EditorSections.Single(section => section.Collapsible);

        Assert.Equal(StepEditorHints.FilePicker, pathField.EditorHint);
        Assert.Equal([ScriptExecutionStepDefinition.ArgumentsFieldId], advanced.FieldIds);
        Assert.False(advanced.InitiallyExpanded);
        Assert.Same(existing, updated);
        Assert.Equal(@"C:\scripts\sample.ps1", updated.Settings.ScriptPath);
        Assert.Equal("-Verbose", updated.Settings.Arguments);
        Assert.True(updated.Settings.WaitForExit);
    }

    [Fact]
    public void ScriptExecutionDefinition_RequiresExistingScript()
    {
        var definition = new ScriptExecutionStepDefinition();
        var draft = definition.CreateDraft();

        Assert.Equal("StepValidation.Required", Assert.Single(definition.ValidateDraft(draft)).Code);

        draft.Values[ScriptExecutionStepDefinition.ScriptPathFieldId] =
            System.Text.Json.Nodes.JsonValue.Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ps1"));

        Assert.Equal("StepValidation.Invalid", Assert.Single(definition.ValidateDraft(draft)).Code);
    }

    [Fact]
    public void GetProcessDefinition_RoundTripsSelectorsAndRequestsSuggestions()
    {
        var definition = new GetProcessStepDefinition();
        var existing = new GetProcessStep
        {
            Settings = new GetProcessSettings
            {
                Query = new ProcessTargetSettings
                {
                    ProcessName = "notepad",
                    ExecutablePath = @"C:\Windows\notepad.exe",
                    WindowTitleContains = "Notes"
                }
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<GetProcessStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal(StepEditorHints.ProcessNameSuggestions,
            definition.Descriptor.Fields.Single(field => field.Id == GetProcessStepDefinition.ProcessNameFieldId).EditorHint);
        Assert.Equal(StepEditorHints.ExecutablePathSuggestions,
            definition.Descriptor.Fields.Single(field => field.Id == GetProcessStepDefinition.ExecutablePathFieldId).EditorHint);
        Assert.Equal("notepad", updated.Settings.Query.ProcessName);
        Assert.Equal(@"C:\Windows\notepad.exe", updated.Settings.Query.ExecutablePath);
        Assert.Equal("Notes", updated.Settings.Query.WindowTitleContains);
    }

    [Fact]
    public void GetProcessDefinition_RequiresNameOrExecutablePath()
    {
        var definition = new GetProcessStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[GetProcessStepDefinition.WindowTitleFieldId] =
            System.Text.Json.Nodes.JsonValue.Create("title alone is not enough");

        Assert.Single(definition.ValidateDraft(draft));

        draft.Values[GetProcessStepDefinition.ProcessNameFieldId] =
            System.Text.Json.Nodes.JsonValue.Create("explorer");
        Assert.Empty(definition.ValidateDraft(draft));
    }

    [Fact]
    public void MakroExecutionDefinition_RoundTripsStableReference()
    {
        var macroId = Guid.NewGuid();
        var definition = new MakroExecutionStepDefinition();
        var existing = new MakroExecutionStep
        {
            Settings = new MakroExecutionSettings
            {
                MakroId = macroId,
                MakroName = "Daily cleanup"
            }
        };

        var field = Assert.Single(definition.Descriptor.Fields);
        var draft = definition.CreateDraft(existing);
        var reference = draft.Values[MakroExecutionStepDefinition.MacroFieldId]!
            .Deserialize<StepReferenceValue>();
        var updated = Assert.IsType<MakroExecutionStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal(StepEditorHints.MacroPicker, field.EditorHint);
        Assert.Equal(macroId.ToString("D"), reference?.Id);
        Assert.Equal("Daily cleanup", reference?.Name);
        Assert.Equal(macroId, updated.Settings.MakroId);
        Assert.Equal("Daily cleanup", updated.Settings.MakroName);
    }

    [Fact]
    public void MakroExecutionDefinition_RejectsMissingOrLegacyNameOnlyReference()
    {
        var definition = new MakroExecutionStepDefinition();
        var missing = definition.CreateDraft();
        var legacy = definition.CreateDraft(new MakroExecutionStep
        {
            Settings = new MakroExecutionSettings { MakroName = "Legacy macro" }
        });

        Assert.Single(definition.ValidateDraft(missing));
        Assert.Single(definition.ValidateDraft(legacy));
    }

    [Fact]
    public void JobExecutionDefinition_RoundTripsReferenceAndWaitOption()
    {
        var jobId = Guid.NewGuid();
        var definition = new JobExecutionStepDefinition();
        var existing = new JobExecutionStep
        {
            Settings = new JobExecutionStepSettings
            {
                JobId = jobId,
                JobName = "Child job",
                WaitForCompletion = false
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<JobExecutionStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal(StepEditorHints.JobPicker,
            definition.Descriptor.Fields.Single(field => field.Id == JobExecutionStepDefinition.JobFieldId).EditorHint);
        Assert.Equal(jobId, updated.Settings.JobId);
        Assert.Equal("Child job", updated.Settings.JobName);
        Assert.False(updated.Settings.WaitForCompletion);
    }

    [Fact]
    public void ActiveProcessDefinition_RoundTripsProcessReference()
    {
        var definition = new ActiveProcessStepDefinition();
        var existing = new ActiveProcessStep
        {
            Settings = new ActiveProcessSettings
            {
                Target = new ProcessTargetSettings
                {
                    ProcessSource = new ResultBinding
                    {
                        SourceStepId = "process-source",
                        PropertyId = "process"
                    }
                }
            }
        };

        var field = Assert.Single(definition.Descriptor.Fields);
        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<ActiveProcessStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal(StepEditorHints.ProcessTargetPicker, field.EditorHint);
        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal("process-source", updated.Settings.Target.ProcessSource.SourceStepId);
        Assert.Equal("process", updated.Settings.Target.ProcessSource.PropertyId);
        Assert.Empty(updated.Settings.Target.ProcessName);
    }

    [Fact]
    public void ActiveProcessDefinition_AcceptsManualNameAndRejectsEmptyTarget()
    {
        var definition = new ActiveProcessStepDefinition();
        var empty = definition.CreateDraft();
        var configured = definition.CreateDraft(new ActiveProcessStep
        {
            Settings = new ActiveProcessSettings
            {
                Target = new ProcessTargetSettings { ProcessName = "explorer" }
            }
        });

        Assert.Single(definition.ValidateDraft(empty));
        Assert.Empty(definition.ValidateDraft(configured));
    }

    [Fact]
    public void ActiveWindowDefinition_RoundTripsSelectorTitleAndCache()
    {
        var definition = new ActiveWindowStepDefinition();
        var existing = new ActiveWindowStep
        {
            Settings = new ActiveWindowSettings
            {
                Target = new ProcessTargetSettings
                {
                    ProcessName = "notepad",
                    WindowTitleContains = "Notes"
                },
                CacheMs = 250
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<ActiveWindowStep>(definition.ApplyDraft(draft, existing));
        var advanced = definition.Descriptor.Presentation.EditorSections.Single(section => section.Collapsible);

        Assert.Equal([ActiveWindowStepDefinition.CacheFieldId], advanced.FieldIds);
        Assert.Equal("notepad", updated.Settings.Target.ProcessName);
        Assert.Equal("Notes", updated.Settings.Target.WindowTitleContains);
        Assert.Equal(250, updated.Settings.CacheMs);
    }

    [Fact]
    public void ActiveWindowDefinition_RejectsNegativeCache()
    {
        var definition = new ActiveWindowStepDefinition();
        var draft = definition.CreateDraft(new ActiveWindowStep
        {
            Settings = new ActiveWindowSettings
            {
                Target = new ProcessTargetSettings { ProcessName = "notepad" }
            }
        });
        draft.Values[ActiveWindowStepDefinition.CacheFieldId] =
            System.Text.Json.Nodes.JsonValue.Create(-1);

        var issue = Assert.Single(definition.ValidateDraft(draft));

        Assert.Equal("StepValidation.Minimum", issue.Code);
        Assert.Equal(ActiveWindowStepDefinition.CacheFieldId, issue.FieldId);
    }

    [Fact]
    public void TerminateProcessDefinition_RoundTripsManualAndReferencedTargets()
    {
        var definition = new TerminateProcessStepDefinition();
        var manual = new TerminateProcessStep
        {
            Settings = new TerminateProcessSettings
            {
                Target = new ProcessTargetSettings
                {
                    ProcessName = "notepad",
                    WindowTitleContains = "Notes"
                }
            }
        };
        var referenced = new TerminateProcessStep
        {
            Settings = new TerminateProcessSettings
            {
                Target = new ProcessTargetSettings
                {
                    ProcessSource = new ResultBinding
                    {
                        SourceStepId = "source-process",
                        PropertyId = "process"
                    }
                }
            }
        };

        var manualDraft = definition.CreateDraft(manual);
        var referencedDraft = definition.CreateDraft(referenced);
        var manualResult = Assert.IsType<TerminateProcessStep>(definition.ApplyDraft(manualDraft, manual));
        var referencedResult = Assert.IsType<TerminateProcessStep>(definition.ApplyDraft(referencedDraft, referenced));

        Assert.Equal(StepEditorHints.ProcessTargetPicker,
            definition.Descriptor.Fields.Single(field => field.Id == TerminateProcessStepDefinition.ProcessTargetFieldId).EditorHint);
        Assert.Empty(definition.ValidateDraft(manualDraft));
        Assert.Empty(definition.ValidateDraft(referencedDraft));
        Assert.Equal("notepad", manualResult.Settings.Target.ProcessName);
        Assert.Equal("Notes", manualResult.Settings.Target.WindowTitleContains);
        Assert.Equal("source-process", referencedResult.Settings.Target.ProcessSource.SourceStepId);
        Assert.Single(definition.ValidateDraft(definition.CreateDraft()));
    }

    [Fact]
    public void FocusProcessDefinition_DescribesEnumsVisibilityAndNormalizesLegacyFullscreen()
    {
        var definition = new FocusProcessStepDefinition();
        var existing = new FocusProcessStep
        {
            Settings = new FocusProcessSettings
            {
                Action = FocusProcessAction.BringToFront,
                WindowMode = FocusProcessWindowMode.Fullscreen,
                Target = new ProcessTargetSettings
                {
                    ExecutablePath = @"C:\Windows\notepad.exe",
                    WindowTitleContains = "Notes"
                }
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<FocusProcessStep>(definition.ApplyDraft(draft, existing));
        var action = definition.Descriptor.Fields.Single(field => field.Id == FocusProcessStepDefinition.ActionFieldId);
        var windowMode = definition.Descriptor.Fields.Single(field => field.Id == FocusProcessStepDefinition.WindowModeFieldId);

        Assert.Equal(StepValueKind.Enum, action.ValueKind);
        Assert.Equal(2, action.Options?.Count);
        Assert.Equal(FocusProcessStepDefinition.ActionFieldId, windowMode.VisibleWhen?.FieldId);
        Assert.Equal(FocusProcessWindowMode.Maximized, updated.Settings.WindowMode);
        Assert.Equal(@"C:\Windows\notepad.exe", updated.Settings.Target.ExecutablePath);
        Assert.Empty(definition.ValidateDraft(draft));

        draft.Values[FocusProcessStepDefinition.ActionFieldId] =
            System.Text.Json.Nodes.JsonValue.Create("Unsupported");
        Assert.Equal(FocusProcessStepDefinition.ActionFieldId, Assert.Single(definition.ValidateDraft(draft)).FieldId);
    }

    [Fact]
    public void StartProcessDefinition_RoundTripsSettingsAndKeepsLegacyTerminateValid()
    {
        var executable = Path.GetTempFileName();
        try
        {
            var definition = new StartProcessStepDefinition();
            var existing = new StartProcessStep
            {
                Settings = new StartProcessSettings
                {
                    ExecutablePath = executable,
                    Arguments = "--quiet",
                    WorkingDirectory = Path.GetTempPath(),
                    WaitForExit = true,
                    MonitorIndex = 2,
                    PlacementMode = StartProcessPlacementMode.Custom,
                    OffsetX = 40,
                    OffsetY = 60,
                    WindowMode = StartProcessWindowMode.Maximized
                }
            };

            var draft = definition.CreateDraft(existing);
            var updated = Assert.IsType<StartProcessStep>(definition.ApplyDraft(draft, existing));
            var placement = definition.Descriptor.Fields.Single(field =>
                field.Id == StartProcessStepDefinition.PlacementModeFieldId);

            Assert.Empty(definition.ValidateDraft(draft));
            Assert.Equal("--quiet", updated.Settings.Arguments);
            Assert.True(updated.Settings.WaitForExit);
            Assert.Equal(2, updated.Settings.MonitorIndex);
            Assert.Equal(StartProcessPlacementMode.Custom, updated.Settings.PlacementMode);
            Assert.Equal(StartProcessStepDefinition.PlacementModeFieldId,
                definition.Descriptor.Fields.Single(field => field.Id == StartProcessStepDefinition.OffsetXFieldId)
                    .VisibleWhen?.FieldId);
            Assert.Equal(2, placement.Options?.Count);

            var legacy = new StartProcessStep
            {
                Settings = new StartProcessSettings
                {
                    Action = StartProcessAction.Terminate,
                    Target = new ProcessTargetSettings { ProcessName = "notepad" }
                }
            };
            Assert.Empty(definition.ValidateDraft(definition.CreateDraft(legacy)));
        }
        finally
        {
            File.Delete(executable);
        }
    }

    [Fact]
    public void DynamicRoiDefinition_RoundTripsBindingAndRejectsInvalidValues()
    {
        var definition = new DynamicRoiStepDefinition();
        var existing = new DynamicRoiStep
        {
            Settings = new DynamicRoiSettings
            {
                BoundsSource = new ResultBinding
                {
                    SourceStepId = "detection",
                    PropertyId = "bounds"
                },
                PaddingSource = ResultBinding.ForStepResult("padding", "value"),
                MinimumConfidence = 0.75,
                FullSearchInterval = 8,
                ResetAfterMisses = 4
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<DynamicRoiStep>(definition.ApplyDraft(draft, existing));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal("bounds", definition.Descriptor.Fields.Single(field =>
            field.Id == DynamicRoiStepDefinition.BoundsSourceFieldId).InputContractId);
        var paddingField = definition.Descriptor.Fields.Single(field =>
            field.Id == DynamicRoiStepDefinition.PaddingSourceFieldId);
        Assert.Equal(StepValueKind.ResultBinding, paddingField.ValueKind);
        Assert.Equal("padding", paddingField.InputContractId);
        Assert.Equal("detection", updated.Settings.BoundsSource.SourceStepId);
        Assert.Equal("padding", updated.Settings.PaddingSource.SourceStepId);
        Assert.Equal(0.75, updated.Settings.MinimumConfidence);

        draft.Values[DynamicRoiStepDefinition.MinimumConfidenceFieldId] =
            System.Text.Json.Nodes.JsonValue.Create(1.1);
        Assert.Equal("StepValidation.Maximum", Assert.Single(definition.ValidateDraft(draft)).Code);

        var fresh = definition.CreateDraft();
        fresh.Values[DynamicRoiStepDefinition.BoundsSourceFieldId] = JsonSerializer.SerializeToNode(
            ResultBinding.ForStepResult("detection", "bounds"));
        Assert.Contains(definition.ValidateDraft(fresh), issue =>
            issue.FieldId == DynamicRoiStepDefinition.PaddingSourceFieldId
            && issue.Code == "StepValidation.Required");
    }

    [Fact]
    public void PredictMovementDefinition_RoundTripsVisibleAndLegacyTimingSettings()
    {
        var definition = new PredictMovementStepDefinition();
        var existing = new PredictMovementStep
        {
            Settings = new PredictMovementSettings
            {
                PointsSource = new ResultBinding { SourceStepId = "points", PropertyId = "point" },
                MinSamples = 5,
                PredictionMs = 175,
                ResetDistanceThreshold = 300,
                MaxSampleAgeMs = 700,
                PredictionModel = "Kalman",
                TimeBasis = "Capture",
                MaxPredictionDistance = 450,
                MaxFitError = 60,
                MinimumConfidence = 0.4
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<PredictMovementStep>(definition.ApplyDraft(draft, existing));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal("Kalman", updated.Settings.PredictionModel);
        Assert.Equal(175, updated.Settings.PredictionMs);
        Assert.Equal("Capture", updated.Settings.TimeBasis);
        Assert.Equal(0.4, updated.Settings.MinimumConfidence);
        Assert.DoesNotContain(PredictMovementStepDefinition.PredictionMsFieldId,
            definition.Descriptor.Presentation.EditorSections.SelectMany(section => section.FieldIds));

        draft.Values[PredictMovementStepDefinition.MinSamplesFieldId] =
            System.Text.Json.Nodes.JsonValue.Create(1);
        Assert.Equal(PredictMovementStepDefinition.MinSamplesFieldId,
            Assert.Single(definition.ValidateDraft(draft)).FieldId);
    }

    [Fact]
    public void KlickOnPointDefinition_RoundTripsAndRejectsUnsupportedClickType()
    {
        var definition = new KlickOnPointStepDefinition();
        var existing = new KlickOnPointStep
        {
            Settings = new KlickOnPointSettings
            {
                PointsSource = new ResultBinding { SourceStepId = "detection", PropertyId = "point" },
                ClickType = "right",
                DoubleClick = true,
                TimeoutMs = 1200,
                OffsetX = 4,
                OffsetY = -3
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<KlickOnPointStep>(definition.ApplyDraft(draft, existing));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal("right", updated.Settings.ClickType);
        Assert.True(updated.Settings.DoubleClick);
        Assert.Equal(1200, updated.Settings.TimeoutMs);
        Assert.Equal(-3, updated.Settings.OffsetY);

        draft.Values[KlickOnPointStepDefinition.ClickTypeFieldId] =
            System.Text.Json.Nodes.JsonValue.Create("unsupported");
        Assert.Equal(KlickOnPointStepDefinition.ClickTypeFieldId,
            Assert.Single(definition.ValidateDraft(draft)).FieldId);
    }

    [Fact]
    public void FileSystemOperationDefinition_RoundTripsUnifiedDirectoryPathsAndValidatesOperationSpecificFields()
    {
        var definition = new FileSystemOperationStepDefinition();
        var existing = new FileSystemOperationStep
        {
            Settings = new FileSystemOperationSettings
            {
                Operation = FileSystemOperation.Move,
                SourceMode = FileSystemPathSource.ExplicitPath,
                SourcePath = @"C:\Source",
                TargetMode = FileSystemPathSource.ExplicitPath,
                TargetPath = @"C:\Target",
                CreateParentDirectories = false,
                RetryLockedFiles = true,
                RetryCount = 7,
                RetryDelayMs = 250
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<FileSystemOperationStep>(definition.ApplyDraft(draft, existing));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal(FileSystemOperation.Move, updated.Settings.Operation);
        Assert.Equal(@"C:\Source", updated.Settings.SourcePath);
        Assert.False(updated.Settings.SourceResult.IsConfigured);
        Assert.Equal(@"C:\Target", updated.Settings.TargetPath);
        Assert.False(updated.Settings.CreateParentDirectories);
        Assert.Equal(7, updated.Settings.RetryCount);

        draft.Values[FileSystemOperationStepDefinition.OperationFieldId] =
            System.Text.Json.Nodes.JsonValue.Create("Rename");
        draft.Values[FileSystemOperationStepDefinition.NewNameFieldId] =
            System.Text.Json.Nodes.JsonValue.Create("folder/invalid");
        Assert.Equal(FileSystemOperationStepDefinition.NewNameFieldId,
            Assert.Single(definition.ValidateDraft(draft)).FieldId);
    }

    [Fact]
    public void ShowTextDefinition_RoundTripsAllDisplaySettingsAndRequiresSelectedSource()
    {
        var definition = new ShowTextStepDefinition();
        var existing = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                Text = "preserved fallback",
                TextResult = new ResultBinding { SourceStepId = "text", PropertyId = "value" },
                FontSize = 31.5f,
                FontColor = "#123456",
                Opacity = 0.65f,
                DesktopIndex = 2,
                OffsetX = -25,
                OffsetY = 80,
                DurationMs = 4200,
                ClearOnJobEnd = true
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<ShowTextStep>(definition.ApplyDraft(draft, existing));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal(ShowTextSource.TaskResult, updated.Settings.TextSource);
        Assert.Equal("text", updated.Settings.TextResult.SourceStepId);
        Assert.Equal(31.5f, updated.Settings.FontSize);
        Assert.Equal("#123456", updated.Settings.FontColor);
        Assert.Equal(0.65f, updated.Settings.Opacity);
        Assert.True(updated.Settings.ClearOnJobEnd);

        draft.Values[ShowTextStepDefinition.TextResultFieldId] = JsonSerializer.SerializeToNode(new ResultBinding());
        Assert.Equal(ShowTextStepDefinition.TextResultFieldId,
            Assert.Single(definition.ValidateDraft(draft)).FieldId);
    }

    [Fact]
    public void ElseAndEndIfDefinitions_AreParameterlessAndPreserveStepState()
    {
        var elseDefinition = new ElseStepDefinition();
        var endIfDefinition = new EndIfStepDefinition();
        var existingElse = new ElseStep
        {
            Id = "existing-else",
            IsBreakpoint = true
        };

        var elseDraft = elseDefinition.CreateDraft(existingElse);
        var updatedElse = Assert.IsType<ElseStep>(elseDefinition.ApplyDraft(elseDraft, existingElse));
        var endIfDraft = endIfDefinition.CreateDraft();

        Assert.Empty(elseDefinition.Descriptor.Fields);
        Assert.Empty(endIfDefinition.Descriptor.Fields);
        Assert.Empty(elseDraft.Values);
        Assert.Empty(endIfDraft.Values);
        Assert.Equal("existing-else", updatedElse.Id);
        Assert.True(updatedElse.IsBreakpoint);
        Assert.False(updatedElse.CanBeDisabled);
        Assert.False(Assert.IsType<EndIfStep>(endIfDefinition.ApplyDraft(endIfDraft)).CanBeDisabled);
        Assert.Empty(elseDefinition.ValidateDraft(elseDraft));
        Assert.Empty(endIfDefinition.ValidateDraft(endIfDraft));
    }

    [Fact]
    public void CameraCaptureDefinition_RoundTripsSpecificQualityAndRejectsMissingCamera()
    {
        var definition = new CameraCaptureStepDefinition();
        var existing = new CameraCaptureStep
        {
            Settings = new CameraCaptureSettings
            {
                CameraId = "camera-1",
                CameraName = "Desk camera",
                QualityMode = CameraQualityMode.Specific,
                Width = 1920,
                Height = 1080,
                FramesPerSecond = 30,
                PixelFormat = "MJPG"
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<CameraCaptureStep>(definition.ApplyDraft(draft, existing));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal("camera-1", updated.Settings.CameraId);
        Assert.Equal("Desk camera", updated.Settings.CameraName);
        Assert.Equal(CameraQualityMode.Specific, updated.Settings.QualityMode);
        Assert.Equal(1920, updated.Settings.Width);
        Assert.Equal(30, updated.Settings.FramesPerSecond);
        Assert.Equal("MJPG", updated.Settings.PixelFormat);

        Assert.Equal(CameraCaptureStepDefinition.CameraFieldId,
            Assert.Single(definition.ValidateDraft(definition.CreateDraft())).FieldId);
    }

    [Fact]
    public void ShowImageDefinition_MigratesLegacyDetectionBindingAndValidatesRequiredValues()
    {
        var definition = new ShowImageStepDefinition();
        var existing = new ShowImageStep
        {
            Settings = new ShowImageSettings
            {
                WindowName = "Preview",
                ImageSource = Binding("capture", "image"),
                DetectionsSource = Binding("detection", "detections")
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<ShowImageStep>(definition.ApplyDraft(draft, existing));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal("Preview", updated.Settings.WindowName);
        Assert.Equal("capture", updated.Settings.ImageSource.SourceStepId);
        Assert.Equal("detection", Assert.Single(updated.Settings.Overlay.DetectionResults).SourceStepId);
        Assert.False(updated.Settings.DetectionsSource.IsConfigured);

        draft.Values[ShowImageStepDefinition.WindowNameFieldId] = JsonValue.Create("");
        Assert.Equal(ShowImageStepDefinition.WindowNameFieldId,
            Assert.Single(definition.ValidateDraft(draft)).FieldId);
    }

    [Fact]
    public void ShowOnDesktopDefinition_RequiresContentAndMigratesLegacyDetectionBinding()
    {
        var definition = new ShowOnDesktopStepDefinition();
        Assert.Equal(ShowOnDesktopStepDefinition.OverlayFieldId,
            Assert.Single(definition.ValidateDraft(definition.CreateDraft())).FieldId);

        var existing = new ShowOnDesktopStep
        {
            Settings = new ShowOnDesktopSettings
            {
                DetectionsSource = Binding("detection", "detections")
            }
        };
        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<ShowOnDesktopStep>(definition.ApplyDraft(draft, existing));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal("detection", Assert.Single(updated.Settings.Overlay.DetectionResults).SourceStepId);
        Assert.False(updated.Settings.DetectionsSource.IsConfigured);
    }

    [Fact]
    public void BuiltInCatalog_ContainsAllMigratedSteps()
    {
        var typeIds = BuiltInStepDefinitions.Instance.Definitions
            .Select(definition => definition.Descriptor.TypeId)
            .ToArray();

        Assert.Equal(
            ["timeout", "block_input", "unblock_input", "end_job", "continue_job", "desktop_duplication", "script_execution", "get_process", "makro_execution", "job_execution", "active_process", "active_window", "terminate_process", "focus_process", "start_process", "dynamic_roi", "predict_movement", "klick_on_point", "klick_on_point_3d", "file_system_operation", "show_text", "user_choice", "point_comparison", "if", "else_if", "windows_state_query", "windows_setting_change", "else", "end_if", "camera_capture", "show_image", "show_on_desktop", "video_creation", "save_image", "template_matching", "ocr", "color_detection", "yolo_detection", "keypoint_matching"],
            typeIds);
    }

    [Fact]
    public void KlickOnPoint3DDefinition_RoundTripsAllSettings()
    {
        var definition = new KlickOnPoint3DStepDefinition();
        var existing = new KlickOnPoint3DStep
        {
            Settings = new KlickOnPoint3DSettings
            {
                PointsSource = Binding("detector", "points"),
                OriginMonitorIndex = 2,
                OriginX = 120,
                OriginY = 80,
                OriginCoordinateSpace = KlickOnPoint3DSettings.MonitorLocalCoordinates,
                ClickType = "right",
                MovementFactorX = 1.5,
                MovementFactorY = 0.75,
                OffsetX = 4,
                OffsetY = -3,
                TimeoutMs = 250,
                DoubleClick = true
            }
        };

        var draft = definition.CreateDraft(existing);
        Assert.Empty(definition.ValidateDraft(draft));
        var updated = Assert.IsType<KlickOnPoint3DStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal("detector", updated.Settings.PointsSource.SourceStepId);
        Assert.Equal(2, updated.Settings.OriginMonitorIndex);
        Assert.Equal((120, 80), (updated.Settings.OriginX, updated.Settings.OriginY));
        Assert.Equal("right", updated.Settings.ClickType);
        Assert.Equal(1.5, updated.Settings.MovementFactorX);
        Assert.Equal(0.75, updated.Settings.MovementFactorY);
        Assert.True(updated.Settings.DoubleClick);
    }

    [Fact]
    public void UserChoiceDefinition_PreservesStableOptionIdsAndRejectsDuplicates()
    {
        var definition = new UserChoiceStepDefinition();
        var title = definition.Descriptor.Fields.Single(field =>
            field.Id == UserChoiceStepDefinition.TitleFieldId);
        Assert.Equal(StepValueKind.Text, title.ValueKind);
        Assert.Equal(StepEditorHints.SingleLineText, title.EditorHint);
        Assert.Null(definition.Descriptor.Fields.Single(field =>
            field.Id == UserChoiceStepDefinition.QuestionFieldId).EditorHint);
        Assert.Null(definition.Descriptor.Fields.Single(field =>
            field.Id == UserChoiceStepDefinition.DescriptionFieldId).EditorHint);
        var existing = new UserChoiceStep
        {
            Settings = new UserChoiceSettings
            {
                Question = "Continue?",
                Options =
                [
                    new UserChoiceOption { Id = "yes", Label = "Yes", Value = "true" },
                    new UserChoiceOption { Id = "no", Label = "No", Value = "false" }
                ]
            }
        };

        var draft = definition.CreateDraft(existing);
        Assert.Empty(definition.ValidateDraft(draft));
        var updated = Assert.IsType<UserChoiceStep>(definition.ApplyDraft(draft, existing));
        Assert.Equal(["yes", "no"], updated.Settings.Options.Select(option => option.Id));

        draft.Values[UserChoiceStepDefinition.OptionsFieldId] = JsonSerializer.SerializeToNode(new[]
        {
            new StepUserChoiceOptionValue("same", "Yes", "1"),
            new StepUserChoiceOptionValue("same", "No", "0")
        });
        Assert.Equal(UserChoiceStepDefinition.OptionsFieldId,
            Assert.Single(definition.ValidateDraft(draft)).FieldId);
    }

    [Fact]
    public void PointComparisonDefinition_RoundTripsExpressionMode()
    {
        var definition = new PointComparisonStepDefinition();
        var existing = new PointComparisonStep
        {
            Settings = new PointComparisonSettings
            {
                Mode = PointComparisonMode.Expression,
                MatchRequirement = PointMatchRequirement.Any,
                Points = [new PointEntry { ManualX = 10, ManualY = 20 }],
                ExpressionSettings = new ExpressionComparisonSettings
                {
                    CombineMode = ExpressionCombineMode.Or,
                    Expressions = [new AxisExpression { Axis = "Y", Operator = PointAxisOperator.GreaterThan, Value = 42 }]
                }
            }
        };

        var draft = definition.CreateDraft(existing);
        Assert.Empty(definition.ValidateDraft(draft));
        var updated = Assert.IsType<PointComparisonStep>(definition.ApplyDraft(draft, existing));

        Assert.Equal(PointComparisonMode.Expression, updated.Settings.Mode);
        Assert.Equal(PointMatchRequirement.Any, updated.Settings.MatchRequirement);
        Assert.Equal(ExpressionCombineMode.Or, updated.Settings.ExpressionSettings.CombineMode);
        var expression = Assert.Single(updated.Settings.ExpressionSettings.Expressions);
        Assert.Equal(("Y", PointAxisOperator.GreaterThan, 42), (expression.Axis, expression.Operator, expression.Value));
    }

    [Fact]
    public void ConditionDefinitions_RoundTripMatchModeConditionsAndLegacyPropertyPath()
    {
        var condition = new StepCondition
        {
            SourceStepId = "source",
            PropertyPath = "Success",
            Operator = ConditionOperator.Equals,
            ComparisonValue = bool.TrueString
        };
        var existing = new IfStep
        {
            Id = "existing-if",
            Settings = new IfConditionSettings
            {
                MatchMode = ConditionMatchMode.Any,
                Conditions = [condition]
            }
        };
        var ifDefinition = new IfStepDefinition();
        var elseIfDefinition = new ElseIfStepDefinition();

        var updatedIf = Assert.IsType<IfStep>(ifDefinition.ApplyDraft(ifDefinition.CreateDraft(existing), existing));
        var updatedElseIf = Assert.IsType<ElseIfStep>(elseIfDefinition.ApplyDraft(
            elseIfDefinition.CreateDraft(new ElseIfStep { Settings = existing.Settings })));

        Assert.Empty(ifDefinition.ValidateDraft(ifDefinition.CreateDraft(updatedIf)));
        Assert.Equal("existing-if", updatedIf.Id);
        Assert.Equal(ConditionMatchMode.Any, updatedIf.Settings.MatchMode);
        Assert.Equal("Success", Assert.Single(updatedIf.Settings.Conditions).PropertyPath);
        Assert.Equal(ConditionMatchMode.Any, updatedElseIf.Settings.MatchMode);

        var emptyDraft = ifDefinition.CreateDraft();
        Assert.Equal(IfStepDefinition.ConditionsFieldId,
            Assert.Single(ifDefinition.ValidateDraft(emptyDraft)).FieldId);
    }

    [Fact]
    public void GeneratedConditionEditor_UsesSharedConditionRowsAndCreatesIfStep()
    {
        var resultType = StepResultMetadata.ResultTypes.First(type => type.Properties.Any());
        var sources = new[] { new SourceStepItem("source", "Source", resultType) };
        GeneratedConditionEditorViewModel? conditionEditor = null;
        var editor = new GeneratedStepEditorViewModel(
            new IfStepDefinition(),
            conditionResolver: (_, value) => conditionEditor = new GeneratedConditionEditorViewModel(value, sources));
        var field = Assert.Single(editor.Fields);

        Assert.True(field.UsesConditionEditor);
        Assert.False(field.UsesTextInput);
        Assert.NotNull(conditionEditor);
        Assert.Single(conditionEditor.Conditions);
        conditionEditor.IsAny = true;

        Assert.True(editor.TryCreateStep(out var created));
        var ifStep = Assert.IsType<IfStep>(created);
        Assert.Equal(ConditionMatchMode.Any, ifStep.Settings.MatchMode);
        Assert.Equal("source", Assert.Single(ifStep.Settings.Conditions).SourceStepId);
    }

    [Fact]
    public void GeneratedConditionEditor_SelectingAPropertyResetsTheOperatorAndEditableValue()
    {
        var integer = new ResultPropertyDescriptor("Count", "Count", ResultValueKind.Integer);
        var text = new ResultPropertyDescriptor("Text", "Text", ResultValueKind.Text);
        var source = new SourceStepItem(
            "source", "Source", new ResultTypeDescriptor("Test", "Test", [integer, text]));
        var editor = new GeneratedConditionEditorViewModel(
            null,
            [source],
            nestedInputResolver: CreateConditionComparisonEditor);
        var row = Assert.Single(editor.Conditions);

        Assert.Equal(ConditionOperator.Equals, row.SelectedOperator);
        Assert.Equal(string.Empty, row.ComparisonField!.InputText);
        row.ComparisonField.InputText = "42";
        var changedProperties = new List<string?>();
        row.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        row.SourcePicker.Load(ResultBinding.ForStepResult(source.StepId, text.StableId));

        Assert.Equal(ConditionOperator.Equals, row.SelectedOperator);
        Assert.Contains(nameof(ConditionRowViewModel.SelectedOperator), changedProperties);
        Assert.Equal(string.Empty, row.ComparisonField!.InputText);
    }

    [Fact]
    public void GeneratedConditionEditor_UsesATrueFalseDropdownForBooleanComparisons()
    {
        var property = new ResultPropertyDescriptor("Found", "Found", ResultValueKind.Boolean);
        var source = new SourceStepItem(
            "source", "Source", new ResultTypeDescriptor("Test", "Test", [property]));
        var editor = new GeneratedConditionEditorViewModel(
            null,
            [source],
            nestedInputResolver: CreateConditionComparisonEditor);
        var field = Assert.Single(editor.Conditions).ComparisonField!;

        Assert.True(field.UsesBooleanDropdown);
        Assert.Equal([true, false], field.BooleanOptions.Select(option => option.Value));
        field.SelectedBooleanOption = field.BooleanOptions[0];
        Assert.Equal(bool.TrueString, field.InputText);
    }

    [Fact]
    public void GeneratedConditionEditor_UsesUserChoiceLabelsForEnumOptions()
    {
        var sourceStep = new UserChoiceStep
        {
            Id = "choice",
            Settings = new UserChoiceSettings
            {
                Options =
                [
                    new UserChoiceOption { Id = "mode-prod", Label = "Production" },
                    new UserChoiceOption { Id = "mode-test", Label = "Test" }
                ]
            }
        };
        var resultType = StepResultMetadata.GetResultTypeForStep(sourceStep)!;
        var source = new SourceStepItem(sourceStep.Id, "Choice", resultType);
        var editor = new GeneratedConditionEditorViewModel(
            null,
            [source],
            nestedInputResolver: CreateConditionComparisonEditor);
        var row = Assert.Single(editor.Conditions);

        row.SourcePicker.Load(ResultBinding.ForStepResult(source.StepId, "selected_option_id"));

        var field = row.ComparisonField!;
        Assert.True(field.UsesEnumPicker);
        Assert.True(field.UsesConditionEnumDirectValue);
        Assert.True(field.ShowsDirectInput);
        Assert.False(field.ShowsInputSourceSelector);
        Assert.Equal(
            [("mode-prod", "Production"), ("mode-test", "Test")],
            field.EnumOptions.Select(option => (option.Value, option.Label)));
    }

    [Fact]
    public void UserChoiceResultContracts_UseStepSpecificEnumIdentities()
    {
        var first = new UserChoiceStep
        {
            Id = "choice-a",
            Settings = new UserChoiceSettings
            {
                Options =
                [
                    new UserChoiceOption { Id = "yes", Label = "Yes" },
                    new UserChoiceOption { Id = "no", Label = "No" }
                ]
            }
        };
        var second = new UserChoiceStep
        {
            Id = "choice-b",
            Settings = new UserChoiceSettings
            {
                Options =
                [
                    new UserChoiceOption { Id = "red", Label = "Red" },
                    new UserChoiceOption { Id = "blue", Label = "Blue" }
                ]
            }
        };

        var firstProperty = StepResultMetadata.GetResultTypeForStep(first)!.Properties.Single(property =>
            property.StableId == "selected_option_id");
        var secondProperty = StepResultMetadata.GetResultTypeForStep(second)!.Properties.Single(property =>
            property.StableId == "selected_option_id");

        Assert.NotEqual(firstProperty.EnumTypeName, secondProperty.EnumTypeName);
        Assert.False(StepResultMetadata.AreComparable(firstProperty, secondProperty));
    }

    [Fact]
    public void UserChoiceOptions_CreateTheEnumContractUsedByAStoredIfCondition()
    {
        var localValues = new List<LocalValue>();
        var choiceDialog = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [],
            cameraCaptureService: new CameraDefinitionTestService(),
            localValues: localValues,
            localValueCreated: localValues.Add);
        choiceDialog.SelectedType = "UserChoice";
        var optionEditor = choiceDialog.GeneratedEditor!.Fields.Single(field =>
            field.Descriptor.Id == UserChoiceStepDefinition.OptionsFieldId).UserChoiceOptionsEditor!;
        var firstId = optionEditor.Options[0].Id;
        var secondId = optionEditor.Options[1].Id;
        optionEditor.Options[0].LabelField!.InputText = "Production";
        optionEditor.Options[0].ValueField!.InputText = "prod";
        optionEditor.Options[1].LabelField!.InputText = "Test";
        optionEditor.Options[1].ValueField!.InputText = "test";

        Assert.True(choiceDialog.GeneratedEditor.TryCreateStep(out var createdChoice),
            choiceDialog.GeneratedEditor.ValidationError);
        var choice = Assert.IsType<UserChoiceStep>(createdChoice);
        choiceDialog.CommitDraftValues(choice);

        var resultProperty = StepResultMetadata.GetResultTypeForStep(choice)!.Properties.Single(property =>
            property.StableId == "selected_option_id");
        Assert.Equal([firstId, secondId], resultProperty.EnumValues);
        Assert.Equal("Production", resultProperty.EnumDisplayNames![firstId]);
        Assert.Equal("Test", resultProperty.EnumDisplayNames[secondId]);

        var ifDialog = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [choice],
            cameraCaptureService: new CameraDefinitionTestService(),
            localValues: localValues,
            localValueCreated: localValues.Add);
        ifDialog.SelectedType = "If";
        var row = Assert.Single(Assert.Single(ifDialog.GeneratedEditor!.Fields).ConditionEditor!.Conditions);
        row.SourcePicker.Load(ResultBinding.ForStepResult(choice.Id, "selected_option_id"));
        var comparison = row.ComparisonField!;

        Assert.Equal(
            [(firstId, "Production"), (secondId, "Test")],
            comparison.EnumOptions.Select(option => (option.Value, option.Label)));
        comparison.SelectedEnumValue = secondId;
        Assert.True(ifDialog.GeneratedEditor.TryCreateStep(out var createdIf),
            ifDialog.GeneratedEditor.ValidationError);
        var ifStep = Assert.IsType<IfStep>(createdIf);
        ifDialog.CommitDraftValues(ifStep);

        var serializerOptions = new JsonSerializerOptions();
        JobJsonSerialization.Configure(serializerOptions);
        var savedJob = new Job { Steps = [choice, ifStep, new EndIfStep()], LocalValues = localValues };
        var json = JsonSerializer.Serialize(savedJob, serializerOptions);
        Assert.DoesNotContain("\"settings\"", json);
        var reopenedJob = JsonSerializer.Deserialize<Job>(json, serializerOptions)!;
        choice = Assert.IsType<UserChoiceStep>(reopenedJob.Steps[0]);
        ifStep = Assert.IsType<IfStep>(reopenedJob.Steps[1]);
        localValues = reopenedJob.LocalValues;

        var editDialog = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [choice],
            cameraCaptureService: new CameraDefinitionTestService(),
            localValues: localValues);
        Assert.True(editDialog.TryLoadGeneratedStep(ifStep));
        var loaded = Assert.Single(Assert.Single(editDialog.GeneratedEditor!.Fields).ConditionEditor!.Conditions)
            .ComparisonField!;

        Assert.Equal(
            [(firstId, "Production"), (secondId, "Test")],
            loaded.EnumOptions.Select(option => (option.Value, option.Label)));
        Assert.Equal(secondId, loaded.SelectedEnumValue);
        var job = new Job { Steps = [choice, ifStep, new EndIfStep()], LocalValues = localValues };
        var validation = JobValidation.ValidateJob(job);
        Assert.True(validation.IsValid, string.Join("; ", validation.Steps.Select(step => step.Error)));
    }

    [Fact]
    public void StoredCondition_CanChangeFromBooleanToUserChoiceAndBack()
    {
        var choice = new UserChoiceStep
        {
            Settings = new UserChoiceSettings
            {
                Options = [new() { Id = "yes", Label = "Yes" }, new() { Id = "no", Label = "No" }]
            }
        };
        var locals = new List<LocalValue>();
        AddJobStepDialogViewModel Dialog() => new(
            new ControllableJobExecutor([]), [choice],
            cameraCaptureService: new CameraDefinitionTestService(),
            localValues: locals, localValueCreated: locals.Add);
        var create = Dialog();
        create.SelectedType = "If";
        var initial = Assert.Single(Assert.Single(create.GeneratedEditor!.Fields).ConditionEditor!.Conditions);
        initial.SourcePicker.Load(ResultBinding.ForStepResult(choice.Id, "was_cancelled"));
        initial.ComparisonField!.BooleanValue = true;
        Assert.True(create.GeneratedEditor.TryCreateStep(out var step), create.GeneratedEditor.ValidationError);
        create.CommitDraftValues(step!);
        var edit = Dialog();
        Assert.True(edit.TryLoadGeneratedStep(step!));
        var row = Assert.Single(Assert.Single(edit.GeneratedEditor!.Fields).ConditionEditor!.Conditions);
        row.SourcePicker.Load(ResultBinding.ForStepResult(choice.Id, "selected_option_id"));
        row.ComparisonField!.SelectedEnumValue = choice.Settings.Options[1].Id;
        Assert.True(row.IsValid, row.ComparisonValueValidationError);
        Assert.True(edit.GeneratedEditor.TryCreateStep(out var updated), edit.GeneratedEditor.ValidationError);
        edit.CommitDraftValues(updated!);
        Assert.True(JobValidation.ValidateJob(new Job
        {
            Steps = [choice, updated!, new EndIfStep()],
            LocalValues = locals
        }).IsValid);
        row.SourcePicker.Load(ResultBinding.ForStepResult(choice.Id, "was_cancelled"));
        row.ComparisonField!.BooleanValue = true;
        Assert.True(row.IsValid, row.ComparisonValueValidationError);
    }

    [Theory]
    [InlineData("yes", true)]
    [InlineData("removed", false)]
    public void StoredLegacyTextComparison_RemainsEditableAsUserChoiceEnum(string token, bool valid)
    {
        var choice = new UserChoiceStep
        {
            Settings = new UserChoiceSettings
            {
                Options = [new() { Id = "yes", Label = "Yes" }, new() { Id = "no", Label = "No" }]
            }
        };
        var local = new LocalValue { ValueKind = ResultValueKind.Text, Value = JsonValue.Create(token) };
        var condition = new StepCondition
        {
            SourceStepId = choice.Id,
            PropertyId = "selected_option_id",
            Operator = ConditionOperator.Equals,
            Comparison = new ComparisonOperand
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.LocalValue,
                SourceId = local.Id.ToString("D")
            }
        };
        var step = new IfStep { Settings = new IfConditionSettings { Conditions = [condition] } };
        var dialog = new AddJobStepDialogViewModel(new ControllableJobExecutor([]), [choice],
            cameraCaptureService: new CameraDefinitionTestService(), localValues: [local]);
        Assert.True(dialog.TryLoadGeneratedStep(step));
        var row = Assert.Single(Assert.Single(dialog.GeneratedEditor!.Fields).ConditionEditor!.Conditions);
        Assert.Equal(token, row.ComparisonField!.InputText);
        Assert.Equal(valid, row.IsValid);
        row.ComparisonField.SelectedEnumValue = "no";
        Assert.True(row.IsValid);
        Assert.Equal(token, local.Value!.GetValue<string>());
        Assert.True(dialog.GeneratedEditor.TryCreateStep(out var updated), dialog.GeneratedEditor.ValidationError);
        dialog.CommitDraftValues(updated!);
        Assert.Equal("no", local.Value!.GetValue<string>());
    }

    [Fact]
    public void OpeningLegacyStep_PreservesNonDefaultScalarAndEnumSettings()
    {
        var step = new PointComparisonStep();
        step.Settings.Mode = PointComparisonMode.Expression;
        step.Settings.OffsetSettings.OffsetX = 123;
        var dialog = new AddJobStepDialogViewModel(new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService());
        Assert.True(dialog.TryLoadGeneratedStep(step));
        Assert.Equal("Expression", dialog.GeneratedEditor!.Fields.Single(field =>
            field.Descriptor.Id == PointComparisonStepDefinition.ModeFieldId).SelectedEnumValue);
        Assert.Equal(123, dialog.GeneratedEditor.Fields.Single(field =>
            field.Descriptor.Id == PointComparisonStepDefinition.OffsetXFieldId).IntegerValue);
    }

    [Fact]
    public void AddStepDialog_EnumComparisonOffersOnlyTheDirectValueSource()
    {
        var sourceStep = new UserChoiceStep
        {
            Id = "choice",
            Settings = new UserChoiceSettings
            {
                Options =
                [
                    new UserChoiceOption { Id = "mode-prod", Label = "Production" },
                    new UserChoiceOption { Id = "mode-test", Label = "Test" }
                ]
            }
        };
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [sourceStep],
            cameraCaptureService: new CameraDefinitionTestService());
        viewModel.SelectedType = "If";
        var conditions = Assert.Single(viewModel.GeneratedEditor!.Fields).ConditionEditor!;
        var row = Assert.Single(conditions.Conditions);

        row.SourcePicker.Load(ResultBinding.ForStepResult(sourceStep.Id, "selected_option_id"));

        var field = row.ComparisonField!;
        Assert.True(field.InputReferenceEditor!.Picker.CanUseDirectValue);
        Assert.False(field.InputReferenceEditor.Picker.CanUseJobVariables);
        Assert.False(field.InputReferenceEditor.Picker.CanUseStepResults);
        Assert.False(field.InputReferenceEditor.Picker.CanUseSecrets);
        Assert.False(field.ShowsInputSourceSelector);
    }

    [Fact]
    public void GeneratedConditionEditor_LoadsUserChoiceEnumOptionsForStoredCondition()
    {
        var sourceStep = new UserChoiceStep
        {
            Id = "choice",
            Settings = new UserChoiceSettings
            {
                Options =
                [
                    new UserChoiceOption { Id = "mode-prod", Label = "Production" },
                    new UserChoiceOption { Id = "mode-test", Label = "Test" }
                ]
            }
        };
        var source = new SourceStepItem(
            sourceStep.Id, "Choice", StepResultMetadata.GetResultTypeForStep(sourceStep)!);
        var settings = new IfConditionSettings
        {
            Conditions =
            [
                new StepCondition
                {
                    SourceStepId = source.StepId,
                    PropertyId = "selected_option_id",
                    PropertyPath = nameof(UserChoiceResult.SelectedOptionId),
                    Operator = ConditionOperator.Equals,
                    Comparison = new ComparisonOperand { Value = "mode-test" }
                }
            ]
        };

        var editor = new GeneratedConditionEditorViewModel(
            JsonSerializer.SerializeToNode(settings),
            [source],
            nestedInputResolver: CreateConditionComparisonEditor);
        var field = Assert.Single(editor.Conditions).ComparisonField!;

        Assert.Equal(
            [("mode-prod", "Production"), ("mode-test", "Test")],
            field.EnumOptions.Select(option => (option.Value, option.Label)));
        Assert.Equal("mode-test", field.SelectedEnumOption?.Value);
    }

    [Fact]
    public void GeneratedConditionEditor_UsesEnumMetadataFromJobVariable()
    {
        var variable = new JobVariable
        {
            Name = "Mode",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("mode-test"),
            EnumTypeName = "workflow.mode",
            EnumValues = ["mode-prod", "mode-test"],
            EnumDisplayNames = new Dictionary<string, string>
            {
                ["mode-prod"] = "Production",
                ["mode-test"] = "Test"
            }
        };

        var editor = new GeneratedConditionEditorViewModel(
            null,
            [],
            [variable],
            nestedInputResolver: CreateConditionComparisonEditor);
        var row = Assert.Single(editor.Conditions);

        Assert.Equal("workflow.mode", row.SelectedProperty?.EnumTypeName);
        Assert.Equal(
            [("mode-prod", "Production"), ("mode-test", "Test")],
            row.ComparisonField!.EnumOptions.Select(option => (option.Value, option.Label)));
        Assert.Null(row.ComparisonField.SelectedEnumValue);
        Assert.False(row.IsValid);

        row.ComparisonField.SelectedEnumValue = "mode-test";

        Assert.True(row.IsValid, row.ComparisonValueValidationError);
    }

    [Fact]
    public void GeneratedEnumField_PreservesUnknownStoredValueUntilUserChangesIt()
    {
        var descriptor = new StepFieldDescriptor(
            "mode", "Mode", StepValueKind.Enum, Required: true,
            DefaultValue: JsonValue.Create("known"),
            Constraints: new StepFieldConstraints(AllowedValues: ["known", "other"]),
            Options:
            [
                new StepFieldOptionDescriptor("known", "Known", "Known"),
                new StepFieldOptionDescriptor("other", "Other", "Other")
            ]);
        var stored = new LocalValue
        {
            Name = "Mode",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("removed")
        };
        var picker = new ValueReferencePickerViewModel(
            [], StepInputContractRegistry.ForField(descriptor), false, [stored]);
        var input = new GeneratedResultBindingEditorViewModel(
            JsonSerializer.SerializeToNode(new ResultBinding
            {
                ProviderId = ValueProviderIds.LocalValue,
                SourceId = stored.Id.ToString("D")
            }),
            picker);

        var field = new GeneratedStepFieldViewModel(
            descriptor, JsonValue.Create("removed"), inputReferenceEditor: input);

        Assert.Equal("removed", field.InputText);
        Assert.Null(field.SelectedEnumValue);
        Assert.Null(field.SelectedEnumOption);
        Assert.True(field.HasInvalidEnumValue);
        Assert.DoesNotContain(field.EnumOptions, option => option.Value == "removed");
        Assert.Equal("removed", stored.Value!.GetValue<string>());
        Assert.False(field.TryWriteValue(new StepDraft("test"), out var error));
        Assert.Contains("removed", error);

        field.SelectedEnumValue = "other";

        Assert.False(field.HasInvalidEnumValue);
        Assert.Equal("other", stored.Value!.GetValue<string>());
    }

    [Fact]
    public void ConditionEnumField_RepairsLegacyExternalBindingToDirectValue()
    {
        var descriptor = new StepFieldDescriptor(
            "comparison", "Ui.Common.Value", StepValueKind.Enum, Required: true,
            EditorHint: GeneratedStepFieldViewModel.ConditionEnumDirectValueEditorHint,
            Options:
            [
                new StepFieldOptionDescriptor("known", "Ui.Common.Value", "Known"),
                new StepFieldOptionDescriptor("other", "Ui.Common.Value", "Other")
            ]);
        var legacy = new JobVariable
        {
            Name = "Legacy mode",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("known")
        };
        var picker = new ValueReferencePickerViewModel(
            [], StepInputContractRegistry.ForField(descriptor), false, [legacy],
            context: new ValueReferencePickerContext(
                "If", "Comparison", null, null, null,
                () => new LocalValue
                {
                    Name = "Comparison",
                    ValueKind = ResultValueKind.Enum,
                    Value = JsonValue.Create("known")
                }));
        picker.Load(new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = legacy.Id.ToString("D")
        });
        var field = new GeneratedStepFieldViewModel(
            descriptor,
            JsonValue.Create("known"),
            inputReferenceEditor: new GeneratedResultBindingEditorViewModel(null, picker));

        Assert.True(picker.IsStepValue);
        Assert.True(picker.CanUseDirectValue);
        Assert.False(field.ShowsInputSourceSelector);
    }

    [Fact]
    public void ValueProviderDescriptor_PreservesEnumSchemaAndSeparatesDifferentEnumTypes()
    {
        var first = new JobVariable
        {
            Name = "First",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("Active"),
            EnumTypeName = "workflow.state",
            EnumValues = ["Active", "Inactive"]
        };
        var second = new JobVariable
        {
            Name = "Second",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("Active"),
            EnumTypeName = "window.state",
            EnumValues = ["Active", "Inactive"]
        };

        var firstProperty = ValueProviderSourceDescriptor.FromVariable(first).ToResultProperty();
        var secondProperty = ValueProviderSourceDescriptor.FromVariable(second).ToResultProperty();

        Assert.Equal(["Active", "Inactive"], firstProperty.EnumValues);
        Assert.False(StepResultMetadata.AreComparable(firstProperty, secondProperty));
    }

    [Fact]
    public void ValueProviderDescriptor_DoesNotInventAnEnumSchemaFromTheCurrentValue()
    {
        var variable = new LocalValue
        {
            Name = "Legacy generated enum",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("only-current-value"),
            EnumTypeName = "generated.choice"
        };

        var descriptor = ValueProviderSourceDescriptor.FromVariable(variable);

        Assert.Null(descriptor.EnumValues);
        Assert.Null(descriptor.ToResultProperty().EnumValues);
    }

    [Fact]
    public void GeneratedConditionEditor_RejectsComparisonReferenceWithDifferentEnumType()
    {
        var actual = new JobVariable
        {
            Name = "Workflow state",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("Active"),
            EnumTypeName = "workflow.state",
            EnumValues = ["Active", "Inactive"]
        };
        var expected = new JobVariable
        {
            Name = "Window state",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create("Active"),
            EnumTypeName = "window.state",
            EnumValues = ["Active", "Inactive"]
        };
        var editor = new GeneratedConditionEditorViewModel(
            null,
            [],
            [actual],
            nestedInputResolver: (key, kind, literal, _, _) =>
            {
                var descriptor = new StepFieldDescriptor(key, string.Empty, kind, DefaultValue: literal);
                var picker = new ValueReferencePickerViewModel(
                    [], StepInputContractRegistry.ForField(descriptor), false, [expected]);
                return new GeneratedResultBindingEditorViewModel(
                    JsonSerializer.SerializeToNode(new ResultBinding
                    {
                        ProviderId = ValueProviderIds.JobVariable,
                        SourceId = expected.Id.ToString("D")
                    }),
                    picker);
            });
        var row = Assert.Single(editor.Conditions);

        Assert.False(row.IsComparisonValueValid);
        Assert.False(row.IsValid);
    }

    private static GeneratedResultBindingEditorViewModel CreateConditionComparisonEditor(
        string key,
        StepValueKind kind,
        JsonNode? literal,
        ResultPropertyDescriptor? enumProperty,
        ResultBinding? binding)
    {
        var value = new LocalValue
        {
            Name = key,
            ValueKind = JobVariableInputMigration.MapKind(kind),
            Value = literal?.DeepClone(),
            EnumTypeName = enumProperty?.EnumTypeName,
            EnumValues = enumProperty?.EnumValues?.ToList(),
            EnumDisplayNames = enumProperty?.EnumDisplayNames is null
                ? null
                : new Dictionary<string, string>(enumProperty.EnumDisplayNames, StringComparer.Ordinal)
        };
        var descriptor = new StepFieldDescriptor(key, string.Empty, kind, DefaultValue: literal);
        var picker = new ValueReferencePickerViewModel(
            [], StepInputContractRegistry.ForField(descriptor), false, [value]);
        return new GeneratedResultBindingEditorViewModel(
            JsonSerializer.SerializeToNode(new ResultBinding
            {
                ProviderId = ValueProviderIds.LocalValue,
                SourceId = value.Id.ToString("D")
            }),
            picker);
    }

    [Fact]
    public void GeneratedConditionEditor_UsesJobVariablesAsValueReferences()
    {
        var variable = new JobVariable
        {
            Name = "Enabled",
            ValueKind = ResultValueKind.Boolean,
            Value = System.Text.Json.Nodes.JsonValue.Create(true)
        };
        GeneratedConditionEditorViewModel? conditionEditor = null;
        var editor = new GeneratedStepEditorViewModel(
            new IfStepDefinition(),
            conditionResolver: (_, value) => conditionEditor =
                new GeneratedConditionEditorViewModel(value, [], [variable]));

        var row = Assert.Single(conditionEditor!.Conditions);
        Assert.True(row.SourcePicker.CanUseJobVariables);
        Assert.True(row.SourcePicker.CanUseStepResults);
        Assert.True(row.SourceField.ShowsInputSourceSelector);
        Assert.Equal(ValueProviderIds.JobVariable, row.SourcePicker.ToBinding().ProviderId);
        row.ComparisonIsJobResult = true;
        var comparisonVariable = Assert.Single(Assert.Single(row.ComparisonSelectionTree).Children);
        comparisonVariable.SelectCommand!.Execute(null);

        Assert.True(editor.TryCreateStep(out var created));
        var condition = Assert.Single(Assert.IsType<IfStep>(created).Settings.Conditions);
        Assert.Equal(ValueProviderIds.JobVariable, condition.ProviderId);
        Assert.Equal(variable.Id.ToString("D"), condition.SourceId);
        Assert.True(string.IsNullOrEmpty(condition.SourceStepId));
    }

    [Fact]
    public void GeneratedConditionEditor_PersistsSelectedCompoundVariableMember()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Target",
            ValueKind = ResultValueKind.Point,
            Value = new JsonObject { ["x"] = 10, ["y"] = 20 }
        };
        GeneratedConditionEditorViewModel? conditionEditor = null;
        var editor = new GeneratedStepEditorViewModel(
            new IfStepDefinition(),
            conditionResolver: (_, value) => conditionEditor =
                new GeneratedConditionEditorViewModel(value, [], [variable]));
        var row = Assert.Single(conditionEditor!.Conditions);
        row.SourcePicker.UseJobVariableCommand.Execute(null);
        var xNode = row.SourcePicker.SelectionTree.Single(node => node.DisplayName == variable.Name)
            .Children.Single(node => node.DisplayName == "X");

        xNode.SelectCommand!.Execute(null);
        row.ComparisonNumber = 10;

        Assert.True(editor.TryCreateStep(out var created));
        var condition = Assert.Single(Assert.IsType<IfStep>(created).Settings.Conditions);
        Assert.Equal(variable.Id.ToString("D"), condition.SourceId);
        Assert.Equal("X", condition.ValuePath);
    }

    [Fact]
    public void GeneratedConditionEditor_RejectsAnInvalidComparisonBeforeCreatingTheStep()
    {
        var numberProperty = StepResultMetadata.ResultTypes
            .SelectMany(type => type.Properties)
            .First(property => property.DataType == ResultValueKind.Number
                               && property.Cardinality == ResultCardinality.Single);
        var source = new SourceStepItem(
            "source", "Source", new ResultTypeDescriptor("Test", "Test", [numberProperty]));
        GeneratedConditionEditorViewModel? conditionEditor = null;
        var editor = new GeneratedStepEditorViewModel(
            new IfStepDefinition(),
            conditionResolver: (_, value) => conditionEditor =
                new GeneratedConditionEditorViewModel(value, [source]));
        var row = Assert.Single(conditionEditor!.Conditions);
        row.SelectedOperator = ConditionOperator.Equals;
        row.ComparisonNumber = null;

        Assert.False(row.IsValid);
        Assert.False(editor.TryCreateStep(out var created));
        Assert.Null(created);
        Assert.NotEmpty(editor.ValidationError);
    }

    [Fact]
    public void GeneratedConditionEditor_AcceptsLocalizedDecimalInput()
    {
        var property = StepResultMetadata.ResultTypes
            .SelectMany(type => type.Properties)
            .First(candidate => candidate.DataType == ResultValueKind.Number
                                && candidate.Cardinality != ResultCardinality.Collection);
        var source = new SourceStepItem(
            "source", "Source", new ResultTypeDescriptor("Test", "Test", [property]));
        var storedValue = new LocalValue
        {
            Name = "Comparison",
            ValueKind = ResultValueKind.Number,
            Value = JsonValue.Create(0d)
        };
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            var editor = new GeneratedConditionEditorViewModel(
                null,
                [source],
                nestedInputResolver: (_, kind, literal, _, _) =>
                {
                    var descriptor = new StepFieldDescriptor(
                        "comparison", string.Empty, kind, DefaultValue: literal);
                    var picker = new ValueReferencePickerViewModel(
                        [], StepInputContractRegistry.ForField(descriptor), false, [storedValue]);
                    return new GeneratedResultBindingEditorViewModel(
                        JsonSerializer.SerializeToNode(new ResultBinding
                        {
                            ProviderId = ValueProviderIds.LocalValue,
                            SourceId = storedValue.Id.ToString("D")
                        }),
                        picker);
                });
            var row = Assert.Single(editor.Conditions);
            row.SelectedOperator = ConditionOperator.Equals;

            row.ComparisonField!.NumberValue = 1.25;

            Assert.Equal("1,25", row.ComparisonField.InputText);
            Assert.True(row.IsValid, row.ComparisonValueValidationError);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void GeneratedConditionEditor_AcceptsLoadedZeroFromLocalComparisonValue()
    {
        var property = StepResultMetadata.ResultTypes
            .SelectMany(type => type.Properties)
            .First(candidate => candidate.DataType == ResultValueKind.Integer
                                && candidate.Cardinality != ResultCardinality.Collection);
        var source = new SourceStepItem(
            "source", "Source", new ResultTypeDescriptor("Test", "Test", [property]));
        var storedValue = new LocalValue
        {
            Name = "Comparison",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(0)
        };
        var settings = new IfConditionSettings
        {
            Conditions =
            [
                new StepCondition
                {
                    ProviderId = ValueProviderIds.StepResult,
                    SourceId = StepResultSourceIdCodec.Create("source", property.StableId),
                    Operator = ConditionOperator.Equals,
                    Comparison = new ComparisonOperand
                    {
                        Kind = ComparisonOperandKind.JobResult,
                        ProviderId = ValueProviderIds.LocalValue,
                        SourceId = storedValue.Id.ToString("D")
                    }
                }
            ]
        };

        var editor = new GeneratedConditionEditorViewModel(
            JsonSerializer.SerializeToNode(settings),
            [source],
            nestedInputResolver: (_, kind, literal, _, _) =>
            {
                var descriptor = new StepFieldDescriptor(
                    "comparison", string.Empty, kind, DefaultValue: literal);
                var picker = new ValueReferencePickerViewModel(
                    [], StepInputContractRegistry.ForField(descriptor), false, [storedValue]);
                return new GeneratedResultBindingEditorViewModel(
                    JsonSerializer.SerializeToNode(settings.Conditions[0].Comparison), picker);
            });

        var row = Assert.Single(editor.Conditions);
        Assert.Equal("0", row.ComparisonField!.InputText);
        Assert.True(row.IsValid, row.ComparisonValueValidationError);
    }

    [Fact]
    public void GeneratedConditionEditor_PreservesIsEmptyOperatorWhenEditing()
    {
        var property = StepResultMetadata.ResultTypes
            .SelectMany(type => type.Properties)
            .First(candidate => candidate.DataType == ResultValueKind.Text
                                && candidate.Cardinality != ResultCardinality.Collection);
        var source = new SourceStepItem(
            "source", "Source", new ResultTypeDescriptor("Test", "Test", [property]));
        var editor = new GeneratedConditionEditorViewModel(
            JsonSerializer.SerializeToNode(new IfConditionSettings
            {
                Conditions =
                [
                    new StepCondition
                    {
                        SourceStepId = source.StepId,
                        PropertyId = property.StableId,
                        PropertyPath = property.Name,
                        Operator = ConditionOperator.IsEmpty
                    }
                ]
            }),
            [source]);

        var saved = Assert.Single(editor.ToValue().Conditions);

        Assert.Equal(ConditionOperator.IsEmpty, saved.Operator);
        Assert.Null(saved.Comparison);
    }

    [Fact]
    public void GeneratedConditionEditor_RaisesOneSemanticChangeForSourceSelection()
    {
        var resultType = StepResultMetadata.ResultTypes.First(type => type.Properties.Any());
        var first = new SourceStepItem("first", "First", resultType);
        var second = new SourceStepItem("second", "Second", resultType);
        var editor = new GeneratedConditionEditorViewModel(null, [first, second]);
        var row = Assert.Single(editor.Conditions);
        var property = resultType.Properties.First(candidate =>
            candidate.Cardinality != ResultCardinality.Collection
            && ConditionRules.GetOperators(candidate.DataType).Count > 0);
        var changes = 0;
        editor.Changed += () => changes++;

        row.SourcePicker.Load(ResultBinding.ForStepResult(second.StepId, property.StableId));

        Assert.Equal(1, changes);
    }

    [Fact]
    public void GeneratedConditionEditor_RaisesOneSemanticChangeForComparisonSelection()
    {
        var property = new ResultPropertyDescriptor("Flag", "Flag", ResultValueKind.Boolean);
        var source = new SourceStepItem(
            "source", "Source", new ResultTypeDescriptor("Test", "Test", [property]));
        var first = new LocalValue
        {
            Name = "First",
            ValueKind = ResultValueKind.Boolean,
            Value = JsonValue.Create(false)
        };
        var second = new LocalValue
        {
            Name = "Second",
            ValueKind = ResultValueKind.Boolean,
            Value = JsonValue.Create(true)
        };
        var editor = new GeneratedConditionEditorViewModel(
            null,
            [source],
            nestedInputResolver: (_, kind, literal, _, _) =>
            {
                var descriptor = new StepFieldDescriptor(
                    "comparison", string.Empty, kind, DefaultValue: literal);
                var picker = new ValueReferencePickerViewModel(
                    [], StepInputContractRegistry.ForField(descriptor), false, [first, second]);
                return new GeneratedResultBindingEditorViewModel(
                    JsonSerializer.SerializeToNode(new ResultBinding
                    {
                        ProviderId = ValueProviderIds.LocalValue,
                        SourceId = first.Id.ToString("D")
                    }),
                    picker);
            });
        var row = Assert.Single(editor.Conditions);
        var changes = 0;
        editor.Changed += () => changes++;

        row.ComparisonField!.InputReferenceEditor!.Picker.Load(new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = second.Id.ToString("D")
        });

        Assert.Equal(1, changes);
        Assert.Equal(second.Id.ToString("D"), editor.ToValue().Conditions[0].EffectiveComparison.SourceId);
    }

    [Fact]
    public void GeneratedConditionEditor_SynchronizesTheStoredDirectValueWhenSaving()
    {
        var sourceStep = new TemplateMatchingStep { Id = "source" };
        var source = new SourceStepItem(
            sourceStep.Id,
            "Source",
            StepResultMetadata.GetResultTypeForStep(sourceStep)!);
        var storedValue = new LocalValue
        {
            Name = "If conditions",
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonSerializer.SerializeToNode(new IfConditionSettings())
        };
        var comparisonValue = new LocalValue
        {
            Name = "Comparison value",
            ValueKind = ResultValueKind.Boolean,
            Value = JsonValue.Create(false)
        };
        GeneratedConditionEditorViewModel? conditionEditor = null;
        var editor = new GeneratedStepEditorViewModel(
            new IfStepDefinition(),
            conditionResolver: (_, value) => conditionEditor =
                new GeneratedConditionEditorViewModel(
                    value,
                    [source],
                    nestedInputResolver: (_, kind, literal, _, _) =>
                    {
                        comparisonValue.Value = literal?.DeepClone() ?? JsonValue.Create(false);
                        var descriptor = new StepFieldDescriptor(
                            "comparison", string.Empty, kind, DefaultValue: literal);
                        var picker = new ValueReferencePickerViewModel(
                            [], StepInputContractRegistry.ForField(descriptor), false, [comparisonValue]);
                        return new GeneratedResultBindingEditorViewModel(
                            JsonSerializer.SerializeToNode(new ResultBinding
                            {
                                ProviderId = ValueProviderIds.LocalValue,
                                SourceId = comparisonValue.Id.ToString("D")
                            }),
                            picker);
                    }),
            inputReferenceResolver: (field, _) =>
            {
                var binding = new ResultBinding
                {
                    ProviderId = ValueProviderIds.LocalValue,
                    SourceId = storedValue.Id.ToString("D")
                };
                var picker = new ValueReferencePickerViewModel(
                    [], StepInputContractRegistry.ForField(field), false, [storedValue]);
                return new GeneratedResultBindingEditorViewModel(
                    JsonSerializer.SerializeToNode(binding), picker);
            });

        Assert.Single(conditionEditor!.Conditions);
        Assert.True(editor.TryCreateStep(out var created), editor.ValidationError);
        Assert.IsType<IfStep>(created);
        var persisted = storedValue.Value!.Deserialize<IfConditionSettings>();
        Assert.NotNull(persisted);
        Assert.Single(persisted.Conditions);
        Assert.Equal("source", persisted.Conditions[0].SourceStepId);
        Assert.Equal(comparisonValue.Id.ToString("D"),
            persisted.Conditions[0].EffectiveComparison.SourceId);
        var validation = JobValidation.ValidateCandidate(
            [sourceStep], created, [sourceStep, created!], [storedValue, comparisonValue]);
        Assert.True(validation.IsValid, validation.Error);
    }

    [Fact]
    public void WindowsCapabilityDefinitions_RoundTripParametersAndValidateCapabilityMode()
    {
        var queryDefinition = new WindowsStateQueryStepDefinition();
        var settingDefinition = new WindowsSettingChangeStepDefinition();
        var query = new WindowsStateQueryStep
        {
            Settings = new WindowsStateQuerySettings
            {
                QueryType = "filesystem.path",
                Parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PATH"] = @"C:\Temp"
                }
            }
        };
        var setting = new WindowsSettingChangeStep
        {
            Settings = new WindowsSettingChangeSettings
            {
                SettingId = "audio.master_volume",
                Parameters = new Dictionary<string, string?> { ["value"] = "75" }
            }
        };

        var updatedQuery = Assert.IsType<WindowsStateQueryStep>(
            queryDefinition.ApplyDraft(queryDefinition.CreateDraft(query), query));
        var updatedSetting = Assert.IsType<WindowsSettingChangeStep>(
            settingDefinition.ApplyDraft(settingDefinition.CreateDraft(setting), setting));

        Assert.Empty(queryDefinition.ValidateDraft(queryDefinition.CreateDraft(updatedQuery)));
        Assert.Equal(@"C:\Temp", updatedQuery.Settings.Parameters["path"]);
        Assert.Empty(settingDefinition.ValidateDraft(settingDefinition.CreateDraft(updatedSetting)));
        Assert.Equal("75", updatedSetting.Settings.Parameters["value"]);

        var wrongMode = queryDefinition.CreateDraft();
        wrongMode.Values[WindowsStateQueryStepDefinition.CapabilityFieldId] = JsonSerializer.SerializeToNode(
            new StepWindowsCapabilitySelectionValue("audio.master_volume",
                new Dictionary<string, string?> { ["value"] = "50" }));
        Assert.Equal(WindowsStateQueryStepDefinition.CapabilityFieldId,
            Assert.Single(queryDefinition.ValidateDraft(wrongMode)).FieldId);
    }

    [Fact]
    public void GeneratedWindowsCapabilityPicker_CreatesSettingThroughGenericAdapter()
    {
        GeneratedWindowsCapabilityEditorViewModel? capabilityEditor = null;
        var editor = new GeneratedStepEditorViewModel(
            new WindowsSettingChangeStepDefinition(),
            windowsCapabilityResolver: (field, value) => capabilityEditor = new(
                value,
                field.WindowsCapabilityPickerOptions!.Mode));
        var field = Assert.Single(editor.Fields);

        Assert.True(field.UsesWindowsCapabilityPicker);
        Assert.False(field.UsesTextInput);
        Assert.NotNull(capabilityEditor);
        Assert.Equal("audio.master_volume", capabilityEditor.Picker.SelectedCapability?.Id);

        Assert.True(editor.TryCreateStep(out var created));
        var step = Assert.IsType<WindowsSettingChangeStep>(created);
        Assert.Equal("50", step.Settings.Parameters["value"]);
    }

    [Fact]
    public void TemplateMatchingDefinition_PreservesStaticDynamicRoiAndHiddenCompatibilitySetting()
    {
        var definition = new TemplateMatchingStepDefinition();
        var existing = new TemplateMatchingStep
        {
            Id = "template-step",
            Settings = new TemplateMatchingSettings
            {
                TemplatePath = @"C:\images\button.png",
                TemplateMatchMode = TemplateMatchModes.SqDiffNormed,
                MultiplePoints = true,
                ConfidenceThreshold = 0.72,
                EnableROI = true,
                ROI = new TaskAutomation.Contracts.Geometry.PixelRegion(10, 20, 300, 200),
                ImageSource = new ResultBinding { SourceStepId = "capture", PropertyId = "image" },
                DynamicRoiSource = new ResultBinding { SourceStepId = "dynamic", PropertyId = "bounds" }
            }
        };

        var updated = Assert.IsType<TemplateMatchingStep>(
            definition.ApplyDraft(definition.CreateDraft(existing), existing));

        Assert.Same(existing, updated);
        Assert.Equal("template-step", updated.Id);
        Assert.True(updated.Settings.MultiplePoints);
        Assert.Equal(TemplateMatchModes.SqDiffNormed, updated.Settings.TemplateMatchMode);
        Assert.Equal(new TaskAutomation.Contracts.Geometry.PixelRegion(10, 20, 300, 200), updated.Settings.ROI);
        Assert.Equal("dynamic", updated.Settings.DynamicRoiSource.SourceStepId);
    }

    [Fact]
    public void ColorDetectionDefinition_UsesSharedRoiPickerAndRejectsInvertedSizeRange()
    {
        var definition = new ColorDetectionStepDefinition();
        var roiField = Assert.Single(definition.Descriptor.Fields,
            field => field.EditorHint == StepEditorHints.RoiPicker);
        var draft = definition.CreateDraft();
        draft.Values[ImageDetectionStepDefinitionSupport.ImageSourceFieldId] = JsonSerializer.SerializeToNode(
            new ResultBinding { SourceStepId = "capture", PropertyId = "image" });
        draft.Values[ColorDetectionStepDefinition.MinSizeFieldId] = JsonValue.Create(100);
        draft.Values[ColorDetectionStepDefinition.MaxSizeFieldId] = JsonValue.Create(50);

        var issue = Assert.Single(definition.ValidateDraft(draft));

        Assert.Equal("dynamicRoi", roiField.RoiPickerOptions?.DynamicInputContractId);
        Assert.Equal("StepValidation.Invalid", issue.Code);
        Assert.Equal(ColorDetectionStepDefinition.MaxSizeFieldId, issue.FieldId);
    }

    [Fact]
    public void YoloDefinition_RoundTripsSelectionConfidenceAndDynamicRoi()
    {
        var definition = new YoloDetectionStepDefinition();
        var existing = new YOLODetectionStep
        {
            Settings = new YOLODetectionStepSettings
            {
                Model = "screen-parser",
                ClassName = "button",
                ConfidenceThreshold = 0.63f,
                EnableROI = true,
                ROI = new TaskAutomation.Contracts.Geometry.PixelRegion(4, 5, 600, 400),
                ImageSource = new ResultBinding { SourceStepId = "capture", PropertyId = "image" },
                DynamicRoiSource = new ResultBinding { SourceStepId = "dynamic", PropertyId = "bounds" }
            }
        };

        var updated = Assert.IsType<YOLODetectionStep>(
            definition.ApplyDraft(definition.CreateDraft(existing), existing));

        Assert.Same(existing, updated);
        Assert.Equal("screen-parser", updated.Settings.Model);
        Assert.Equal("button", updated.Settings.ClassName);
        Assert.Equal(0.63f, updated.Settings.ConfidenceThreshold);
        Assert.Equal(new TaskAutomation.Contracts.Geometry.PixelRegion(4, 5, 600, 400), updated.Settings.ROI);
        Assert.Equal("dynamic", updated.Settings.DynamicRoiSource.SourceStepId);
    }

    [Fact]
    public async Task GeneratedYoloPicker_LoadsDependentClassesAndPublishesRecommendedConfidence()
    {
        var value = JsonSerializer.SerializeToNode(new StepYoloSelectionValue("model-a", "class-a"));
        var editor = new GeneratedYoloEditorViewModel(
            value,
            () => ["model-a", "model-b"],
            model => model == "model-b" ? ["class-b"] : ["class-a"],
            model => model == "model-b" ? 0.67 : 0.5);
        double? recommended = null;
        editor.RecommendedConfidenceChanged += value => recommended = value;

        await editor.Initialization;
        editor.Model = "model-b";
        await editor.ClassLoading;

        Assert.Equal(0.67, recommended);
        Assert.Contains("class-b", editor.Classes);
        Assert.Equal("model-b", editor.ToValue().Model);
    }

    [Fact]
    public void KeyPointDefinition_RejectsZeroRatioWithFieldSpecificIssue()
    {
        var templatePath = Path.GetTempFileName();
        try
        {
            var definition = new KeyPointMatchingStepDefinition();
            var draft = definition.CreateDraft();
            draft.Values[ImageDetectionStepDefinitionSupport.ImageSourceFieldId] = JsonSerializer.SerializeToNode(
                new ResultBinding { SourceStepId = "capture", PropertyId = "image" });
            draft.Values[KeyPointMatchingStepDefinition.TemplatePathFieldId] = JsonValue.Create(templatePath);
            draft.Values[KeyPointMatchingStepDefinition.RatioFieldId] = JsonValue.Create(0d);

            var issue = Assert.Single(definition.ValidateDraft(draft));

            Assert.Equal("StepValidation.Minimum", issue.Code);
            Assert.Equal(KeyPointMatchingStepDefinition.RatioFieldId, issue.FieldId);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Fact]
    public void VideoCreationDefinition_MigratesLegacyDetectionBindingAndPreservesSettings()
    {
        var definition = new VideoCreationStepDefinition();
        var existing = new VideoCreationStep
        {
            Settings = new VideoCreationSettings
            {
                SavePath = Path.GetTempPath(),
                FileName = "capture.mp4",
                ImageSource = Binding("capture", "image"),
                DetectionsSource = Binding("detection", "detections")
            }
        };

        var draft = definition.CreateDraft(existing);
        var updated = Assert.IsType<VideoCreationStep>(definition.ApplyDraft(draft));

        Assert.Empty(definition.ValidateDraft(draft));
        Assert.Equal(existing.Settings.SavePath, updated.Settings.SavePath);
        Assert.Equal("capture.mp4", updated.Settings.FileName);
        Assert.Equal("capture", updated.Settings.ImageSource.SourceStepId);
        Assert.Equal("detection", Assert.Single(updated.Settings.Overlay.DetectionResults).SourceStepId);
        Assert.False(updated.Settings.DetectionsSource.IsConfigured);
    }

    [Fact]
    public void SaveImageDefinition_ValidatesImageExtensionAndUsesReusableDirectoryPicker()
    {
        var definition = new SaveImageStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values["save_path"] = JsonValue.Create(Path.GetTempPath());
        draft.Values["image_source"] = JsonSerializer.SerializeToNode(Binding("capture", "image"));
        draft.Values["file_name"] = JsonValue.Create("capture.mp4");

        Assert.Equal("file_name", Assert.Single(definition.ValidateDraft(draft)).FieldId);

        draft.Values["file_name"] = JsonValue.Create("capture.png");
        Assert.Empty(definition.ValidateDraft(draft));
        var directory = definition.Descriptor.Fields.Single(field => field.Id == "save_path");
        Assert.Equal(StepEditorHints.DirectoryPicker, directory.EditorHint);
        Assert.Equal(StepKnownDirectory.Pictures, directory.DirectoryPickerOptions?.SuggestedDirectory);
        Assert.Equal("DesktopAutomation", directory.DirectoryPickerOptions?.SuggestedSubfolder);
    }

    [Fact]
    public void GeneratedDirectoryPicker_UsesPortableSuggestedLocation()
    {
        var descriptor = new VideoCreationStepDefinition().Descriptor.Fields
            .Single(field => field.Id == "save_path");

        var field = new GeneratedStepFieldViewModel(descriptor, JsonValue.Create(string.Empty));

        Assert.True(field.UsesDirectoryPicker);
        Assert.False(field.UsesTextInput);
        Assert.EndsWith("DesktopAutomation", field.InputText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JobValidation_UsesDefinitionConstraintsForMigratedSteps()
    {
        var invalid = new BlockInputStep
        {
            Settings = new BlockInputSettings { SafetyTimeoutSeconds = 3601 }
        };
        var valid = new BlockInputStep
        {
            Settings = new BlockInputSettings { SafetyTimeoutSeconds = 3600 }
        };

        Assert.False(JobValidation.ValidateCandidate([], invalid).IsValid);
        Assert.True(JobValidation.ValidateCandidate([], valid).IsValid);

        Assert.False(JobValidation.ValidateCandidate([], new DesktopDuplicationStep
        {
            Settings = new DesktopDuplicationSettings { DesktopIdx = -1 }
        }).IsValid);
        Assert.True(JobValidation.ValidateCandidate([], new DesktopDuplicationStep
        {
            Settings = new DesktopDuplicationSettings { DesktopIdx = 0 }
        }).IsValid);

        Assert.False(JobValidation.ValidateCandidate([], new ScriptExecutionStep()).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new GetProcessStep()).IsValid);
        Assert.True(JobValidation.ValidateCandidate([], new GetProcessStep
        {
            Settings = new GetProcessSettings
            {
                Query = new ProcessTargetSettings { ProcessName = "explorer" }
            }
        }).IsValid);

        Assert.False(JobValidation.ValidateCandidate([], new MakroExecutionStep()).IsValid);
        Assert.True(JobValidation.ValidateCandidate([], new MakroExecutionStep
        {
            Settings = new MakroExecutionSettings
            {
                MakroId = Guid.NewGuid(),
                MakroName = "Macro"
            }
        }).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new JobExecutionStep()).IsValid);
        Assert.True(JobValidation.ValidateCandidate([], new JobExecutionStep
        {
            Settings = new JobExecutionStepSettings
            {
                JobId = Guid.NewGuid(),
                JobName = "Child job"
            }
        }).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new ActiveProcessStep()).IsValid);
        Assert.True(JobValidation.ValidateCandidate([], new ActiveProcessStep
        {
            Settings = new ActiveProcessSettings
            {
                Target = new ProcessTargetSettings { ProcessName = "explorer" }
            }
        }).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new ActiveWindowStep
        {
            Settings = new ActiveWindowSettings
            {
                Target = new ProcessTargetSettings { ProcessName = "explorer" },
                CacheMs = -1
            }
        }).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new TerminateProcessStep()).IsValid);
        Assert.True(JobValidation.ValidateCandidate([], new TerminateProcessStep
        {
            Settings = new TerminateProcessSettings
            {
                Target = new ProcessTargetSettings { ProcessName = "notepad" }
            }
        }).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new FocusProcessStep()).IsValid);
        Assert.True(JobValidation.ValidateCandidate([], new FocusProcessStep
        {
            Settings = new FocusProcessSettings
            {
                Target = new ProcessTargetSettings { ExecutablePath = @"C:\Windows\notepad.exe" }
            }
        }).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new StartProcessStep()).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new DynamicRoiStep()).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new PredictMovementStep()).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new KlickOnPointStep()).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new FileSystemOperationStep()).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new ShowTextStep()).IsValid);
        Assert.False(JobValidation.ValidateCandidate([], new CameraCaptureStep()).IsValid);
    }

    [Fact]
    public void Catalog_RejectsDuplicateDefinitions()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new StepDefinitionCatalog([new TimeoutStepDefinition(), new TimeoutStepDefinition()]));

        Assert.Contains("Duplicate", exception.Message);
    }

    [Fact]
    public void Catalog_RejectsUnknownVisibleWhenAllFieldAndEditorHint()
    {
        var timeout = new TimeoutStepDefinition();
        var field = Assert.Single(timeout.Descriptor.Fields);
        var unknownVisibility = new DescriptorOverrideDefinition(timeout, timeout.Descriptor with
        {
            Fields = [field with { VisibleWhenAll = [new StepVisibilityRule("missing")] }]
        });
        var unknownHint = new DescriptorOverrideDefinition(timeout, timeout.Descriptor with
        {
            TypeId = "timeout_unknown_hint",
            Fields = [field with { EditorHint = "unknown-editor" }]
        });
        Assert.Contains("unknown visibility field", Assert.Throws<InvalidOperationException>(() =>
            new StepDefinitionCatalog([unknownVisibility])).Message);
        Assert.Contains("unknown editor hint", Assert.Throws<InvalidOperationException>(() =>
            new StepDefinitionCatalog([unknownHint])).Message);
    }

    [Fact]
    public void Catalog_RequiresClosedEnumOptionsAndExplicitKnownDefault()
    {
        var focus = new FocusProcessStepDefinition();
        var enumField = focus.Descriptor.Fields.Single(field =>
            field.Id == FocusProcessStepDefinition.ActionFieldId);

        Invalid(enumField with { Options = null }, "options");
        Invalid(enumField with { DefaultValue = null }, "default");
        Invalid(enumField with { DefaultValue = JsonValue.Create("bringtofront") }, "unknown default");

        void Invalid(StepFieldDescriptor replacement, string expectedMessage)
        {
            var definition = new DescriptorOverrideDefinition(focus, focus.Descriptor with
            {
                TypeId = $"focus_process_{Guid.NewGuid():N}",
                Fields = focus.Descriptor.Fields
                    .Select(field => field.Id == replacement.Id ? replacement : field)
                    .ToArray()
            });
            Assert.Contains(expectedMessage, Assert.Throws<InvalidOperationException>(() =>
                new StepDefinitionCatalog([definition])).Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EveryBuiltInEnumField_HasExactOptionsAndAnExplicitKnownDefault()
    {
        foreach (var field in BuiltInStepDefinitions.Instance.Definitions
                     .SelectMany(definition => definition.Descriptor.Fields)
                     .Where(field => field.ValueKind == StepValueKind.Enum))
        {
            Assert.NotEmpty(field.Options ?? []);
            Assert.True(StepEnumRules.TryReadDefaultToken(field, out var token));
            Assert.Contains(field.Options!, option =>
                string.Equals(option.Value, token, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void InvalidClrEnumToken_IsRejectedInsteadOfFallingBackToZero()
    {
        IStepDefinition definition = new FileSystemOperationStepDefinition();
        var draft = definition.CreateDraft();
        draft.Values[FileSystemOperationStepDefinition.OperationFieldId] = JsonValue.Create("copy");

        Assert.Contains(definition.ValidateDraft(draft), issue =>
            issue.FieldId == FileSystemOperationStepDefinition.OperationFieldId
            && issue.Code == "StepValidation.Invalid");
        Assert.Throws<InvalidOperationException>(() => definition.ApplyDraft(draft));
    }

    [Fact]
    public void Definition_ValidatesDescriptorConstraintsAndIgnoresHiddenRequiredFields()
    {
        IStepDefinition timeout = new TimeoutStepDefinition();
        var invalidTimeout = timeout.CreateDraft();
        invalidTimeout.Values[TimeoutStepDefinition.DelayFieldId] = JsonValue.Create(-1);

        Assert.Equal("StepValidation.Minimum", Assert.Single(timeout.ValidateDraft(invalidTimeout)).Code);

        IStepDefinition legacyTerminate = new StartProcessStepDefinition();
        var draft = legacyTerminate.CreateDraft(new StartProcessStep
        {
            Settings = new StartProcessSettings
            {
                Action = StartProcessAction.Terminate,
                Target = new ProcessTargetSettings { ProcessName = "notepad" }
            }
        });
        Assert.Empty(legacyTerminate.ValidateDraft(draft));
    }

    [Fact]
    public void Definitions_EnumerateConfiguredInputsWithoutTypeSwitches()
    {
        IStepDefinition fileSystem = new FileSystemOperationStepDefinition();
        var explicitPath = new FileSystemOperationStep
        {
            Settings = new FileSystemOperationSettings { SourcePath = @"C:\Source", TargetPath = @"C:\Target" }
        };
        Assert.Empty(fileSystem.GetInputBindings(explicitPath));

        explicitPath.Settings.SourceMode = FileSystemPathSource.TaskResult;
        explicitPath.Settings.SourceResult = Binding("source", "Value");
        Assert.Empty(fileSystem.GetInputBindings(explicitPath));

        IStepDefinition comparison = new PointComparisonStepDefinition();
        var points = new PointComparisonStep
        {
            Settings = new PointComparisonSettings
            {
                Points =
                [
                    new PointEntry(),
                    new PointEntry { Source = PointEntrySource.JobResult, PointsSource = Binding("points", "Points") }
                ]
            }
        };
        var pointInput = Assert.Single(comparison.GetInputBindings(points));
        Assert.Equal("points", pointInput.ContractId);
        Assert.Equal("points", pointInput.Binding.SourceStepId);
    }

    [Fact]
    public void GeneratedEditor_CreatesTimeoutAndValidatesInput()
    {
        var editor = new GeneratedStepEditorViewModel(new TimeoutStepDefinition());
        var field = Assert.Single(editor.Fields);
        field.InputText = "1500";

        Assert.True(editor.TryCreateStep(out var created));
        Assert.Equal(1500, Assert.IsType<TimeoutStep>(created).Settings.DelayMs);

        field.InputText = "-1";
        Assert.False(editor.TryCreateStep(out created));
        Assert.Null(created);
        Assert.NotNull(editor.ValidationError);
    }

    [Fact]
    public void AddStepDialog_ValidatesDirectTimeoutThroughVariableInputPath()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [], cameraCaptureService: new CameraDefinitionTestService());
        viewModel.SelectedType = "Timeout";
        var editor = Assert.IsType<GeneratedStepEditorViewModel>(viewModel.GeneratedEditor);
        var field = Assert.Single(editor.Fields);

        field.InputText = "-1";

        Assert.True(field.InputReferenceEditor!.Picker.IsStepValue);
        Assert.False(editor.TryCreateStep(out var created));
        Assert.Null(created);
        Assert.NotNull(editor.ValidationError);
    }

    [Fact]
    public void GeneratedEditor_CreatesBlockAndUnblockInputSteps()
    {
        var blockEditor = new GeneratedStepEditorViewModel(new BlockInputStepDefinition());
        Assert.Single(blockEditor.Fields).InputText = "45";

        Assert.True(blockEditor.TryCreateStep(out var createdBlock));
        Assert.Equal(45, Assert.IsType<BlockInputStep>(createdBlock).Settings.SafetyTimeoutSeconds);

        var unblockEditor = new GeneratedStepEditorViewModel(new UnblockInputStepDefinition());
        Assert.Empty(unblockEditor.Fields);
        Assert.True(unblockEditor.HasEditorDescription);
        Assert.True(unblockEditor.TryCreateStep(out var createdUnblock));
        Assert.IsType<UnblockInputStep>(createdUnblock);
    }

    [Fact]
    public void GeneratedEditor_RendersBooleanAndCreatesEndJobStep()
    {
        var editor = new GeneratedStepEditorViewModel(new EndJobStepDefinition());
        var field = Assert.Single(editor.Fields);

        Assert.True(field.IsBoolean);
        Assert.False(field.UsesTextInput);
        Assert.False(field.BooleanValue);

        field.BooleanValue = true;

        Assert.True(editor.TryCreateStep(out var created));
        Assert.True(Assert.IsType<EndJobStep>(created).Settings.SkipEndSteps);
    }

    [Fact]
    public void GeneratedEditor_UsesMonitorPickerHintAndCreatesDesktopCapture()
    {
        var editor = new GeneratedStepEditorViewModel(new DesktopDuplicationStepDefinition());
        var monitor = editor.Fields.Single(field =>
            field.Descriptor.Id == DesktopDuplicationStepDefinition.DesktopIndexFieldId);
        var cursor = editor.Fields.Single(field =>
            field.Descriptor.Id == DesktopDuplicationStepDefinition.CaptureCursorFieldId);

        Assert.True(monitor.UsesMonitorPicker);
        Assert.False(monitor.UsesTextInput);
        editor.ApplyMonitorSelection(monitor, 3, "DISPLAY-A");
        cursor.BooleanValue = true;

        Assert.True(editor.TryCreateStep(out var created));
        var capture = Assert.IsType<DesktopDuplicationStep>(created);
        Assert.Equal(3, capture.Settings.DesktopIdx);
        Assert.Equal("DISPLAY-A", capture.Settings.MonitorDeviceName);
        Assert.True(capture.Settings.CaptureCursor);
    }

    [Fact]
    public void GeneratedEditor_UsesFilePickerAndAdvancedSectionForScript()
    {
        var scriptPath = Path.GetTempFileName();
        try
        {
            var editor = new GeneratedStepEditorViewModel(new ScriptExecutionStepDefinition());
            var path = editor.Fields.Single(field =>
                field.Descriptor.Id == ScriptExecutionStepDefinition.ScriptPathFieldId);

            Assert.True(path.UsesFilePicker);
            Assert.False(path.UsesTextInput);
            Assert.True(editor.Sections.Single(section => section.IsCollapsible).Descriptor.Collapsible);
            path.InputText = scriptPath;

            Assert.True(editor.TryCreateStep(out var created));
            Assert.Equal(scriptPath, Assert.IsType<ScriptExecutionStep>(created).Settings.ScriptPath);
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    [Fact]
    public void GeneratedEditor_UsesProvidedProcessSuggestionsAndCreatesGetProcess()
    {
        var editor = new GeneratedStepEditorViewModel(
            new GetProcessStepDefinition(),
            suggestionResolver: field => field.EditorHint == StepEditorHints.ProcessNameSuggestions
                ? ["explorer", "notepad"]
                : null);
        var processName = editor.Fields.Single(field =>
            field.Descriptor.Id == GetProcessStepDefinition.ProcessNameFieldId);

        Assert.True(processName.UsesSuggestions);
        Assert.Equal(["explorer", "notepad"], processName.Suggestions);
        processName.InputText = "notepad";

        Assert.True(editor.TryCreateStep(out var created));
        Assert.Equal("notepad", Assert.IsType<GetProcessStep>(created).Settings.Query.ProcessName);
    }

    [Fact]
    public void GeneratedEditor_SelectsRequiredReferenceAndCreatesMacroStep()
    {
        var macroId = Guid.NewGuid();
        var option = new GeneratedStepChoiceOptionViewModel(
            new StepReferenceValue(macroId.ToString("D"), "Cleanup"));
        var editor = new GeneratedStepEditorViewModel(
            new MakroExecutionStepDefinition(),
            choiceResolver: _ => [option]);
        var field = Assert.Single(editor.Fields);

        Assert.True(field.UsesChoicePicker);
        Assert.Same(option, field.SelectedChoice);
        Assert.True(editor.TryCreateStep(out var created));
        var macro = Assert.IsType<MakroExecutionStep>(created);
        Assert.Equal(macroId, macro.Settings.MakroId);
        Assert.Equal("Cleanup", macro.Settings.MakroName);
    }

    [Fact]
    public void GeneratedEditor_ResolvesLegacyReferenceByNameAndUpdatesStableId()
    {
        var macroId = Guid.NewGuid();
        var existing = new MakroExecutionStep
        {
            Settings = new MakroExecutionSettings { MakroName = "Legacy macro" }
        };
        var option = new GeneratedStepChoiceOptionViewModel(
            new StepReferenceValue(macroId.ToString("D"), "Legacy macro"));
        var editor = new GeneratedStepEditorViewModel(
            new MakroExecutionStepDefinition(),
            existing,
            choiceResolver: _ => [option]);

        Assert.Same(option, Assert.Single(editor.Fields).SelectedChoice);
        Assert.True(editor.TryCreateStep(out var created));
        Assert.Equal(macroId, Assert.IsType<MakroExecutionStep>(created).Settings.MakroId);
    }

    [Fact]
    public void GeneratedEditor_UsesProcessTargetAdapterAndCreatesActiveProcess()
    {
        var contract = StepInputContractRegistry.Get(typeof(ActiveProcessStep), "process");
        Assert.NotNull(contract);
        var processEditor = new GeneratedProcessTargetEditorViewModel(
            value: null,
            new ValueReferencePickerViewModel([], contract!, false),
            ["explorer", "notepad"]);
        var editor = new GeneratedStepEditorViewModel(
            new ActiveProcessStepDefinition(),
            processTargetResolver: (_, _) => processEditor);
        var field = Assert.Single(editor.Fields);

        Assert.True(field.UsesProcessTargetPicker);
        Assert.False(field.UsesTextInput);
        processEditor.ProcessName = "notepad";

        Assert.True(editor.TryCreateStep(out var created));
        Assert.Equal("notepad", Assert.IsType<ActiveProcessStep>(created).Settings.Target.ProcessName);
    }

    [Fact]
    public void ProcessTargetEditor_UsesDistinctContentForEachSourceMode()
    {
        var contract = StepInputContractRegistry.Get(typeof(ActiveProcessStep), "process");
        Assert.NotNull(contract);
        var editor = new GeneratedProcessTargetEditorViewModel(
            value: null,
            new ValueReferencePickerViewModel([], contract!, false),
            ["explorer", "notepad"]);

        var processNameContent = Assert.IsType<GeneratedProcessNameTargetContentViewModel>(
            editor.SelectedSourceContent);
        Assert.Same(editor, processNameContent.Editor);

        editor.SelectedSourceOption = editor.SourceOptions.Single(option => option.Value == "JobResult");

        var processReferenceContent = Assert.IsType<GeneratedProcessReferenceTargetContentViewModel>(
            editor.SelectedSourceContent);
        Assert.Same(editor, processReferenceContent.Editor);
        Assert.NotSame(processNameContent, processReferenceContent);
    }

    [Fact]
    public void ProcessTargetEditor_WritesEitherWholeReferenceOrIndividualSources()
    {
        var nestedBinding = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = Guid.NewGuid().ToString("D")
        };
        var contract = StepInputContractRegistry.Get(typeof(ActiveProcessStep), "process")!;
        var editor = new GeneratedProcessTargetEditorViewModel(
            value: null,
            new ValueReferencePickerViewModel([], contract, false),
            [],
            nestedInputResolver: (_, _, _) => new GeneratedResultBindingEditorViewModel(
                JsonSerializer.SerializeToNode(nestedBinding),
                new ValueReferencePickerViewModel([], contract, true)));

        Assert.All(editor.InputBindings.Values, binding => Assert.True(binding.IsConfigured));

        editor.WholeValueSource.UsesReference = true;

        Assert.False(editor.WholeValueSource.ShowsIndividualValues);
        Assert.All(editor.InputBindings.Values, binding => Assert.False(binding.IsConfigured));
    }

    [Fact]
    public void ProcessTargetEditor_RoundTripsWindowTitleInsideSharedSelector()
    {
        var definition = new ActiveProcessStepDefinition();
        var existing = new ActiveProcessStep
        {
            Settings = new ActiveProcessSettings
            {
                Target = new ProcessTargetSettings
                {
                    ProcessName = "notepad",
                    WindowTitleContains = "Editor"
                }
            }
        };
        var value = definition.CreateDraft(existing).Values[ActiveProcessStepDefinition.ProcessTargetFieldId];
        var processEditor = new GeneratedProcessTargetEditorViewModel(
            value,
            new ValueReferencePickerViewModel(
                [],
                StepInputContractRegistry.Get(typeof(ActiveProcessStep), "process")!,
                false),
            ["notepad"]);
        var editor = new GeneratedStepEditorViewModel(
            definition,
            existing,
            processTargetResolver: (_, _) => processEditor);

        Assert.Equal("Editor", processEditor.WindowTitleContains);
        processEditor.WindowTitleContains = "Document";

        Assert.True(editor.TryCreateStep(out var created));
        var target = Assert.IsType<ActiveProcessStep>(created).Settings.Target;
        Assert.Equal("notepad", target.ProcessName);
        Assert.Equal("Document", target.WindowTitleContains);
    }

    [Fact]
    public void ProcessTargetEditor_LoadsLegacySelectorWithoutWindowTitle()
    {
        var value = JsonNode.Parse("""
            {
              "process_source": {},
              "process_name": "notepad",
              "executable_path": ""
            }
            """);
        var editor = new GeneratedProcessTargetEditorViewModel(
            value,
            new ValueReferencePickerViewModel(
                [],
                StepInputContractRegistry.Get(typeof(ActiveProcessStep), "process")!,
                false),
            ["notepad"]);

        Assert.Equal("notepad", editor.ProcessName);
        Assert.Empty(editor.WindowTitleContains);
    }

    [Fact]
    public void ProcessTargetEditor_PreservesManualFallbackWhenReferenceIsNotConfigured()
    {
        var editor = new GeneratedProcessTargetEditorViewModel(
            value: null,
            new ValueReferencePickerViewModel(
                [],
                StepInputContractRegistry.Get(typeof(ActiveProcessStep), "process")!,
                false),
            []);
        editor.ProcessName = "notepad";
        editor.WindowTitleContains = "Editor";
        editor.SelectedSourceOption = editor.SourceOptions.Single(option => option.Value == "JobResult");

        var value = editor.ToValue();

        Assert.Equal("notepad", value.ProcessName);
        Assert.Equal("Editor", value.WindowTitleContains);
    }

    [Fact]
    public void ProcessAndWindowSteps_KeepWindowTitleInsideSharedTargetField()
    {
        IStepDefinition[] definitions =
        [
            new ActiveProcessStepDefinition(),
            new ActiveWindowStepDefinition(),
            new TerminateProcessStepDefinition(),
            new FocusProcessStepDefinition()
        ];

        Assert.All(definitions, definition =>
        {
            Assert.Single(definition.Descriptor.Fields, field => field.Id == "process_target");
            Assert.DoesNotContain(definition.Descriptor.Fields, field => field.Id == "window_title_contains");
        });
    }

    [Fact]
    public void ProcessAndWindowSteps_UseEditableProcessNamePickerForManualMode()
    {
        IStepDefinition[] definitions =
        [
            new ActiveProcessStepDefinition(),
            new ActiveWindowStepDefinition(),
            new TerminateProcessStepDefinition(),
            new FocusProcessStepDefinition()
        ];

        Assert.All(definitions, definition =>
        {
            var target = definition.Descriptor.Fields.Single(field => field.Id == "process_target");
            Assert.Equal(StepEditorHints.ProcessTargetPicker, target.EditorHint);
        });
    }

    [Fact]
    public void GeneratedEditor_UsesEnumOptionsAndConditionalVisibilityForFocusProcess()
    {
        var definition = new FocusProcessStepDefinition();
        var processEditor = new GeneratedProcessTargetEditorViewModel(
            value: null,
            new ValueReferencePickerViewModel([], StepInputContractRegistry.Get(typeof(FocusProcessStep), "process")!, false),
            []);
        var existing = new FocusProcessStep
        {
            Settings = new FocusProcessSettings
            {
                Action = FocusProcessAction.BringToFront,
                WindowMode = FocusProcessWindowMode.Maximized
            }
        };
        var editor = new GeneratedStepEditorViewModel(
            definition,
            existing,
            processTargetResolver: (_, _) => processEditor);
        var target = editor.Fields.Single(field => field.Descriptor.Id == FocusProcessStepDefinition.ProcessTargetFieldId);
        var action = editor.Fields.Single(field => field.Descriptor.Id == FocusProcessStepDefinition.ActionFieldId);
        var windowMode = editor.Fields.Single(field => field.Descriptor.Id == FocusProcessStepDefinition.WindowModeFieldId);

        Assert.True(target.UsesNamedProcessTargetPicker);
        Assert.True(action.UsesEnumPicker);
        Assert.Equal(2, action.EnumOptions.Count);
        Assert.True(windowMode.IsVisible);

        action.SelectedEnumOption = action.EnumOptions.Single(option => option.Value == nameof(FocusProcessAction.Minimize));
        Assert.False(windowMode.IsVisible);
        windowMode.InputText = "removed-window-mode";
        processEditor.ProcessName = "notepad";

        Assert.True(editor.TryCreateStep(out var created));
        var focus = Assert.IsType<FocusProcessStep>(created);
        Assert.Equal(FocusProcessAction.Minimize, focus.Settings.Action);
        Assert.Equal(FocusProcessWindowMode.Maximized, focus.Settings.WindowMode);
        Assert.Equal("notepad", focus.Settings.Target.ProcessName);
        Assert.Empty(focus.Settings.Target.ExecutablePath);
    }

    [Fact]
    public void FocusProcessEditor_ConvertsLegacyExecutablePathToEditableProcessName()
    {
        var definition = new FocusProcessStepDefinition();
        var existing = new FocusProcessStep
        {
            Settings = new FocusProcessSettings
            {
                Target = new ProcessTargetSettings { ExecutablePath = @"C:\Windows\notepad.exe" }
            }
        };
        var value = definition.CreateDraft(existing).Values[FocusProcessStepDefinition.ProcessTargetFieldId];
        var processEditor = new GeneratedProcessTargetEditorViewModel(
            value,
            new ValueReferencePickerViewModel([], StepInputContractRegistry.Get(typeof(FocusProcessStep), "process")!, false),
            []);
        var editor = new GeneratedStepEditorViewModel(
            definition,
            existing,
            processTargetResolver: (_, _) => processEditor);

        Assert.Equal("notepad", processEditor.ProcessName);
        Assert.True(editor.TryCreateStep(out var created));
        var focus = Assert.IsType<FocusProcessStep>(created);
        Assert.Equal("notepad", focus.Settings.Target.ProcessName);
        Assert.Empty(focus.Settings.Target.ExecutablePath);
    }

    [Fact]
    public void GeneratedEditor_CreatesStartProcessWithPickerEnumsAndConditionalOffsets()
    {
        var executable = Path.GetTempFileName();
        try
        {
            var editor = new GeneratedStepEditorViewModel(
                new StartProcessStepDefinition(),
                suggestionResolver: field => field.EditorHint == StepEditorHints.StartProgramPicker
                    ? [executable]
                    : null);
            var path = editor.Fields.Single(field => field.Descriptor.Id == StartProcessStepDefinition.ExecutablePathFieldId);
            var placement = editor.Fields.Single(field => field.Descriptor.Id == StartProcessStepDefinition.PlacementModeFieldId);
            var offset = editor.Fields.Single(field => field.Descriptor.Id == StartProcessStepDefinition.OffsetXFieldId);

            Assert.True(path.UsesSuggestionFilePicker);
            Assert.False(offset.IsVisible);
            path.InputText = executable;
            placement.SelectedEnumOption = placement.EnumOptions.Single(option =>
                option.Value == nameof(StartProcessPlacementMode.Custom));
            Assert.True(offset.IsVisible);
            offset.InputText = "25";

            Assert.True(editor.TryCreateStep(out var created));
            var start = Assert.IsType<StartProcessStep>(created);
            Assert.Equal(StartProcessPlacementMode.Custom, start.Settings.PlacementMode);
            Assert.Equal(25, start.Settings.OffsetX);
        }
        finally
        {
            File.Delete(executable);
        }
    }

    [Fact]
    public void GeneratedEditor_UsesResultBindingAndPercentageAdaptersForDynamicRoi()
    {
        var resultType = StepResultMetadata.ResultTypes.Single(type =>
            type.TypeName == nameof(TemplateMatchingResult));
        var source = new SourceStepItem("detection", "Detection", resultType);
        var padding = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "ROI padding",
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create(12)
        };
        var editor = new GeneratedStepEditorViewModel(
            new DynamicRoiStepDefinition(),
            resultBindingResolver: (field, value) =>
            {
                if (field.ValueKind != StepValueKind.ResultBinding) return null;
                var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), field.InputContractId!)!;
                return new GeneratedResultBindingEditorViewModel(
                    value,
                    new ValueReferencePickerViewModel(
                        field.InputContractId == "bounds" ? [source] : [],
                        contract,
                        true,
                        [padding]));
            });
        var sourceField = editor.Fields.Single(field => field.Descriptor.Id == DynamicRoiStepDefinition.BoundsSourceFieldId);
        var paddingField = editor.Fields.Single(field => field.Descriptor.Id == DynamicRoiStepDefinition.PaddingSourceFieldId);
        var confidence = editor.Fields.Single(field => field.Descriptor.Id == DynamicRoiStepDefinition.MinimumConfidenceFieldId);

        Assert.True(sourceField.UsesValueReferencePicker);
        Assert.True(paddingField.UsesValueReferencePicker);
        Assert.True(confidence.UsesPercentagePicker);
        confidence.NumberValue = 0.6;

        Assert.True(editor.TryCreateStep(out var created));
        var dynamicRoi = Assert.IsType<DynamicRoiStep>(created);
        Assert.Equal("detection", dynamicRoi.Settings.BoundsSource.SourceStepId);
        Assert.Equal(ValueProviderIds.JobVariable, dynamicRoi.Settings.PaddingSource.ProviderId);
        Assert.Equal(padding.Id.ToString("D"), dynamicRoi.Settings.PaddingSource.SourceId);
        Assert.Equal(0.6, dynamicRoi.Settings.MinimumConfidence, 3);
    }

    [Fact]
    public void ValueReferencePicker_SelectsCompatibleJobVariableAsProviderReference()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Target",
            ValueKind = ResultValueKind.Point,
            Cardinality = ResultCardinality.Single,
            Value = new System.Text.Json.Nodes.JsonObject { ["x"] = 10, ["y"] = 20 }
        };
        var contract = new StepInputDescriptor(
            "point", true, MissingValuePolicy.FailStep, CollectionConsumptionMode.FirstValue,
            new AcceptedResultShape(ResultValueKind.Point, ResultCardinality.Single))
        {
            AllowedProviderIds = new HashSet<string> { ValueProviderIds.JobVariable }
        };
        var picker = new ValueReferencePickerViewModel([], contract, selectDefault: false, variables: [variable]);
        picker.UseJobVariableCommand.Execute(null);

        Assert.Single(picker.SelectionTree);
        var variableNode = picker.SelectionTree.Single(node => node.DisplayName == variable.Name);
        Assert.Equal(["x", "y"], variableNode.Children.Select(node => node.DisplayName));
        variableNode.SelectCommand!.Execute(null);
        var binding = picker.ToBinding();

        Assert.Equal(ValueProviderIds.JobVariable, binding.ProviderId);
        Assert.Equal(variable.Id.ToString("D"), binding.SourceId);
        Assert.True(string.IsNullOrEmpty(binding.SourceStepId));
        Assert.True(string.IsNullOrEmpty(binding.PropertyId));
        Assert.Equal(variable.Name, picker.SelectedPropertyName);
        Assert.Contains("x: 10", picker.SelectedPreviewValue);
        Assert.Equal(variable.Name, picker.SelectedInlineText);
        Assert.Contains("x: 10", picker.SelectedTooltipValue);
        Assert.Equal(Loc.Get("Ui.Job.Variables.Scope.Shared"), picker.SelectedPreviewSource);
        Assert.False(string.IsNullOrWhiteSpace(picker.SelectedPreviewType));
        var selectedNode = picker.SelectionTree.Single(node => node.DisplayName == variable.Name);
        Assert.True(selectedNode.IsSelected);
        Assert.Equal(Loc.Get("Ui.Job.Variables.Scope.Shared"), selectedNode.SourceText);
        Assert.False(string.IsNullOrWhiteSpace(selectedNode.ValueText));
        Assert.False(string.IsNullOrWhiteSpace(selectedNode.FullValueText));
        Assert.False(string.IsNullOrWhiteSpace(selectedNode.SecondaryText));
    }

    [Fact]
    public void ValueReferencePicker_SelectsCompatibleMemberOfCompoundJobVariable()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Target",
            ValueKind = ResultValueKind.Point,
            Value = new JsonObject { ["x"] = 10, ["y"] = 20 }
        };
        var contract = new StepInputDescriptor(
            "coordinate", true, MissingValuePolicy.FailStep, CollectionConsumptionMode.FirstValue,
            new AcceptedResultShape(ResultValueKind.Integer, ResultCardinality.Single))
        {
            AllowedProviderIds = new HashSet<string> { ValueProviderIds.JobVariable }
        };
        var picker = new ValueReferencePickerViewModel([], contract, selectDefault: false, variables: [variable]);
        picker.UseJobVariableCommand.Execute(null);

        var variableNode = picker.SelectionTree.Single(node => node.DisplayName == variable.Name);
        var xNode = variableNode.Children.Single(node => node.DisplayName == "X");
        xNode.SelectCommand!.Execute(null);
        var binding = picker.ToBinding();

        Assert.Equal(ValueProviderIds.JobVariable, binding.ProviderId);
        Assert.Equal(variable.Id.ToString("D"), binding.SourceId);
        Assert.Equal("X", binding.ValuePath);
        Assert.Contains("Target", picker.SelectedPropertyName);
        Assert.Contains("X", picker.SelectedPropertyName);
        Assert.Equal("10", xNode.ValueText);
        Assert.Equal("10", xNode.FullValueText);
        Assert.Equal("10", picker.SelectedPreviewValue);
        Assert.Equal(picker.SelectedPropertyName, picker.SelectedInlineText);
        Assert.Equal("10", picker.SelectedTooltipValue);
        Assert.Equal(Loc.Get("Ui.Job.Variables.Scope.Shared"), picker.SelectedPreviewSource);
        Assert.False(string.IsNullOrWhiteSpace(picker.SelectedPreviewType));

        var reloaded = new ValueReferencePickerViewModel([], contract, selectDefault: false, variables: [variable]);
        reloaded.Load(binding);
        Assert.Equal("X", reloaded.ToBinding().ValuePath);
        Assert.True(reloaded.SelectionTree.Single(node => node.DisplayName == variable.Name)
            .Children.Single(node => node.DisplayName == "X").IsSelected);

        var options = new JsonSerializerOptions();
        JobJsonSerialization.Configure(options);
        Assert.Contains("\"value_path\":\"X\"", JsonSerializer.Serialize(binding, options));
    }

    [Fact]
    public void ValueReferencePicker_ShowsRuntimeValueForNestedJobVariableProperty()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Window area",
            ValueKind = ResultValueKind.Rectangle,
            Value = new JsonObject { ["x"] = 120, ["y"] = 80, ["width"] = 640, ["height"] = 480 }
        };
        var contract = new StepInputDescriptor(
            "coordinate", true, MissingValuePolicy.FailStep, CollectionConsumptionMode.FirstValue,
            new AcceptedResultShape(ResultValueKind.Integer, ResultCardinality.Single))
        {
            AllowedProviderIds = new HashSet<string> { ValueProviderIds.JobVariable }
        };
        var picker = new ValueReferencePickerViewModel([], contract, selectDefault: false, variables: [variable]);
        picker.UseJobVariableCommand.Execute(null);

        var variableNode = picker.SelectionTree.Single(node => node.DisplayName == variable.Name);
        var centerNode = variableNode.Children.Single(node => node.DisplayName == "Center");
        var centerXNode = centerNode.Children.Single(node => node.DisplayName == "X");
        centerXNode.SelectCommand!.Execute(null);

        Assert.Equal("440", centerXNode.ValueText);
        Assert.Equal("440", picker.SelectedPreviewValue);
        Assert.Equal(picker.SelectedPropertyName, picker.SelectedInlineText);
        Assert.DoesNotContain("120", picker.SelectedPreviewValue);
        Assert.Contains(variable.Name, picker.SelectedInlineText);
        Assert.Equal("Center.X", picker.ToBinding().ValuePath);
    }

    [Fact]
    public void ValueReferencePicker_GroupsStepResultsAndUsesVariablePlaceholder()
    {
        var resultType = StepResultMetadata.ResultTypes.Single(type =>
            type.TypeName == nameof(TemplateMatchingResult));
        var source = new SourceStepItem("detection", "Detection", resultType);
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "bounds")!;

        var picker = new ValueReferencePickerViewModel([source], contract, selectDefault: false);
        picker.UseStepResultCommand.Execute(null);

        Assert.Equal("Detection", Assert.Single(picker.SelectionTree).DisplayName);
        Assert.Equal(Loc.Get("Ui.ValueReference.SelectVariable"), picker.SelectedDisplayPath);
    }

    [Fact]
    public void ValueReferencePicker_DoesNotOfferSecretsForStepInputs()
    {
        var secret = new ValueProviderSourceDescriptor(
            ValueProviderIds.Secret,
            Guid.NewGuid().ToString("D"),
            "API token",
            string.Empty,
            ResultValueKind.Text,
            ResultCardinality.Single,
            IsSensitive: true);
        var contract = StepInputContractRegistry.Get(typeof(ShowTextStep), "text")!;
        var picker = new ValueReferencePickerViewModel(
            [], contract, selectDefault: false, providerSources: [secret],
            context: new ValueReferencePickerContext("Show text", "Text", CreateSecret: () => secret));

        Assert.False(picker.CanUseSecrets);
        Assert.False(picker.UseSecretCommand.CanExecute(null));
        Assert.False(picker.CanCreateSecret);
        Assert.DoesNotContain(picker.SelectionTree, node => node.DisplayName == secret.Name);
    }

    [Fact]
    public void ValueReferencePicker_ListsCompatibleVariablesDirectlyInSelectedSourceKind()
    {
        var variables = new[]
        {
            new JobVariable
            {
                Scope = JobVariableScope.Shared,
                Name = "Outer padding",
                Description = "Space around the detection",
                ValueKind = ResultValueKind.Integer,
                Value = JsonValue.Create(25)
            },
            new JobVariable
            {
                Scope = JobVariableScope.Shared,
                Name = "Retries",
                ValueKind = ResultValueKind.Integer,
                Value = JsonValue.Create(3)
            }
        };
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "padding")!;
        var picker = new ValueReferencePickerViewModel([], contract, false, variables);
        picker.UseJobVariableCommand.Execute(null);

        Assert.Contains(picker.SelectionTree, child => child.DisplayName == "Outer padding");
        Assert.Contains(picker.SelectionTree, child => child.DisplayName == "Retries");

        var detailed = picker.SelectionTree.Single(child => child.DisplayName == "Outer padding");
        Assert.Equal("25", detailed.ValueText);
        Assert.Equal("Space around the detection", detailed.Description);
        Assert.False(string.IsNullOrWhiteSpace(detailed.SecondaryText));
        Assert.Equal(Loc.Get("Ui.Job.Variables.Scope.Shared"), detailed.SourceText);
        Assert.True(detailed.HasIcon);

        picker.SearchText = "Space around";
        Assert.Equal("Outer padding", Assert.Single(picker.SelectionTree).DisplayName);

        picker.SearchText = "3";
        Assert.Equal("Retries", Assert.Single(picker.SelectionTree).DisplayName);
    }

    [Fact]
    public void ValueReferencePicker_ShowsQuotedTextBesideVariableName()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Greeting",
            ValueKind = ResultValueKind.Text,
            Value = JsonValue.Create("Hello world")
        };
        var contract = StepInputContractRegistry.Get(typeof(ShowTextStep), "text")!;
        var picker = new ValueReferencePickerViewModel([], contract, false, [variable]);
        picker.UseJobVariableCommand.Execute(null);

        var node = Assert.Single(picker.SelectionTree);
        node.SelectCommand!.Execute(null);

        Assert.Equal("Greeting", picker.SelectedInlineText);
        Assert.Equal("“Hello world”", picker.SelectedPreviewValue);
        Assert.Equal("“Hello world”", node.ValueText);
        Assert.Equal(Loc.Get("Ui.Job.Variables.Scope.Shared"), picker.SelectedPreviewSource);
        Assert.False(string.IsNullOrWhiteSpace(picker.SelectedPreviewType));
    }

    [Fact]
    public void ValueReferencePicker_CreatesAndSelectsCompatibleJobVariable()
    {
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "padding")!;
        var created = new JobVariable
        {
            Name = "dynamischen_bildbereich_erstellen_rand_px",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(15)
        };
        var picker = new ValueReferencePickerViewModel(
            [], contract, false, context: new ValueReferencePickerContext(
                "Dynamischen Bildbereich erstellen",
                "Rand (px)",
                _ => created));

        picker.CreateJobVariableCommand.Execute(null);

        Assert.Equal(ValueProviderIds.JobVariable, picker.ToBinding().ProviderId);
        Assert.Equal(created.Id.ToString("D"), picker.ToBinding().SourceId);
        Assert.Contains(picker.SelectionTree, node => node.DisplayName == created.Name);
    }

    [Fact]
    public void ValueReferencePicker_RequiresExplicitChoiceBeforeEditingMultiplyUsedStepValue()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.StepValue,
            Name = "Timeout · Duration",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(1000)
        };
        var contract = StepInputContractRegistry.ForField(
            new TimeoutStepDefinition().Descriptor.Fields.Single());
        var picker = new ValueReferencePickerViewModel(
            [], contract, false, [variable], context: new ValueReferencePickerContext(
                "Timeout", "Duration", GetVariableUsageCount: _ => 2));
        picker.Load(new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        });

        Assert.True(picker.RequiresInlineEditChoice);
        Assert.False(picker.CanEditStepValueInline);

        picker.EditEverywhereCommand.Execute(null);

        Assert.False(picker.RequiresInlineEditChoice);
        Assert.True(picker.CanEditStepValueInline);
        Assert.Equal(variable.Id.ToString("D"), picker.ToBinding().SourceId);
    }

    [Fact]
    public void ValueReferencePicker_RaisesOneSemanticChangePerSelection()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Retries",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(3)
        };
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "padding")!;
        var picker = new ValueReferencePickerViewModel([], contract, false, [variable]);
        picker.UseJobVariableCommand.Execute(null);
        var editor = new GeneratedResultBindingEditorViewModel(null, picker);
        var changes = 0;
        editor.Changed += () => changes++;

        picker.SelectionTree.Single(node => node.DisplayName == variable.Name).SelectCommand!.Execute(null);

        Assert.Equal(1, changes);
    }

    [Fact]
    public void ValueReferencePicker_CachesUsageCountAndRefreshesValueWithoutRebuildingSelection()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.StepValue,
            Name = "Timeout · Duration",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(1000)
        };
        var usageChecks = 0;
        var contract = StepInputContractRegistry.ForField(
            new TimeoutStepDefinition().Descriptor.Fields.Single());
        var picker = new ValueReferencePickerViewModel(
            [], contract, false, [variable], context: new ValueReferencePickerContext(
                "Timeout", "Duration", GetVariableUsageCount: _ =>
                {
                    usageChecks++;
                    return 2;
                }));
        var semanticChanges = 0;
        picker.ReferenceChanged += (_, _) => semanticChanges++;

        picker.Load(new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        });
        var selectionTree = picker.SelectionTree;
        Assert.Equal(2, picker.SelectedVariableUsageCount);
        Assert.Equal(2, picker.SelectedVariableUsageCount);

        picker.RefreshSelectedValue();

        Assert.Equal(1, usageChecks);
        Assert.Equal(1, semanticChanges);
        Assert.Same(selectionTree, picker.SelectionTree);
    }

    [Fact]
    public void ValueReferencePicker_UpdatesSelectionWithoutReplacingTree()
    {
        var first = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "First",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(1)
        };
        var second = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Second",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(2)
        };
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "padding")!;
        var picker = new ValueReferencePickerViewModel([], contract, false, [first, second]);
        picker.UseJobVariableCommand.Execute(null);
        var tree = picker.SelectionTree;
        var firstNode = tree.Single(node => node.DisplayName == first.Name);
        var secondNode = tree.Single(node => node.DisplayName == second.Name);

        firstNode.SelectCommand!.Execute(null);
        secondNode.SelectCommand!.Execute(null);

        Assert.Same(tree, picker.SelectionTree);
        Assert.False(firstNode.IsSelected);
        Assert.True(secondNode.IsSelected);
    }

    [Fact]
    public void GeneratedInputReference_RefreshesInlineEditStateWithoutSemanticChange()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.StepValue,
            Name = "Timeout Â· Duration",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(1000)
        };
        var descriptor = new TimeoutStepDefinition().Descriptor.Fields.Single();
        var picker = new ValueReferencePickerViewModel(
            [], StepInputContractRegistry.ForField(descriptor), false, [variable],
            context: new ValueReferencePickerContext(
                "Timeout", "Duration", GetVariableUsageCount: _ => 2));
        picker.Load(new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        });
        var bindingEditor = new GeneratedResultBindingEditorViewModel(null, picker);
        var field = new GeneratedStepFieldViewModel(
            descriptor, JsonValue.Create(1000), inputReferenceEditor: bindingEditor);
        var semanticChanges = 0;
        bindingEditor.Changed += () => semanticChanges++;

        picker.EditEverywhereCommand.Execute(null);

        Assert.False(field.RequiresInlineEditChoice);
        Assert.True(field.CanEditInlineStepValue);
        Assert.Equal(0, semanticChanges);
    }

    [Fact]
    public void WholeValueSource_IgnoresRepeatedIndividualModeSelection()
    {
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "padding")!;
        var picker = new ValueReferencePickerViewModel([], contract, false);
        var source = new GeneratedWholeValueSourceViewModel(picker, usesReference: false);
        var sourceChanges = 0;
        var referenceChanges = 0;
        source.Changed += () => sourceChanges++;
        picker.ReferenceChanged += (_, _) => referenceChanges++;

        source.UsesReference = false;
        source.UseIndividualValuesCommand.Execute(null);

        Assert.Equal(0, sourceChanges);
        Assert.Equal(0, referenceChanges);
    }

    [Fact]
    public void RoiEnabled_DoesNotChangeValuesOrWholeValueSource()
    {
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "padding")!;
        var picker = new ValueReferencePickerViewModel([], contract, false);
        var editor = new GeneratedRoiEditorViewModel(
            JsonSerializer.SerializeToNode(new StepRoiSelectionValue(false, 10, 20, 300, 200, null)),
            picker);
        editor.UseDynamicRoi = true;

        editor.IsRoiEnabled = true;

        Assert.True(editor.IsRoiEnabled);
        Assert.True(editor.UseDynamicRoi);
        Assert.Equal(10, editor.X);
        Assert.Equal(20, editor.Y);
        Assert.Equal(300, editor.RoiWidth);
        Assert.Equal(200, editor.RoiHeight);
        Assert.DoesNotContain("enabled", editor.InputBindings.Keys);
    }

    [Fact]
    public void GeneratedEditor_UsageSnapshotContainsCurrentReferenceAndExistingStepId()
    {
        var first = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "First delay",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(1000)
        };
        var second = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Second delay",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(2000)
        };
        var definition = new TimeoutStepDefinition();
        var existing = new TimeoutStep();
        existing.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = first.Id.ToString("D")
        };
        var editor = new GeneratedStepEditorViewModel(
            definition,
            existing,
            inputReferenceResolver: (field, binding) =>
            {
                var picker = new ValueReferencePickerViewModel(
                    [], StepInputContractRegistry.ForField(field), false, [first, second]);
                return new GeneratedResultBindingEditorViewModel(
                    JsonSerializer.SerializeToNode(binding), picker);
            });
        var picker = editor.Fields.Single().InputReferenceEditor!.Picker;

        picker.SelectionTree.Single(node => node.DisplayName == second.Name).SelectCommand!.Execute(null);
        var snapshot = editor.CreateUsageSnapshot();

        Assert.NotNull(snapshot);
        Assert.Equal(existing.Id, snapshot!.Id);
        Assert.Equal(second.Id.ToString("D"),
            snapshot.Inputs[TimeoutStepDefinition.DelayFieldId].SourceId);
    }

    [Fact]
    public void ValueReferencePicker_DetachesMultiplyUsedStepValueForCurrentField()
    {
        var variable = new JobVariable
        {
            Scope = JobVariableScope.StepValue,
            Name = "Timeout · Duration",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(1000)
        };
        JobVariable? detached = null;
        var contract = StepInputContractRegistry.ForField(
            new TimeoutStepDefinition().Descriptor.Fields.Single());
        var picker = new ValueReferencePickerViewModel(
            [], contract, false, [variable], context: new ValueReferencePickerContext(
                "Timeout", "Duration", GetVariableUsageCount: _ => 2,
                DetachStepValue: source => detached = new JobVariable
                {
                    Scope = JobVariableScope.StepValue,
                    Name = "Timeout · Duration 2",
                    ValueKind = source.ValueKind,
                    Value = source.Value?.DeepClone()
                }));
        picker.Load(new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        });

        picker.EditOnlyHereCommand.Execute(null);

        Assert.NotNull(detached);
        Assert.Equal(detached!.Id.ToString("D"), picker.ToBinding().SourceId);
        Assert.True(picker.CanEditStepValueInline);
        Assert.Equal(1000, detached.Value!.GetValue<int>());
    }

    [Fact]
    public void QuickCreateJobVariable_RequiresUserSuppliedUniqueNameAndProvidesValue()
    {
        var existing = new[]
        {
            new JobVariable { Name = "bildschirm_duplizieren_desktop" }
        };
        var contract = StepInputContractRegistry.Get(typeof(DynamicRoiStep), "padding")!;

        var viewModel = new QuickCreateJobVariableViewModel(
            contract, "Bildschirm duplizieren", "Desktop", existing);

        Assert.Equal(string.Empty, viewModel.Variable.Name);
        Assert.False(viewModel.CanCreate);
        viewModel.Name = "bildschirm_duplizieren_desktop";
        Assert.False(viewModel.CanCreate);
        viewModel.Name = "Mein Rand";
        Assert.True(viewModel.CanCreate);
        Assert.Equal("Mein Rand", viewModel.Variable.Name);
        Assert.Equal(JobVariableScope.Shared, viewModel.Variable.Scope);
        Assert.Contains("Desktop", viewModel.Variable.Description);
        Assert.Equal(ResultValueKind.Integer, viewModel.Variable.ValueKind);
        Assert.Equal(ResultValueKind.Integer, viewModel.SelectedKind.Kind);
        Assert.Contains(viewModel.TypeOptions, option => option.Kind == ResultValueKind.Integer);
        Assert.Equal(0, viewModel.Variable.Value!.GetValue<int>());
    }

    [Fact]
    public void JobVariableEditor_OffersOnlySimpleVariableTypesButKeepsLegacyTypesReadable()
    {
        var expected = new HashSet<ResultValueKind>
        {
            ResultValueKind.Text,
            ResultValueKind.Boolean,
            ResultValueKind.Integer,
            ResultValueKind.Number,
            ResultValueKind.DateTime,
            ResultValueKind.Point,
            ResultValueKind.Rectangle,
            ResultValueKind.Color,
            ResultValueKind.FilePath
        };
        Assert.True(expected.SetEquals(JobVariableEditorViewModel.SupportedKinds));
        Assert.DoesNotContain(ResultValueKind.Enum, JobVariableEditorViewModel.SupportedKinds);
        Assert.DoesNotContain(ResultValueKind.Image, JobVariableEditorViewModel.SupportedKinds);
        Assert.DoesNotContain(ResultValueKind.ResultObject, JobVariableEditorViewModel.SupportedKinds);
        Assert.DoesNotContain(ResultValueKind.Detection, JobVariableEditorViewModel.SupportedKinds);
        Assert.DoesNotContain(ResultValueKind.ProcessReference, JobVariableEditorViewModel.SupportedKinds);

        var legacyVariable = new JobVariable
        {
            ValueKind = ResultValueKind.ResultObject,
            Value = new System.Text.Json.Nodes.JsonObject { ["value"] = 42 }
        };
        var editor = new JobVariableEditorViewModel(legacyVariable, _ => { });

        Assert.Equal(ResultValueKind.ResultObject, editor.SelectedKind.Kind);
        Assert.Equal(42, legacyVariable.Value!["value"]!.GetValue<int>());

        foreach (var legacyKind in new[] { ResultValueKind.Enum, ResultValueKind.Image })
        {
            var legacyEditor = new JobVariableEditorViewModel(new JobVariable
            {
                ValueKind = legacyKind,
                Value = JsonValue.Create(string.Empty)
            }, _ => { });
            Assert.Equal(legacyKind, legacyEditor.SelectedKind.Kind);
        }
    }

    [Fact]
    public void ValueReferencePicker_OffersOnlyApprovedVariableKindsButLoadsLegacyReferences()
    {
        var text = Variable("Text", ResultValueKind.Text);
        var color = Variable("Color", ResultValueKind.Color);
        color.Value = JsonValue.Create("#123456");
        var file = Variable("File", ResultValueKind.FilePath);
        var legacyEnum = Variable("Legacy enum", ResultValueKind.Enum);
        var legacyImage = Variable("Legacy image", ResultValueKind.Image);
        var contract = new StepInputDescriptor(
            "value", true, MissingValuePolicy.FailStep, CollectionConsumptionMode.NotApplicable,
            new AcceptedResultShape(ResultValueKind.Text, ResultCardinality.Single),
            new AcceptedResultShape(ResultValueKind.Color, ResultCardinality.Single),
            new AcceptedResultShape(ResultValueKind.FilePath, ResultCardinality.Single),
            new AcceptedResultShape(ResultValueKind.Enum, ResultCardinality.Single),
            new AcceptedResultShape(ResultValueKind.Image, ResultCardinality.Single))
        {
            AllowedProviderIds = new HashSet<string> { ValueProviderIds.JobVariable }
        };
        var picker = new ValueReferencePickerViewModel(
            [], contract, false, [text, color, file, legacyEnum, legacyImage]);

        picker.UseJobVariableCommand.Execute(null);

        Assert.Contains(picker.SelectionTree, node => node.DisplayName == text.Name);
        Assert.Contains(picker.SelectionTree, node => node.DisplayName == color.Name);
        Assert.Contains(picker.SelectionTree, node => node.DisplayName == file.Name);
        Assert.DoesNotContain(picker.SelectionTree, node => node.DisplayName == legacyEnum.Name);
        Assert.DoesNotContain(picker.SelectionTree, node => node.DisplayName == legacyImage.Name);
        Assert.Equal("#123456", picker.SelectionTree.Single(node => node.DisplayName == color.Name).ColorPreview);

        picker.Load(new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = legacyEnum.Id.ToString("D")
        });
        Assert.Equal(legacyEnum.Id, picker.SelectedJobVariable?.Id);

        static JobVariable Variable(string name, ResultValueKind kind) => new()
        {
            Name = name,
            Scope = JobVariableScope.Shared,
            ValueKind = kind,
            Value = JsonValue.Create(string.Empty)
        };
    }

    [Fact]
    public void JobVariableEditor_StoresBooleanColorFilePathAndFullTimestampValues()
    {
        var variable = new JobVariable
        {
            ValueKind = ResultValueKind.DateTime,
            Value = JsonValue.Create(new DateTime(2026, 8, 24, 14, 35, 12))
        };
        var editor = new JobVariableEditorViewModel(variable, _ => { });

        Assert.Equal(14, editor.DateTimeValue.Hour);
        Assert.Equal(35, editor.DateTimeValue.Minute);

        editor.SelectedKind = editor.KindOptions.Single(option => option.Kind == ResultValueKind.Boolean);
        Assert.Equal(2, editor.BooleanOptions.Count);
        editor.SelectedBooleanOption = editor.BooleanOptions.Single(option => option.Value);
        Assert.True(editor.BooleanValue);
        Assert.True(variable.Value!.GetValue<bool>());

        editor.SelectedKind = editor.KindOptions.Single(option => option.Kind == ResultValueKind.Color);
        editor.ColorValue = System.Windows.Media.Color.FromRgb(0x12, 0xAB, 0xEF);
        Assert.Equal("#12ABEF", variable.Value!.GetValue<string>());

        editor.SelectedKind = editor.KindOptions.Single(option => option.Kind == ResultValueKind.FilePath);
        editor.FilePath = @"C:\Data\input.txt";
        Assert.Equal(@"C:\Data\input.txt", variable.Value!.GetValue<string>());
    }

    [Fact]
    public void ValueReferencePicker_RespectsProviderRestrictionsAndExplainsHiddenValues()
    {
        var secret = new ValueProviderSourceDescriptor(
            ValueProviderIds.Secret, Guid.NewGuid().ToString("D"), "Token", string.Empty,
            ResultValueKind.Text, ResultCardinality.Single, IsSensitive: true);
        var contract = StepInputContractRegistry.Get(typeof(ShowTextStep), "text")! with
        {
            AllowedProviderIds = new HashSet<string>
            {
                ValueProviderIds.JobVariable,
                ValueProviderIds.StepResult
            }
        };

        var picker = new ValueReferencePickerViewModel(
            [], contract, false, providerSources: [secret]);
        picker.SelectSourceKind(StepInputSourceKind.Secret);

        Assert.DoesNotContain(picker.SelectionTree, node => node.DisplayName == "Token");
        Assert.True(picker.HasIncompatible);
        picker.ShowIncompatible = true;
        Assert.DoesNotContain(picker.SelectionTree, node => node.DisplayName == "Token");
    }

    [Fact]
    public void UnifiedDataInputEditors_DefaultToDirectValuesWhileLegacyModelsKeepLiteralDefaults()
    {
        Assert.Equal(ShowTextSource.TaskResult,
            new ShowTextStepDefinition().CreateDefaultStep().Settings.TextSource);
        var fileSystem = new FileSystemOperationStepDefinition().CreateDefaultStep();
        Assert.Equal(FileSystemPathSource.ExplicitPath, fileSystem.Settings.SourceMode);
        Assert.Equal(FileSystemPathSource.ExplicitPath, fileSystem.Settings.TargetMode);

        var owner = new System.Collections.ObjectModel.ObservableCollection<PointEntryViewModel>();
        Assert.Equal(PointEntrySource.Manual, new PointEntryViewModel(owner, []).Source);

        Assert.Equal(ShowTextSource.ExplicitText, new ShowTextSettings().TextSource);
        Assert.Equal(FileSystemPathSource.ExplicitPath, new FileSystemOperationSettings().SourceMode);
    }

    [Fact]
    public void PointAndOverlayInputs_OfferCompatibleJobVariables()
    {
        var pointVariable = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            Name = "Target point",
            ValueKind = ResultValueKind.Point,
            Value = new System.Text.Json.Nodes.JsonObject { ["x"] = 10, ["y"] = 20 }
        };
        var pointEditor = new GeneratedPointEntryListEditorViewModel(null, [], [pointVariable]);
        var pointEntry = Assert.Single(pointEditor.Points);
        var pointSource = pointEntry.PointsSource;
        Assert.True(pointSource.CanUseJobVariables);
        Assert.False(pointSource.CanUseSecrets);
        Assert.True(pointEntry.WholeValueSource.UsesIndividualValues);

        var detectionContract = StepInputContractRegistry.Get(typeof(ShowOnDesktopStep), "detections")!;
        var textContract = StepInputContractRegistry.Get(typeof(ShowOnDesktopStep), "text")!;
        var overlay = new GeneratedVisualOverlayEditorViewModel(
            null, [], detectionContract, textContract, false, variables: [pointVariable]);
        overlay.AddOverlayDetectionCommand.Execute(null);
        var row = Assert.Single(overlay.OverlayDetectionRows);

        Assert.True(row.Source.CanUseJobVariables);
        Assert.True(row.Source.CanUseStepResults);
        Assert.True(row.SourceField.ShowsInputSourceSelector);
        Assert.False(row.Source.CanUseSecrets);
        Assert.False(row.Source.ToBinding().IsConfigured);

    }

    [Fact]
    public void AddStepDialog_OverlayTextOffersDirectVariableAndResultSources()
    {
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]), [],
            cameraCaptureService: new CameraDefinitionTestService());
        viewModel.SelectedType = "ShowOnDesktop";
        var overlay = Assert.Single(viewModel.GeneratedEditor!.Fields).VisualOverlayEditor!;

        overlay.AddOverlayTextCommand.Execute(null);

        var text = Assert.Single(overlay.OverlayTextRows).TextSourceField!;
        Assert.True(text.InputReferenceEditor!.Picker.CanUseDirectValue);
        Assert.True(text.InputReferenceEditor.Picker.CanUseJobVariables);
        Assert.True(text.InputReferenceEditor.Picker.CanUseStepResults);
        Assert.True(text.ShowsInputSourceSelector);
        Assert.True(text.InputReferenceEditor.Picker.IsDirectSource);
        Assert.True(text.IsInlineStepValue);
        Assert.True(text.ShowsDirectInput);
        Assert.False(text.ShowsInputSourcePicker);
    }

    [Fact]
    public void AddStepDialog_EditLoadsVisualOverlayFromStoredLocalValue()
    {
        const string sourceStepId = "ocr-source";
        var detection = ResultBinding.ForStepResult(sourceStepId, "words.bounding_box");
        var storedOverlay = new LocalValue
        {
            OwnerStepId = "show-image",
            InputPath = ShowImageStepDefinition.OverlayFieldId,
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonSerializer.SerializeToNode(new VisualOverlaySettings
            {
                DetectionResults = [detection]
            })
        };
        var existing = new ShowImageStep { Id = "show-image" };
        existing.Inputs[ShowImageStepDefinition.OverlayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = storedOverlay.Id.ToString("D"),
            SchemaId = ValueBindingSchemaRegistry.VisualOverlay
        };
        var viewModel = new AddJobStepDialogViewModel(
            new ControllableJobExecutor([]),
            [new OcrStep { Id = sourceStepId }],
            cameraCaptureService: new CameraDefinitionTestService(),
            localValues: [storedOverlay]);

        Assert.True(viewModel.TryLoadGeneratedStep(existing));

        var overlay = viewModel.GeneratedEditor!.Fields.Single(field =>
            field.Descriptor.Id == ShowImageStepDefinition.OverlayFieldId).VisualOverlayEditor!;
        var row = Assert.Single(overlay.OverlayDetectionRows);
        Assert.Equal(ValueProviderIds.StepResult, row.Source.ToBinding().ProviderId);
        Assert.Equal(detection.SourceId, row.Source.ToBinding().SourceId);
    }

    [Fact]
    public void GeneratedEditor_ResolvesInitialValueBeforeEverySpecializedResolver()
    {
        var observed = new List<JsonNode?>();
        JsonNode? Capture(JsonNode? value)
        {
            observed.Add(value);
            return value;
        }

        var editor = new GeneratedStepEditorViewModel(
            new TimeoutStepDefinition(),
            processTargetResolver: (_, value) => { Capture(value); return null; },
            resultBindingResolver: (_, value) => { Capture(value); return null; },
            cameraResolver: (_, value) => { Capture(value); return null; },
            visualOverlayResolver: (_, value) => { Capture(value); return null; },
            roiResolver: (_, value) => { Capture(value); return null; },
            yoloResolver: (_, value) => { Capture(value); return null; },
            conditionResolver: (_, value) => { Capture(value); return null; },
            windowsCapabilityResolver: (_, value) => { Capture(value); return null; },
            screenPointResolver: (_, value) => { Capture(value); return null; },
            userChoiceOptionsResolver: (_, value) => { Capture(value); return null; },
            pointEntryListResolver: (_, value) => { Capture(value); return null; },
            axisExpressionListResolver: (_, value) => { Capture(value); return null; },
            initialValueResolver: (_, _) => JsonValue.Create(73));

        Assert.Equal(73, Assert.Single(editor.Fields).IntegerValue);
        Assert.Equal(12, observed.Count);
        Assert.All(observed, value => Assert.Equal(73, value!.GetValue<int>()));
    }

    [Fact]
    public void AddStepDialog_EditLoadsConditionsAndOptionRowsFromStoredLocalValues()
    {
        var source = new OcrStep { Id = "ocr-source" };
        var textSource = ResultBinding.ForStepResult(source.Id, "text");
        var conditions = new IfConditionSettings
        {
            MatchMode = ConditionMatchMode.Any,
            Conditions =
            [
                new StepCondition
                {
                    ProviderId = textSource.ProviderId,
                    SourceId = textSource.SourceId,
                    Operator = ConditionOperator.IsEmpty
                },
                new StepCondition
                {
                    ProviderId = textSource.ProviderId,
                    SourceId = textSource.SourceId,
                    Operator = ConditionOperator.IsNotEmpty
                }
            ]
        };
        var conditionEditor = LoadStoredField(
            new IfStep { Id = "if-step" },
            IfStepDefinition.ConditionsFieldId,
            JsonSerializer.SerializeToNode(conditions),
            precedingSteps: [source]).ConditionEditor!;

        Assert.True(conditionEditor.IsAny);
        Assert.Equal(
            [ConditionOperator.IsEmpty, ConditionOperator.IsNotEmpty],
            conditionEditor.Conditions.Select(row => row.SelectedOperator));

        var options = new[]
        {
            new StepUserChoiceOptionValue("one", "First", "1"),
            new StepUserChoiceOptionValue("two", "Second", "2"),
            new StepUserChoiceOptionValue("three", "Third", "3")
        };
        var optionEditor = LoadStoredField(
            new UserChoiceStep { Id = "choice-step" },
            UserChoiceStepDefinition.OptionsFieldId,
            JsonSerializer.SerializeToNode(options)).UserChoiceOptionsEditor!;

        Assert.Equal(["one", "two", "three"], optionEditor.Options.Select(option => option.Id));
        Assert.Equal(["First", "Second", "Third"], optionEditor.Options.Select(option => option.Label));
        Assert.Equal(["1", "2", "3"], optionEditor.Options.Select(option => option.Value));
    }

    [Fact]
    public async Task AddStepDialog_EditLoadsCameraAndJobChoiceFromStoredLocalValues()
    {
        var camera = LoadStoredField(
            new CameraCaptureStep { Id = "camera-step" },
            CameraCaptureStepDefinition.CameraFieldId,
            JsonSerializer.SerializeToNode(new StepCameraSelectionValue(
                "camera-1", "Test camera", CameraQualityMode.Specific.ToString(),
                1280, 720, 25, "YUY2"))).CameraEditor!;

        await camera.Initialization;
        await camera.QualityLoading;
        Assert.Equal("camera-1", camera.SelectedCamera?.Id);
        Assert.Equal(CameraQualityMode.Specific, camera.SelectedQuality?.QualityMode);
        Assert.Equal(1280, camera.SelectedQuality?.Mode?.Width);

        var selectedJob = new Job { Id = Guid.NewGuid(), Name = "Stored child job" };
        var jobField = LoadStoredField(
            new JobExecutionStep { Id = "job-step" },
            JobExecutionStepDefinition.JobFieldId,
            JsonSerializer.SerializeToNode(new StepReferenceValue(
                selectedJob.Id.ToString("D"), selectedJob.Name)),
            executor: new ControllableJobExecutor([selectedJob]),
            valueKind: ResultValueKind.JobReference);

        Assert.Equal(selectedJob.Id.ToString("D"), jobField.SelectedChoice?.Value.Id);
    }

    [Fact]
    public async Task AddStepDialog_EditLoadsRemainingSpecializedEditorsFromStoredLocalValues()
    {
        var processSource = ResultBinding.ForStepResult("process-source", "process");
        var process = LoadStoredField(
            new ActiveProcessStep { Id = "active-process" },
            ActiveProcessStepDefinition.ProcessTargetFieldId,
            JsonSerializer.SerializeToNode(new StepProcessSelectorValue(
                JsonSerializer.SerializeToNode(processSource), "fallback", "", "Editor")))
            .ProcessTargetEditor!;
        Assert.True(process.UseProcessReference);
        Assert.Equal(processSource.SourceId, process.WholeValueSource.ToBinding().SourceId);

        var roi = LoadStoredField(
            new TemplateMatchingStep { Id = "template" },
            ImageDetectionStepDefinitionSupport.RoiFieldId,
            JsonSerializer.SerializeToNode(new StepRoiSelectionValue(true, 11, 22, 333, 444, null)))
            .RoiEditor!;
        Assert.True(roi.IsRoiEnabled);
        Assert.Equal((11, 22, 333, 444), (roi.X, roi.Y, roi.RoiWidth, roi.RoiHeight));

        var pointSource = ResultBinding.ForStepResult("point-source", "point");
        var origin = LoadStoredField(
            new KlickOnPoint3DStep { Id = "click-3d" },
            KlickOnPoint3DStepDefinition.OriginFieldId,
            JsonSerializer.SerializeToNode(new StepScreenPointSelectionValue(
                2, 123, 456, KlickOnPoint3DSettings.MonitorLocalCoordinates,
                JsonSerializer.SerializeToNode(pointSource))))
            .ScreenPointEditor!;
        Assert.True(origin.WholeValueSource.UsesReference);
        Assert.Equal(pointSource.SourceId, origin.WholeValueSource.ToBinding().SourceId);

        var capability = LoadStoredField(
            new WindowsSettingChangeStep { Id = "windows-setting" },
            WindowsStateQueryStepDefinition.CapabilityFieldId,
            JsonSerializer.SerializeToNode(new StepWindowsCapabilitySelectionValue(
                "audio.master_volume", new Dictionary<string, string?> { ["value"] = "64" })))
            .WindowsCapabilityEditor!;
        Assert.Equal("audio.master_volume", capability.Picker.SelectedCapability?.Id);
        Assert.Equal("64", capability.Picker.ToDictionary()["value"]);

        var points = LoadStoredField(
            new PointComparisonStep { Id = "point-comparison" },
            PointComparisonStepDefinition.PointsFieldId,
            JsonSerializer.SerializeToNode(new[]
            {
                new StepPointEntryValue("Manual", 10, 20, null),
                new StepPointEntryValue("Manual", 30, 40, null)
            })).PointEntryListEditor!;
        Assert.Equal([(10, 20), (30, 40)], points.Points.Select(point => (point.ManualX, point.ManualY)));

        var expressions = LoadStoredField(
            new PointComparisonStep { Id = "expression-comparison" },
            PointComparisonStepDefinition.ExpressionsFieldId,
            JsonSerializer.SerializeToNode(new[]
            {
                new StepAxisExpressionValue("X", "GreaterThan", 5),
                new StepAxisExpressionValue("Y", "LessThanOrEqual", 9)
            })).AxisExpressionListEditor!;
        Assert.Equal(
            [("X", PointAxisOperator.GreaterThan, 5), ("Y", PointAxisOperator.LessThanOrEqual, 9)],
            expressions.Expressions.Select(expression => (expression.Axis, expression.Operator, expression.Value)));

        var yolo = LoadStoredField(
            new YOLODetectionStep { Id = "yolo" },
            YoloDetectionStepDefinition.SelectionFieldId,
            JsonSerializer.SerializeToNode(new StepYoloSelectionValue("stored-model", "stored-class")))
            .YoloEditor!;
        await yolo.Initialization;
        Assert.Equal("stored-model", yolo.Model);
        Assert.Equal("stored-class", yolo.ClassName);
    }

    [Fact]
    public void VisualOverlayDefinition_PreservesProviderReferences()
    {
        var variableId = Guid.NewGuid();
        var existing = new ShowOnDesktopStep
        {
            Settings = new ShowOnDesktopSettings
            {
                Overlay = new VisualOverlaySettings
                {
                    DetectionResults =
                    [
                        new ResultBinding
                        {
                            ProviderId = ValueProviderIds.JobVariable,
                            SourceId = variableId.ToString("D")
                        }
                    ]
                }
            }
        };
        var definition = new ShowOnDesktopStepDefinition();

        var updated = Assert.IsType<ShowOnDesktopStep>(
            definition.ApplyDraft(definition.CreateDraft(existing), existing));
        var binding = Assert.Single(updated.Settings.Overlay.DetectionResults);

        Assert.Equal(ValueProviderIds.JobVariable, binding.ProviderId);
        Assert.Equal(variableId.ToString("D"), binding.SourceId);
        Assert.Null(binding.LegacySourceStepId);
    }

    [Fact]
    public void GeneratedEditor_CreatesPredictMovementWithCompatiblePointBinding()
    {
        var contract = StepInputContractRegistry.Get(typeof(PredictMovementStep), "points")!;
        var resultType = StepResultMetadata.ResultTypes.First(type =>
            contract.FindPreferredProperty(type.Properties) is not null);
        var picker = new ValueReferencePickerViewModel(
            [new SourceStepItem("detection", "Detection", resultType)], contract, true);
        var editor = new GeneratedStepEditorViewModel(
            new PredictMovementStepDefinition(),
            resultBindingResolver: (_, value) => new GeneratedResultBindingEditorViewModel(value, picker));
        var model = editor.Fields.Single(field =>
            field.Descriptor.Id == PredictMovementStepDefinition.PredictionModelFieldId);
        var confidence = editor.Fields.Single(field =>
            field.Descriptor.Id == PredictMovementStepDefinition.MinimumConfidenceFieldId);

        model.SelectedEnumOption = model.EnumOptions.Single(option => option.Value == "Acceleration");
        confidence.NumberValue = 0.8;

        Assert.True(editor.TryCreateStep(out var created));
        var prediction = Assert.IsType<PredictMovementStep>(created);
        Assert.Equal("Acceleration", prediction.Settings.PredictionModel);
        Assert.Equal("detection", prediction.Settings.PointsSource.SourceStepId);
        Assert.Equal(0.8, prediction.Settings.MinimumConfidence, 3);
        Assert.Equal(0, prediction.Settings.PredictionMs);
        Assert.Equal("Execution", prediction.Settings.TimeBasis);
    }

    [Fact]
    public void GeneratedEditor_CreatesKlickOnPointWithLocalizedEnumValue()
    {
        var contract = StepInputContractRegistry.Get(typeof(KlickOnPointStep), "points")!;
        var resultType = StepResultMetadata.ResultTypes.First(type =>
            contract.FindPreferredProperty(type.Properties) is not null);
        var picker = new ValueReferencePickerViewModel(
            [new SourceStepItem("prediction", "Prediction", resultType)], contract, true);
        var editor = new GeneratedStepEditorViewModel(
            new KlickOnPointStepDefinition(),
            resultBindingResolver: (_, value) => new GeneratedResultBindingEditorViewModel(value, picker));
        var clickType = editor.Fields.Single(field =>
            field.Descriptor.Id == KlickOnPointStepDefinition.ClickTypeFieldId);
        var doubleClick = editor.Fields.Single(field =>
            field.Descriptor.Id == KlickOnPointStepDefinition.DoubleClickFieldId);

        clickType.SelectedEnumOption = clickType.EnumOptions.Single(option => option.Value == "middle");
        doubleClick.BooleanValue = true;

        Assert.True(editor.TryCreateStep(out var created));
        var click = Assert.IsType<KlickOnPointStep>(created);
        Assert.Equal("middle", click.Settings.ClickType);
        Assert.True(click.Settings.DoubleClick);
        Assert.Equal("prediction", click.Settings.PointsSource.SourceStepId);
    }

    [Fact]
    public void GeneratedEditor_UpdatesConditionalFileSystemFieldsAndCreatesMoveStep()
    {
        var editor = new GeneratedStepEditorViewModel(new FileSystemOperationStepDefinition());
        var operation = editor.Fields.Single(field =>
            field.Descriptor.Id == FileSystemOperationStepDefinition.OperationFieldId);
        var sourcePath = editor.Fields.Single(field =>
            field.Descriptor.Id == FileSystemOperationStepDefinition.SourcePathFieldId);
        var targetPath = editor.Fields.Single(field =>
            field.Descriptor.Id == FileSystemOperationStepDefinition.TargetPathFieldId);
        var newName = editor.Fields.Single(field =>
            field.Descriptor.Id == FileSystemOperationStepDefinition.NewNameFieldId);

        Assert.True(sourcePath.UsesDirectoryPicker);
        Assert.True(targetPath.UsesDirectoryPicker);
        Assert.True(targetPath.IsVisible);
        Assert.False(newName.IsVisible);
        operation.SelectedEnumOption = operation.EnumOptions.Single(option => option.Value == "Rename");
        Assert.False(targetPath.IsVisible);
        Assert.True(newName.IsVisible);

        operation.SelectedEnumOption = operation.EnumOptions.Single(option => option.Value == "Move");
        sourcePath.InputText = @"C:\Source";
        targetPath.InputText = @"C:\Target";

        Assert.True(targetPath.IsVisible);
        Assert.True(editor.TryCreateStep(out var created));
        var move = Assert.IsType<FileSystemOperationStep>(created);
        Assert.Equal(FileSystemOperation.Move, move.Settings.Operation);
        Assert.Equal(@"C:\Source", move.Settings.SourcePath);
        Assert.Equal(@"C:\Target", move.Settings.TargetPath);
    }

    [Fact]
    public void GeneratedEditor_CreatesParameterlessElseAndEndIfSteps()
    {
        var elseEditor = new GeneratedStepEditorViewModel(new ElseStepDefinition());
        var endIfEditor = new GeneratedStepEditorViewModel(new EndIfStepDefinition());

        Assert.Empty(elseEditor.Fields);
        Assert.True(elseEditor.HasEditorDescription);
        Assert.True(elseEditor.TryCreateStep(out var createdElse));
        Assert.IsType<ElseStep>(createdElse);

        Assert.Empty(endIfEditor.Fields);
        Assert.True(endIfEditor.HasEditorDescription);
        Assert.True(endIfEditor.TryCreateStep(out var createdEndIf));
        Assert.IsType<EndIfStep>(createdEndIf);
    }

    [Fact]
    public async Task GeneratedEditor_LoadsCameraChoicesAndCreatesSelectedQuality()
    {
        using var service = new CameraDefinitionTestService();
        GeneratedCameraEditorViewModel? cameraEditor = null;
        var editor = new GeneratedStepEditorViewModel(
            new CameraCaptureStepDefinition(),
            cameraResolver: (_, value) => cameraEditor = new GeneratedCameraEditorViewModel(value, service));
        var field = Assert.Single(editor.Fields);

        Assert.True(field.UsesCameraPicker);
        Assert.False(field.UsesTextInput);
        Assert.NotNull(cameraEditor);
        await cameraEditor.Initialization;
        await cameraEditor.QualityLoading;

        Assert.Equal("camera-1", cameraEditor.SelectedCamera?.Id);
        cameraEditor.SelectedQuality = cameraEditor.Qualities.Single(choice =>
            choice.QualityMode == CameraQualityMode.Specific);

        Assert.True(editor.TryCreateStep(out var created));
        var camera = Assert.IsType<CameraCaptureStep>(created);
        Assert.Equal("camera-1", camera.Settings.CameraId);
        Assert.Equal(CameraQualityMode.Specific, camera.Settings.QualityMode);
        Assert.Equal(1280, camera.Settings.Width);
        Assert.Equal(720, camera.Settings.Height);
        Assert.Equal(25, camera.Settings.FramesPerSecond);
        Assert.Equal("YUY2", camera.Settings.PixelFormat);
    }

    [Fact]
    public void GeneratedEditor_CreatesDesktopOverlayThroughGenericAdapter()
    {
        var contract = StepInputContractRegistry.Get(typeof(ShowOnDesktopStep), "detections")!;
        var resultType = StepResultMetadata.ResultTypes.First(type =>
            contract.FindPreferredProperty(type.Properties) is not null);
        var property = contract.FindPreferredProperty(resultType.Properties)!;
        var sources = new[] { new SourceStepItem("detection", "Detection", resultType) };
        var settings = new VisualOverlaySettings
        {
            DetectionResults = [new ResultBinding
            {
                SourceStepId = "detection",
                PropertyId = property.StableId,
                PropertyPath = property.Name
            }]
        };
        GeneratedVisualOverlayEditorViewModel? overlayEditor = null;
        var editor = new GeneratedStepEditorViewModel(
            new ShowOnDesktopStepDefinition(),
            new ShowOnDesktopStep { Settings = new() { Overlay = settings } },
            visualOverlayResolver: (_, value) => overlayEditor = new(
                value,
                sources,
                StepInputContractRegistry.Get(typeof(ShowOnDesktopStep), "detections")!,
                StepInputContractRegistry.Get(typeof(ShowOnDesktopStep), "text")!,
                showDesktopOptions: true));
        var field = Assert.Single(editor.Fields);

        Assert.True(field.UsesVisualOverlay);
        Assert.False(field.UsesTextInput);
        Assert.NotNull(overlayEditor);
        Assert.True(overlayEditor.ShowOverlayDesktopOptions);
        Assert.Single(overlayEditor.OverlayDetectionRows);
        Assert.True(editor.TryCreateStep(out var created));
        Assert.Equal("detection", Assert.Single(
            Assert.IsType<ShowOnDesktopStep>(created).Settings.Overlay.DetectionResults).SourceStepId);
    }

    [Fact]
    public void GeneratedEditor_EditPreservesStepStateWithoutMutatingOriginal()
    {
        var existing = new BlockInputStep
        {
            Id = "existing-block",
            IsEnabled = false,
            IsBreakpoint = true,
            Settings = new BlockInputSettings { SafetyTimeoutSeconds = 30 }
        };
        var editor = new GeneratedStepEditorViewModel(new BlockInputStepDefinition(), existing);
        Assert.Single(editor.Fields).InputText = "90";

        Assert.True(editor.TryCreateStep(out var created));
        var updated = Assert.IsType<BlockInputStep>(created);

        Assert.NotSame(existing, updated);
        Assert.Equal("existing-block", updated.Id);
        Assert.False(updated.IsEnabled);
        Assert.True(updated.IsBreakpoint);
        Assert.Equal(90, updated.Settings.SafetyTimeoutSeconds);
        Assert.Equal(30, existing.Settings.SafetyTimeoutSeconds);
    }

    private static ResultBinding Binding(string stepId, string propertyPath) => new()
    {
        SourceStepId = stepId,
        PropertyPath = propertyPath
    };

    private static GeneratedStepFieldViewModel LoadStoredField(
        JobStep step,
        string fieldId,
        JsonNode? value,
        ControllableJobExecutor? executor = null,
        IReadOnlyList<JobStep>? precedingSteps = null,
        ResultValueKind valueKind = ResultValueKind.ResultObject)
    {
        var stored = new LocalValue
        {
            OwnerStepId = step.Id,
            InputPath = fieldId,
            ValueKind = valueKind,
            Value = value
        };
        step.Inputs[fieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = stored.Id.ToString("D")
        };
        var viewModel = new AddJobStepDialogViewModel(
            executor ?? new ControllableJobExecutor([]),
            precedingSteps ?? [],
            cameraCaptureService: new CameraDefinitionTestService(),
            localValues: [stored]);

        Assert.True(viewModel.TryLoadGeneratedStep(step));
        return viewModel.GeneratedEditor!.Fields.Single(field => field.Descriptor.Id == fieldId);
    }

    private sealed class CameraDefinitionTestService : ICameraCaptureService
    {
        public IReadOnlyList<CameraDeviceInfo> GetAvailableCameras() =>
            [new("camera-1", "Test camera", 0)];

        public IReadOnlyList<CameraCaptureMode> GetSupportedModes(string cameraId) =>
            [new(1280, 720, 25, "YUY2")];

        public Task<CameraCaptureFrame> CaptureAsync(
            string cameraId,
            CameraCaptureOptions options,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class DescriptorOverrideDefinition(IStepDefinition inner, StepDescriptor descriptor) : IStepDefinition
    {
        public Type StepType => inner.StepType;
        public StepDescriptor Descriptor { get; } = descriptor;
        public JobStep CreateDefault() => inner.CreateDefault();
        public StepDraft CreateDraft(JobStep? step = null) => inner.CreateDraft(step);
        public JobStep ApplyDraft(StepDraft draft, JobStep? existingStep = null) => inner.ApplyDraft(draft, existingStep);
        public IReadOnlyList<StepValidationIssue> ValidateDraft(StepDraft draft) => inner.ValidateDraft(draft);
        public IReadOnlyList<StepValidationIssue> ValidateDraft(StepDraft draft, StepValidationContext context) =>
            inner.ValidateDraft(draft, context);
        public IReadOnlyList<StepInputBinding> GetInputBindings(JobStep step) => inner.GetInputBindings(step);
    }
}
