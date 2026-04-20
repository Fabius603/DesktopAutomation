using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
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

        public Job Job { get; }
        public string Title => Job.Name;

        public ObservableCollection<JobStep> Steps => _steps;

        private JobStep? _selectedStep;
        public JobStep? SelectedStep
        {
            get => _selectedStep;
            set { _selectedStep = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set { _hasUnsavedChanges = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        private bool _isJobRunning;
        public bool IsJobRunning
        {
            get => _isJobRunning;
            private set { _isJobRunning = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
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
        public ICommand StartJobCommand { get; }
        public ICommand StopJobCommand { get; }

        public event Action? RequestBack;

        public JobStepsViewModel(Job job, IJobExecutor jobExecutionContext, IJobExecutor executor, IJsonRepository<Job> repo, IJobDispatcher dispatcher)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            _jobExecutionContext = jobExecutionContext;
            _executor = executor;
            _jobRepo = repo;
            _dispatcher = dispatcher;

            _steps = new ObservableCollection<JobStep>(Job.Steps ?? Enumerable.Empty<JobStep>());

            // Wenn sich die Step-Liste ändert (hinzufügen, löschen, verschieben),
            // muss die Steps-Property neu notifiziert werden, damit alle MultiBinding-
            // Konverter in der View (StepPrerequisiteStateConverter) neu ausgewertet werden.
            _steps.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Steps));

            BackCommand   = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand   = new RelayCommand(async () => await Save(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(DiscardChanges, () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(async () => await Rename());

            AddStepCommand    = new RelayCommand(async () => await AddStep());
            EditStepCommand   = new RelayCommand<JobStep?>(EditStep, s => s != null || SelectedStep != null);
            MoveStepUpCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, -1), s => CanMoveRelative(s ?? SelectedStep, -1));
            MoveStepDownCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, +1), s => CanMoveRelative(s ?? SelectedStep, +1));
            ReorderStepCommand = new RelayCommand<(int from, int to)>(t => MoveToIndex(t.from, t.to));
            DeleteStepCommand = new RelayCommand<JobStep?>(DeleteStep, s => (s ?? SelectedStep) != null);

            StartJobCommand = new RelayCommand(() => _dispatcher.StartJob(Job.Id), () => !IsJobRunning);
            StopJobCommand  = new RelayCommand(() => _dispatcher.CancelJob(Job.Id), () => IsJobRunning);

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;
            IsJobRunning = _dispatcher.RunningJobIds.Contains(Job.Id);
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
            var errors = StepPipelineRegistry.ValidateStepChain(_steps);
            if (errors.Count > 0)
            {
                var msg = "Der Job kann nicht gespeichert werden, da folgende Voraussetzungen nicht erfüllt sind:\n\n"
                    + string.Join("\n", errors.Select(e =>
                        $"  • Step {e.StepIndex + 1} ({e.StepTypeName}): benötigt \u201e{e.MissingPrerequisite}\u201c"));
                AppDialog.Show(msg, "Ungültige Pipeline", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                _steps.Insert(insertIndex, vm.CreatedStep);
                SelectedStep = vm.CreatedStep;
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
                { Mode = StepDialogMode.Edit };
            Prefill(vm, target);

            ShowDialogWithVm(vm, out bool? result);

            if (result != true || vm.CreatedStep == null) return;

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
                    break;

                case VideoCreationStep v:
                    vm.SelectedType = "VideoCreation";
                    vm.VideoCreationStep_SavePath = v.Settings.SavePath;
                    vm.VideoCreationStep_FileName = v.Settings.FileName;
                    vm.VideoCreationStep_UseRawImage = v.Settings.UseRawImage;
                    vm.VideoCreationStep_UseProcessedImage = v.Settings.UseProcessedImage;
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
                    break;

                case TimeoutStep to:
                    vm.SelectedType = "Timeout";
                    vm.TimeoutStep_DelayMs = to.Settings.DelayMs;
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
            return newIdx >= 0 && newIdx < _steps.Count;
        }

        private void MoveRelative(JobStep? step, int delta)
        {
            if (!CanMoveRelative(step, delta) || step == null) return;

            var idx = _steps.IndexOf(step);
            var newIdx = idx + delta;

            _steps.RemoveAt(idx);
            _steps.Insert(newIdx, step);

            SelectedStep = step;
            HasUnsavedChanges = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private void MoveToIndex(int from, int to)
        {
            if (from < 0 || from >= _steps.Count || to < 0 || to >= _steps.Count || from == to) return;
            var step = _steps[from];
            _steps.RemoveAt(from);
            _steps.Insert(to, step);
            SelectedStep = step;
            HasUnsavedChanges = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private void DeleteStep(JobStep? step)
        {
            var target = step ?? SelectedStep;
            if (target == null) return;

            var result = AppDialog.Show(
                $"Möchten Sie den Step wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var idx = _steps.IndexOf(target);
            if (idx < 0) return;

            var next = _steps.ElementAtOrDefault(Math.Max(0, idx - 1))
                      ?? _steps.ElementAtOrDefault(idx + 1);

            _steps.RemoveAt(idx);
            SelectedStep = next;
            HasUnsavedChanges = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnRunningJobsChanged()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsJobRunning = _dispatcher.RunningJobIds.Contains(Job.Id);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _dispatcher.RunningJobsChanged -= OnRunningJobsChanged;
            base.Dispose(disposing);
        }
    }
}
