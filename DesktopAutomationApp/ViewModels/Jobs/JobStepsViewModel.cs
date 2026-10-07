using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;
using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;
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
using System.ComponentModel;
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
        private JobVariablesDialog? _openVariablesDialog;
        private bool _variableDraftSessionActive;
        private readonly List<JobVariable> _pendingDeletedVariables = [];

        private sealed record JobStepsSnapshot(
            List<JobStep> StartSteps,
            List<JobStep> RunSteps,
            List<JobStep> EndSteps,
            List<LocalValue> LocalValues);

        private sealed record JobEditState(
            IReadOnlyList<JobStep> StartSteps,
            IReadOnlyList<JobStep> RunSteps,
            IReadOnlyList<JobStep> EndSteps,
            IReadOnlyList<JobVariable> Variables,
            IReadOnlyList<LocalValue> LocalValues,
            int EndPhaseTimeoutSeconds,
            bool Repeating);

        private readonly Stack<JobStepsSnapshot> _undoStack = new();
        private readonly Stack<JobStepsSnapshot> _redoStack = new();
        private List<JobStep> _clipboard = new();
        private List<LocalValue> _clipboardLocalValues = new();
        private List<JobStep> _savedSnapshot;
        private List<JobStep> _savedStartSnapshot;
        private List<JobStep> _savedEndSnapshot;
        private List<JobVariable> _savedVariables;
        private List<LocalValue> _savedLocalValues;
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
        private AddJobStepDialogViewModel? _selectedStepEditor;
        private string? _selectedEditorStepId;
        private bool _inlineEditCheckpointCreated;

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
        public IReadOnlyList<DebugContextGroup> SelectedDebugContextGroups => SelectedStep is null
            ? []
            : _debugContextGroups.Where(group => group.StepId == SelectedStep.Id).ToArray();
        public bool HasSelectedDebugContext => SelectedDebugContextGroups.Count > 0;

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
        public ObservableCollection<JobVariableEditorViewModel> FilteredJobVariables { get; } = [];
        public IReadOnlyList<JobVariable> Variables => Job.Variables;
        public IReadOnlyList<LocalValue> LocalValues => Job.LocalValues;
        public IReadOnlyList<ValueProviderSourceDescriptor> ProviderSources => _providerSources;
        public IReadOnlyList<JobStep> AllJobSteps => _allJobStepsSnapshot;
        public int StartStepCount => _startSteps.Count(step => step is not EndIfStep);
        public int RunStepCount => _runSteps.Count(step => step is not EndIfStep);
        public int EndStepCount => _endSteps.Count(step => step is not EndIfStep);
        public int TotalStepCount => StartStepCount + RunStepCount + EndStepCount;
        public string TotalStepsSummary => Loc.Format("Ui.Job.Steps.TotalCount", TotalStepCount);
        public AddJobStepDialogViewModel? SelectedStepEditor
        {
            get => _selectedStepEditor;
            private set
            {
                if (ReferenceEquals(_selectedStepEditor, value)) return;
                if (_selectedStepEditor is not null)
                    _selectedStepEditor.PropertyChanged -= OnSelectedStepEditorPropertyChanged;
                _selectedStepEditor = value;
                if (_selectedStepEditor is not null)
                    _selectedStepEditor.PropertyChanged += OnSelectedStepEditorPropertyChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedGeneratedEditor));
                OnPropertyChanged(nameof(HasSelectedStepEditor));
                NotifyInlineEditorValidationChanged();
            }
        }
        public GeneratedStepEditorViewModel? SelectedGeneratedEditor => SelectedStepEditor?.GeneratedEditor;
        public bool HasSelectedStepEditor => SelectedGeneratedEditor is not null && HasSingleSelectedStep;
        public bool HasSingleSelectedStep => SelectedStep is not null && SelectedSteps.Count <= 1;
        public sealed record StepValidationIssue(JobStep Step, string Message, bool IsDraft = false)
        {
            public string DisplayName => Step is EndIfStep ? Loc.Get("Ui.Job.Steps.BlockStructureProblem") : StepLocalization.Type(Step.GetType());
        }
        private sealed record InvalidEditorDraft(AddJobStepDialogViewModel Editor, bool HasCheckpoint);
        private readonly HashSet<string> _editedEditorIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, InvalidEditorDraft> _invalidEditorDrafts = new(StringComparer.Ordinal);

        private void CaptureInvalidEditorDraft()
        {
            if (_selectedEditorStepId is not { } id || SelectedGeneratedEditor is not { } generated
                || SelectedStepEditor is not { } editor || !_editedEditorIds.Contains(id)
                || !AllSteps().Any(step => step.Id == id)) return;
            if (!generated.TryCreateWorkingStep(out _))
                _invalidEditorDrafts[id] = new(editor, _inlineEditCheckpointCreated);
            else _invalidEditorDrafts.Remove(id);
        }

        public string? SelectedStepValidationError => ValidationIssues.FirstOrDefault(issue => issue.Step.Id == SelectedStep?.Id)?.Message;
        public bool HasSelectedStepValidationError => !string.IsNullOrWhiteSpace(SelectedStepValidationError);
        public bool HasInlineEditorError => !string.IsNullOrWhiteSpace(InlineEditorValidationError);
        public string? InlineEditorValidationError => SelectedGeneratedEditor?.ValidationError;
        public string SelectedStepDisplayName => SelectedStep is null
            ? string.Empty
            : StepLocalization.Type(SelectedStep.GetType());
        public string SelectedStepDescription => SelectedStepEditor?.StepTypeDescription ?? string.Empty;
        public string SelectedStepNumber => SelectedStep is null
            ? string.Empty
            : StepLocalization.DisplayNumber(AllSteps(), SelectedStep)?.ToString() ?? string.Empty;
        public bool HasJobVariables => JobVariables.Count > 0;
        public bool HasFilteredJobVariables => FilteredJobVariables.Count > 0;
        public bool HasManagedJobVariables => JobVariables.Any(variable => variable.IsShared);
        public bool HasEmptyVariableView => !HasManagedJobVariables;
        public bool HasEmptyVariableFilterResult => HasManagedJobVariables && !HasFilteredJobVariables;
        public bool HasVariableDraftChanges => _pendingDeletedVariables.Count > 0
                                               || JobVariables.Any(variable => variable.IsDirty);
        public int VariableDraftChangeCount => _pendingDeletedVariables.Count
                                               + JobVariables.Count(variable => variable.IsDirty);
        public string VariableDraftStatusText => VariableDraftChangeCount switch
        {
            0 => Loc.Get("Ui.Job.Variables.Draft.Status.None"),
            1 => Loc.Get("Ui.Job.Variables.Draft.Status.One"),
            var count => Loc.Format("Ui.Job.Variables.Draft.Status.Many", count)
        };
        public bool CanApplySelectedVariable => SelectedJobVariable?.IsDirty == true;
        public bool ShowApplyAllVariableChanges => VariableDraftChangeCount > 1
                                                   || _pendingDeletedVariables.Count > 0
                                                   || VariableDraftChangeCount == 1 && !CanApplySelectedVariable;
        public bool CanApplyVariableToSelectedUsage => SelectedJobVariable?.HasMultipleUsages == true
                                                       && SelectedVariableUsage is not null;
        private JobVariableEditorViewModel? _selectedJobVariable;
        public JobVariableEditorViewModel? SelectedJobVariable
        {
            get => _selectedJobVariable;
            set
            {
                if (ReferenceEquals(_selectedJobVariable, value)) return;
                SetProperty(ref _selectedJobVariable, value);
                SelectedVariableUsage = null;
                OnPropertyChanged(nameof(HasSelectedJobVariable));
                OnPropertyChanged(nameof(ShowApplyAllVariableChanges));
                InvalidateVariableDraftCommands();
            }
        }
        public bool HasSelectedJobVariable => SelectedJobVariable is not null;
        private JobVariableUsageViewModel? _selectedVariableUsage;
        public JobVariableUsageViewModel? SelectedVariableUsage
        {
            get => _selectedVariableUsage;
            set
            {
                if (ReferenceEquals(_selectedVariableUsage, value)) return;
                SetProperty(ref _selectedVariableUsage, value);
                OnPropertyChanged(nameof(CanApplyVariableToSelectedUsage));
                InvalidateVariableDraftCommands();
            }
        }
        private string _variableSearchText = string.Empty;
        public string VariableSearchText
        {
            get => _variableSearchText;
            set { value ??= string.Empty; if (_variableSearchText == value) return; SetProperty(ref _variableSearchText, value); RefreshVariableFilter(); }
        }
        public IReadOnlyList<JobVariableTypeFilterOption> VariableTypeFilterOptions { get; } =
        [
            new(null, "Ui.Job.Variables.Filter.AllTypes"),
            new(ResultValueKind.Text, "Ui.Job.Variables.Type.Text"),
            new(ResultValueKind.Boolean, "Ui.Job.Variables.Type.Boolean"),
            new(ResultValueKind.Integer, "Ui.Job.Variables.Type.Integer"),
            new(ResultValueKind.Number, "Ui.Job.Variables.Type.Number"),
            new(ResultValueKind.DateTime, "Ui.Job.Variables.Type.DateTime"),
            new(ResultValueKind.Point, "Ui.Job.Variables.Type.Point"),
            new(ResultValueKind.Rectangle, "Ui.Job.Variables.Type.Rectangle"),
            new(ResultValueKind.Color, "Ui.Job.Variables.Type.Color"),
            new(ResultValueKind.FilePath, "Ui.Job.Variables.Type.FilePath")
        ];
        private JobVariableTypeFilterOption? _selectedVariableTypeFilter;
        public JobVariableTypeFilterOption SelectedVariableTypeFilter
        {
            get => _selectedVariableTypeFilter ??= VariableTypeFilterOptions[0];
            set
            {
                if (ReferenceEquals(_selectedVariableTypeFilter, value)) return;
                SetProperty(ref _selectedVariableTypeFilter, value);
                RefreshVariableFilter();
            }
        }
        public bool HasActiveVariableFilters => HasTypeVariableFilter;
        public bool HasTypeVariableFilter => SelectedVariableTypeFilter.Kind.HasValue;
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
        public bool HasStartStepErrors => ValidationIssues.Any(issue => _startSteps.Any(step => step.Id == issue.Step.Id));
        public bool HasStepErrors => ValidationIssues.Any(issue => _runSteps.Any(step => step.Id == issue.Step.Id));
        public bool HasEndStepErrors => ValidationIssues.Any(issue => _endSteps.Any(step => step.Id == issue.Step.Id));
        public int ValidationErrorCount => ValidationIssues.Count;
        public bool HasValidationErrors => ValidationErrorCount > 0;
        public int SelectedStepCount => SelectedSteps.Count;
        public bool HasSelectedSteps => SelectedStepCount > 0;
        public bool HasMultipleSelectedSteps => SelectedStepCount > 1;
        public string SelectedStepsSummary => Loc.Format("Ui.Job.Steps.SelectedCount", SelectedStepCount);
        public IReadOnlyList<StepValidationIssue> ValidationIssues => AllSteps().Select(step =>
        {
            var draft = _invalidEditorDrafts.GetValueOrDefault(step.Id)?.Editor.GeneratedEditor?.ValidationError;
            var messages = new[] { !step.IsValid ? step.ValidationError : null, draft, ReferenceEquals(step, SelectedStep) ? InlineEditorValidationError : null };
            var message = string.Join(Environment.NewLine, messages.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
            var visible = step is EndIfStep && FindSection(step) is { } section
                ? StepListProjection.Get(section, StepsVersion).VisibleOwner(step) ?? step : step;
            return string.IsNullOrWhiteSpace(message) ? null : new StepValidationIssue(visible, message, draft is not null);
        }).OfType<StepValidationIssue>().GroupBy(issue => issue.Step.Id)
            .Select(group => new StepValidationIssue(group.First().Step,
                string.Join(Environment.NewLine, group.Select(issue => issue.Message).Distinct()), group.Any(issue => issue.IsDraft))).ToArray();
        public ICommand ShowValidationIssueCommand { get; }

        private void ShowValidationIssue(JobStep? step)
        {
            if (step is null || FindSection(step) is not { } section) return;
            if (ReferenceEquals(section, _startSteps)) IsStartSectionExpanded = true;
            else if (ReferenceEquals(section, _endSteps)) IsEndSectionExpanded = true;
            else IsRunSectionExpanded = true;
            // A matched closing marker is intentionally hidden; reveal its owning header.
            var index = section.IndexOf(step);
            var block = step is EndIfStep
                ? ControlFlowStructureAnalyzer.Analyze(section).Blocks.FirstOrDefault(candidate => candidate.EndIndex == index)
                : null;
            SetSelectedSteps([block is null ? step : section[block.StartIndex]], section);
            OnPropertyChanged(nameof(SelectedStep));
        }

        public string ValidationSummary => Loc.Format("Ui.Job.Steps.ProblemCount", ValidationErrorCount);

        private bool _isStartSectionExpanded = true;
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

        private bool _isEndSectionExpanded = true;
        public bool IsEndSectionExpanded
        {
            get => _isEndSectionExpanded;
            set { _isEndSectionExpanded = value; OnPropertyChanged(); }
        }

        /// <summary>Incrementiert bei jeder Listenänderung; wird von Konvertern als Cache-Schlüssel genutzt.</summary>
        public int StepsVersion { get; private set; }
        public IReadOnlyCollection<string> CollapsedBlockIds { get; private set; } = Array.Empty<string>();
        public ICommand ToggleBlockCommand { get; }
        public ICommand ExpandDropBlockCommand { get; }


        public ICommand RemoveConditionCommand { get; }
        public string SelectedStepBreadcrumb => SelectedStep is not { } step || FindSection(step) is not { } section
            ? string.Empty
            : string.Join(" › ", ControlFlowStructureAnalyzer.Analyze(section).Blocks
                .Where(block => block.StartIndex < section.IndexOf(step) && block.Contains(section.IndexOf(step), section.Count))
                .OrderBy(block => block.Depth).Select(block => StepLocalization.Type(section[block.StartIndex].GetType())));
        private void ToggleBlock(JobStep? step)
        {
            if (step is null) return;
            var ids = CollapsedBlockIds.ToHashSet();
            if (!ids.Remove(step.Id)) ids.Add(step.Id);
            CollapsedBlockIds = ids;
            OnPropertyChanged(nameof(CollapsedBlockIds));
        }


        private void RevealStep(JobStep? value)
        {
            if (value is not null && FindSection(value) is { } selectedSection)
            {
                var projection = StepListProjection.Get(selectedSection, StepsVersion);
                var ancestors = projection.CollapsedOwners(selectedSection.IndexOf(value), CollapsedBlockIds).ToHashSet();
                CollapsedBlockIds = CollapsedBlockIds.Where(id => !ancestors.Contains(id)).ToArray();
                OnPropertyChanged(nameof(CollapsedBlockIds));
            }
        }

        private JobStep? _selectedStep;
        public JobStep? SelectedStep
        {
            get => _selectedStep;
            set
            {
                if (value is EndIfStep && FindSection(value) is { } markerSection)
                    value = StepListProjection.Get(markerSection, StepsVersion).VisibleOwner(value);
                RevealStep(value);
                if (ReferenceEquals(_selectedStep, value)) return;
                CaptureInvalidEditorDraft();
                _selectedStep = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedStepBreadcrumb));
                OnPropertyChanged(nameof(SelectedStepDisplayName));
                OnPropertyChanged(nameof(SelectedStepNumber));
                OnPropertyChanged(nameof(HasSingleSelectedStep));
                NotifyDebugInspectorChanged();
                OnPropertyChanged(nameof(SelectedDebugContextGroups));
                OnPropertyChanged(nameof(HasSelectedDebugContext));
                InvalidateSelectionCommands();
                RefreshSelectedStepEditor();
            }
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges || _invalidEditorDrafts.Count > 0;
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
        public StepDragDrop.DragIndexResolver DragIndicesResolver { get; }
        public StepDragDrop.PreviewValidator PreviewMoveValidator { get; }
        public StepDragDrop.InsertionPlacementResolver DropPlacementResolver { get; } = JobStepDropPlacement.Resolve;
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
        public ICommand OpenVariableUsageCommand { get; }
        public ICommand ApplySelectedVariableCommand { get; }
        public ICommand ApplyAllVariableChangesCommand { get; }
        public ICommand DiscardVariableChangesCommand { get; }
        public ICommand ApplyVariableToSelectedUsageCommand { get; }

        public event Action? RequestBack;

        public CollapsiblePaneState InspectorPane { get; }

        public JobStepsViewModel(
            Job job,
            IJobExecutor jobExecutionContext,
            IJobApplicationService jobAppService,
            IDialogService dialogService,
            IJobDispatcher dispatcher,
            ICameraCaptureService cameraCaptureService,
            IStepDefinitionCatalog? stepDefinitionCatalog = null,
            ISecretStore? secretStore = null,
            DesktopAutomation.Application.Settings.IUserPreferencesService? preferences = null)
        {
            InspectorPane = new CollapsiblePaneState(CollapsiblePaneState.InspectorCollapseWidth, preferences?.Current.StepInspectorCollapsed ?? false, async collapsed =>
            {
                if (preferences is null) return;
                preferences.Current.StepInspectorCollapsed = collapsed;
                await preferences.SaveAsync();
            });
            Job = job ?? throw new ArgumentNullException(nameof(job));
            Job.Variables ??= [];
            Job.LocalValues ??= [];
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
            _isStartSectionExpanded = true;
            _isEndSectionExpanded = true;
            _savedStartSnapshot = DeepCloneSteps(_startSteps);
            _savedSnapshot = DeepCloneSteps(_runSteps);
            _savedEndSnapshot = DeepCloneSteps(_endSteps);
            _savedVariables = DeepCloneVariables(Job.Variables);
            _savedLocalValues = DeepCloneLocalValues(Job.LocalValues);
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

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand = new AsyncRelayCommand(Save, () => HasUnsavedChanges && !IsDebugActive && !IsMutationBusy);
            CancelCommand = new AsyncRelayCommand(ConfirmDiscardChangesAsync, () => HasUnsavedChanges && !IsDebugActive);
            RenameCommand = new AsyncRelayCommand(Rename, () => !IsDebugActive);
            OpenFileCommand = new RelayCommand(OpenFileInExplorer);

            AddStepCommand = new AsyncRelayCommand(AddStep, () => !IsDebugActive && !IsMutationBusy);
            EditStepCommand = new AsyncRelayCommand<JobStep?>(
                s => EditStep(GetSingleSelection(s)),
                s => { var t = GetSingleSelection(s); return !IsDebugActive && !IsMutationBusy && t != null && t is not TaskAutomation.Jobs.ElseStep and not TaskAutomation.Jobs.EndIfStep; });
            MoveStepUpCommand = new AsyncRelayCommand<JobStep?>(s => MoveSelectionRelativeAsync(s, -1), s => !IsDebugActive && !IsMutationBusy && CanMoveSelectionRelative(s, -1));
            MoveStepDownCommand = new AsyncRelayCommand<JobStep?>(s => MoveSelectionRelativeAsync(s, +1), s => !IsDebugActive && !IsMutationBusy && CanMoveSelectionRelative(s, +1));
            DragIndicesResolver = ResolveDragIndices;
            PreviewMoveValidator = CanPreviewMove;
            ReorderStepCommand = new AsyncRelayCommand<StepDragDrop.MoveRequest>(MoveStepAsync, _ => !IsDebugActive && !IsMutationBusy);
            ShowValidationIssueCommand = new RelayCommand<JobStep?>(ShowValidationIssue);
            ToggleBlockCommand = new RelayCommand<JobStep?>(ToggleBlock);
            ExpandDropBlockCommand = new RelayCommand<JobStep?>(ToggleBlock,
                step => step is IfStep or ElseIfStep or ElseStep && CollapsedBlockIds.Contains(step.Id));
            RemoveConditionCommand = new AsyncRelayCommand<JobStep?>(RemoveConditionAsync,
                step => !IsDebugActive && !IsMutationBusy && (step ?? SelectedStep) is IfStep);
            DeleteStepCommand = new AsyncRelayCommand<JobStep?>(DeleteStepAsync, s => !IsDebugActive && !IsMutationBusy && (s ?? SelectedStep) != null);
            DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => !IsDebugActive && !IsMutationBusy && (SelectedSteps.Count > 0 || SelectedStep != null));
            UndoCommand = new AsyncRelayCommand(UndoAsync, () => !IsDebugActive && !IsMutationBusy && CanUndo);
            RedoCommand = new AsyncRelayCommand(RedoAsync, () => !IsDebugActive && !IsMutationBusy && CanRedo);
            CopyCommand = new AsyncRelayCommand(CopySelectedAsync, () => !IsMutationBusy && (SelectedSteps.Count > 0 || SelectedStep != null));
            PasteCommand = new AsyncRelayCommand(PasteAsync, () => !IsDebugActive && !IsMutationBusy && _clipboard.Count > 0);
            DuplicateStepCommand = new AsyncRelayCommand(DuplicateSelectedAsync, () => !IsDebugActive && !IsMutationBusy && (SelectedSteps.Count > 0 || SelectedStep != null));

            StartJobCommand = new RelayCommand(() =>
            {
                try { _dispatcher.StartJob(Job.Id); }
                catch (JobLimitExceededException) { }
            }, () => !IsJobRunning && !HasUnsavedChanges && !HasValidationErrors && !IsDebugActive && AllSteps().Any(step => step.IsEnabled));
            StopJobCommand = new RelayCommand(() =>
            {
                if (IsDebugActive && _debugSession != null)
                    _dispatcher.CancelDebugJob(_debugSession.InstanceId);
                else
                    _dispatcher.CancelJobsByDefinition(Job.Id);
            }, () => CanRequestJobStop || IsDebugActive);
            DebugJobCommand = new RelayCommand(StartDebugJob,
                () => !IsJobRunning && !HasUnsavedChanges && !HasValidationErrors && AllSteps().Any(step => step.IsEnabled));
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

            AddElseIfCommand = new AsyncRelayCommand<JobStep?>(step => AddElseIfAsync(step ?? SelectedStep), step => !IsDebugActive && !IsMutationBusy && CanAddElseIf(step ?? SelectedStep));
            AddElseCommand = new AsyncRelayCommand<JobStep?>(step => AddElseAsync(step ?? SelectedStep), step => !IsDebugActive && !IsMutationBusy && CanAddElse(step ?? SelectedStep));
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
            OpenVariableUsageCommand = new RelayCommand<JobVariableUsageViewModel?>(NavigateToVariableUsage);
            ApplySelectedVariableCommand = new RelayCommand(
                () => ApplySelectedVariableDraft(),
                () => CanApplySelectedVariable);
            ApplyAllVariableChangesCommand = new RelayCommand(
                () => ApplyAllVariableDrafts(),
                () => HasVariableDraftChanges);
            DiscardVariableChangesCommand = new RelayCommand(
                DiscardVariableDrafts,
                () => HasVariableDraftChanges);
            ApplyVariableToSelectedUsageCommand = new RelayCommand<JobVariableUsageViewModel?>(usage =>
            {
                if (usage is not null) SelectedVariableUsage = usage;
                ApplyVariableDraftToSelectedUsage();
            }, usage => SelectedJobVariable?.HasMultipleUsages == true
                && (usage is null ? SelectedVariableUsage is not null : SelectedJobVariable.UsageItems.Contains(usage)));

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
                JobValidation.RemoveInvalidSourceSelections(ReferenceJob());
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
            var liveIds = AllSteps().Select(step => step.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var id in _invalidEditorDrafts.Keys.Where(id => !liveIds.Contains(id)).ToArray()) _invalidEditorDrafts.Remove(id);
            _editedEditorIds.RemoveWhere(id => !liveIds.Contains(id));
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
            OnPropertyChanged(nameof(TotalStepCount));
            OnPropertyChanged(nameof(StartStepCount));
            OnPropertyChanged(nameof(RunStepCount));
            OnPropertyChanged(nameof(EndStepCount));
            OnPropertyChanged(nameof(TotalStepsSummary));
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
            _savedLocalValues,
            _savedEndPhaseTimeoutSeconds,
            _savedRepeating);

        private JobEditState CaptureCurrentEditState() => new(
            _startSteps.ToArray(),
            _runSteps.ToArray(),
            _endSteps.ToArray(),
            Job.Variables.ToArray(),
            Job.LocalValues.ToArray(),
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
                baseline.StartSteps, baseline.RunSteps, baseline.EndSteps).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) return false;
            var currentSerialized = await JobStepsSnapshotService.SerializeAsync(
                current.StartSteps, current.RunSteps, current.EndSteps).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) return false;
            if (baselineSerialized != currentSerialized) return false;

            var baselineVariables = JsonSerializer.Serialize(baseline.Variables);
            var currentVariables = JsonSerializer.Serialize(current.Variables);
            if (baselineVariables != currentVariables) return false;
            return JsonSerializer.Serialize(baseline.LocalValues)
                   == JsonSerializer.Serialize(current.LocalValues);
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
            var editor = CreateVariableEditor(variable);
            if (_variableDraftSessionActive)
                editor.BeginDraftSession(isNew: true);
            else
                Job.Variables.Add(variable);
            JobVariables.Add(editor);
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
            SelectedJobVariable = editor;
            OnVariableChanged(null);
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

        private void RegisterCreatedLocalValue(LocalValue value)
        {
            if (Job.LocalValues.Any(existing => existing.Id == value.Id)) return;
            Job.LocalValues.Add(value);
            _providerSources = _providerSources.Append(ValueProviderSourceDescriptor.FromVariable(value))
                .DistinctBy(source => (source.ProviderId, source.SourceId)).ToArray();
            OnPropertyChanged(nameof(ProviderSources));
            ScheduleDirtyCheck();
            ScheduleValidation();
        }

        private Task DeleteVariableAsync(JobVariableEditorViewModel? editor) => DeleteVariableAsync(editor, true);

        private async Task DeleteVariableAsync(JobVariableEditorViewModel? editor, bool confirm)
        {
            if (editor == null) return;
            var workingJob = ReferenceJob();
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
            if (confirm && !await _dialogService.ConfirmAsync(message, Loc.Get("Ui.Job.Variables.Delete.Title"))) return;

            var oldIndex = JobVariables.IndexOf(editor);
            if (_variableDraftSessionActive)
            {
                if (!editor.IsNewDraft) _pendingDeletedVariables.Add(editor.CommittedModel);
            }
            else
            {
                Job.Variables.Remove(editor.Model);
            }
            JobVariables.Remove(editor);
            if (ReferenceEquals(SelectedJobVariable, editor))
                SelectedJobVariable = JobVariables.ElementAtOrDefault(Math.Min(oldIndex, JobVariables.Count - 1));
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
            InvalidateReferenceDisplays();
            if (_variableDraftSessionActive)
                NotifyVariableDraftStateChanged();
            else
            {
                ScheduleDirtyCheck();
                ScheduleValidation();
            }
        }

        private JobVariableEditorViewModel CreateVariableEditor(JobVariable variable)
        {
            var editor = new JobVariableEditorViewModel(variable, OnVariableChanged);
            UpdateVariableUsage(editor);
            return editor;
        }

        private void UpdateVariableUsage(JobVariableEditorViewModel editor)
        {
            var allSteps = AllSteps().ToArray();
            var logicalUsages = ValueReferenceUsageInspector.FindLogical(
                ReferenceJob(),
                ValueProviderIds.JobVariable,
                editor.Id.ToString("D"));
            var usageItems = logicalUsages
                .Select(usage => new JobVariableUsageViewModel(
                    usage.Step,
                    usage.Reference,
                    StepLocalization.NumberedName(usage.Step, allSteps),
                    VariableUsageInputName(usage),
                    usage.Path))
                .ToArray();
            var summary = string.Join(Environment.NewLine, usageItems
                .Select(usage => usage.StepName)
                .Distinct(StringComparer.CurrentCultureIgnoreCase));
            editor.SetUsage(logicalUsages.Count, summary, usageItems);
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
            SelectedJobVariable = JobVariables.FirstOrDefault();
            OnPropertyChanged(nameof(Variables));
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
        }

        private void OpenVariablesDialog()
        {
            RefreshVariableUsages();
            BeginVariableDraftSession();
            var dialog = new JobVariablesDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = this
            };
            SelectedJobVariable ??= FilteredJobVariables.FirstOrDefault();
            _openVariablesDialog = dialog;
            try { dialog.ShowDialog(); }
            finally
            {
                _openVariablesDialog = null;
                EndVariableDraftSession();
            }
        }

        private void OnVariableChanged(string? propertyName)
        {
            if (!string.IsNullOrWhiteSpace(VariableSearchText)
                || HasTypeVariableFilter && propertyName == nameof(JobVariableEditorViewModel.SelectedKind))
                RefreshVariableFilter();
            InvalidateReferenceDisplays();
            if (_variableDraftSessionActive)
            {
                NotifyVariableDraftStateChanged();
                return;
            }
            ScheduleDirtyCheck();
            ScheduleValidation();
        }

        private void DuplicateVariable(JobVariableEditorViewModel? editor)
        {
            if (editor is null) return;
            var copy = DeepCloneVariables([editor.Model]).Single();
            copy.Id = Guid.NewGuid();
            copy.Name = Loc.Format("Ui.Job.Variables.CopyName", editor.Name);
            var copyEditor = CreateVariableEditor(copy);
            if (_variableDraftSessionActive)
                copyEditor.BeginDraftSession(isNew: true);
            else
                Job.Variables.Add(copy);
            JobVariables.Add(copyEditor);
            OnPropertyChanged(nameof(Variables));
            OnPropertyChanged(nameof(HasJobVariables));
            RefreshVariableFilter();
            SelectedJobVariable = copyEditor;
            OnVariableChanged(null);
        }

        public bool TryCloseVariableDraftSession()
        {
            if (!HasVariableDraftChanges) return true;
            var decision = _dialogService.ConfirmWithCancelAsync(
                    Loc.Get("Ui.Job.Variables.Close.Message"),
                    Loc.Get("Ui.Job.Variables.Close.Title"))
                .GetAwaiter().GetResult();
            return decision switch
            {
                true => ApplyAllVariableDrafts(),
                false => DiscardVariableDraftsAndContinue(),
                _ => false
            };
        }

        public void BeginVariableDraftSession()
        {
            if (_variableDraftSessionActive) return;
            _variableDraftSessionActive = true;
            _pendingDeletedVariables.Clear();
            foreach (var editor in JobVariables) editor.BeginDraftSession();
            NotifyVariableDraftStateChanged();
        }

        private void EndVariableDraftSession()
        {
            if (!_variableDraftSessionActive) return;
            foreach (var editor in JobVariables) editor.EndDraftSession();
            _pendingDeletedVariables.Clear();
            _variableDraftSessionActive = false;
            ResetVariableEditors(Job.Variables);
            NotifyVariableDraftStateChanged();
        }

        public async Task DeleteVariableSelectionAsync(IReadOnlyList<JobVariableEditorViewModel> selected)
        {
            if (IsDebugActive || IsMutationBusy || selected.Count == 0) return;
            var working = ReferenceJob();
            if (selected.Any(editor => ValueReferenceUsageInspector.Find(working, ValueProviderIds.JobVariable, editor.Model.Id.ToString("D")).Count > 0))
            {
                _dialogService.ShowError(Loc.Get("Ui.Context.VariablesInUse"), Loc.Get("Ui.Job.Variables.Delete.Title"));
                return;
            }
            if (!await _dialogService.ConfirmAsync(Loc.Format("Ui.Context.DeleteSelection", selected.Count), Loc.Get("Ui.Job.Variables.Delete.Title"))) return;
            foreach (var editor in selected) await DeleteVariableAsync(editor, false);
        }

        public void DuplicateVariableSelection(IReadOnlyList<JobVariableEditorViewModel> selected)
        {
            foreach (var editor in selected) if (DuplicateVariableCommand.CanExecute(editor)) DuplicateVariable(editor);
        }

        public bool ApplyVariableSelection(IReadOnlyList<JobVariableEditorViewModel> selected)
        {
            if (!ValidateVariableDrafts(selected)) return false;
            foreach (var editor in selected.Where(editor => editor.IsDirty)) CommitVariableDraft(editor);
            CompleteVariableDraftCommit();
            return true;
        }

        private bool ApplySelectedVariableDraft()
        {
            var editor = SelectedJobVariable;
            if (editor is null || !editor.IsDirty || !ValidateVariableDrafts([editor])) return false;
            CommitVariableDraft(editor);
            CompleteVariableDraftCommit();
            return true;
        }

        private bool ApplyAllVariableDrafts()
        {
            var changed = JobVariables.Where(variable => variable.IsDirty).ToArray();
            if (_pendingDeletedVariables.Count == 0 && changed.Length == 0) return true;
            if (!ValidateVariableDrafts(JobVariables)) return false;

            foreach (var deleted in _pendingDeletedVariables)
                Job.Variables.RemoveAll(variable => variable.Id == deleted.Id);
            _pendingDeletedVariables.Clear();
            foreach (var editor in changed) CommitVariableDraft(editor);
            CompleteVariableDraftCommit();
            return true;
        }

        private void CommitVariableDraft(JobVariableEditorViewModel editor)
        {
            if (!Job.Variables.Any(variable => variable.Id == editor.Id))
                Job.Variables.Add(editor.CommittedModel);
            editor.AcceptDraft();
        }

        private void CompleteVariableDraftCommit()
        {
            RefreshVariableUsages();
            InvalidateReferenceDisplays();
            ScheduleDirtyCheck();
            ScheduleValidation();
            NotifyVariableDraftStateChanged();
        }

        private void DiscardVariableDrafts()
        {
            DiscardVariableDraftsAndContinue();
        }

        private bool DiscardVariableDraftsAndContinue()
        {
            var selectedId = SelectedJobVariable?.Id;
            _pendingDeletedVariables.Clear();
            ResetVariableEditors(Job.Variables);
            foreach (var editor in JobVariables) editor.BeginDraftSession();
            SelectedJobVariable = JobVariables.FirstOrDefault(variable => variable.Id == selectedId)
                                  ?? JobVariables.FirstOrDefault();
            NotifyVariableDraftStateChanged();
            return true;
        }

        private void ApplyVariableDraftToSelectedUsage()
        {
            var editor = SelectedJobVariable;
            var usage = SelectedVariableUsage;
            if (editor is null || usage is null || !ValidateVariableDrafts([editor])) return;

            var detached = DeepCloneVariables([editor.Model]).Single();
            detached.Id = Guid.NewGuid();
            detached.Name = UniqueVariableName(Loc.Format("Ui.Job.Variables.CopyName", editor.Name));
            Job.Variables.Add(detached);
            var detachedId = detached.Id.ToString("D");
            var logicalPath = ValueReferenceUsageInspector.NormalizeLogicalPath(usage.SearchText);
            var workingJob = ReferenceJob();
            foreach (var matchingUsage in ValueReferenceUsageInspector.Find(
                         workingJob, ValueProviderIds.JobVariable, editor.Id.ToString("D"))
                     .Where(candidate => ReferenceEquals(candidate.Step, usage.Step)
                                         && ValueReferenceUsageInspector.NormalizeLogicalPath(candidate.Path) == logicalPath))
                matchingUsage.UpdateReference(reference => reference.SourceId = detachedId);
            editor.DiscardDraft();

            var detachedEditor = CreateVariableEditor(detached);
            detachedEditor.BeginDraftSession();
            JobVariables.Add(detachedEditor);
            SelectedJobVariable = detachedEditor;
            CompleteVariableDraftCommit();
        }

        private bool ValidateVariableDrafts(IEnumerable<JobVariableEditorViewModel> editors)
        {
            var candidates = JobVariables.ToArray();
            var invalid = editors.FirstOrDefault(editor => string.IsNullOrWhiteSpace(editor.Name));
            if (invalid is not null)
            {
                SelectedJobVariable = invalid;
                _dialogService.ShowError(
                    Loc.Get("Ui.ValueReference.CreateVariable.NameRequired"),
                    Loc.Get("Ui.Job.Variables.Validation.Title"));
                return false;
            }

            invalid = editors.FirstOrDefault(editor => candidates.Any(other =>
                !ReferenceEquals(other, editor)
                && string.Equals(other.Name.Trim(), editor.Name.Trim(), StringComparison.CurrentCultureIgnoreCase)));
            if (invalid is null) return true;
            SelectedJobVariable = invalid;
            _dialogService.ShowError(
                Loc.Get("Ui.ValueReference.CreateVariable.NameDuplicate"),
                Loc.Get("Ui.Job.Variables.Validation.Title"));
            return false;
        }

        private string UniqueVariableName(string requested)
        {
            var names = Job.Variables.Select(variable => variable.Name)
                .Concat(JobVariables.Select(variable => variable.Name))
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            if (!names.Contains(requested)) return requested;
            for (var suffix = 2; ; suffix++)
            {
                var candidate = $"{requested} {suffix}";
                if (!names.Contains(candidate)) return candidate;
            }
        }

        private void NotifyVariableDraftStateChanged()
        {
            OnPropertyChanged(nameof(HasVariableDraftChanges));
            OnPropertyChanged(nameof(VariableDraftChangeCount));
            OnPropertyChanged(nameof(VariableDraftStatusText));
            OnPropertyChanged(nameof(CanApplySelectedVariable));
            OnPropertyChanged(nameof(ShowApplyAllVariableChanges));
            OnPropertyChanged(nameof(CanApplyVariableToSelectedUsage));
            InvalidateVariableDraftCommands();
        }

        private void InvalidateVariableDraftCommands()
        {
            (ApplySelectedVariableCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ApplyAllVariableChangesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DiscardVariableChangesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ApplyVariableToSelectedUsageCommand as RelayCommand<JobVariableUsageViewModel?>)?.RaiseCanExecuteChanged();
        }

        private bool MatchesVariableFilter(object item)
        {
            if (item is not JobVariableEditorViewModel variable) return false;
            if (!variable.IsShared) return false;
            if (SelectedVariableTypeFilter.Kind is { } kind && variable.Model.ValueKind != kind) return false;
            if (string.IsNullOrWhiteSpace(VariableSearchText)) return true;
            var search = VariableSearchText.Trim();
            return variable.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || variable.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || variable.SearchValue.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || variable.UsageSummary.Contains(search, StringComparison.CurrentCultureIgnoreCase);
        }

        private void RefreshVariableFilter()
        {
            var desiredVariables = JobVariables.Where(MatchesVariableFilter).ToArray();
            for (var index = FilteredJobVariables.Count - 1; index >= 0; index--)
            {
                if (!desiredVariables.Contains(FilteredJobVariables[index]))
                    FilteredJobVariables.RemoveAt(index);
            }
            for (var index = 0; index < desiredVariables.Length; index++)
            {
                var variable = desiredVariables[index];
                var currentIndex = FilteredJobVariables.IndexOf(variable);
                if (currentIndex < 0)
                    FilteredJobVariables.Insert(index, variable);
                else if (currentIndex != index)
                    FilteredJobVariables.Move(currentIndex, index);
            }
            OnPropertyChanged(nameof(HasFilteredJobVariables));
            OnPropertyChanged(nameof(HasManagedJobVariables));
            OnPropertyChanged(nameof(HasEmptyVariableView));
            OnPropertyChanged(nameof(HasEmptyVariableFilterResult));
            OnPropertyChanged(nameof(HasActiveVariableFilters));
            OnPropertyChanged(nameof(HasTypeVariableFilter));
            if (SelectedJobVariable is not null && !FilteredJobVariables.Contains(SelectedJobVariable))
                SelectedJobVariable = FilteredJobVariables.FirstOrDefault();
        }

        private string VariableUsageInputName(ValueReferenceUsage usage)
        {
            if (_stepDefinitionCatalog.TryGetByType(usage.Step.GetType(), out var definition))
            {
                var field = definition.Descriptor.Fields.FirstOrDefault(candidate =>
                    usage.Path.Contains(candidate.Id, StringComparison.OrdinalIgnoreCase));
                if (field is not null) return Loc.Get(field.LabelKey);
            }

            var leaf = usage.Path.Split('.', '[', ']').LastOrDefault(part => !string.IsNullOrWhiteSpace(part));
            return StepLocalization.PropertyPath(leaf ?? usage.Path);
        }

        private void NavigateToVariableUsage(JobVariableUsageViewModel? usage)
        {
            if (usage is null) return;
            SelectedSteps.Clear();
            SelectedStep = usage.Step;
            _openVariablesDialog?.Close();
        }

        private Job ReferenceJob() => new()
        {
            StartSteps = _startSteps.ToList(),
            Steps = _runSteps.ToList(),
            EndSteps = _endSteps.ToList(),
            Variables = Job.Variables,
            LocalValues = Job.LocalValues
        };

        private void CleanupUnusedStepValues()
        {
            var usedIds = ValueReferenceUsageInspector.Find(ReferenceJob())
                .Where(usage => string.Equals(usage.Reference.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal))
                .Select(usage => usage.Reference.SourceId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unused = Job.LocalValues
                .Where(variable => !usedIds.Contains(variable.Id.ToString("D")))
                .ToArray();
            foreach (var variable in unused)
            {
                Job.LocalValues.Remove(variable);
            }
            if (unused.Length > 0)
            {
                var ids = unused.Select(value => value.Id.ToString("D"))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                _providerSources = _providerSources.Where(source =>
                    source.ProviderId != ValueProviderIds.LocalValue || !ids.Contains(source.SourceId)).ToArray();
                OnPropertyChanged(nameof(ProviderSources));
            }
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

        private static List<LocalValue> DeepCloneLocalValues(IEnumerable<LocalValue> values)
        {
            var json = JsonSerializer.Serialize(values);
            return JsonSerializer.Deserialize<List<LocalValue>>(json) ?? [];
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

        private Task ToggleBreakpointsAsync(JobStep? step)
            => SetSelectionBreakpointsAsync(step, GetOrderedSelection(step).Any(target => !target.IsBreakpoint));

        public async Task SetSelectionBreakpointsAsync(JobStep? step, bool enable)
        {
            var targets = GetOrderedSelection(step);
            if (IsMutationBusy || targets.Count == 0) return;
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

        private Task ToggleSelectedStepsEnabledAsync(JobStep? step)
            => SetSelectionEnabledAsync(step, GetOrderedSelection(step).Any(target => target.CanBeDisabled && !target.IsEnabled));

        public async Task SetSelectionEnabledAsync(JobStep? step, bool enable)
        {
            var targets = GetOrderedSelection(step).Where(target => target.CanBeDisabled).ToList();
            if (IsDebugActive || IsMutationBusy || targets.Count == 0) return;
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
            OnPropertyChanged(nameof(SelectedDebugContextGroups));
            OnPropertyChanged(nameof(HasSelectedDebugContext));
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
            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(ValidationSummary));
            OnPropertyChanged(nameof(ValidationIssues));
            OnPropertyChanged(nameof(SelectedStepValidationError));
            OnPropertyChanged(nameof(HasSelectedStepValidationError));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(AllJobSteps));
        }

        // ---------- Selection sync (called from code-behind) ----------
        public void SetSelectedSteps(IEnumerable<object> items, System.Collections.IList? section = null)
        {
            if (section is ObservableRangeCollection<JobStep> typedSection && IsKnownSection(typedSection))
                _steps = typedSection;
            var selectedItems = items.OfType<JobStep>().Where(step => step is not EndIfStep).ToList();
            // Replacing a materialized inline-editor draft makes WPF briefly report an empty
            // selection. The mutation publishes the replacement selection immediately after
            // the collection change; treating this transient event as a user deselection would
            // dispose the active editor session and hide its validation state.
            if (selectedItems.Count == 0 && IsMutationBusy && SelectedStep is not null)
                return;
            SelectedSteps.Clear();
            SelectedSteps.AddRange(selectedItems);
            // Keep SelectedStep in sync with the last selected item
            if (SelectedSteps.Count > 0)
                SelectedStep = SelectedSteps[^1];
            else
                SelectedStep = null;
            NotifySelectionChanged();
            InvalidateAllCommands();
        }

        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedStepCount));
            OnPropertyChanged(nameof(HasSelectedSteps));
            OnPropertyChanged(nameof(HasMultipleSelectedSteps));
            OnPropertyChanged(nameof(SelectedStepsSummary));
            OnPropertyChanged(nameof(HasSingleSelectedStep));
            RefreshSelectedStepEditor();
        }

        private void RefreshSelectedStepEditor()
        {
            if (!HasSingleSelectedStep || SelectedStep is null)
            {
                CaptureInvalidEditorDraft();
                _selectedEditorStepId = null;
                _inlineEditCheckpointCreated = false;
                SelectedStepEditor = null;
                OnPropertyChanged(nameof(SelectedStepDescription));
                return;
            }

            if (SelectedStepEditor is not null
                && string.Equals(_selectedEditorStepId, SelectedStep.Id, StringComparison.Ordinal))
                return;

            var section = FindSection(SelectedStep);
            var index = section?.IndexOf(SelectedStep) ?? -1;
            if (section is null || index < 0)
            {
                SelectedStepEditor = null;
                return;
            }

            if (_invalidEditorDrafts.TryGetValue(SelectedStep.Id, out var pending))
            {
                _selectedEditorStepId = SelectedStep.Id;
                _inlineEditCheckpointCreated = pending.HasCheckpoint;
                SelectedStepEditor = pending.Editor;
                NotifyInlineEditorValidationChanged();
                OnPropertyChanged(nameof(SelectedStepDescription));
                return;
            }

            var editor = new AddJobStepDialogViewModel(
                _jobExecutionContext,
                GetPrecedingSteps(section, index),
                Job.Id,
                AllSteps(),
                null,
                _cameraCaptureService,
                _stepDefinitionCatalog,
                Job.Variables,
                _providerSources,
                RegisterCreatedVariable,
                _secretStore,
                Job.LocalValues,
                RegisterCreatedLocalValue)
            {
                Mode = StepDialogMode.Edit,
                IsTypeLocked = true
            };
            if (!editor.TryLoadGeneratedStep(SelectedStep))
            {
                SelectedStepEditor = null;
                return;
            }

            _selectedEditorStepId = SelectedStep.Id;
            _inlineEditCheckpointCreated = false;
            SelectedStepEditor = editor;
            editor.GeneratedEditor?.TryCreateWorkingStep(out _);
            NotifyInlineEditorValidationChanged();
            OnPropertyChanged(nameof(SelectedStepDescription));
            _ = editor.InitializeAsync();
        }

        private async void OnSelectedStepEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, SelectedStepEditor)
                || e.PropertyName != nameof(AddJobStepDialogViewModel.GeneratedEditor))
                return;
            if (_selectedEditorStepId is { } id) _editedEditorIds.Add(id);
            await ApplySelectedStepEditorAsync();
        }

        private async Task<bool> ApplySelectedStepEditorAsync()
        {
            var editor = SelectedStepEditor;
            var generated = editor?.GeneratedEditor;
            var editedStepId = _selectedEditorStepId;
            if (editor is null || generated is null || string.IsNullOrWhiteSpace(editedStepId))
                return true;

            if (!generated.TryCreateWorkingStep(out var candidate) || candidate is null)
            {
                _invalidEditorDrafts[editedStepId] = new(editor, _inlineEditCheckpointCreated);
                NotifyInlineEditorValidationChanged();
                return false;
            }

            _invalidEditorDrafts.Remove(editedStepId);
            _editedEditorIds.Remove(editedStepId);
            await RunMutationAsync(async () =>
            {
                var current = AllSteps().FirstOrDefault(step => step.Id == editedStepId);
                if (current is null || FindSection(current) is not { } section) return;
                var index = section.IndexOf(current);
                if (index < 0) return;

                if (!_inlineEditCheckpointCreated)
                {
                    await PushUndoAsync();
                    _inlineEditCheckpointCreated = true;
                }

                candidate.Id = current.Id;
                candidate.IsEnabled = current.IsEnabled;
                candidate.IsBreakpoint = current.IsBreakpoint;
                editor.CommitDraftValues(candidate);
                section[index] = candidate;
                _steps = section;
                for (var selectedIndex = 0; selectedIndex < SelectedSteps.Count; selectedIndex++)
                    if (SelectedSteps[selectedIndex].Id == candidate.Id)
                        SelectedSteps[selectedIndex] = candidate;
                _selectedStep = candidate;
                OnPropertyChanged(nameof(SelectedStep));
                OnPropertyChanged(nameof(SelectedStepBreadcrumb));
                OnPropertyChanged(nameof(SelectedStepDisplayName));
                OnPropertyChanged(nameof(SelectedStepNumber));
                CleanupUnusedStepValues();
                ScheduleDirtyCheck();
                ScheduleValidation();
            });

            NotifyInlineEditorValidationChanged();
            return true;
        }

        private void NotifyInlineEditorValidationChanged()
        {
            OnPropertyChanged(nameof(HasStartStepErrors));
            OnPropertyChanged(nameof(HasStepErrors));
            OnPropertyChanged(nameof(HasEndStepErrors));
            OnPropertyChanged(nameof(InlineEditorValidationError));
            OnPropertyChanged(nameof(HasInlineEditorError));
            OnPropertyChanged(nameof(ValidationErrorCount));
            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(ValidationSummary));
            OnPropertyChanged(nameof(ValidationIssues));
            OnPropertyChanged(nameof(SelectedStepValidationError));
            OnPropertyChanged(nameof(HasSelectedStepValidationError));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            InvalidateSaveCommands();
            InvalidateAllCommands();
        }

        private JobStep? GetSingleSelection(JobStep? context)
        {
            if (SelectedSteps.Count > 1 && (context is null || SelectedSteps.Contains(context)))
                return null;
            return context ?? SelectedStep;
        }

        private List<JobStep> GetOrderedSelection(JobStep? context = null, bool expandStructures = false, bool forDeletion = false)
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
            if (forDeletion)
                indices = ControlFlowEditRules.ExpandDeletionSelection(section, indices).ToHashSet();
            else if (expandStructures)
                indices = ControlFlowEditRules.ExpandSelection(section, indices).ToHashSet();
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
            SelectedStepEditor = null;
            _invalidEditorDrafts.Clear();
            _editedEditorIds.Clear();
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
            Job.LocalValues = DeepCloneLocalValues(_savedLocalValues);
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
            await ApplySelectedStepEditorAsync();
            if (ValidationIssues.FirstOrDefault(issue => issue.IsDraft) is { } draftIssue)
            {
                ShowValidationIssue(draftIssue.Step);
                return;
            }
            JobValidation.RemoveInvalidSourceSelections(ReferenceJob());
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
                LocalValues = Job.LocalValues.ToList(),
                Repeating = IsRepeating,
                EndPhaseTimeoutSeconds = EndPhaseTimeoutSeconds
            }, _providerSources));
            ApplyValidation(validation, generation);
            if (!validation.IsValid)
            {
                if (ValidationIssues.FirstOrDefault() is { } issue) ShowValidationIssue(issue.Step);
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
            _savedLocalValues = DeepCloneLocalValues(Job.LocalValues);
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
        internal ObservableRangeCollection<JobStep> ResolveAddStepSection()
            => SelectedStep is not null && FindSection(SelectedStep) is { } section ? section : _runSteps;

        internal string DescribeAddStepTarget(ObservableRangeCollection<JobStep> section)
        {
            var phase = Loc.Get(ReferenceEquals(section, _startSteps) ? "Ui.Job.Steps.Section.Start"
                : ReferenceEquals(section, _endSteps) ? "Ui.Job.Steps.Section.End" : "Ui.Job.Steps.Section.Run");
            return SelectedStep is not null && section.Contains(SelectedStep)
                ? Loc.Format("Ui.Job.Steps.Picker.After", StepLocalization.Type(SelectedStep.GetType()), phase)
                : Loc.Format("Ui.Job.Steps.Picker.Append", phase);
        }

        private async Task AddStep()
        {
            // Determine insert position before opening the dialog so the
            // dialog receives the correct preceding-steps snapshot.
            var section = ResolveAddStepSection();
            int insertIndex = ControlFlowEditRules.ResolveAddInsertionIndex(section, SelectedStep is null ? -1 : section.IndexOf(SelectedStep));
            var insertionDescription = DescribeAddStepTarget(section);

            var precedingSteps = GetPrecedingSteps(section, insertIndex);
            var allSteps = AllSteps();
            var preparedSources = await PrepareDialogSourcesAsync(precedingSteps);
            var providerSources = await LoadProviderSourcesAsync();
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id, allSteps, preparedSources, _cameraCaptureService, _stepDefinitionCatalog, Job.Variables, providerSources, RegisterCreatedVariable, _secretStore, Job.LocalValues, RegisterCreatedLocalValue)
            { Mode = StepDialogMode.Add, IsPickerOnly = true, InsertionDescription = insertionDescription };

            ShowDialogWithVm(vm, out bool? result);

            if (result == true && vm.CreatedStep != null)
            {
                await RunMutationAsync(async () =>
                {
                    await PushUndoAsync();
                    var insertion = vm.CreatedStep is TaskAutomation.Jobs.IfStep
                        ? new JobStep[] { vm.CreatedStep, new TaskAutomation.Jobs.EndIfStep() }
                        : [vm.CreatedStep];
                    _steps = section;
                    section.InsertRange(insertIndex, insertion);
                    ExpandSection(section);
                    CleanupUnusedStepValues();
                    // If-Abfrage: automatisch EndIf direkt dahinter einfügen
                    SelectedSteps.Clear();
                    SelectedSteps.Add(vm.CreatedStep);
                    SelectedStep = vm.CreatedStep;
                    _inlineEditCheckpointCreated = true;
                    NotifySelectionChanged();
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

        private Task EditStep(JobStep? step = null)
        {
            var target = step ?? SelectedStep;
            if (target is null) return Task.CompletedTask;
            if (FindSection(target) is { } section) _steps = section;
            SelectedSteps.Clear();
            SelectedSteps.Add(target);
            SelectedStep = target;
            NotifySelectionChanged();
            RefreshSelectedStepEditor();
            return Task.CompletedTask;
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
            if (!await ConfirmMoveImpactAsync(section, section, reordered, reordered)) return;

            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                section.ReplaceRange(reordered);
                _steps = section;
                SelectedSteps.Clear();
                SelectedSteps.AddRange(moving.Where(step => step is not EndIfStep));
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
            if (anchorIndex < 0 || anchorIndex >= section.Count || moving.Contains(section[anchorIndex])) return false;
            return ControlFlowEditRules.TryMove(section, section,
                moving.Select(step => section.ToList().IndexOf(step)), anchorIndex + (delta > 0 ? 1 : 0),
                out _, out reordered);
        }

        private async Task MoveStepAsync(StepDragDrop.MoveRequest? request)
        {
            if (request is null
                || !TryCreateMoveSimulation(
                    request,
                    out var source,
                    out var target,
                    out var moving,
                    out var sourceSimulation,
                    out var targetSimulation))
                return;

            if (!await ConfirmMoveImpactAsync(source, target, sourceSimulation, targetSimulation)) return;
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
                SelectedStep = moving[^1];
                _steps = target;
                SelectedSteps.Clear();
                SelectedSteps.AddRange(moving.Where(step => step is not EndIfStep));
                NotifySelectionChanged();
                ScheduleDirtyCheck();
                ExpandSection(target);
                InvalidateSelectionCommands();
            });
        }

        private async Task<bool> ConfirmMoveImpactAsync(ObservableRangeCollection<JobStep> source,
            ObservableRangeCollection<JobStep> target, List<JobStep> sourceSteps, List<JobStep> targetSteps)
        {
            var version = StepsVersion;
            var before = ReferenceJob();
            List<JobStep> Project(ObservableRangeCollection<JobStep> phase) => ReferenceEquals(phase, target)
                ? targetSteps : ReferenceEquals(phase, source) ? sourceSteps : phase.ToList();
            var after = new Job
            {
                StartSteps = Project(_startSteps),
                Steps = Project(_runSteps),
                EndSteps = Project(_endSteps),
                Variables = Job.Variables,
                LocalValues = Job.LocalValues
            };
            var introduced = await Task.Run(() => ControlFlowEditRules.IntroducedValidationErrors(before, after, _providerSources));
            if (introduced.Count > 0 && !await _dialogService.ConfirmAsync(
                Loc.Format("Ui.Job.Steps.MoveDependencyWarning", string.Join(Environment.NewLine,
                    introduced.Select(result => StepLocalization.NumberedName(result.Step, AllJobSteps)
                        + ": " + JobValidationErrorLocalizer.Localize(string.Join(Environment.NewLine, result.Errors!), result.Step)))),
                Loc.Get("Ui.Job.Steps.ValidationTitle"))) return false;
            return StepsVersion == version && !IsDebugActive && !IsMutationBusy;
        }

        private IReadOnlyList<int> ResolveDragIndices(StepDragDrop.DragStartRequest request)
        {
            if (request.Source is not ObservableRangeCollection<JobStep> source
                || !IsKnownSection(source)
                || request.SourceIndex < 0
                || request.SourceIndex >= source.Count)
                return request.SelectedIndices;

            var moving = GetOrderedSelection(source[request.SourceIndex], expandStructures: true);
            return moving.Select(source.IndexOf).Where(index => index >= 0).OrderBy(index => index).ToArray();
        }

        private bool CanPreviewMove(StepDragDrop.MoveRequest request)
            => !IsDebugActive
               && !IsMutationBusy
               && TryCreateMoveSimulation(request, out _, out _, out _, out _, out _);

        private bool TryCreateMoveSimulation(
            StepDragDrop.MoveRequest request,
            out ObservableRangeCollection<JobStep> source,
            out ObservableRangeCollection<JobStep> target,
            out List<JobStep> moving,
            out List<JobStep> sourceSimulation,
            out List<JobStep> targetSimulation)
        {
            source = null!;
            target = null!;
            moving = [];
            sourceSimulation = [];
            targetSimulation = [];
            if (request.Source is not ObservableRangeCollection<JobStep> sourceCollection
                || request.Target is not ObservableRangeCollection<JobStep> targetCollection
                || !IsKnownSection(sourceCollection)
                || !IsKnownSection(targetCollection)
                || request.SourceIndex < 0
                || request.SourceIndex >= sourceCollection.Count)
                return false;

            // Assign only after all type and section checks passed so callers receive a coherent simulation.
            source = sourceCollection;
            target = targetCollection;

            var dragged = sourceCollection[request.SourceIndex];
            moving = request.SourceIndices is { Count: > 0 } captured
                ? ControlFlowEditRules.ExpandSelection(sourceCollection, captured).Select(index => sourceCollection[index]).ToList()
                : GetOrderedSelection(dragged, expandStructures: true);
            if (moving.Count == 0 || moving.Any(step => !sourceCollection.Contains(step))) return false;
            return ControlFlowEditRules.TryMove(sourceCollection, targetCollection,
                moving.Select(sourceCollection.IndexOf), Math.Clamp(request.TargetIndex, 0, targetCollection.Count),
                out sourceSimulation, out targetSimulation);
        }

        private bool CanMoveSelectionToSection(JobStep? step, ObservableRangeCollection<JobStep> target)
        {
            var moving = GetOrderedSelection(step, expandStructures: true);
            return moving.Count > 0
                   && FindSection(moving[0]) is { } source
                   && moving.All(source.Contains)
                   && !ReferenceEquals(source, target)
                   && ControlFlowEditRules.TryMove(source, target, moving.Select(source.IndexOf), target.Count, out _, out _);
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
            if (index < 0 || index >= steps.Count) return -1;
            return ControlFlowStructureAnalyzer.Analyze(steps)
                .GetOwningBlock(index, ControlFlowBlockKind.Conditional)?.StartIndex ?? -1;
        }

        private static int FindMatchingEndIfIndex(IReadOnlyList<JobStep> steps, int ifIndex)
        {
            if (ifIndex < 0 || ifIndex >= steps.Count) return -1;
            var structure = ControlFlowStructureAnalyzer.Analyze(steps);
            var block = structure.GetBlockStartingAt(ifIndex)
                        ?? structure.GetOwningBlock(ifIndex, ControlFlowBlockKind.Conditional);
            return block?.EndIndex ?? -1;
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
            var localValueSnapshot = DeepCloneLocalValues(Job.LocalValues);
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
                            ,
                        LocalValues = localValueSnapshot
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
                    liveStep.SetValidationResult(result.IsValid, JobValidationErrorLocalizer.Localize(result.Error, liveStep));
            }
            NotifySectionStateChanged();
        }

        private Task DeleteStepAsync(JobStep? step) => DeleteStepsAsync(GetOrderedSelection(step, forDeletion: true));

        private async Task RemoveConditionAsync(JobStep? step)
        {
            var target = step ?? SelectedStep;
            if (target is null || FindSection(target) is not { } section) return;
            var indices = ControlFlowEditRules.RemoveConditionMarkers(section, section.IndexOf(target)).ToHashSet();
            if (indices.Count == 0 || !await _dialogService.ConfirmAsync(
                Loc.Get("Step.Delete.UnwrapWarning"), Loc.Get("Dialog.Delete.Title"))) return;
            await DeleteStepsAsync(indices.Order().Select(index => section[index]).ToList(), confirm: false);
        }

        private async Task DeleteStepsAsync(List<JobStep> targets, bool confirm = true)
        {
            if (targets.Count == 0 || FindSection(targets[0]) is not { } section) return;
            if (confirm && !await _dialogService.ConfirmAsync(
                targets.Count == 1 ? Loc.Get("Step.Delete.One") : Loc.Format("Step.Delete.Many", targets.Count),
                Loc.Get("Dialog.Delete.Title"))) return;
            var first = targets.Select(section.IndexOf).Min();
            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                section.ReplaceRange(section.Except(targets).ToList());
                CleanupUnusedStepValues();
                SelectedSteps.Clear();
                SelectedStep = section.ElementAtOrDefault(Math.Max(0, first - 1));
                ScheduleDirtyCheck();
                NotifySelectionChanged();
                InvalidateSelectionCommands();
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
                materialized.EndSteps.ToList(),
                DeepCloneLocalValues(Job.LocalValues));
        }

        private void RestoreSnapshot(JobStepsSnapshot snapshot)
        {
            SelectedStepEditor = null;
            _invalidEditorDrafts.Clear();
            _editedEditorIds.Clear();
            BeginCollectionUpdate();
            try
            {
                _startSteps.ReplaceRange(snapshot.StartSteps);
                _runSteps.ReplaceRange(snapshot.RunSteps);
                _steps = _runSteps;
                _endSteps.ReplaceRange(snapshot.EndSteps);
                Job.LocalValues = DeepCloneLocalValues(snapshot.LocalValues);
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
                var graph = await JobStepsSnapshotService.CaptureGraphAsync(ReferenceJob(), sources);
                _clipboard = graph.Steps.ToList();
                _clipboardLocalValues = graph.LocalValues.ToList();
                InvalidateClipboardCommands();
            });
        }

        private async Task PasteAsync()
        {
            if (_clipboard.Count == 0) return;

            await RunMutationAsync(async () =>
            {
                int insertAt = GetSelectionInsertionIndex();

                var graph = await JobStepsSnapshotService.CloneGraphAsync(new JobStepGraph(_clipboard, _clipboardLocalValues));
                var toInsert = graph.Steps.ToList();
                await PushUndoAsync();
                Job.LocalValues.AddRange(graph.LocalValues);
                _steps.InsertRange(insertAt, toInsert);
                SelectedSteps.Clear();
                SelectedSteps.AddRange(toInsert.Where(step => step is not EndIfStep));
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
            var secretSources = _secretStore is null
                ? []
                : (await _secretStore.ListAsync()).Select(secret => new ValueProviderSourceDescriptor(
                    ValueProviderIds.Secret,
                    secret.Id.ToString("D"),
                    secret.Name,
                    secret.Description,
                    ResultValueKind.Text,
                    ResultCardinality.Single,
                    IsSensitive: true)).ToArray();
            _providerSources = Job.LocalValues.Select(ValueProviderSourceDescriptor.FromVariable)
                .Concat(secretSources).ToArray();
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
        private Task DeleteSelectedAsync() => DeleteStepsAsync(GetOrderedSelection(forDeletion: true));

        // ---------- Deep clone helpers ----------
        private static List<JobStep> DeepCloneSteps(IEnumerable<JobStep> steps, bool newIds = false)
            => steps.Select(s => DeepCloneStep(s, newIds)).ToList();

        private static JobStep DeepCloneStep(JobStep s, bool newId = false)
        {
            var json = JsonSerializer.Serialize(s, s.GetType());
            var clone = (JobStep)JsonSerializer.Deserialize(json, s.GetType())!;
            if (newId) clone.Id = Guid.NewGuid().ToString();
            return clone;
        }

        private Task AddElseIfAsync(JobStep? step) => AddAlternativeAsync(step, elseIf: true);
        private Task AddElseAsync(JobStep? step) => AddAlternativeAsync(step, elseIf: false);

        private async Task AddAlternativeAsync(JobStep? step, bool elseIf)
        {
            if (step is null || FindSection(step) is not { } section) return;
            if (ControlFlowEditRules.ResolveBranchInsertionIndex(section, section.IndexOf(step), elseIf) is not int insertIndex) return;
            await RunMutationAsync(async () =>
            {
                await PushUndoAsync();
                JobStep created = elseIf ? new ElseIfStep() : new ElseStep();
                _steps = section;
                section.InsertRange(insertIndex, [created]);
                ExpandSection(section);
                RevealStep(created);
                SelectedSteps.Clear();
                SelectedSteps.Add(created);
                SelectedStep = created;
                _inlineEditCheckpointCreated = true;
                NotifySelectionChanged();
                ScheduleDirtyCheck();
                InvalidateSelectionCommands();
            });
        }

        private bool CanAddElse(JobStep? step)
            => step is not null && FindSection(step) is { } section
               && ControlFlowEditRules.ResolveBranchInsertionIndex(section, section.IndexOf(step), elseIf: false) is not null;

        private bool CanAddElseIf(JobStep? step)
            => step is not null && FindSection(step) is { } section
               && ControlFlowEditRules.ResolveBranchInsertionIndex(section, section.IndexOf(step), elseIf: true) is not null;

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
            (RemoveConditionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
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
            (RemoveConditionCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
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
            (RenameCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (StartJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            InvalidateDebugCommands();
            (ExpandDebugContextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CollapseDebugContextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleBreakpointCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (ToggleStepEnabledCommand as AsyncRelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
        }

    }
}
