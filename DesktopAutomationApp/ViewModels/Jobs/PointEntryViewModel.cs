using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text.Json.Nodes;
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

        public ValueReferencePickerViewModel PointsSource { get; }

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
        public GeneratedStepFieldViewModel? ManualXField { get; private set; }
        public GeneratedStepFieldViewModel? ManualYField { get; private set; }
        public int ManualX
        {
            get => ManualXField?.IntegerValue ?? _manualX;
            set { Source = PointEntrySource.Manual; if (ManualXField is not null) ManualXField.IntegerValue = value; _manualX = value; OnChange(); }
        }

        private int _manualY;
        public int ManualY
        {
            get => ManualYField?.IntegerValue ?? _manualY;
            set { Source = PointEntrySource.Manual; if (ManualYField is not null) ManualYField.IntegerValue = value; _manualY = value; OnChange(); }
        }

        public ICommand RemoveCommand { get; }

        public PointEntryViewModel(
            ObservableCollection<PointEntryViewModel> owner,
            IReadOnlyList<SourceStepItem> detectionSteps,
            IReadOnlyList<JobVariable>? variables = null,
            IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
            ValueReferencePickerContext? pickerContext = null)
        {
            PointsSource = new ValueReferencePickerViewModel(detectionSteps,
                StepInputContractRegistry.Get(typeof(PointComparisonStep), "points")!, true,
                variables, providerSources, pickerContext);
            _source = PointEntrySource.Manual;
            RemoveCommand            = new RelayCommand(() => owner.Remove(this));
        }

        public PointEntry ToPointEntry() => new PointEntry
        {
            Source                = _source,
            ManualX               = ManualX,
            ManualY               = ManualY,
            PointsSource = PointsSource.ToBinding()
        };

        public void ConfigureNestedInputs(
            string keyPrefix,
            Func<string, TaskAutomation.Contracts.Steps.StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
        {
            ManualXField = CreateNestedField($"{keyPrefix}.manual_x", _manualX, resolver);
            ManualYField = CreateNestedField($"{keyPrefix}.manual_y", _manualY, resolver);
            OnChange(nameof(ManualXField));
            OnChange(nameof(ManualYField));
        }

        private static GeneratedStepFieldViewModel CreateNestedField(
            string key,
            int value,
            Func<string, TaskAutomation.Contracts.Steps.StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
        {
            var node = JsonValue.Create(value);
            var descriptor = new TaskAutomation.Contracts.Steps.StepFieldDescriptor(
                key, string.Empty, TaskAutomation.Contracts.Steps.StepValueKind.Integer, DefaultValue: node);
            return new GeneratedStepFieldViewModel(descriptor, node,
                inputReferenceEditor: resolver(key, TaskAutomation.Contracts.Steps.StepValueKind.Integer, node));
        }

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
