using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels
{
    /// <summary>
    /// ViewModel für eine einzelne Zeile in der Punkteliste des PointComparisonStep-Dialogs.
    /// </summary>
    public sealed class PointEntryViewModel : INotifyPropertyChanged
    {
        public IReadOnlyList<EditorChoiceOptionViewModel> SourceOptions { get; } =
        [
            new("Manual", Loc.Get("Ui.Step.Settings.Manual")),
            new("JobResult", Loc.Get("Ui.Step.Settings.FromDetectionResult"))
        ];
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChange([CallerMemberName] string? p = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public ResultBindingPickerViewModel PointsSource { get; }

        private PointEntrySource _source = PointEntrySource.Manual;

        public bool IsManual
        {
            get => _source == PointEntrySource.Manual;
            set { if (value) Source = PointEntrySource.Manual; }
        }

        public bool IsJobResult
        {
            get => _source == PointEntrySource.JobResult;
            set => Source = value ? PointEntrySource.JobResult : PointEntrySource.Manual;
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
                OnChange(nameof(SelectedSourceOption));
            }
        }

        public EditorChoiceOptionViewModel SelectedSourceOption
        {
            get => SourceOptions.First(option => option.Value == Source.ToString());
            set
            {
                if (value is not null && Enum.TryParse<PointEntrySource>(value.Value, out var source))
                    Source = source;
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

        public ICommand RemoveCommand { get; }

        public PointEntryViewModel(
            ObservableCollection<PointEntryViewModel> owner,
            IReadOnlyList<SourceStepItem> detectionSteps)
        {
            PointsSource = new ResultBindingPickerViewModel(detectionSteps,
                StepInputContractRegistry.Get(typeof(PointComparisonStep), "points")!, true);
            RemoveCommand            = new RelayCommand(() => owner.Remove(this));
        }

        public PointEntry ToPointEntry() => new PointEntry
        {
            Source                = _source,
            ManualX               = _manualX,
            ManualY               = _manualY,
            PointsSource = PointsSource.ToBinding()
        };

        public void LoadFrom(PointEntry e)
        {
            Source = e.Source;
            _manualX = e.ManualX;
            OnChange(nameof(ManualX));
            _manualY = e.ManualY;
            OnChange(nameof(ManualY));
            PointsSource.Load(e.PointsSource);
        }
    }
}
