using System.Text.Json;
using System.Text.Json.Nodes;
using DesktopAutomationApp.Services.Jobs;
using DesktopAutomationApp.ViewModels;
using OpenCvSharp;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class StepDefinitionCatalogTests
{
    [Fact]
    public void FileSystemEditor_DeclaresTwoIndependentChoiceGroups()
    {
        var section = new FileSystemOperationStepDefinition().Descriptor.Presentation.EditorSections
            .Single(candidate => candidate.Id == "general");
        var groups = section.EditorNodes!.OfType<StepChoiceGroupDescriptor>().ToArray();

        Assert.Equal(2, groups.Length);
        Assert.Equal(FileSystemOperationStepDefinition.SourceModeFieldId, groups[0].SelectionFieldId);
        Assert.Equal(FileSystemOperationStepDefinition.TargetModeFieldId, groups[1].SelectionFieldId);
        Assert.All(groups, group => Assert.Equal(2, group.Branches.Count));
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
    public void GeneratedEditor_KeepsMultipleChoiceGroupsIndependent()
    {
        var editor = new GeneratedStepEditorViewModel(new FileSystemOperationStepDefinition());
        var groups = editor.Sections.Single(section => section.Descriptor.Id == "general")
            .Nodes.OfType<GeneratedStepChoiceGroupViewModel>().ToArray();

        groups[0].SelectedBranch = groups[0].Branches[1];

        Assert.Equal("TaskResult", groups[0].SelectedBranch.Value);
        Assert.Equal("ExplicitPath", groups[1].SelectedBranch.Value);
        Assert.Single(groups[0].SelectedBranch.Children);
        Assert.Single(groups[1].SelectedBranch.Children);
    }

    [Fact]
    public void Catalog_RejectsInvalidChoiceGroupStructure()
    {
        var showText = new ShowTextStepDefinition();
        var general = showText.Descriptor.Presentation.EditorSections[0];
        var invalidGroup = new StepChoiceGroupDescriptor(ShowTextStepDefinition.TextSourceFieldId,
        [
            new("ExplicitText", "Ui.Step.IfEditor.LiteralValue", [new StepFieldNodeDescriptor(ShowTextStepDefinition.TextFieldId)]),
            new("ExplicitText", "Ui.Step.IfEditor.JobResultValue", [new StepFieldNodeDescriptor("missing")])
        ]);
        var invalid = new DescriptorOverrideDefinition(showText, showText.Descriptor with
        {
            TypeId = "show_text_invalid_choice_group",
            Presentation = showText.Descriptor.Presentation with
            {
                EditorSections = [general with { EditorNodes = [invalidGroup] }]
            }
        });

        Assert.Contains("choice group", Assert.Throws<InvalidOperationException>(() =>
            new StepDefinitionCatalog([invalid])).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExistingManualOrResultChoices_DeclareHierarchicalChoiceGroups()
    {
        var fields = new[]
        {
            new ShowTextStepDefinition().Descriptor.Fields.Single(field => field.Id == ShowTextStepDefinition.TextSourceFieldId),
            new FileSystemOperationStepDefinition().Descriptor.Fields.Single(field => field.Id == FileSystemOperationStepDefinition.SourceModeFieldId),
            new FileSystemOperationStepDefinition().Descriptor.Fields.Single(field => field.Id == FileSystemOperationStepDefinition.TargetModeFieldId),
            new PointComparisonStepDefinition().Descriptor.Fields.Single(field => field.Id == PointComparisonStepDefinition.ReferenceSourceFieldId)
        };

        Assert.All(fields, field => Assert.Null(field.EditorHint));
        Assert.All(fields, field => Assert.Equal(2, field.Options?.Count));
    }

    [Fact]
    public void GeneratedChoiceGroup_PreservesManualValueWhileSwitchingSources()
    {
        var editor = new GeneratedStepEditorViewModel(
            new ShowTextStepDefinition(),
            new ShowTextStep { Settings = new ShowTextSettings { Text = "Beibehalten" } });
        var manual = editor.Fields.Single(field => field.Descriptor.Id == ShowTextStepDefinition.TextFieldId);
        var result = editor.Fields.Single(field => field.Descriptor.Id == ShowTextStepDefinition.TextResultFieldId);
        var group = editor.Sections[0].Nodes.OfType<GeneratedStepChoiceGroupViewModel>().Single();

        Assert.True(manual.IsVisible);
        Assert.False(result.IsVisible);
        group.SelectedBranch = group.Branches[1];
        Assert.False(manual.IsVisible);
        Assert.True(result.IsVisible);
        group.SelectedBranch = group.Branches[0];

        Assert.Equal("Beibehalten", manual.InputText);
        Assert.True(manual.IsVisible);
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
    public void GeneratedChoiceGroup_GroupsItsDependentFields()
    {
        var editor = new GeneratedStepEditorViewModel(new PointComparisonStepDefinition());
        var group = editor.Sections.Single(section => section.Descriptor.Id == "offset")
            .Nodes.OfType<GeneratedStepChoiceGroupViewModel>().Single();

        var manualPoint = Assert.IsType<GeneratedStepPointFieldPairViewModel>(
            Assert.Single(group.Branches[0].Children));
        Assert.Equal(PointComparisonStepDefinition.ReferenceXFieldId, manualPoint.XField.Descriptor.Id);
        Assert.Equal(PointComparisonStepDefinition.ReferenceYFieldId, manualPoint.YField.Descriptor.Id);
        Assert.False(manualPoint.HasLabel);
        Assert.Equal(
            [PointComparisonStepDefinition.ReferencePointsFieldId],
            group.Branches[1].Children.Cast<GeneratedStepFieldNodeViewModel>().Select(node => node.Field.Descriptor.Id));
    }

    [Fact]
    public void ChoiceGroupStructure_DeterminesWhichBranchFieldsAreActive()
    {
        var definition = new ShowTextStepDefinition();
        var text = definition.Descriptor.Fields.Single(field => field.Id == ShowTextStepDefinition.TextFieldId);
        var result = definition.Descriptor.Fields.Single(field => field.Id == ShowTextStepDefinition.TextResultFieldId);
        var draft = definition.CreateDraft();
        draft.Values[ShowTextStepDefinition.TextFieldId] = JsonValue.Create("Visible text");
        draft.Values[ShowTextStepDefinition.TextResultFieldId] = null;

        Assert.Null(text.VisibleWhen);
        Assert.Null(result.VisibleWhen);
        Assert.Empty(definition.ValidateDraft(draft));

        draft.Values[ShowTextStepDefinition.TextSourceFieldId] = JsonValue.Create("TaskResult");

        Assert.Contains(definition.ValidateDraft(draft), issue =>
            issue.FieldId == ShowTextStepDefinition.TextResultFieldId
            && issue.Code == "StepValidation.Required");
    }

    [Fact]
    public void GeneratedChoiceGroup_HidesWithItsSelectionField()
    {
        var editor = new GeneratedStepEditorViewModel(new PointComparisonStepDefinition());
        var mode = editor.Fields.Single(field => field.Descriptor.Id == PointComparisonStepDefinition.ModeFieldId);
        var group = editor.Sections.Single(section => section.Descriptor.Id == "offset")
            .Nodes.OfType<GeneratedStepChoiceGroupViewModel>().Single();

        Assert.True(group.IsVisible);

        mode.SelectedEnumOption = mode.EnumOptions.Single(option => option.Value == "Expression");

        Assert.False(group.IsVisible);
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
                Padding = 35,
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
        Assert.Equal("detection", updated.Settings.BoundsSource.SourceStepId);
        Assert.Equal(0.75, updated.Settings.MinimumConfidence);

        draft.Values[DynamicRoiStepDefinition.MinimumConfidenceFieldId] =
            System.Text.Json.Nodes.JsonValue.Create(1.1);
        Assert.Equal("StepValidation.Maximum", Assert.Single(definition.ValidateDraft(draft)).Code);
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
    public void FileSystemOperationDefinition_RoundTripsResultSourcesAndValidatesOperationSpecificFields()
    {
        var definition = new FileSystemOperationStepDefinition();
        var existing = new FileSystemOperationStep
        {
            Settings = new FileSystemOperationSettings
            {
                Operation = FileSystemOperation.Move,
                SourceMode = FileSystemPathSource.TaskResult,
                SourceResult = new ResultBinding { SourceStepId = "source", PropertyId = "path" },
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
        Assert.Equal("source", updated.Settings.SourceResult.SourceStepId);
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
            ["timeout", "block_input", "unblock_input", "end_job", "continue_job", "desktop_duplication", "script_execution", "get_process", "makro_execution", "job_execution", "active_process", "active_window", "terminate_process", "focus_process", "start_process", "dynamic_roi", "predict_movement", "klick_on_point", "klick_on_point_3d", "file_system_operation", "show_text", "user_choice", "point_comparison", "if", "else_if", "windows_state_query", "windows_setting_change", "else", "end_if", "camera_capture", "show_image", "show_on_desktop", "video_creation", "save_image", "template_matching", "color_detection", "yolo_detection", "keypoint_matching"],
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
        var input = Assert.Single(fileSystem.GetInputBindings(explicitPath));
        Assert.Equal("source", input.ContractId);
        Assert.Equal("source", input.Binding.SourceStepId);

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
        monitor.IntegerValue = 3;
        cursor.BooleanValue = true;

        Assert.True(editor.TryCreateStep(out var created));
        var capture = Assert.IsType<DesktopDuplicationStep>(created);
        Assert.Equal(3, capture.Settings.DesktopIdx);
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
            new ResultBindingPickerViewModel([], contract!, false),
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
            new ResultBindingPickerViewModel([], contract!, false),
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
            new ResultBindingPickerViewModel(
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
            new ResultBindingPickerViewModel(
                [],
                StepInputContractRegistry.Get(typeof(ActiveProcessStep), "process")!,
                false),
            ["notepad"]);

        Assert.Equal("notepad", editor.ProcessName);
        Assert.Empty(editor.WindowTitleContains);
    }

    [Fact]
    public void ProcessTargetEditor_ClearsWindowTitleWhenUsingProcessReference()
    {
        var editor = new GeneratedProcessTargetEditorViewModel(
            value: null,
            new ResultBindingPickerViewModel(
                [],
                StepInputContractRegistry.Get(typeof(ActiveProcessStep), "process")!,
                false),
            []);
        editor.ProcessName = "notepad";
        editor.WindowTitleContains = "Editor";
        editor.SelectedSourceOption = editor.SourceOptions.Single(option => option.Value == "JobResult");

        var value = editor.ToValue();

        Assert.Empty(value.ProcessName);
        Assert.Empty(value.WindowTitleContains);
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
            new ResultBindingPickerViewModel([], StepInputContractRegistry.Get(typeof(FocusProcessStep), "process")!, false),
            []);
        var editor = new GeneratedStepEditorViewModel(
            definition,
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
        processEditor.ProcessName = "notepad";

        Assert.True(editor.TryCreateStep(out var created));
        var focus = Assert.IsType<FocusProcessStep>(created);
        Assert.Equal(FocusProcessAction.Minimize, focus.Settings.Action);
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
            new ResultBindingPickerViewModel([], StepInputContractRegistry.Get(typeof(FocusProcessStep), "process")!, false),
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
        var picker = new ResultBindingPickerViewModel(
            [source],
            StepInputContractRegistry.Get(typeof(DynamicRoiStep), "bounds")!,
            true);
        var bindingEditor = new GeneratedResultBindingEditorViewModel(null, picker);
        var editor = new GeneratedStepEditorViewModel(
            new DynamicRoiStepDefinition(),
            resultBindingResolver: (_, _) => bindingEditor);
        var sourceField = editor.Fields.Single(field => field.Descriptor.Id == DynamicRoiStepDefinition.BoundsSourceFieldId);
        var confidence = editor.Fields.Single(field => field.Descriptor.Id == DynamicRoiStepDefinition.MinimumConfidenceFieldId);

        Assert.True(sourceField.UsesResultBindingPicker);
        Assert.True(confidence.UsesPercentagePicker);
        confidence.NumberValue = 0.6;

        Assert.True(editor.TryCreateStep(out var created));
        var dynamicRoi = Assert.IsType<DynamicRoiStep>(created);
        Assert.Equal("detection", dynamicRoi.Settings.BoundsSource.SourceStepId);
        Assert.Equal(0.6, dynamicRoi.Settings.MinimumConfidence, 3);
    }

    [Fact]
    public void GeneratedEditor_CreatesPredictMovementWithCompatiblePointBinding()
    {
        var contract = StepInputContractRegistry.Get(typeof(PredictMovementStep), "points")!;
        var resultType = StepResultMetadata.ResultTypes.First(type =>
            contract.FindPreferredProperty(type.Properties) is not null);
        var picker = new ResultBindingPickerViewModel(
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
        var picker = new ResultBindingPickerViewModel(
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

        Assert.True(sourcePath.UsesFileOrFolderPicker);
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
    public void GeneratedEditor_CreatesShowTextWithMultilineAndColorFields()
    {
        var editor = new GeneratedStepEditorViewModel(new ShowTextStepDefinition());
        var text = editor.Fields.Single(field => field.Descriptor.Id == ShowTextStepDefinition.TextFieldId);
        var color = editor.Fields.Single(field => field.Descriptor.Id == ShowTextStepDefinition.FontColorFieldId);
        var monitor = editor.Fields.Single(field => field.Descriptor.Id == ShowTextStepDefinition.DesktopFieldId);

        Assert.True(text.UsesMultilineTextInput);
        Assert.True(color.UsesColorPicker);
        Assert.True(monitor.UsesMonitorPicker);
        text.InputText = "First line\nSecond line";
        color.ColorValue = System.Windows.Media.Color.FromRgb(0x12, 0x34, 0x56);
        monitor.IntegerValue = 1;

        Assert.True(editor.TryCreateStep(out var created));
        var showText = Assert.IsType<ShowTextStep>(created);
        Assert.Equal("First line\nSecond line", showText.Settings.Text);
        Assert.Equal("#123456", showText.Settings.FontColor);
        Assert.Equal(1, showText.Settings.DesktopIndex);
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
        public IReadOnlyList<StepInputBinding> GetInputBindings(JobStep step) => inner.GetInputBindings(step);
    }
}
