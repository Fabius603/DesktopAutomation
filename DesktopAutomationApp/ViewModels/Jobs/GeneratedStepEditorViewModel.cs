using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels.WindowsIntegration;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.WindowsIntegration;

namespace DesktopAutomationApp.ViewModels;

public sealed class GeneratedStepEditorViewModel : INotifyPropertyChanged
{
    private readonly IStepDefinition _definition;
    private readonly StepDraft _baseDraft;
    private readonly JobStep? _existingStep;
    private readonly IReadOnlySet<string> _editableFieldIds;
    private string? _validationError;

    public GeneratedStepEditorViewModel(
        IStepDefinition definition,
        JobStep? step = null,
        Func<StepFieldDescriptor, IEnumerable<string>?>? suggestionResolver = null,
        Func<StepFieldDescriptor, IEnumerable<GeneratedStepChoiceOptionViewModel>?>? choiceResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedProcessTargetEditorViewModel?>? processTargetResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedResultBindingEditorViewModel?>? resultBindingResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedCameraEditorViewModel?>? cameraResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedVisualOverlayEditorViewModel?>? visualOverlayResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedRoiEditorViewModel?>? roiResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedYoloEditorViewModel?>? yoloResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedConditionEditorViewModel?>? conditionResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedWindowsCapabilityEditorViewModel?>? windowsCapabilityResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedScreenPointEditorViewModel?>? screenPointResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedUserChoiceOptionsEditorViewModel?>? userChoiceOptionsResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedPointEntryListEditorViewModel?>? pointEntryListResolver = null,
        Func<StepFieldDescriptor, JsonNode?, GeneratedAxisExpressionListEditorViewModel?>? axisExpressionListResolver = null)
    {
        _definition = definition;
        _existingStep = step;
        _baseDraft = definition.CreateDraft(step);
        _editableFieldIds = definition.Descriptor.Presentation.EditorSections
            .SelectMany(section => section.FieldIds)
            .ToHashSet(StringComparer.Ordinal);
        Fields = new ObservableCollection<GeneratedStepFieldViewModel>(
            definition.Descriptor.Fields
                .OrderBy(field => field.Order)
                .Select(field => new GeneratedStepFieldViewModel(
                    field,
                    _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue,
                    suggestionResolver?.Invoke(field),
                    choiceResolver?.Invoke(field),
                    processTargetResolver?.Invoke(
                        field,
                        _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    resultBindingResolver?.Invoke(
                        field,
                        _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    cameraResolver?.Invoke(
                        field,
                        _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    visualOverlayResolver?.Invoke(
                        field,
                        _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    roiResolver?.Invoke(
                        field,
                        _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    yoloResolver?.Invoke(
                        field,
                        _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    conditionResolver?.Invoke(
                        field,
                        _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    windowsCapabilityResolver?.Invoke(
                        field,
                        _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    screenPointResolver?.Invoke(field, _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    userChoiceOptionsResolver?.Invoke(field, _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    pointEntryListResolver?.Invoke(field, _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue),
                    axisExpressionListResolver?.Invoke(field, _baseDraft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue))));
        foreach (var field in Fields.Where(field => field.YoloEditor is not null))
        {
            field.YoloEditor!.RecommendedConfidenceChanged += confidence =>
            {
                var targetId = field.Descriptor.YoloPickerOptions?.RecommendedConfidenceTargetFieldId;
                var target = Fields.FirstOrDefault(candidate => candidate.Descriptor.Id == targetId);
                if (target is not null)
                    target.NumberValue = confidence;
            };
        }
        foreach (var field in Fields.Where(field => _editableFieldIds.Contains(field.Descriptor.Id)))
            field.PropertyChanged += OnFieldChanged;
        RefreshVisibility();
        var fieldsById = Fields.ToDictionary(field => field.Descriptor.Id, StringComparer.Ordinal);
        Sections = new ObservableCollection<GeneratedStepEditorSectionViewModel>(
            definition.Descriptor.Presentation.EditorSections
                .OrderBy(section => section.Order)
                .Select(section => new GeneratedStepEditorSectionViewModel(
                    section,
                    section.FieldIds.Select(fieldId => fieldsById[fieldId]).ToArray())));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    public StepDescriptor Descriptor => _definition.Descriptor;
    public ObservableCollection<GeneratedStepFieldViewModel> Fields { get; }
    public ObservableCollection<GeneratedStepEditorSectionViewModel> Sections { get; }
    public string EditorDescription => string.IsNullOrWhiteSpace(Descriptor.Presentation.EditorDescriptionKey)
        ? string.Empty
        : Loc.Get(Descriptor.Presentation.EditorDescriptionKey);
    public bool HasEditorDescription => !string.IsNullOrWhiteSpace(EditorDescription);
    public string? ValidationError
    {
        get => _validationError;
        private set
        {
            if (_validationError == value) return;
            _validationError = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationError)));
        }
    }

    public bool TryCreateStep(out JobStep? step)
    {
        var draft = _baseDraft.Clone();
        foreach (var field in Fields.Where(field => _editableFieldIds.Contains(field.Descriptor.Id)))
        {
            if (!field.TryWriteValue(draft, out var inputError))
            {
                ValidationError = inputError;
                step = null;
                return false;
            }
        }

        var issue = _definition.ValidateDraft(draft)
            .FirstOrDefault(candidate => candidate.Severity == StepValidationSeverity.Error);
        if (issue is not null)
        {
            ValidationError = FormatIssue(issue);
            step = null;
            return false;
        }

        ValidationError = null;
        step = _definition.ApplyDraft(draft);
        if (_existingStep is not null)
        {
            step.Id = _existingStep.Id;
            step.IsEnabled = _existingStep.IsEnabled;
            step.IsBreakpoint = _existingStep.IsBreakpoint;
        }
        return true;
    }

    private void OnFieldChanged(object? sender, PropertyChangedEventArgs e)
    {
        ValidationError = null;
        RefreshVisibility();
        Changed?.Invoke();
    }

    private void RefreshVisibility()
    {
        var fieldsById = Fields.ToDictionary(field => field.Descriptor.Id, StringComparer.Ordinal);
        foreach (var field in Fields)
        {
            var rule = field.Descriptor.VisibleWhen;
            var visible = rule is null || RuleMatches(rule, fieldsById);
            if (visible && field.Descriptor.VisibleWhenAll is { Count: > 0 } rules)
                visible = rules.All(candidate => RuleMatches(candidate, fieldsById));
            field.SetVisibility(visible);
        }
    }

    private static bool RuleMatches(
        StepVisibilityRule rule,
        IReadOnlyDictionary<string, GeneratedStepFieldViewModel> fieldsById) =>
        fieldsById.TryGetValue(rule.FieldId, out var source)
        && (rule.AnyOfValues is { Count: > 0 }
            ? rule.AnyOfValues.Any(source.ValueEquals)
            : source.ValueEquals(rule.EqualsValue));

    private string FormatIssue(StepValidationIssue issue)
    {
        var field = Fields.FirstOrDefault(candidate => candidate.Descriptor.Id == issue.FieldId);
        var label = field?.Label ?? issue.FieldId ?? Descriptor.DisplayNameKey;
        return issue.Code switch
        {
            "StepValidation.Required" => Loc.Format("Ui.Step.Generated.Validation.Required", label),
            "StepValidation.Integer" => Loc.Format("Ui.Step.Generated.Validation.Integer", label),
            "StepValidation.Boolean" => Loc.Format("Ui.Step.Generated.Validation.Boolean", label),
            "StepValidation.Minimum" => Loc.Format(
                "Ui.Step.Generated.Validation.Minimum",
                label,
                issue.Arguments?.GetValueOrDefault("minimum") ?? 0),
            "StepValidation.Maximum" => Loc.Format(
                "Ui.Step.Generated.Validation.Maximum",
                label,
                issue.Arguments?.GetValueOrDefault("maximum") ?? 0),
            _ => Loc.Get("Ui.Step.Generated.Validation.Invalid")
        };
    }

    public void RefreshSuggestions(Func<StepFieldDescriptor, IEnumerable<string>?> suggestionResolver)
    {
        foreach (var field in Fields)
            field.SetSuggestions(suggestionResolver(field.Descriptor));
    }
}

public sealed class GeneratedStepEditorSectionViewModel
{
    public GeneratedStepEditorSectionViewModel(
        StepEditorSectionDescriptor descriptor,
        IReadOnlyList<GeneratedStepFieldViewModel> fields)
    {
        Descriptor = descriptor;
        Fields = fields;
    }

    public StepEditorSectionDescriptor Descriptor { get; }
    public IReadOnlyList<GeneratedStepFieldViewModel> Fields { get; }
    public string Title => string.IsNullOrWhiteSpace(Descriptor.TitleKey)
        ? string.Empty
        : Loc.Get(Descriptor.TitleKey);
    public bool IsCollapsible => Descriptor.Collapsible;
    public bool IsInitiallyExpanded => Descriptor.InitiallyExpanded;
}

public sealed class GeneratedStepFieldViewModel : INotifyPropertyChanged
{
    private string _inputText;
    private GeneratedStepChoiceOptionViewModel? _selectedChoice;
    private GeneratedStepEnumOptionViewModel? _selectedEnumOption;
    private bool _isVisible = true;

    public GeneratedStepFieldViewModel(
        StepFieldDescriptor descriptor,
        JsonNode? value,
        IEnumerable<string>? suggestions = null,
        IEnumerable<GeneratedStepChoiceOptionViewModel>? choices = null,
        GeneratedProcessTargetEditorViewModel? processTargetEditor = null,
        GeneratedResultBindingEditorViewModel? resultBindingEditor = null,
        GeneratedCameraEditorViewModel? cameraEditor = null,
        GeneratedVisualOverlayEditorViewModel? visualOverlayEditor = null,
        GeneratedRoiEditorViewModel? roiEditor = null,
        GeneratedYoloEditorViewModel? yoloEditor = null,
        GeneratedConditionEditorViewModel? conditionEditor = null,
        GeneratedWindowsCapabilityEditorViewModel? windowsCapabilityEditor = null,
        GeneratedScreenPointEditorViewModel? screenPointEditor = null,
        GeneratedUserChoiceOptionsEditorViewModel? userChoiceOptionsEditor = null,
        GeneratedPointEntryListEditorViewModel? pointEntryListEditor = null,
        GeneratedAxisExpressionListEditorViewModel? axisExpressionListEditor = null)
    {
        Descriptor = descriptor;
        _inputText = FormatValue(value, descriptor.ValueKind);
        if (string.IsNullOrWhiteSpace(_inputText)
            && descriptor.DirectoryPickerOptions is { } directoryOptions)
            _inputText = ResolveSuggestedDirectory(directoryOptions);
        Suggestions = new ObservableCollection<string>(suggestions ?? []);
        Choices = new ObservableCollection<GeneratedStepChoiceOptionViewModel>(choices ?? []);
        EnumOptions = new ObservableCollection<GeneratedStepEnumOptionViewModel>(
            (descriptor.Options ?? []).Select(option => new GeneratedStepEnumOptionViewModel(
                option.Value,
                Loc.Get(option.LabelKey))));
        ProcessTargetEditor = processTargetEditor;
        if (ProcessTargetEditor is not null)
            ProcessTargetEditor.Changed += OnProcessTargetChanged;
        ResultBindingEditor = resultBindingEditor;
        if (ResultBindingEditor is not null)
            ResultBindingEditor.Changed += OnResultBindingChanged;
        CameraEditor = cameraEditor;
        if (CameraEditor is not null)
            CameraEditor.Changed += OnCameraChanged;
        VisualOverlayEditor = visualOverlayEditor;
        if (VisualOverlayEditor is not null)
            VisualOverlayEditor.Changed += OnVisualOverlayChanged;
        RoiEditor = roiEditor;
        if (RoiEditor is not null)
            RoiEditor.Changed += OnRoiChanged;
        YoloEditor = yoloEditor;
        if (YoloEditor is not null)
            YoloEditor.Changed += OnYoloChanged;
        ConditionEditor = conditionEditor;
        if (ConditionEditor is not null)
            ConditionEditor.Changed += OnConditionChanged;
        WindowsCapabilityEditor = windowsCapabilityEditor;
        if (WindowsCapabilityEditor is not null)
            WindowsCapabilityEditor.Changed += OnWindowsCapabilityChanged;
        ScreenPointEditor = screenPointEditor;
        UserChoiceOptionsEditor = userChoiceOptionsEditor;
        PointEntryListEditor = pointEntryListEditor;
        AxisExpressionListEditor = axisExpressionListEditor;
        foreach (var editor in new IGeneratedValueEditor?[] { ScreenPointEditor, UserChoiceOptionsEditor, PointEntryListEditor, AxisExpressionListEditor })
            if (editor is not null) editor.Changed += OnCustomValueChanged;
        _selectedChoice = FindChoice(value)
            ?? (Descriptor.Required && UsesChoicePicker ? Choices.FirstOrDefault() : null);
        if (_selectedChoice is not null)
            _inputText = JsonSerializer.SerializeToNode(_selectedChoice.Value)?.ToJsonString() ?? string.Empty;
        _selectedEnumOption = EnumOptions.FirstOrDefault(option =>
            string.Equals(option.Value, _inputText, StringComparison.OrdinalIgnoreCase));
        if (_selectedEnumOption is null && descriptor.Required && UsesEnumPicker)
            _selectedEnumOption = EnumOptions.FirstOrDefault();
        if (_selectedEnumOption is not null)
            _inputText = _selectedEnumOption.Value;
    }

    private static string ResolveSuggestedDirectory(StepDirectoryPickerOptions options)
    {
        var folder = options.SuggestedDirectory switch
        {
            StepKnownDirectory.Pictures => Environment.SpecialFolder.MyPictures,
            StepKnownDirectory.Videos => Environment.SpecialFolder.MyVideos,
            StepKnownDirectory.Desktop => Environment.SpecialFolder.DesktopDirectory,
            _ => Environment.SpecialFolder.MyDocuments
        };
        var path = Environment.GetFolderPath(folder);
        return string.IsNullOrWhiteSpace(options.SuggestedSubfolder)
            ? path
            : Path.Combine(path, options.SuggestedSubfolder);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public StepFieldDescriptor Descriptor { get; }
    public ObservableCollection<string> Suggestions { get; }
    public ObservableCollection<GeneratedStepChoiceOptionViewModel> Choices { get; }
    public ObservableCollection<GeneratedStepEnumOptionViewModel> EnumOptions { get; }
    public GeneratedProcessTargetEditorViewModel? ProcessTargetEditor { get; }
    public GeneratedResultBindingEditorViewModel? ResultBindingEditor { get; }
    public GeneratedCameraEditorViewModel? CameraEditor { get; }
    public GeneratedVisualOverlayEditorViewModel? VisualOverlayEditor { get; }
    public GeneratedRoiEditorViewModel? RoiEditor { get; }
    public GeneratedYoloEditorViewModel? YoloEditor { get; }
    public GeneratedConditionEditorViewModel? ConditionEditor { get; }
    public GeneratedWindowsCapabilityEditorViewModel? WindowsCapabilityEditor { get; }
    public GeneratedScreenPointEditorViewModel? ScreenPointEditor { get; }
    public GeneratedUserChoiceOptionsEditorViewModel? UserChoiceOptionsEditor { get; }
    public GeneratedPointEntryListEditorViewModel? PointEntryListEditor { get; }
    public GeneratedAxisExpressionListEditorViewModel? AxisExpressionListEditor { get; }
    public string Label => Loc.Get(Descriptor.LabelKey);
    public string Description => string.IsNullOrWhiteSpace(Descriptor.DescriptionKey)
        ? string.Empty
        : Loc.Get(Descriptor.DescriptionKey);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool IsBoolean => Descriptor.ValueKind == StepValueKind.Boolean;
    public bool UsesMonitorPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.MonitorPicker,
        StringComparison.Ordinal);
    public bool UsesFilePicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.FilePicker,
        StringComparison.Ordinal);
    public bool ShowsFilePreview => Descriptor.FilePickerOptions?.ShowPreview == true;
    public ImageSource? FilePreview => ShowsFilePreview ? LoadImagePreview(_inputText) : null;
    public bool HasFilePreview => FilePreview is not null;
    public bool UsesDirectoryPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.DirectoryPicker,
        StringComparison.Ordinal);
    public bool UsesFileOrFolderPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.FileOrFolderPicker,
        StringComparison.Ordinal);
    public bool UsesSuggestions => string.Equals(
            Descriptor.EditorHint,
            StepEditorHints.ProcessNameSuggestions,
            StringComparison.Ordinal)
        || string.Equals(
            Descriptor.EditorHint,
            StepEditorHints.ExecutablePathSuggestions,
            StringComparison.Ordinal);
    public bool UsesSuggestionFilePicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.StartProgramPicker,
        StringComparison.Ordinal);
    public bool UsesChoicePicker => string.Equals(
            Descriptor.EditorHint,
            StepEditorHints.MacroPicker,
            StringComparison.Ordinal)
        || string.Equals(
            Descriptor.EditorHint,
            StepEditorHints.JobPicker,
            StringComparison.Ordinal);
    public bool UsesProcessTargetPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.ProcessTargetPicker,
        StringComparison.Ordinal)
        || string.Equals(
            Descriptor.EditorHint,
            StepEditorHints.ExecutableProcessTargetPicker,
            StringComparison.Ordinal);
    public bool UsesNamedProcessTargetPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.ProcessTargetPicker,
        StringComparison.Ordinal);
    public bool UsesExecutableProcessTargetPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.ExecutableProcessTargetPicker,
        StringComparison.Ordinal);
    public bool UsesEnumPicker => Descriptor.ValueKind == StepValueKind.Enum;
    public bool UsesResultBindingPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.ResultBindingPicker,
        StringComparison.Ordinal);
    public bool UsesPercentagePicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.Percentage,
        StringComparison.Ordinal);
    public bool UsesCameraPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.CameraPicker,
        StringComparison.Ordinal);
    public bool UsesVisualOverlay => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.VisualOverlay,
        StringComparison.Ordinal);
    public bool UsesRoiPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.RoiPicker,
        StringComparison.Ordinal);
    public bool UsesYoloPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.YoloPicker,
        StringComparison.Ordinal);
    public bool UsesConditionEditor => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.ConditionEditor,
        StringComparison.Ordinal);
    public bool UsesWindowsCapabilityPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.WindowsCapabilityPicker,
        StringComparison.Ordinal);
    public bool UsesScreenPointPicker => Descriptor.EditorHint == StepEditorHints.ScreenPointPicker;
    public bool UsesUserChoiceOptions => Descriptor.EditorHint == StepEditorHints.UserChoiceOptions;
    public bool UsesPointEntryList => Descriptor.EditorHint == StepEditorHints.PointEntryList;
    public bool UsesAxisExpressionList => Descriptor.EditorHint == StepEditorHints.AxisExpressionList;
    public bool UsesEmojiText => Descriptor.EditorHint == StepEditorHints.EmojiText;
    public bool UsesColorPicker => Descriptor.ValueKind == StepValueKind.Color;
    public bool UsesMultilineTextInput => Descriptor.ValueKind == StepValueKind.MultilineText && !UsesEmojiText;
    public bool UsesTextInput => !IsBoolean && !UsesMonitorPicker && !UsesFilePicker && !UsesDirectoryPicker
        && !UsesFileOrFolderPicker && !UsesColorPicker && !UsesMultilineTextInput && !UsesCameraPicker
        && !UsesVisualOverlay && !UsesRoiPicker && !UsesYoloPicker && !UsesConditionEditor
        && !UsesWindowsCapabilityPicker
        && !UsesScreenPointPicker && !UsesUserChoiceOptions && !UsesPointEntryList && !UsesAxisExpressionList
        && !UsesEmojiText
        && !UsesSuggestions && !UsesSuggestionFilePicker && !UsesChoicePicker
        && !UsesProcessTargetPicker && !UsesEnumPicker && !UsesResultBindingPicker
        && !UsesPercentagePicker;
    public bool IsVisible => _isVisible;

    public GeneratedStepEnumOptionViewModel? SelectedEnumOption
    {
        get => _selectedEnumOption;
        set
        {
            if (ReferenceEquals(_selectedEnumOption, value)) return;
            _selectedEnumOption = value;
            _inputText = value?.Value ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEnumOption)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputText)));
            if (ShowsFilePreview)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePreview)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasFilePreview)));
            }
        }
    }

    public GeneratedStepChoiceOptionViewModel? SelectedChoice
    {
        get => _selectedChoice;
        set
        {
            if (ReferenceEquals(_selectedChoice, value)) return;
            _selectedChoice = value;
            _inputText = value is null
                ? string.Empty
                : JsonSerializer.SerializeToNode(value.Value)?.ToJsonString() ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedChoice)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputText)));
        }
    }

    public void SetSuggestions(IEnumerable<string>? suggestions)
    {
        Suggestions.Clear();
        foreach (var suggestion in suggestions ?? [])
            Suggestions.Add(suggestion);
    }

    public bool BooleanValue
    {
        get => bool.TryParse(_inputText, out var value) && value;
        set => InputText = value.ToString(CultureInfo.InvariantCulture);
    }

    public int IntegerValue
    {
        get => int.TryParse(_inputText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value)
            ? value
            : 0;
        set => InputText = value.ToString(CultureInfo.CurrentCulture);
    }

    public double NumberValue
    {
        get => double.TryParse(_inputText, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
            ? value
            : 0;
        set => InputText = value.ToString(CultureInfo.CurrentCulture);
    }

    public Color ColorValue
    {
        get
        {
            try { return (Color)ColorConverter.ConvertFromString(_inputText); }
            catch (FormatException) { return Colors.White; }
            catch (NotSupportedException) { return Colors.White; }
        }
        set => InputText = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (_inputText == value) return;
            _inputText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputText)));
            if (IsBoolean)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BooleanValue)));
            if (Descriptor.ValueKind is StepValueKind.Integer or StepValueKind.Duration)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IntegerValue)));
            if (Descriptor.ValueKind == StepValueKind.Number)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberValue)));
            if (Descriptor.ValueKind == StepValueKind.Color)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorValue)));
        }
    }

    public bool TryWriteValue(StepDraft draft, out string? error)
    {
        error = null;
        if (UsesChoicePicker)
        {
            if (SelectedChoice is null)
            {
                error = Loc.Format("Ui.Step.Generated.Validation.Required", Label);
                return false;
            }
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(SelectedChoice.Value);
            return true;
        }
        if (UsesProcessTargetPicker && ProcessTargetEditor is not null)
        {
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(ProcessTargetEditor.ToValue());
            return true;
        }
        if (UsesResultBindingPicker && ResultBindingEditor is not null)
        {
            if (Descriptor.Required && !ResultBindingEditor.Picker.IsConfigured)
            {
                error = Loc.Format("Ui.Step.Generated.Validation.Required", Label);
                return false;
            }
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(ResultBindingEditor.Picker.ToBinding());
            return true;
        }
        if (UsesCameraPicker && CameraEditor is not null)
        {
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(CameraEditor.ToValue());
            return true;
        }
        if (UsesVisualOverlay && VisualOverlayEditor is not null)
        {
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(VisualOverlayEditor.ToValue());
            return true;
        }
        if (UsesRoiPicker && RoiEditor is not null)
        {
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(RoiEditor.ToValue());
            return true;
        }
        if (UsesYoloPicker && YoloEditor is not null)
        {
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(YoloEditor.ToValue());
            return true;
        }
        if (UsesConditionEditor && ConditionEditor is not null)
        {
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(ConditionEditor.ToValue());
            return true;
        }
        if (UsesWindowsCapabilityPicker && WindowsCapabilityEditor is not null)
        {
            draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(WindowsCapabilityEditor.ToValue());
            return true;
        }
        if (UsesScreenPointPicker && ScreenPointEditor is not null) return WriteCustom(draft, ScreenPointEditor);
        if (UsesUserChoiceOptions && UserChoiceOptionsEditor is not null) return WriteCustom(draft, UserChoiceOptionsEditor);
        if (UsesPointEntryList && PointEntryListEditor is not null) return WriteCustom(draft, PointEntryListEditor);
        if (UsesAxisExpressionList && AxisExpressionListEditor is not null) return WriteCustom(draft, AxisExpressionListEditor);
        var text = InputText.Trim();
        if (text.Length == 0)
        {
            if (Descriptor.Required)
            {
                error = Loc.Format("Ui.Step.Generated.Validation.Required", Label);
                return false;
            }
            draft.Values[Descriptor.Id] = null;
            return true;
        }

        switch (Descriptor.ValueKind)
        {
            case StepValueKind.Integer:
            case StepValueKind.Duration:
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var integer))
                {
                    error = Loc.Format("Ui.Step.Generated.Validation.Integer", Label);
                    return false;
                }
                draft.Values[Descriptor.Id] = JsonValue.Create(integer);
                return true;
            case StepValueKind.Number:
                if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var number))
                {
                    error = Loc.Format("Ui.Step.Generated.Validation.Number", Label);
                    return false;
                }
                draft.Values[Descriptor.Id] = JsonValue.Create(number);
                return true;
            case StepValueKind.Boolean:
                if (!bool.TryParse(text, out var flag))
                {
                    error = Loc.Get("Ui.Step.Generated.Validation.Invalid");
                    return false;
                }
                draft.Values[Descriptor.Id] = JsonValue.Create(flag);
                return true;
            default:
                draft.Values[Descriptor.Id] = JsonValue.Create(InputText);
                return true;
        }
    }

    public bool ValueEquals(JsonNode? expected)
    {
        if (expected is null) return string.IsNullOrWhiteSpace(InputText);
        try { return string.Equals(InputText, expected.GetValue<string>(), StringComparison.OrdinalIgnoreCase); }
        catch (InvalidOperationException) { return string.Equals(InputText, expected.ToJsonString(), StringComparison.Ordinal); }
    }

    public void SetVisibility(bool isVisible)
    {
        if (_isVisible == isVisible) return;
        _isVisible = isVisible;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
    }

    private static string FormatValue(JsonNode? value, StepValueKind kind)
    {
        if (value is null) return string.Empty;
        try
        {
            return kind switch
            {
                StepValueKind.Integer or StepValueKind.Duration =>
                    value.GetValue<int>().ToString(CultureInfo.CurrentCulture),
                StepValueKind.Number => value.GetValue<decimal>().ToString(CultureInfo.CurrentCulture),
                StepValueKind.Boolean => value.GetValue<bool>().ToString(),
                _ => value.GetValue<string>()
            };
        }
        catch (InvalidOperationException)
        {
            return value.ToJsonString();
        }
    }

    private static ImageSource? LoadImagePreview(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
            image.DecodePixelWidth = 320;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch { return null; }
    }

    private GeneratedStepChoiceOptionViewModel? FindChoice(JsonNode? value)
    {
        if (value is null) return null;
        try
        {
            var reference = value.Deserialize<StepReferenceValue>();
            if (reference is null) return null;
            return Choices.FirstOrDefault(option =>
                       !string.IsNullOrWhiteSpace(reference.Id)
                       && string.Equals(option.Value.Id, reference.Id, StringComparison.OrdinalIgnoreCase))
                   ?? Choices.FirstOrDefault(option =>
                       !string.IsNullOrWhiteSpace(reference.Name)
                       && string.Equals(option.Value.Name, reference.Name, StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void OnProcessTargetChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProcessTargetEditor)));
    }

    private void OnResultBindingChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultBindingEditor)));
    }

    private void OnCameraChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraEditor)));
    }

    private void OnVisualOverlayChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisualOverlayEditor)));
    }

    private void OnRoiChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RoiEditor)));
    }

    private void OnYoloChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YoloEditor)));
    }

    private void OnConditionChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConditionEditor)));
    }

    private void OnWindowsCapabilityChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowsCapabilityEditor)));
    }

    private bool WriteCustom(StepDraft draft, IGeneratedValueEditor editor)
    {
        draft.Values[Descriptor.Id] = editor.ToNode();
        return true;
    }

    private void OnCustomValueChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputText)));
}

