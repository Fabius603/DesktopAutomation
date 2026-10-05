using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Windows.Input;
using System.Windows.Media;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels;

public sealed class DetectionOverlayRowViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<DetectionOverlayRowViewModel> _owner;

    public DetectionOverlayRowViewModel(
        ObservableCollection<DetectionOverlayRowViewModel> owner,
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor inputContract,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
        ResultBinding? binding = null,
        ValueReferencePickerContext? pickerContext = null,
        ValueReferenceSourceCatalog? sourceCatalog = null)
    {
        _owner = owner;
        Source = sourceCatalog is null
            ? new ValueReferencePickerViewModel(
                sources, inputContract, false, variables, providerSources, pickerContext)
            : new ValueReferencePickerViewModel(sourceCatalog, inputContract, false, pickerContext);
        SourceField = new GeneratedStepFieldViewModel(
            new StepFieldDescriptor("detection_result", string.Empty, StepValueKind.ResultBinding,
                Required: true, AllowsDirectValue: false),
            null,
            inputReferenceEditor: new GeneratedResultBindingEditorViewModel(
                JsonValue.Create(string.Empty), Source));
        Source.ReferenceChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(Source)));
        if (binding is not null) Source.Load(binding);
        RemoveCommand = new RelayCommand(() => owner.Remove(this));
        MoveUpCommand = new RelayCommand(() => Move(-1));
        MoveDownCommand = new RelayCommand(() => Move(1));
    }

    public ValueReferencePickerViewModel Source { get; }
    public GeneratedStepFieldViewModel SourceField { get; }
    public ICommand RemoveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Move(int delta)
    {
        var index = _owner.IndexOf(this);
        var target = index + delta;
        if (index >= 0 && target >= 0 && target < _owner.Count)
            _owner.Move(index, target);
    }
}

