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

namespace DesktopAutomationApp.ViewModels
{
    public sealed class JobStepsViewModel : ViewModelBase, INavigationGuard
    {
        private readonly IJobExecutor _jobExecutionContext;
        private readonly ObservableCollection<JobStep> _startSteps;
        private readonly ObservableCollection<JobStep> _runSteps;
        private ObservableCollection<JobStep> _steps;
        private readonly ObservableCollection<JobStep> _endSteps;
        private readonly IJobApplicationService _jobAppService;
        private readonly IDialogService _dialogService;
        private readonly IJobDispatcher _dispatcher;

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

        /// <summary>All currently selected steps (synced from the view's ListBox.SelectedItems).</summary>
        public List<JobStep> SelectedSteps { get; } = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public Job Job { get; }
        public string Title => Job.Name;

        public ObservableCollection<JobStep> Steps => _runSteps;
        public ObservableCollection<JobStep> StartSteps => _startSteps;
        public ObservableCollection<JobStep> EndSteps => _endSteps;
        public IReadOnlyList<JobStep> AllJobSteps => AllSteps();

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
            set { _selectedStep = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set { _hasUnsavedChanges = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        private bool _isJobRunning;
        public bool IsJobRunning
        {
            get => _isJobRunning;
            private set { _isJobRunning = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        private bool _canRequestJobStop;
        public bool CanRequestJobStop
        {
            get => _canRequestJobStop;
            private set { _canRequestJobStop = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

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
        public ICommand StartJobCommand { get; }
        public ICommand StopJobCommand { get; }
        public ICommand AddElseIfCommand { get; }
        public ICommand AddElseCommand { get; }
        public ICommand MoveToStartSectionCommand { get; }
        public ICommand MoveToRunSectionCommand { get; }
        public ICommand MoveToEndSectionCommand { get; }

        public event Action? RequestBack;

        public JobStepsViewModel(Job job, IJobExecutor jobExecutionContext, IJobApplicationService jobAppService, IDialogService dialogService, IJobDispatcher dispatcher)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            _jobExecutionContext = jobExecutionContext;
            _jobAppService = jobAppService;
            _dialogService = dialogService;
            _dispatcher = dispatcher;

            _startSteps = new ObservableCollection<JobStep>(Job.StartSteps ?? Enumerable.Empty<JobStep>());
            _runSteps = new ObservableCollection<JobStep>(Job.Steps ?? Enumerable.Empty<JobStep>());
            _steps = _runSteps;
            _endSteps = new ObservableCollection<JobStep>(Job.EndSteps ?? Enumerable.Empty<JobStep>());
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
            _runSteps.CollectionChanged += (_, e) =>
            {
                if (e.OldItems != null)
                    foreach (JobStep s in e.OldItems) s.PropertyChanged -= OnStepPropertyChanged;
                if (e.NewItems != null)
                    foreach (JobStep s in e.NewItems) s.PropertyChanged += OnStepPropertyChanged;

                StepsVersion++;
                OnPropertyChanged(nameof(StepsVersion));
                NotifySectionStateChanged();
                InvalidateAllCommands();
                ScheduleValidation();
            };

            _startSteps.CollectionChanged += OnSectionCollectionChanged;
            _endSteps.CollectionChanged += OnSectionCollectionChanged;

            // Initiale Steps abonnieren
            foreach (var s in _startSteps.Concat(_runSteps).Concat(_endSteps))
                s.PropertyChanged += OnStepPropertyChanged;

            BackCommand   = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand   = new RelayCommand(async () => await Save(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(DiscardChanges, () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(async () => await Rename());
            OpenFileCommand = new RelayCommand(OpenFileInExplorer);

            AddStepCommand    = new RelayCommand(async () => await AddStep());
            EditStepCommand   = new RelayCommand<JobStep?>(
                EditStep,
                s => { var t = s ?? SelectedStep; return t != null && t is not TaskAutomation.Jobs.ElseStep and not TaskAutomation.Jobs.EndIfStep; });
            MoveStepUpCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, -1), s => CanMoveRelative(s ?? SelectedStep, -1));
            MoveStepDownCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, +1), s => CanMoveRelative(s ?? SelectedStep, +1));
            ReorderStepCommand = new RelayCommand<StepDragDrop.MoveRequest>(MoveStep);
            DeleteStepCommand    = new RelayCommand<JobStep?>(async s => await DeleteStepAsync(s), s => (s ?? SelectedStep) != null);
            DeleteSelectedCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => SelectedSteps.Count > 0 || SelectedStep != null);
            UndoCommand           = new RelayCommand(Undo, () => CanUndo);
            RedoCommand           = new RelayCommand(Redo, () => CanRedo);
            CopyCommand           = new RelayCommand(CopySelected, () => SelectedSteps.Count > 0 || SelectedStep != null);
            PasteCommand          = new RelayCommand(Paste, () => _clipboard.Count > 0);

            StartJobCommand = new RelayCommand(() =>
            {
                try { _dispatcher.StartJob(Job.Id); }
                catch (JobLimitExceededException) { /* kein Popup – wird still ignoriert */ }
            }, () => !IsJobRunning && _startSteps.Concat(_runSteps).Concat(_endSteps).Any(s => s.IsEnabled));
            StopJobCommand = new RelayCommand(() =>
            {
                _dispatcher.CancelJobsByDefinition(Job.Id);
            }, () => CanRequestJobStop);

            AddElseIfCommand = new RelayCommand<JobStep?>(AddElseIf, CanAddElseIf);
            AddElseCommand   = new RelayCommand<JobStep?>(AddElse, CanAddElse);
            MoveToStartSectionCommand = new RelayCommand<JobStep?>(
                step => MoveStepToSection(step, _startSteps),
                step => CanMoveStepToSection(step, _startSteps));
            MoveToRunSectionCommand = new RelayCommand<JobStep?>(
                step => MoveStepToSection(step, _runSteps),
                step => CanMoveStepToSection(step, _runSteps));
            MoveToEndSectionCommand = new RelayCommand<JobStep?>(
                step => MoveStepToSection(step, _endSteps),
                step => CanMoveStepToSection(step, _endSteps));

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;
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
                InvalidateAllCommands();
                ScheduleValidation();
            }
        }

        private void OnSectionCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (JobStep s in e.OldItems) s.PropertyChanged -= OnStepPropertyChanged;
            if (e.NewItems != null)
                foreach (JobStep s in e.NewItems) s.PropertyChanged += OnStepPropertyChanged;

            StepsVersion++;
            OnPropertyChanged(nameof(StepsVersion));
            NotifySectionStateChanged();
            InvalidateAllCommands();
            ScheduleValidation();
        }

        private void NotifySectionStateChanged()
        {
            OnPropertyChanged(nameof(HasStartSteps));
            OnPropertyChanged(nameof(HasSteps));
            OnPropertyChanged(nameof(HasEndSteps));
            OnPropertyChanged(nameof(HasStartStepErrors));
            OnPropertyChanged(nameof(HasStepErrors));
            OnPropertyChanged(nameof(HasEndStepErrors));
            OnPropertyChanged(nameof(AllJobSteps));
        }

        // ---------- Selection sync (called from code-behind) ----------
        public void SetSelectedSteps(IEnumerable<object> items, System.Collections.IList? section = null)
        {
            if (section is ObservableCollection<JobStep> typedSection && IsKnownSection(typedSection))
                _steps = typedSection;
            SelectedSteps.Clear();
            SelectedSteps.AddRange(items.OfType<JobStep>());
            // Keep SelectedStep in sync with the last selected item
            if (SelectedSteps.Count > 0)
                SelectedStep = SelectedSteps[^1];
            InvalidateAllCommands();
        }

        // ---------- INavigationGuard ----------
        public async Task SaveAsync() => await Save();

        public void DiscardChanges()
        {
            _startSteps.Clear();
            foreach (var s in DeepCloneSteps(_savedStartSnapshot))
                _startSteps.Add(s);
            _runSteps.Clear();
            foreach (var s in DeepCloneSteps(_savedSnapshot))
                _runSteps.Add(s);
            _endSteps.Clear();
            foreach (var s in DeepCloneSteps(_savedEndSnapshot))
                _endSteps.Add(s);
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
            var validation = await Task.Run(() => JobValidation.ValidateJob(new Job
            {
                StartSteps = _startSteps.ToList(),
                Steps = _runSteps.ToList(),
                EndSteps = _endSteps.ToList(),
                EndPhaseTimeoutSeconds = EndPhaseTimeoutSeconds
            }));
            ApplyValidation(validation);
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
            _savedStartSnapshot = DeepCloneSteps(_startSteps);
            _savedSnapshot = DeepCloneSteps(_runSteps);
            _savedEndSnapshot = DeepCloneSteps(_endSteps);
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
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id, AllSteps())
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

                _steps.Insert(insertIndex, vm.CreatedStep);
                // If-Abfrage: automatisch EndIf direkt dahinter einfügen
                if (vm.CreatedStep is TaskAutomation.Jobs.IfStep)
                    _steps.Insert(insertIndex + 1, new TaskAutomation.Jobs.EndIfStep());
                SelectedStep = vm.CreatedStep;
                PushUndo();
                HasUnsavedChanges = true;
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

        private void EditStep(JobStep? step = null)
        {
            var target = step ?? SelectedStep;
            if (target == null) return;
            if (FindSection(target) is { } section) _steps = section;

            var idx = _steps.IndexOf(target);
            if (idx < 0) return;

            // Only steps before the edited one count as "preceding" for
            // prerequisite evaluation.
            var precedingSteps = GetPrecedingSteps(_steps, idx);
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id, AllSteps())
                {
                    Mode = StepDialogMode.Edit,
                    IsTypeLocked = target is TaskAutomation.Jobs.ElseIfStep
                };
            Prefill(vm, target);

            ShowDialogWithVm(vm, out bool? result);

            if (result != true || vm.CreatedStep == null) return;

            vm.CreatedStep.Id = target.Id;   // preserve original ID
            PushUndo();
            _steps[idx] = vm.CreatedStep;
            SelectedStep = vm.CreatedStep;
            HasUnsavedChanges = true;
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
            _steps = section;
            return !WouldViolateIfStructure(idx, newIdx);
        }