public interface IGeneratedValueEditor
{
    event Action? Changed;
    JsonNode? ToNode();
}

public sealed record GeneratedStepChoiceOptionViewModel(StepReferenceValue Value)
{
    public string Label => Value.Name;
}

public sealed record GeneratedStepEnumOptionViewModel(string Value, string Label);

public sealed class GeneratedConditionEditorViewModel : INotifyPropertyChanged
{
    private ConditionMatchMode _matchMode = ConditionMatchMode.All;
    private readonly IReadOnlyList<SourceStepItem> _sources;

    public GeneratedConditionEditorViewModel(JsonNode? value, IReadOnlyList<SourceStepItem> sources)
    {
        _sources = sources;
        Conditions.CollectionChanged += OnCollectionChanged;
        AddCommand = new RelayCommand(AddCondition);

        IfConditionSettings settings;
        try { settings = value?.Deserialize<IfConditionSettings>() ?? new IfConditionSettings(); }
        catch (JsonException) { settings = new IfConditionSettings(); }
        catch (InvalidOperationException) { settings = new IfConditionSettings(); }

        _matchMode = settings.MatchMode;
        foreach (var condition in settings.Conditions)
        {
            var row = new ConditionRowViewModel(Conditions, _sources);
            row.LoadFrom(condition);
            Conditions.Add(row);
        }
        if (Conditions.Count == 0)
            AddCondition();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    public ObservableCollection<ConditionRowViewModel> Conditions { get; } = [];
    public ICommand AddCommand { get; }

    public bool IsAll
    {
        get => _matchMode == ConditionMatchMode.All;
        set { if (value) SetMatchMode(ConditionMatchMode.All); }
    }

    public bool IsAny
    {
        get => _matchMode == ConditionMatchMode.Any;
        set { if (value) SetMatchMode(ConditionMatchMode.Any); }
    }

    public IfConditionSettings ToValue() => new()
    {
        MatchMode = _matchMode,
        Conditions = Conditions.Select(condition => condition.ToCondition()).ToList()
    };

    private void AddCondition() =>
        Conditions.Add(new ConditionRowViewModel(Conditions, _sources));

    private void SetMatchMode(ConditionMatchMode value)
    {
        if (_matchMode == value) return;
        _matchMode = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAll)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAny)));
        Changed?.Invoke();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (ConditionRowViewModel row in e.OldItems)
                row.PropertyChanged -= OnConditionChanged;
        if (e.NewItems is not null)
            foreach (ConditionRowViewModel row in e.NewItems)
                row.PropertyChanged += OnConditionChanged;
        Changed?.Invoke();
    }

    private void OnConditionChanged(object? sender, PropertyChangedEventArgs e) => Changed?.Invoke();
}

