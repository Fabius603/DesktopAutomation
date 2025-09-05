using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using OpenCvSharp;
using TaskAutomation.Jobs;
using DesktopAutomationApp.Views;
using System.CodeDom.Compiler;
using Common.JsonRepository;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class JobStepsViewModel : ViewModelBase
    {
        private readonly IJobExecutor _jobExecutionContext;
        private readonly ObservableCollection<JobStep> _steps;
        private readonly IJsonRepository<Job> _jobRepo;
        private readonly IJobExecutor _executor;

        public Job Job { get; }
        public string Title => Job.Name;

        public ObservableCollection<JobStep> Steps => _steps;

        private JobStep? _selectedStep;
        public JobStep? SelectedStep
        {
            get => _selectedStep;
            set { _selectedStep = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AddStepCommand { get; }
        public ICommand EditStepCommand { get; }
        public ICommand MoveStepUpCommand { get; }
        public ICommand MoveStepDownCommand { get; }
        public ICommand DeleteStepCommand { get; }

        public event Action? RequestBack;

        public JobStepsViewModel(Job job, IJobExecutor jobExecutionContext, IJobExecutor executor, IJsonRepository<Job> repo)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            _jobExecutionContext = jobExecutionContext;
            _executor = executor;
            _jobRepo = repo;

            _steps = new ObservableCollection<JobStep>(Job.Steps ?? Enumerable.Empty<JobStep>());

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand = new RelayCommand(async () => await Save(), () => true);

            AddStepCommand = new RelayCommand(async () => await AddStep());
            EditStepCommand = new RelayCommand<JobStep?>(EditStep, s => s != null || SelectedStep != null);
            MoveStepUpCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, -1), s => CanMoveRelative(s ?? SelectedStep, -1));
            MoveStepDownCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, +1), s => CanMoveRelative(s ?? SelectedStep, +1));
            DeleteStepCommand = new RelayCommand<JobStep?>(DeleteStep, s => (s ?? SelectedStep) != null);
            _executor = executor;
        }

        private async Task Save()
        {
            Job.Steps = _steps.ToList();
            await _jobRepo.SaveAsync(Job);
            await _executor.ReloadJobsAsync();
        }

        private async Task AddStep()
        {
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, Job) { Mode = StepDialogMode.Add };

            bool? result;
            ShowDialogWithVm(vm, out result);

            if (result == true && vm.CreatedStep != null)
            {
                int insertIndex = SelectedStep != null
                    ? Math.Min(_steps.Count, _steps.IndexOf(SelectedStep) + 1)
                    : _steps.Count;

                _steps.Insert(insertIndex, vm.CreatedStep);
                SelectedStep = vm.CreatedStep;
                
                // Automatisch speichern nach Hinzufügen eines neuen Steps
                await Save();
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

        private async void EditStep(JobStep? step = null)
        {
            var target = step ?? SelectedStep;
            if (target == null) return;

            var vm = new AddJobStepDialogViewModel(_jobExecutionContext, Job) { Mode = StepDialogMode.Edit };

            // <<<<<< Prefill hier im ViewModel >>>>>>
            Prefill(vm, target);

            bool? result;
            ShowDialogWithVm(vm, out result);

            if (result != true || vm.CreatedStep == null) return;

            var idx = _steps.IndexOf(target);
            if (idx < 0) return;

            _steps[idx] = vm.CreatedStep;
            SelectedStep = vm.CreatedStep;
            
            // Automatisch speichern nach Bearbeiten
            await Save();
        }

        // ---------- Prefill: setzt alle benötigten Eigenschaften im Dialog-VM ----------
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

                //case ProcessDuplicationStep p:
                //    vm.SelectedType = "ProcessDuplication";
                //    vm.ProcessName = p.Settings.ProcessName;
                //    break;

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
                    vm.MakroExecutionStep_SelectedMakroName = me.Settings.MakroName;
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

                case JobExecutionStep je:
                    vm.SelectedType = "JobExecution";
                    vm.JobExecutionStep_SelectedJobName = je.Settings.JobName;
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
            }
        }

        // ---------- Move/Delete ----------
        private bool CanMoveRelative(JobStep? step, int delta)
        {
            if (step == null) return false;
            var idx = _steps.IndexOf(step);
            if (idx < 0) return false;
            var newIdx = idx + delta;
            return newIdx >= 0 && newIdx < _steps.Count;
        }

        private async void MoveRelative(JobStep? step, int delta)
        {
            if (!CanMoveRelative(step, delta) || step == null) return;

            var idx = _steps.IndexOf(step);
            var newIdx = idx + delta;

            _steps.RemoveAt(idx);
            _steps.Insert(newIdx, step);

            SelectedStep = step;
            CommandManager.InvalidateRequerySuggested();
            
            // Automatisch speichern nach Verschieben
            await Save();
        }

        private async void DeleteStep(JobStep? step)
        {
            var target = step ?? SelectedStep;
            if (target == null) return;

            var result = MessageBox.Show(
                $"Möchten Sie den Makro „{target}“ wirklich löschen?",
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
            CommandManager.InvalidateRequerySuggested();
            
            // Automatisch speichern nach Löschen
            await Save();
        }
    }
}
