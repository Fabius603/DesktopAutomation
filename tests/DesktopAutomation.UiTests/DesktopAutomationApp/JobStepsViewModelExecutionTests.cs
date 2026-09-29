using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Behaviors;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepsViewModelExecutionTests
{
    [Fact]
    public async Task ManuallyRestoredSetting_ClearsUnsavedState()
    {
        var job = new Job { Name = "Dirty state", Repeating = false, Steps = [new TimeoutStep()] };
        var viewModel = CreateViewModel(job);

        viewModel.IsRepeating = true;
        Assert.True(viewModel.HasUnsavedChanges);

        viewModel.IsRepeating = false;
        await viewModel.WaitForDirtyStateAsync();

        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
        Assert.False(viewModel.CancelCommand.CanExecute(null));
    }

    [Fact]
    public void StartJobCommand_StartsTheEditedJob()
    {
        var job = new Job
        {
            Name = "Header execution",
            Steps = [new TimeoutStep()]
        };
        var dispatcher = new RecordingJobDispatcher();
        var viewModel = CreateViewModel(job, dispatcher);

        Assert.True(viewModel.StartJobCommand.CanExecute(null));

        viewModel.StartJobCommand.Execute(null);

        Assert.Collection(dispatcher.StartedJobs,
            started => Assert.Equal(job.Id, started.Id));
    }

    [Fact]
    public async Task DuplicateStepCommand_CopiesSelectedStepWithNewIdentity()
    {
        var original = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 250 } };
        var job = new Job { Name = "Duplicate", Steps = [original] };
        var viewModel = CreateViewModel(job);
        viewModel.SelectedStep = original;
        var command = Assert.IsType<AsyncRelayCommand>(viewModel.DuplicateStepCommand);

        command.Execute(null);
        while (command.IsExecuting)
            await Task.Yield();

        Assert.Equal(2, viewModel.Steps.Count);
        var duplicate = Assert.IsType<TimeoutStep>(viewModel.Steps[1]);
        Assert.Equal(original.Settings.DelayMs, duplicate.Settings.DelayMs);
        Assert.NotEqual(original.Id, duplicate.Id);
    }

    [Fact]
    public void SingleSelection_UsesInlineEditorAndKeepsInvalidIntermediateInputOutOfJobDraft()
    {
        var step = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 250 } };
        var viewModel = CreateViewModel(new Job { Name = "Inline editor", Steps = [step] });
        viewModel.SetSelectedSteps([step], viewModel.Steps);
        Assert.True(viewModel.HasSingleSelectedStep);
        var editor = Assert.IsType<GeneratedStepEditorViewModel>(viewModel.SelectedGeneratedEditor);
        var delay = editor.Fields.Single(field => field.Descriptor.Id == TimeoutStepDefinition.DelayFieldId);

        delay.InputText = "not a number";

        Assert.True(viewModel.HasInlineEditorError);
        Assert.Equal(250, Assert.IsType<TimeoutStep>(viewModel.Steps[0]).Settings.DelayMs);
        Assert.Equal(step.Id, viewModel.Steps[0].Id);
        Assert.Same(step, viewModel.SelectedStep);
        Assert.Same(editor, viewModel.SelectedGeneratedEditor);
    }

    [Fact]
    public async Task InlineEditorReplacement_IgnoresTransientEmptyWpfSelectionAndKeepsEditorOpen()
    {
        var step = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 250 } };
        var viewModel = CreateViewModel(new Job { Name = "Inline editor", Steps = [step] });
        viewModel.SetSelectedSteps([step], viewModel.Steps);
        var editor = Assert.IsType<GeneratedStepEditorViewModel>(viewModel.SelectedGeneratedEditor);
        var delay = editor.Fields.Single(field => field.Descriptor.Id == TimeoutStepDefinition.DelayFieldId);
        viewModel.Steps.CollectionChanged += (_, args) =>
        {
            if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
                viewModel.SetSelectedSteps([], viewModel.Steps);
        };

        delay.IntegerValue = 500;
        await WaitUntilAsync(() =>
            Assert.IsType<TimeoutStep>(viewModel.Steps[0]).Settings.DelayMs == 500
            && !viewModel.IsMutationBusy);

        Assert.Same(viewModel.Steps[0], viewModel.SelectedStep);
        Assert.Contains(viewModel.SelectedStep, viewModel.SelectedSteps);
        Assert.Same(editor, viewModel.SelectedGeneratedEditor);
        Assert.True(viewModel.HasSingleSelectedStep);
    }

    [Fact]
    public async Task SingleSelection_ValidInlineEditPreservesIdentityAndCanBeUndoneAsOneSession()
    {
        var step = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 250 } };
        var viewModel = CreateViewModel(new Job { Name = "Inline editor", Steps = [step] });
        viewModel.SetSelectedSteps([step], viewModel.Steps);
        var editor = Assert.IsType<GeneratedStepEditorViewModel>(viewModel.SelectedGeneratedEditor);
        var delay = editor.Fields.Single(field => field.Descriptor.Id == TimeoutStepDefinition.DelayFieldId);

        delay.IntegerValue = 500;
        await WaitUntilAsync(() =>
            Assert.IsType<TimeoutStep>(viewModel.Steps[0]).Settings.DelayMs == 500
            && !viewModel.IsMutationBusy);

        Assert.Equal(step.Id, viewModel.Steps[0].Id);
        Assert.True(viewModel.CanUndo);
        var undo = Assert.IsType<AsyncRelayCommand>(viewModel.UndoCommand);
        undo.Execute(null);
        while (undo.IsExecuting) await Task.Yield();
        Assert.Equal(250, Assert.IsType<TimeoutStep>(viewModel.Steps[0]).Settings.DelayMs);
    }

    [Fact]
    public async Task DuplicateStepCommand_RemapsLocalValuesInsideStructuredBindings()
    {
        var original = new ActiveProcessStep();
        var target = new LocalValue
        {
            OwnerStepId = original.Id,
            InputPath = ActiveProcessStepDefinition.ProcessTargetFieldId,
            ValueKind = ResultValueKind.ResultObject,
            Value = System.Text.Json.JsonSerializer.SerializeToNode(
                new TaskAutomation.Contracts.Steps.StepProcessSelectorValue(null, "notepad", string.Empty, "Editor"))
        };
        var title = new LocalValue
        {
            OwnerStepId = original.Id,
            InputPath = $"{ActiveProcessStepDefinition.ProcessTargetFieldId}.window_title_contains",
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("Bericht")
        };
        original.Inputs[ActiveProcessStepDefinition.ProcessTargetFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = target.Id.ToString("D")
        };
        ValueBindingTree.Set(original.Inputs, title.InputPath, new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = title.Id.ToString("D")
        });
        Assert.True(BuiltInStepDefinitions.Instance.TryGetByType(typeof(ActiveProcessStep), out var definition));
        ValueBindingTree.ApplySchemas(original.Inputs, definition.Descriptor.Fields);
        var job = new Job
        {
            Name = "Structured duplicate",
            FormatVersion = Job.CurrentFormatVersion,
            Steps = [original],
            LocalValues = [target, title]
        };
        var viewModel = CreateViewModel(job);
        viewModel.SelectedStep = original;
        var command = Assert.IsType<AsyncRelayCommand>(viewModel.DuplicateStepCommand);

        command.Execute(null);
        while (command.IsExecuting) await Task.Yield();

        var duplicate = Assert.IsType<ActiveProcessStep>(viewModel.Steps[1]);
        var originalReferences = ValueBindingTree.EnumerateReferences(original.Inputs)
            .Where(item => item.Binding.ProviderId == ValueProviderIds.LocalValue).ToArray();
        var duplicateReferences = ValueBindingTree.EnumerateReferences(duplicate.Inputs)
            .Where(item => item.Binding.ProviderId == ValueProviderIds.LocalValue).ToArray();
        Assert.Equal(originalReferences.Select(item => item.Path), duplicateReferences.Select(item => item.Path));
        Assert.All(duplicateReferences, item =>
        {
            Assert.DoesNotContain(originalReferences, originalItem =>
                originalItem.Binding.SourceId == item.Binding.SourceId);
            var local = Assert.Single(job.LocalValues, candidate =>
                candidate.Id.ToString("D") == item.Binding.SourceId);
            Assert.Equal(duplicate.Id, local.OwnerStepId);
            Assert.Equal(item.Path, local.InputPath);
        });
    }

    [Fact]
    public async Task MultiSelection_DuplicatesInListOrderAndDisablesSingleEdit()
    {
        var first = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 100 } };
        var middle = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 200 } };
        var last = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 300 } };
        var viewModel = CreateViewModel(new Job { Name = "Multi", Steps = [first, middle, last] });
        viewModel.SetSelectedSteps([last, first], viewModel.Steps);

        Assert.False(viewModel.EditStepCommand.CanExecute(first));
        var command = Assert.IsType<AsyncRelayCommand>(viewModel.DuplicateStepCommand);
        command.Execute(null);
        while (command.IsExecuting) await Task.Yield();

        Assert.Equal([100, 200, 300, 100, 300], viewModel.Steps.Select(step => Assert.IsType<TimeoutStep>(step).Settings.DelayMs));
        Assert.Equal(2, viewModel.SelectedStepCount);
        Assert.DoesNotContain(viewModel.Steps.Skip(3), clone => clone.Id == first.Id || clone.Id == last.Id);
    }

    [Fact]
    public async Task MultiSelection_MovesAsBlockAndTogglesSharedState()
    {
        var first = new TimeoutStep();
        var second = new TimeoutStep();
        var third = new TimeoutStep { IsEnabled = false };
        var fourth = new TimeoutStep();
        var viewModel = CreateViewModel(new Job { Name = "Batch", Steps = [first, second, third, fourth] });
        viewModel.SetSelectedSteps([second, third], viewModel.Steps);

        var move = Assert.IsType<AsyncRelayCommand<JobStep?>>(viewModel.MoveStepUpCommand);
        move.Execute(second);
        while (move.IsExecuting) await Task.Yield();
        Assert.Equal([second, third, first, fourth], viewModel.Steps);

        var toggle = Assert.IsType<AsyncRelayCommand<JobStep?>>(viewModel.ToggleStepEnabledCommand);
        toggle.Execute(second);
        while (toggle.IsExecuting) await Task.Yield();
        Assert.True(second.IsEnabled);
        Assert.True(third.IsEnabled);

        var breakpoints = Assert.IsType<AsyncRelayCommand<JobStep?>>(viewModel.ToggleBreakpointCommand);
        breakpoints.Execute(second);
        while (breakpoints.IsExecuting) await Task.Yield();
        Assert.True(second.IsBreakpoint);
        Assert.True(third.IsBreakpoint);
    }

    [Fact]
    public async Task MultiSelection_MovesTogetherToAnotherJobPhase()
    {
        var first = new TimeoutStep();
        var second = new TimeoutStep();
        var remaining = new TimeoutStep();
        var viewModel = CreateViewModel(new Job { Name = "Phases", Steps = [first, second, remaining] });
        viewModel.SetSelectedSteps([first, second], viewModel.Steps);
        var command = Assert.IsType<AsyncRelayCommand<JobStep?>>(viewModel.MoveToStartSectionCommand);

        command.Execute(first);
        while (command.IsExecuting) await Task.Yield();

        Assert.Equal([first, second], viewModel.StartSteps);
        Assert.Equal([remaining], viewModel.Steps);
        Assert.Equal(2, viewModel.SelectedStepCount);
    }

    [Fact]
    public async Task MultiSelection_MovesCompleteConditionalStructureToAnotherPhase()
    {
        var untouchedBefore = new TimeoutStep();
        var conditional = new IfStep();
        var body = new TimeoutStep();
        var endIf = new EndIfStep();
        var selectedAfter = new TimeoutStep();
        var untouchedAfter = new TimeoutStep();
        var viewModel = CreateViewModel(new Job
        {
            Name = "Conditional batch",
            Steps = [untouchedBefore, conditional, body, endIf, selectedAfter, untouchedAfter]
        });
        viewModel.SetSelectedSteps([conditional, selectedAfter], viewModel.Steps);
        var command = Assert.IsType<AsyncRelayCommand<JobStep?>>(viewModel.MoveToStartSectionCommand);

        command.Execute(conditional);
        while (command.IsExecuting) await Task.Yield();

        Assert.Equal([conditional, body, endIf, selectedAfter], viewModel.StartSteps);
        Assert.Equal([untouchedBefore, untouchedAfter], viewModel.Steps);
        Assert.Equal(4, viewModel.SelectedStepCount);
    }

    [Fact]
    public void DragPreview_ExpandsControlFlowBlocksAndRejectsTargetsInsideTheMovingBlock()
    {
        var before = new TimeoutStep();
        var conditional = new IfStep();
        var body = new TimeoutStep();
        var endIf = new EndIfStep();
        var after = new TimeoutStep();
        var viewModel = CreateViewModel(new Job
        {
            Name = "Drag preview",
            Steps = [before, conditional, body, endIf, after]
        });

        viewModel.SetSelectedSteps([conditional], viewModel.Steps);
        var expanded = viewModel.DragIndicesResolver(new StepDragDrop.DragStartRequest(
            viewModel.Steps,
            SourceIndex: 1,
            SelectedIndices: [1]));

        Assert.Equal([1, 2, 3], expanded);
        Assert.True(viewModel.PreviewMoveValidator(new StepDragDrop.MoveRequest(
            viewModel.Steps,
            SourceIndex: 1,
            Target: viewModel.Steps,
            TargetIndex: 5,
            SourceIndices: expanded)));

        viewModel.SetSelectedSteps([conditional], viewModel.Steps);
        Assert.False(viewModel.PreviewMoveValidator(new StepDragDrop.MoveRequest(
            viewModel.Steps,
            SourceIndex: 1,
            Target: viewModel.Steps,
            TargetIndex: 2,
            SourceIndices: expanded)));
    }

    [Fact]
    public async Task DiscardCommand_RequiresConfirmation()
    {
        var job = new Job { Name = "Discard", Repeating = false, Steps = [new TimeoutStep()] };
        var dialog = new DialogServiceStub { ConfirmResult = false };
        var viewModel = CreateViewModel(job, dialog: dialog);
        viewModel.IsRepeating = true;
        var command = Assert.IsType<AsyncRelayCommand>(viewModel.CancelCommand);

        command.Execute(null);
        while (command.IsExecuting) await Task.Yield();
        Assert.True(viewModel.IsRepeating);

        dialog.ConfirmResult = true;
        command.Execute(null);
        while (command.IsExecuting) await Task.Yield();
        Assert.False(viewModel.IsRepeating);
        Assert.Equal(2, dialog.ConfirmCalls);
    }

    [Fact]
    public async Task DiscardCommand_RestoresSavedStepsAndResetsEditorState()
    {
        var original = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 250 } };
        var dialog = new DialogServiceStub { ConfirmResult = true };
        var viewModel = CreateViewModel(new Job { Name = "Discard steps", Steps = [original] }, dialog: dialog);
        viewModel.SelectedStep = original;
        var cancelNotifications = 0;
        viewModel.CancelCommand.CanExecuteChanged += (_, _) => cancelNotifications++;
        var duplicate = Assert.IsType<AsyncRelayCommand>(viewModel.DuplicateStepCommand);

        duplicate.Execute(null);
        while (duplicate.IsExecuting) await Task.Yield();
        await viewModel.WaitForDirtyStateAsync();

        Assert.True(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.CanUndo);
        Assert.True(cancelNotifications > 0);
        var discard = Assert.IsType<AsyncRelayCommand>(viewModel.CancelCommand);
        discard.Execute(null);
        while (discard.IsExecuting) await Task.Yield();

        var restored = Assert.IsType<TimeoutStep>(Assert.Single(viewModel.Steps));
        Assert.Equal(250, restored.Settings.DelayMs);
        Assert.Equal(original.Id, restored.Id);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.CanUndo);
        Assert.False(viewModel.CanRedo);
        Assert.Null(viewModel.SelectedStep);
        Assert.Empty(viewModel.SelectedSteps);
        Assert.Equal(1, dialog.ConfirmCalls);
    }

    [Fact]
    public async Task JobVariables_ParticipateInDirtyTrackingAndDiscard()
    {
        var original = new JobVariable
        {
            Name = "URL",
            Description = "Service endpoint",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("https://example.test")
        };
        var viewModel = CreateViewModel(new Job { Name = "Variables", Variables = [original] });

        var editor = Assert.Single(viewModel.JobVariables);
        editor.TextValue = "https://changed.test";
        await viewModel.WaitForDirtyStateAsync();

        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal("https://changed.test", original.Value!.GetValue<string>());

        viewModel.DiscardChanges();

        var restored = Assert.Single(viewModel.JobVariables);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal("https://example.test", restored.TextValue);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task DeleteVariableCommand_BlocksDeletionWhileVariableIsReferenced()
    {
        var variable = new JobVariable
        {
            Name = "URL",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("https://example.test")
        };
        var consumingStep = new ShowTextStep
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
        var dialog = new DialogServiceStub();
        var viewModel = CreateViewModel(
            new Job { Name = "Variables", Variables = [variable], Steps = [consumingStep] },
            dialog: dialog);
        var editor = Assert.Single(viewModel.JobVariables, candidate => candidate.Id == variable.Id);
        var variableCount = viewModel.JobVariables.Count;
        var command = Assert.IsType<AsyncRelayCommand<JobVariableEditorViewModel?>>(viewModel.DeleteVariableCommand);

        command.Execute(editor);
        while (command.IsExecuting) await Task.Yield();

        Assert.Equal(variableCount, viewModel.JobVariables.Count);
        Assert.Contains(viewModel.Job.Variables, candidate => candidate.Id == variable.Id);
        Assert.NotNull(dialog.LastError);
        Assert.Equal(0, dialog.ConfirmCalls);
    }

    [Fact]
    public void VariableDraft_DoesNotMutateJobUntilSelectedVariableIsApplied()
    {
        var variable = new JobVariable
        {
            Name = "Greeting",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("Hello")
        };
        var viewModel = CreateViewModel(new Job { Name = "Variables", Variables = [variable] });
        viewModel.BeginVariableDraftSession();
        var editor = Assert.Single(viewModel.JobVariables);
        viewModel.SelectedJobVariable = editor;

        editor.TextValue = "Draft";

        Assert.Equal("Hello", variable.Value!.GetValue<string>());
        Assert.True(editor.IsDirty);
        Assert.True(viewModel.HasVariableDraftChanges);
        Assert.Equal(1, viewModel.VariableDraftChangeCount);
        Assert.False(viewModel.ShowApplyAllVariableChanges);

        viewModel.ApplySelectedVariableCommand.Execute(null);

        Assert.Equal("Draft", variable.Value!.GetValue<string>());
        Assert.False(editor.IsDirty);
        Assert.False(viewModel.HasVariableDraftChanges);
    }

    [Fact]
    public void VariableDraft_CanApplyAllChangedVariablesTogether()
    {
        var first = new JobVariable
        {
            Name = "First",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("one")
        };
        var second = new JobVariable
        {
            Name = "Second",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("two")
        };
        var viewModel = CreateViewModel(new Job { Name = "Variables", Variables = [first, second] });
        viewModel.BeginVariableDraftSession();

        viewModel.JobVariables.Single(variable => variable.Id == first.Id).TextValue = "changed one";
        viewModel.JobVariables.Single(variable => variable.Id == second.Id).TextValue = "changed two";
        Assert.Equal(2, viewModel.VariableDraftChangeCount);
        Assert.True(viewModel.ShowApplyAllVariableChanges);
        viewModel.ApplyAllVariableChangesCommand.Execute(null);

        Assert.Equal("changed one", first.Value!.GetValue<string>());
        Assert.Equal("changed two", second.Value!.GetValue<string>());
        Assert.False(viewModel.HasVariableDraftChanges);
    }

    [Fact]
    public async Task VariableDraft_StagesCreationAndDeletionUntilApplyAll()
    {
        var existing = new JobVariable
        {
            Name = "Existing",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("value")
        };
        var viewModel = CreateViewModel(new Job { Name = "Variables", Variables = [existing] });
        viewModel.BeginVariableDraftSession();

        viewModel.AddVariableCommand.Execute(null);
        Assert.Single(viewModel.Job.Variables);
        Assert.Equal(2, viewModel.JobVariables.Count);

        var existingEditor = viewModel.JobVariables.Single(variable => variable.Id == existing.Id);
        var delete = Assert.IsType<AsyncRelayCommand<JobVariableEditorViewModel?>>(viewModel.DeleteVariableCommand);
        delete.Execute(existingEditor);
        while (delete.IsExecuting) await Task.Yield();

        Assert.Contains(viewModel.Job.Variables, variable => variable.Id == existing.Id);
        Assert.DoesNotContain(viewModel.JobVariables, variable => variable.Id == existing.Id);

        viewModel.ApplyAllVariableChangesCommand.Execute(null);

        Assert.DoesNotContain(viewModel.Job.Variables, variable => variable.Id == existing.Id);
        Assert.Single(viewModel.Job.Variables);
    }

    [Fact]
    public void VariableDraft_OnlyThisUsageCreatesDetachedVariable()
    {
        var variable = new JobVariable
        {
            Name = "Greeting",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("Hello")
        };
        var firstReference = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        };
        var secondReference = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        };
        var firstStep = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = firstReference
            }
        };
        var secondStep = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = secondReference
            }
        };
        var viewModel = CreateViewModel(new Job
        {
            Name = "Variables",
            Variables = [variable],
            Steps = [firstStep, secondStep]
        });
        viewModel.BeginVariableDraftSession();
        var editor = Assert.Single(viewModel.JobVariables);
        viewModel.SelectedJobVariable = editor;
        var selectedUsage = editor.UsageItems.Single(usage => ReferenceEquals(usage.Step, firstStep));
        var remainingUsage = editor.UsageItems.Single(usage => ReferenceEquals(usage.Step, secondStep));
        viewModel.SelectedVariableUsage = selectedUsage;
        Assert.True(viewModel.ApplyVariableToSelectedUsageCommand.CanExecute(null));
        editor.TextValue = "Only here";

        viewModel.ApplyVariableToSelectedUsageCommand.Execute(null);

        Assert.Equal("Hello", variable.Value!.GetValue<string>());
        Assert.Equal(variable.Id.ToString("D"), remainingUsage.Reference.SourceId);
        Assert.NotEqual(variable.Id.ToString("D"), selectedUsage.Reference.SourceId);
        var detached = Assert.Single(viewModel.Job.Variables,
            candidate => candidate.Id.ToString("D") == selectedUsage.Reference.SourceId);
        Assert.Equal("Only here", detached.Value!.GetValue<string>());
    }

    [Fact]
    public void VariableView_FiltersOnlyByTypeAndSearchesUsingStepName()
    {
        var used = new JobVariable
        {
            Name = "Greeting",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("Hello")
        };
        var unused = new JobVariable
        {
            Name = "Retries",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create(3)
        };
        var step = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = used.Id.ToString("D") }
            }
        };
        var viewModel = CreateViewModel(new Job { Name = "Variables", Variables = [used, unused], Steps = [step] });

        Assert.Equal(2, viewModel.FilteredJobVariables.Count);
        Assert.False(viewModel.JobVariables.Single(candidate => candidate.Id == used.Id).CanChangeKind);
        Assert.True(viewModel.JobVariables.Single(candidate => candidate.Id == unused.Id).CanChangeKind);

        viewModel.VariableSearchText = viewModel.JobVariables.Single(candidate => candidate.Id == used.Id).UsageSteps.Single();
        Assert.Contains(viewModel.FilteredJobVariables.Cast<JobVariableEditorViewModel>(), candidate => candidate.Id == used.Id);

        viewModel.VariableSearchText = "does not exist";
        Assert.True(viewModel.HasEmptyVariableFilterResult);

        viewModel.VariableSearchText = string.Empty;
        Assert.Contains(viewModel.FilteredJobVariables.Cast<JobVariableEditorViewModel>(), candidate => candidate.Id == unused.Id);

        viewModel.SelectedVariableTypeFilter = viewModel.VariableTypeFilterOptions.Single(option =>
            option.Kind == ResultValueKind.Integer);
        Assert.Equal(unused.Id, Assert.Single(viewModel.FilteredJobVariables.Cast<JobVariableEditorViewModel>()).Id);
        Assert.True(viewModel.HasTypeVariableFilter);

        viewModel.SelectedVariableTypeFilter = viewModel.VariableTypeFilterOptions[0];
        Assert.Equal(2, viewModel.FilteredJobVariables.Count);
        Assert.False(viewModel.HasActiveVariableFilters);
    }

    [Fact]
    public void VariableView_CanDuplicateSharedVariable()
    {
        var variable = new JobVariable
        {
            Name = "Retries",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Integer,
            Value = System.Text.Json.Nodes.JsonValue.Create(3)
        };
        var viewModel = CreateViewModel(new Job { Name = "Variables", Variables = [variable] });
        var editor = Assert.Single(viewModel.JobVariables);

        viewModel.DuplicateVariableCommand.Execute(editor);
        var copy = Assert.Single(viewModel.JobVariables, candidate => candidate.Id != editor.Id);
        Assert.Equal(3, copy.IntegerValue);

        Assert.True(editor.IsShared);
        Assert.True(copy.IsShared);
        Assert.Same(copy, viewModel.SelectedJobVariable);
    }

    [Fact]
    public void JobVariableEditing_KeepsFilteredCollectionSelectionAndTypeStable()
    {
        var first = new JobVariable
        {
            Name = "First",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("one")
        };
        var second = new JobVariable
        {
            Name = "Second",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("two")
        };
        var viewModel = CreateViewModel(new Job { Name = "Variables", Variables = [first, second] });
        var filteredVariables = viewModel.FilteredJobVariables;
        var editor = viewModel.JobVariables.Single(candidate => candidate.Id == second.Id);
        viewModel.SelectedJobVariable = editor;

        editor.TextValue = "changed";
        editor.SelectedKindValue = ResultValueKind.Integer;

        Assert.Same(filteredVariables, viewModel.FilteredJobVariables);
        Assert.Same(editor, viewModel.SelectedJobVariable);
        Assert.Contains(editor, viewModel.FilteredJobVariables.Cast<JobVariableEditorViewModel>());
        Assert.Equal(ResultValueKind.Integer, editor.SelectedKindValue);
        Assert.Equal(ResultValueKind.Integer, editor.SelectedKind.Kind);
    }

    [Fact]
    public void VariableUsage_NavigatesToTheConsumingStep()
    {
        var variable = new JobVariable
        {
            Name = "Greeting",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = System.Text.Json.Nodes.JsonValue.Create("Hello")
        };
        var first = new TimeoutStep();
        var consuming = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = variable.Id.ToString("D") }
            }
        };
        var viewModel = CreateViewModel(new Job
        {
            Name = "Variables",
            Variables = [variable],
            Steps = [first, consuming]
        });
        var editor = Assert.Single(viewModel.JobVariables);
        var usage = Assert.Single(editor.UsageItems);

        viewModel.OpenVariableUsageCommand.Execute(usage);

        Assert.Same(consuming, viewModel.SelectedStep);
        Assert.NotEmpty(usage.InputName);
        Assert.Contains("2", usage.StepName);
    }

    private static JobStepsViewModel CreateViewModel(
        Job job,
        RecordingJobDispatcher? dispatcher = null,
        DialogServiceStub? dialog = null) => new(
        job,
        new ControllableJobExecutor([job]),
        new JobApplicationServiceStub(job),
        dialog ?? new DialogServiceStub(),
        dispatcher ?? new RecordingJobDispatcher(),
        new NoOpCameraCaptureService());

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }

    private sealed class JobApplicationServiceStub(Job job) : IJobApplicationService
    {
        public IReadOnlyDictionary<string, Job> Jobs { get; } =
            new Dictionary<string, Job> { [job.Id.ToString()] = job };
        public Task<Job> CreateJobAsync(string name) => throw new NotSupportedException();
        public Task SaveJobAsync(Job jobToSave) => Task.CompletedTask;
        public Task DeleteJobAsync(Guid id) => throw new NotSupportedException();
        public Task ReloadAsync() => Task.CompletedTask;
        public string GetStoragePath() => Path.GetTempPath();
    }

    private sealed class DialogServiceStub : IDialogService
    {
        public bool ConfirmResult { get; set; } = true;
        public int ConfirmCalls { get; private set; }
        public string? LastError { get; private set; }
        public Task<bool> ConfirmAsync(string message, string title)
        {
            ConfirmCalls++;
            return Task.FromResult(ConfirmResult);
        }
        public Task<bool?> ConfirmWithCancelAsync(string message, string title) => Task.FromResult<bool?>(true);
        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null) =>
            Task.FromResult(defaultValue);
        public void ShowError(string message, string title) => LastError = message;
    }
}