public sealed class GeneratedWindowsCapabilityEditorViewModel
{
    public GeneratedWindowsCapabilityEditorViewModel(
        JsonNode? value,
        StepWindowsCapabilityPickerMode mode,
        IWindowsSettingOptionProvider? optionProvider = null)
    {
        StepWindowsCapabilitySelectionValue selection;
        try
        {
            selection = value?.Deserialize<StepWindowsCapabilitySelectionValue>()
                        ?? new StepWindowsCapabilitySelectionValue(string.Empty, new Dictionary<string, string?>());
        }
        catch (JsonException)
        {
            selection = new StepWindowsCapabilitySelectionValue(string.Empty, new Dictionary<string, string?>());
        }
        catch (InvalidOperationException)
        {
            selection = new StepWindowsCapabilitySelectionValue(string.Empty, new Dictionary<string, string?>());
        }

        var pickerMode = mode == StepWindowsCapabilityPickerMode.SettingChange
            ? WindowsCapabilityPickerMode.SettingChange
            : WindowsCapabilityPickerMode.StateQuery;
        Picker = new WindowsCapabilityPickerViewModel(
            new WindowsCapabilityCatalog(), pickerMode, selection.CapabilityId, selection.Parameters, optionProvider);
        Picker.Changed += () => Changed?.Invoke();
    }

