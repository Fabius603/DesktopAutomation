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
        public Task<bool> ConfirmAsync(string message, string title)
        {
            ConfirmCalls++;
            return Task.FromResult(ConfirmResult);
        }
        public Task<bool?> ConfirmWithCancelAsync(string message, string title) => Task.FromResult<bool?>(true);
        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null) =>
            Task.FromResult(defaultValue);
        public void ShowError(string message, string title) { }
    }
}
