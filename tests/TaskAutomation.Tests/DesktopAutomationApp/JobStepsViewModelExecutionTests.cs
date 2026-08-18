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

    private static JobStepsViewModel CreateViewModel(Job job, RecordingJobDispatcher? dispatcher = null) => new(
        job,
        new ControllableJobExecutor([job]),
        new JobApplicationServiceStub(job),
        new DialogServiceStub(),
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
        public Task<bool> ConfirmAsync(string message, string title) => Task.FromResult(true);
        public Task<bool?> ConfirmWithCancelAsync(string message, string title) => Task.FromResult<bool?>(true);
        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null) =>
            Task.FromResult(defaultValue);
        public void ShowError(string message, string title) { }
    }
}
