using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text.Json.Nodes;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;
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
        public GeneratedWholeValueSourceViewModel WholeValueSource { get; }

        private PointEntrySource _source = PointEntrySource.Manual;
        private string? _unknownSource;
        public string SourceToken => _unknownSource ?? Source.ToString();
        public bool HasInvalidToken => _unknownSource is not null;
        public string InvalidTokenMessage => HasInvalidToken
            ? Loc.Format("Ui.Step.Generated.Validation.UnknownSavedToken", _unknownSource) : string.Empty;

        public void LoadSourceToken(string token)
        {
            _unknownSource = StepEnumRules.TryRead<PointEntrySource>(token, out var source) ? null : token;
            Source = _unknownSource is null ? source : (PointEntrySource)(-1);
            OnChange(nameof(HasInvalidToken)); OnChange(nameof(InvalidTokenMessage));
        }

        public bool IsManual
        {
            get => _source == PointEntrySource.Manual;
            set { if (value) WholeValueSource.UsesReference = false; }
        }

        public bool IsJobResult
        {
            get => _source == PointEntrySource.JobResult;
            set => WholeValueSource.UsesReference = value;
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

        public EditorChoiceOptionViewModel? SelectedSourceOption
        {
            get => SourceOptions.FirstOrDefault(option => option.Value == SourceToken);
            set
            {
                if (value is not null && StepEnumRules.TryRead<PointEntrySource>(value.Value, out var source))
                {
                    LoadSourceToken(value.Value);
                    WholeValueSource.UsesReference = source != PointEntrySource.Manual;
                }
            }
        }

        public bool ShowManual => _source == PointEntrySource.Manual;
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
        public ICommand? DuplicateCommand { get; set; }

        public PointEntryViewModel(
            ObservableCollection<PointEntryViewModel> owner,
            IReadOnlyList<SourceStepItem> detectionSteps,
            IReadOnlyList<JobVariable>? variables = null,
            IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
            ValueReferencePickerContext? pickerContext = null,
            ValueReferenceSourceCatalog? sourceCatalog = null)
        {
            var contract = StepInputContractRegistry.Get(typeof(PointComparisonStep), "points")!;
            PointsSource = sourceCatalog is null
                ? new ValueReferencePickerViewModel(
                    detectionSteps, contract, true, variables, providerSources, pickerContext)
                : new ValueReferencePickerViewModel(sourceCatalog, contract, true, pickerContext);
            WholeValueSource = new GeneratedWholeValueSourceViewModel(PointsSource, false);
            WholeValueSource.Changed += () =>
            {
                _unknownSource = null;
                Source = WholeValueSource.UsesReference
                    ? PointEntrySource.JobResult
                    : PointEntrySource.Manual;
                OnChange(string.Empty);
            };
            _source = PointEntrySource.Manual;
            RemoveCommand = new RelayCommand(() => owner.Remove(this));
        }

        public PointEntry ToPointEntry() => new PointEntry
        {
            Source = WholeValueSource.UsesReference ? PointEntrySource.JobResult : PointEntrySource.Manual,
            ManualX = ManualX,
            ManualY = ManualY,
            PointsSource = WholeValueSource.ToBinding()
        };

        public void ConfigureNestedInputs(
            string keyPrefix,
            Func<string, TaskAutomation.Contracts.Steps.StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
        {
            ManualXField = CreateNestedField($"{keyPrefix}.manual_x", _manualX, resolver);
            ManualYField = CreateNestedField($"{keyPrefix}.manual_y", _manualY, resolver);
            ManualXField.PropertyChanged += (_, args) => ClearWholeValueForSemanticFieldChange(args);
            ManualYField.PropertyChanged += (_, args) => ClearWholeValueForSemanticFieldChange(args);
            OnChange(nameof(ManualXField));
            OnChange(nameof(ManualYField));
        }

        private void ClearWholeValueForSemanticFieldChange(PropertyChangedEventArgs args)
        {
            if (args.PropertyName is nameof(GeneratedStepFieldViewModel.InputText)
                or nameof(GeneratedStepFieldViewModel.InputReferenceEditor))
                WholeValueSource.UsesReference = false;
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
            WholeValueSource.Load(e.PointsSource, e.Source != PointEntrySource.Manual);
            Source = WholeValueSource.UsesReference ? PointEntrySource.JobResult : PointEntrySource.Manual;
            _manualX = e.ManualX;
            OnChange(nameof(ManualX));
            _manualY = e.ManualY;
            OnChange(nameof(ManualY));
        }
    }
}
