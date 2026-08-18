using System.Collections.ObjectModel;
using System.Drawing;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Behaviors;
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
    public async Task Undo_DissolvedGroupReturnsToSavedStateWithoutRemainingDirty()
    {
        var group = new MakroGruppe { Id = "group", Title = "Group" };
        using var viewModel = CreateViewModel(CreateMacro(group));

        viewModel.DissolveGroupCommand.Execute(group.Id);
        viewModel.UndoCommand.Execute(null);
        await viewModel.WaitForDirtyStateAsync();

        Assert.Equal(group.Id, Assert.Single(viewModel.Groups).Id);
        Assert.All(viewModel.Steps, step => Assert.Equal(group.Id, step.GroupId));
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.CanRedo);
    }

    [Fact]
    public async Task ManuallyRestoredStepOrder_ClearsUnsavedState()
    {
        var group = new MakroGruppe { Id = "group", Title = "Group" };
        using var viewModel = CreateViewModel(CreateMacro(group));
        var first = viewModel.Steps[0];

        viewModel.MoveStepDownCommand.Execute(first);
        viewModel.MoveStepUpCommand.Execute(first);
        await viewModel.WaitForDirtyStateAsync();

        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
        Assert.False(viewModel.CancelCommand.CanExecute(null));
    }

    [Fact]
    public async Task DiscardCommand_RequiresConfirmation()
    {
        var group = new MakroGruppe { Id = "group", Title = "Group" };
        var dialog = new DialogServiceStub { ConfirmResult = false };
        using var viewModel = CreateViewModel(CreateMacro(group), dialog: dialog);
        viewModel.DissolveGroupCommand.Execute(group.Id);

        viewModel.CancelCommand.Execute(null);
        await Task.Yield();
        Assert.Empty(viewModel.Groups);

        dialog.ConfirmResult = true;
        viewModel.CancelCommand.Execute(null);
        await Task.Yield();
        Assert.Single(viewModel.Groups);
        Assert.Equal(2, dialog.ConfirmCalls);
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

    [Fact]
    public void StartMakroCommand_IsDisabledWhileChangesAreUnsaved()
    {
        var group = new MakroGruppe { Id = "group", Title = "Group" };
        var dispatcher = new RecordingJobDispatcher();
        using var viewModel = CreateViewModel(CreateMacro(group), dispatcher);

        Assert.True(viewModel.StartMakroCommand.CanExecute(null));

        viewModel.DissolveGroupCommand.Execute(group.Id);

        Assert.True(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.StartMakroCommand.CanExecute(null));
        viewModel.StartMakroCommand.Execute(null);
        Assert.Empty(dispatcher.StartedMakros);
    }

    [Fact]
    public void PlaybackPreview_IsDisabledWhileAPreviewIsActive()
    {
        Assert.False(MakroStepsViewModel.CanStartPreview(
            stepCount: 3,
            isBusy: false,
            isActive: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReorderStep_DroppedOnGroup_MovesBeforeOrAfterWholeGroup(bool insertAfter)
    {
        var group = new MakroGruppe { Id = "target", Title = "Target" };
        var loose = new KeyDownBefehl { Id = "loose", Key = "A" };
        var groupedOne = new MouseMoveAbsoluteBefehl { Id = "group-1", GroupId = group.Id };
        var groupedTwo = new MouseMoveRelativeBefehl { Id = "group-2", GroupId = group.Id };
        var commands = insertAfter
            ? new MakroBefehl[] { loose, groupedOne, groupedTwo }
            : [groupedOne, groupedTwo, loose];
        using var viewModel = CreateViewModel(new Makro
        {
            Name = "Macro",
            Gruppen = new ObservableCollection<MakroGruppe>([group]),
            Befehle = new ObservableCollection<MakroBefehl>(commands)
        });
        var sourceItem = viewModel.VisibleItems.OfType<MacroStepListItem>().Single(item => item.Step.Id == "loose");
        var targetItem = viewModel.VisibleItems.OfType<MacroGroupListItem>().Single();
        var visibleItems = (System.Collections.IList)viewModel.VisibleItems;

        viewModel.ReorderStepCommand.Execute(new StepDragDrop.MoveRequest(
            visibleItems,
            viewModel.VisibleItems.IndexOf(sourceItem),
            visibleItems,
            viewModel.VisibleItems.IndexOf(targetItem),
            targetItem,
            insertAfter));

        var expected = insertAfter
            ? new[] { "group-1", "group-2", "loose" }
            : ["loose", "group-1", "group-2"];
        Assert.Equal(expected, viewModel.Steps.Select(step => step.Id));
        Assert.Null(viewModel.Steps.Single(step => step.Id == "loose").GroupId);
        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReorderGroup_DroppedOnGroup_MovesBeforeOrAfterWholeGroup(bool insertAfter)
    {
        var sourceGroup = new MakroGruppe { Id = "source", Title = "Source" };
        var targetGroup = new MakroGruppe { Id = "target", Title = "Target" };
        MakroBefehl[] sourceSteps =
        [
            new MouseMoveAbsoluteBefehl { Id = "source-1", GroupId = sourceGroup.Id },
            new MouseMoveRelativeBefehl { Id = "source-2", GroupId = sourceGroup.Id }
        ];
        MakroBefehl[] targetSteps =
        [
            new MouseMoveAbsoluteBefehl { Id = "target-1", GroupId = targetGroup.Id },
            new MouseMoveRelativeBefehl { Id = "target-2", GroupId = targetGroup.Id }
        ];
        var commands = insertAfter ? sourceSteps.Concat(targetSteps) : targetSteps.Concat(sourceSteps);
        using var viewModel = CreateViewModel(new Makro
        {
            Name = "Macro",
            Gruppen = new ObservableCollection<MakroGruppe>([sourceGroup, targetGroup]),
            Befehle = new ObservableCollection<MakroBefehl>(commands)
        });
        var sourceItem = viewModel.VisibleItems.OfType<MacroGroupListItem>().Single(item => item.GroupId == sourceGroup.Id);
        var targetItem = viewModel.VisibleItems.OfType<MacroGroupListItem>().Single(item => item.GroupId == targetGroup.Id);
        var visibleItems = (System.Collections.IList)viewModel.VisibleItems;

        viewModel.ReorderStepCommand.Execute(new StepDragDrop.MoveRequest(
            visibleItems,
            viewModel.VisibleItems.IndexOf(sourceItem),
            visibleItems,
            viewModel.VisibleItems.IndexOf(targetItem),
            targetItem,
            insertAfter));

        var expected = insertAfter
            ? new[] { "target-1", "target-2", "source-1", "source-2" }
            : ["source-1", "source-2", "target-1", "target-2"];
        Assert.Equal(expected, viewModel.Steps.Select(step => step.Id));
        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public void ReorderOnlyStep_DroppedOnItsGroup_RemovesTheEmptyGroup()
    {
        var group = new MakroGruppe { Id = "group", Title = "Group" };
        using var viewModel = CreateViewModel(new Makro
        {
            Name = "Macro",
            Gruppen = new ObservableCollection<MakroGruppe>([group]),
            Befehle = new ObservableCollection<MakroBefehl>(
            [
                new MouseMoveAbsoluteBefehl { Id = "step", GroupId = group.Id }
            ])
        });
        viewModel.ToggleGroupCommand.Execute(group.Id);
        var sourceItem = viewModel.VisibleItems.OfType<MacroStepListItem>().Single();
        var targetItem = viewModel.VisibleItems.OfType<MacroGroupListItem>().Single();
        var visibleItems = (System.Collections.IList)viewModel.VisibleItems;

        viewModel.ReorderStepCommand.Execute(new StepDragDrop.MoveRequest(
            visibleItems,
            viewModel.VisibleItems.IndexOf(sourceItem),
            visibleItems,
            viewModel.VisibleItems.IndexOf(targetItem),
            targetItem,
            InsertAfterTarget: true));

        Assert.Empty(viewModel.Groups);
        Assert.Null(Assert.Single(viewModel.Steps).GroupId);
        Assert.True(viewModel.HasUnsavedChanges);
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

    private static MakroStepsViewModel CreateViewModel(
        Makro macro,
        RecordingJobDispatcher? dispatcher = null,
        DialogServiceStub? dialog = null) => new(
        macro,
        NullLogger<MakroStepsViewModel>.Instance,
        new PreviewStub(),
        new MacroApplicationServiceStub(),
        dialog ?? new DialogServiceStub(),
        new HotkeyServiceStub(),
        dispatcher ?? new RecordingJobDispatcher());

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
        public bool ConfirmResult { get; set; } = true;
        public int ConfirmCalls { get; private set; }
        public Task<bool> ConfirmAsync(string message, string title)
        {
            ConfirmCalls++;
            return Task.FromResult(ConfirmResult);
        }
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