        private void MoveRelative(JobStep? step, int delta)
        {
            if (!CanMoveRelative(step, delta) || step == null) return;

            var idx = _steps.IndexOf(step);
            var newIdx = idx + delta;

            PushUndo();
            _steps.RemoveAt(idx);
            _steps.Insert(newIdx, step);

            JobValidation.RemoveInvalidSourceSelections(AllSteps());

            SelectedStep = step;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
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

        private void MoveToIndex(int from, int to)
        {
            if (from < 0 || from >= _steps.Count || to < 0 || to >= _steps.Count || from == to) return;
            if (WouldViolateIfStructure(from, to)) return;
            PushUndo();
            var step = _steps[from];
            _steps.RemoveAt(from);
            _steps.Insert(to, step);
            JobValidation.RemoveInvalidSourceSelections(AllSteps());
            SelectedStep = step;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
            ScheduleValidation();
        }

        private void MoveStep(StepDragDrop.MoveRequest request)
        {
            if (request.Source is not ObservableCollection<JobStep> source
                || request.Target is not ObservableCollection<JobStep> target
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

            PushUndo();
            for (int i = last; i >= first; i--)
                source.RemoveAt(i);
            for (int i = 0; i < moving.Count; i++)
                target.Insert(insertIndex + i, moving[i]);

            JobValidation.RemoveInvalidSourceSelections(AllSteps());

            SelectedStep = moving[0];
            _steps = target;
            SelectedSteps.Clear();
            SelectedSteps.AddRange(moving);
            HasUnsavedChanges = true;
            ExpandSection(target);
            InvalidateAllCommands();
            ScheduleValidation();
        }

        private bool CanMoveStepToSection(JobStep? step, ObservableCollection<JobStep> target)
            => step != null
               && FindSection(step) is { } source
               && !ReferenceEquals(source, target);

        private void MoveStepToSection(JobStep? step, ObservableCollection<JobStep> target)
        {
            if (step == null || FindSection(step) is not { } source || ReferenceEquals(source, target))
                return;

            MoveStep(new StepDragDrop.MoveRequest(
                source,
                source.IndexOf(step),
                target,
                target.Count));
        }

        private bool IsKnownSection(ObservableCollection<JobStep> section)
            => ReferenceEquals(section, _startSteps)
               || ReferenceEquals(section, _runSteps)
               || ReferenceEquals(section, _endSteps);

        private ObservableCollection<JobStep>? FindSection(JobStep step)
        {
            if (_startSteps.Contains(step)) return _startSteps;
            if (_runSteps.Contains(step)) return _runSteps;
            if (_endSteps.Contains(step)) return _endSteps;
            return null;
        }

        private List<JobStep> AllSteps()
            => _startSteps.Concat(_runSteps).Concat(_endSteps).ToList();

        private List<JobStep> GetPrecedingSteps(ObservableCollection<JobStep> section, int index)
        {
            IEnumerable<JobStep> precedingPhases = ReferenceEquals(section, _runSteps)
                ? _startSteps
                : ReferenceEquals(section, _endSteps)
                    ? _startSteps.Concat(_runSteps)
                    : [];
            return precedingPhases.Concat(section.Take(Math.Clamp(index, 0, section.Count))).ToList();
        }

        private void ExpandSection(ObservableCollection<JobStep> section)
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
            var startSnapshot = _startSteps.ToList();
            var snapshot = _runSteps.ToList();
            var endSnapshot = _endSteps.ToList();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(120, cts.Token);
                    var result = JobValidation.ValidateJob(new Job
                    {
                        StartSteps = startSnapshot,
                        Steps = snapshot,
                        EndSteps = endSnapshot
                    });
                    if (cts.IsCancellationRequested) return;
                    await Application.Current.Dispatcher.InvokeAsync(() => ApplyValidation(result));
                }
                catch (OperationCanceledException) { }
            });
        }

        private void ApplyValidation(JobValidationResult validation)
        {
            foreach (var result in validation.Steps)
            {
                result.Step.SetValidationResult(result.IsValid, result.Error);
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

            PushUndo();
            var idx = _steps.IndexOf(target);
            if (idx < 0) return;

            if (isIfOrEndIf)
            {
                int ifIdx    = target is TaskAutomation.Jobs.IfStep ? idx : FindOwningIfStep(idx);
                int endIfIdx = target is TaskAutomation.Jobs.EndIfStep ? idx : FindMatchingEndIf(idx);

                if (ifIdx >= 0 && endIfIdx > ifIdx)
                {
                    // Collect indices of If/ElseIf/Else/EndIf steps only — preserve regular steps inside.
                    var indicesToRemove = new List<int>();
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
                    // Remove from highest index downward to keep indices stable.
                    for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                        _steps.RemoveAt(indicesToRemove[i]);
                    SelectedStep = _steps.ElementAtOrDefault(Math.Max(0, ifIdx - 1));
                }
                else
                {
                    _steps.RemoveAt(idx);
                    SelectedStep = _steps.ElementAtOrDefault(Math.Max(0, idx - 1));
                }
            }
            else
            {
                var next = _steps.ElementAtOrDefault(Math.Max(0, idx - 1))
                          ?? _steps.ElementAtOrDefault(idx + 1);
                _steps.RemoveAt(idx);
                SelectedStep = next;
            }

            HasUnsavedChanges = true;
            InvalidateAllCommands();
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

        private JobStepsSnapshot CreateSnapshot()
            => new(
                DeepCloneSteps(_startSteps),
                DeepCloneSteps(_runSteps),
                DeepCloneSteps(_endSteps));

        private void RestoreSnapshot(JobStepsSnapshot snapshot)
        {
            _startSteps.Clear();
            foreach (var s in snapshot.StartSteps) _startSteps.Add(s);
            _runSteps.Clear();
            foreach (var s in snapshot.RunSteps) _runSteps.Add(s);
            _steps = _runSteps;
            _endSteps.Clear();
            foreach (var s in snapshot.EndSteps) _endSteps.Add(s);
            SelectedStep = null;
            SelectedSteps.Clear();
            HasUnsavedChanges = true;
        }

        // ---------- Copy / Paste ----------
        private void CopySelected()
        {
            var sources = SelectedSteps.Count > 0
                ? SelectedSteps.OrderBy(s => _steps.IndexOf(s)).ToList()
                : (SelectedStep != null ? new List<JobStep> { SelectedStep } : null);
            if (sources == null) return;
            _clipboard = DeepCloneSteps(sources, newIds: false);
            InvalidateAllCommands();
        }

        private void Paste()
        {
            if (_clipboard.Count == 0) return;

            int insertAt = SelectedStep != null
                ? Math.Min(_steps.Count, _steps.IndexOf(SelectedStep) + 1)
                : _steps.Count;

            // Clone again so multiple pastes produce independent copies with fresh IDs.
            var toInsert = DeepCloneSteps(_clipboard, newIds: true);
            PushUndo();
            for (int i = 0; i < toInsert.Count; i++)
                _steps.Insert(insertAt + i, toInsert[i]);

            SelectedStep = toInsert[^1];
            HasUnsavedChanges = true;
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

            PushUndo();

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

            int firstRemoved = indicesToRemove.Min;
            foreach (var i in indicesToRemove) _steps.RemoveAt(i);

            SelectedStep = _steps.ElementAtOrDefault(Math.Max(0, firstRemoved - 1));
            SelectedSteps.Clear();
            HasUnsavedChanges = true;
            InvalidateAllCommands();
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
        private bool WouldViolateIfStructure(int from, int to)
        {
            if (from == to) return false;
            var step = _steps[from];

            // Regular steps cannot break the control-flow structure.
            if (step is not (TaskAutomation.Jobs.IfStep     or
                             TaskAutomation.Jobs.ElseIfStep or
                             TaskAutomation.Jobs.ElseStep   or
                             TaskAutomation.Jobs.EndIfStep))
                return false;

            var sim = new System.Collections.Generic.List<JobStep>(_steps);
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

        private void AddElseIf(JobStep? step)
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
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id, AllSteps())
                { Mode = StepDialogMode.Add, IsTypeLocked = true };
            vm.SelectedType = "ElseIf";

            ShowDialogWithVm(vm, out bool? result);

            if (result == true && vm.CreatedStep != null)
            {
                _steps.Insert(insertIdx, vm.CreatedStep);
                SelectedStep = vm.CreatedStep;
                HasUnsavedChanges = true;
                InvalidateAllCommands();
            }
        }

        private void AddElse(JobStep? step)
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
            _steps.Insert(endIfIdx, new TaskAutomation.Jobs.ElseStep());
            HasUnsavedChanges = true;
            InvalidateAllCommands();
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
                foreach (var step in _startSteps.Concat(_runSteps).Concat(_endSteps))
                    step.PropertyChanged -= OnStepPropertyChanged;
                _validationCts?.Cancel();
                _validationCts?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ---------- Command invalidation helper ----------
        private void InvalidateAllCommands()
        {
            (SaveCommand          as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand        as RelayCommand)?.RaiseCanExecuteChanged();
            (StartJobCommand      as RelayCommand)?.RaiseCanExecuteChanged();
            (StopJobCommand       as RelayCommand)?.RaiseCanExecuteChanged();
            (EditStepCommand      as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveStepUpCommand    as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveStepDownCommand  as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (DeleteStepCommand    as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (UndoCommand          as RelayCommand)?.RaiseCanExecuteChanged();
            (RedoCommand          as RelayCommand)?.RaiseCanExecuteChanged();
            (CopyCommand          as RelayCommand)?.RaiseCanExecuteChanged();
            (PasteCommand         as RelayCommand)?.RaiseCanExecuteChanged();
            (AddElseIfCommand     as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (AddElseCommand       as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToStartSectionCommand as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToRunSectionCommand   as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
            (MoveToEndSectionCommand   as RelayCommand<JobStep?>)?.RaiseCanExecuteChanged();
        }

    }
}
