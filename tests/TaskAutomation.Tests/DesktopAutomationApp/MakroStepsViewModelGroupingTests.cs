using System.Collections.ObjectModel;
using System.Drawing;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Services.Preview;
using DesktopAutomationApp.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Hotkeys;
using TaskAutomation.Makros;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class MakroStepsViewModelGroupingTests
{
    [Fact]
    public void DiscardChanges_RestoresDissolvedGroupAndItsStepAssignments()
    {
        var group = new MakroGruppe { Id = "group", Title = "Group" };
        var macro = CreateMacro(group);
        using var viewModel = CreateViewModel(macro);

        viewModel.DissolveGroupCommand.Execute(group.Id);

        Assert.Empty(viewModel.Groups);
        Assert.All(viewModel.Steps, step => Assert.Null(step.GroupId));

        viewModel.DiscardChanges();

        Assert.Equal(group.Id, Assert.Single(viewModel.Groups).Id);
        Assert.All(viewModel.Steps, step => Assert.Equal(group.Id, step.GroupId));
        Assert.Null(viewModel.SelectedStep);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.CanUndo);
        Assert.False(viewModel.CanRedo);
    }

    [Fact]
    public void Undo_DissolvedGroupReturnsToSavedStateWithoutRemainingDirty()
    {
        var group = new MakroGruppe { Id = "group", Title = "Group" };
        using var viewModel = CreateViewModel(CreateMacro(group));

        viewModel.DissolveGroupCommand.Execute(group.Id);
        viewModel.UndoCommand.Execute(null);

        Assert.Equal(group.Id, Assert.Single(viewModel.Groups).Id);
        Assert.All(viewModel.Steps, step => Assert.Equal(group.Id, step.GroupId));
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.CanRedo);
    }

    [Fact]
    public void PreserveEditedStepIdentity_KeepsIdAndGroupAssignment()
    {
        var original = new MouseMoveAbsoluteBefehl { Id = "step", GroupId = "group" };
        var replacement = new MouseMoveRelativeBefehl();

        MakroStepsViewModel.PreserveEditedStepIdentity(original, replacement);

        Assert.Equal(original.Id, replacement.Id);
        Assert.Equal(original.GroupId, replacement.GroupId);
    }

    [Fact]
    public async Task Save_RefreshesIndependentBaselineForLaterDiscard()
    {
        var initialGroup = new MakroGruppe { Id = "initial", Title = "Initial" };
        using var viewModel = CreateViewModel(CreateMacro(initialGroup));
        var savedGroup = new MakroGruppe { Id = "saved", Title = "Saved" };
        viewModel.Groups.Clear();
        viewModel.Groups.Add(savedGroup);
        foreach (var step in viewModel.Steps)
            step.GroupId = savedGroup.Id;

        await viewModel.SaveAsync();
        viewModel.DissolveGroupCommand.Execute(savedGroup.Id);
        viewModel.DiscardChanges();

        Assert.Equal(savedGroup.Id, Assert.Single(viewModel.Groups).Id);
        Assert.All(viewModel.Steps, step => Assert.Equal(savedGroup.Id, step.GroupId));
        Assert.False(viewModel.HasUnsavedChanges);
    }

    private static Makro CreateMacro(MakroGruppe group) => new()
    {
        Name = "Macro",
        Gruppen = new ObservableCollection<MakroGruppe>([group]),
        Befehle = new ObservableCollection<MakroBefehl>(
        [
            new MouseMoveAbsoluteBefehl { GroupId = group.Id },
            new MouseMoveRelativeBefehl { GroupId = group.Id }
        ])
    };

    private static MakroStepsViewModel CreateViewModel(Makro macro) => new(
        macro,
        NullLogger<MakroStepsViewModel>.Instance,
        new PreviewStub(),
        new MacroApplicationServiceStub(),
        new DialogServiceStub(),
        new HotkeyServiceStub(),
        new RecordingJobDispatcher());

    private sealed class PreviewStub : IMacroPreviewService
    {
        public MacroPreviewService.PreviewResult Build(Makro makro, Rectangle virtualBounds, Rectangle overlayBounds)
            => new([], [], 0);
    }

    private sealed class MacroApplicationServiceStub : IMakroApplicationService
    {
        public IReadOnlyDictionary<string, Makro> Makros { get; } = new Dictionary<string, Makro>();
        public Task<Makro> CreateMakroAsync(string name) => throw new NotSupportedException();
        public Task SaveMakroAsync(Makro makro) => Task.CompletedTask;
        public Task DeleteMakroAsync(Guid id) => throw new NotSupportedException();
        public Task ReloadAsync() => Task.CompletedTask;
        public string GetStoragePath() => Path.GetTempPath();
    }

    private sealed class DialogServiceStub : IDialogService
    {
        public Task<bool> ConfirmAsync(string message, string title) => Task.FromResult(true);
        public Task<bool?> ConfirmWithCancelAsync(string message, string title) => Task.FromResult<bool?>(true);
        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null)
            => Task.FromResult(defaultValue);
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
        public Task<(KeyModifiers Modifiers, uint VirtualKeyCode)> CaptureNextAsync(CancellationToken ct = default)
            => Task.FromResult((KeyModifiers.None, 0u));
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