    public event Action? Changed;
    public WindowsCapabilityPickerViewModel Picker { get; }

    public StepWindowsCapabilitySelectionValue ToValue() => new(
        Picker.SelectedCapability?.Id ?? string.Empty,
        Picker.ToDictionary());
}

public sealed class GeneratedScreenPointEditorViewModel : INotifyPropertyChanged, IGeneratedValueEditor
{
    private int _monitorIndex;
    private int _x;
    private int _y;
    private readonly Func<int?> _selectMonitor;
    private readonly Func<Task<StepScreenPointSelectionValue?>> _capturePoint;

    public GeneratedScreenPointEditorViewModel(
        JsonNode? value,
        Func<StepScreenPointSelectionValue, StepScreenPointSelectionValue> normalize,
        Func<int?> selectMonitor,
        Func<Task<StepScreenPointSelectionValue?>> capturePoint)
    {
        StepScreenPointSelectionValue selection;
        try { selection = value?.Deserialize<StepScreenPointSelectionValue>() ?? new(0, 0, 0, KlickOnPoint3DSettings.MonitorLocalCoordinates); }
        catch (JsonException) { selection = new(0, 0, 0, KlickOnPoint3DSettings.MonitorLocalCoordinates); }
        selection = normalize(selection);
        _monitorIndex = selection.MonitorIndex;
        _x = selection.X;
        _y = selection.Y;
        _selectMonitor = selectMonitor;
        _capturePoint = capturePoint;
        SelectMonitorCommand = new RelayCommand(SelectMonitor);
        CaptureCommand = new RelayCommand(Capture);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    public ICommand SelectMonitorCommand { get; }
    public ICommand CaptureCommand { get; }
    public int MonitorIndex { get => _monitorIndex; set => Set(ref _monitorIndex, value); }
    public int X { get => _x; set => Set(ref _x, value); }
    public int Y { get => _y; set => Set(ref _y, value); }
    public JsonNode? ToNode() => JsonSerializer.SerializeToNode(new StepScreenPointSelectionValue(
        MonitorIndex, X, Y, KlickOnPoint3DSettings.MonitorLocalCoordinates));

    private void SelectMonitor()
    {
        var selected = _selectMonitor();
        if (selected.HasValue) MonitorIndex = selected.Value;
    }

    private async void Capture()
    {
        var selected = await _capturePoint();
        if (selected is null) return;
        MonitorIndex = selected.MonitorIndex;
        X = selected.X;
        Y = selected.Y;
    }

    private void Set(ref int field, int value, [CallerMemberName] string? name = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
        Changed?.Invoke();
    }
}

public sealed class GeneratedUserChoiceOptionsEditorViewModel : IGeneratedValueEditor
{
    public GeneratedUserChoiceOptionsEditorViewModel(JsonNode? value)
    {
        Options.CollectionChanged += OnCollectionChanged;
        IReadOnlyList<StepUserChoiceOptionValue> values;
        try { values = value?.Deserialize<List<StepUserChoiceOptionValue>>() ?? []; }
        catch (JsonException) { values = []; }
        foreach (var option in values)
            Options.Add(new UserChoiceOptionEditorViewModel(Options, option.Id, option.Label, option.Value));
        while (Options.Count < 2) Options.Add(new UserChoiceOptionEditorViewModel(Options));
        AddCommand = new RelayCommand(Add, () => Options.Count < 18);
    }

