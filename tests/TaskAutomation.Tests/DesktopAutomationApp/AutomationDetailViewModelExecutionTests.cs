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
        AutomationApplicationServiceStub? automationService = null) => new(
            automation,
            automationService ?? new AutomationApplicationServiceStub(),
            new DialogServiceStub(),
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
        public Task<bool> ConfirmAsync(string message, string title) => Task.FromResult(true);
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
