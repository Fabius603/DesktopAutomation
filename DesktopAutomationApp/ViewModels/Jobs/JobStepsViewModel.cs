using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;
using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
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
using TaskAutomation.Security;

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
        private readonly IStepDefinitionCatalog _stepDefinitionCatalog;
        private readonly ISecretStore? _secretStore;
        private IReadOnlyList<ValueProviderSourceDescriptor> _providerSources = [];

        private sealed record JobStepsSnapshot(
            List<JobStep> StartSteps,
            List<JobStep> RunSteps,
            List<JobStep> EndSteps);

        private sealed record JobEditState(
            IReadOnlyList<JobStep> StartSteps,
            IReadOnlyList<JobStep> RunSteps,
            IReadOnlyList<JobStep> EndSteps,
            IReadOnlyList<JobVariable> Variables,
            int EndPhaseTimeoutSeconds,
            bool Repeating);

        private readonly Stack<JobStepsSnapshot> _undoStack = new();
        private readonly Stack<JobStepsSnapshot> _redoStack = new();
        private List<JobStep> _clipboard  = new();
        private List<JobStep> _savedSnapshot;
        private List<JobStep> _savedStartSnapshot;
        private List<JobStep> _savedEndSnapshot;
        private List<JobVariable> _savedVariables;
        private int _savedEndPhaseTimeoutSeconds;
        private bool _savedRepeating;
        private readonly EditorChangeTracker<JobEditState> _changeTracker;
        private bool _suppressDirtyTracking;
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
        public ObservableCollection<JobVariableEditorViewModel> JobVariables { get; } = [];
        public IReadOnlyList<JobVariableEditorViewModel> FilteredJobVariables =>
            JobVariables.Where(MatchesVariableFilter).ToArray();
        public IReadOnlyList<JobVariable> Variables => Job.Variables;
        public IReadOnlyList<ValueProviderSourceDescriptor> ProviderSources => _providerSources;
        public IReadOnlyList<JobStep> AllJobSteps => _allJobStepsSnapshot;
        public bool HasJobVariables => JobVariables.Count > 0;
        public bool HasFilteredJobVariables => FilteredJobVariables.Count > 0;
        private string _variableSearchText = string.Empty;
        public string VariableSearchText
        {
            get => _variableSearchText;
            set { value ??= string.Empty; if (_variableSearchText == value) return; SetProperty(ref _variableSearchText, value); RefreshVariableFilter(); }
        }
        private bool _showSharedVariables = true;
        public bool ShowSharedVariables
        {
            get => _showSharedVariables;
            set { if (_showSharedVariables == value) return; SetProperty(ref _showSharedVariables, value); RefreshVariableFilter(); }
        }
        private bool _showStepValues = true;
        public bool ShowStepValues
        {
            get => _showStepValues;
            set { if (_showStepValues == value) return; SetProperty(ref _showStepValues, value); RefreshVariableFilter(); }
        }
        private bool _showUsedVariables = true;
        public bool ShowUsedVariables
        {
            get => _showUsedVariables;
            set { if (_showUsedVariables == value) return; SetProperty(ref _showUsedVariables, value); RefreshVariableFilter(); }
        }
        private bool _showUnusedVariables = true;
        public bool ShowUnusedVariables
        {
            get => _showUnusedVariables;
            set { if (_showUnusedVariables == value) return; SetProperty(ref _showUnusedVariables, value); RefreshVariableFilter(); }
        }

        private int _endPhaseTimeoutSeconds;
        private bool _isRepeating;

        public bool IsRepeating
        {
            get => _isRepeating;
            set
            {
                if (_isRepeating == value) return;
                _isRepeating = value;
                OnPropertyChanged();
                ScheduleDirtyCheck();
            }
        }

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
                ScheduleDirtyCheck();
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
        public bool HasMultipleSelectedSteps => SelectedStepCount > 1;
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
        public ICommand DuplicateStepCommand { get; }
        public ICommand StartJobCommand { get; }
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
        public ICommand OpenVariablesCommand { get; }
        public ICommand AddVariableCommand { get; }
        public ICommand DeleteVariableCommand { get; }
        public ICommand DuplicateVariableCommand { get; }
        public ICommand PromoteVariableCommand { get; }

        public event Action? RequestBack;

        public JobStepsViewModel(
            Job job,
            IJobExecutor jobExecutionContext,
            IJobApplicationService jobAppService,
            IDialogService dialogService,
            IJobDispatcher dispatcher,
            ICameraCaptureService cameraCaptureService,
            IStepDefinitionCatalog? stepDefinitionCatalog = null,
            ISecretStore? secretStore = null)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            Job.Variables ??= [];
            _jobExecutionContext = jobExecutionContext;
            _jobAppService = jobAppService;
            _dialogService = dialogService;
            _dispatcher = dispatcher;
            _cameraCaptureService = cameraCaptureService;
            _stepDefinitionCatalog = stepDefinitionCatalog ?? BuiltInStepDefinitions.Instance;
            _secretStore = secretStore;
            JobVariableInputMigration.Migrate(Job, _stepDefinitionCatalog);

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
            _savedVariables = DeepCloneVariables(Job.Variables);
            ResetVariableEditors(Job.Variables);
            _endPhaseTimeoutSeconds = Math.Clamp(
                Job.EndPhaseTimeoutSeconds,
                Job.MinEndPhaseTimeoutSeconds,
                Job.MaxEndPhaseTimeoutSeconds);
            _savedEndPhaseTimeoutSeconds = _endPhaseTimeoutSeconds;
            _isRepeating = Job.Repeating;
            _savedRepeating = _isRepeating;
            _changeTracker = new EditorChangeTracker<JobEditState>(
                CaptureSavedEditState(),
                JobStatesMatchAsync,
                isDirty => HasUnsavedChanges = isDirty,
                TimeSpan.FromMilliseconds(60));

            _runSteps.CollectionChanged += OnSectionCollectionChanged;
            _startSteps.CollectionChanged += OnSectionCollectionChanged;
            _endSteps.CollectionChanged += OnSectionCollectionChanged;

            ReconcileStepSubscriptions();

            BackCommand   = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand   = new AsyncRelayCommand(Save, () => HasUnsavedChanges && !IsDebugActive && !IsMutationBusy);
            CancelCommand = new AsyncRelayCommand(ConfirmDiscardChangesAsync, () => HasUnsavedChanges && !IsDebugActive);
            RenameCommand = new AsyncRelayCommand(Rename, () => !IsDebugActive);
            OpenFileCommand = new RelayCommand(OpenFileInExplorer);

            AddStepCommand    = new AsyncRelayCommand(AddStep, () => !IsDebugActive && !IsMutationBusy);
            EditStepCommand   = new AsyncRelayCommand<JobStep?>(
                s => EditStep(GetSingleSelection(s)),
                s => { var t = GetSingleSelection(s); return !IsDebugActive && !IsMutationBusy && t != null && t is not TaskAutomation.Jobs.ElseStep and not TaskAutomation.Jobs.EndIfStep; });
            MoveStepUpCommand = new AsyncRelayCommand<JobStep?>(s => MoveSelectionRelativeAsync(s, -1), s => !IsDebugActive && !IsMutationBusy && CanMoveSelectionRelative(s, -1));
            MoveStepDownCommand = new AsyncRelayCommand<JobStep?>(s => MoveSelectionRelativeAsync(s, +1), s => !IsDebugActive && !IsMutationBusy && CanMoveSelectionRelative(s, +1));
            ReorderStepCommand = new AsyncRelayCommand<StepDragDrop.MoveRequest>(MoveStepAsync, _ => !IsDebugActive && !IsMutationBusy);
            DeleteStepCommand = new AsyncRelayCommand<JobStep?>(DeleteStepAsync, s => !IsDebugActive && !IsMutationBusy && (s ?? SelectedStep) != null);
            DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => !IsDebugActive && !IsMutationBusy && (SelectedSteps.Count > 0 || SelectedStep != null));
            UndoCommand           = new AsyncRelayCommand(UndoAsync, () => !IsDebugActive && !IsMutationBusy && CanUndo);
            RedoCommand           = new AsyncRelayCommand(RedoAsync, () => !IsDebugActive && !IsMutationBusy && CanRedo);
            CopyCommand           = new AsyncRelayCommand(CopySelectedAsync, () => !IsMutationBusy && (SelectedSteps.Count > 0 || SelectedStep != null));
            PasteCommand          = new AsyncRelayCommand(PasteAsync, () => !IsDebugActive && !IsMutationBusy && _clipboard.Count > 0);
            DuplicateStepCommand  = new AsyncRelayCommand(DuplicateSelectedAsync, () => !IsDebugActive && !IsMutationBusy && (SelectedSteps.Count > 0 || SelectedStep != null));

            StartJobCommand = new RelayCommand(() =>
            {
                try { _dispatcher.StartJob(Job.Id); }
                catch (JobLimitExceededException) { }
            }, () => !IsJobRunning && !HasUnsavedChanges && !IsDebugActive && AllSteps().Any(step => step.IsEnabled));
            StopJobCommand = new RelayCommand(() =>
            {
                if (IsDebugActive && _debugSession != null)
                    _dispatcher.CancelDebugJob(_debugSession.InstanceId);
                else
                    _dispatcher.CancelJobsByDefinition(Job.Id);
            }, () => CanRequestJobStop || IsDebugActive);
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
            ToggleBreakpointCommand = new AsyncRelayCommand<JobStep?>(
                ToggleBreakpointsAsync,
                step => !IsMutationBusy && GetOrderedSelection(step).Count > 0);
            ToggleStepEnabledCommand = new AsyncRelayCommand<JobStep?>(
                ToggleSelectedStepsEnabledAsync,
                step => !IsDebugActive && !IsMutationBusy && GetOrderedSelection(step).Any(selected => selected.CanBeDisabled));

            AddElseIfCommand = new AsyncRelayCommand<JobStep?>(step => AddElseIfAsync(GetSingleSelection(step)), step => !IsDebugActive && !IsMutationBusy && CanAddElseIf(GetSingleSelection(step)));
            AddElseCommand   = new AsyncRelayCommand<JobStep?>(step => AddElseAsync(GetSingleSelection(step)), step => !IsDebugActive && !IsMutationBusy && CanAddElse(GetSingleSelection(step)));
            MoveToStartSectionCommand = new AsyncRelayCommand<JobStep?>(
                step => MoveSelectionToSectionAsync(step, _startSteps),
                step => !IsDebugActive && !IsMutationBusy && CanMoveSelectionToSection(step, _startSteps));
            MoveToRunSectionCommand = new AsyncRelayCommand<JobStep?>(
                step => MoveSelectionToSectionAsync(step, _runSteps),
                step => !IsDebugActive && !IsMutationBusy && CanMoveSelectionToSection(step, _runSteps));
            MoveToEndSectionCommand = new AsyncRelayCommand<JobStep?>(
                step => MoveSelectionToSectionAsync(step, _endSteps),
                step => !IsDebugActive && !IsMutationBusy && CanMoveSelectionToSection(step, _endSteps));
            OpenVariablesCommand = new RelayCommand(
                OpenVariablesDialog,
                () => !IsDebugActive && !IsMutationBusy);
            AddVariableCommand = new RelayCommand(AddVariable, () => !IsDebugActive && !IsMutationBusy);
            DeleteVariableCommand = new AsyncRelayCommand<JobVariableEditorViewModel?>(
                DeleteVariableAsync,
                variable => variable != null && !IsDebugActive && !IsMutationBusy);
            DuplicateVariableCommand = new RelayCommand<JobVariableEditorViewModel?>(
                DuplicateVariable,
                variable => variable != null && !IsDebugActive && !IsMutationBusy);
            PromoteVariableCommand = new RelayCommand<JobVariableEditorViewModel?>(
                PromoteVariable,
                variable => variable?.IsStepValue == true && !IsDebugActive && !IsMutationBusy);

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
            InitializeProviderSources();
            ScheduleValidation();
        }