public sealed class TextOverlayRowViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<TextOverlayRowViewModel> _owner;
    private Guid _id = Guid.NewGuid();
    private float _fontSize = 24f;
    private Color _fontColor = Colors.White;
    private string _fontColorToken = "#FFFFFF";
    private float _opacity = 1f;
    private int _desktopIndex;
    private int _offsetX;
    private int _offsetY;
    private int _durationMs = 5000;
    private bool _clearOnJobEnd = true;
    private readonly string _inputKeyPrefix;

    public TextOverlayRowViewModel(
        ObservableCollection<TextOverlayRowViewModel> owner,
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor inputContract,
        Action<TextOverlayRowViewModel>? chooseMonitor,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
        TextResultOverlaySettings? settings = null,
        ValueReferencePickerContext? pickerContext = null,
        string inputKeyPrefix = "overlay.text_results.0",
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null,
        ValueReferenceSourceCatalog? sourceCatalog = null)
    {
        _owner = owner;
        _inputKeyPrefix = inputKeyPrefix;
        Source = sourceCatalog is null
            ? new ValueReferencePickerViewModel(
                sources, inputContract, false, variables, providerSources, pickerContext)
            : new ValueReferencePickerViewModel(sourceCatalog, inputContract, false, pickerContext);
        Source.ReferenceChanged += (_, _) => OnChange(nameof(Source));
        if (settings is not null)
        {
            _id = settings.Id;
            _fontSize = settings.FontSize;
            _fontColor = ParseColor(settings.FontColor);
            _fontColorToken = settings.FontColor;
            _opacity = settings.Opacity;
            _desktopIndex = settings.DesktopIndex;
            _offsetX = settings.OffsetX;
            _offsetY = settings.OffsetY;
            _durationMs = settings.DurationMs;
            _clearOnJobEnd = settings.ClearOnJobEnd;
            Source.Load(settings.Result);
        }
        if (nestedInputResolver is not null)
        {
            TextSourceField = CreateNestedField("result", StepValueKind.Text,
                JsonValue.Create(string.Empty), null, nestedInputResolver);
            if (settings?.Result?.IsConfigured == true)
                TextSourceField.InputReferenceEditor!.Picker.Load(settings.Result);
            FontSizeField = CreateNestedField("font_size", StepValueKind.Number,
                JsonValue.Create(_fontSize), null, nestedInputResolver);
            FontColorField = CreateNestedField("font_color", StepValueKind.Color,
                JsonValue.Create(_fontColorToken), null, nestedInputResolver);
            OpacityField = CreateNestedField("opacity", StepValueKind.Number,
                JsonValue.Create(_opacity), StepEditorHints.Percentage, nestedInputResolver);
            DesktopIndexField = CreateNestedField("desktop_index", StepValueKind.Integer,
                JsonValue.Create(_desktopIndex), StepEditorHints.MonitorPicker, nestedInputResolver);
            OffsetXField = CreateNestedField("offset_x", StepValueKind.Integer,
                JsonValue.Create(_offsetX), null, nestedInputResolver);
            OffsetYField = CreateNestedField("offset_y", StepValueKind.Integer,
                JsonValue.Create(_offsetY), null, nestedInputResolver);
            DurationField = CreateNestedField("duration_ms", StepValueKind.Duration,
                JsonValue.Create(_durationMs), null, nestedInputResolver);
            ClearOnJobEndField = CreateNestedField("clear_on_job_end", StepValueKind.Boolean,
                JsonValue.Create(_clearOnJobEnd), null, nestedInputResolver);
            foreach (var field in NestedFields)
                field.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName is nameof(GeneratedStepFieldViewModel.InputText)
                        or nameof(GeneratedStepFieldViewModel.InputReferenceEditor))
                        OnChange();
                };
        }
        else
        {
            TextSourceField = new GeneratedStepFieldViewModel(
                new StepFieldDescriptor("text_result", string.Empty, StepValueKind.ResultBinding,
                    Required: true, AllowsDirectValue: false),
                null,
                inputReferenceEditor: new GeneratedResultBindingEditorViewModel(
                    JsonValue.Create(string.Empty), Source));
        }
        RemoveCommand = new RelayCommand(() => owner.Remove(this));
        MoveUpCommand = new RelayCommand(() => Move(-1));
        MoveDownCommand = new RelayCommand(() => Move(1));
        ChooseMonitorCommand = new RelayCommand(() => chooseMonitor?.Invoke(this));
    }

    public ValueReferencePickerViewModel Source { get; }
    public GeneratedStepFieldViewModel? TextSourceField { get; }
    public ICommand RemoveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand ChooseMonitorCommand { get; }
    public GeneratedStepFieldViewModel? FontSizeField { get; }
    public GeneratedStepFieldViewModel? FontColorField { get; }
    public GeneratedStepFieldViewModel? OpacityField { get; }
    public GeneratedStepFieldViewModel? DesktopIndexField { get; }
    public GeneratedStepFieldViewModel? OffsetXField { get; }
    public GeneratedStepFieldViewModel? OffsetYField { get; }
    public GeneratedStepFieldViewModel? DurationField { get; }
    public GeneratedStepFieldViewModel? ClearOnJobEndField { get; }
    private IEnumerable<GeneratedStepFieldViewModel> NestedFields =>
        new[] { TextSourceField, FontSizeField, FontColorField, OpacityField, DesktopIndexField,
            OffsetXField, OffsetYField, DurationField, ClearOnJobEndField }
            .OfType<GeneratedStepFieldViewModel>();
    public IReadOnlyDictionary<string, ResultBinding> InputBindings => NestedFields
        .Where(field => !ReferenceEquals(field, TextSourceField)).ToDictionary(
        field => field.Descriptor.Id, field => field.InputReferenceEditor!.Picker.ToBinding(), StringComparer.Ordinal);
    public float FontSize { get => (float)(FontSizeField?.NumberValue ?? _fontSize); set { if (FontSizeField is not null) { FontSizeField.NumberValue = value; _fontSize = value; } else if (_fontSize != value) { _fontSize = value; OnChange(); } } }
    public Color FontColor { get => FontColorField?.ColorValue ?? _fontColor; set { if (FontColorField is not null) { FontColorField.ColorValue = value; _fontColor = value; } else if (_fontColor != value || !WpfColorParser.TryParse(_fontColorToken, out _)) { _fontColor = value; _fontColorToken = value.ToString(); OnChange(); } } }
    public float Opacity { get => (float)(OpacityField?.NumberValue ?? _opacity); set { if (OpacityField is not null) { OpacityField.NumberValue = value; _opacity = value; } else if (_opacity != value) { _opacity = value; OnChange(); } } }
    public int DesktopIndex { get => DesktopIndexField?.IntegerValue ?? _desktopIndex; set { if (DesktopIndexField is not null) { DesktopIndexField.IntegerValue = value; _desktopIndex = value; } else if (_desktopIndex != value) { _desktopIndex = value; OnChange(); } } }
    public int OffsetX { get => OffsetXField?.IntegerValue ?? _offsetX; set { if (OffsetXField is not null) { OffsetXField.IntegerValue = value; _offsetX = value; } else if (_offsetX != value) { _offsetX = value; OnChange(); } } }
    public int OffsetY { get => OffsetYField?.IntegerValue ?? _offsetY; set { if (OffsetYField is not null) { OffsetYField.IntegerValue = value; _offsetY = value; } else if (_offsetY != value) { _offsetY = value; OnChange(); } } }
    public int DurationMs { get => DurationField?.IntegerValue ?? _durationMs; set { if (DurationField is not null) { DurationField.IntegerValue = value; _durationMs = value; } else if (_durationMs != value) { _durationMs = value; OnChange(); } } }
    public bool ClearOnJobEnd { get => ClearOnJobEndField?.BooleanValue ?? _clearOnJobEnd; set { if (ClearOnJobEndField is not null) { ClearOnJobEndField.BooleanValue = value; _clearOnJobEnd = value; } else if (_clearOnJobEnd != value) { _clearOnJobEnd = value; OnChange(); } } }

    public TextResultOverlaySettings ToSettings() => new()
    {
        Id = _id,
        Result = TextSourceField?.InputReferenceEditor?.Picker.ToBinding() ?? Source.ToBinding(),
        FontSize = FontSize,
        FontColor = FontColorField?.InputText ?? _fontColorToken,
        Opacity = Opacity,
        DesktopIndex = DesktopIndex,
        OffsetX = OffsetX,
        OffsetY = OffsetY,
        DurationMs = DurationMs,
        ClearOnJobEnd = ClearOnJobEnd
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChange([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new(name));

    private void Move(int delta)
    {
        var index = _owner.IndexOf(this);
        var target = index + delta;
        if (index >= 0 && target >= 0 && target < _owner.Count)
            _owner.Move(index, target);
    }

    private static Color ParseColor(string value)
        => WpfColorParser.TryParse(value, out var color) ? color : Colors.White;

    private GeneratedStepFieldViewModel CreateNestedField(
        string memberId,
        StepValueKind kind,
        JsonNode? value,
        string? editorHint,
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
    {
        var key = $"{_inputKeyPrefix}.{memberId}";
        var descriptor = new StepFieldDescriptor(key, string.Empty, kind,
            DefaultValue: value?.DeepClone(), EditorHint: editorHint);
        return new GeneratedStepFieldViewModel(descriptor, value?.DeepClone(),
            inputReferenceEditor: resolver(key, kind, value));
    }
}
