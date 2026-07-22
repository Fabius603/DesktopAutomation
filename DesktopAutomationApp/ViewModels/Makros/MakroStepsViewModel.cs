using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Services.Preview;
using DesktopAutomationApp.Views;
using DesktopOverlay;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Behaviors;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class MakroStepsViewModel : ViewModelBase, INavigationGuard, IDisposable
    {
        private readonly ILogger<MakroStepsViewModel> _log;
        private readonly IMacroPreviewService _preview;
        private readonly IMakroApplicationService _makroAppService;
        private readonly IDialogService _dialogService;
        private readonly IGlobalHotkeyService _hotkeys;
        private readonly IJobDispatcher _dispatcher;

        private readonly List<MakroBefehl> _originalSteps;
        private readonly List<MakroGruppe> _originalGroups;
        private MakroRecordingSettings _originalRecordingSettings;
        private MakroRecordedEnvironment? _originalRecordedEnvironment;
        private Overlay _overlay;
        private MacroPreviewService.PreviewResult _lastPreview;

        private readonly Stack<MacroSnapshot> _undoStack = new();
        private readonly Stack<MacroSnapshot> _redoStack = new();
        private List<MakroBefehl> _clipboard = new();
        private readonly HashSet<string> _collapsedGroupIds = new(StringComparer.Ordinal);

        public Makro Makro { get; }
        public string Title => Makro.Name;

        private readonly ObservableRangeCollection<MakroBefehl> _stepItems;
        public ObservableCollection<MakroBefehl> Steps => _stepItems;
        public ObservableCollection<MakroGruppe> Groups => Makro.Gruppen;
        private readonly ObservableRangeCollection<MacroListItem> _visibleItems = new();
        public IList<MacroListItem> VisibleItems => _visibleItems;
        public ObservableCollection<MacroFilterOption> GroupFilterOptions { get; } = new();
        public IReadOnlyList<MacroFilterOption> StepTypeFilterOptions { get; } =
        [
            new("all", Loc.Get("Ui.Macro.Filter.AllSteps")),
            new("movement", Loc.Get("Ui.Macro.Filter.Movement")),
            new("mouse", Loc.Get("Ui.Macro.Filter.Mouse")),
            new("keyboard", Loc.Get("Ui.Macro.Filter.Keyboard")),
            new("timeout", Loc.Get("Ui.Macro.Filter.Timeout"))
        ];

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set { if (_filterText == value) return; _filterText = value ?? string.Empty; OnPropertyChanged(); RebuildVisibleItems(); }
        }

        private MacroFilterOption? _selectedGroupFilter;
        public MacroFilterOption? SelectedGroupFilter
        {
            get => _selectedGroupFilter;
            set { if (_selectedGroupFilter == value) return; _selectedGroupFilter = value; OnPropertyChanged(); if (!_updatingFilters) RebuildVisibleItems(); }
        }

        private MacroFilterOption? _selectedStepTypeFilter;
        public MacroFilterOption? SelectedStepTypeFilter
        {
            get => _selectedStepTypeFilter;
            set { if (_selectedStepTypeFilter == value) return; _selectedStepTypeFilter = value; OnPropertyChanged(); RebuildVisibleItems(); }
        }

        public int StepCount => Steps.Count;
        public int GroupCount => Groups.Count;
        public string TotalDurationDisplay => MakroTimeFormatter.FormatMicroseconds(MakroTimeline.GetTotalDurationMicroseconds(Steps));

        /// <summary>Incrementiert bei jeder Listenänderung; wird von Konvertern als Cache-Schlüssel genutzt.</summary>
        public int StepsVersion { get; private set; }

        /// <summary>All currently selected steps (synced from the view's ListBox.SelectedItems).</summary>
        public List<MakroBefehl> SelectedSteps { get; } = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        private MakroBefehl? _selectedStep;
        public MakroBefehl? SelectedStep
        {
            get => _selectedStep;
            set { _selectedStep = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set { _hasUnsavedChanges = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        // --- Recording state ---
        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            private set { if (_isRecording == value) return; _isRecording = value; OnPropertyChanged(); OnPropertyChanged(nameof(RecordButtonText)); InvalidateAllCommands(); }
        }
        public string RecordButtonText => Loc.Get(IsRecording ? "Macro.Record.Stop" : "Macro.Record.Start");

        private bool _isCapturingClick;
        public bool IsCapturingClick
        {
            get => _isCapturingClick;
            private set { if (_isCapturingClick == value) return; _isCapturingClick = value; OnPropertyChanged(); OnPropertyChanged(nameof(CaptureClickButtonText)); InvalidateAllCommands(); }
        }
        public string CaptureClickButtonText => Loc.Get(IsCapturingClick ? "Macro.CaptureClick.Running" : "Macro.CaptureClick.Start");

        public string RecordingModeText => Makro.RecordingSettings.Mode switch
        {
            MakroRecordingMode.ScreenAccurateAbsolute => Loc.Get("Ui.Macro.Recording.ScreenAccurate.Short"),
            MakroRecordingMode.MotionFaithfulRelative => Loc.Get("Ui.Macro.Recording.MotionFaithful.Short"),
            _ => Loc.Get("Ui.Macro.Recording.ClicksOnly.Short")
        };

        private bool _isMakroRunning;
        public bool IsMakroRunning
        {
            get => _isMakroRunning;
            private set { _isMakroRunning = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        // --- Commands ---
        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand AddStepCommand { get; }
        public ICommand EditStepCommand { get; }
        public ICommand MoveStepUpCommand { get; }
        public ICommand MoveStepDownCommand { get; }
        public ICommand ReorderStepCommand { get; }
        public ICommand DeleteStepCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand DuplicateStepCommand { get; }
        public ICommand CreateGroupCommand { get; }
        public ICommand RemoveFromGroupCommand { get; }
        public ICommand RenameGroupCommand { get; }
        public ICommand DissolveGroupCommand { get; }
        public ICommand ToggleGroupCommand { get; }
        public ICommand RecordStepsCommand { get; }
        public ICommand CaptureClickCommand { get; }
        public ICommand OpenRecordingSettingsCommand { get; }
        public ICommand PreviewOverviewCommand { get; }
        public ICommand PreviewPlaybackCommand { get; }
        public ICommand PreviewStopCommand { get; }
        public ICommand StartMakroCommand { get; }
        public ICommand StopMakroCommand { get; }

        public event Action? RequestBack;

        public MakroStepsViewModel(
            Makro makro,
            ILogger<MakroStepsViewModel> log,
            IMacroPreviewService preview,
            IMakroApplicationService makroAppService,
            IDialogService dialogService,
            IGlobalHotkeyService hotkeys,
            IJobDispatcher dispatcher)
        {
            Makro = makro ?? throw new ArgumentNullException(nameof(makro));
            _log = log;
            _preview = preview;
            _makroAppService = makroAppService;
            _dialogService = dialogService;
            _hotkeys = hotkeys;
            _dispatcher = dispatcher;

            _originalSteps = new List<MakroBefehl>(makro.Befehle ?? Enumerable.Empty<MakroBefehl>());
            _originalGroups = makro.Gruppen.Select(CloneGroup).ToList();
            _collapsedGroupIds.UnionWith(makro.Gruppen.Select(group => group.Id));
            _originalRecordingSettings = makro.RecordingSettings.Clone();
            _originalRecordedEnvironment = makro.RecordedEnvironment?.Clone();
            _stepItems = new ObservableRangeCollection<MakroBefehl>();
            _stepItems.ReplaceRange(makro.Befehle ?? Enumerable.Empty<MakroBefehl>());
            Steps.CollectionChanged += (_, _) =>
            {
                StepsVersion++;
                OnPropertyChanged(nameof(StepsVersion));
                InvalidateAllCommands();
                ValidateAndApply();
                RecalculatePresentation();
            };
            Groups.CollectionChanged += Groups_CollectionChanged;

            BackCommand   = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand   = new RelayCommand(async () => await SaveInternal(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(DiscardChanges, () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(async () => await Rename());
            OpenFileCommand = new RelayCommand(OpenFileInExplorer);

            AddStepCommand      = new RelayCommand(async () => await OpenAddStepDialog());
            EditStepCommand     = new RelayCommand<MakroBefehl?>(EditStep, s => s != null);
            MoveStepUpCommand   = new RelayCommand<MakroBefehl?>(s => MoveRelative(s, -1), s => CanMoveRelative(s, -1));
            MoveStepDownCommand = new RelayCommand<MakroBefehl?>(s => MoveRelative(s, +1), s => CanMoveRelative(s, +1));
            ReorderStepCommand  = new RelayCommand<StepDragDrop.MoveRequest>(MoveStep);
            DeleteStepCommand     = new RelayCommand<MakroBefehl?>(async s => await DeleteStepAsync(s), s => s != null);
            DeleteSelectedCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => SelectedSteps.Count > 0 || SelectedStep != null);
            UndoCommand           = new RelayCommand(Undo, () => CanUndo);
            RedoCommand           = new RelayCommand(Redo, () => CanRedo);
            CopyCommand           = new RelayCommand(CopySelected, () => SelectedSteps.Count > 0 || SelectedStep != null);
            PasteCommand          = new RelayCommand(Paste, () => _clipboard.Count > 0);
            DuplicateStepCommand  = new RelayCommand<MakroBefehl?>(DuplicateStep, s => s != null);
            CreateGroupCommand     = new RelayCommand(async () => await CreateGroupAsync(), () => SelectedSteps.Count > 0 || SelectedStep != null);
            RemoveFromGroupCommand = new RelayCommand(RemoveSelectedFromGroup, () => GetSelection().Any(s => s.HasGroup));
            RenameGroupCommand     = new RelayCommand<string?>(async id => await RenameGroupAsync(id), id => FindGroup(id) != null);
            DissolveGroupCommand   = new RelayCommand<string?>(DissolveGroup, id => FindGroup(id) != null);
            ToggleGroupCommand     = new RelayCommand<string?>(ToggleGroup, id => FindGroup(id) != null);

            RecordStepsCommand   = new RelayCommand(async () => await ToggleRecordAsync());
            CaptureClickCommand  = new RelayCommand(async () => await CaptureClickAsync(), () => !IsCapturingClick && !IsRecording);
            OpenRecordingSettingsCommand = new RelayCommand(OpenRecordingSettings, () => !IsRecording);

            PreviewOverviewCommand = new RelayCommand(ShowOverview, CanPreview);
            PreviewPlaybackCommand = new RelayCommand(() => ShowPlayback(), CanPreview);
            PreviewStopCommand     = new RelayCommand(StopPreview, () => _overlay != null);

            StartMakroCommand = new RelayCommand(() => _dispatcher.StartMakro(Makro.Id), () => !IsMakroRunning);
            StopMakroCommand  = new RelayCommand(() => _dispatcher.CancelMakro(Makro.Id), () => IsMakroRunning);

            _dispatcher.RunningMakrosChanged += OnRunningMakrosChanged;
            _hotkeys.RecordingHotkeyPressed += OnRecordingHotkeyPressed;
            ApplyRecordingHotkey();
            IsMakroRunning = _dispatcher.RunningMakroIds.Contains(Makro.Id);
            SelectedStepTypeFilter = StepTypeFilterOptions[0];
            RecalculatePresentation();
            ValidateAndApply();
        }

        // ---------- Selection sync (called from code-behind) ----------
        private void OpenFileInExplorer()
            => ShowFileInExplorer(_makroAppService.GetStoragePath(), $"{Makro.Id}.json");

        private static void ShowFileInExplorer(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo(File.Exists(path) ? path : directory) { UseShellExecute = true });
        }

        public void SetSelectedSteps(IEnumerable<object> items)
        {
            SelectedSteps.Clear();
            foreach (var item in items)
            {
                if (item is MacroStepListItem stepItem)
                    SelectedSteps.Add(stepItem.Step);
                else if (item is MacroGroupListItem groupItem)
                    SelectedSteps.AddRange(Steps.Where(step => step.GroupId == groupItem.GroupId));
                else if (item is MakroBefehl step)
                    SelectedSteps.Add(step);
            }
            var distinct = SelectedSteps.Distinct().ToList();
            SelectedSteps.Clear();
            SelectedSteps.AddRange(distinct);
            if (SelectedSteps.Count > 0)
                SelectedStep = SelectedSteps[^1];
            InvalidateAllCommands();
        }

        private IEnumerable<MakroBefehl> GetSelection()
            => SelectedSteps.Count > 0
                ? SelectedSteps
                : SelectedStep is not null ? [SelectedStep] : [];

        // ---------- INavigationGuard ----------
        public async Task SaveAsync() => await SaveInternal();

        public void DiscardChanges()
        {
            _stepItems.ReplaceRange(_originalSteps);
            Groups.CollectionChanged -= Groups_CollectionChanged;
            Groups.Clear();
            foreach (var group in _originalGroups.Select(CloneGroup))
                Groups.Add(group);
            _collapsedGroupIds.Clear();
            _collapsedGroupIds.UnionWith(Groups.Select(group => group.Id));
            Groups.CollectionChanged += Groups_CollectionChanged;
            Makro.RecordingSettings = _originalRecordingSettings.Clone();
            Makro.RecordedEnvironment = _originalRecordedEnvironment?.Clone();
            ApplyRecordingHotkey();
            OnPropertyChanged(nameof(RecordingModeText));
            HasUnsavedChanges = false;
        }

        // ---------- Save ----------
        private async Task SaveInternal()
        {
            Makro.Befehle = new ObservableCollection<MakroBefehl>(Steps);
            RemoveUnusedGroups();
            var validation = MakroValidation.Validate(Makro);
            ApplyValidation(validation);
            if (!validation.IsValid)
            {
                _dialogService.ShowError(LocalizeValidationError(validation.Error), Loc.Get("Validation.Title"));
                return;
            }
            await _makroAppService.SaveMakroAsync(Makro);
            // Update snapshot for future cancel
            _originalSteps.Clear();
            _originalSteps.AddRange(Steps);
            _originalGroups.Clear();
            _originalGroups.AddRange(Groups.Select(CloneGroup));
            _originalRecordingSettings = Makro.RecordingSettings.Clone();
            _originalRecordedEnvironment = Makro.RecordedEnvironment?.Clone();
            HasUnsavedChanges = false;
        }

        private void ValidateAndApply()
        {
            var draft = new Makro
            {
                Name = Makro.Name,
                Befehle = new ObservableCollection<MakroBefehl>(Steps),
                Gruppen = new ObservableCollection<MakroGruppe>(Groups)
            };
            ApplyValidation(MakroValidation.Validate(draft));
        }

        private static void ApplyValidation(MakroValidationResult validation)
        {
            foreach (var result in validation.Commands)
                result.Command.SetValidationResult(result.IsValid, result.Error);
        }

        private static string LocalizeValidationError(MakroValidationError error) => error switch
        {
            _ => MakroValidation.Describe(error)
        };

        // ---------- Rename ----------
        private async Task Rename()
        {
            var newName = await _dialogService.AskForNameAsync(Loc.Get("Common.Rename"), Loc.Get("Dialog.NewName"), Makro.Name);
            if (newName == null) return;

            Makro.Name = newName.Trim();
            OnPropertyChanged(nameof(Title));
            await _makroAppService.SaveMakroAsync(Makro);
        }

        // ---------- Steps ----------
        private bool CanMoveRelative(MakroBefehl? step, int delta)
        {
            if (step == null) return false;
            var idx = Steps.IndexOf(step);
            if (idx < 0) return false;
            var newIdx = idx + delta;
            return newIdx >= 0 && newIdx < Steps.Count;
        }

        private void MoveRelative(MakroBefehl? step, int delta)
        {
            if (!CanMoveRelative(step, delta) || step == null) return;
            PushUndo();
            var idx = Steps.IndexOf(step);
            Steps.RemoveAt(idx);
            Steps.Insert(idx + delta, step);
            SelectedStep = step;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void MoveToIndex(int from, int to)
        {
            if (from < 0 || from >= Steps.Count || to < 0 || to >= Steps.Count || from == to) return;
            PushUndo();
            var step = Steps[from];
            Steps.RemoveAt(from);
            Steps.Insert(to, step);
            SelectedStep = step;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void MoveStep(StepDragDrop.MoveRequest request)
        {
            if (ReferenceEquals(request.Source, Steps) && ReferenceEquals(request.Target, Steps))
            {
                var targetIndex = Math.Clamp(request.TargetIndex, 0, Steps.Count);
                if (targetIndex > request.SourceIndex) targetIndex--;
                MoveToIndex(request.SourceIndex, targetIndex);
                return;
            }

            if (!ReferenceEquals(request.Source, VisibleItems)
                || !ReferenceEquals(request.Target, VisibleItems)
                || request.SourceIndex < 0 || request.SourceIndex >= VisibleItems.Count)
                return;

            if (VisibleItems[request.SourceIndex] is MacroGroupListItem sourceGroup)
            {
                MoveGroup(sourceGroup.GroupId, request.TargetIndex);
                return;
            }
            if (VisibleItems[request.SourceIndex] is not MacroStepListItem sourceItem) return;

            var from = Steps.IndexOf(sourceItem.Step);
            var target = request.TargetIndex >= VisibleItems.Count
                ? Steps.Count - 1
                : VisibleItems[request.TargetIndex] switch
                {
                    MacroStepListItem targetStep => Steps.IndexOf(targetStep.Step),
                    MacroGroupListItem targetGroup => Steps.ToList().FindIndex(step => step.GroupId == targetGroup.GroupId),
                    _ => from
                };
            if (target > from) target--;
            MoveToIndex(from, Math.Clamp(target, 0, Math.Max(0, Steps.Count - 1)));
        }

        private void MoveGroup(string groupId, int visibleTargetIndex)
        {
            var moving = Steps.Where(step => step.GroupId == groupId).ToList();
            if (moving.Count == 0) return;
            var movingSet = moving.ToHashSet();

            MakroBefehl? anchor = null;
            if (visibleTargetIndex < VisibleItems.Count)
            {
                anchor = VisibleItems[visibleTargetIndex] switch
                {
                    MacroGroupListItem targetGroup when targetGroup.GroupId != groupId
                        => Steps.FirstOrDefault(step => step.GroupId == targetGroup.GroupId),
                    MacroStepListItem targetStep when !movingSet.Contains(targetStep.Step) && targetStep.Step.GroupId is { } targetGroupId
                        => Steps.FirstOrDefault(step => step.GroupId == targetGroupId),
                    MacroStepListItem targetStep when !movingSet.Contains(targetStep.Step) => targetStep.Step,
                    _ => null
                };
                if (anchor is null && VisibleItems[visibleTargetIndex] is MacroGroupListItem or MacroStepListItem)
                    return;
            }

            var reordered = MakroGrouping.MoveGroupBefore(Steps, groupId, anchor);
            if (Steps.SequenceEqual(reordered))
                return;

            PushUndo();
            _stepItems.ReplaceRange(reordered);
            SelectedStep = moving[^1];
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void EditStep(MakroBefehl? step)
        {
            if (step == null) return;
            var index = Steps.IndexOf(step);
            if (index < 0) return;

            var vm = new AddStepDialogViewModel(_hotkeys) { Mode = StepDialogMode.Edit };
            Prefill(vm, step);

            var dlg = new AddStepDialog { Owner = Application.Current.MainWindow, DataContext = vm };
            if (dlg.ShowDialog() != true || vm.CreatedStep == null) return;

            PushUndo();
            Steps[index] = vm.CreatedStep;
            SelectedStep = vm.CreatedStep;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private async Task OpenAddStepDialog()
        {
            var dlgVm = new AddStepDialogViewModel(_hotkeys) { Mode = StepDialogMode.Add };
            var dlg = new AddStepDialog { Owner = Application.Current.MainWindow, DataContext = dlgVm };

            if (dlg.ShowDialog() != true || dlgVm.CreatedStep == null) return;

            int insertIndex = SelectedStep != null
                ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                : Steps.Count;

            PushUndo();
            Steps.Insert(insertIndex, dlgVm.CreatedStep);
            SelectedStep = dlgVm.CreatedStep;
            HasUnsavedChanges = true;
        }

        private async Task DeleteStepAsync(MakroBefehl? step)
        {
            if (step == null) return;

            var confirmed = await _dialogService.ConfirmAsync(Loc.Get("Step.Delete.One"), Loc.Get("Dialog.Delete.Title"));
            if (!confirmed) return;

            PushUndo();
            var idx = Steps.IndexOf(step);
            if (idx < 0) return;

            var next = Steps.ElementAtOrDefault(Math.Max(0, idx - 1))
                      ?? Steps.ElementAtOrDefault(idx + 1);
            Steps.RemoveAt(idx);
            SelectedStep = next;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void DuplicateStep(MakroBefehl? step)
        {
            if (step == null) return;
            var sources = SelectedSteps.Count > 1 && SelectedSteps.Contains(step)
                ? SelectedSteps.OrderBy(Steps.IndexOf).ToList()
                : [step];
            PushUndo();
            var clones = sources.Select(CloneStep).ToList();
            var insertIndex = Math.Min(Steps.Count, sources.Max(Steps.IndexOf) + 1);
            _stepItems.InsertRange(insertIndex, clones);
            SelectedStep = clones[^1];
            HasUnsavedChanges = true;
        }

        private static MakroBefehl CloneStep(MakroBefehl s)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(s, JsonOptions.Default);
            return System.Text.Json.JsonSerializer.Deserialize<MakroBefehl>(json, JsonOptions.Default)!;
        }

        private static MakroGruppe CloneGroup(MakroGruppe group)
            => new() { Id = group.Id, Title = group.Title, IsAutomatic = group.IsAutomatic };

        // ---------- Undo / Redo ----------
        private void PushUndo()
        {
            _undoStack.Push(CreateSnapshot());
            _redoStack.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;
            _redoStack.Push(CreateSnapshot());
            RestoreSnapshot(_undoStack.Pop());
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Push(CreateSnapshot());
            RestoreSnapshot(_redoStack.Pop());
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private MacroSnapshot CreateSnapshot()
            => new(Steps.Select(CloneStep).ToList(), Groups.Select(CloneGroup).ToList());

        private void RestoreSnapshot(MacroSnapshot snapshot)
        {
            _stepItems.ReplaceRange(snapshot.Steps);
            Groups.CollectionChanged -= Groups_CollectionChanged;
            Groups.Clear();
            foreach (var group in snapshot.Groups) Groups.Add(group);
            _collapsedGroupIds.Clear();
            _collapsedGroupIds.UnionWith(Groups.Select(group => group.Id));
            Groups.CollectionChanged += Groups_CollectionChanged;
            SelectedStep = null;
            SelectedSteps.Clear();
            HasUnsavedChanges = true;
            RecalculatePresentation();
        }

        // ---------- Copy / Paste ----------
        private void CopySelected()
        {
            var sources = SelectedSteps.Count > 0
                ? SelectedSteps.OrderBy(s => Steps.IndexOf(s)).ToList()
                : (SelectedStep != null ? new List<MakroBefehl> { SelectedStep } : null);
            if (sources == null) return;
            _clipboard = sources.Select(s => CloneStep(s)).ToList();
            InvalidateAllCommands();
        }

        private void Paste()
        {
            if (_clipboard.Count == 0) return;

            int insertAt = SelectedStep != null
                ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                : Steps.Count;

            var toInsert = _clipboard.Select(s => CloneStep(s)).ToList();
            PushUndo();
            for (int i = 0; i < toInsert.Count; i++)
                Steps.Insert(insertAt + i, toInsert[i]);

            SelectedStep = toInsert[^1];
            HasUnsavedChanges = true;
        }

        // ---------- Delete selected ----------
        private async Task DeleteSelectedAsync()
        {
            var targets = SelectedSteps.Count > 0
                ? SelectedSteps.ToList()
                : (SelectedStep != null ? new List<MakroBefehl> { SelectedStep } : null);
            if (targets == null || targets.Count == 0) return;

            string message = targets.Count == 1
                ? Loc.Get("Step.Delete.One")
                : Loc.Format("Step.Delete.Many", targets.Count);

            if (!await _dialogService.ConfirmAsync(message, Loc.Get("Dialog.Delete.Title")))
                return;

            PushUndo();
            int firstIdx = targets.Select(t => Steps.IndexOf(t)).Where(i => i >= 0).DefaultIfEmpty(0).Min();
            foreach (var t in targets)
            {
                var idx = Steps.IndexOf(t);
                if (idx >= 0) Steps.RemoveAt(idx);
            }

            SelectedStep = Steps.ElementAtOrDefault(Math.Max(0, firstIdx - 1));
            SelectedSteps.Clear();
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private static void Prefill(AddStepDialogViewModel vm, MakroBefehl step)
        {
            vm.DelayBeforeMicroseconds = step.DelayBeforeMicroseconds;
            switch (step)
            {
                case MouseMoveAbsoluteBefehl mm:
                    vm.SelectedType = "MouseMoveAbsolute"; vm.X = mm.X; vm.Y = mm.Y; break;
                case MouseMoveRelativeBefehl mr:
                    vm.SelectedType = "MouseMoveRelative"; vm.DeltaX = mr.DeltaX; vm.DeltaY = mr.DeltaY; break;
                case MouseDownBefehl md:
                    vm.SelectedType = "MouseDown"; vm.MouseButton = md.Button; break;
                case MouseUpBefehl mu:
                    vm.SelectedType = "MouseUp"; vm.MouseButton = mu.Button; break;
                case KeyDownBefehl kd:
                    vm.SelectedType = "KeyDown"; vm.Key = kd.Key; break;
                case KeyUpBefehl ku:
                    vm.SelectedType = "KeyUp"; vm.Key = ku.Key; break;
                case TimeoutBefehl to:
                    vm.SelectedType = "Timeout"; vm.Duration = to.Duration; break;
            }
        }

        // ---------- Groups, filters and timeline ----------
        private void Groups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
                foreach (var group in e.NewItems.OfType<MakroGruppe>())
                    _collapsedGroupIds.Add(group.Id);
            if (e.OldItems is not null)
                foreach (var group in e.OldItems.OfType<MakroGruppe>())
                    _collapsedGroupIds.Remove(group.Id);
            HasUnsavedChanges = true;
            RecalculatePresentation();
        }

        private void RecalculatePresentation()
        {
            var timeline = MakroTimeline.Calculate(Steps);
            var executionTimes = timeline.ToDictionary(entry => entry.Command, entry => entry.ExecutionTimeMicroseconds);
            var groupsById = Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
            var groupPresentations = Steps
                .Where(step => step.GroupId is { Length: > 0 } && groupsById.ContainsKey(step.GroupId))
                .GroupBy(step => step.GroupId!, StringComparer.Ordinal)
                .ToDictionary(grouping => grouping.Key, grouping =>
                {
                    var groupedSteps = grouping.ToList();
                    var first = groupedSteps[0];
                    var last = groupedSteps[^1];
                    var start = executionTimes.GetValueOrDefault(first);
                    var end = executionTimes.GetValueOrDefault(last)
                        + (last is TimeoutBefehl timeout ? timeout.Duration * 1_000L : 0);
                    return new GroupPresentation(
                        groupsById[grouping.Key].Title,
                        $"{MakroTimeFormatter.FormatMicroseconds(Math.Max(0, end - start))} · {groupedSteps.Count} Steps");
                }, StringComparer.Ordinal);

            foreach (var step in Steps)
            {
                var executionTime = executionTimes.GetValueOrDefault(step);
                if (step.GroupId is { Length: > 0 } groupId && groupPresentations.TryGetValue(groupId, out var presentation))
                {
                    step.SetDisplayMetadata(
                        MakroTimeFormatter.FormatMicroseconds(executionTime),
                        presentation.Title,
                        presentation.Summary);
                }
                else
                {
                    step.SetDisplayMetadata(MakroTimeFormatter.FormatMicroseconds(executionTime));
                }
            }

            RefreshGroupFilters();
            OnPropertyChanged(nameof(StepCount));
            OnPropertyChanged(nameof(GroupCount));
            OnPropertyChanged(nameof(TotalDurationDisplay));
            RebuildVisibleItems();
        }

        private bool _updatingFilters;
        private void RefreshGroupFilters()
        {
            var selectedId = SelectedGroupFilter?.Id ?? "all";
            _updatingFilters = true;
            GroupFilterOptions.Clear();
            GroupFilterOptions.Add(new MacroFilterOption("all", Loc.Get("Ui.Macro.Filter.AllGroups")));
            GroupFilterOptions.Add(new MacroFilterOption("ungrouped", Loc.Get("Ui.Macro.Filter.Ungrouped")));
            foreach (var group in Groups.OrderBy(group => group.Title, StringComparer.CurrentCultureIgnoreCase))
                GroupFilterOptions.Add(new MacroFilterOption(group.Id, group.Title));
            _selectedGroupFilter = GroupFilterOptions.FirstOrDefault(item => item.Id == selectedId) ?? GroupFilterOptions[0];
            OnPropertyChanged(nameof(SelectedGroupFilter));
            _updatingFilters = false;
        }

        private void RebuildVisibleItems()
        {
            var visibleItems = new List<MacroListItem>(Steps.Count + Groups.Count);
            string? previousGroupId = null;
            var groupSegmentOpen = false;
            for (var index = 0; index < Steps.Count; index++)
            {
                var step = Steps[index];
                if (!FilterStep(step)) continue;
                if (step.GroupId is { Length: > 0 } groupId)
                {
                    if (!groupSegmentOpen || previousGroupId != groupId)
                    {
                        var group = FindGroup(groupId);
                        if (group is not null)
                            visibleItems.Add(new MacroGroupListItem(
                                group.Id, group.Title, step.GroupSummaryDisplay, _collapsedGroupIds.Contains(group.Id)));
                    }
                    previousGroupId = groupId;
                    groupSegmentOpen = true;
                }
                else
                {
                    previousGroupId = null;
                    groupSegmentOpen = false;
                }
                if (step.GroupId is null || !_collapsedGroupIds.Contains(step.GroupId))
                    visibleItems.Add(new MacroStepListItem(step, index + 1));
            }
            _visibleItems.ReplaceRange(visibleItems);
        }

        public MacroListItem? GetVisibleItem(MakroBefehl step)
            => VisibleItems.OfType<MacroStepListItem>().FirstOrDefault(item => ReferenceEquals(item.Step, step));

        private bool FilterStep(object item)
        {
            if (item is not MakroBefehl step) return false;
            var typeFilter = SelectedStepTypeFilter?.Id ?? "all";
            var typeMatches = typeFilter switch
            {
                "movement" => step is MouseMoveAbsoluteBefehl or MouseMoveRelativeBefehl,
                "mouse" => step is MouseDownBefehl or MouseUpBefehl,
                "keyboard" => step is KeyDownBefehl or KeyUpBefehl,
                "timeout" => step is TimeoutBefehl,
                _ => true
            };
            if (!typeMatches) return false;

            var groupFilter = SelectedGroupFilter?.Id ?? "all";
            if (groupFilter == "ungrouped" && step.HasGroup) return false;
            if (groupFilter is not ("all" or "ungrouped") && step.GroupId != groupFilter) return false;

            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            var search = FilterText.Trim();
            return step.GetType().Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || step.GroupTitleDisplay.Contains(search, StringComparison.CurrentCultureIgnoreCase);
        }

        private async Task CreateGroupAsync()
        {
            var selected = GetSelection().OrderBy(Steps.IndexOf).ToList();
            if (selected.Count == 0) return;
            var title = await _dialogService.AskForNameAsync(
                Loc.Get("Ui.Macro.Group.Create"),
                Loc.Get("Ui.Macro.Group.Name"),
                Loc.Format("Ui.Macro.Group.DefaultTitle", Groups.Count + 1));
            if (string.IsNullOrWhiteSpace(title)) return;

            PushUndo();
            var group = new MakroGruppe { Title = title.Trim() };
            Groups.Add(group);
            foreach (var step in selected) step.GroupId = group.Id;
            RemoveUnusedGroups();
            HasUnsavedChanges = true;
            RecalculatePresentation();
        }

        private void RemoveSelectedFromGroup()
        {
            var selected = GetSelection().Where(step => step.HasGroup).ToList();
            if (selected.Count == 0) return;
            PushUndo();
            foreach (var step in selected) step.GroupId = null;
            RemoveUnusedGroups();
            HasUnsavedChanges = true;
            RecalculatePresentation();
        }

        private async Task RenameGroupAsync(string? groupId)
        {
            var group = FindGroup(groupId);
            if (group is null) return;
            var title = await _dialogService.AskForNameAsync(
                Loc.Get("Ui.Macro.Group.Rename"), Loc.Get("Ui.Macro.Group.Name"), group.Title);
            if (string.IsNullOrWhiteSpace(title) || title.Trim() == group.Title) return;
            PushUndo();
            group.Title = title.Trim();
            HasUnsavedChanges = true;
            RecalculatePresentation();
        }

        private void DissolveGroup(string? groupId)
        {
            var group = FindGroup(groupId);
            if (group is null) return;
            PushUndo();
            foreach (var step in Steps.Where(step => step.GroupId == group.Id)) step.GroupId = null;
            Groups.Remove(group);
            HasUnsavedChanges = true;
            RecalculatePresentation();
        }

        private void ToggleGroup(string? groupId)
        {
            if (FindGroup(groupId) is null || groupId is null) return;
            if (!_collapsedGroupIds.Add(groupId)) _collapsedGroupIds.Remove(groupId);
            RebuildVisibleItems();
        }

        private MakroGruppe? FindGroup(string? groupId)
            => string.IsNullOrWhiteSpace(groupId) ? null : Groups.FirstOrDefault(group => group.Id == groupId);

        private void RemoveUnusedGroups()
        {
            var used = Steps.Where(step => step.GroupId is not null).Select(step => step.GroupId!).ToHashSet(StringComparer.Ordinal);
            foreach (var group in Groups.Where(group => !used.Contains(group.Id)).ToList())
                Groups.Remove(group);
        }

        // ---------- Recording ----------
        private void OpenRecordingSettings()
        {
            var dialog = new RecordingSettingsDialog(Makro.RecordingSettings.Clone(), _hotkeys)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() != true)
                return;

            Makro.RecordingSettings = dialog.Settings;
            ApplyRecordingHotkey();
            OnPropertyChanged(nameof(RecordingModeText));
            HasUnsavedChanges = true;
        }

        private async Task ToggleRecordAsync(bool triggeredByHotkey = false)
        {
            try
            {
                if (!IsRecording)
                {
                    _hotkeys.StartRecordHotkeys(Makro.RecordingSettings);
                    IsRecording = true;
                    _log.LogInformation("Makro-Aufnahme gestartet.");
                }
                else
                {
                    var events = _hotkeys.StopRecordHotkeys();
                    IsRecording = false;
                    _log.LogInformation("Makro-Aufnahme gestoppt. Events: {Count}", events?.Count ?? 0);

                    if (events == null || events.Count == 0) return;

                    var mappingSettings = Makro.RecordingSettings.Clone();
                    if (triggeredByHotkey)
                        mappingSettings.RemoveStopGesture = false;
                    var mapped = MakroRecordingMapper.Map(events, mappingSettings, _hotkeys.FormatKey, _hotkeys.FormatMouseButton);
                    var automaticGroups = mappingSettings.AutomaticMovementGroups
                        ? MakroGrouping.CreateAutomaticMovementGroups(mapped, Loc.Get("Ui.Macro.Group.Movement"))
                        : Array.Empty<MakroGruppe>();
                    var desktop = ScreenHelper.GetVirtualDesktopBounds();
                    var initialCursor = events.OfType<MouseMoveCaptured>().FirstOrDefault();
                    Makro.RecordedEnvironment = new MakroRecordedEnvironment
                    {
                        VirtualDesktopX = desktop.X,
                        VirtualDesktopY = desktop.Y,
                        VirtualDesktopWidth = desktop.Width,
                        VirtualDesktopHeight = desktop.Height,
                        RecordedAtUtc = DateTime.UtcNow,
                        StartCursorX = initialCursor?.X,
                        StartCursorY = initialCursor?.Y
                    };
                    int insertIndex = SelectedStep != null
                        ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                        : Steps.Count;

                    PushUndo();
                    foreach (var group in automaticGroups)
                        Groups.Add(group);
                    _stepItems.InsertRange(insertIndex, mapped);

                    if (mapped.Count > 0)
                    {
                        SelectedStep = mapped.Last();
                        HasUnsavedChanges = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler bei der Makro-Aufnahme.");
                IsRecording = false;
            }
        }

        private async Task CaptureClickAsync()
        {
            try
            {
                IsCapturingClick = true;
                _log.LogInformation("Einzelklick-Erfassung gestartet.");

                using var clickOverlay = new ClickCaptureOverlay();
                var clickPoint = await clickOverlay.CaptureClickAsync();

                _log.LogInformation("Klick erfasst bei: {X}, {Y}", clickPoint.X, clickPoint.Y);

                var moveCommand = new MouseMoveAbsoluteBefehl { X = clickPoint.X, Y = clickPoint.Y };

                int insertIndex = SelectedStep != null
                    ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                    : Steps.Count;

                Steps.Insert(insertIndex, moveCommand);
                SelectedStep = moveCommand;
                HasUnsavedChanges = true;
            }
            catch (OperationCanceledException)
            {
                _log.LogInformation("Einzelklick-Erfassung abgebrochen.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler bei der Einzelklick-Erfassung.");
            }
            finally
            {
                IsCapturingClick = false;
            }
        }

        // ---------- Preview ----------
        private bool _previewBusy;
        private bool CanPreview() => Steps.Count > 0 && !_previewBusy;

        private void EnsureOverlay()
        {
            if (_overlay != null) return;
            var v = ScreenHelper.GetVirtualDesktopBounds();
            _overlay = new Overlay(v.Left, v.Top, v.Width, v.Height);
            _overlay.RunInNewThread();
        }

        private void BuildPreview()
        {
            var v = ScreenHelper.GetVirtualDesktopBounds();
            // Build preview from current Steps (not saved Makro.Befehle)
            var tempMakro = new Makro { Name = Makro.Name, Befehle = new ObservableCollection<MakroBefehl>(Steps) };
            _lastPreview = _preview.Build(tempMakro, v, v);
        }

        private async void ShowOverview()
        {
            if (_previewBusy) return;
            _previewBusy = true;
            InvalidateAllCommands();
            StopPreview();
            await System.Threading.Tasks.Task.Delay(100);
            EnsureOverlay();
            BuildPreview();
            _overlay.AddItems(_lastPreview.StaticItems);
            await System.Threading.Tasks.Task.Delay(400);
            _previewBusy = false;
            InvalidateAllCommands();
        }

        private async void ShowPlayback(double speed = 1.0)
        {
            if (_previewBusy) return;
            _previewBusy = true;
            InvalidateAllCommands();
            StopPreview();
            await System.Threading.Tasks.Task.Delay(100);
            EnsureOverlay();
            BuildPreview();
            _overlay.AddItems(_lastPreview.StaticItems);
            _overlay.AddItems(_lastPreview.TimedItems);
            _overlay.PlaybackSpeed = speed;
            _overlay.StartPlayback(0.0);
            await System.Threading.Tasks.Task.Delay(400);
            _previewBusy = false;
            InvalidateAllCommands();
        }

        private void StopPreview()
        {
            if (_overlay == null) return;
            _overlay.StopPlayback();
            _overlay.ClearItems();
            _overlay.Dispose();
            _overlay = null;
            InvalidateAllCommands();
        }

        public new void Dispose()
        {
            _dispatcher.RunningMakrosChanged -= OnRunningMakrosChanged;
            _hotkeys.RecordingHotkeyPressed -= OnRecordingHotkeyPressed;
            Groups.CollectionChanged -= Groups_CollectionChanged;
            _hotkeys.ClearRecordingHotkey();
            StopPreview();
            _overlay?.Dispose();
            _overlay = null;
        }

        private void OnRunningMakrosChanged()
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                IsMakroRunning = _dispatcher.RunningMakroIds.Contains(Makro.Id);
            });
        }

        private void ApplyRecordingHotkey()
            => _hotkeys.SetRecordingHotkey(
                Makro.RecordingSettings.RecordingHotkeyModifiers,
                Makro.RecordingSettings.RecordingHotkeyVirtualKey);

        private void OnRecordingHotkeyPressed()
            => Application.Current?.Dispatcher?.InvokeAsync(() => _ = ToggleRecordAsync(triggeredByHotkey: true));

        // ---------- Command invalidation helper ----------
        private void InvalidateAllCommands()
        {
            (SaveCommand            as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand          as RelayCommand)?.RaiseCanExecuteChanged();
            (StartMakroCommand      as RelayCommand)?.RaiseCanExecuteChanged();
            (StopMakroCommand       as RelayCommand)?.RaiseCanExecuteChanged();
            (RecordStepsCommand     as RelayCommand)?.RaiseCanExecuteChanged();
            (CaptureClickCommand    as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviewOverviewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviewPlaybackCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviewStopCommand     as RelayCommand)?.RaiseCanExecuteChanged();
            (EditStepCommand        as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (MoveStepUpCommand      as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (MoveStepDownCommand    as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (DeleteStepCommand      as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (DuplicateStepCommand   as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (DeleteSelectedCommand  as RelayCommand)?.RaiseCanExecuteChanged();
            (CopyCommand            as RelayCommand)?.RaiseCanExecuteChanged();
            (PasteCommand           as RelayCommand)?.RaiseCanExecuteChanged();
            (UndoCommand            as RelayCommand)?.RaiseCanExecuteChanged();
            (RedoCommand            as RelayCommand)?.RaiseCanExecuteChanged();
            (CreateGroupCommand     as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveFromGroupCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RenameGroupCommand     as RelayCommand<string?>)?.RaiseCanExecuteChanged();
            (DissolveGroupCommand   as RelayCommand<string?>)?.RaiseCanExecuteChanged();
            (ToggleGroupCommand     as RelayCommand<string?>)?.RaiseCanExecuteChanged();
            (OpenRecordingSettingsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public sealed record MacroFilterOption(string Id, string Label);
    public abstract record MacroListItem;
    public sealed record MacroStepListItem(MakroBefehl Step, int Number) : MacroListItem;
    public sealed record MacroGroupListItem(string GroupId, string Title, string Summary, bool IsCollapsed) : MacroListItem;
    internal sealed record MacroSnapshot(List<MakroBefehl> Steps, List<MakroGruppe> Groups);
    internal sealed record GroupPresentation(string Title, string Summary);

    internal static class JsonOptions
    {
        public static readonly System.Text.Json.JsonSerializerOptions Default = new()
        {
            WriteIndented = false,
            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }
}