// ---------- Step property changes ----------
        private void OpenFileInExplorer()
            => ShowFileInExplorer(_jobAppService.GetStoragePath(), Job.Id.ToString());

        private static void ShowFileInExplorer(string directory, string key)
        {
            var path = Common.JsonRepository.JsonRepositoryPath.ForKey(directory, key);
            Directory.CreateDirectory(directory);
            if (!File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
                return;
            }

            var startInfo = new ProcessStartInfo("notepad.exe") { UseShellExecute = true };
            startInfo.ArgumentList.Add(path);
            Process.Start(startInfo);
        }

        private void OnStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(JobStep.IsEnabled))
            {
                JobValidation.RemoveInvalidSourceSelections(AllSteps());
                ScheduleDirtyCheck();
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
            ScheduleDirtyCheck();
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

        private void ScheduleDirtyCheck()
        {
            if (_suppressDirtyTracking)
                return;

            _changeTracker.Evaluate(CaptureCurrentEditState());
        }

        internal Task WaitForDirtyStateAsync() => _changeTracker.WhenIdleAsync();

        private JobEditState CaptureSavedEditState() => new(
            _savedStartSnapshot,
            _savedSnapshot,
            _savedEndSnapshot,
            _savedVariables,
            _savedEndPhaseTimeoutSeconds,
            _savedRepeating);

        private JobEditState CaptureCurrentEditState() => new(
            _startSteps.ToArray(),
            _runSteps.ToArray(),
            _endSteps.ToArray(),
            Job.Variables.ToArray(),
            EndPhaseTimeoutSeconds,
            IsRepeating);

        private static async Task<bool> JobStatesMatchAsync(
            JobEditState baseline,
            JobEditState current,
            CancellationToken cancellationToken)
        {
            if (baseline.EndPhaseTimeoutSeconds != current.EndPhaseTimeoutSeconds
                || baseline.Repeating != current.Repeating)
                return false;

            var baselineSerialized = await JobStepsSnapshotService.SerializeAsync(
                baseline.StartSteps, baseline.RunSteps, baseline.EndSteps, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var currentSerialized = await JobStepsSnapshotService.SerializeAsync(
                current.StartSteps, current.RunSteps, current.EndSteps, cancellationToken).ConfigureAwait(false);
            if (baselineSerialized != currentSerialized) return false;

            var baselineVariables = JsonSerializer.Serialize(baseline.Variables);
            var currentVariables = JsonSerializer.Serialize(current.Variables);
            return baselineVariables == currentVariables;
        }

        private void AddVariable()
        {
            var variable = new JobVariable
            {
                Name = Loc.Get("Ui.Job.Variables.NewName"),
                Scope = JobVariableScope.Shared,
                ValueKind = ResultValueKind.Text,
                Cardinality = ResultCardinality.Single,
                Value = System.Text.Json.Nodes.JsonValue.Create(string.Empty)
            };
            Job.Variables.Add(variable);
            JobVariables.Add(CreateVariableEditor(variable));
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
            InvalidateReferenceDisplays();
            ScheduleDirtyCheck();
            ScheduleValidation();
        }

        private void RegisterCreatedVariable(JobVariable variable)
        {
            if (Job.Variables.Any(existing => existing.Id == variable.Id)) return;
            Job.Variables.Add(variable);
            JobVariables.Add(CreateVariableEditor(variable));
            OnPropertyChanged(nameof(Variables));
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
            InvalidateReferenceDisplays();
            ScheduleDirtyCheck();
            ScheduleValidation();
        }

        private async Task DeleteVariableAsync(JobVariableEditorViewModel? editor)
        {
            if (editor == null) return;
            var workingJob = new Job
            {
                StartSteps = _startSteps.ToList(),
                Steps = _runSteps.ToList(),
                EndSteps = _endSteps.ToList()
            };
            var usages = ValueReferenceUsageInspector.Find(
                workingJob,
                ValueProviderIds.JobVariable,
                editor.Model.Id.ToString("D"));
            if (usages.Count > 0)
            {
                var steps = usages.Select(usage => StepLocalization.Type(usage.Step.GetType().Name))
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .Take(5)
                    .ToArray();
                _dialogService.ShowError(
                    Loc.Format(
                        "Ui.Job.Variables.Delete.InUse",
                        editor.Name,
                        usages.Count,
                        string.Join(", ", steps)),
                    Loc.Get("Ui.Job.Variables.Delete.InUseTitle"));
                return;
            }
            var message = Loc.Format("Ui.Job.Variables.Delete.Message", editor.Name);
            if (!await _dialogService.ConfirmAsync(message, Loc.Get("Ui.Job.Variables.Delete.Title"))) return;

            Job.Variables.Remove(editor.Model);
            JobVariables.Remove(editor);
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
            InvalidateReferenceDisplays();
            ScheduleDirtyCheck();
            ScheduleValidation();
        }

        private JobVariableEditorViewModel CreateVariableEditor(JobVariable variable)
        {
            var editor = new JobVariableEditorViewModel(variable, OnVariableChanged);
            UpdateVariableUsage(editor);
            return editor;
        }

        private void UpdateVariableUsage(JobVariableEditorViewModel editor)
        {
            var usages = ValueReferenceUsageInspector.Find(
                new Job { StartSteps = _startSteps.ToList(), Steps = _runSteps.ToList(), EndSteps = _endSteps.ToList() },
                ValueProviderIds.JobVariable,
                editor.Id.ToString("D"));
            var usageSteps = usages
                .Select(usage => StepLocalization.Type(usage.Step.GetType().Name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            var summary = string.Join(Environment.NewLine, usageSteps);
            editor.SetUsage(usages.Count, summary, usageSteps);
        }

        private void RefreshVariableUsages()
        {
            foreach (var editor in JobVariables) UpdateVariableUsage(editor);
            RefreshVariableFilter();
        }

        private void ResetVariableEditors(IEnumerable<JobVariable> variables)
        {
            JobVariables.Clear();
            foreach (var variable in variables) JobVariables.Add(CreateVariableEditor(variable));
            OnPropertyChanged(nameof(Variables));
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
        }

        private void OpenVariablesDialog()
        {
            RefreshVariableUsages();
            var dialog = new JobVariablesDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = this
            };
            dialog.ShowDialog();
        }

        private void OnVariableChanged()
        {
            RefreshVariableFilter();
            InvalidateReferenceDisplays();
            ScheduleDirtyCheck();
            ScheduleValidation();
        }

        private void DuplicateVariable(JobVariableEditorViewModel? editor)
        {
            if (editor is null) return;
            var copy = DeepCloneVariables([editor.Model]).Single();
            copy.Id = Guid.NewGuid();
            copy.Name = Loc.Format("Ui.Job.Variables.CopyName", editor.Name);
            Job.Variables.Add(copy);
            JobVariables.Add(CreateVariableEditor(copy));
            OnPropertyChanged(nameof(Variables));
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
            InvalidateReferenceDisplays();
            ScheduleDirtyCheck();
        }

        private void PromoteVariable(JobVariableEditorViewModel? editor)
        {
            if (editor?.IsStepValue != true) return;
            editor.PromoteToShared();
            RefreshVariableFilter();
            InvalidateReferenceDisplays();
            ScheduleDirtyCheck();
        }

        private bool MatchesVariableFilter(object item)
        {
            if (item is not JobVariableEditorViewModel variable) return false;
            if (variable.IsShared && !ShowSharedVariables || variable.IsStepValue && !ShowStepValues) return false;
            if (variable.IsUsed && !ShowUsedVariables || !variable.IsUsed && !ShowUnusedVariables) return false;
            if (string.IsNullOrWhiteSpace(VariableSearchText)) return true;
            var search = VariableSearchText.Trim();
            return variable.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || variable.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || variable.SearchValue.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || variable.UsageSummary.Contains(search, StringComparison.CurrentCultureIgnoreCase);
        }

        private void RefreshVariableFilter()
        {
            OnPropertyChanged(nameof(FilteredJobVariables));
            OnPropertyChanged(nameof(HasFilteredJobVariables));
        }

        private void CleanupUnusedStepValues()
        {
            var usedIds = ValueReferenceUsageInspector.Find(new Job
                {
                    StartSteps = _startSteps.ToList(), Steps = _runSteps.ToList(), EndSteps = _endSteps.ToList()
                })
                .Where(usage => string.Equals(usage.Reference.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal))
                .Select(usage => usage.Reference.SourceId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unused = Job.Variables
                .Where(variable => variable.Scope == JobVariableScope.StepValue
                                   && !usedIds.Contains(variable.Id.ToString("D")))
                .ToArray();
            foreach (var variable in unused)
            {
                Job.Variables.Remove(variable);
                var editor = JobVariables.FirstOrDefault(candidate => candidate.Id == variable.Id);
                if (editor is not null) JobVariables.Remove(editor);
            }
            if (unused.Length > 0) OnPropertyChanged(nameof(HasJobVariables));
        }

        private void InvalidateReferenceDisplays()
        {
            StepsVersion++;
            OnPropertyChanged(nameof(StepsVersion));
        }

        private static List<JobVariable> DeepCloneVariables(IEnumerable<JobVariable> variables)
        {
            var json = JsonSerializer.Serialize(variables);
            return JsonSerializer.Deserialize<List<JobVariable>>(json) ?? [];
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

        private async Task ToggleBreakpointsAsync(JobStep? step)
        {
            var targets = GetOrderedSelection(step);
            if (targets.Count == 0) return;
            var enable = targets.Any(target => !target.IsBreakpoint);
            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                foreach (var target in targets)
                {
                    target.IsBreakpoint = enable;
                    SynchronizeBreakpointWithRuntimeJob(target);
                }
                ScheduleDirtyCheck();
            });
        }

        private async Task ToggleSelectedStepsEnabledAsync(JobStep? step)
        {
            var targets = GetOrderedSelection(step).Where(target => target.CanBeDisabled).ToList();
            if (targets.Count == 0) return;
            var enable = targets.Any(target => !target.IsEnabled);
            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                foreach (var target in targets) target.IsEnabled = enable;
                ScheduleDirtyCheck();
            });
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
                    ? BuildConditionDebugNodes(conditionEvaluation, steps, Job.Variables)
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
            IList<JobStep> steps,
            IReadOnlyList<JobVariable> variables)
        {
            var conditionNodes = evaluation.Conditions
                .Select((item, index) =>
                {
                    var children = new List<JobDebugValueNode>
                    {
                        new(
                            Loc.Get("Ui.Job.Debug.Condition.Expression"),
                            ConditionDisplayFormatter.Format(
                                item.Definition,
                                steps as System.Collections.IList,
                                variables),
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
            NotifySelectionChanged();
            // Keep SelectedStep in sync with the last selected item
            if (SelectedSteps.Count > 0)
                SelectedStep = SelectedSteps[^1];
            else
                SelectedStep = null;
            InvalidateAllCommands();
        }

        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedStepCount));
            OnPropertyChanged(nameof(HasSelectedSteps));
            OnPropertyChanged(nameof(HasMultipleSelectedSteps));
            OnPropertyChanged(nameof(SelectedStepsSummary));
        }

        private JobStep? GetSingleSelection(JobStep? context)
        {
            if (SelectedSteps.Count > 1 && (context is null || SelectedSteps.Contains(context)))
                return null;
            return context ?? SelectedStep;
        }

        private List<JobStep> GetOrderedSelection(JobStep? context = null, bool expandStructures = false)
        {
            var selected = SelectedSteps.Count > 1 && (context is null || SelectedSteps.Contains(context))
                ? SelectedSteps.ToList()
                : context is not null
                    ? [context]
                    : SelectedSteps.Count > 0
                        ? SelectedSteps.ToList()
                        : SelectedStep is not null ? [SelectedStep] : [];
            if (selected.Count == 0) return [];

            var section = FindSection(selected[0]);
            if (section is null || selected.Any(step => !section.Contains(step))) return [];
            var indices = selected.Select(section.IndexOf).Where(index => index >= 0).ToHashSet();
            if (expandStructures)
            {
                foreach (var index in indices.ToArray())
                {
                    if (section[index] is not (IfStep or ElseIfStep or ElseStep or EndIfStep)) continue;
                    var first = FindOwningIfIndex(section, index);
                    var last = first >= 0 ? FindMatchingEndIfIndex(section, first) : -1;
                    if (first < 0 || last < first) continue;
                    for (var blockIndex = first; blockIndex <= last; blockIndex++) indices.Add(blockIndex);
                }
            }
            return indices.OrderBy(index => index).Select(index => section[index]).ToList();
        }

        private int GetSelectionInsertionIndex()
        {
            var selected = GetOrderedSelection();
            return selected.Count == 0
                ? _steps.Count
                : Math.Min(_steps.Count, selected.Max(_steps.IndexOf) + 1);
        }

        // ---------- INavigationGuard ----------
        public async Task SaveAsync() => await Save();

        public void DiscardChanges()
        {
            _suppressDirtyTracking = true;
            BeginCollectionUpdate();
            try
            {
                _startSteps.ReplaceRange(DeepCloneSteps(_savedStartSnapshot));
                _runSteps.ReplaceRange(DeepCloneSteps(_savedSnapshot));
                _steps = _runSteps;
                _endSteps.ReplaceRange(DeepCloneSteps(_savedEndSnapshot));
                SelectedStep = null;
                SelectedSteps.Clear();
                NotifySelectionChanged();
                _undoStack.Clear();
                _redoStack.Clear();
            }
            finally
            {
                EndCollectionUpdate();
                _suppressDirtyTracking = false;
            }
            Job.StartSteps = DeepCloneSteps(_savedStartSnapshot);
            Job.Steps = DeepCloneSteps(_savedSnapshot);
            Job.EndSteps = DeepCloneSteps(_savedEndSnapshot);
            Job.Variables = DeepCloneVariables(_savedVariables);
            ResetVariableEditors(Job.Variables);
            _endPhaseTimeoutSeconds = _savedEndPhaseTimeoutSeconds;
            Job.EndPhaseTimeoutSeconds = _savedEndPhaseTimeoutSeconds;
            _isRepeating = _savedRepeating;
            Job.Repeating = _savedRepeating;
            OnPropertyChanged(nameof(EndPhaseTimeoutSeconds));
            OnPropertyChanged(nameof(IsRepeating));
            _changeTracker.Accept(CaptureSavedEditState());
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            InvalidateHistoryCommands();
            InvalidateAllCommands();
            ScheduleValidation();
        }

        private async Task ConfirmDiscardChangesAsync()
        {
            if (await _dialogService.ConfirmAsync(
                    Loc.Get("Dialog.Discard.Message"),
                    Loc.Get("Dialog.Discard.Title")))
                DiscardChanges();
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
                    Variables = Job.Variables.ToList(),
                    Repeating = IsRepeating,
                    EndPhaseTimeoutSeconds = EndPhaseTimeoutSeconds
                }, _providerSources));
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
            Job.Repeating = IsRepeating;
            Job.EndPhaseTimeoutSeconds = EndPhaseTimeoutSeconds;
            await _jobAppService.SaveJobAsync(Job);
            var savedSerialized = await JobStepsSnapshotService.SerializeAsync(
                _startSteps.ToArray(), _runSteps.ToArray(), _endSteps.ToArray());
            var savedMaterialized = await JobStepsSnapshotService.DeserializeAsync(savedSerialized);
            _savedStartSnapshot = savedMaterialized.StartSteps.ToList();
            _savedSnapshot = savedMaterialized.RunSteps.ToList();
            _savedEndSnapshot = savedMaterialized.EndSteps.ToList();
            _savedVariables = DeepCloneVariables(Job.Variables);
            _savedEndPhaseTimeoutSeconds = EndPhaseTimeoutSeconds;
            _savedRepeating = IsRepeating;
            _changeTracker.Accept(CaptureSavedEditState());
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
            int insertIndex = GetSelectionInsertionIndex();

            var precedingSteps = GetPrecedingSteps(_steps, insertIndex);
            var allSteps = AllSteps();
            var preparedSources = await PrepareDialogSourcesAsync(precedingSteps);
            var providerSources = await LoadProviderSourcesAsync();
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id, allSteps, preparedSources, _cameraCaptureService, _stepDefinitionCatalog, Job.Variables, providerSources, RegisterCreatedVariable)
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
                    CleanupUnusedStepValues();
                // If-Abfrage: automatisch EndIf direkt dahinter einfügen
                    SelectedStep = vm.CreatedStep;
                    ScheduleDirtyCheck();
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
            var providerSources = await LoadProviderSourcesAsync();
            var vm = new AddJobStepDialogViewModel(
                _jobExecutionContext, precedingSteps, Job.Id, allSteps, preparedSources, _cameraCaptureService, _stepDefinitionCatalog, Job.Variables, providerSources, RegisterCreatedVariable);
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
                CleanupUnusedStepValues();
                SelectedStep = vm.CreatedStep;
                ScheduleDirtyCheck();
            });
        }

        // ---------- Prefill ----------
        private static void Prefill(AddJobStepDialogViewModel vm, JobStep s)
        {
            if (!vm.TryLoadGeneratedStep(s))
                throw new InvalidOperationException($"No step definition registered for {s.GetType().Name}.");
        }

        // ---------- Move / Delete ----------
        private bool CanMoveSelectionRelative(JobStep? step, int delta)
        {
            var moving = GetOrderedSelection(step, expandStructures: true);
            if (moving.Count == 0 || FindSection(moving[0]) is not { } section) return false;
            var movingSet = moving.ToHashSet();
            var first = section.IndexOf(moving[0]);
            var last = section.IndexOf(moving[^1]);
            if (delta < 0 && first == 0 || delta > 0 && last == section.Count - 1) return false;
            var anchor = delta < 0 ? first - 1 : last + 1;
            if (movingSet.Contains(section[anchor])) return false;
            return TryBuildSelectionMove(section, moving, anchor, delta, out _);
        }

        private async Task MoveSelectionRelativeAsync(JobStep? step, int delta)
        {
            var moving = GetOrderedSelection(step, expandStructures: true);
            if (moving.Count == 0 || FindSection(moving[0]) is not { } section) return;
            var anchor = delta < 0 ? section.IndexOf(moving[0]) - 1 : section.IndexOf(moving[^1]) + 1;
            if (!TryBuildSelectionMove(section, moving, anchor, delta, out var reordered)) return;

            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                section.ReplaceRange(reordered);
                _steps = section;
                JobValidation.RemoveInvalidSourceSelections(AllSteps());
                SelectedSteps.Clear();
                SelectedSteps.AddRange(moving);
                SelectedStep = moving[^1];
                NotifySelectionChanged();
                ScheduleDirtyCheck();
                InvalidateSelectionCommands();
            });
        }

        private static bool TryBuildSelectionMove(
            IReadOnlyList<JobStep> section,
            IReadOnlyList<JobStep> moving,
            int anchorIndex,
            int delta,
            out List<JobStep> reordered)
        {
            reordered = section.ToList();
            if (anchorIndex < 0 || anchorIndex >= section.Count) return false;
            var anchor = section[anchorIndex];
            var movingSet = moving.ToHashSet();
            if (movingSet.Contains(anchor)) return false;
            reordered.RemoveAll(movingSet.Contains);
            var insertAt = reordered.IndexOf(anchor) + (delta > 0 ? 1 : 0);
            reordered.InsertRange(insertAt, moving);
            return JobValidation.IsIfStructureAllowed(reordered) && !section.SequenceEqual(reordered);
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

            var dragged = source[request.SourceIndex];
            var moving = GetOrderedSelection(dragged, expandStructures: true);
            if (moving.Count == 0 || moving.Any(step => !source.Contains(step))) return;
            var movingIndices = moving.Select(source.IndexOf).OrderBy(index => index).ToList();
            var first = movingIndices[0];
            var last = movingIndices[^1];
            int insertIndex = Math.Clamp(request.TargetIndex, 0, target.Count);

            // Ein Drop innerhalb des gerade gezogenen Blocks verändert nichts.
            if (ReferenceEquals(source, target)
                && movingIndices.Contains(Math.Clamp(insertIndex, 0, Math.Max(0, source.Count - 1))))
                return;

            var sourceSimulation = source.ToList();
            sourceSimulation.RemoveAll(moving.ToHashSet().Contains);

            var targetSimulation = ReferenceEquals(source, target)
                ? sourceSimulation
                : target.ToList();

            if (ReferenceEquals(source, target))
                insertIndex -= movingIndices.Count(index => index < insertIndex);
            insertIndex = Math.Clamp(insertIndex, 0, targetSimulation.Count);
            targetSimulation.InsertRange(insertIndex, moving);

            if (!JobValidation.IsIfStructureAllowed(sourceSimulation)
                || !JobValidation.IsIfStructureAllowed(targetSimulation))
                return;

            if (ReferenceEquals(source, target)
                && source.SequenceEqual(targetSimulation))
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
                SelectedStep = moving[^1];
                _steps = target;
                SelectedSteps.Clear();
                SelectedSteps.AddRange(moving);
                NotifySelectionChanged();
                ScheduleDirtyCheck();
                ExpandSection(target);
                InvalidateSelectionCommands();
            });
        }

        private bool CanMoveSelectionToSection(JobStep? step, ObservableRangeCollection<JobStep> target)
        {
            var moving = GetOrderedSelection(step, expandStructures: true);
            return moving.Count > 0
                   && FindSection(moving[0]) is { } source
                   && moving.All(source.Contains)
                   && !ReferenceEquals(source, target)
                   && JobValidation.IsIfStructureAllowed(source.Where(item => !moving.Contains(item)).ToList())
                   && JobValidation.IsIfStructureAllowed(target.Concat(moving).ToList());
        }

        private Task MoveSelectionToSectionAsync(JobStep? step, ObservableRangeCollection<JobStep> target)
        {
            var moving = GetOrderedSelection(step, expandStructures: true);
            if (moving.Count == 0 || FindSection(moving[0]) is not { } source || ReferenceEquals(source, target))
                return Task.CompletedTask;
            return MoveStepAsync(new StepDragDrop.MoveRequest(source, source.IndexOf(moving[0]), target, target.Count,
                SourceIndices: moving.Select(source.IndexOf).ToArray()));
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
            var variableSnapshot = DeepCloneVariables(Job.Variables);
            _ = ValidateAsync();

            async Task ValidateAsync()
            {
                try
                {
                    if (!await WaitForValidationDebounceAsync(cts.Token)
                        || generation != _validationGeneration) return;
                    var serialized = await JobStepsSnapshotService.SerializeAsync(
                        startSnapshot, runSnapshot, endSnapshot);
                    if (cts.IsCancellationRequested || generation != _validationGeneration) return;
                    var materialized = await JobStepsSnapshotService.DeserializeAsync(serialized);
                    if (cts.IsCancellationRequested || generation != _validationGeneration) return;
                    var result = await Task.Run(() => JobValidation.ValidateJob(new Job
                        {
                            StartSteps = materialized.StartSteps.ToList(),
                            Steps = materialized.RunSteps.ToList(),
                            EndSteps = materialized.EndSteps.ToList(),
                            Variables = variableSnapshot
                        }, _providerSources));
                    if (cts.IsCancellationRequested || generation != _validationGeneration) return;
                    await Application.Current.Dispatcher.InvokeAsync(() => ApplyValidation(result, generation));
                }
                catch (OperationCanceledException) { }
            }
        }

        internal static async Task<bool> WaitForValidationDebounceAsync(
            CancellationToken cancellationToken,
            int delayMilliseconds = 120)
        {
            await Task.Delay(delayMilliseconds);
            return !cancellationToken.IsCancellationRequested;
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
                ScheduleDirtyCheck();
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
            NotifySelectionChanged();
            ScheduleDirtyCheck();
        }

        // ---------- Copy / Paste ----------
        private async Task CopySelectedAsync()
        {
            var sources = GetOrderedSelection(expandStructures: true);
            if (sources.Count == 0) return;
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
                int insertAt = GetSelectionInsertionIndex();

                var toInsert = (await JobStepsSnapshotService.CloneAsync(_clipboard, newIds: true)).ToList();
                await PushUndoAsync();
                _steps.InsertRange(insertAt, toInsert);
                SelectedSteps.Clear();
                SelectedSteps.AddRange(toInsert);
                SelectedStep = toInsert[^1];
                NotifySelectionChanged();
                ScheduleDirtyCheck();
                InvalidateSelectionCommands();
            });
        }

        private async Task DuplicateSelectedAsync()
        {
            await CopySelectedAsync();
            await PasteAsync();
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

        private async Task<IReadOnlyList<ValueProviderSourceDescriptor>> LoadProviderSourcesAsync()
        {
            if (_secretStore is null) return [];
            var secrets = await _secretStore.ListAsync();
            _providerSources = secrets.Select(secret => new ValueProviderSourceDescriptor(
                    ValueProviderIds.Secret,
                    secret.Id.ToString("D"),
                    secret.Name,
                    secret.Description,
                    ResultValueKind.Text,
                    ResultCardinality.Single,
                    IsSensitive: true))
                .ToArray();
            OnPropertyChanged(nameof(ProviderSources));
            InvalidateReferenceDisplays();
            return _providerSources;
        }

        private async void InitializeProviderSources()
        {
            try
            {
                await LoadProviderSourcesAsync();
                ScheduleValidation();
            }
            catch (SecretStoreException)
            {
                // The Secrets settings page owns storage error reporting. The job editor
                // remains usable and validates secret references again after the next load.
            }
        }

        // ---------- Delete selected ----------
        private async Task DeleteSelectedAsync()
        {
            var targets = GetOrderedSelection(expandStructures: SelectedSteps.Count > 1);
            if (targets.Count == 0) return;

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
                ScheduleDirtyCheck();
                NotifySelectionChanged();
                InvalidateSelectionCommands();
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
            var providerSources = await LoadProviderSourcesAsync();
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id, allSteps, preparedSources, _cameraCaptureService, _stepDefinitionCatalog, Job.Variables, providerSources, RegisterCreatedVariable)
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
                    ScheduleDirtyCheck();
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
                ScheduleDirtyCheck();
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
                _changeTracker.Dispose();
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
            (DuplicateStepCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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
            (CancelCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (StartJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DebugJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void InvalidateDebugCommands()
        {
            (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
            (DuplicateStepCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (AddElseIfCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (AddElseCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToStartSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToRunSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToEndSectionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (OpenVariablesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (AddVariableCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteVariableCommand as AsyncRelayCommand<JobVariableEditorViewModel?>)?.RaiseCanExecuteChanged();
        }

        private void InvalidateAllCommands()
        {
            InvalidateSaveCommands();
            InvalidateMutationCommands();
            (RenameCommand        as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (StartJobCommand      as RelayCommand)?.RaiseCanExecuteChanged();
            (StopJobCommand       as RelayCommand)?.RaiseCanExecuteChanged();
            InvalidateDebugCommands();
            (ExpandDebugContextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CollapseDebugContextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleBreakpointCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (ToggleStepEnabledCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
        }

    }
}