    public event Action? Changed;
    public ObservableCollection<UserChoiceOptionEditorViewModel> Options { get; } = [];
    public ICommand AddCommand { get; }
    public JsonNode? ToNode() => JsonSerializer.SerializeToNode(Options.Select(option =>
        new StepUserChoiceOptionValue(option.Id, option.Label, option.Value)).ToArray());
    private void Add() => Options.Add(new UserChoiceOptionEditorViewModel(Options));
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null) foreach (UserChoiceOptionEditorViewModel item in e.OldItems) item.PropertyChanged -= ItemChanged;
        if (e.NewItems is not null) foreach (UserChoiceOptionEditorViewModel item in e.NewItems) item.PropertyChanged += ItemChanged;
        (AddCommand as RelayCommand)?.RaiseCanExecuteChanged();
        Changed?.Invoke();
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) => Changed?.Invoke();
}

public sealed class GeneratedPointEntryListEditorViewModel : IGeneratedValueEditor
{
    private readonly IReadOnlyList<SourceStepItem> _sources;
    public GeneratedPointEntryListEditorViewModel(JsonNode? value, IReadOnlyList<SourceStepItem> sources)
    {
        _sources = sources;
        Points.CollectionChanged += OnCollectionChanged;
        IReadOnlyList<StepPointEntryValue> values;
        try { values = value?.Deserialize<List<StepPointEntryValue>>() ?? []; }
        catch (JsonException) { values = []; }
        foreach (var valueItem in values) Add(valueItem);
        if (Points.Count == 0) Add();
        AddCommand = new RelayCommand(() => Add());
    }
    public event Action? Changed;
    public ObservableCollection<PointEntryViewModel> Points { get; } = [];
    public ICommand AddCommand { get; }
    public JsonNode? ToNode() => JsonSerializer.SerializeToNode(Points.Select(point =>
    {
        var value = point.ToPointEntry();
        return new StepPointEntryValue(value.Source.ToString(), value.ManualX, value.ManualY,
            JsonSerializer.SerializeToNode(value.PointsSource));
    }).ToArray());
    private void Add(StepPointEntryValue? value = null)
    {
        var item = new PointEntryViewModel(Points, _sources);
        if (value is not null)
        {
            ResultBinding binding;
            try { binding = value.PointsSource?.Deserialize<ResultBinding>() ?? new(); } catch (JsonException) { binding = new(); }
            item.LoadFrom(new PointEntry
            {
                Source = Enum.TryParse(value.Source, out PointEntrySource source) ? source : PointEntrySource.Manual,
                ManualX = value.ManualX, ManualY = value.ManualY, PointsSource = binding
            });
        }
        Points.Add(item);
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null) foreach (PointEntryViewModel item in e.OldItems) { item.PropertyChanged -= ItemChanged; item.PointsSource.PropertyChanged -= ItemChanged; }
        if (e.NewItems is not null) foreach (PointEntryViewModel item in e.NewItems) { item.PropertyChanged += ItemChanged; item.PointsSource.PropertyChanged += ItemChanged; }
        Changed?.Invoke();
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) => Changed?.Invoke();
}

