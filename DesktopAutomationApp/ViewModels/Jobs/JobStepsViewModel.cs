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
using Common.JsonRepository;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class JobStepsViewModel : ViewModelBase, INavigationGuard
    {
        private readonly IJobExecutor _jobExecutionContext;
        private readonly ObservableCollection<JobStep> _steps;
        private readonly IJsonRepository<Job> _jobRepo;
        private readonly IJobExecutor _executor;
        private readonly IJobDispatcher _dispatcher;

        private readonly Stack<List<JobStep>> _undoStack = new();
        private readonly Stack<List<JobStep>> _redoStack = new();
        private List<JobStep> _clipboard  = new();

        /// <summary>All currently selected steps (synced from the view's ListBox.SelectedItems).</summary>
        public List<JobStep> SelectedSteps { get; } = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public Job Job { get; }
        public string Title => Job.Name;

        public ObservableCollection<JobStep> Steps => _steps;

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

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RenameCommand { get; }
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

        public event Action? RequestBack;

        public JobStepsViewModel(Job job, IJobExecutor jobExecutionContext, IJobExecutor executor, IJsonRepository<Job> repo, IJobDispatcher dispatcher)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            _jobExecutionContext = jobExecutionContext;
            _executor = executor;
            _jobRepo = repo;
            _dispatcher = dispatcher;

            _steps = new ObservableCollection<JobStep>(Job.Steps ?? Enumerable.Empty<JobStep>());
            ResolveConditionDisplayNames();

            // Wenn sich die Step-Liste ändert (hinzufügen, löschen, verschieben),
            // muss die Steps-Property neu notifiziert werden, damit alle MultiBinding-
            // Konverter in der View (StepPrerequisiteStateConverter) neu ausgewertet werden.
            _steps.CollectionChanged += (_, _) =>
            {
                StepsVersion++;
                OnPropertyChanged(nameof(StepsVersion));
                ResolveConditionDisplayNames();
                InvalidateAllCommands();
            };

            BackCommand   = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand   = new RelayCommand(async () => await Save(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(DiscardChanges, () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(async () => await Rename());

            AddStepCommand    = new RelayCommand(async () => await AddStep());
            EditStepCommand   = new RelayCommand<JobStep?>(
                EditStep,
                s => { var t = s ?? SelectedStep; return t != null && t is not TaskAutomation.Jobs.ElseStep and not TaskAutomation.Jobs.EndIfStep; });
            MoveStepUpCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, -1), s => CanMoveRelative(s ?? SelectedStep, -1));
            MoveStepDownCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, +1), s => CanMoveRelative(s ?? SelectedStep, +1));
            ReorderStepCommand = new RelayCommand<(int from, int to)>(t => MoveToIndex(t.from, t.to));
            DeleteStepCommand    = new RelayCommand<JobStep?>(DeleteStep, s => (s ?? SelectedStep) != null);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedSteps.Count > 0 || SelectedStep != null);
            UndoCommand           = new RelayCommand(Undo, () => CanUndo);
            RedoCommand           = new RelayCommand(Redo, () => CanRedo);
            CopyCommand           = new RelayCommand(CopySelected, () => SelectedSteps.Count > 0 || SelectedStep != null);
            PasteCommand          = new RelayCommand(Paste, () => _clipboard.Count > 0);

            StartJobCommand = new RelayCommand(() => _dispatcher.StartJob(Job.Id), () => !IsJobRunning);
            StopJobCommand  = new RelayCommand(() => _dispatcher.CancelJob(Job.Id), () => IsJobRunning);

            AddElseIfCommand = new RelayCommand<JobStep?>(AddElseIf, CanAddElseIf);
            AddElseCommand   = new RelayCommand<JobStep?>(AddElse, CanAddElse);

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;
            IsJobRunning = _dispatcher.RunningJobIds.Contains(Job.Id);
        }

        // ---------- Selection sync (called from code-behind) ----------
        public void SetSelectedSteps(IEnumerable<object> items)
        {
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
            _steps.Clear();
            foreach (var s in Job.Steps ?? Enumerable.Empty<JobStep>())
                _steps.Add(s);
            HasUnsavedChanges = false;
        }

        // ---------- Save ----------
        private async Task Save()
        {
            Job.Steps = _steps.ToList();
            await _jobRepo.SaveAsync(Job);
            await _executor.ReloadJobsAsync();
            HasUnsavedChanges = false;
        }

        // ---------- Rename ----------
        private async Task Rename()
        {
            var dlg = new NewItemNameDialog("Umbenennen", "Neuer Name:", Job.Name)
                { Owner = Application.Current?.MainWindow };
            if (dlg.ShowDialog() != true) return;

            Job.Name = dlg.ResultName.Trim();
            OnPropertyChanged(nameof(Title));
            await _jobRepo.SaveAsync(Job);
            await _executor.ReloadJobsAsync();
        }

        // ---------- Add / Edit ----------
        private async Task AddStep()
        {
            // Determine insert position before opening the dialog so the
            // dialog receives the correct preceding-steps snapshot.
            int insertIndex = SelectedStep != null
                ? Math.Min(_steps.Count, _steps.IndexOf(SelectedStep) + 1)
                : _steps.Count;

            var precedingSteps = _steps.Take(insertIndex).ToList();
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id)
                { Mode = StepDialogMode.Add };

            ShowDialogWithVm(vm, out bool? result);

            if (result == true && vm.CreatedStep != null)
            {
                // Prevent nesting: IfStep cannot be inserted inside an existing block.
                if (vm.CreatedStep is TaskAutomation.Jobs.IfStep && CountOpenBlocksAt(insertIndex) > 0)
                {
                    MessageBox.Show(
                        "If-Abfragen können nicht verschachtelt werden.",
                        "Nicht erlaubt",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
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

            var idx = _steps.IndexOf(target);
            if (idx < 0) return;

            // Only steps before the edited one count as "preceding" for
            // prerequisite evaluation.
            var precedingSteps = _steps.Take(idx).ToList();
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id)
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
                    vm.TemplateMatchingStep_DrawResults = t.Settings.DrawResults;
                    vm.TemplateMatchingStep_SourceCaptureStep = vm.AvailableCaptureSteps.FirstOrDefault(s => s.StepId == t.Settings.SourceCaptureStepId);
                    break;

                case DesktopDuplicationStep d:
                    vm.SelectedType = "DesktopDuplication";
                    vm.DesktopDuplicationStep_DesktopIdx = d.Settings.DesktopIdx;
                    break;

                case ShowImageStep si:
                    vm.SelectedType = "ShowImage";
                    vm.ShowImageStep_WindowName = si.Settings.WindowName;
                    vm.ShowImageStep_ShowRawImage = si.Settings.ShowRawImage;
                    vm.ShowImageStep_ShowProcessedImage = si.Settings.ShowProcessedImage;
                    vm.ShowImageStep_SourceCaptureStep   = vm.AvailableCaptureSteps.FirstOrDefault(s => s.StepId == si.Settings.SourceCaptureStepId);
                    vm.ShowImageStep_SourceDetectionStep = vm.AvailableDetectionSteps.FirstOrDefault(s => s.StepId == si.Settings.SourceDetectionStepId);
                    break;

                case VideoCreationStep v:
                    vm.SelectedType = "VideoCreation";
                    vm.VideoCreationStep_SavePath = v.Settings.SavePath;
                    vm.VideoCreationStep_FileName = v.Settings.FileName;
                    vm.VideoCreationStep_UseRawImage = v.Settings.UseRawImage;
                    vm.VideoCreationStep_UseProcessedImage = v.Settings.UseProcessedImage;
                    vm.VideoCreationStep_SourceCaptureStep   = vm.AvailableCaptureSteps.FirstOrDefault(s => s.StepId == v.Settings.SourceCaptureStepId);
                    vm.VideoCreationStep_SourceDetectionStep = vm.AvailableDetectionSteps.FirstOrDefault(s => s.StepId == v.Settings.SourceDetectionStepId);
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
                    vm.ScriptExecutionStep_FireAndForget = se.Settings.FireAndForget;
                    break;

                case KlickOnPointStep kp:
                    vm.SelectedType = "KlickOnPoint";
                    vm.KlickOnPointStep_ClickType = kp.Settings.ClickType;
                    vm.KlickOnPointStep_DoubleClick = kp.Settings.DoubleClick;
                    vm.KlickOnPointStep_TimeoutMs = kp.Settings.TimeoutMs;
                    vm.KlickOnPointStep_SourceDetectionStep = vm.AvailableDetectionSteps.FirstOrDefault(s => s.StepId == kp.Settings.SourceDetectionStepId);
                    break;

                case KlickOnPoint3DStep kp3d:
                    vm.SelectedType = "KlickOnPoint3D";
                    vm.KlickOnPoint3DStep_FOV = kp3d.Settings.FOV;
                    vm.KlickOnPoint3DStep_MausSensitivityX = kp3d.Settings.MausSensitivityX;
                    vm.KlickOnPoint3DStep_MausSensitivityY = kp3d.Settings.MausSensitivityY;
                    vm.KlickOnPoint3DStep_DoubleClick = kp3d.Settings.DoubleClick;
                    vm.KlickOnPoint3DStep_ClickType = kp3d.Settings.ClickType;
                    vm.KlickOnPoint3DStep_Timeout = kp3d.Settings.TimeoutMs;
                    vm.KlickOnPoint3DStep_InvertMouseMovementY = kp3d.Settings.InvertMouseMovementY;
                    vm.KlickOnPoint3DStep_InvertMouseMovementX = kp3d.Settings.InvertMouseMovementX;
                    vm.KlickOnPoint3DStep_SourceDetectionStep = vm.AvailableDetectionSteps.FirstOrDefault(s => s.StepId == kp3d.Settings.SourceDetectionStepId);
                    vm.KlickOnPoint3DStep_SourceCaptureStep   = vm.AvailableCaptureSteps.FirstOrDefault(s => s.StepId == kp3d.Settings.SourceCaptureStepId);
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
                    vm.YoloDetectionStep_DrawResults = yd.Settings.DrawResults;
                    vm.YoloDetectionStep_EnableROI = yd.Settings.EnableROI;
                    vm.YoloDetectionStep_RoiX = yd.Settings.ROI.X;
                    vm.YoloDetectionStep_RoiY = yd.Settings.ROI.Y;
                    vm.YoloDetectionStep_RoiW = yd.Settings.ROI.Width;
                    vm.YoloDetectionStep_RoiH = yd.Settings.ROI.Height;
                    vm.YoloDetectionStep_SourceCaptureStep = vm.AvailableCaptureSteps.FirstOrDefault(s => s.StepId == yd.Settings.SourceCaptureStepId);
                    break;

                case TimeoutStep to:
                    vm.SelectedType = "Timeout";
                    vm.TimeoutStep_DelayMs = to.Settings.DelayMs;
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
            }
        }

        // ---------- Move / Delete ----------
        private bool CanMoveRelative(JobStep? step, int delta)
        {
            if (step == null) return false;
            var idx = _steps.IndexOf(step);
            if (idx < 0) return false;
            var newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= _steps.Count) return false;
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

            SelectedStep = step;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void MoveToIndex(int from, int to)
        {
            if (from < 0 || from >= _steps.Count || to < 0 || to >= _steps.Count || from == to) return;
            if (WouldViolateIfStructure(from, to)) return;
            PushUndo();
            var step = _steps[from];
            _steps.RemoveAt(from);
            _steps.Insert(to, step);
            SelectedStep = step;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void DeleteStep(JobStep? step)
        {
            var target = step ?? SelectedStep;
            if (target == null) return;

            bool isIfOrEndIf = target is TaskAutomation.Jobs.IfStep or TaskAutomation.Jobs.EndIfStep;
            string message = isIfOrEndIf
                ? "Möchten Sie den If-Block löschen (If, Else-If, Else und End-If werden entfernt, die Steps innerhalb bleiben erhalten)?"
                : "Möchten Sie den Step wirklich löschen?";

            var result = AppDialog.Show(message, "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

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
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsJobRunning = _dispatcher.RunningJobIds.Contains(Job.Id);
            });
        }

        // ---------- Undo / Redo ----------
        private void PushUndo()
        {
            _undoStack.Push(DeepCloneSteps(_steps));
            _redoStack.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;
            _redoStack.Push(DeepCloneSteps(_steps));
            RestoreSnapshot(_undoStack.Pop());
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Push(DeepCloneSteps(_steps));
            RestoreSnapshot(_redoStack.Pop());
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void RestoreSnapshot(List<JobStep> snapshot)
        {
            _steps.Clear();
            foreach (var s in snapshot) _steps.Add(s);
            SelectedStep = null;
            SelectedSteps.Clear();
            HasUnsavedChanges = true;
        }

        // ---------- Copy / Paste ----------
        private void CopySelected()
        {
            var sources = SelectedSteps.Count > 0 ? SelectedSteps : (SelectedStep != null ? new List<JobStep> { SelectedStep } : null);
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
        private void DeleteSelected()
        {
            var targets = SelectedSteps.Count > 0
                ? SelectedSteps.ToList()
                : (SelectedStep != null ? new List<JobStep> { SelectedStep } : null);
            if (targets == null || targets.Count == 0) return;

            string message = targets.Count == 1
                ? (targets[0] is TaskAutomation.Jobs.IfStep or TaskAutomation.Jobs.EndIfStep
                    ? "Möchten Sie den If-Block löschen (If, Else-If, Else und End-If werden entfernt, die Steps innerhalb bleiben erhalten)?"
                    : "Möchten Sie den Step wirklich löschen?")
                : $"Möchten Sie die {targets.Count} ausgewählten Steps löschen?";

            if (AppDialog.Show(message, "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
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
            return !IsValidIfStructure(sim);
        }

        /// <summary>
        /// Validates that every If-block in <paramref name="steps"/> obeys
        /// If → ElseIf* → Else? → EndIf ordering (no ElseIf after Else, no orphaned markers).
        /// </summary>
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

            var precedingSteps = _steps.Take(insertIdx).ToList();
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, precedingSteps, Job.Id)
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
                _dispatcher.RunningJobsChanged -= OnRunningJobsChanged;
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
        }

        // ---------- Display-Name-Auflösung für If/ElseIf-Bedingungen ----------
        /// <summary>
        /// Füllt <see cref="TaskAutomation.Jobs.StepCondition.SourceStepDisplayName"/> für alle
        /// If/ElseIf-Steps anhand der aktuellen Step-Liste. Wird nach dem Laden und nach jeder
        /// Listenänderung aufgerufen, da SourceStepDisplayName nicht serialisiert wird.
        /// </summary>
        private void ResolveConditionDisplayNames()
        {
            // Schnelle Lookup-Map: StepId → (friendlyName, 1-basierter Index)
            var idToName = new Dictionary<string, string>(_steps.Count, StringComparer.Ordinal);
            for (int i = 0; i < _steps.Count; i++)
            {
                var step = _steps[i];
                var friendly = TaskAutomation.Steps.StepResultMetadata.GetFriendlyName(step.GetType().Name);
                idToName[step.Id] = $"{friendly} (Step {i + 1})";
            }

            foreach (var step in _steps)
            {
                IEnumerable<TaskAutomation.Jobs.StepCondition>? conditions = step switch
                {
                    TaskAutomation.Jobs.IfStep     ifs  => ifs.Settings.Conditions,
                    TaskAutomation.Jobs.ElseIfStep eifs => eifs.Settings.Conditions,
                    _                                   => null
                };
                if (conditions is null) continue;

                foreach (var cond in conditions)
                    cond.SourceStepDisplayName = idToName.TryGetValue(cond.SourceStepId, out var name)
                        ? name
                        : cond.SourceStepId;
            }
        }
    }
}
