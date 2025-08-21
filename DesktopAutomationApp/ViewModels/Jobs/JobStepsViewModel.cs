using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using OpenCvSharp;
using TaskAutomation.Jobs;
using DesktopAutomationApp.Views;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class JobStepsViewModel : ViewModelBase
    {
        private readonly IJobExecutionContext _jobExecutionContext;
        private readonly ObservableCollection<JobStep> _steps;

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

        public JobStepsViewModel(Job job, IJobExecutionContext jobExecutionContext)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            _jobExecutionContext = jobExecutionContext;

            _steps = new ObservableCollection<JobStep>(Job.Steps ?? Enumerable.Empty<JobStep>());

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand = new RelayCommand(Save, () => true);

            AddStepCommand = new RelayCommand(AddStep);
            EditStepCommand = new RelayCommand<JobStep?>(EditStep, s => s != null || SelectedStep != null);
            MoveStepUpCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, -1), s => CanMoveRelative(s ?? SelectedStep, -1));
            MoveStepDownCommand = new RelayCommand<JobStep?>(s => MoveRelative(s ?? SelectedStep, +1), s => CanMoveRelative(s ?? SelectedStep, +1));
            DeleteStepCommand = new RelayCommand<JobStep?>(DeleteStep, s => (s ?? SelectedStep) != null);
        }

        private void Save()
        {
            Job.Steps = _steps.ToList();
            // Persistenz folgt später (Repo etc.)
        }

        private void AddStep()
        {
            var vm = new AddJobStepDialogViewModel(_jobExecutionContext) { Mode = StepDialogMode.Add };

            var dlg = new AddJobStepDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = vm
            };

            var result = dlg.ShowDialog();
            if (result == true && vm.CreatedStep != null)
            {
                int insertIndex = SelectedStep != null
                    ? Math.Min(_steps.Count, _steps.IndexOf(SelectedStep) + 1)
                    : _steps.Count;

                _steps.Insert(insertIndex, vm.CreatedStep);
                SelectedStep = vm.CreatedStep;
            }
        }

        private void EditStep(JobStep? step = null)
        {
            var target = step ?? SelectedStep;
            if (target == null) return;

            var vm = new AddJobStepDialogViewModel(_jobExecutionContext) { Mode = StepDialogMode.Edit };

            // <<<<<< Prefill hier im ViewModel >>>>>>
            Prefill(vm, target);

            var dlg = new AddJobStepDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = vm
            };

            var ok = dlg.ShowDialog() == true;
            if (!ok || vm.CreatedStep == null) return;

            var idx = _steps.IndexOf(target);
            if (idx < 0) return;

            _steps[idx] = vm.CreatedStep;
            SelectedStep = vm.CreatedStep;
        }

        // ---------- Prefill: setzt alle benötigten Eigenschaften im Dialog-VM ----------
        private static void Prefill(AddJobStepDialogViewModel vm, JobStep s)
        {
            switch (s)
            {
                case TemplateMatchingStep t:
                    vm.SelectedType = "TemplateMatching";
                    vm.TemplatePath = t.Settings.TemplatePath;
                    vm.TemplateMatchMode = t.Settings.TemplateMatchMode;
                    vm.MultiplePoints = t.Settings.MultiplePoints;
                    vm.ConfidenceThreshold = t.Settings.ConfidenceThreshold;
                    vm.EnableROI = t.Settings.EnableROI;
                    vm.RoiX = t.Settings.ROI.X;
                    vm.RoiY = t.Settings.ROI.Y;
                    vm.RoiW = t.Settings.ROI.Width;
                    vm.RoiH = t.Settings.ROI.Height;
                    vm.DrawResults = t.Settings.DrawResults;
                    break;

                case DesktopDuplicationStep d:
                    vm.SelectedType = "DesktopDuplication";
                    vm.DesktopIdx = d.Settings.DesktopIdx;
                    break;

                case ProcessDuplicationStep p:
                    vm.SelectedType = "ProcessDuplication";
                    vm.ProcessName = p.Settings.ProcessName;
                    break;

                case ShowImageStep si:
                    vm.SelectedType = "ShowImage";
                    vm.WindowName = si.Settings.WindowName;
                    vm.ShowRawImage = si.Settings.ShowRawImage;
                    vm.ShowProcessedImage = si.Settings.ShowProcessedImage;
                    break;

                case VideoCreationStep v:
                    vm.SelectedType = "VideoCreation";
                    vm.SavePath = v.Settings.SavePath;
                    vm.FileName = v.Settings.FileName;
                    vm.UseRawImage = v.Settings.UseRawImage;
                    vm.UseProcessedImage = v.Settings.UseProcessedImage;
                    break;

                case MakroExecutionStep me:
                    vm.SelectedType = "MakroExecution";
                    vm.SelectedMakroName = me.Settings.MakroName;
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

        private void MoveRelative(JobStep? step, int delta)
        {
            if (!CanMoveRelative(step, delta) || step == null) return;

            var idx = _steps.IndexOf(step);
            var newIdx = idx + delta;

            _steps.RemoveAt(idx);
            _steps.Insert(newIdx, step);

            SelectedStep = step;
            CommandManager.InvalidateRequerySuggested();
        }

        private void DeleteStep(JobStep? step)
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
        }
    }
}