public sealed class GeneratedAxisExpressionListEditorViewModel : IGeneratedValueEditor
{
    public GeneratedAxisExpressionListEditorViewModel(JsonNode? value)
    {
        Expressions.CollectionChanged += OnCollectionChanged;
        IReadOnlyList<StepAxisExpressionValue> values;
        try { values = value?.Deserialize<List<StepAxisExpressionValue>>() ?? []; } catch (JsonException) { values = []; }
        foreach (var item in values) Add(item);
        if (Expressions.Count == 0) Add();
        AddCommand = new RelayCommand(() => Add());
    }
    public event Action? Changed;
    public ObservableCollection<AxisExpressionViewModel> Expressions { get; } = [];
    public ICommand AddCommand { get; }
    public JsonNode? ToNode() => JsonSerializer.SerializeToNode(Expressions.Select(item =>
    {
        var value = item.ToAxisExpression();
        return new StepAxisExpressionValue(value.Axis, value.Operator.ToString(), value.Value);
    }).ToArray());
    private void Add(StepAxisExpressionValue? value = null)
    {
        var item = new AxisExpressionViewModel(Expressions);
        if (value is not null) item.LoadFrom(new AxisExpression
        {
            Axis = value.Axis,
            Operator = Enum.TryParse(value.Operator, out PointAxisOperator op) ? op : PointAxisOperator.LessThan,
            Value = value.Value
        });
        Expressions.Add(item);
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null) foreach (AxisExpressionViewModel item in e.OldItems) item.PropertyChanged -= ItemChanged;
        if (e.NewItems is not null) foreach (AxisExpressionViewModel item in e.NewItems) item.PropertyChanged += ItemChanged;
        Changed?.Invoke();
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) => Changed?.Invoke();
}

public sealed class GeneratedRoiEditorViewModel : INotifyPropertyChanged
{
    private bool _isRoiEnabled;
    private bool _useDynamicRoi;
    private int _x;
    private int _y;
    private int _width;
    private int _height;

    public GeneratedRoiEditorViewModel(JsonNode? value, ResultBindingPickerViewModel dynamicRoiSource)
    {
        DetectionDynamicRoiSource = dynamicRoiSource;
        var selection = Read(value);
        _isRoiEnabled = selection.Enabled;
        _x = selection.X;
        _y = selection.Y;
        _width = selection.Width;
        _height = selection.Height;
        try { DetectionDynamicRoiSource.Load(selection.DynamicSource?.Deserialize<ResultBinding>() ?? new ResultBinding()); }
        catch (JsonException) { DetectionDynamicRoiSource.Load(new ResultBinding()); }
        _useDynamicRoi = DetectionDynamicRoiSource.IsConfigured;
        DetectionDynamicRoiSource.PropertyChanged += (_, _) =>
        {
            PropertyChanged?.Invoke(this, new(nameof(HasSelectedDynamicRoi)));
            Changed?.Invoke();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    public ResultBindingPickerViewModel DetectionDynamicRoiSource { get; }
    public bool HasSelectedDynamicRoi => UseDynamicRoi && DetectionDynamicRoiSource.IsConfigured;

    public bool IsRoiEnabled { get => _isRoiEnabled; set => Set(ref _isRoiEnabled, value, nameof(IsRoiEnabled)); }
    public bool UseDynamicRoi
    {
        get => _useDynamicRoi;
        set
        {
            if (_useDynamicRoi == value) return;
            _useDynamicRoi = value;
            PropertyChanged?.Invoke(this, new(nameof(UseDynamicRoi)));
            PropertyChanged?.Invoke(this, new(nameof(HasSelectedDynamicRoi)));
            Changed?.Invoke();
        }
    }
    public int X { get => _x; set => Set(ref _x, value, nameof(X)); }
    public int Y { get => _y; set => Set(ref _y, value, nameof(Y)); }
    public int RoiWidth { get => _width; set => Set(ref _width, value, nameof(RoiWidth)); }
    public int RoiHeight { get => _height; set => Set(ref _height, value, nameof(RoiHeight)); }

    public StepRoiSelectionValue ToValue() => new(
        IsRoiEnabled, X, Y, RoiWidth, RoiHeight,
        JsonSerializer.SerializeToNode(UseDynamicRoi ? DetectionDynamicRoiSource.ToBinding() : new ResultBinding()));

    private void Set(ref int field, int value, string propertyName)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new(propertyName));
        Changed?.Invoke();
    }

    private void Set(ref bool field, bool value, string propertyName)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new(propertyName));
        Changed?.Invoke();
    }

    private static StepRoiSelectionValue Read(JsonNode? value)
    {
        try { return value?.Deserialize<StepRoiSelectionValue>() ?? new(false, 0, 0, 0, 0, null); }
        catch (JsonException) { return new(false, 0, 0, 0, 0, null); }
        catch (InvalidOperationException) { return new(false, 0, 0, 0, 0, null); }
    }
}

public sealed class GeneratedYoloEditorViewModel : INotifyPropertyChanged
{
    private readonly Func<IReadOnlyList<string>> _modelLoader;
    private readonly Func<string, IReadOnlyList<string>> _classLoader;
    private readonly Func<string, double?> _recommendedConfidenceLoader;
    private string _model;
    private string _className;
    private int _classLoadVersion;

    public GeneratedYoloEditorViewModel(
        JsonNode? value,
        Func<IReadOnlyList<string>> modelLoader,
        Func<string, IReadOnlyList<string>> classLoader,
        Func<string, double?> recommendedConfidenceLoader)
    {
        _modelLoader = modelLoader;
        _classLoader = classLoader;
        _recommendedConfidenceLoader = recommendedConfidenceLoader;
        var selection = Read(value);
        _model = selection.Model;
        _className = selection.ClassName;
        Initialization = LoadModelsAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    public event Action<double>? RecommendedConfidenceChanged;
    public ObservableCollection<string> Models { get; } = [];
    public ObservableCollection<string> Classes { get; } = [];
    public Task Initialization { get; }
    public Task ClassLoading { get; private set; } = Task.CompletedTask;

    public string Model
    {
        get => _model;
        set
        {
            if (_model == value) return;
            _model = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new(nameof(Model)));
            Changed?.Invoke();
            try
            {
                if (_recommendedConfidenceLoader(_model) is { } confidence)
                    RecommendedConfidenceChanged?.Invoke(confidence);
            }
            catch { }
            ClassLoading = LoadClassesAsync(_model);
        }
    }

