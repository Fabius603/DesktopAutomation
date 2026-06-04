using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels
{
    /// <summary>
    /// ViewModel für eine einzelne Zeile in der Punkteliste des PointComparisonStep-Dialogs.
    /// </summary>
    public sealed class PointEntryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChange([CallerMemberName] string? p = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public IReadOnlyList<SourceStepItem> AvailableDetectionSteps { get; }

        private PointEntrySource _source = PointEntrySource.Manual;

        public bool IsManual
        {
            get => _source == PointEntrySource.Manual;
            set { if (value) Source = PointEntrySource.Manual; }
        }

        public bool IsJobResult
        {
            get => _source == PointEntrySource.JobResult;
            set { if (value) Source = PointEntrySource.JobResult; }
        }

        public PointEntrySource Source
        {
            get => _source;
            private set
            {
                _source = value;
                OnChange(nameof(IsManual));
                OnChange(nameof(IsJobResult));
                OnChange(nameof(ShowManual));
                OnChange(nameof(ShowJobResult));
            }
        }

        public bool ShowManual    => _source == PointEntrySource.Manual;
        public bool ShowJobResult => _source == PointEntrySource.JobResult;

        private int _manualX;
        public int ManualX
        {
            get => _manualX;
            set { _manualX = value; OnChange(); }
        }

        private int _manualY;
        public int ManualY
        {
            get => _manualY;
            set { _manualY = value; OnChange(); }
        }

        private SourceStepItem? _selectedDetectionStep;
        public SourceStepItem? SelectedDetectionStep
        {
            get => _selectedDetectionStep;
            set { _selectedDetectionStep = value; OnChange(); }
        }

        private bool _useAllDetections;
        public bool UseAllDetections
        {
            get => _useAllDetections;
            set { _useAllDetections = value; OnChange(); }
        }

        public ICommand RemoveCommand { get; }

        public PointEntryViewModel(
            ObservableCollection<PointEntryViewModel> owner,
            IReadOnlyList<SourceStepItem> detectionSteps)
        {
            AvailableDetectionSteps  = detectionSteps;
            SelectedDetectionStep    = AvailableDetectionSteps.FirstOrDefault();
            RemoveCommand            = new RelayCommand(() => owner.Remove(this));
        }

        public PointEntry ToPointEntry() => new PointEntry
        {
            Source                = _source,
            ManualX               = _manualX,
            ManualY               = _manualY,
            SourceDetectionStepId = _selectedDetectionStep?.StepId ?? "",
            UseAllDetections      = _useAllDetections
        };

        public void LoadFrom(PointEntry e)
        {
            Source = e.Source;
            _manualX = e.ManualX;
            OnChange(nameof(ManualX));
            _manualY = e.ManualY;
            OnChange(nameof(ManualY));
            _selectedDetectionStep = AvailableDetectionSteps
                .FirstOrDefault(s => s.StepId == e.SourceDetectionStepId);
            OnChange(nameof(SelectedDetectionStep));
            UseAllDetections = e.UseAllDetections;
        }
    }
}
