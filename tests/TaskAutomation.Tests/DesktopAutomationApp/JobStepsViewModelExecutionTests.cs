using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;
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
