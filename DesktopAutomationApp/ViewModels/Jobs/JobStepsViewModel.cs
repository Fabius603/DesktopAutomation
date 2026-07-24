using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;
using OpenCvSharp;
using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using TaskAutomation.Steps;
using DesktopAutomationApp.Views;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Behaviors;
using DesktopAutomationApp.Converters;
using DesktopAutomationApp.Services.Jobs;
using System.Threading;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class JobStepsViewModel : ViewModelBase, INavigationGuard
    {
        private readonly IJobExecutor _jobExecutionContext;
        private readonly ObservableRangeCollection<JobStep> _startSteps;
        private readonly ObservableRangeCollection<JobStep> _runSteps;
        private ObservableRangeCollection<JobStep> _steps;
        private readonly ObservableRangeCollection<JobStep> _endSteps;
        private readonly IJobApplicationService _jobAppService;
        private readonly IDialogService _dialogService;
        private readonly IJobDispatcher _dispatcher;
        private readonly ICameraCaptureService _cameraCaptureService;

        private sealed record JobStepsSnapshot(
            List<JobStep> StartSteps,
            List<JobStep> RunSteps,
            List<JobStep> EndSteps);

        private readonly Stack<JobStepsSnapshot> _undoStack = new();
        private readonly Stack<JobStepsSnapshot> _redoStack = new();
        private List<JobStep> _clipboard  = new();
        private List<JobStep> _savedSnapshot;
        private List<JobStep> _savedStartSnapshot;
        private List<JobStep> _savedEndSnapshot;
        private int _savedEndPhaseTimeoutSeconds;
        private CancellationTokenSource? _validationCts;
        private int _validationGeneration;
        private JobDebugSession? _debugSession;
        private readonly HashSet<JobStep> _subscribedSteps =
            new(ReferenceEqualityComparer.Instance);
        private readonly SemaphoreSlim _mutationGate = new(1, 1);
        private bool _isMutationBusy;
        private IReadOnlyList<JobStep> _allJobStepsSnapshot = Array.Empty<JobStep>();
        private int _collectionUpdateDepth;
        private bool _collectionRefreshPending;

        public sealed class DebugContextValue : ViewModelBase
        {
            private bool _isExpanded;

            public DebugContextValue(string key, JobDebugValueNode node, string? resultTypeName)
            {
                Key = key;
                Name = string.IsNullOrWhiteSpace(node.PropertyPath)
                    ? node.Name
                    : StepLocalization.PropertyPath(resultTypeName, node.PropertyPath);
                Value = LocalizeValue(node);
                TypeName = node.TypeName;
                ConditionState = node.TypeName == nameof(ConditionDebugState)
                    ? node.DisplayValue
                    : null;
                Description = ResolveDescription(resultTypeName, node)
                    ?? StepLocalization.DebugValueType(TypeName);
                Children = node.Children
                    .Select((child, index) => new DebugContextValue(
                        $"{key}/{index}:{child.Name}", child, resultTypeName))
                    .ToArray();
                _isExpanded = false;
            }

            public string Key { get; }
            public string Name { get; }
            public string Value { get; }
            public string TypeName { get; }
            public string? ConditionState { get; }
            public string Description { get; }
            public IReadOnlyList<DebugContextValue> Children { get; }
            public bool HasChildren => Children.Count > 0;
            public bool IsBoolean => TypeName == nameof(Boolean);
            public bool IsNull => TypeName == "null";
            public bool IsTrue => IsBoolean && Value == Loc.Get("Ui.Job.Debug.Value.True");
            public bool IsExpanded
            {
                get => _isExpanded;
                set => SetProperty(ref _isExpanded, value);
            }

            public void SetExpandedRecursively(bool expanded)
            {
                IsExpanded = expanded;
                foreach (var child in Children) child.SetExpandedRecursively(expanded);
            }

            private static string LocalizeValue(JobDebugValueNode node)
            {
                if (node.TypeName == nameof(ConditionDebugState))
                    return Loc.Get($"Ui.Job.Debug.Condition.State.{node.DisplayValue}");
                if (node.CollectionCount is { } count)
                    return Loc.Format("Ui.Job.Debug.Value.CollectionCount", count);
                if (node.Children.Count > 0)
                    return StepLocalization.DebugValueType(node.TypeName);
                if (node.TypeName == "null")
                    return Loc.Get("Ui.Job.Debug.Value.Null");
                if (node.TypeName == nameof(Boolean))
                    return Loc.Get(node.DisplayValue == bool.TrueString
                        ? "Ui.Job.Debug.Value.True"
                        : "Ui.Job.Debug.Value.False");

                var enumKey = $"Enum.{node.TypeName}.{node.DisplayValue}";
                var enumValue = Loc.Get(enumKey);
                return enumValue == $"[{enumKey}]" ? node.DisplayValue : enumValue;
            }

            private static string? ResolveDescription(string? resultTypeName, JobDebugValueNode node)
            {
                if (string.IsNullOrWhiteSpace(resultTypeName)
                    || string.IsNullOrWhiteSpace(node.PropertyPath)
                    || !StepResultMetadata.TryGetProperty(
                        resultTypeName, node.PropertyPath, out var property))
                    return null;
                var description = StepLocalization.PropertyDescription(resultTypeName, property);
                return string.IsNullOrWhiteSpace(description) ? null : description;
            }
        }

        public sealed class DebugContextGroup : ViewModelBase
        {
            private bool _isExpanded = true;

            public required string StepId { get; init; }
            public required string Title { get; init; }
            public required string Subtitle { get; init; }
            public required string Status { get; init; }
            public required string Summary { get; init; }
            public required JobStepDebugState State { get; init; }
            public required IReadOnlyList<DebugContextValue> Values { get; init; }
            public bool IsExpanded
            {
                get => _isExpanded;
                set => SetProperty(ref _isExpanded, value);
            }

            public void SetExpandedRecursively(bool expanded)
            {
                IsExpanded = expanded;
                foreach (var value in Values) value.SetExpandedRecursively(expanded);
            }
        }

        private readonly ObservableCollection<DebugContextGroup> _debugContextGroups = [];

        /// <summary>All currently selected steps (synced from the view's ListBox.SelectedItems).</summary>
        public List<JobStep> SelectedSteps { get; } = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public bool IsMutationBusy
        {
            get => _isMutationBusy;
            private set
            {
                if (_isMutationBusy == value) return;
                _isMutationBusy = value;
                OnPropertyChanged();
                InvalidateMutationCommands();
            }
        }

        public Job Job { get; }
        public string Title => Job.Name;

        public ObservableCollection<JobStep> Steps => _runSteps;
        public ObservableCollection<JobStep> StartSteps => _startSteps;
        public ObservableCollection<JobStep> EndSteps => _endSteps;
        public IReadOnlyList<JobStep> AllJobSteps => _allJobStepsSnapshot;

        private int _endPhaseTimeoutSeconds;
        public int EndPhaseTimeoutSeconds
        {
            get => _endPhaseTimeoutSeconds;
            set
            {
                var normalized = Math.Clamp(
                    value,
                    Job.MinEndPhaseTimeoutSeconds,
                    Job.MaxEndPhaseTimeoutSeconds);
                if (_endPhaseTimeoutSeconds == normalized) return;
                _endPhaseTimeoutSeconds = normalized;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public bool HasStartSteps => _startSteps.Count > 0;
        public bool HasSteps => _runSteps.Count > 0;
        public bool HasEndSteps => _endSteps.Count > 0;
        public bool HasStartStepErrors => _startSteps.Any(s => !s.IsValid);
        public bool HasStepErrors => _runSteps.Any(s => !s.IsValid);
        public bool HasEndStepErrors => _endSteps.Any(s => !s.IsValid);
        public int ValidationErrorCount => AllSteps().Count(step => !step.IsValid);
        public int SelectedStepCount => SelectedSteps.Count;
        public bool HasSelectedSteps => SelectedStepCount > 0;
        public string SelectedStepsSummary => Loc.Format("Ui.Job.Steps.SelectedCount", SelectedStepCount);
        public string ValidationSummary => Loc.Format("Ui.Job.Steps.ProblemCount", ValidationErrorCount);

        private bool _isStartSectionExpanded;
        public bool IsStartSectionExpanded
        {
            get => _isStartSectionExpanded;
            set { _isStartSectionExpanded = value; OnPropertyChanged(); }
        }

        private bool _isRunSectionExpanded = true;
        public bool IsRunSectionExpanded
        {
            get => _isRunSectionExpanded;
            set { _isRunSectionExpanded = value; OnPropertyChanged(); }
        }

        private bool _isEndSectionExpanded;
        public bool IsEndSectionExpanded
        {
            get => _isEndSectionExpanded;
            set { _isEndSectionExpanded = value; OnPropertyChanged(); }
        }

        /// <summary>Incrementiert bei jeder Listenänderung; wird von Konvertern als Cache-Schlüssel genutzt.</summary>
        public int StepsVersion { get; private set; }

        private JobStep? _selectedStep;
        public JobStep? SelectedStep
        {
            get => _selectedStep;
            set
            {
                if (ReferenceEquals(_selectedStep, value)) return;
                _selectedStep = value;
                OnPropertyChanged();
                NotifyDebugInspectorChanged();
                InvalidateSelectionCommands();
            }
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                if (_hasUnsavedChanges == value) return;
                _hasUnsavedChanges = value;
                OnPropertyChanged();
                InvalidateSaveCommands();
            }
        }

        private bool _isJobRunning;
        public bool IsJobRunning
        {
            get => _isJobRunning;
            private set
            {
                _isJobRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditContextVisible));
                OnPropertyChanged(nameof(IsRunContextVisible));
                InvalidateAllCommands();
            }
        }

        private bool _canRequestJobStop;
        public bool CanRequestJobStop
        {
            get => _canRequestJobStop;
            private set
            {
                if (_canRequestJobStop == value) return;
                _canRequestJobStop = value;
                OnPropertyChanged();
                (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool HasDebugSession => _debugSession != null;
        public bool IsEditContextVisible => !HasDebugSession && !IsJobRunning;
        public bool IsRunContextVisible => !HasDebugSession && IsJobRunning;
        public bool IsDebugActive => _debugSession?.State is JobDebugSessionState.Starting or JobDebugSessionState.Paused or JobDebugSessionState.Running;
        public bool IsDebugPaused => _debugSession?.State == JobDebugSessionState.Paused;
        public string DebugStatusText => LocalizeDebugStatus();
        public bool HasDebugIteration => (_debugSession?.Iteration ?? 0) > 0;
        public string DebugIterationText => HasDebugIteration
            ? Loc.Format("Ui.Job.Debug.Iteration", _debugSession!.Iteration)
            : string.Empty;
        private bool _isDebugPanelOpen = true;
        public bool IsDebugPanelOpen
        {
            get => _isDebugPanelOpen;
            set
            {
                if (_isDebugPanelOpen == value) return;
                _isDebugPanelOpen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDebugPanelVisible));
            }
        }
        public bool IsDebugPanelVisible => HasDebugSession && IsDebugPanelOpen;
        public ObservableCollection<DebugContextGroup> DebugContextGroups => _debugContextGroups;
        public bool HasDebugContext => _debugContextGroups.Count > 0;
        public string DebugContextResultCountText => Loc.Format(
            "Ui.Job.Debug.Panel.ResultCount", _debugContextGroups.Count);

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
        public ICommand StopJobCommand { get; }
        public ICommand DebugJobCommand { get; }
        public ICommand DebugStepCommand { get; }
        public ICommand DebugContinueCommand { get; }
        public ICommand CancelDebugCommand { get; }
        public ICommand CloseDebuggerCommand { get; }
        public ICommand ToggleDebugPanelCommand { get; }
        public ICommand ExpandDebugContextCommand { get; }
        public ICommand CollapseDebugContextCommand { get; }
        public ICommand ToggleBreakpointCommand { get; }
        public ICommand ToggleStepEnabledCommand { get; }
        public ICommand AddElseIfCommand { get; }
        public ICommand AddElseCommand { get; }
        public ICommand MoveToStartSectionCommand { get; }
        public ICommand MoveToRunSectionCommand { get; }
        public ICommand MoveToEndSectionCommand { get; }

        public event Action? RequestBack;

        public JobStepsViewModel(Job job, IJobExecutor jobExecutionContext, IJobApplicationService jobAppService, IDialogService dialogService, IJobDispatcher dispatcher, ICameraCaptureService cameraCaptureService)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            _jobExecutionContext = jobExecutionContext;
            _jobAppService = jobAppService;
            _dialogService = dialogService;
            _dispatcher = dispatcher;
            _cameraCaptureService = cameraCaptureService;

            _startSteps = new ObservableRangeCollection<JobStep>();
            _startSteps.ReplaceRange(Job.StartSteps ?? Enumerable.Empty<JobStep>());
            _runSteps = new ObservableRangeCollection<JobStep>();
            _runSteps.ReplaceRange(Job.Steps ?? Enumerable.Empty<JobStep>());
            _steps = _runSteps;
            _endSteps = new ObservableRangeCollection<JobStep>();
            _endSteps.ReplaceRange(Job.EndSteps ?? Enumerable.Empty<JobStep>());
            RefreshAllStepsSnapshot();
            _isStartSectionExpanded = _startSteps.Count > 0;
            _isEndSectionExpanded = _endSteps.Count > 0;
            _savedStartSnapshot = DeepCloneSteps(_startSteps);
            _savedSnapshot = DeepCloneSteps(_runSteps);
            _savedEndSnapshot = DeepCloneSteps(_endSteps);
            _endPhaseTimeoutSeconds = Math.Clamp(
                Job.EndPhaseTimeoutSeconds,
                Job.MinEndPhaseTimeoutSeconds,
                Job.MaxEndPhaseTimeoutSeconds);
            _savedEndPhaseTimeoutSeconds = _endPhaseTimeoutSeconds;

            // Wenn sich die Step-Liste ändert (hinzufügen, löschen, verschieben),
            // muss die Steps-Property neu notifiziert werden, damit alle MultiBinding-
            // Konverter in der View (StepPrerequisiteStateConverter) neu ausgewertet werden.
            _runSteps.CollectionChanged += OnSectionCollectionChanged;
            _startSteps.CollectionChanged += OnSectionCollectionChanged;
            _endSteps.CollectionChanged += OnSectionCollectionChanged;

            ReconcileStepSubscriptions();

            BackCommand   = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand   = new AsyncRelayCommand(Save, () => HasUnsavedChanges && !IsDebugActive && !IsMutationBusy);
            CancelCommand = new RelayCommand(DiscardChanges, () => HasUnsavedChanges && !IsDebugActive);
            RenameCommand = new AsyncRelayCommand(Rename, () => !IsDebugActive);
            OpenFileCommand = new RelayCommand(OpenFileInExplorer);

            AddStepCommand    = new AsyncRelayCommand(AddStep, () => !IsDebugActive && !IsMutationBusy);
            EditStepCommand   = new AsyncRelayCommand<JobStep?>(
                EditStep,
                s => { var t = s ?? SelectedStep; return !IsDebugActive && !IsMutationBusy && t != null && t is not TaskAutomation.Jobs.ElseStep and not TaskAutomation.Jobs.EndIfStep; });
            MoveStepUpCommand = new AsyncRelayCommand<JobStep?>(s => MoveRelativeAsync(s ?? SelectedStep, -1), s => !IsDebugActive && !IsMutationBusy && CanMoveRelative(s ?? SelectedStep, -1));
            MoveStepDownCommand = new AsyncRelayCommand<JobStep?>(s => MoveRelativeAsync(s ?? SelectedStep, +1), s => !IsDebugActive && !IsMutationBusy && CanMoveRelative(s ?? SelectedStep, +1));
            ReorderStepCommand = new AsyncRelayCommand<StepDragDrop.MoveRequest>(MoveStepAsync, _ => !IsDebugActive && !IsMutationBusy);
            DeleteStepCommand = new AsyncRelayCommand<JobStep?>(DeleteStepAsync, s => !IsDebugActive && !IsMutationBusy && (s ?? SelectedStep) != null);
            DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => !IsDebugActive && !IsMutationBusy && (SelectedSteps.Count > 0 || SelectedStep != null));
            UndoCommand           = new AsyncRelayCommand(UndoAsync, () => !IsDebugActive && !IsMutationBusy && CanUndo);
            RedoCommand           = new AsyncRelayCommand(RedoAsync, () => !IsDebugActive && !IsMutationBusy && CanRedo);
            CopyCommand           = new AsyncRelayCommand(CopySelectedAsync, () => !IsMutationBusy && (SelectedSteps.Count > 0 || SelectedStep != null));
            PasteCommand          = new AsyncRelayCommand(PasteAsync, () => !IsDebugActive && !IsMutationBusy && _clipboard.Count > 0);

            StopJobCommand = new RelayCommand(() =>
            {
                _dispatcher.CancelJobsByDefinition(Job.Id);
            }, () => CanRequestJobStop);
            DebugJobCommand = new RelayCommand(StartDebugJob,
                () => !IsJobRunning && !HasUnsavedChanges && AllSteps().Any(step => step.IsEnabled));
            DebugStepCommand = new RelayCommand(
                () =>
                {
                    if (_debugSession != null) _dispatcher.DebugStep(_debugSession.InstanceId);
                    InvalidateDebugCommands();
                },
                () => IsDebugPaused);
            DebugContinueCommand = new RelayCommand(
                () =>
                {
                    if (_debugSession != null) _dispatcher.DebugContinue(_debugSession.InstanceId);
                    InvalidateDebugCommands();
                },
                () => IsDebugPaused);
            CancelDebugCommand = new RelayCommand(
                () => { if (_debugSession != null) _dispatcher.CancelDebugJob(_debugSession.InstanceId); },
                () => IsDebugActive);
            CloseDebuggerCommand = new RelayCommand(CloseDebugger, () => HasDebugSession && !IsDebugActive);
            ToggleDebugPanelCommand = new RelayCommand(
                () => IsDebugPanelOpen = !IsDebugPanelOpen,
                () => HasDebugSession);
            ExpandDebugContextCommand = new RelayCommand(
                () => SetDebugContextExpanded(true),
                () => HasDebugContext);
            CollapseDebugContextCommand = new RelayCommand(
                () => SetDebugContextExpanded(false),
                () => HasDebugContext);
            ToggleBreakpointCommand = new RelayCommand<JobStep?>(
                ToggleBreakpoint,
                step => step != null);
            ToggleStepEnabledCommand = new RelayCommand<JobStep?>(
                step => { if (step != null) step.IsEnabled = !step.IsEnabled; },
                step => !IsDebugActive && step?.CanBeDisabled == true);

            AddElseIfCommand = new AsyncRelayCommand<JobStep?>(AddElseIfAsync, step => !IsDebugActive && !IsMutationBusy && CanAddElseIf(step));
            AddElseCommand   = new AsyncRelayCommand<JobStep?>(AddElseAsync, step => !IsDebugActive && !IsMutationBusy && CanAddElse(step));
            MoveToStartSectionCommand = new AsyncRelayCommand<JobStep?>(
                step => MoveStepToSectionAsync(step, _startSteps),
                step => !IsDebugActive && !IsMutationBusy && CanMoveStepToSection(step, _startSteps));
            MoveToRunSectionCommand = new AsyncRelayCommand<JobStep?>(
                step => MoveStepToSectionAsync(step, _runSteps),
                step => !IsDebugActive && !IsMutationBusy && CanMoveStepToSection(step, _runSteps));
            MoveToEndSectionCommand = new AsyncRelayCommand<JobStep?>(
                step => MoveStepToSectionAsync(step, _endSteps),
                step => !IsDebugActive && !IsMutationBusy && CanMoveStepToSection(step, _endSteps));

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;
            _debugSession = _dispatcher.DebugSessions.FirstOrDefault(session => session.JobId == Job.Id);
            if (_debugSession != null)
            {
                _debugSession.Changed += OnDebugSessionChanged;
                _debugSession.IterationChanged += OnDebugIterationChanged;
            }
            IsJobRunning = _dispatcher.RunningJobIds.Contains(Job.Id);
            CanRequestJobStop = _dispatcher.RunningJobInstances.Any(instance =>
                instance.JobId == Job.Id && instance.State.CanRequestStop());
            ScheduleValidation();
        }

        // ---------- Step property changes ----------
        private void OpenFileInExplorer()
            => ShowFileInExplorer(_jobAppService.GetStoragePath(), $"{Job.Id}.json");

        private static void ShowFileInExplorer(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo(File.Exists(path) ? path : directory) { UseShellExecute = true });
        }

        private void OnStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(JobStep.IsEnabled))
            {
                JobValidation.RemoveInvalidSourceSelections(AllSteps());
                HasUnsavedChanges = true;
                InvalidateStructureCommands();
                (DebugJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
                ScheduleValidation();
            }
        }

        private void OnSectionCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_collectionUpdateDepth > 0)
            {
                _collectionRefreshPending = true;
                return;
            }
            CompleteCollectionRefresh();
        }

        private void CompleteCollectionRefresh()
        {
            ReconcileStepSubscriptions();
            RefreshAllStepsSnapshot();
            StepsVersion++;
            OnPropertyChanged(nameof(StepsVersion));
            NotifySectionStateChanged();
            InvalidateStructureCommands();
            ScheduleValidation();
        }

        private void BeginCollectionUpdate() => _collectionUpdateDepth++;

        private void EndCollectionUpdate()
        {
            if (_collectionUpdateDepth == 0 || --_collectionUpdateDepth > 0) return;
            if (!_collectionRefreshPending) return;
            _collectionRefreshPending = false;
            CompleteCollectionRefresh();
        }

        private void RefreshAllStepsSnapshot()
        {
            _allJobStepsSnapshot = _startSteps.Concat(_runSteps).Concat(_endSteps).ToArray();
            OnPropertyChanged(nameof(AllJobSteps));
        }

        private void ReconcileStepSubscriptions()
        {
            var current = new HashSet<JobStep>(AllSteps(), ReferenceEqualityComparer.Instance);
            foreach (var removed in _subscribedSteps.Where(step => !current.Contains(step)).ToArray())
            {
                removed.PropertyChanged -= OnStepPropertyChanged;
                _subscribedSteps.Remove(removed);
            }
            foreach (var added in current.Where(step => !_subscribedSteps.Contains(step)))
            {
                added.PropertyChanged += OnStepPropertyChanged;
                _subscribedSteps.Add(added);
            }
        }

        private void StartDebugJob()
        {
            SynchronizeBreakpointsWithRuntimeJob();
            CloseDebugger();
            var session = _dispatcher.StartDebugJob(Job.Id);
            if (session == null) return;
            _debugSession = session;
            IsDebugPanelOpen = true;
            session.Changed += OnDebugSessionChanged;
            session.IterationChanged += OnDebugIterationChanged;
            NotifyDebugStateChanged();
        }

        private void ToggleBreakpoint(JobStep? step)
        {
            if (step == null) return;
            step.IsBreakpoint = !step.IsBreakpoint;
            SynchronizeBreakpointWithRuntimeJob(step);
        }

        private void SynchronizeBreakpointsWithRuntimeJob()
        {
            foreach (var step in AllSteps())
                SynchronizeBreakpointWithRuntimeJob(step);
        }

        private void SynchronizeBreakpointWithRuntimeJob(JobStep source)
        {
            var runtimeJob = _jobExecutionContext.AllJobs.Values
                .FirstOrDefault(candidate => candidate.Id == Job.Id);
            var runtimeStep = runtimeJob?.StartSteps
                .Concat(runtimeJob.Steps)
                .Concat(runtimeJob.EndSteps)
                .FirstOrDefault(candidate => candidate.Id == source.Id);
            if (runtimeStep != null)
                runtimeStep.IsBreakpoint = source.IsBreakpoint;
        }

        private void CloseDebugger()
        {
            if (_debugSession != null)
            {
                _debugSession.Changed -= OnDebugSessionChanged;
                _debugSession.IterationChanged -= OnDebugIterationChanged;
            }
            _debugSession = null;
            foreach (var step in AllSteps())
            {
                step.DebugState = JobStepDebugState.None;
                step.DebugDetails = null;
            }
            IsDebugPanelOpen = false;
            NotifyDebugStateChanged();
        }

        private void OnDebugSessionChanged()
            => Application.Current?.Dispatcher?.InvokeAsync(NotifyDebugStateChanged);

        private void OnDebugIterationChanged()
            => Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(HasDebugIteration));
                OnPropertyChanged(nameof(DebugIterationText));
            });

        private void NotifyDebugStateChanged()
        {
            if (_debugSession?.CurrentStepId is { } currentStepId)
            {
                var currentStep = AllSteps().FirstOrDefault(step => step.Id == currentStepId);
                if (currentStep != null && !ReferenceEquals(SelectedStep, currentStep))
                    SelectedStep = currentStep;
            }
            OnPropertyChanged(nameof(HasDebugSession));
            OnPropertyChanged(nameof(IsDebugActive));
            OnPropertyChanged(nameof(IsDebugPaused));
            OnPropertyChanged(nameof(DebugStatusText));
            OnPropertyChanged(nameof(HasDebugIteration));
            OnPropertyChanged(nameof(DebugIterationText));
            OnPropertyChanged(nameof(IsDebugPanelVisible));
            OnPropertyChanged(nameof(IsEditContextVisible));
            OnPropertyChanged(nameof(IsRunContextVisible));
            NotifyDebugInspectorChanged();
            InvalidateSelectionCommands();
            InvalidateDebugCommands();
        }

        private void NotifyDebugInspectorChanged()
        {
            RebuildDebugContext();
            OnPropertyChanged(nameof(HasDebugContext));
            OnPropertyChanged(nameof(DebugContextResultCountText));
            (ExpandDebugContextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CollapseDebugContextCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void RebuildDebugContext()
        {
            var groupExpansion = _debugContextGroups.ToDictionary(group => group.StepId, group => group.IsExpanded);
            var valueExpansion = new Dictionary<string, bool>();
            foreach (var group in _debugContextGroups)
                foreach (var value in group.Values)
                    CaptureValueExpansion(value, valueExpansion);

            _debugContextGroups.Clear();
            if (_debugSession == null) return;

            var snapshots = _debugSession.GetSnapshots().ToDictionary(snapshot => snapshot.StepId);
            var steps = AllSteps();
            var visibleStepIds = steps
                .Where(step => step.IsEnabled
                    && snapshots.TryGetValue(step.Id, out var snapshot)
                    && snapshot.State is JobStepDebugState.Completed or JobStepDebugState.Skipped or JobStepDebugState.Failed)
                .Select(step => step.Id)
                .ToArray();
            var newestStepId = visibleStepIds.LastOrDefault();

            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                if (!step.IsEnabled
                    || !snapshots.TryGetValue(step.Id, out var snapshot)
                    || snapshot.State is not (JobStepDebugState.Completed or JobStepDebugState.Skipped or JobStepDebugState.Failed))
                    continue;

                var outputNodes = snapshot.ConditionEvaluation is { } conditionEvaluation
                    ? BuildConditionDebugNodes(conditionEvaluation, steps)
                    : snapshot.OutputValues;
                var values = outputNodes
                    .Select((node, nodeIndex) => new DebugContextValue(
                        $"{step.Id}/{nodeIndex}:{node.Name}", node, snapshot.ResultTypeName))
                    .ToArray();
                foreach (var value in values) RestoreValueExpansion(value, valueExpansion);
                var summary = string.Join(" · ", values
                    .Where(value => !value.HasChildren)
                    .Take(2)
                    .Select(value => $"{value.Name}: {value.Value}"));
                if (string.IsNullOrWhiteSpace(summary))
                    summary = values.FirstOrDefault() is { } first
                        ? $"{first.Name}: {first.Value}"
                        : Loc.Get("Ui.Job.Debug.Panel.NoReturnValues");

                var iteration = snapshot.Iteration > 0
                    ? $" · {Loc.Format("Ui.Job.Debug.Iteration", snapshot.Iteration)}"
                    : string.Empty;
                var numberingScope = _startSteps.Contains(step)
                    ? _startSteps
                    : _endSteps.Contains(step)
                        ? _endSteps
                        : _steps;
                var displayNumber = StepLocalization.DisplayNumber(numberingScope, step);
                var stepTitle = displayNumber.HasValue
                    ? $"{displayNumber.Value}. {StepLocalization.Type(snapshot.StepType)}"
                    : StepLocalization.Type(snapshot.StepType);
                _debugContextGroups.Add(new DebugContextGroup
                {
                    StepId = step.Id,
                    Title = stepTitle,
                    Subtitle = $"{LocalizeDebugState(snapshot.State)} · {LocalizeDebugPhase(snapshot.Phase)}{iteration}",
                    Status = LocalizeDebugState(snapshot.State),
                    Summary = summary,
                    State = snapshot.State,
                    Values = values,
                    IsExpanded = groupExpansion.TryGetValue(step.Id, out var expanded)
                        ? expanded
                        : step.Id == newestStepId
                });
            }
        }

        private static IReadOnlyList<JobDebugValueNode> BuildConditionDebugNodes(
            ConditionDebugEvaluation evaluation,
            IList<JobStep> steps)
        {
            var conditionNodes = evaluation.Conditions
                .Select((item, index) =>
                {
                    var children = new List<JobDebugValueNode>
                    {
                        new(
                            Loc.Get("Ui.Job.Debug.Condition.Expression"),
                            ConditionDisplayFormatter.Format(item.Definition, steps as System.Collections.IList),
                            "String",
                            []),
                        new(
                            Loc.Get("Ui.Job.Debug.Condition.ActualValue"),
                            item.ActualValue ?? string.Empty,
                            "String",
                            []),
                        new(
                            Loc.Get("Ui.Job.Debug.Condition.ExpectedValue"),
                            item.ExpectedValue ?? string.Empty,
                            "String",
                            [])
                    };
                    if (!string.IsNullOrWhiteSpace(item.Diagnostic))
                        children.Add(new JobDebugValueNode(
                            Loc.Get("Ui.Job.Debug.Condition.Diagnostic"),
                            item.Diagnostic,
                            "String",
                            []));
                    return new JobDebugValueNode(
                        Loc.Format("Ui.Job.Debug.Condition.Number", index + 1),
                        item.State.ToString(),
                        nameof(ConditionDebugState),
                        children);
                })
                .ToArray();

            var mode = evaluation.MatchMode == ConditionMatchMode.All
                ? Loc.Get("Ui.Step.Settings.AllAND")
                : Loc.Get("Ui.Step.Settings.OneOR");
            var nodes = new List<JobDebugValueNode>
            {
                new(Loc.Get("Ui.Step.Settings.ConditionMatchMode"), mode, "String", []),
                new(
                    Loc.Get("Ui.Job.Debug.Condition.OverallResult"),
                    evaluation.State.ToString(),
                    nameof(ConditionDebugState),
                    []),
                new(
                    Loc.Get("Ui.Job.Debug.Condition.Branch"),
                    evaluation.BranchExecuted
                        ? Loc.Get("Ui.Job.Debug.Condition.Executed")
                        : Loc.Get("Ui.Job.Debug.Condition.Skipped"),
                    "String",
                    []),
                new(
                    Loc.Get("Ui.Job.Steps.DetailsConditions"),
                    $"{conditionNodes.Length}",
                    "Collection",
                    conditionNodes,
                    CollectionCount: conditionNodes.Length)
            };
            if (!string.IsNullOrWhiteSpace(evaluation.Diagnostic))
                nodes.Add(new JobDebugValueNode(
                    Loc.Get("Ui.Job.Debug.Condition.Diagnostic"),
                    evaluation.Diagnostic,
                    "String",
                    []));
            return nodes;
        }

        private static void CaptureValueExpansion(DebugContextValue value, IDictionary<string, bool> states)
        {
            states[value.Key] = value.IsExpanded;
            foreach (var child in value.Children) CaptureValueExpansion(child, states);
        }

        private static void RestoreValueExpansion(DebugContextValue value, IReadOnlyDictionary<string, bool> states)
        {
            if (states.TryGetValue(value.Key, out var expanded)) value.IsExpanded = expanded;
            foreach (var child in value.Children) RestoreValueExpansion(child, states);
        }

        private static string LocalizeDebugState(JobStepDebugState state) =>
            Loc.Get($"Ui.Job.Debug.State.{state}");

        private string LocalizeDebugStatus()
        {
            if (_debugSession == null) return string.Empty;
            var step = _debugSession.CurrentStepId is { } stepId
                ? AllSteps().FirstOrDefault(candidate => candidate.Id == stepId)
                : null;
            var stepName = step is null
                ? string.Empty
                : StepLocalization.Type(step.GetType());
            var phase = LocalizeDebugPhase(_debugSession.Phase);

            return _debugSession.State switch
            {
                JobDebugSessionState.Starting => Loc.Get("Ui.Job.Debug.Status.Starting"),
                JobDebugSessionState.Running => Loc.Format(
                    "Ui.Job.Debug.Status.Running", phase, stepName),
                JobDebugSessionState.Paused when _debugSession.StatusText.StartsWith(
                    "Fehler in ", StringComparison.Ordinal) => Loc.Format(
                        "Ui.Job.Debug.Status.Error",
                        stepName,
                        _debugSession.StatusText.Split(": ", 2).ElementAtOrDefault(1) ?? string.Empty),
                JobDebugSessionState.Paused when _debugSession.IsAtIterationEnd => Loc.Format(
                    "Ui.Job.Debug.Status.IterationCompleted", _debugSession.Iteration),
                JobDebugSessionState.Paused => Loc.Format(
                    "Ui.Job.Debug.Status.Paused", phase, stepName),
                JobDebugSessionState.Completed => Loc.Get("Ui.Job.Debug.Status.Completed"),
                JobDebugSessionState.Cancelled => Loc.Get("Ui.Job.Debug.Status.Cancelled"),
                JobDebugSessionState.Failed => Loc.Get("Ui.Job.Debug.Status.Failed"),
                _ => _debugSession.StatusText
            };
        }

        private static string LocalizeDebugPhase(string phase)
        {
            var key = phase switch
            {
                "Startphase" => "Start",
                "Hauptphase" or "Durchlauf" => "Run",
                "Endphase" => "End",
                _ => null
            };
            return key is null ? phase : Loc.Get($"Ui.Job.Debug.Phase.{key}");
        }

        private void SetDebugContextExpanded(bool expanded)
        {
            foreach (var group in _debugContextGroups) group.SetExpandedRecursively(expanded);
        }

        private void NotifySectionStateChanged()
        {
            OnPropertyChanged(nameof(HasStartSteps));
            OnPropertyChanged(nameof(HasSteps));
            OnPropertyChanged(nameof(HasEndSteps));
            OnPropertyChanged(nameof(HasStartStepErrors));
            OnPropertyChanged(nameof(HasStepErrors));
            OnPropertyChanged(nameof(HasEndStepErrors));
            OnPropertyChanged(nameof(ValidationErrorCount));
            OnPropertyChanged(nameof(ValidationSummary));
            OnPropertyChanged(nameof(AllJobSteps));
        }

        // ---------- Selection sync (called from code-behind) ----------
        public void SetSelectedSteps(IEnumerable<object> items, System.Collections.IList? section = null)
        {
            if (section is ObservableRangeCollection<JobStep> typedSection && IsKnownSection(typedSection))
                _steps = typedSection;
            SelectedSteps.Clear();
            SelectedSteps.AddRange(items.OfType<JobStep>());
            OnPropertyChanged(nameof(SelectedStepCount));
            OnPropertyChanged(nameof(HasSelectedSteps));
            OnPropertyChanged(nameof(SelectedStepsSummary));
            // Keep SelectedStep in sync with the last selected item
            if (SelectedSteps.Count > 0)
                SelectedStep = SelectedSteps[^1];
            InvalidateAllCommands();
        }

        // ---------- INavigationGuard ----------
        public async Task SaveAsync() => await Save();

        public void DiscardChanges()
        {
            BeginCollectionUpdate();
            try
            {
                _startSteps.ReplaceRange(DeepCloneSteps(_savedStartSnapshot));
                _runSteps.ReplaceRange(DeepCloneSteps(_savedSnapshot));
                _endSteps.ReplaceRange(DeepCloneSteps(_savedEndSnapshot));
            }
            finally
            {
                EndCollectionUpdate();
            }
            Job.StartSteps = DeepCloneSteps(_savedStartSnapshot);
            Job.Steps = DeepCloneSteps(_savedSnapshot);
            Job.EndSteps = DeepCloneSteps(_savedEndSnapshot);
            _endPhaseTimeoutSeconds = _savedEndPhaseTimeoutSeconds;
            Job.EndPhaseTimeoutSeconds = _savedEndPhaseTimeoutSeconds;
            OnPropertyChanged(nameof(EndPhaseTimeoutSeconds));
            HasUnsavedChanges = false;
            ScheduleValidation();
        }

        // ---------- Save ----------
        private async Task Save()
        {
            JobValidation.RemoveInvalidSourceSelections(AllSteps());
            _validationCts?.Cancel();
            var generation = ++_validationGeneration;
            var serialized = await JobStepsSnapshotService.SerializeAsync(
                _startSteps.ToArray(), _runSteps.ToArray(), _endSteps.ToArray());
            var materialized = await JobStepsSnapshotService.DeserializeAsync(serialized);
            var validation = await Task.Run(() => JobValidation.ValidateJob(new Job
            {
                StartSteps = materialized.StartSteps.ToList(),
                Steps = materialized.RunSteps.ToList(),
                EndSteps = materialized.EndSteps.ToList(),
                EndPhaseTimeoutSeconds = EndPhaseTimeoutSeconds
            }));
            ApplyValidation(validation, generation);
            if (!validation.IsValid)
            {
                var errors = validation.Steps.Where(s => !s.IsValid).Select(s => s.Error).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct();
                MessageBox.Show(string.Join(Environment.NewLine, errors), "Job kann nicht gespeichert werden", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Job.StartSteps = _startSteps.ToList();
            Job.Steps = _runSteps.ToList();
            Job.EndSteps = _endSteps.ToList();
            Job.EndPhaseTimeoutSeconds = EndPhaseTimeoutSeconds;
            await _jobAppService.SaveJobAsync(Job);
            var savedSerialized = await JobStepsSnapshotService.SerializeAsync(
                _startSteps.ToArray(), _runSteps.ToArray(), _endSteps.ToArray());
            var savedMaterialized = await JobStepsSnapshotService.DeserializeAsync(savedSerialized);
            _savedStartSnapshot = savedMaterialized.StartSteps.ToList();
            _savedSnapshot = savedMaterialized.RunSteps.ToList();
            _savedEndSnapshot = savedMaterialized.EndSteps.ToList();
            _savedEndPhaseTimeoutSeconds = EndPhaseTimeoutSeconds;
            HasUnsavedChanges = false;
        }

        // ---------- Rename ----------
        private async Task Rename()
        {
            var newName = await _dialogService.AskForNameAsync(Loc.Get("Common.Rename"), Loc.Get("Dialog.NewName"), Job.Name);
            if (newName == null) return;

            Job.Name = newName.Trim();
            OnPropertyChanged(nameof(Title));
            await _jobAppService.SaveJobAsync(Job);
        }

        // ---------- Add / Edit ----------
        private async Task AddStep()
        {
            // Determine insert position before opening the dialog so the
            // dialog receives the correct preceding-steps snapshot.
            int insertIndex = SelectedStep != null
                ? Math.Min(_steps.Count, _steps.IndexOf(SelectedStep) + 1)
                : _steps.Count;

            var precedingSteps = GetPrecedingSteps(_steps, insertIndex);
            var allSteps = AllSteps();
            var preparedSources = await PrepareDialogSourcesAsync(precedingSteps);
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id, allSteps, preparedSources, _cameraCaptureService)
                { Mode = StepDialogMode.Add };

            ShowDialogWithVm(vm, out bool? result);

            if (result == true && vm.CreatedStep != null)
            {
                // Prevent nesting: IfStep cannot be inside an existing block.
                // Automatically advance to the next valid (non-nested) position.
                if (vm.CreatedStep is TaskAutomation.Jobs.IfStep && CountOpenBlocksAt(insertIndex) > 0)
                {
                    while (insertIndex < _steps.Count && CountOpenBlocksAt(insertIndex) > 0)
                        insertIndex++;
                }

                await RunMutationAsync(async () =>
                {
                    await PushUndoAsync();
                    var insertion = vm.CreatedStep is TaskAutomation.Jobs.IfStep
                        ? new JobStep[] { vm.CreatedStep, new TaskAutomation.Jobs.EndIfStep() }
                        : [vm.CreatedStep];
                    _steps.InsertRange(insertIndex, insertion);
                // If-Abfrage: automatisch EndIf direkt dahinter einfügen
                    SelectedStep = vm.CreatedStep;
                    HasUnsavedChanges = true;
                });
            }
        }

        private static bool ShowDialogWithVm(AddJobStepDialogViewModel vm, out bool? dialogResult)
        {
            var dlg = new AddJobStepDialog { Owner = Application.Current.MainWindow, DataContext = vm };
            void OnRequestClose(bool ok) => dlg.DialogResult = ok;
            vm.RequestClose += OnRequestClose;
            var res = dlg.ShowDialog();
            vm.RequestClose -= OnRequestClose;
            dialogResult = res;
            return res == true;
        }

        private async Task EditStep(JobStep? step = null)
        {
            var target = step ?? SelectedStep;
            if (target == null) return;
            if (FindSection(target) is { } section) _steps = section;

            var idx = _steps.IndexOf(target);
            if (idx < 0) return;

            // Only steps before the edited one count as "preceding" for
            // prerequisite evaluation.
            var precedingSteps = GetPrecedingSteps(_steps, idx);
            var allSteps = AllSteps();
            var preparedSources = await PrepareDialogSourcesAsync(precedingSteps);
            var vm = new AddJobStepDialogViewModel(
                _jobExecutionContext, precedingSteps, Job.Id, allSteps, preparedSources, _cameraCaptureService);
            using (vm.DeferNotifications())
            {
                vm.Mode = StepDialogMode.Edit;
                vm.IsTypeLocked = target is TaskAutomation.Jobs.ElseIfStep;
                Prefill(vm, target);
            }

            ShowDialogWithVm(vm, out bool? result);

            if (result != true || vm.CreatedStep == null) return;

            vm.CreatedStep.Id = target.Id;   // preserve original ID
            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                _steps[idx] = vm.CreatedStep;
                SelectedStep = vm.CreatedStep;
                HasUnsavedChanges = true;
            });
        }

        // ---------- Prefill ----------
        private static void Prefill(AddJobStepDialogViewModel vm, JobStep s)
        {
            switch (s)
            {
                case TemplateMatchingStep t:
                    vm.SelectedType = "TemplateMatching";
                    vm.TemplateMatchingStep_TemplatePath = t.Settings.TemplatePath;
                    vm.TemplateMatchingStep_TemplateMatchMode = t.Settings.TemplateMatchMode;
                    vm.TemplateMatchingStep_ConfidenceThreshold = t.Settings.ConfidenceThreshold;
                    vm.TemplateMatchingStep_EnableROI = t.Settings.EnableROI;
                    vm.TemplateMatchingStep_RoiX = t.Settings.ROI.X;
                    vm.TemplateMatchingStep_RoiY = t.Settings.ROI.Y;
                    vm.TemplateMatchingStep_RoiW = t.Settings.ROI.Width;
                    vm.TemplateMatchingStep_RoiH = t.Settings.ROI.Height;
                    vm.TemplateMatchingStep_ImageSource.Load(t.Settings.ImageSource);
                    vm.DetectionDynamicRoiSource.Load(t.Settings.DynamicRoiSource);
                    vm.UseDynamicRoi = t.Settings.DynamicRoiSource.IsConfigured;
                    break;

                case ColorDetectionStep cd:
                    vm.SelectedType = "ColorDetection";
                    vm.ColorDetectionStep_Color = HexToWpfColor(cd.Settings.ColorHex);
                    vm.ColorDetectionStep_ConfidenceThreshold = cd.Settings.ConfidenceThreshold;
                    vm.ColorDetectionStep_MinSize = cd.Settings.MinSize;
                    vm.ColorDetectionStep_MaxSize = cd.Settings.MaxSize;
                    vm.ColorDetectionStep_MinWidth = cd.Settings.MinWidth;
                    vm.ColorDetectionStep_MinHeight = cd.Settings.MinHeight;
                    vm.ColorDetectionStep_DownscaleFactor = cd.Settings.DownscaleFactor;
                    vm.ColorDetectionStep_EnableROI = cd.Settings.EnableROI;
                    vm.ColorDetectionStep_RoiX = cd.Settings.ROI.X;
                    vm.ColorDetectionStep_RoiY = cd.Settings.ROI.Y;
                    vm.ColorDetectionStep_RoiW = cd.Settings.ROI.Width;
                    vm.ColorDetectionStep_RoiH = cd.Settings.ROI.Height;
                    vm.ColorDetectionStep_ImageSource.Load(cd.Settings.ImageSource);
                    vm.DetectionDynamicRoiSource.Load(cd.Settings.DynamicRoiSource);
                    vm.UseDynamicRoi = cd.Settings.DynamicRoiSource.IsConfigured;
                    break;

                case PredictMovementStep pm:
                    vm.SelectedType = "PredictMovement";
                    vm.PredictMovementStep_PointsSource.Load(pm.Settings.PointsSource);
                    vm.PredictMovementStep_MinSamples = pm.Settings.MinSamples;
                    vm.PredictMovementStep_PredictionMs = pm.Settings.PredictionMs;
                    vm.PredictMovementStep_ResetDistanceThreshold = pm.Settings.ResetDistanceThreshold;
                    vm.PredictMovementStep_MaxSampleAgeMs = pm.Settings.MaxSampleAgeMs;
                    vm.PredictMovementStep_PredictionModel = pm.Settings.PredictionModel;
                    vm.PredictMovementStep_TimeBasis = pm.Settings.TimeBasis;
                    vm.PredictMovementStep_MaxPredictionDistance = pm.Settings.MaxPredictionDistance;
                    vm.PredictMovementStep_MaxFitError = pm.Settings.MaxFitError;
                    vm.PredictMovementStep_MinimumConfidence = pm.Settings.MinimumConfidence;
                    break;

                case DesktopDuplicationStep d:
                    vm.SelectedType = "DesktopDuplication";
                    vm.DesktopDuplicationStep_DesktopIdx    = d.Settings.DesktopIdx;
                    vm.DesktopDuplicationStep_CaptureCursor = d.Settings.CaptureCursor;
                    break;

                case CameraCaptureStep camera:
                    vm.SelectedType = "CameraCapture";
                    vm.LoadCameraSelection(camera.Settings.CameraId, camera.Settings.CameraName);
                    break;

                case ShowImageStep si:
                    vm.SelectedType = "ShowImage";
                    vm.ShowImageStep_WindowName = si.Settings.WindowName;
                    vm.ShowImageStep_ImageSource.Load(si.Settings.ImageSource);
                    vm.ShowImageStep_DetectionsSource.Load(si.Settings.DetectionsSource);
                    break;

                case ShowOnDesktopStep sod:
                    vm.SelectedType = "ShowOnDesktop";
                    vm.ShowOnDesktopStep_DetectionsSource.Load(sod.Settings.DetectionsSource);
                    break;

                case VideoCreationStep v:
                    vm.SelectedType = "VideoCreation";
                    vm.VideoCreationStep_SavePath = v.Settings.SavePath;
                    vm.VideoCreationStep_FileName = v.Settings.FileName;
                    vm.VideoCreationStep_ImageSource.Load(v.Settings.ImageSource);
                    vm.VideoCreationStep_DetectionsSource.Load(v.Settings.DetectionsSource);
                    break;

                case MakroExecutionStep me:
                    vm.SelectedType = "MakroExecution";
                    if (me.Settings.MakroId.HasValue)
                        vm.MakroExecutionStep_SelectedMakro = vm.AvailableMakros.FirstOrDefault(m => m.Id == me.Settings.MakroId.Value);
                    if (vm.MakroExecutionStep_SelectedMakro == null && !string.IsNullOrWhiteSpace(me.Settings.MakroName))
                        vm.MakroExecutionStep_SelectedMakro = vm.AvailableMakros.FirstOrDefault(m => string.Equals(m.Name, me.Settings.MakroName, StringComparison.OrdinalIgnoreCase));
                    break;

                case ScriptExecutionStep se:
                    vm.SelectedType = "ScriptExecution";
                    vm.ScriptExecutionStep_ScriptPath = se.Settings.ScriptPath;
                    vm.ScriptExecutionStep_Arguments = se.Settings.Arguments;
                    vm.ScriptExecutionStep_WaitForExit = se.Settings.WaitForExit;
                    break;

                case KlickOnPointStep kp:
                    vm.SelectedType = "KlickOnPoint";
                    vm.KlickOnPointStep_ClickType = kp.Settings.ClickType;
                    vm.KlickOnPointStep_DoubleClick = kp.Settings.DoubleClick;
                    vm.KlickOnPointStep_TimeoutMs = kp.Settings.TimeoutMs;
                    vm.KlickOnPointStep_OffsetX = kp.Settings.OffsetX;
                    vm.KlickOnPointStep_OffsetY = kp.Settings.OffsetY;
                    vm.KlickOnPointStep_PointsSource.Load(kp.Settings.PointsSource);
                    break;

                case KlickOnPoint3DStep kp3d:
                    vm.SelectedType = "KlickOnPoint3D";
                    vm.KlickOnPoint3DStep_DoubleClick = kp3d.Settings.DoubleClick;
                    vm.KlickOnPoint3DStep_ClickType = kp3d.Settings.ClickType;
                    vm.KlickOnPoint3DStep_Timeout = kp3d.Settings.TimeoutMs;
                    vm.KlickOnPoint3DStep_OriginX = kp3d.Settings.OriginX;
                    vm.KlickOnPoint3DStep_OriginY = kp3d.Settings.OriginY;
                    vm.KlickOnPoint3DStep_OffsetX = kp3d.Settings.OffsetX;
                    vm.KlickOnPoint3DStep_OffsetY = kp3d.Settings.OffsetY;
                    vm.KlickOnPoint3DStep_PointsSource.Load(kp3d.Settings.PointsSource);
                    break;

                case JobExecutionStep je:
                    vm.SelectedType = "JobExecution";
                    if (je.Settings.JobId.HasValue)
                        vm.JobExecutionStep_SelectedJob = vm.AvailableJobs.FirstOrDefault(j => j.Id == je.Settings.JobId.Value);
                    if (vm.JobExecutionStep_SelectedJob == null && !string.IsNullOrWhiteSpace(je.Settings.JobName))
                        vm.JobExecutionStep_SelectedJob = vm.AvailableJobs.FirstOrDefault(j => string.Equals(j.Name, je.Settings.JobName, StringComparison.OrdinalIgnoreCase));
                    vm.JobExecutionStep_WaitForCompletion = je.Settings.WaitForCompletion;
                    break;

                case YOLODetectionStep yd:
                    vm.SelectedType = "YoloDetection";
                    vm.YoloDetectionStep_Model = yd.Settings.Model;
                    vm.YoloDetectionStep_ConfidenceThreshold = yd.Settings.ConfidenceThreshold;
                    vm.YoloDetectionStep_ClassName = yd.Settings.ClassName;
                    vm.YoloDetectionStep_EnableROI = yd.Settings.EnableROI;
                    vm.YoloDetectionStep_RoiX = yd.Settings.ROI.X;
                    vm.YoloDetectionStep_RoiY = yd.Settings.ROI.Y;
                    vm.YoloDetectionStep_RoiW = yd.Settings.ROI.Width;
                    vm.YoloDetectionStep_RoiH = yd.Settings.ROI.Height;
                    vm.YoloDetectionStep_ImageSource.Load(yd.Settings.ImageSource);
                    vm.DetectionDynamicRoiSource.Load(yd.Settings.DynamicRoiSource);
                    vm.UseDynamicRoi = yd.Settings.DynamicRoiSource.IsConfigured;
                    break;

                case TimeoutStep to:
                    vm.SelectedType = "Timeout";
                    vm.TimeoutStep_DelayMs = to.Settings.DelayMs;
                    break;
                case BlockInputStep blockInput:
                    vm.SelectedType = "BlockInput";
                    vm.BlockInputStep_SafetyTimeoutSeconds = blockInput.Settings.SafetyTimeoutSeconds;
                    break;
                case UnblockInputStep:
                    vm.SelectedType = "UnblockInput";
                    break;

                case ActiveProcessStep ap:
                    vm.SelectedType = "ActiveProcess";
                    vm.ActiveProcessStep_ProcessName = ap.Settings.Target.ProcessName;
                    vm.ActiveProcessStep_ProcessSource.Load(ap.Settings.Target.ProcessSource);
                    vm.ActiveProcessStep_UsesProcessSource = ap.Settings.Target.ProcessSource.IsConfigured;
                    break;

                case GetProcessStep gp:
                    vm.SelectedType = "GetProcess";
                    vm.GetProcessStep_ProcessName = gp.Settings.Query.ProcessName;
                    vm.GetProcessStep_ExecutablePath = gp.Settings.Query.ExecutablePath;
                    vm.GetProcessStep_WindowTitleContains = gp.Settings.Query.WindowTitleContains;
                    break;

                case StartProcessStep sp:
                    vm.SelectedType = sp.Settings.Action == StartProcessAction.Terminate
                        ? "TerminateProcess"
                        : "StartProcess";
                    vm.StartProcessStep_Action = sp.Settings.Action;
                    vm.StartProcessStep_ExecutablePath = sp.Settings.ExecutablePath;
                    vm.StartProcessStep_ProcessName = sp.Settings.Target.ProcessName;
                    vm.StartProcessStep_WindowTitleContains = sp.Settings.Target.WindowTitleContains;
                    vm.StartProcessStep_Arguments      = sp.Settings.Arguments;
                    vm.StartProcessStep_WorkingDirectory = sp.Settings.WorkingDirectory;
                    vm.StartProcessStep_WaitForExit    = sp.Settings.WaitForExit;
                    vm.StartProcessStep_MonitorIndex = sp.Settings.MonitorIndex;
                    vm.StartProcessStep_PlacementMode = sp.Settings.PlacementMode;
                    vm.StartProcessStep_OffsetX = sp.Settings.OffsetX;
                    vm.StartProcessStep_OffsetY = sp.Settings.OffsetY;
                    vm.StartProcessStep_WindowMode = sp.Settings.WindowMode;
                    vm.StartProcessStep_ProcessSource.Load(sp.Settings.Target.ProcessSource);
                    vm.StartProcessStep_UsesProcessSource = sp.Settings.Target.ProcessSource.IsConfigured;
                    break;

                case TerminateProcessStep tp:
                    vm.SelectedType = "TerminateProcess";
                    vm.StartProcessStep_ProcessName = tp.Settings.Target.ProcessName;
                    vm.StartProcessStep_WindowTitleContains = tp.Settings.Target.WindowTitleContains;
                    vm.StartProcessStep_ProcessSource.Load(tp.Settings.Target.ProcessSource);
                    vm.StartProcessStep_UsesProcessSource = tp.Settings.Target.ProcessSource.IsConfigured;
                    break;

                case FocusProcessStep fp:
                    vm.SelectedType = "FocusProcess";
                    vm.FocusProcessStep_Action = fp.Settings.Action;
                    vm.FocusProcessStep_ExecutablePath = fp.Settings.Target.ExecutablePath;
                    vm.FocusProcessStep_WindowTitleContains = fp.Settings.Target.WindowTitleContains;
                    vm.FocusProcessStep_WindowMode = fp.Settings.WindowMode == FocusProcessWindowMode.Fullscreen
                        ? FocusProcessWindowMode.Maximized
                        : fp.Settings.WindowMode;
                    vm.FocusProcessStep_ProcessSource.Load(fp.Settings.Target.ProcessSource);
                    vm.FocusProcessStep_UsesProcessSource = fp.Settings.Target.ProcessSource.IsConfigured;
                    break;

                case ShowTextStep st:
                    vm.SelectedType               = "ShowText";
                    vm.ShowTextStep_IsTaskResult  = st.Settings.TextSource == ShowTextSource.TaskResult;
                    vm.ShowTextStep_Text          = st.Settings.Text;
                    vm.ShowTextStep_TextResult.Load(st.Settings.TextResult);
                    vm.ShowTextStep_FontSize      = st.Settings.FontSize;
                    vm.ShowTextStep_FontColorWpf  = HexToWpfColor(st.Settings.FontColor);
                    vm.ShowTextStep_Opacity       = st.Settings.Opacity;
                    vm.ShowTextStep_DesktopIndex  = st.Settings.DesktopIndex;
                    vm.ShowTextStep_OffsetX       = st.Settings.OffsetX;
                    vm.ShowTextStep_OffsetY       = st.Settings.OffsetY;
                    vm.ShowTextStep_DurationMs    = st.Settings.DurationMs;
                    vm.ShowTextStep_ClearOnJobEnd = st.Settings.ClearOnJobEnd;
                    break;

                case ActiveWindowStep aw:
                    vm.SelectedType = "ActiveWindow";
                    vm.ActiveWindowStep_ProcessName = aw.Settings.Target.ProcessName;
                    vm.ActiveWindowStep_WindowTitleContains = aw.Settings.Target.WindowTitleContains;
                    vm.ActiveWindowStep_CacheMs = aw.Settings.CacheMs;
                    vm.ActiveWindowStep_ProcessSource.Load(aw.Settings.Target.ProcessSource);
                    vm.ActiveWindowStep_UsesProcessSource = aw.Settings.Target.ProcessSource.IsConfigured;
                    break;

                case KeyPointMatchingStep km:
                    vm.SelectedType = "KeyPointMatching";
                    vm.KeyPointMatchingStep_TemplatePath        = km.Settings.TemplatePath;
                    vm.KeyPointMatchingStep_MinMatchCount       = km.Settings.MinMatchCount;
                    vm.KeyPointMatchingStep_LowesRatioThreshold = km.Settings.LowesRatioThreshold;
                    vm.KeyPointMatchingStep_EnableROI           = km.Settings.EnableROI;
                    vm.KeyPointMatchingStep_RoiX = km.Settings.ROI.X;
                    vm.KeyPointMatchingStep_RoiY = km.Settings.ROI.Y;
                    vm.KeyPointMatchingStep_RoiW = km.Settings.ROI.Width;
                    vm.KeyPointMatchingStep_RoiH = km.Settings.ROI.Height;
                    vm.KeyPointMatchingStep_ImageSource.Load(km.Settings.ImageSource);
                    vm.DetectionDynamicRoiSource.Load(km.Settings.DynamicRoiSource);
                    vm.UseDynamicRoi = km.Settings.DynamicRoiSource.IsConfigured;
                    break;

                case DynamicRoiStep dr:
                    vm.SelectedType = "DynamicRoi";
                    vm.DynamicRoiStep_BoundsSource.Load(dr.Settings.BoundsSource);
                    vm.DynamicRoiStep_Padding = dr.Settings.Padding;
                    vm.DynamicRoiStep_MinimumConfidence = dr.Settings.MinimumConfidence;
                    vm.DynamicRoiStep_FullSearchInterval = dr.Settings.FullSearchInterval;
                    vm.DynamicRoiStep_ResetAfterMisses = dr.Settings.ResetAfterMisses;
                    break;

                case TaskAutomation.Jobs.IfStep ifs:
                    vm.SelectedType = "If";
                    vm.LoadIfStepConditions(ifs.Settings);
                    break;

                case TaskAutomation.Jobs.ElseIfStep eifs:
                    vm.SelectedType = "ElseIf";
                    vm.LoadElseIfStepConditions(eifs.Settings);
                    break;

                case TaskAutomation.Jobs.ElseStep:
                    vm.SelectedType = "Else";
                    break;

                case TaskAutomation.Jobs.EndIfStep:
                    vm.SelectedType = "EndIf";
                    break;

                case WindowsStateQueryStep windowsState:
                    vm.SelectedType = "WindowsStateQuery";
                    vm.LoadWindowsStateQuery(windowsState.Settings);
                    break;

                case TaskAutomation.Jobs.EndJobStep endJob:
                    vm.SelectedType = "EndJob";
                    vm.EndJobStep_SkipEndSteps = endJob.Settings.SkipEndSteps;
                    break;
                case TaskAutomation.Jobs.ContinueJobStep:
                    vm.SelectedType = "ContinueJob";
                    break;

                case TaskAutomation.Jobs.PointComparisonStep pcs:
                    vm.SelectedType = "PointComparison";
                    vm.PointComparisonStep_Mode             = pcs.Settings.Mode;
                    vm.PointComparisonStep_MatchRequirement = pcs.Settings.MatchRequirement;
                    vm.PointComparisonStep_RefSource        = pcs.Settings.OffsetSettings.ReferenceSource;
                    vm.PointComparisonStep_RefX             = pcs.Settings.OffsetSettings.ReferenceX;
                    vm.PointComparisonStep_RefY             = pcs.Settings.OffsetSettings.ReferenceY;
                    vm.PointComparisonStep_ReferencePointsSource.Load(pcs.Settings.OffsetSettings.ReferencePointsSource);
                    vm.PointComparisonStep_OffsetX          = pcs.Settings.OffsetSettings.OffsetX;
                    vm.PointComparisonStep_OffsetY          = pcs.Settings.OffsetSettings.OffsetY;
                    vm.PointComparisonStep_ExprCombineMode  = pcs.Settings.ExpressionSettings.CombineMode;
                    vm.PointComparisonStep_Expressions.Clear();
                    foreach (var expr in pcs.Settings.ExpressionSettings.Expressions)
                    {
                        var exprVm = new AxisExpressionViewModel(vm.PointComparisonStep_Expressions);
                        exprVm.LoadFrom(expr);
                        vm.PointComparisonStep_Expressions.Add(exprVm);
                    }
                    vm.PointComparisonStep_Points.Clear();
                    foreach (var pt in pcs.Settings.Points)
                    {
                        var ptVm = new PointEntryViewModel(vm.PointComparisonStep_Points, vm.AvailableDetectionSteps);
                        ptVm.LoadFrom(pt);
                        vm.PointComparisonStep_Points.Add(ptVm);
                    }
                    break;
            }
        }

        // ---------- Move / Delete ----------
        private bool CanMoveRelative(JobStep? step, int delta)
        {
            if (step == null) return false;
            var section = FindSection(step);
            if (section == null) return false;
            var idx = section.IndexOf(step);
            if (idx < 0) return false;
            var newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= section.Count) return false;
            return !WouldViolateIfStructure(section, idx, newIdx);
        }

        private async Task MoveRelativeAsync(JobStep? step, int delta)
        {
            if (!CanMoveRelative(step, delta) || step == null) return;

            var section = FindSection(step);
            if (section is null) return;
            var idx = section.IndexOf(step);
            var newIdx = idx + delta;

            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                var reordered = section.ToList();
                reordered.RemoveAt(idx);
                reordered.Insert(newIdx, step);
                section.ReplaceRange(reordered);
                _steps = section;
                JobValidation.RemoveInvalidSourceSelections(AllSteps());
                SelectedStep = step;
                HasUnsavedChanges = true;
            });
        }

        private static System.Windows.Media.Color HexToWpfColor(string? hex)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(hex))
                {
                    var h = hex.TrimStart('#');
                    if (h.Length == 6)
                    {
                        var r = Convert.ToByte(h.Substring(0, 2), 16);
                        var g = Convert.ToByte(h.Substring(2, 2), 16);
                        var b = Convert.ToByte(h.Substring(4, 2), 16);
                        return System.Windows.Media.Color.FromRgb(r, g, b);
                    }
                }
            }
            catch { }
            return System.Windows.Media.Colors.White;
        }

        private async Task MoveStepAsync(StepDragDrop.MoveRequest? request)
        {
            if (request is null) return;
            if (request.Source is not ObservableRangeCollection<JobStep> source
                || request.Target is not ObservableRangeCollection<JobStep> target
                || !IsKnownSection(source)
                || !IsKnownSection(target)
                || request.SourceIndex < 0
                || request.SourceIndex >= source.Count)
                return;

            int first = request.SourceIndex;
            int last = first;
            var dragged = source[first];

            if (dragged is IfStep or ElseIfStep or ElseStep or EndIfStep)
            {
                first = FindOwningIfIndex(source, request.SourceIndex);
                if (first < 0) return;
                last = FindMatchingEndIfIndex(source, first);
                if (last < first) return;
            }

            var moving = source.Skip(first).Take(last - first + 1).ToList();
            int insertIndex = Math.Clamp(request.TargetIndex, 0, target.Count);

            // Ein Drop innerhalb des gerade gezogenen Blocks verändert nichts.
            if (ReferenceEquals(source, target)
                && insertIndex >= first
                && insertIndex <= last + 1)
                return;

            var sourceSimulation = source.ToList();
            sourceSimulation.RemoveRange(first, moving.Count);

            var targetSimulation = ReferenceEquals(source, target)
                ? sourceSimulation
                : target.ToList();

            if (ReferenceEquals(source, target) && insertIndex > first)
                insertIndex -= moving.Count;
            insertIndex = Math.Clamp(insertIndex, 0, targetSimulation.Count);
            targetSimulation.InsertRange(insertIndex, moving);

            if (!JobValidation.IsIfStructureAllowed(sourceSimulation)
                || !JobValidation.IsIfStructureAllowed(targetSimulation))
                return;

            if (ReferenceEquals(source, target)
                && first == insertIndex)
                return;

            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                if (ReferenceEquals(source, target))
                {
                    source.ReplaceRange(targetSimulation);
                }
                else
                {
                    BeginCollectionUpdate();
                    try
                    {
                        source.ReplaceRange(sourceSimulation);
                        target.ReplaceRange(targetSimulation);
                    }
                    finally
                    {
                        EndCollectionUpdate();
                    }
                }
                JobValidation.RemoveInvalidSourceSelections(AllSteps());
                SelectedStep = moving[0];
                _steps = target;
                SelectedSteps.Clear();
                SelectedSteps.AddRange(moving);
                HasUnsavedChanges = true;
                ExpandSection(target);
                InvalidateSelectionCommands();
            });
        }

        private bool CanMoveStepToSection(JobStep? step, ObservableRangeCollection<JobStep> target)
            => step != null
               && FindSection(step) is { } source
               && !ReferenceEquals(source, target);

        private Task MoveStepToSectionAsync(JobStep? step, ObservableRangeCollection<JobStep> target)
        {
            if (step == null || FindSection(step) is not { } source || ReferenceEquals(source, target))
                return Task.CompletedTask;

            return MoveStepAsync(new StepDragDrop.MoveRequest(
                source,
                source.IndexOf(step),
                target,
                target.Count));
        }

        private bool IsKnownSection(ObservableRangeCollection<JobStep> section)
            => ReferenceEquals(section, _startSteps)
               || ReferenceEquals(section, _runSteps)
               || ReferenceEquals(section, _endSteps);

        private ObservableRangeCollection<JobStep>? FindSection(JobStep step)
        {
            if (_startSteps.Contains(step)) return _startSteps;
            if (_runSteps.Contains(step)) return _runSteps;
            if (_endSteps.Contains(step)) return _endSteps;
            return null;
        }

        private List<JobStep> AllSteps()
            => _startSteps.Concat(_runSteps).Concat(_endSteps).ToList();

        private List<JobStep> GetPrecedingSteps(ObservableRangeCollection<JobStep> section, int index)
        {
            IEnumerable<JobStep> precedingPhases = ReferenceEquals(section, _runSteps)
                ? _startSteps
                : ReferenceEquals(section, _endSteps)
                    ? _startSteps.Concat(_runSteps)
                    : [];
            return precedingPhases.Concat(section.Take(Math.Clamp(index, 0, section.Count))).ToList();
        }

        private void ExpandSection(ObservableRangeCollection<JobStep> section)
        {
            if (ReferenceEquals(section, _startSteps)) IsStartSectionExpanded = true;
            else if (ReferenceEquals(section, _runSteps)) IsRunSectionExpanded = true;
            else if (ReferenceEquals(section, _endSteps)) IsEndSectionExpanded = true;
        }

        private static int FindOwningIfIndex(IReadOnlyList<JobStep> steps, int index)
        {
            if (index >= 0 && index < steps.Count && steps[index] is IfStep) return index;
            int depth = 0;
            for (int i = index - 1; i >= 0; i--)
            {
                if (steps[i] is EndIfStep) depth++;
                else if (steps[i] is IfStep)
                {
                    if (depth == 0) return i;
                    depth--;
                }
            }
            return -1;
        }

        private static int FindMatchingEndIfIndex(IReadOnlyList<JobStep> steps, int ifIndex)
        {
            int depth = 0;
            for (int i = ifIndex + 1; i < steps.Count; i++)
            {
                if (steps[i] is IfStep) depth++;
                else if (steps[i] is EndIfStep)
                {
                    if (depth == 0) return i;
                    depth--;
                }
            }
            return -1;
        }

        private void ScheduleValidation()
        {
            _validationCts?.Cancel();
            var cts = _validationCts = new CancellationTokenSource();
            var generation = ++_validationGeneration;
            var startSnapshot = _startSteps.ToArray();
            var runSnapshot = _runSteps.ToArray();
            var endSnapshot = _endSteps.ToArray();
            _ = ValidateAsync();

            async Task ValidateAsync()
            {
                try
                {
                    await Task.Delay(120, cts.Token);
                    var serialized = await JobStepsSnapshotService.SerializeAsync(
                        startSnapshot, runSnapshot, endSnapshot, cts.Token);
                    var materialized = await JobStepsSnapshotService.DeserializeAsync(serialized, cts.Token);
                    var result = await Task.Run(() => JobValidation.ValidateJob(new Job
                    {
                        StartSteps = materialized.StartSteps.ToList(),
                        Steps = materialized.RunSteps.ToList(),
                        EndSteps = materialized.EndSteps.ToList()
                    }), cts.Token);
                    if (cts.IsCancellationRequested || generation != _validationGeneration) return;
                    await Application.Current.Dispatcher.InvokeAsync(() => ApplyValidation(result, generation));
                }
                catch (OperationCanceledException) { }
            }
        }

        private void ApplyValidation(JobValidationResult validation, int generation)
        {
            if (generation != _validationGeneration) return;
            var liveSteps = AllSteps()
                .GroupBy(step => step.Id)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (var result in validation.Steps)
            {
                if (liveSteps.TryGetValue(result.Step.Id, out var liveStep))
                    liveStep.SetValidationResult(result.IsValid, result.Error);
            }
            NotifySectionStateChanged();
        }

        private async Task DeleteStepAsync(JobStep? step)
        {
            var target = step ?? SelectedStep;
            if (target == null) return;
            if (FindSection(target) is { } section) _steps = section;

            bool isIfOrEndIf = target is TaskAutomation.Jobs.IfStep or TaskAutomation.Jobs.EndIfStep;
            string message = isIfOrEndIf ? Loc.Get("Step.Delete.IfBlock") : Loc.Get("Step.Delete.One");

            if (!await _dialogService.ConfirmAsync(message, Loc.Get("Dialog.Delete.Title"))) return;

            var idx = _steps.IndexOf(target);
            if (idx < 0) return;

            var indicesToRemove = new SortedSet<int>();
            if (isIfOrEndIf)
            {
                int ifIdx    = target is TaskAutomation.Jobs.IfStep ? idx : FindOwningIfStep(idx);
                int endIfIdx = target is TaskAutomation.Jobs.EndIfStep ? idx : FindMatchingEndIf(idx);

                if (ifIdx >= 0 && endIfIdx > ifIdx)
                {
                    // Collect indices of If/ElseIf/Else/EndIf steps only — preserve regular steps inside.
                    for (int i = ifIdx; i <= endIfIdx; i++)
                    {
                        if (_steps[i] is TaskAutomation.Jobs.IfStep
                            or TaskAutomation.Jobs.ElseIfStep
                            or TaskAutomation.Jobs.ElseStep
                            or TaskAutomation.Jobs.EndIfStep)
                        {
                            indicesToRemove.Add(i);
                        }
                    }
                }
                else
                {
                    indicesToRemove.Add(idx);
                }
            }
            else
            {
                indicesToRemove.Add(idx);
            }

            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                var remaining = _steps.Where((_, index) => !indicesToRemove.Contains(index)).ToList();
                _steps.ReplaceRange(remaining);
                SelectedStep = remaining.ElementAtOrDefault(Math.Max(0, idx - 1));
                HasUnsavedChanges = true;
            });
        }

        private void OnRunningJobsChanged()
        {
            // Snapshot on ThreadPool thread – only marshal the bool result to the UI thread.
            var isRunning = _dispatcher.RunningJobIds.Contains(Job.Id);
            var canRequestStop = _dispatcher.RunningJobInstances.Any(instance =>
                instance.JobId == Job.Id && instance.State.CanRequestStop());
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                IsJobRunning = isRunning;
                CanRequestJobStop = canRequestStop;
            });
        }

        // ---------- Undo / Redo ----------
        private async Task PushUndoAsync()
        {
            _undoStack.Push(await CreateSnapshotAsync());
            _redoStack.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            InvalidateHistoryCommands();
        }

        private async Task UndoAsync()
        {
            if (_undoStack.Count == 0) return;
            await RunMutationAsync(async () =>
            {
                _redoStack.Push(await CreateSnapshotAsync());
                RestoreSnapshot(_undoStack.Pop());
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
                InvalidateHistoryCommands();
            });
        }

        private async Task RedoAsync()
        {
            if (_redoStack.Count == 0) return;
            await RunMutationAsync(async () =>
            {
                _undoStack.Push(await CreateSnapshotAsync());
                RestoreSnapshot(_redoStack.Pop());
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
                InvalidateHistoryCommands();
            });
        }

        private async Task<JobStepsSnapshot> CreateSnapshotAsync()
        {
            var serialized = await JobStepsSnapshotService.SerializeAsync(
                _startSteps.ToArray(), _runSteps.ToArray(), _endSteps.ToArray());
            var materialized = await JobStepsSnapshotService.DeserializeAsync(serialized);
            return new JobStepsSnapshot(
                materialized.StartSteps.ToList(),
                materialized.RunSteps.ToList(),
                materialized.EndSteps.ToList());
        }

        private void RestoreSnapshot(JobStepsSnapshot snapshot)
        {
            BeginCollectionUpdate();
            try
            {
                _startSteps.ReplaceRange(snapshot.StartSteps);
                _runSteps.ReplaceRange(snapshot.RunSteps);
                _steps = _runSteps;
                _endSteps.ReplaceRange(snapshot.EndSteps);
            }
            finally
            {
                EndCollectionUpdate();
            }
            SelectedStep = null;
            SelectedSteps.Clear();
            HasUnsavedChanges = true;
        }

        // ---------- Copy / Paste ----------
        private async Task CopySelectedAsync()
        {
            var sources = SelectedSteps.Count > 0
                ? SelectedSteps.OrderBy(s => _steps.IndexOf(s)).ToList()
                : (SelectedStep != null ? new List<JobStep> { SelectedStep } : null);
            if (sources == null) return;
            await RunMutationAsync(async () =>
            {
                _clipboard = (await JobStepsSnapshotService.CloneAsync(sources, newIds: false)).ToList();
                InvalidateClipboardCommands();
            });
        }

        private async Task PasteAsync()
        {
            if (_clipboard.Count == 0) return;

            await RunMutationAsync(async () =>
            {
                int insertAt = SelectedStep != null
                    ? Math.Min(_steps.Count, _steps.IndexOf(SelectedStep) + 1)
                    : _steps.Count;

                var toInsert = (await JobStepsSnapshotService.CloneAsync(_clipboard, newIds: true)).ToList();
                await PushUndoAsync();
                _steps.InsertRange(insertAt, toInsert);
                SelectedStep = toInsert[^1];
                HasUnsavedChanges = true;
            });
        }

        private async Task RunMutationAsync(Func<Task> action)
        {
            await _mutationGate.WaitAsync();
            IsMutationBusy = true;
            try
            {
                await action();
            }
            finally
            {
                IsMutationBusy = false;
                _mutationGate.Release();
            }
        }

        private async Task<AddJobStepDialogViewModel.PreparedSources> PrepareDialogSourcesAsync(
            IReadOnlyList<JobStep> precedingSteps)
        {
            await _mutationGate.WaitAsync();
            IsMutationBusy = true;
            try
            {
                return await AddJobStepDialogViewModel.PrepareSourcesAsync(precedingSteps);
            }
            finally
            {
                IsMutationBusy = false;
                _mutationGate.Release();
            }
        }

        // ---------- Delete selected ----------
        private async Task DeleteSelectedAsync()
        {
            var targets = SelectedSteps.Count > 0
                ? SelectedSteps.ToList()
                : (SelectedStep != null ? new List<JobStep> { SelectedStep } : null);
            if (targets == null || targets.Count == 0) return;

            string message = targets.Count == 1
                ? (targets[0] is TaskAutomation.Jobs.IfStep or TaskAutomation.Jobs.EndIfStep
                    ? Loc.Get("Step.Delete.IfBlock")
                    : Loc.Get("Step.Delete.One"))
                : Loc.Format("Step.Delete.Many", targets.Count);

            if (!await _dialogService.ConfirmAsync(message, Loc.Get("Dialog.Delete.Title")))
                return;

            // Collect all indices to remove (handle If/EndIf structure steps).
            var indicesToRemove = new SortedSet<int>(Comparer<int>.Create((a, b) => b.CompareTo(a))); // descending
            foreach (var target in targets)
            {
                int idx = _steps.IndexOf(target);
                if (idx < 0) continue;

                bool isStructure = target is TaskAutomation.Jobs.IfStep or TaskAutomation.Jobs.EndIfStep;
                if (isStructure)
                {
                    int ifIdx    = target is TaskAutomation.Jobs.IfStep ? idx : FindOwningIfStep(idx);
                    int endIfIdx = target is TaskAutomation.Jobs.EndIfStep ? idx : FindMatchingEndIf(idx);
                    if (ifIdx >= 0 && endIfIdx > ifIdx)
                    {
                        for (int i = ifIdx; i <= endIfIdx; i++)
                            if (_steps[i] is TaskAutomation.Jobs.IfStep or TaskAutomation.Jobs.ElseIfStep
                                            or TaskAutomation.Jobs.ElseStep or TaskAutomation.Jobs.EndIfStep)
                                indicesToRemove.Add(i);
                    }
                    else { indicesToRemove.Add(idx); }
                }
                else { indicesToRemove.Add(idx); }
            }

            if (indicesToRemove.Count == 0) return;
            int firstRemoved = indicesToRemove.DefaultIfEmpty(0).Min();
            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                var remaining = _steps.Where((_, index) => !indicesToRemove.Contains(index)).ToList();
                _steps.ReplaceRange(remaining);
                SelectedStep = remaining.ElementAtOrDefault(Math.Max(0, firstRemoved - 1));
                SelectedSteps.Clear();
                HasUnsavedChanges = true;
            });
        }

        // ---------- Deep clone helpers ----------
        private static List<JobStep> DeepCloneSteps(IEnumerable<JobStep> steps, bool newIds = false)
            => steps.Select(s => DeepCloneStep(s, newIds)).ToList();

        private static JobStep DeepCloneStep(JobStep s, bool newId = false)
        {
            var json  = JsonSerializer.Serialize(s, s.GetType());
            var clone = (JobStep)JsonSerializer.Deserialize(json, s.GetType())!;
            if (newId) clone.Id = Guid.NewGuid().ToString();
            return clone;
        }

        // ---------- If/ElseIf/Else helpers ----------

        /// <summary>
        /// Scans backwards from <paramref name="fromIndex"/> to find the IfStep that owns the
        /// ElseIf / Else / EndIf at that index (same nesting depth).
        /// Returns -1 if not found.
        /// </summary>
        private int FindOwningIfStep(int fromIndex)
        {
            int depth = 0;
            for (int i = fromIndex - 1; i >= 0; i--)
            {
                if (_steps[i] is TaskAutomation.Jobs.EndIfStep) depth++;
                else if (_steps[i] is TaskAutomation.Jobs.IfStep)
                {
                    if (depth == 0) return i;
                    depth--;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the index just before the first same-level ElseStep in the block,
        /// or <paramref name="endIfIdx"/> if no Else exists. Used to insert ElseIf at
        /// the correct position (always before Else, never after it).
        /// </summary>
        private int FindInsertBeforeElseOrEndIf(int ifIdx, int endIfIdx)
        {
            int depth = 0;
            for (int i = ifIdx + 1; i < endIfIdx; i++)
            {
                if (_steps[i] is TaskAutomation.Jobs.IfStep) depth++;
                else if (_steps[i] is TaskAutomation.Jobs.EndIfStep) depth--;
                else if (_steps[i] is TaskAutomation.Jobs.ElseStep && depth == 0) return i;
            }
            return endIfIdx;
        }

        /// <summary>
        /// Returns true if moving the step at <paramref name="from"/> to <paramref name="to"/>
        /// would produce an invalid If / ElseIf / Else / EndIf ordering.
        /// Valid order within every block: If → ElseIf* → Else? → EndIf
        /// Works by simulating the move on a copy and validating the result.
        /// </summary>
        private static bool WouldViolateIfStructure(IReadOnlyList<JobStep> steps, int from, int to)
        {
            if (from == to) return false;
            var step = steps[from];

            // Regular steps cannot break the control-flow structure.
            if (step is not (TaskAutomation.Jobs.IfStep     or
                             TaskAutomation.Jobs.ElseIfStep or
                             TaskAutomation.Jobs.ElseStep   or
                             TaskAutomation.Jobs.EndIfStep))
                return false;

            var sim = new System.Collections.Generic.List<JobStep>(steps);
            sim.RemoveAt(from);
            sim.Insert(to, step);
            return !JobValidation.IsIfStructureAllowed(sim);
        }

        /// <summary>
        /// Validates that every If-block in <paramref name="steps"/> obeys
        /// If → ElseIf* → Else? → EndIf ordering (no ElseIf after Else, no orphaned markers).
        /// </summary>
#if false // Fachregel liegt in TaskAutomation.JobValidation.IsIfStructureAllowed.
        private static bool IsValidIfStructure(System.Collections.Generic.IReadOnlyList<JobStep> steps)
        {
            // Each stack entry: true = an Else has already been seen in this block.
            var seenElse = new System.Collections.Generic.Stack<bool>();
            foreach (var s in steps)
            {
                if (s is TaskAutomation.Jobs.IfStep)
                {
                    if (seenElse.Count > 0) return false; // no nesting allowed
                    seenElse.Push(false);
                }
                else if (s is TaskAutomation.Jobs.ElseIfStep)
                {
                    if (seenElse.Count == 0) return false; // no owning If
                    if (seenElse.Peek()) return false;     // ElseIf after Else
                }
                else if (s is TaskAutomation.Jobs.ElseStep)
                {
                    if (seenElse.Count == 0) return false; // no owning If
                    if (seenElse.Peek()) return false;     // duplicate Else
                    seenElse.Pop();
                    seenElse.Push(true);
                }
                else if (s is TaskAutomation.Jobs.EndIfStep)
                {
                    if (seenElse.Count == 0) return false; // no owning If
                    seenElse.Pop();
                }
            }
            return seenElse.Count == 0; // every If must be closed
        }
#endif

        /// <summary>
        /// Returns the number of currently open (unclosed) If-blocks at the given insert index.
        /// Used to prevent nesting: returns > 0 when the position is inside an existing block.
        /// </summary>
        private int CountOpenBlocksAt(int insertIndex)
        {
            int depth = 0;
            for (int i = 0; i < insertIndex && i < _steps.Count; i++)
            {
                if (_steps[i] is TaskAutomation.Jobs.IfStep)    depth++;
                else if (_steps[i] is TaskAutomation.Jobs.EndIfStep && depth > 0) depth--;
            }
            return depth;
        }

        /// <summary>
        /// Findet den passenden EndIfStep zum IfStep/ElseIfStep bei fromIndex.
        /// Scan vorwärts: jeder IfStep erhöht die Tiefe, EndIfStep bei Tiefe 0 ist der Treffer.
        /// </summary>
        private int FindMatchingEndIf(int fromIndex)
        {
            int depth = 0;
            for (int i = fromIndex + 1; i < _steps.Count; i++)
            {
                if (_steps[i] is TaskAutomation.Jobs.IfStep) depth++;
                else if (_steps[i] is TaskAutomation.Jobs.EndIfStep)
                {
                    if (depth == 0) return i;
                    depth--;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gibt true zurück, wenn zwischen fromIndex und endIfIndex bereits ein ElseStep auf
        /// der gleichen Verschachtelungsebene vorhanden ist.
        /// </summary>
        private bool HasElseInBlock(int fromIndex, int endIfIndex)
        {
            int depth = 0;
            for (int i = fromIndex + 1; i < endIfIndex; i++)
            {
                if (_steps[i] is TaskAutomation.Jobs.IfStep) depth++;
                else if (_steps[i] is TaskAutomation.Jobs.EndIfStep) depth--;
                else if (_steps[i] is TaskAutomation.Jobs.ElseStep && depth == 0) return true;
            }
            return false;
        }

        private async Task AddElseIfAsync(JobStep? step)
        {
            if (step == null) return;
            int idx = _steps.IndexOf(step);
            if (idx < 0) return;

            // Normalize: always work relative to the owning IfStep
            int ifIdx = step is TaskAutomation.Jobs.IfStep ? idx : FindOwningIfStep(idx);
            if (ifIdx < 0) return;

            int endIfIdx = FindMatchingEndIf(ifIdx);
            if (endIfIdx < 0) return;

            // Insert before Else (if one exists) to keep If→ElseIf*→Else?→EndIf order
            int insertIdx = FindInsertBeforeElseOrEndIf(ifIdx, endIfIdx);

            var precedingSteps = GetPrecedingSteps(_steps, insertIdx);
            var allSteps = AllSteps();
            var preparedSources = await PrepareDialogSourcesAsync(precedingSteps);
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id, allSteps, preparedSources, _cameraCaptureService)
                { Mode = StepDialogMode.Add, IsTypeLocked = true };
            vm.SelectedType = "ElseIf";

            ShowDialogWithVm(vm, out bool? result);

            if (result == true && vm.CreatedStep != null)
            {
                await RunMutationAsync(async () =>
                {
                    await PushUndoAsync();
                    _steps.InsertRange(insertIdx, [vm.CreatedStep]);
                    SelectedStep = vm.CreatedStep;
                    HasUnsavedChanges = true;
                });
            }
        }

        private async Task AddElseAsync(JobStep? step)
        {
            if (step == null) return;
            int idx = _steps.IndexOf(step);
            if (idx < 0) return;

            // Normalize to IfStep so HasElseInBlock scans the full block
            int ifIdx = step is TaskAutomation.Jobs.IfStep ? idx : FindOwningIfStep(idx);
            if (ifIdx < 0) return;

            int endIfIdx = FindMatchingEndIf(ifIdx);
            if (endIfIdx < 0 || HasElseInBlock(ifIdx, endIfIdx)) return;

            // Guard already passed above: HasElseInBlock returned false
            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                var elseStep = new TaskAutomation.Jobs.ElseStep();
                _steps.InsertRange(endIfIdx, [elseStep]);
                SelectedStep = elseStep;
                HasUnsavedChanges = true;
            });
        }

        private bool CanAddElse(JobStep? step)
        {
            if (step == null) return false;
            int idx = _steps.IndexOf(step);
            if (idx < 0) return false;

            int ifIdx = step is TaskAutomation.Jobs.IfStep ? idx : FindOwningIfStep(idx);
            if (ifIdx < 0) return false;

            int endIfIdx = FindMatchingEndIf(ifIdx);
            if (endIfIdx < 0) return false;
            return !HasElseInBlock(ifIdx, endIfIdx);
        }

        private bool CanAddElseIf(JobStep? step)
        {
            if (step == null) return false;
            int idx = _steps.IndexOf(step);
            if (idx < 0) return false;
            int ifIdx = step is TaskAutomation.Jobs.IfStep ? idx : FindOwningIfStep(idx);
            if (ifIdx < 0) return false;
            int endIfIdx = FindMatchingEndIf(ifIdx);
            if (endIfIdx < 0) return false;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dispatcher.RunningJobsChanged -= OnRunningJobsChanged;
                if (_debugSession != null)
                {
                    _debugSession.Changed -= OnDebugSessionChanged;
                    _debugSession.IterationChanged -= OnDebugIterationChanged;
                }
                foreach (var step in _startSteps.Concat(_runSteps).Concat(_endSteps))
                    step.PropertyChanged -= OnStepPropertyChanged;
                _validationCts?.Cancel();
                _validationCts?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ---------- Command invalidation helper ----------
        private void InvalidateSelectionCommands()
        {
            (EditStepCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (DeleteStepCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (DeleteSelectedCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (CopyCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        private void InvalidateHistoryCommands()
        {
            (UndoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (RedoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        private void InvalidateClipboardCommands()
            => (PasteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

        private void InvalidateSaveCommands()
        {
            (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DebugJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void InvalidateDebugCommands()
        {
            (DebugStepCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DebugContinueCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelDebugCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CloseDebuggerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleDebugPanelCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void InvalidateStructureCommands()
        {
            (MoveStepUpCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveStepDownCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (AddElseIfCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (AddElseCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToStartSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToRunSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToEndSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            InvalidateSelectionCommands();
        }

        private void InvalidateMutationCommands()
        {
            (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (AddStepCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (EditStepCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveStepUpCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveStepDownCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (ReorderStepCommand as AsyncRelayCommand<StepDragDrop.MoveRequest>)?.RaiseCanExecuteChanged();
            (DeleteStepCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (DeleteSelectedCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (UndoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (RedoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (CopyCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (PasteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (AddElseIfCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (AddElseCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToStartSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToRunSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToEndSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
        }

        private void InvalidateAllCommands()
        {
            InvalidateSaveCommands();
            InvalidateMutationCommands();
            (RenameCommand        as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (StopJobCommand       as RelayCommand)?.RaiseCanExecuteChanged();
            InvalidateDebugCommands();
            (ExpandDebugContextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CollapseDebugContextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleBreakpointCommand as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (ToggleStepEnabledCommand as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
        }

    }
}