    public string ClassName
    {
        get => _className;
        set
        {
            if (_className == value) return;
            _className = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new(nameof(ClassName)));
            Changed?.Invoke();
        }
    }

    public StepYoloSelectionValue ToValue() => new(Model.Trim(), ClassName.Trim());

    private async Task LoadModelsAsync()
    {
        IReadOnlyList<string> models;
        try { models = await Task.Run(_modelLoader); }
        catch { models = []; }
        Models.Clear();
        foreach (var model in models.Distinct(StringComparer.OrdinalIgnoreCase))
            Models.Add(model);
        if (!string.IsNullOrWhiteSpace(_model) && !Models.Contains(_model))
            Models.Add(_model);
        if (string.IsNullOrWhiteSpace(_model) && Models.Count > 0)
            Model = Models[0];
        else
            await LoadClassesAsync(_model);
    }

    private async Task LoadClassesAsync(string model)
    {
        var version = ++_classLoadVersion;
        IReadOnlyList<string> classes;
        try
        {
            classes = string.IsNullOrWhiteSpace(model)
                ? []
                : await Task.Run(() => _classLoader(model));
        }
        catch { classes = []; }
        if (version != _classLoadVersion || !string.Equals(model, _model, StringComparison.Ordinal)) return;
        Classes.Clear();
        foreach (var className in classes.Distinct(StringComparer.OrdinalIgnoreCase))
            Classes.Add(className);
        if (!string.IsNullOrWhiteSpace(_className) && !Classes.Contains(_className))
            Classes.Add(_className);
        if (string.IsNullOrWhiteSpace(_className) && Classes.Count > 0)
            ClassName = Classes[0];
    }

    private static StepYoloSelectionValue Read(JsonNode? value)
    {
        try { return value?.Deserialize<StepYoloSelectionValue>() ?? new(string.Empty, string.Empty); }
        catch (JsonException) { return new(string.Empty, string.Empty); }
        catch (InvalidOperationException) { return new(string.Empty, string.Empty); }
    }
}

public sealed class GeneratedResultBindingEditorViewModel
{
    public GeneratedResultBindingEditorViewModel(JsonNode? value, ResultBindingPickerViewModel picker)
    {
        Picker = picker;
        try { Picker.Load(value?.Deserialize<ResultBinding>() ?? new ResultBinding()); }
        catch (JsonException) { Picker.Load(new ResultBinding()); }
        Picker.PropertyChanged += (_, _) => Changed?.Invoke();
    }

    public event Action? Changed;
    public ResultBindingPickerViewModel Picker { get; }
}

public sealed class GeneratedProcessTargetEditorViewModel : INotifyPropertyChanged
{
    private bool _useProcessReference;
    private string _processName;
    private string _executablePath;

    public GeneratedProcessTargetEditorViewModel(
        JsonNode? value,
        ResultBindingPickerViewModel picker,
        IEnumerable<string> processNames)
    {
        Picker = picker;
        ProcessNames = processNames;
        var selector = ReadSelector(value);
        var binding = ReadBinding(selector.ProcessSource);
        Picker.Load(binding);
        _useProcessReference = binding.IsConfigured;
        _processName = selector.ProcessName;
        _executablePath = selector.ExecutablePath;
        Picker.PropertyChanged += (_, _) => Changed?.Invoke();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    public ResultBindingPickerViewModel Picker { get; }
    public IEnumerable<string> ProcessNames { get; }

    public bool UseProcessReference
    {
        get => _useProcessReference;
        set
        {
            if (_useProcessReference == value) return;
            _useProcessReference = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseProcessReference)));
            Changed?.Invoke();
        }
    }

    public string ProcessName
    {
        get => _processName;
        set
        {
            if (_processName == value) return;
            _processName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProcessName)));
            Changed?.Invoke();
        }
    }

    public string ExecutablePath
    {
        get => _executablePath;
        set
        {
            if (_executablePath == value) return;
            _executablePath = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecutablePath)));
            Changed?.Invoke();
        }
    }

    public StepProcessSelectorValue ToValue() => new(
        JsonSerializer.SerializeToNode(UseProcessReference ? Picker.ToBinding() : new ResultBinding()),
        UseProcessReference ? string.Empty : ProcessName,
        UseProcessReference ? string.Empty : _executablePath);

    private static StepProcessSelectorValue ReadSelector(JsonNode? value)
    {
        try
        {
            return value?.Deserialize<StepProcessSelectorValue>()
                ?? new StepProcessSelectorValue(null, string.Empty, string.Empty);
        }
        catch (JsonException)
        {
            return new StepProcessSelectorValue(null, string.Empty, string.Empty);
        }
    }

    private static ResultBinding ReadBinding(JsonNode? value)
    {
        try { return value?.Deserialize<ResultBinding>() ?? new ResultBinding(); }
        catch (JsonException) { return new ResultBinding(); }
    }
}

public sealed record GeneratedCameraQualityChoice(
    CameraQualityMode QualityMode,
    CameraCaptureMode? Mode,
    string DisplayName);

public sealed class GeneratedCameraEditorViewModel : INotifyPropertyChanged
{
    private readonly ICameraCaptureService _service;
    private string _cameraId;
    private string _cameraName;
    private CameraQualityMode _qualityMode;
    private int _width;
    private int _height;
    private double _framesPerSecond;
    private string _pixelFormat;
    private CameraDeviceInfo? _selectedCamera;
    private GeneratedCameraQualityChoice? _selectedQuality;
    private bool _isLoadingCameras;
    private bool _isLoadingQualities;
    private string _cameraStatus = string.Empty;
    private string _qualityStatus = string.Empty;
    private int _qualityLoadVersion;

