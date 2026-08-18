using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Models;
using DesktopAutomationApp.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class AutomationDetailViewModelExecutionTests
{
    [Fact]
    public async Task ManuallyRestoredValue_ClearsUnsavedState()
    {
        var job = new Job { Name = "Target" };
        var automation = CreateExistingAutomation(job);
        using var viewModel = CreateViewModel(automation, job);
        var originalDescription = automation.Description;

        automation.Description = "Changed";
        Assert.True(viewModel.HasUnsavedChanges);

        automation.Description = originalDescription;
        await viewModel.WaitForDirtyStateAsync();

        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
        Assert.False(viewModel.CancelCommand.CanExecute(null));
    }

    [Fact]
    public void TriggerCommand_TriggersTheEditedAutomation()
    {
        var job = new Job { Name = "Target" };
        var automation = CreateExistingAutomation(job);
        var automationService = new AutomationApplicationServiceStub();
        using var viewModel = CreateViewModel(automation, job, automationService);

        Assert.True(viewModel.TriggerCommand.CanExecute(null));

        viewModel.TriggerCommand.Execute(null);

        Assert.Equal([automation.Id], automationService.TriggeredIds);
    }

    [Fact]
    public async Task UndoAndRedo_RestoreAutomationEditsAndDirtyState()
    {
        var job = new Job { Name = "Target" };
        var automation = CreateExistingAutomation(job);
        using var viewModel = CreateViewModel(automation, job);
        var originalDescription = automation.Description;

        automation.Description = "Changed";
        Assert.True(viewModel.UndoCommand.CanExecute(null));

        viewModel.UndoCommand.Execute(null);
        await viewModel.WaitForDirtyStateAsync();

        Assert.Equal(originalDescription, automation.Description);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.RedoCommand.CanExecute(null));

        viewModel.RedoCommand.Execute(null);
        await viewModel.WaitForDirtyStateAsync();

        Assert.Equal("Changed", automation.Description);
        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public void Undo_RestoresActionSelectionAsSingleChange()
    {
        var job = new Job { Name = "Target" };
        var automation = CreateExistingAutomation(job);
        using var viewModel = CreateViewModel(automation, job);

        viewModel.SelectedAction = null;
        viewModel.UndoCommand.Execute(null);

        Assert.Equal(job.Id, automation.Action.JobId);
        Assert.Equal(job.Name, automation.Action.Name);
        Assert.Equal(AutomationActionTarget.Job, automation.Action.ActionType);
        Assert.False(viewModel.UndoCommand.CanExecute(null));
    }

    [Fact]
    public async Task DiscardCommand_RequiresConfirmation()
    {
        var job = new Job { Name = "Target" };
        var automation = CreateExistingAutomation(job);
        var dialog = new DialogServiceStub { ConfirmResult = false };
        using var viewModel = CreateViewModel(automation, job, dialog: dialog);
        automation.Description = "Changed";

        viewModel.CancelCommand.Execute(null);
        await Task.Yield();
        Assert.Equal("Changed", automation.Description);

        dialog.ConfirmResult = true;
        viewModel.CancelCommand.Execute(null);
        await Task.Yield();
        Assert.Equal(string.Empty, automation.Description);
        Assert.Equal(2, dialog.ConfirmCalls);
    }

    private static EditableAutomation CreateExistingAutomation(Job job) => new()
    {
        Name = "Manual trigger",
        Active = true,
        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        UpdatedAt = DateTimeOffset.UtcNow,
        Action = new EditableAutomationAction
        {
            Name = job.Name,
            JobId = job.Id,
            ActionType = AutomationActionTarget.Job
        }
    };

    private static AutomationDetailViewModel CreateViewModel(
        EditableAutomation automation,
        Job job,
        AutomationApplicationServiceStub? automationService = null,
        DialogServiceStub? dialog = null) => new(
            automation,
            automationService ?? new AutomationApplicationServiceStub(),
            dialog ?? new DialogServiceStub(),
            new JobApplicationServiceStub(job),
            new MakroApplicationServiceStub(),
            new HotkeyServiceStub(),
            new WindowsCapabilityCatalog(),
            NullLogger<AutomationDetailViewModel>.Instance);

    private sealed class AutomationApplicationServiceStub : IAutomationApplicationService
    {
        public List<Guid> TriggeredIds { get; } = [];
        public Task<IReadOnlyList<AutomationDefinition>> LoadAllAsync() =>
            Task.FromResult<IReadOnlyList<AutomationDefinition>>([]);
        public Task SaveAsync(AutomationDefinition automation) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task TriggerAsync(Guid id) { TriggeredIds.Add(id); return Task.CompletedTask; }
        public string GetStoragePath() => Path.GetTempPath();
    }

    private sealed class JobApplicationServiceStub(Job job) : IJobApplicationService
    {
        public IReadOnlyDictionary<string, Job> Jobs { get; } =
            new Dictionary<string, Job> { [job.Id.ToString()] = job };
        public Task<Job> CreateJobAsync(string name) => throw new NotSupportedException();
        public Task SaveJobAsync(Job jobToSave) => Task.CompletedTask;
        public Task DeleteJobAsync(Guid id) => Task.CompletedTask;
        public Task ReloadAsync() => Task.CompletedTask;
        public string GetStoragePath() => Path.GetTempPath();
    }

    private sealed class MakroApplicationServiceStub : IMakroApplicationService
    {
        public IReadOnlyDictionary<string, Makro> Makros { get; } = new Dictionary<string, Makro>();
        public Task<Makro> CreateMakroAsync(string name) => throw new NotSupportedException();
        public Task SaveMakroAsync(Makro makro) => Task.CompletedTask;
        public Task DeleteMakroAsync(Guid id) => Task.CompletedTask;
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

    private sealed class HotkeyServiceStub : IGlobalHotkeyService
    {
        public event Action<Guid>? AutomationHotkeyPressed;
        public event Action? PausedChanged;
        public event Action? EmergencyStopPressed;
        public event Action? RecordingHotkeyPressed;
        public uint ForceStopVirtualKey => 0x79;
        public bool IsPaused => false;
        public void SetForceStopKey(uint virtualKeyCode) { }
        public Task<(KeyModifiers Modifiers, uint VirtualKeyCode)> CaptureNextAsync(CancellationToken ct = default) =>
            Task.FromResult((KeyModifiers.None, 0u));
        public void RegisterAutomationHotkey(Guid automationId, KeyModifiers modifiers, uint virtualKeyCode) { }
        public void UnregisterAutomationHotkey(Guid automationId) { }
        public void StartWithMessageLoop() { }
        public void StartRecordHotkeys(MakroRecordingSettings? settings = null) { }
        public IReadOnlyList<CapturedInputEvent> StopRecordHotkeys() => [];
        public void SetRecordingHotkey(KeyModifiers modifiers, uint virtualKeyCode) { }
        public void ClearRecordingHotkey() { }
        public string FormatKey(KeyModifiers mods, uint vk) => vk.ToString();
        public string FormatMouseButton(MouseButtons button) => button.ToString();
        public void SetPaused(bool paused) { }
    }
}