    public GeneratedCameraEditorViewModel(JsonNode? value, ICameraCaptureService service)
    {
        _service = service;
        var selection = ReadSelection(value);
        _cameraId = selection.CameraId;
        _cameraName = selection.CameraName;
        _qualityMode = Enum.TryParse<CameraQualityMode>(selection.QualityMode, out var mode)
            ? mode
            : CameraQualityMode.Automatic;
        _width = selection.Width;
        _height = selection.Height;
        _framesPerSecond = selection.FramesPerSecond;
        _pixelFormat = selection.PixelFormat;
        RefreshCommand = new RelayCommand(() => _ = RefreshAsync(), () => !IsLoadingCameras);
        Initialization = RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    public ObservableCollection<CameraDeviceInfo> Cameras { get; } = [];
    public ObservableCollection<GeneratedCameraQualityChoice> Qualities { get; } = [];
    public ICommand RefreshCommand { get; }
    public Task Initialization { get; }
    public Task QualityLoading { get; private set; } = Task.CompletedTask;

    public CameraDeviceInfo? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (ReferenceEquals(_selectedCamera, value)) return;
            _selectedCamera = value;
            if (value is not null)
            {
                _cameraId = value.Id;
                if (value.Index >= 0 || string.IsNullOrWhiteSpace(_cameraName))
                    _cameraName = value.Name;
            }
            OnChanged(nameof(SelectedCamera));
            QualityLoading = LoadQualitiesAsync(value);
        }
    }

    public GeneratedCameraQualityChoice? SelectedQuality
    {
        get => _selectedQuality;
        set
        {
            if (ReferenceEquals(_selectedQuality, value)) return;
            _selectedQuality = value;
            if (value is not null)
            {
                _qualityMode = value.QualityMode;
                _width = value.Mode?.Width ?? 0;
                _height = value.Mode?.Height ?? 0;
                _framesPerSecond = value.Mode?.FramesPerSecond ?? 0;
                _pixelFormat = value.Mode?.PixelFormat ?? string.Empty;
            }
            OnChanged(nameof(SelectedQuality));
        }
    }

    public bool IsLoadingCameras
    {
        get => _isLoadingCameras;
        private set
        {
            if (_isLoadingCameras == value) return;
            _isLoadingCameras = value;
            PropertyChanged?.Invoke(this, new(nameof(IsLoadingCameras)));
            (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool IsLoadingQualities
    {
        get => _isLoadingQualities;
        private set
        {
            if (_isLoadingQualities == value) return;
            _isLoadingQualities = value;
            PropertyChanged?.Invoke(this, new(nameof(IsLoadingQualities)));
        }
    }

    public string CameraStatus
    {
        get => _cameraStatus;
        private set { _cameraStatus = value; PropertyChanged?.Invoke(this, new(nameof(CameraStatus))); }
    }

    public string QualityStatus
    {
        get => _qualityStatus;
        private set { _qualityStatus = value; PropertyChanged?.Invoke(this, new(nameof(QualityStatus))); }
    }

    public async Task RefreshAsync()
    {
        if (IsLoadingCameras) return;
        IsLoadingCameras = true;
        CameraStatus = Loc.Get("Ui.Step.Camera.Loading");
        try
        {
            var devices = await Task.Run(_service.GetAvailableCameras);
            Cameras.Clear();
            foreach (var device in devices)
                Cameras.Add(device);

            var selected = Cameras.FirstOrDefault(camera =>
                string.Equals(camera.Id, _cameraId, StringComparison.OrdinalIgnoreCase));
            if (selected is null && !string.IsNullOrWhiteSpace(_cameraId))
            {
                selected = new CameraDeviceInfo(
                    _cameraId,
                    Loc.Format("Ui.Step.Camera.Unavailable", _cameraName),
                    -1);
                Cameras.Add(selected);
            }

            SelectedCamera = selected ?? Cameras.FirstOrDefault();
            CameraStatus = devices.Count == 0
                ? Loc.Get("Ui.Step.Camera.NoneFound")
                : Loc.Format("Ui.Step.Camera.FoundCount", devices.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CameraStatus = Loc.Format("Ui.Step.Camera.LoadFailed", ex.Message);
        }
        finally
        {
            IsLoadingCameras = false;
        }
    }

    public StepCameraSelectionValue ToValue() => new(
        _cameraId,
        _cameraName,
        _qualityMode.ToString(),
        _width,
        _height,
        _framesPerSecond,
        _pixelFormat);

    private async Task LoadQualitiesAsync(CameraDeviceInfo? camera)
    {
        var loadVersion = ++_qualityLoadVersion;
        Qualities.Clear();
        _selectedQuality = null;
        PropertyChanged?.Invoke(this, new(nameof(SelectedQuality)));
        if (camera is null || camera.Index < 0)
        {
            QualityStatus = string.Empty;
            return;
        }

        IsLoadingQualities = true;
        QualityStatus = Loc.Get("Ui.Step.Camera.QualityLoading");
        try
        {
            var modes = await Task.Run(() => _service.GetSupportedModes(camera.Id));
            if (loadVersion != _qualityLoadVersion) return;
            AddBaseQualities();
            foreach (var mode in modes)
                Qualities.Add(new(
                    CameraQualityMode.Specific,
                    mode,
                    $"{mode.Width} × {mode.Height} · {mode.FramesPerSecond:0.##} FPS · {mode.PixelFormat}"));

            SelectedQuality = Qualities.FirstOrDefault(choice =>
                choice.QualityMode == _qualityMode
                && (choice.QualityMode != CameraQualityMode.Specific
                    || choice.Mode is not null
                    && choice.Mode.Width == _width
                    && choice.Mode.Height == _height
                    && Math.Abs(choice.Mode.FramesPerSecond - _framesPerSecond) < 0.02
                    && string.Equals(choice.Mode.PixelFormat, _pixelFormat, StringComparison.OrdinalIgnoreCase)))
                ?? Qualities[0];
            QualityStatus = Loc.Format("Ui.Step.Camera.QualityFoundCount", modes.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (loadVersion != _qualityLoadVersion) return;
            AddBaseQualities();
            SelectedQuality = Qualities[0];
            QualityStatus = Loc.Format("Ui.Step.Camera.QualityLoadFailed", ex.Message);
        }
        finally
        {
            if (loadVersion == _qualityLoadVersion)
                IsLoadingQualities = false;
        }
    }

    private void AddBaseQualities()
    {
        Qualities.Add(new(CameraQualityMode.Automatic, null, Loc.Get("Ui.Step.Camera.QualityAutomatic")));
        Qualities.Add(new(CameraQualityMode.HighestAvailable, null, Loc.Get("Ui.Step.Camera.QualityHighest")));
    }

    private void OnChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
        Changed?.Invoke();
    }

    private static StepCameraSelectionValue ReadSelection(JsonNode? value)
    {
        try
        {
            return value?.Deserialize<StepCameraSelectionValue>()
                ?? new(string.Empty, string.Empty, nameof(CameraQualityMode.Automatic), 0, 0, 0, string.Empty);
        }
        catch (JsonException)
        {
            return new(string.Empty, string.Empty, nameof(CameraQualityMode.Automatic), 0, 0, 0, string.Empty);
        }
        catch (InvalidOperationException)
        {
            return new(string.Empty, string.Empty, nameof(CameraQualityMode.Automatic), 0, 0, 0, string.Empty);
        }
    }
}

public sealed class GeneratedVisualOverlayEditorViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<SourceStepItem> _sources;
    private readonly StepInputDescriptor _detectionInputContract;
    private readonly StepInputDescriptor _textInputContract;
    private readonly Action<TextOverlayRowViewModel>? _chooseMonitor;

    public GeneratedVisualOverlayEditorViewModel(
        JsonNode? value,
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor detectionInputContract,
        StepInputDescriptor textInputContract,
        bool showDesktopOptions,
        Action<TextOverlayRowViewModel>? chooseMonitor = null)
    {
        _sources = sources;
        _detectionInputContract = detectionInputContract;
        _textInputContract = textInputContract;
        _chooseMonitor = chooseMonitor;
        ShowOverlayDesktopOptions = showDesktopOptions;
        OverlayDetectionRows.CollectionChanged += OnCollectionChanged;
        OverlayTextRows.CollectionChanged += OnCollectionChanged;
        AddOverlayDetectionCommand = new RelayCommand(AddDetection);
        AddOverlayTextCommand = new RelayCommand(AddText);

        var settings = ReadSettings(value);
        foreach (var binding in settings.DetectionResults)
            OverlayDetectionRows.Add(new(OverlayDetectionRows, sources, detectionInputContract, binding));
        foreach (var text in settings.TextResults)
            OverlayTextRows.Add(new(OverlayTextRows, sources, textInputContract, chooseMonitor, text));
    }

    public ObservableCollection<DetectionOverlayRowViewModel> OverlayDetectionRows { get; } = [];
    public ObservableCollection<TextOverlayRowViewModel> OverlayTextRows { get; } = [];
    public ICommand AddOverlayDetectionCommand { get; }
    public ICommand AddOverlayTextCommand { get; }
    public bool ShowOverlayDesktopOptions { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    public VisualOverlaySettings ToValue() => new()
    {
        DetectionResults = OverlayDetectionRows.Select(row => row.Source.ToBinding()).ToList(),
        TextResults = OverlayTextRows.Select(row => row.ToSettings()).ToList()
    };

    private void AddDetection() =>
        OverlayDetectionRows.Add(new(OverlayDetectionRows, _sources, _detectionInputContract));

    private void AddText() =>
        OverlayTextRows.Add(new(OverlayTextRows, _sources, _textInputContract, _chooseMonitor));

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (INotifyPropertyChanged item in e.OldItems)
                item.PropertyChanged -= OnRowChanged;
        if (e.NewItems is not null)
            foreach (INotifyPropertyChanged item in e.NewItems)
                item.PropertyChanged += OnRowChanged;
        OnChanged();
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e) => OnChanged();

    private void OnChanged()
    {
        PropertyChanged?.Invoke(this, new(nameof(OverlayDetectionRows)));
        PropertyChanged?.Invoke(this, new(nameof(OverlayTextRows)));
        Changed?.Invoke();
    }

    private static VisualOverlaySettings ReadSettings(JsonNode? value)
    {
        try
        {
            var settings = value?.Deserialize<VisualOverlaySettings>() ?? new();
            settings.DetectionResults ??= [];
            settings.TextResults ??= [];
            return settings;
        }
        catch (JsonException) { return new(); }
        catch (InvalidOperationException) { return new(); }
    }
}
