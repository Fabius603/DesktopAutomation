using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Media;
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
    public void ApplyMonitorSelection(GeneratedStepFieldViewModel field, int index, string deviceName)
    {
        field.IntegerValue = index;
        if (field.Descriptor.MonitorDeviceNameFieldId is { } identityFieldId)
        {
            var identity = Fields.FirstOrDefault(candidate => candidate.Descriptor.Id == identityFieldId);
            if (identity != null) identity.InputText = deviceName;
        }
    }

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
        Func<StepFieldDescriptor, JsonNode?, GeneratedAxisExpressionListEditorViewModel?>? axisExpressionListResolver = null,
        Func<StepFieldDescriptor, ResultBinding?, GeneratedResultBindingEditorViewModel?>? inputReferenceResolver = null,
        Func<StepFieldDescriptor, JsonNode?, JsonNode?>? initialValueResolver = null)
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
                .Select(field =>
                {
                    var fallback = _baseDraft.Values.TryGetValue(field.Id, out var persistedValue)
                        ? persistedValue
                        : field.DefaultValue;
                    var value = initialValueResolver?.Invoke(field, fallback) ?? fallback;
                    return new GeneratedStepFieldViewModel(
                        field,
                        value,
                        suggestionResolver?.Invoke(field),
                        choiceResolver?.Invoke(field),
                        processTargetResolver?.Invoke(field, value),
                        resultBindingResolver?.Invoke(field, value),
                        cameraResolver?.Invoke(field, value),
                        visualOverlayResolver?.Invoke(field, value),
                        roiResolver?.Invoke(field, value),
                        yoloResolver?.Invoke(field, value),
                        conditionResolver?.Invoke(field, value),
                        windowsCapabilityResolver?.Invoke(field, value),
                        screenPointResolver?.Invoke(field, value),
                        userChoiceOptionsResolver?.Invoke(field, value),
                        pointEntryListResolver?.Invoke(field, value),
                        axisExpressionListResolver?.Invoke(field, value),
                        inputReferenceResolver?.Invoke(field, step?.Inputs?.GetValueOrDefault(field.Id)));
                }));
        var fieldsById = Fields.ToDictionary(field => field.Descriptor.Id, StringComparer.Ordinal);
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
        Sections = new ObservableCollection<GeneratedStepEditorSectionViewModel>(
            definition.Descriptor.Presentation.EditorSections
                .OrderBy(section => section.Order)
                .Select(section => new GeneratedStepEditorSectionViewModel(
                    section,
                    section.FieldIds.Select(fieldId => fieldsById[fieldId]).ToArray(),
                    BuildEditorNodes(section, fieldsById))));
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
        var success = TryBuildStep(validate: true, out step, out var error);
        ValidationError = error;
        return success;
    }

    /// <summary>
    /// Materializes the current editor state for an inline job-step editing session.
    /// Incomplete or invalid input remains in the editor and is exposed through
    /// <see cref="ValidationError"/> instead of replacing the last valid job step.
    /// </summary>
    internal bool TryCreateWorkingStep(out JobStep? step) => TryCreateStep(out step);

    public JobStep? CreateUsageSnapshot() =>
        TryBuildStep(validate: false, out var step, out _) ? step : null;

    private bool TryBuildStep(bool validate, out JobStep? step, out string? error)
    {
        var draft = _baseDraft.Clone();
        var presentationFields = Fields.Concat(Fields.SelectMany(CompositeInputEditors).SelectMany(editor => editor.InputFields))
            .Concat(Sections.SelectMany(section => FindPointPairs(section.Nodes)).SelectMany(pair => new[] { pair.XField, pair.YField }))
            .Distinct().ToArray();
        if (validate)
            foreach (var field in presentationFields) field.Validation.SetMessage(null);
        void MarkField(GeneratedStepFieldViewModel field, string? message)
        {
            if (validate) field.Validation.SetMessage(message);
        }
        var incompleteNodeLabel = Sections
            .SelectMany(section => FindIncompleteReferenceLabels(section.Nodes))
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(incompleteNodeLabel))
        {
            error = Loc.Format("Ui.Step.Generated.Validation.Required", incompleteNodeLabel);
            foreach (var field in presentationFields.Where(field => field.Label == incompleteNodeLabel || IsIncomplete(field)))
                MarkField(field, error);
            foreach (var pair in Sections.SelectMany(section => FindPointPairs(section.Nodes)).Where(pair => pair.Label == incompleteNodeLabel))
            {
                MarkField(pair.XField, error);
                MarkField(pair.YField, error);
            }
            step = null;
            return false;
        }
        foreach (var field in Fields.Where(field => _editableFieldIds.Contains(field.Descriptor.Id)))
        {
            if (field.IsVisible && CompositeInputEditors(field).Any(HasIncompleteReferenceSelection))
            {
                error = Loc.Format("Ui.Step.Generated.Validation.Required", field.Label);
                var incompleteFields = CompositeInputEditors(field).SelectMany(editor => editor.InputFields).Where(IsIncomplete).ToArray();
                if (incompleteFields.Length == 0) MarkField(field, error);
                foreach (var nested in incompleteFields) MarkField(nested, error);
                step = null;
                return false;
            }
            if (!field.TryWriteValue(draft, out var inputError) && field.IsVisible)
            {
                error = inputError;
                MarkField(field, error);
                step = null;
                return false;
            }
        }

        var unresolvedPaths = GetUnresolvedAuthoringPaths();
        var issues = validate
            ? _definition.ValidateDraft(draft, new StepValidationContext(StepValidationPhase.Authoring, unresolvedPaths))
                .Where(candidate => candidate.Severity == StepValidationSeverity.Error).ToArray()
            : [];
        if (issues.Length > 0)
        {
            foreach (var issue in issues)
                foreach (var field in presentationFields.Where(field => field.Descriptor.Id == issue.FieldId || issue.DependencyFieldIds?.Contains(field.Descriptor.Id) == true))
                    MarkField(field, FormatIssue(issue));
            error = FormatIssue(issues[0]);
            step = null;
            return false;
        }

        error = null;
        step = _definition.ApplyDraft(draft);
        foreach (var field in Fields.Where(field => field.InputReferenceEditor is not null))
            step.Inputs[field.Descriptor.Id] = field.InputReferenceEditor!.Picker.ToBinding();
        foreach (var pair in Sections.SelectMany(section => FindPointPairs(section.Nodes))
                     .Where(pair => pair.WholeValueSource?.UsesReference == true))
        {
            step.Inputs[pair.XField.Descriptor.Id] = new ResultBinding();
            step.Inputs[pair.YField.Descriptor.Id] = new ResultBinding();
        }
        foreach (var composite in Fields.SelectMany(CompositeInputEditors))
            foreach (var (key, binding) in composite.InputBindings)
                ValueBindingTree.Set(step.Inputs, key, binding);
        ValueBindingTree.ApplySchemas(step.Inputs, _definition.Descriptor.Fields);
        if (_existingStep is not null)
        {
            step.Id = _existingStep.Id;
            step.IsEnabled = _existingStep.IsEnabled;
            step.IsBreakpoint = _existingStep.IsBreakpoint;
        }
        return true;
    }

    private IReadOnlySet<string> GetUnresolvedAuthoringPaths()
    {
        var unresolved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in Fields)
        {
            if (field.InputReferenceEditor is not null
                && field.Descriptor.ValueKind != StepValueKind.ResultBinding
                && field.UsesExternalInputReference)
                unresolved.Add(field.Descriptor.Id);
            if (field.WholeValueSource?.UsesReference == true)
                unresolved.Add(field.Descriptor.Id);
            foreach (var composite in CompositeInputEditors(field))
                foreach (var (path, binding) in composite.InputBindings)
                    if (binding.IsConfigured && !IsAuthoringResolved(binding))
                        unresolved.Add(path);
        }
        return unresolved;
    }

    private static bool IsAuthoringResolved(ResultBinding binding)
    {
        if (binding.HasProviderReference
            && !string.Equals(binding.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal))
            return false;
        if (binding.TryGetStepResult(out _))
            return false;
        return (binding.Members?.Values.All(IsAuthoringResolved) ?? true)
               && (binding.Items?.All(IsAuthoringResolved) ?? true);
    }

    private static IEnumerable<GeneratedStepPointFieldPairViewModel> FindPointPairs(
        IEnumerable<GeneratedStepEditorNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is GeneratedStepPointFieldPairViewModel pair)
                yield return pair;
            if (node is GeneratedStepChoiceGroupViewModel group)
                foreach (var nested in group.Branches.SelectMany(branch => FindPointPairs(branch.Children)))
                    yield return nested;
        }
    }

    private static IEnumerable<string> FindIncompleteReferenceLabels(
        IEnumerable<GeneratedStepEditorNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is GeneratedStepFieldNodeViewModel { Field.IsVisible: true } fieldNode
                && IsIncomplete(fieldNode.Field))
                yield return fieldNode.Field.Label;
            if (node is GeneratedStepPointFieldPairViewModel { XField.IsVisible: true } pair)
            {
                if (pair.WholeValueSource is { UsesReference: true, Picker.IsConfigured: false })
                    yield return pair.Label;
                else
                {
                    if (IsIncomplete(pair.XField)) yield return pair.XField.Label;
                    if (IsIncomplete(pair.YField)) yield return pair.YField.Label;
                }
            }
            if (node is GeneratedStepChoiceGroupViewModel group)
                foreach (var label in FindIncompleteReferenceLabels(
                             group.Branches.SelectMany(branch => branch.Children)))
                    yield return label;
        }
    }

    private static IEnumerable<IGeneratedCompositeInputEditor> CompositeInputEditors(GeneratedStepFieldViewModel field)
    {
        if (field.ProcessTargetEditor is IGeneratedCompositeInputEditor process) yield return process;
        if (field.RoiEditor is IGeneratedCompositeInputEditor roi) yield return roi;
        if (field.PointEntryListEditor is IGeneratedCompositeInputEditor points) yield return points;
        if (field.ScreenPointEditor is IGeneratedCompositeInputEditor screenPoint) yield return screenPoint;
        if (field.YoloEditor is IGeneratedCompositeInputEditor yolo) yield return yolo;
        if (field.UserChoiceOptionsEditor is IGeneratedCompositeInputEditor choices) yield return choices;
        if (field.VisualOverlayEditor is IGeneratedCompositeInputEditor overlay) yield return overlay;
    }

    private static bool HasIncompleteReferenceSelection(IGeneratedCompositeInputEditor editor) => editor switch
    {
        GeneratedScreenPointEditorViewModel screenPoint => screenPoint.WholeValueSource.UsesReference
            ? !screenPoint.WholeValueSource.Picker.IsConfigured
            : IsIncomplete(screenPoint.MonitorField) || IsIncomplete(screenPoint.XField) || IsIncomplete(screenPoint.YField),
        GeneratedUserChoiceOptionsEditorViewModel choices => choices.Options.Any(option =>
            IsIncomplete(option.LabelField) || IsIncomplete(option.ValueField)),
        GeneratedPointEntryListEditorViewModel points => points.Points.Any(point => point.WholeValueSource.UsesReference
            ? !point.PointsSource.IsConfigured
            : IsIncomplete(point.ManualXField) || IsIncomplete(point.ManualYField)),
        GeneratedRoiEditorViewModel roi => roi.WholeValueSource.UsesReference
            ? !roi.DetectionDynamicRoiSource.IsConfigured
            : IsIncomplete(roi.XField) || IsIncomplete(roi.YField)
              || IsIncomplete(roi.WidthField) || IsIncomplete(roi.HeightField),
        GeneratedYoloEditorViewModel yolo => IsIncomplete(yolo.ModelField) || IsIncomplete(yolo.ClassField),
        GeneratedProcessTargetEditorViewModel process => process.WholeValueSource.UsesReference
            ? !process.Picker.IsConfigured
            : IsIncomplete(process.ProcessNameField) || IsIncomplete(process.ExecutablePathField)
              || IsIncomplete(process.WindowTitleField),
        GeneratedVisualOverlayEditorViewModel overlay =>
            overlay.OverlayDetectionRows.Any(row => IsIncomplete(row.SourceField))
            || overlay.OverlayTextRows.Any(row =>
                IsIncomplete(row.TextSourceField) || IsIncomplete(row.FontSizeField)
                || IsIncomplete(row.FontColorField) || IsIncomplete(row.OpacityField)
                || IsIncomplete(row.DesktopIndexField) || IsIncomplete(row.OffsetXField)
                || IsIncomplete(row.OffsetYField) || IsIncomplete(row.DurationField)
                || IsIncomplete(row.ClearOnJobEndField)),
        _ => false
    };

    private static bool IsIncomplete(GeneratedStepFieldViewModel? field) =>
        field is { ShowsInputSourcePicker: true, InputReferenceEditor.Picker.IsConfigured: false };

    private void OnFieldChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GeneratedStepFieldViewModel.IsInlineStepValue)
            or nameof(GeneratedStepFieldViewModel.UsesExternalInputReference)
            or nameof(GeneratedStepFieldViewModel.CanEditInlineStepValue)
            or nameof(GeneratedStepFieldViewModel.RequiresInlineEditChoice)
            or nameof(GeneratedStepFieldViewModel.ShowsDirectInput)
            or nameof(GeneratedStepFieldViewModel.ShowsInputSourcePicker)
            or nameof(GeneratedStepFieldViewModel.BooleanValue)
            or nameof(GeneratedStepFieldViewModel.IntegerValue)
            or nameof(GeneratedStepFieldViewModel.NumberValue)
            or nameof(GeneratedStepFieldViewModel.DateTimeValue)
            or nameof(GeneratedStepFieldViewModel.ColorValue)
            or nameof(GeneratedStepFieldViewModel.FilePreview)
            or nameof(GeneratedStepFieldViewModel.HasFilePreview))
            return;
        ValidationError = null;
        RefreshVisibility();
        Changed?.Invoke();
    }

    private void RefreshVisibility()
    {
        var fieldsById = Fields.ToDictionary(field => field.Descriptor.Id, StringComparer.Ordinal);
        var conventionallyVisible = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var field in Fields)
        {
            var visible = StepEditorActivity.IsVisible(field.Descriptor, rule =>
                RuleMatches(rule, fieldsById));
            conventionallyVisible[field.Descriptor.Id] = visible;
        }
        var structurallyActive = StepEditorActivity.GetActiveFieldIds(
            Descriptor,
            fieldId => fieldsById.GetValueOrDefault(fieldId)?.InputText,
            fieldId => conventionallyVisible.GetValueOrDefault(fieldId));
        foreach (var field in Fields)
            field.SetVisibility(conventionallyVisible[field.Descriptor.Id]
                                && structurallyActive.Contains(field.Descriptor.Id));
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
        return JobValidationErrorLocalizer.FormatIssue(issue, label);
    }

    public void RefreshSuggestions(Func<StepFieldDescriptor, IEnumerable<string>?> suggestionResolver)
    {
        foreach (var field in Fields)
            field.SetSuggestions(suggestionResolver(field.Descriptor));
    }

    private static IReadOnlyList<GeneratedStepEditorNodeViewModel> BuildEditorNodes(
        StepEditorSectionDescriptor section,
        IReadOnlyDictionary<string, GeneratedStepFieldViewModel> fieldsById)
    {
        var descriptors = section.EditorNodes
            ?? section.FieldIds.Select(id => (StepEditorNodeDescriptor)new StepFieldNodeDescriptor(id)).ToArray();
        return descriptors.Select(descriptor => BuildEditorNode(descriptor, fieldsById)).ToArray();
    }

    private static GeneratedStepEditorNodeViewModel BuildEditorNode(
        StepEditorNodeDescriptor descriptor,
        IReadOnlyDictionary<string, GeneratedStepFieldViewModel> fieldsById) => descriptor switch
        {
            StepFieldNodeDescriptor field => new GeneratedStepFieldNodeViewModel(fieldsById[field.FieldId]),
            StepPointFieldPairDescriptor pair => new GeneratedStepPointFieldPairViewModel(
                fieldsById[pair.XFieldId], fieldsById[pair.YFieldId],
                string.IsNullOrWhiteSpace(pair.LabelKey) ? string.Empty : Loc.Get(pair.LabelKey),
                string.IsNullOrWhiteSpace(pair.SourceFieldId) ? null : fieldsById[pair.SourceFieldId],
                string.IsNullOrWhiteSpace(pair.ReferenceFieldId) ? null : fieldsById[pair.ReferenceFieldId]),
            StepChoiceGroupDescriptor group => new GeneratedStepChoiceGroupViewModel(
                fieldsById[group.SelectionFieldId],
                group.Branches.Select(branch => new GeneratedStepChoiceBranchViewModel(
                    branch.Value,
                    Loc.Get(branch.LabelKey),
                    branch.Children.Select(child => BuildEditorNode(child, fieldsById)).ToArray(),
                    string.IsNullOrWhiteSpace(branch.DescriptionKey) ? string.Empty : Loc.Get(branch.DescriptionKey))).ToArray()),
            _ => throw new InvalidOperationException($"Unknown editor node '{descriptor.GetType().Name}'.")
        };
}

public sealed class GeneratedStepEditorSectionViewModel
{
    public GeneratedStepEditorSectionViewModel(
        StepEditorSectionDescriptor descriptor,
        IReadOnlyList<GeneratedStepFieldViewModel> fields,
        IReadOnlyList<GeneratedStepEditorNodeViewModel> nodes)
    {
        Descriptor = descriptor;
        Fields = fields;
        Nodes = nodes;
    }

    public StepEditorSectionDescriptor Descriptor { get; }
    public IReadOnlyList<GeneratedStepFieldViewModel> Fields { get; }
    public IReadOnlyList<GeneratedStepEditorNodeViewModel> Nodes { get; }
    public string Title => string.IsNullOrWhiteSpace(Descriptor.TitleKey)
        ? string.Empty
        : Loc.Get(Descriptor.TitleKey);
    public bool IsCollapsible => Descriptor.Collapsible;
    public bool IsInitiallyExpanded => Descriptor.InitiallyExpanded;
}

public abstract class GeneratedStepEditorNodeViewModel
{
}

public sealed class GeneratedStepFieldNodeViewModel(GeneratedStepFieldViewModel field)
    : GeneratedStepEditorNodeViewModel
{
    public GeneratedStepFieldViewModel Field { get; } = field;
}

public sealed class GeneratedStepPointFieldPairViewModel : GeneratedStepEditorNodeViewModel, INotifyPropertyChanged
{
    public GeneratedStepPointFieldPairViewModel(GeneratedStepFieldViewModel xField,
        GeneratedStepFieldViewModel yField, string label,
        GeneratedStepFieldViewModel? sourceField = null, GeneratedStepFieldViewModel? referenceField = null)
    {
        XField = xField;
        YField = yField;
        Label = label;
        WholeValueSource = CreateWholeValueSource(xField, yField, sourceField, referenceField);
        if (WholeValueSource is not null)
            WholeValueSource.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(GeneratedWholeValueSourceViewModel.ShowsIndividualValues)
                    or nameof(GeneratedWholeValueSourceViewModel.UsesReference))
                {
                    PropertyChanged?.Invoke(this, new(nameof(ShowsIndividualValues)));
                    PropertyChanged?.Invoke(this, new(nameof(UsesWholeValueReference)));
                }
            };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public GeneratedStepFieldViewModel XField { get; }
    public GeneratedStepFieldViewModel YField { get; }
    public string Label { get; }
    public bool HasLabel => !string.IsNullOrWhiteSpace(Label);
    public GeneratedWholeValueSourceViewModel? WholeValueSource { get; }
    public bool HasWholeValueSource => WholeValueSource is not null;
    public bool ShowsIndividualValues => WholeValueSource?.ShowsIndividualValues ?? true;
    public bool UsesWholeValueReference => WholeValueSource?.UsesReference ?? false;

    private static GeneratedWholeValueSourceViewModel? CreateWholeValueSource(
        GeneratedStepFieldViewModel x,
        GeneratedStepFieldViewModel y,
        GeneratedStepFieldViewModel? source,
        GeneratedStepFieldViewModel? reference)
    {
        if (source is null || reference?.InputReferenceEditor is null) return null;
        var whole = new GeneratedWholeValueSourceViewModel(
            reference.InputReferenceEditor.Picker,
            !string.Equals(source.InputText, "Manual", StringComparison.OrdinalIgnoreCase));
        whole.Changed += () => source.InputText = whole.UsesReference ? "JobResult" : "Manual";
        x.PropertyChanged += (_, args) => ClearWholeValueForSemanticFieldChange(whole, args);
        y.PropertyChanged += (_, args) => ClearWholeValueForSemanticFieldChange(whole, args);
        return whole;
    }

    private static void ClearWholeValueForSemanticFieldChange(
        GeneratedWholeValueSourceViewModel whole,
        PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(GeneratedStepFieldViewModel.InputText)
            or nameof(GeneratedStepFieldViewModel.InputReferenceEditor))
            whole.UsesReference = false;
    }
}

public sealed class GeneratedStepChoiceBranchViewModel(
    string value,
    string label,
    IReadOnlyList<GeneratedStepEditorNodeViewModel> children,
    string description)
{
    public string Value { get; } = value;
    public string Label { get; } = label;
    public string Description { get; } = description;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public IReadOnlyList<GeneratedStepEditorNodeViewModel> Children { get; } = children;
}

public sealed class GeneratedStepChoiceGroupViewModel : GeneratedStepEditorNodeViewModel, INotifyPropertyChanged
{
    private readonly GeneratedStepFieldViewModel _selectionField;

    public GeneratedStepChoiceGroupViewModel(
        GeneratedStepFieldViewModel selectionField,
        IReadOnlyList<GeneratedStepChoiceBranchViewModel> branches)
    {
        _selectionField = selectionField;
        Branches = branches;
        _selectionField.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(GeneratedStepFieldViewModel.SelectedEnumValue)
                or nameof(GeneratedStepFieldViewModel.SelectedEnumOption)
                or nameof(GeneratedStepFieldViewModel.InputText))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBranch)));
            if (args.PropertyName == nameof(GeneratedStepFieldViewModel.IsVisible))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Label => _selectionField.Label;
    public string Description => _selectionField.Description;
    public bool HasDescription => _selectionField.HasDescription;
    public bool IsVisible => _selectionField.IsVisible;
    public IReadOnlyList<GeneratedStepChoiceBranchViewModel> Branches { get; }

    public GeneratedStepChoiceBranchViewModel? SelectedBranch
    {
        get => Branches.FirstOrDefault(branch =>
            string.Equals(branch.Value, _selectionField.SelectedEnumValue, StringComparison.Ordinal));
        set
        {
            if (value is null || ReferenceEquals(SelectedBranch, value)) return;
            _selectionField.SelectedEnumValue = value.Value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBranch)));
        }
    }
}

public sealed class GeneratedStepFieldViewModel : INotifyPropertyChanged
{
    public const string BooleanDropdownEditorHint = "condition-boolean-dropdown";
    public const string ConditionEnumDirectValueEditorHint = "condition-enum-direct-value";

    private string _inputText;
    private GeneratedStepChoiceOptionViewModel? _selectedChoice;
    private bool _isVisible = true;
    private string? _filePreviewPath;
    private ImageSource? _filePreview;

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
        GeneratedAxisExpressionListEditorViewModel? axisExpressionListEditor = null,
        GeneratedResultBindingEditorViewModel? inputReferenceEditor = null)
    {
        Descriptor = descriptor;
        _inputText = FormatValue(value, descriptor.ValueKind);
        if (string.IsNullOrWhiteSpace(_inputText)
            && descriptor.DirectoryPickerOptions is { } directoryOptions)
            _inputText = ResolveSuggestedDirectory(directoryOptions);
        Suggestions = new ObservableCollection<string>(suggestions ?? []);
        Choices = new ObservableCollection<GeneratedStepChoiceOptionViewModel>(choices ?? []);
        EnumOptions = BuildEnumOptions(descriptor).ToArray();
        ProcessTargetEditor = processTargetEditor;
        if (ProcessTargetEditor is not null)
            ProcessTargetEditor.Changed += OnProcessTargetChanged;
        ResultBindingEditor = resultBindingEditor;
        if (ResultBindingEditor is not null)
            ResultBindingEditor.Changed += OnResultBindingChanged;
        InputReferenceEditor = inputReferenceEditor;
        if (InputReferenceEditor is not null)
        {
            InputReferenceEditor.Changed += OnInputReferenceChanged;
            InputReferenceEditor.DisplayStateChanged += OnInputReferenceDisplayStateChanged;
        }
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
        _selectedChoice = (UsesChoicePicker ? FindChoice(value) : null)
            ?? (Descriptor.Required && UsesChoicePicker ? Choices.FirstOrDefault() : null);
        if (_selectedChoice is not null)
            _inputText = JsonSerializer.SerializeToNode(_selectedChoice.Value)?.ToJsonString() ?? string.Empty;
        LoadInlineStepValue();
        if (!SupportsDirectValue && InputReferenceEditor is not null && !InputReferenceEditor.Picker.IsConfigured)
            InputReferenceEditor.Picker.SelectSourceKind(StepInputSourceKind.StepResult);
        UseVariableCommand = new RelayCommand(() =>
        {
            InputReferenceEditor?.Picker.SelectSourceKind(StepInputSourceKind.JobVariable);
            NotifyInputMode();
        });
        UseDirectValueCommand = new RelayCommand(() =>
        {
            InputReferenceEditor?.Picker.UseDirectValueCommand.Execute(null);
            LoadInlineStepValue();
            NotifyInputMode();
        }, () => SupportsDirectValue
                 && InputReferenceEditor?.Picker.UseDirectValueCommand.CanExecute(null) == true);
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

    private static IEnumerable<GeneratedStepEnumOptionViewModel> BuildEnumOptions(
        StepFieldDescriptor descriptor)
    {
        return StepEnumRules.GetOptions(descriptor)
            .Select(option => new GeneratedStepEnumOptionViewModel(
                option.Value,
                EnumValueLocalization.ForStepOption(option)));
    }

    private GeneratedStepEnumOptionViewModel? ResolveEnumOption(string? value) =>
        EnumOptions.FirstOrDefault(option =>
            string.Equals(option.Value, value, StringComparison.Ordinal));

    public event PropertyChangedEventHandler? PropertyChanged;

    public GeneratedFieldValidationState Validation { get; } = new();
    public StepFieldDescriptor Descriptor { get; }
    public ObservableCollection<string> Suggestions { get; }
    public ObservableCollection<GeneratedStepChoiceOptionViewModel> Choices { get; }
    public IReadOnlyList<GeneratedStepEnumOptionViewModel> EnumOptions { get; }
    public IReadOnlyList<GeneratedStepBooleanOptionViewModel> BooleanOptions { get; } =
    [
        new(true, "Ui.Job.Variables.Boolean.True"),
        new(false, "Ui.Job.Variables.Boolean.False")
    ];
    public GeneratedProcessTargetEditorViewModel? ProcessTargetEditor { get; }
    public GeneratedResultBindingEditorViewModel? ResultBindingEditor { get; }
    public GeneratedResultBindingEditorViewModel? InputReferenceEditor { get; }
    public ICommand UseVariableCommand { get; }
    public ICommand UseDirectValueCommand { get; }
    public bool SupportsDirectValue => InputReferenceEditor is not null
                                       && (Descriptor.AllowsDirectValue
                                           ?? Descriptor.ValueKind != StepValueKind.ResultBinding);
    public bool IsInlineStepValue => SupportsDirectValue
                                     && InputReferenceEditor?.Picker.IsStepValue == true;
    public bool UsesExternalInputReference => InputReferenceEditor is not null && !IsInlineStepValue;
    public bool CanEditInlineStepValue => IsInlineStepValue
                                          && InputReferenceEditor?.Picker.CanEditStepValueInline == true;
    public bool RequiresInlineEditChoice => IsInlineStepValue
                                            && InputReferenceEditor?.Picker.RequiresInlineEditChoice == true;
    public bool ShowsDirectInput => IsInlineStepValue;
    public bool ShowsInputSourcePicker => !IsInlineStepValue;
    public bool ShowsInputSourceSelector => InputReferenceEditor?.Picker.CanSwitchSource == true
        && (!UsesConditionEnumDirectValue || InputReferenceEditor.Picker.IsStepValue == false);
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
    public GeneratedWholeValueSourceViewModel? WholeValueSource =>
        ProcessTargetEditor?.WholeValueSource
        ?? RoiEditor?.WholeValueSource
        ?? ScreenPointEditor?.WholeValueSource;
    public bool HasWholeValueSource => WholeValueSource is not null;
    public string Label => Loc.Get(Descriptor.LabelKey);
    public string Description => string.IsNullOrWhiteSpace(Descriptor.DescriptionKey)
        ? string.Empty
        : Loc.Get(Descriptor.DescriptionKey);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    private StepValueKind EffectiveValueKind => Descriptor.ValueKind == StepValueKind.ResultBinding
        && IsInlineStepValue
        && InputReferenceEditor?.Picker.SelectedJobVariable is { } variable
            ? MapDirectValueKind(variable.ValueKind)
            : Descriptor.ValueKind;
    public bool IsBoolean => EffectiveValueKind == StepValueKind.Boolean;
    public bool UsesBooleanDropdown => IsBoolean
        && string.Equals(Descriptor.EditorHint, BooleanDropdownEditorHint, StringComparison.Ordinal);
    public bool UsesDateTimePicker => EffectiveValueKind == StepValueKind.DateTime;
    public bool UsesMonitorPicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.MonitorPicker,
        StringComparison.Ordinal);
    public bool UsesFilePicker => string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.FilePicker,
        StringComparison.Ordinal);
    public bool ShowsFilePreview => Descriptor.FilePickerOptions?.ShowPreview == true;
    public ImageSource? FilePreview
    {
        get
        {
            if (!ShowsFilePreview) return null;
            if (string.Equals(_filePreviewPath, _inputText, StringComparison.Ordinal)) return _filePreview;
            _filePreviewPath = _inputText;
            _filePreview = WpfImagePreviewLoader.TryLoad(_inputText);
            return _filePreview;
        }
    }
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
    public bool UsesEnumPicker => EffectiveValueKind == StepValueKind.Enum;
    public bool UsesConditionEnumDirectValue => UsesEnumPicker
        && string.Equals(Descriptor.EditorHint, ConditionEnumDirectValueEditorHint, StringComparison.Ordinal);
    public bool UsesValueReferencePicker => !IsInlineStepValue && (string.Equals(
        Descriptor.EditorHint,
        StepEditorHints.ResultBindingPicker,
        StringComparison.Ordinal)
        || string.Equals(
            Descriptor.EditorHint,
            StepEditorHints.ValueReferencePicker,
            StringComparison.Ordinal));
    public bool UsesInputReference => InputReferenceEditor is not null
                                      && !UsesProcessTargetPicker
                                      && !UsesRoiPicker
                                      && !UsesPointEntryList
                                      && !UsesScreenPointPicker
                                      && !UsesYoloPicker
                                      && !UsesUserChoiceOptions;
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
    public bool UsesSingleLineText => Descriptor.EditorHint == StepEditorHints.SingleLineText;
    public bool UsesEmojiText => Descriptor.EditorHint == StepEditorHints.EmojiText;
    public bool UsesColorPicker => EffectiveValueKind == StepValueKind.Color;
    public bool UsesMultilineTextInput => EffectiveValueKind == StepValueKind.MultilineText && !UsesEmojiText;
    public bool UsesTextInput => !IsBoolean && !UsesDateTimePicker && !UsesMonitorPicker && !UsesFilePicker && !UsesDirectoryPicker
        && !UsesFileOrFolderPicker && !UsesColorPicker && !UsesMultilineTextInput && !UsesCameraPicker
        && !UsesVisualOverlay && !UsesRoiPicker && !UsesYoloPicker && !UsesConditionEditor
        && !UsesWindowsCapabilityPicker
        && !UsesScreenPointPicker && !UsesUserChoiceOptions && !UsesPointEntryList && !UsesAxisExpressionList
        && !UsesSingleLineText && !UsesEmojiText
        && !UsesSuggestions && !UsesSuggestionFilePicker && !UsesChoicePicker
        && !UsesProcessTargetPicker && !UsesEnumPicker && !UsesValueReferencePicker
        && !UsesPercentagePicker;
    public bool IsVisible => _isVisible;

    public string? SelectedEnumValue
    {
        get => ResolveEnumOption(_inputText)?.Value;
        set
        {
            if (string.Equals(SelectedEnumValue, value, StringComparison.Ordinal)) return;
            if (value is null && !string.IsNullOrWhiteSpace(_inputText)) return;
            if (value is not null && !StepEnumRules.IsKnownToken(Descriptor, value)) return;
            _inputText = value ?? string.Empty;
            StoreInlineStepValue();
            InvalidateFilePreview();
            NotifyEnumState();
        }
    }

    public GeneratedStepEnumOptionViewModel? SelectedEnumOption
    {
        get => ResolveEnumOption(_inputText);
        set => SelectedEnumValue = value?.Value;
    }

    public bool HasInvalidEnumValue => UsesEnumPicker
                                       && !string.IsNullOrWhiteSpace(_inputText)
                                       && SelectedEnumValue is null;
    public string InvalidEnumValue => HasInvalidEnumValue ? _inputText : string.Empty;
    public string EnumValidationMessage => HasInvalidEnumValue
        ? Loc.Format("Ui.Step.Generated.Validation.UnknownEnum", Label, InvalidEnumValue)
        : string.Empty;

    private void NotifyEnumState()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEnumValue)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEnumOption)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasInvalidEnumValue)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InvalidEnumValue)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnumValidationMessage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputText)));
        if (ShowsFilePreview)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePreview)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasFilePreview)));
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
            StoreInlineStepValue();
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

    public GeneratedStepBooleanOptionViewModel SelectedBooleanOption
    {
        get => BooleanOptions.First(option => option.Value == BooleanValue);
        set
        {
            if (value is not null)
                BooleanValue = value.Value;
        }
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

    public DateTime? DateTimeValue
    {
        get => DateTime.TryParse(_inputText, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var value) ? value : null;
        set => InputText = value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public Color ColorValue
    {
        get => WpfColorParser.TryParse(_inputText, out var color) ? color : Colors.White;
        set => InputText = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (_inputText == value) return;
            _inputText = value;
            StoreInlineStepValue();
            InvalidateFilePreview();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputText)));
            if (UsesEnumPicker)
                NotifyEnumState();
            if (IsBoolean)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BooleanValue)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBooleanOption)));
            }
            if (EffectiveValueKind is StepValueKind.Integer or StepValueKind.Duration)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IntegerValue)));
            if (EffectiveValueKind == StepValueKind.Number)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberValue)));
            if (EffectiveValueKind == StepValueKind.DateTime)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DateTimeValue)));
            if (EffectiveValueKind == StepValueKind.Color)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorValue)));
        }
    }

    public bool TryWriteValue(StepDraft draft, out string? error)
    {
        error = null;
        if (UsesEnumPicker && (!UsesInputReference || IsInlineStepValue) && HasInvalidEnumValue)
        {
            error = EnumValidationMessage;
            return false;
        }
        if (UsesConditionEditor && ConditionEditor is { IsValid: false })
        {
            error = Loc.Get("Ui.Step.Generated.Validation.Invalid");
            return false;
        }
        if (InputReferenceEditor is not null)
        {
            if (!InputReferenceEditor.Picker.IsConfigured)
            {
                if (Descriptor.Required && IsVisible)
                {
                    error = Loc.Format("Ui.Step.Generated.Validation.Required", Label);
                    return false;
                }
                return true;
            }
            if (Descriptor.ValueKind == StepValueKind.ResultBinding)
                draft.Values[Descriptor.Id] = JsonSerializer.SerializeToNode(
                    InputReferenceEditor.Picker.ToBinding());
            else if (InputReferenceEditor.Picker.IsStepValue)
            {
                var directValue = CurrentDirectValue();
                draft.Values[Descriptor.Id] = directValue?.DeepClone();
                if (InputReferenceEditor.Picker.SelectedJobVariable is { } variable)
                {
                    variable.Value = directValue?.DeepClone();
                    InputReferenceEditor.Picker.RefreshSelectedValue();
                }
            }
            return true;
        }
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
        if (UsesValueReferencePicker && ResultBindingEditor is not null)
        {
            if (Descriptor.Required && IsVisible && !ResultBindingEditor.Picker.IsConfigured)
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
        var comparison = UsesEnumPicker ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return expected is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text)
            ? string.Equals(InputText, text, comparison)
            : string.Equals(InputText, expected.ToJsonString(), comparison);
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
        if (value is not JsonValue jsonValue)
            return value.ToJsonString();

        return kind switch
        {
            StepValueKind.Integer or StepValueKind.Duration => FormatInteger(jsonValue),
            StepValueKind.Number => FormatNumber(jsonValue),
            StepValueKind.Boolean when jsonValue.TryGetValue<bool>(out var flag) => flag.ToString(),
            _ when jsonValue.TryGetValue<string>(out var text) => text,
            _ => value.ToJsonString()
        };
    }

    private static string FormatInteger(JsonValue value)
    {
        if (value.TryGetValue<int>(out var integer)) return integer.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<long>(out var longInteger)) return longInteger.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<decimal>(out var decimalNumber)) return decimalNumber.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<double>(out var doubleNumber)) return doubleNumber.ToString(CultureInfo.CurrentCulture);
        return value.ToJsonString();
    }

    private static string FormatNumber(JsonValue value)
    {
        if (value.TryGetValue<decimal>(out var decimalNumber)) return decimalNumber.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<double>(out var doubleNumber)) return doubleNumber.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<float>(out var floatNumber)) return floatNumber.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<long>(out var integer)) return integer.ToString(CultureInfo.CurrentCulture);
        return value.ToJsonString();
    }

    private void InvalidateFilePreview()
    {
        _filePreviewPath = null;
        _filePreview = null;
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
        StoreInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProcessTargetEditor)));
    }

    private void OnResultBindingChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultBindingEditor)));
    }

    private void OnInputReferenceChanged()
    {
        LoadInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputReferenceEditor)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInlineStepValue)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsesExternalInputReference)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditInlineStepValue)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RequiresInlineEditChoice)));
        NotifyInputMode();
    }

    private void OnInputReferenceDisplayStateChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditInlineStepValue)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RequiresInlineEditChoice)));
    }

    private void NotifyInputMode()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowsDirectInput)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowsInputSourcePicker)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowsInputSourceSelector)));
    }

    private void LoadInlineStepValue()
    {
        if (!IsInlineStepValue || InputReferenceEditor?.Picker.SelectedJobVariable is not { } variable) return;
        if (variable.Value is null && CurrentDirectValue() is { } initialValue)
        {
            variable.Value = initialValue;
            InputReferenceEditor.Picker.RefreshSelectedValue();
            return;
        }
        var formattedValue = FormatValue(variable.Value, EffectiveValueKind);
        if (string.Equals(_inputText, formattedValue, StringComparison.Ordinal)) return;
        _inputText = formattedValue;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BooleanValue)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBooleanOption)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IntegerValue)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberValue)));
        NotifyEnumState();
    }

    private void StoreInlineStepValue()
    {
        if (!IsInlineStepValue || InputReferenceEditor?.Picker.SelectedJobVariable is not { } variable) return;
        variable.Value = CurrentDirectValue();
        InputReferenceEditor.Picker.RefreshSelectedValue();
    }

    private JsonNode? CurrentDirectValue()
    {
        if (UsesChoicePicker)
            return SelectedChoice is null ? null : JsonSerializer.SerializeToNode(SelectedChoice.Value);
        if (UsesProcessTargetPicker && ProcessTargetEditor is not null)
            return JsonSerializer.SerializeToNode(ProcessTargetEditor.ToValue());
        if (UsesCameraPicker && CameraEditor is not null)
            return JsonSerializer.SerializeToNode(CameraEditor.ToValue());
        if (UsesVisualOverlay && VisualOverlayEditor is not null)
            return JsonSerializer.SerializeToNode(VisualOverlayEditor.ToValue());
        if (UsesRoiPicker && RoiEditor is not null)
            return JsonSerializer.SerializeToNode(RoiEditor.ToValue());
        if (UsesYoloPicker && YoloEditor is not null)
            return JsonSerializer.SerializeToNode(YoloEditor.ToValue());
        if (UsesConditionEditor && ConditionEditor is not null)
            return JsonSerializer.SerializeToNode(ConditionEditor.ToValue());
        if (UsesWindowsCapabilityPicker && WindowsCapabilityEditor is not null)
            return JsonSerializer.SerializeToNode(WindowsCapabilityEditor.ToValue());
        if (UsesScreenPointPicker && ScreenPointEditor is not null) return ScreenPointEditor.ToNode();
        if (UsesUserChoiceOptions && UserChoiceOptionsEditor is not null) return UserChoiceOptionsEditor.ToNode();
        if (UsesPointEntryList && PointEntryListEditor is not null) return PointEntryListEditor.ToNode();
        if (UsesAxisExpressionList && AxisExpressionListEditor is not null) return AxisExpressionListEditor.ToNode();

        var text = _inputText.Trim();
        return EffectiveValueKind switch
        {
            StepValueKind.Integer or StepValueKind.Duration
                when int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var integer)
                => JsonValue.Create(integer),
            StepValueKind.Number
                when decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var number)
                => JsonValue.Create(number),
            StepValueKind.Boolean when bool.TryParse(text, out var flag) => JsonValue.Create(flag),
            StepValueKind.DateTime when DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dateTime) => JsonValue.Create(dateTime.ToUniversalTime()),
            _ => JsonValue.Create(_inputText)
        };
    }

    private static StepValueKind MapDirectValueKind(ResultValueKind kind) => kind switch
    {
        ResultValueKind.Boolean => StepValueKind.Boolean,
        ResultValueKind.Integer => StepValueKind.Integer,
        ResultValueKind.Number => StepValueKind.Number,
        ResultValueKind.DateTime => StepValueKind.DateTime,
        ResultValueKind.Color => StepValueKind.Color,
        ResultValueKind.FilePath => StepValueKind.FilePath,
        ResultValueKind.Enum => StepValueKind.Enum,
        ResultValueKind.Point => StepValueKind.Point,
        ResultValueKind.Rectangle => StepValueKind.Rectangle,
        _ => StepValueKind.Text
    };

    private void OnCameraChanged()
    {
        StoreInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraEditor)));
    }

    private void OnVisualOverlayChanged()
    {
        StoreInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisualOverlayEditor)));
    }

    private void OnRoiChanged()
    {
        StoreInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RoiEditor)));
    }

    private void OnYoloChanged()
    {
        StoreInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YoloEditor)));
    }

    private void OnConditionChanged()
    {
        StoreInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConditionEditor)));
    }

    private void OnWindowsCapabilityChanged()
    {
        StoreInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowsCapabilityEditor)));
    }

    private bool WriteCustom(StepDraft draft, IGeneratedValueEditor editor)
    {
        draft.Values[Descriptor.Id] = editor.ToNode();
        return true;
    }

    private void OnCustomValueChanged()
    {
        StoreInlineStepValue();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputText)));
    }
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
public sealed class GeneratedStepBooleanOptionViewModel(bool value, string labelKey)
{
    public bool Value { get; } = value;
    public string Label => Loc.Get(labelKey);
}

public sealed class GeneratedConditionEditorViewModel : INotifyPropertyChanged
{
    private ConditionMatchMode _matchMode = ConditionMatchMode.All;
    private readonly IReadOnlyList<SourceStepItem> _sources;
    private readonly IReadOnlyList<JobVariable> _variables;
    private readonly IReadOnlyList<ValueProviderSourceDescriptor> _providerSources;
    private readonly string _inputKeyPrefix;
    private readonly Func<string, StepValueKind, JsonNode?, ResultPropertyDescriptor?, ResultBinding?, GeneratedResultBindingEditorViewModel>? _nestedInputResolver;
    private readonly ValueReferenceSourceCatalog? _sourceCatalog;
    private int _nextConditionKey;

    public GeneratedConditionEditorViewModel(
        JsonNode? value,
        IReadOnlyList<SourceStepItem> sources,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
        string inputKeyPrefix = "conditions",
        Func<string, StepValueKind, JsonNode?, ResultPropertyDescriptor?, ResultBinding?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null,
        ValueReferenceSourceCatalog? sourceCatalog = null)
    {
        _sources = sources;
        _variables = variables ?? [];
        _providerSources = providerSources ?? [];
        _inputKeyPrefix = inputKeyPrefix;
        _nestedInputResolver = nestedInputResolver;
        _sourceCatalog = sourceCatalog;
        Conditions.CollectionChanged += OnCollectionChanged;
        AddCommand = new RelayCommand(AddCondition);

        IfConditionSettings settings;
        try { settings = value?.Deserialize<IfConditionSettings>() ?? new IfConditionSettings(); }
        catch (JsonException) { settings = new IfConditionSettings(); }
        catch (InvalidOperationException) { settings = new IfConditionSettings(); }

        _matchMode = settings.MatchMode;
        foreach (var condition in settings.Conditions ?? [])
        {
            if (condition is null) continue;
            var row = CreateRow();
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
    public bool IsValid => Conditions.Count > 0 && Conditions.All(condition => condition.IsValid);

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

    private void AddCondition() => Conditions.Add(CreateRow());

    private ConditionRowViewModel CreateRow() => new(
        Conditions, _sources, _variables, _providerSources,
        $"{_inputKeyPrefix}.{_nextConditionKey++}.comparison",
        _nestedInputResolver, _sourceCatalog);

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
                row.Changed -= OnConditionChanged;
        if (e.NewItems is not null)
            foreach (ConditionRowViewModel row in e.NewItems)
                row.Changed += OnConditionChanged;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
        Changed?.Invoke();
    }

    private void OnConditionChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
        Changed?.Invoke();
    }
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

public sealed class GeneratedScreenPointEditorViewModel : INotifyPropertyChanged, IGeneratedValueEditor, IGeneratedCompositeInputEditor
{
    private int _monitorIndex;
    private int _x;
    private int _y;
    private readonly Func<int?> _selectMonitor;
    private readonly Func<Task<StepScreenPointSelectionValue?>> _capturePoint;

    public GeneratedScreenPointEditorViewModel(
        JsonNode? value,
        ValueReferencePickerViewModel pointSource,
        Func<StepScreenPointSelectionValue, StepScreenPointSelectionValue> normalize,
        Func<int?> selectMonitor,
        Func<Task<StepScreenPointSelectionValue?>> capturePoint,
        string inputKeyPrefix = "screen_point",
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null)
    {
        StepScreenPointSelectionValue selection;
        try { selection = value?.Deserialize<StepScreenPointSelectionValue>() ?? new(0, 0, 0, KlickOnPoint3DSettings.MonitorLocalCoordinates); }
        catch (JsonException) { selection = new(0, 0, 0, KlickOnPoint3DSettings.MonitorLocalCoordinates); }
        selection = normalize(selection);
        _monitorIndex = selection.MonitorIndex;
        _x = selection.X;
        _y = selection.Y;
        ResultBinding pointBinding;
        try { pointBinding = selection.PointSource?.Deserialize<ResultBinding>() ?? new ResultBinding(); }
        catch (JsonException) { pointBinding = new ResultBinding(); }
        pointSource.Load(pointBinding);
        WholeValueSource = new GeneratedWholeValueSourceViewModel(pointSource, pointBinding.IsConfigured);
        WholeValueSource.Changed += () => Changed?.Invoke();
        if (nestedInputResolver is not null)
        {
            MonitorField = CreateNestedField($"{inputKeyPrefix}.monitor_index", _monitorIndex, nestedInputResolver);
            XField = CreateNestedField($"{inputKeyPrefix}.x", _x, nestedInputResolver);
            YField = CreateNestedField($"{inputKeyPrefix}.y", _y, nestedInputResolver);
            foreach (var field in NestedFields) field.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is not nameof(GeneratedStepFieldViewModel.InputText)
                    and not nameof(GeneratedStepFieldViewModel.InputReferenceEditor))
                    return;
                WholeValueSource.UsesReference = false;
                Changed?.Invoke();
            };
        }
        _selectMonitor = selectMonitor;
        _capturePoint = capturePoint;
        SelectMonitorCommand = new RelayCommand(SelectMonitor);
        CaptureCommand = new RelayCommand(Capture);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    public ICommand SelectMonitorCommand { get; }
    public ICommand CaptureCommand { get; }
    public GeneratedStepFieldViewModel? MonitorField { get; }
    public GeneratedStepFieldViewModel? XField { get; }
    public GeneratedStepFieldViewModel? YField { get; }
    public GeneratedWholeValueSourceViewModel WholeValueSource { get; }
    public IEnumerable<GeneratedStepFieldViewModel> InputFields => NestedFields;
    private IEnumerable<GeneratedStepFieldViewModel> NestedFields =>
        new[] { MonitorField, XField, YField }.OfType<GeneratedStepFieldViewModel>();
    public IReadOnlyDictionary<string, ResultBinding> InputBindings => NestedFields.ToDictionary(
        field => field.Descriptor.Id,
        field => WholeValueSource.UsesReference
            ? new ResultBinding()
            : field.InputReferenceEditor!.Picker.ToBinding(),
        StringComparer.Ordinal);
    public int MonitorIndex { get => MonitorField?.IntegerValue ?? _monitorIndex; set { if (MonitorField is not null) { MonitorField.IntegerValue = value; _monitorIndex = value; PropertyChanged?.Invoke(this, new(nameof(MonitorIndex))); } else Set(ref _monitorIndex, value); } }
    public int X { get => XField?.IntegerValue ?? _x; set { if (XField is not null) { XField.IntegerValue = value; _x = value; PropertyChanged?.Invoke(this, new(nameof(X))); } else Set(ref _x, value); } }
    public int Y { get => YField?.IntegerValue ?? _y; set { if (YField is not null) { YField.IntegerValue = value; _y = value; PropertyChanged?.Invoke(this, new(nameof(Y))); } else Set(ref _y, value); } }
    public JsonNode? ToNode() => JsonSerializer.SerializeToNode(new StepScreenPointSelectionValue(
        MonitorIndex, X, Y, KlickOnPoint3DSettings.MonitorLocalCoordinates,
        JsonSerializer.SerializeToNode(WholeValueSource.ToBinding())));

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

    private static GeneratedStepFieldViewModel CreateNestedField(
        string key, int value,
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
    {
        var node = JsonValue.Create(value);
        var descriptor = new StepFieldDescriptor(key, string.Empty, StepValueKind.Integer, DefaultValue: node,
            EditorHint: key.EndsWith(".monitor_index", StringComparison.Ordinal) ? StepEditorHints.MonitorPicker : null);
        return new GeneratedStepFieldViewModel(descriptor, node,
            inputReferenceEditor: resolver(key, StepValueKind.Integer, node));
    }
}

public sealed class GeneratedUserChoiceOptionsEditorViewModel : IGeneratedValueEditor, IGeneratedCompositeInputEditor
{
    private JsonNode? _malformedValue;
    private bool _initializing = true;

    private readonly string _inputKeyPrefix;
    private readonly Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? _nestedInputResolver;

    public GeneratedUserChoiceOptionsEditorViewModel(
        JsonNode? value,
        string inputKeyPrefix = "options",
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null)
    {
        _inputKeyPrefix = inputKeyPrefix;
        _nestedInputResolver = nestedInputResolver;
        Options.CollectionChanged += OnCollectionChanged;
        IReadOnlyList<StepUserChoiceOptionValue> values;
        try { values = value?.Deserialize<List<StepUserChoiceOptionValue>>() ?? []; }
        catch (JsonException) { values = []; _malformedValue = value?.DeepClone(); }
        if (value is not null && values.Count < 2) _malformedValue = value.DeepClone();
        if (values.Any(item => item is null)) { _malformedValue = value?.DeepClone(); values = []; }
        foreach (var option in values) Add(option);
        while (Options.Count < UserChoiceStepDefinition.MinimumOptions) Add();
        AddCommand = new RelayCommand(() => Add(), () => Options.Count < UserChoiceStepDefinition.MaximumOptions);
        _initializing = false;
    }

    public event Action? Changed;
    public IEnumerable<GeneratedStepFieldViewModel> InputFields => Options.SelectMany(option => new[] { option.LabelField, option.ValueField }).OfType<GeneratedStepFieldViewModel>();
    public ObservableCollection<UserChoiceOptionEditorViewModel> Options { get; } = [];
    public ICommand AddCommand { get; }
    public IReadOnlyDictionary<string, ResultBinding> InputBindings => Options
        .SelectMany((option, index) => new[]
        {
            new KeyValuePair<string, ResultBinding>($"{_inputKeyPrefix}.{index}.label",
                option.LabelField?.InputReferenceEditor?.Picker.ToBinding() ?? new ResultBinding()),
            new KeyValuePair<string, ResultBinding>($"{_inputKeyPrefix}.{index}.value",
                option.ValueField?.InputReferenceEditor?.Picker.ToBinding() ?? new ResultBinding())
        }).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    public JsonNode? ToNode() => _malformedValue?.DeepClone() ?? JsonSerializer.SerializeToNode(Options.Select(option =>
        new StepUserChoiceOptionValue(option.Id, option.Label, option.Value)).ToArray());
    private void Add(StepUserChoiceOptionValue? value = null)
    {
        var item = value is null
            ? new UserChoiceOptionEditorViewModel(Options)
            : new UserChoiceOptionEditorViewModel(Options, value.Id, value.Label, value.Value);
        if (_nestedInputResolver is not null)
            item.ConfigureNestedInputs($"{_inputKeyPrefix}.{Options.Count}", _nestedInputResolver);
        item.DuplicateCommand = new RelayCommand(() =>
        {
            Add(new StepUserChoiceOptionValue(Guid.NewGuid().ToString("N"), item.Label, item.Value));
            var copy = Options[^1];
            ContextFieldCopies.Copy(item.LabelField, copy.LabelField);
            ContextFieldCopies.Copy(item.ValueField, copy.ValueField);
            Options.Move(Options.Count - 1, Options.IndexOf(item) + 1);
        }, () => AddCommand?.CanExecute(null) != false);
        Options.Add(item);
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null) foreach (UserChoiceOptionEditorViewModel item in e.OldItems) item.PropertyChanged -= ItemChanged;
        if (e.NewItems is not null) foreach (UserChoiceOptionEditorViewModel item in e.NewItems) item.PropertyChanged += ItemChanged;
        (AddCommand as RelayCommand)?.RaiseCanExecuteChanged();
        if (!_initializing) _malformedValue = null;
        Changed?.Invoke();
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) => Edited();
    private void Edited() { if (!_initializing) _malformedValue = null; Changed?.Invoke(); }

}

public sealed class GeneratedPointEntryListEditorViewModel : IGeneratedValueEditor, IGeneratedCompositeInputEditor
{
    private JsonNode? _malformedValue;
    private bool _initializing = true;

    private readonly IReadOnlyList<SourceStepItem> _sources;
    private readonly IReadOnlyList<JobVariable> _variables;
    private readonly IReadOnlyList<ValueProviderSourceDescriptor> _providerSources;
    private readonly ValueReferencePickerContext? _pickerContext;
    private readonly string _inputKeyPrefix;
    private readonly Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? _nestedInputResolver;
    private readonly ValueReferenceSourceCatalog? _sourceCatalog;
    public GeneratedPointEntryListEditorViewModel(
        JsonNode? value,
        IReadOnlyList<SourceStepItem> sources,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
        ValueReferencePickerContext? pickerContext = null,
        string inputKeyPrefix = "points",
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null,
        ValueReferenceSourceCatalog? sourceCatalog = null)
    {
        _sources = sources;
        _variables = variables ?? [];
        _providerSources = providerSources ?? [];
        _pickerContext = pickerContext;
        _inputKeyPrefix = inputKeyPrefix;
        _nestedInputResolver = nestedInputResolver;
        _sourceCatalog = sourceCatalog;
        Points.CollectionChanged += OnCollectionChanged;
        IReadOnlyList<StepPointEntryValue> values;
        try { values = value?.Deserialize<List<StepPointEntryValue>>() ?? []; }
        catch (JsonException) { values = []; _malformedValue = value?.DeepClone(); }
        if (value is not null && values.Count < 1) _malformedValue = value.DeepClone();
        if (values.Any(item => item is null)) { _malformedValue = value?.DeepClone(); values = []; }
        foreach (var valueItem in values) Add(valueItem);
        if (Points.Count == 0) Add();
        AddCommand = new RelayCommand(() => Add());
        _initializing = false;
    }
    public event Action? Changed;
    public IEnumerable<GeneratedStepFieldViewModel> InputFields => Points.SelectMany(point => new[] { point.ManualXField, point.ManualYField }).OfType<GeneratedStepFieldViewModel>();
    public ObservableCollection<PointEntryViewModel> Points { get; } = [];
    public ICommand AddCommand { get; }
    public IReadOnlyDictionary<string, ResultBinding> InputBindings => Points
        .SelectMany((point, index) => new[]
        {
            new KeyValuePair<string, ResultBinding>($"{_inputKeyPrefix}.{index}.manual_x",
                point.WholeValueSource.UsesReference
                    ? new ResultBinding()
                    : point.ManualXField?.InputReferenceEditor?.Picker.ToBinding() ?? new ResultBinding()),
            new KeyValuePair<string, ResultBinding>($"{_inputKeyPrefix}.{index}.manual_y",
                point.WholeValueSource.UsesReference
                    ? new ResultBinding()
                    : point.ManualYField?.InputReferenceEditor?.Picker.ToBinding() ?? new ResultBinding())
        }).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    public JsonNode? ToNode() => _malformedValue?.DeepClone() ?? JsonSerializer.SerializeToNode(Points.Select(point =>
    {
        var value = point.ToPointEntry();
        return new StepPointEntryValue(point.SourceToken, value.ManualX, value.ManualY,
            JsonSerializer.SerializeToNode(value.PointsSource));
    }).ToArray());
    private void Add(StepPointEntryValue? value = null)
    {
        var item = new PointEntryViewModel(
            Points, _sources, _variables, _providerSources, _pickerContext, _sourceCatalog);
        if (value is not null)
        {
            ResultBinding binding;
            try { binding = value.PointsSource?.Deserialize<ResultBinding>() ?? new(); } catch (JsonException) { binding = new(); }
            item.LoadFrom(new PointEntry
            {
                Source = StepEnumRules.TryRead<PointEntrySource>(value.Source, out var source) ? source : (PointEntrySource)(-1),
                ManualX = value.ManualX,
                ManualY = value.ManualY,
                PointsSource = binding
            });
            item.LoadSourceToken(value.Source);
        }
        if (_nestedInputResolver is not null)
            item.ConfigureNestedInputs($"{_inputKeyPrefix}.{Points.Count}", _nestedInputResolver);
        item.DuplicateCommand = new RelayCommand(() =>
        {
            Add(new StepPointEntryValue(item.SourceToken, item.ManualX, item.ManualY, JsonSerializer.SerializeToNode(item.WholeValueSource.ToBinding())));
            var copy = Points[^1];
            ContextFieldCopies.Copy(item.ManualXField, copy.ManualXField);
            ContextFieldCopies.Copy(item.ManualYField, copy.ManualYField);
            copy.WholeValueSource.Load(item.WholeValueSource.ToBinding(), item.WholeValueSource.UsesReference);
            Points.Move(Points.Count - 1, Points.IndexOf(item) + 1);
        });
        Points.Add(item);
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null) foreach (PointEntryViewModel item in e.OldItems) { item.PropertyChanged -= ItemChanged; item.PointsSource.ReferenceChanged -= ReferenceItemChanged; }
        if (e.NewItems is not null) foreach (PointEntryViewModel item in e.NewItems) { item.PropertyChanged += ItemChanged; item.PointsSource.ReferenceChanged += ReferenceItemChanged; }
        if (!_initializing) _malformedValue = null;
        Changed?.Invoke();
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) => Edited();
    private void ReferenceItemChanged(object? sender, EventArgs e) => Edited();
    private void Edited() { if (!_initializing) _malformedValue = null; Changed?.Invoke(); }

}

public sealed class GeneratedAxisExpressionListEditorViewModel : IGeneratedValueEditor
{
    private JsonNode? _malformedValue;
    private bool _initializing = true;

    public GeneratedAxisExpressionListEditorViewModel(JsonNode? value)
    {
        Expressions.CollectionChanged += OnCollectionChanged;
        IReadOnlyList<StepAxisExpressionValue> values;
        try { values = value?.Deserialize<List<StepAxisExpressionValue>>() ?? []; } catch (JsonException) { values = []; _malformedValue = value?.DeepClone(); }
        if (value is not null && values.Count < 1) _malformedValue = value.DeepClone();
        if (values.Any(item => item is null)) { _malformedValue = value?.DeepClone(); values = []; }
        foreach (var item in values) Add(item);
        if (Expressions.Count == 0) Add();
        AddCommand = new RelayCommand(() => Add());
        _initializing = false;
    }
    public event Action? Changed;
    public ObservableCollection<AxisExpressionViewModel> Expressions { get; } = [];
    public ICommand AddCommand { get; }
    public JsonNode? ToNode() => _malformedValue?.DeepClone() ?? JsonSerializer.SerializeToNode(Expressions.Select(item =>
    {
        return new StepAxisExpressionValue(item.Axis, item.OperatorToken, item.Value);
    }).ToArray());
    private void Add(StepAxisExpressionValue? value = null)
    {
        var item = new AxisExpressionViewModel(Expressions);
        if (value is not null) item.LoadFrom(new AxisExpression
        {
            Axis = value.Axis,
            Operator = StepEnumRules.TryRead<PointAxisOperator>(value.Operator, out var op) ? op : default,
            Value = value.Value
        });
        if (value is not null) item.LoadOperatorToken(value.Operator);
        item.DuplicateCommand = new RelayCommand(() =>
        {
            Add(new StepAxisExpressionValue(item.Axis, item.OperatorToken, item.Value));
            Expressions.Move(Expressions.Count - 1, Expressions.IndexOf(item) + 1);
        });
        Expressions.Add(item);
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null) foreach (AxisExpressionViewModel item in e.OldItems) item.PropertyChanged -= ItemChanged;
        if (e.NewItems is not null) foreach (AxisExpressionViewModel item in e.NewItems) item.PropertyChanged += ItemChanged;
        if (!_initializing) _malformedValue = null;
        Changed?.Invoke();
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) => Edited();
    private void Edited() { if (!_initializing) _malformedValue = null; Changed?.Invoke(); }

}

public sealed class GeneratedRoiEditorViewModel : INotifyPropertyChanged, IGeneratedCompositeInputEditor
{
    private bool _isRoiEnabled;
    private bool _useDynamicRoi;
    private int _x;
    private int _y;
    private int _width;
    private int _height;

    public GeneratedRoiEditorViewModel(
        JsonNode? value,
        ValueReferencePickerViewModel dynamicRoiSource,
        string inputKeyPrefix = "roi",
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null)
    {
        DetectionDynamicRoiSource = dynamicRoiSource;
        var selection = Read(value);
        _isRoiEnabled = selection.Enabled;
        _x = selection.X;
        _y = selection.Y;
        _width = selection.Width;
        _height = selection.Height;
        if (nestedInputResolver is not null)
        {
            EnabledField = CreateNestedField($"{inputKeyPrefix}.enabled", StepValueKind.Boolean,
                JsonValue.Create(_isRoiEnabled), nestedInputResolver);
            XField = CreateNestedField($"{inputKeyPrefix}.x", StepValueKind.Integer,
                JsonValue.Create(_x), nestedInputResolver);
            YField = CreateNestedField($"{inputKeyPrefix}.y", StepValueKind.Integer,
                JsonValue.Create(_y), nestedInputResolver);
            WidthField = CreateNestedField($"{inputKeyPrefix}.width", StepValueKind.Integer,
                JsonValue.Create(_width), nestedInputResolver);
            HeightField = CreateNestedField($"{inputKeyPrefix}.height", StepValueKind.Integer,
                JsonValue.Create(_height), nestedInputResolver);
            foreach (var field in NestedFields) field.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is not nameof(GeneratedStepFieldViewModel.InputText)
                    and not nameof(GeneratedStepFieldViewModel.InputReferenceEditor))
                    return;
                if (!ReferenceEquals(field, EnabledField)) DisableDynamicRoi();
                Changed?.Invoke();
            };
        }
        try { DetectionDynamicRoiSource.Load(selection.DynamicSource?.Deserialize<ResultBinding>() ?? new ResultBinding()); }
        catch (JsonException) { DetectionDynamicRoiSource.Load(new ResultBinding()); }
        _useDynamicRoi = DetectionDynamicRoiSource.IsConfigured;
        WholeValueSource = new GeneratedWholeValueSourceViewModel(
            DetectionDynamicRoiSource, _useDynamicRoi);
        WholeValueSource.Changed += () =>
        {
            _useDynamicRoi = WholeValueSource.UsesReference;
            PropertyChanged?.Invoke(this, new(nameof(UseDynamicRoi)));
            PropertyChanged?.Invoke(this, new(nameof(HasSelectedDynamicRoi)));
            Changed?.Invoke();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    public ValueReferencePickerViewModel DetectionDynamicRoiSource { get; }
    public GeneratedWholeValueSourceViewModel WholeValueSource { get; }
    public GeneratedStepFieldViewModel? EnabledField { get; }
    public GeneratedStepFieldViewModel? XField { get; }
    public GeneratedStepFieldViewModel? YField { get; }
    public GeneratedStepFieldViewModel? WidthField { get; }
    public GeneratedStepFieldViewModel? HeightField { get; }
    public IEnumerable<GeneratedStepFieldViewModel> InputFields => NestedFields;
    private IEnumerable<GeneratedStepFieldViewModel> NestedFields =>
        new[] { EnabledField, XField, YField, WidthField, HeightField }.OfType<GeneratedStepFieldViewModel>();
    public IReadOnlyDictionary<string, ResultBinding> InputBindings => NestedFields
        .Where(field => !ReferenceEquals(field, EnabledField))
        .ToDictionary(
            field => field.Descriptor.Id,
            field => WholeValueSource.UsesReference
                ? new ResultBinding()
                : field.InputReferenceEditor!.Picker.ToBinding(),
            StringComparer.Ordinal);
    public bool HasSelectedDynamicRoi => UseDynamicRoi && DetectionDynamicRoiSource.IsConfigured;

    public bool IsRoiEnabled { get => EnabledField?.BooleanValue ?? _isRoiEnabled; set { if (EnabledField is not null) { EnabledField.BooleanValue = value; _isRoiEnabled = value; PropertyChanged?.Invoke(this, new(nameof(IsRoiEnabled))); } else Set(ref _isRoiEnabled, value, nameof(IsRoiEnabled)); } }
    public bool UseDynamicRoi
    {
        get => _useDynamicRoi;
        set
        {
            if (_useDynamicRoi == value) return;
            _useDynamicRoi = value;
            WholeValueSource.UsesReference = value;
            PropertyChanged?.Invoke(this, new(nameof(UseDynamicRoi)));
            PropertyChanged?.Invoke(this, new(nameof(HasSelectedDynamicRoi)));
            Changed?.Invoke();
        }
    }
    public int X { get => XField?.IntegerValue ?? _x; set { DisableDynamicRoi(); if (XField is not null) { XField.IntegerValue = value; _x = value; PropertyChanged?.Invoke(this, new(nameof(X))); } else Set(ref _x, value, nameof(X)); } }
    public int Y { get => YField?.IntegerValue ?? _y; set { DisableDynamicRoi(); if (YField is not null) { YField.IntegerValue = value; _y = value; PropertyChanged?.Invoke(this, new(nameof(Y))); } else Set(ref _y, value, nameof(Y)); } }
    public int RoiWidth { get => WidthField?.IntegerValue ?? _width; set { DisableDynamicRoi(); if (WidthField is not null) { WidthField.IntegerValue = value; _width = value; PropertyChanged?.Invoke(this, new(nameof(RoiWidth))); } else Set(ref _width, value, nameof(RoiWidth)); } }
    public int RoiHeight { get => HeightField?.IntegerValue ?? _height; set { DisableDynamicRoi(); if (HeightField is not null) { HeightField.IntegerValue = value; _height = value; PropertyChanged?.Invoke(this, new(nameof(RoiHeight))); } else Set(ref _height, value, nameof(RoiHeight)); } }

    public StepRoiSelectionValue ToValue() => new(
        IsRoiEnabled, X, Y, RoiWidth, RoiHeight,
        JsonSerializer.SerializeToNode(WholeValueSource.ToBinding()));

    private void DisableDynamicRoi()
    {
        if (_useDynamicRoi) UseDynamicRoi = false;
    }

    private static GeneratedStepFieldViewModel CreateNestedField(
        string key,
        StepValueKind kind,
        JsonNode? value,
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
    {
        var descriptor = new StepFieldDescriptor(key, string.Empty, kind, DefaultValue: value?.DeepClone());
        return new GeneratedStepFieldViewModel(descriptor, value?.DeepClone(),
            inputReferenceEditor: resolver(key, kind, value));
    }

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

public sealed class GeneratedYoloEditorViewModel : INotifyPropertyChanged, IGeneratedCompositeInputEditor
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
        Func<string, double?> recommendedConfidenceLoader,
        string inputKeyPrefix = "yolo",
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null)
    {
        _modelLoader = modelLoader;
        _classLoader = classLoader;
        _recommendedConfidenceLoader = recommendedConfidenceLoader;
        var selection = Read(value);
        _model = selection.Model;
        _className = selection.ClassName;
        if (nestedInputResolver is not null)
        {
            ModelField = CreateNestedTextField($"{inputKeyPrefix}.model", _model, nestedInputResolver);
            ClassField = CreateNestedTextField($"{inputKeyPrefix}.class_name", _className, nestedInputResolver);
            ModelField.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(GeneratedStepFieldViewModel.InputText))
                    Model = ModelField.InputText;
                else if (args.PropertyName == nameof(GeneratedStepFieldViewModel.InputReferenceEditor))
                    Changed?.Invoke();
            };
            ClassField.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(GeneratedStepFieldViewModel.InputText))
                    ClassName = ClassField.InputText;
                else if (args.PropertyName == nameof(GeneratedStepFieldViewModel.InputReferenceEditor))
                    Changed?.Invoke();
            };
        }
        Initialization = LoadModelsAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    public event Action<double>? RecommendedConfidenceChanged;
    public ObservableCollection<string> Models { get; } = [];
    public ObservableCollection<string> Classes { get; } = [];
    public GeneratedStepFieldViewModel? ModelField { get; }
    public GeneratedStepFieldViewModel? ClassField { get; }
    public IEnumerable<GeneratedStepFieldViewModel> InputFields => NestedFields;
    private IEnumerable<GeneratedStepFieldViewModel> NestedFields =>
        new[] { ModelField, ClassField }.OfType<GeneratedStepFieldViewModel>();
    public IReadOnlyDictionary<string, ResultBinding> InputBindings => NestedFields.ToDictionary(
        field => field.Descriptor.Id, field => field.InputReferenceEditor!.Picker.ToBinding(), StringComparer.Ordinal);
    public Task Initialization { get; }
    public Task ClassLoading { get; private set; } = Task.CompletedTask;

    public string Model
    {
        get => ModelField?.InputText ?? _model;
        set
        {
            value ??= string.Empty;
            if (_model == value) return;
            _model = value;
            if (ModelField is not null && ModelField.InputText != value)
                ModelField.InputText = value;
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
        get => ClassField?.InputText ?? _className;
        set
        {
            value ??= string.Empty;
            if (_className == value) return;
            _className = value;
            if (ClassField is not null && ClassField.InputText != value)
                ClassField.InputText = value;
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
        ModelField?.SetSuggestions(Models);
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
        ClassField?.SetSuggestions(Classes);
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

    private static GeneratedStepFieldViewModel CreateNestedTextField(
        string key,
        string value,
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
    {
        var node = JsonValue.Create(value);
        var descriptor = new StepFieldDescriptor(key, string.Empty, StepValueKind.Text,
            DefaultValue: node, EditorHint: StepEditorHints.ProcessNameSuggestions);
        return new GeneratedStepFieldViewModel(descriptor, node,
            inputReferenceEditor: resolver(key, StepValueKind.Text, node));
    }
}

public sealed class GeneratedResultBindingEditorViewModel
{
    public GeneratedResultBindingEditorViewModel(JsonNode? value, ValueReferencePickerViewModel picker)
    {
        Picker = picker;
        try { Picker.Load(value?.Deserialize<ResultBinding>() ?? new ResultBinding()); }
        catch (JsonException) { Picker.Load(new ResultBinding()); }
        Picker.ReferenceChanged += (_, _) => Changed?.Invoke();
        Picker.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ValueReferencePickerViewModel.RequiresInlineEditChoice))
                DisplayStateChanged?.Invoke();
        };
    }

    public event Action? Changed;
    public event Action? DisplayStateChanged;
    public ValueReferencePickerViewModel Picker { get; }
}

public enum WholeValueInputMode
{
    IndividualValues,
    JobVariable,
    StepResult
}

public sealed class GeneratedWholeValueSourceViewModel : INotifyPropertyChanged
{
    private WholeValueInputMode _mode;
    private bool _updatingMode;

    public GeneratedWholeValueSourceViewModel(ValueReferencePickerViewModel picker, bool usesReference)
    {
        Picker = picker;
        _mode = ResolveMode(picker.ToBinding(), usesReference && picker.IsConfigured);
        UseIndividualValuesCommand = new RelayCommand(() => SelectMode(WholeValueInputMode.IndividualValues));
        UseJobVariableCommand = new RelayCommand(
            () => SelectMode(WholeValueInputMode.JobVariable),
            () => Picker.CanUseJobVariables);
        UseStepResultCommand = new RelayCommand(
            () => SelectMode(WholeValueInputMode.StepResult),
            () => Picker.CanUseStepResults);
        Picker.ReferenceChanged += (_, _) =>
        {
            if (!_updatingMode)
                Changed?.Invoke();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    public ValueReferencePickerViewModel Picker { get; }
    public ICommand UseIndividualValuesCommand { get; }
    public ICommand UseJobVariableCommand { get; }
    public ICommand UseStepResultCommand { get; }
    public WholeValueInputMode Mode => _mode;
    public bool UsesIndividualValues => Mode == WholeValueInputMode.IndividualValues;
    public bool UsesJobVariable => Mode == WholeValueInputMode.JobVariable;
    public bool UsesStepResult => Mode == WholeValueInputMode.StepResult;
    public bool ShowsIndividualValues => UsesIndividualValues;

    public bool UsesReference
    {
        get => !UsesIndividualValues;
        set
        {
            if (!value)
            {
                if (UsesIndividualValues) return;
                SelectMode(WholeValueInputMode.IndividualValues);
            }
            else if (!UsesReference)
                SelectMode(Picker.ActiveSourceKind switch
                {
                    StepInputSourceKind.StepResult when Picker.CanUseStepResults => WholeValueInputMode.StepResult,
                    StepInputSourceKind.JobVariable when Picker.CanUseJobVariables => WholeValueInputMode.JobVariable,
                    _ when Picker.CanUseJobVariables => WholeValueInputMode.JobVariable,
                    _ => WholeValueInputMode.StepResult
                });
        }
    }

    public ResultBinding ToBinding() => UsesReference ? Picker.ToBinding() : new ResultBinding();

    public void Load(ResultBinding binding, bool usesReference)
    {
        _updatingMode = true;
        try
        {
            Picker.Load(binding);
            _mode = ResolveMode(binding, usesReference && binding.IsConfigured);
        }
        finally
        {
            _updatingMode = false;
        }
        NotifyModeChanged();
    }

    private void SelectMode(WholeValueInputMode mode)
    {
        if (mode == WholeValueInputMode.JobVariable && !Picker.CanUseJobVariables) return;
        if (mode == WholeValueInputMode.StepResult && !Picker.CanUseStepResults) return;
        if (_mode == mode) return;
        _mode = mode;
        _updatingMode = true;
        try
        {
            if (mode == WholeValueInputMode.IndividualValues)
            {
                if (Picker.ClearCommand.CanExecute(null)) Picker.ClearCommand.Execute(null);
            }
            else
            {
                Picker.SelectSourceKind(mode == WholeValueInputMode.JobVariable
                    ? StepInputSourceKind.JobVariable
                    : StepInputSourceKind.StepResult);
            }
        }
        finally
        {
            _updatingMode = false;
        }
        NotifyModeChanged();
    }

    private void NotifyModeChanged()
    {
        PropertyChanged?.Invoke(this, new(nameof(Mode)));
        PropertyChanged?.Invoke(this, new(nameof(UsesReference)));
        PropertyChanged?.Invoke(this, new(nameof(UsesIndividualValues)));
        PropertyChanged?.Invoke(this, new(nameof(UsesJobVariable)));
        PropertyChanged?.Invoke(this, new(nameof(UsesStepResult)));
        PropertyChanged?.Invoke(this, new(nameof(ShowsIndividualValues)));
        Changed?.Invoke();
    }

    private static WholeValueInputMode ResolveMode(ResultBinding binding, bool usesReference) =>
        !usesReference
            ? WholeValueInputMode.IndividualValues
            : binding.ProviderId == ValueProviderIds.StepResult || !string.IsNullOrWhiteSpace(binding.SourceStepId)
                ? WholeValueInputMode.StepResult
                : WholeValueInputMode.JobVariable;
}

public interface IGeneratedCompositeInputEditor
{
    IReadOnlyDictionary<string, ResultBinding> InputBindings { get; }
    IEnumerable<GeneratedStepFieldViewModel> InputFields { get; }
}

public sealed class GeneratedProcessTargetEditorViewModel : INotifyPropertyChanged, IGeneratedCompositeInputEditor
{
    private readonly bool _useExecutablePath;
    private bool _useProcessReference;
    private string _processName;
    private string _executablePath;
    private string _windowTitleContains;
    public IReadOnlyList<EditorChoiceOptionViewModel> SourceOptions { get; } =
    [
        new("Manual", Loc.Get("Ui.Step.ProcessSource.SearchByCharacteristics")),
        new("JobResult", Loc.Get("Ui.Step.Settings.ProcessSource"))
    ];

    public GeneratedProcessTargetEditorViewModel(
        JsonNode? value,
        ValueReferencePickerViewModel picker,
        IEnumerable<string> processNames,
        bool useExecutablePath = false,
        string inputKeyPrefix = "process_target",
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null)
    {
        _useExecutablePath = useExecutablePath;
        Picker = picker;
        ProcessNames = processNames;
        ManualSourceContent = useExecutablePath
            ? new GeneratedExecutableProcessTargetContentViewModel(this)
            : new GeneratedProcessNameTargetContentViewModel(this);
        ProcessReferenceContent = new GeneratedProcessReferenceTargetContentViewModel(this);
        var selector = ReadSelector(value);
        var binding = ReadBinding(selector.ProcessSource);
        Picker.Load(binding);
        _useProcessReference = binding.IsConfigured;
        WholeValueSource = new GeneratedWholeValueSourceViewModel(Picker, _useProcessReference);
        WholeValueSource.Changed += () =>
        {
            _useProcessReference = WholeValueSource.UsesReference;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseProcessReference)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSourceOption)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSourceContent)));
            Changed?.Invoke();
        };
        _processName = useExecutablePath || !string.IsNullOrWhiteSpace(selector.ProcessName)
            ? selector.ProcessName
            : Path.GetFileNameWithoutExtension(selector.ExecutablePath);
        _executablePath = useExecutablePath ? selector.ExecutablePath : string.Empty;
        _windowTitleContains = selector.WindowTitleContains ?? string.Empty;
        if (nestedInputResolver is not null)
        {
            ProcessNameField = CreateNestedTextField($"{inputKeyPrefix}.process_name", _processName, nestedInputResolver);
            ProcessNameField.SetSuggestions(ProcessNames);
            ExecutablePathField = CreateNestedTextField($"{inputKeyPrefix}.executable_path", _executablePath, nestedInputResolver,
                StepEditorHints.FilePicker);
            WindowTitleField = CreateNestedTextField($"{inputKeyPrefix}.window_title_contains", _windowTitleContains, nestedInputResolver);
            foreach (var field in NestedFields) field.PropertyChanged += NestedFieldChanged;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    public ValueReferencePickerViewModel Picker { get; }
    public GeneratedWholeValueSourceViewModel WholeValueSource { get; }
    public GeneratedStepFieldViewModel? ProcessNameField { get; }
    public GeneratedStepFieldViewModel? ExecutablePathField { get; }
    public GeneratedStepFieldViewModel? WindowTitleField { get; }
    public IEnumerable<GeneratedStepFieldViewModel> InputFields => NestedFields;
    private IEnumerable<GeneratedStepFieldViewModel> NestedFields =>
        new[] { ProcessNameField, ExecutablePathField, WindowTitleField }.OfType<GeneratedStepFieldViewModel>();
    public IReadOnlyDictionary<string, ResultBinding> InputBindings => NestedFields.ToDictionary(
        field => field.Descriptor.Id,
        field => WholeValueSource.UsesReference
            ? new ResultBinding()
            : field.InputReferenceEditor!.Picker.ToBinding(),
        StringComparer.Ordinal);
    public IEnumerable<string> ProcessNames { get; }
    public GeneratedProcessTargetContentViewModel ManualSourceContent { get; }
    public GeneratedProcessReferenceTargetContentViewModel ProcessReferenceContent { get; }
    public GeneratedProcessTargetContentViewModel SelectedSourceContent =>
        UseProcessReference ? ProcessReferenceContent : ManualSourceContent;

    public bool UseProcessReference
    {
        get => _useProcessReference;
        set
        {
            if (_useProcessReference == value) return;
            _useProcessReference = value;
            WholeValueSource.UsesReference = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseProcessReference)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSourceOption)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSourceContent)));
            Changed?.Invoke();
        }
    }

    public EditorChoiceOptionViewModel SelectedSourceOption
    {
        get => SourceOptions[UseProcessReference ? 1 : 0];
        set
        {
            if (value is not null)
                UseProcessReference = string.Equals(value.Value, "JobResult", StringComparison.Ordinal);
        }
    }

    public string ProcessName
    {
        get => ProcessNameField?.InputText ?? _processName;
        set
        {
            if (_processName == value) return;
            UseProcessReference = false;
            _processName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProcessName)));
            Changed?.Invoke();
        }
    }

    public string ExecutablePath
    {
        get => ExecutablePathField?.InputText ?? _executablePath;
        set
        {
            if (_executablePath == value) return;
            UseProcessReference = false;
            _executablePath = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecutablePath)));
            Changed?.Invoke();
        }
    }

    public string WindowTitleContains
    {
        get => WindowTitleField?.InputText ?? _windowTitleContains;
        set
        {
            if (_windowTitleContains == value) return;
            UseProcessReference = false;
            _windowTitleContains = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitleContains)));
            Changed?.Invoke();
        }
    }

    public StepProcessSelectorValue ToValue() => new(
        JsonSerializer.SerializeToNode(WholeValueSource.ToBinding()),
        _useExecutablePath ? string.Empty : ProcessName,
        _useExecutablePath ? _executablePath : string.Empty,
        WindowTitleContains);

    private static GeneratedStepFieldViewModel CreateNestedTextField(
        string key,
        string value,
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver,
        string? editorHint = null)
    {
        var descriptor = new StepFieldDescriptor(key, string.Empty, StepValueKind.Text,
            DefaultValue: JsonValue.Create(value), EditorHint: editorHint);
        return new GeneratedStepFieldViewModel(descriptor, JsonValue.Create(value),
            inputReferenceEditor: resolver(key, StepValueKind.Text, JsonValue.Create(value)));
    }

    private static GeneratedStepFieldViewModel CreateNestedTextField(
        string key, string value,
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
    {
        var node = JsonValue.Create(value);
        var descriptor = new StepFieldDescriptor(key, string.Empty, StepValueKind.Text,
            DefaultValue: node, EditorHint: StepEditorHints.ProcessNameSuggestions);
        return new GeneratedStepFieldViewModel(descriptor, node,
            inputReferenceEditor: resolver(key, StepValueKind.Text, node));
    }

    private void NestedFieldChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is not nameof(GeneratedStepFieldViewModel.InputText)
            and not nameof(GeneratedStepFieldViewModel.InputReferenceEditor))
            return;
        UseProcessReference = false;
        Changed?.Invoke();
    }

    private static StepProcessSelectorValue ReadSelector(JsonNode? value)
    {
        try
        {
            return value?.Deserialize<StepProcessSelectorValue>()
                ?? new StepProcessSelectorValue(null, string.Empty, string.Empty, string.Empty);
        }
        catch (JsonException)
        {
            return new StepProcessSelectorValue(null, string.Empty, string.Empty, string.Empty);
        }
    }

    private static ResultBinding ReadBinding(JsonNode? value)
    {
        try { return value?.Deserialize<ResultBinding>() ?? new ResultBinding(); }
        catch (JsonException) { return new ResultBinding(); }
    }
}

public abstract class GeneratedProcessTargetContentViewModel(
    GeneratedProcessTargetEditorViewModel editor)
{
    public GeneratedProcessTargetEditorViewModel Editor { get; } = editor;
}

public sealed class GeneratedProcessNameTargetContentViewModel(
    GeneratedProcessTargetEditorViewModel editor)
    : GeneratedProcessTargetContentViewModel(editor);

public sealed class GeneratedExecutableProcessTargetContentViewModel(
    GeneratedProcessTargetEditorViewModel editor)
    : GeneratedProcessTargetContentViewModel(editor);

public sealed class GeneratedProcessReferenceTargetContentViewModel(
    GeneratedProcessTargetEditorViewModel editor)
    : GeneratedProcessTargetContentViewModel(editor);

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
    private string _qualityToken;
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
        _qualityToken = selection.QualityMode;
        _qualityMode = StepEnumRules.TryRead<CameraQualityMode>(_qualityToken, out var mode)
            ? mode : (CameraQualityMode)(-1);
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
    public bool HasInvalidQualityToken => !StepEnumRules.TryRead<CameraQualityMode>(_qualityToken, out _);
    public string InvalidQualityMessage => HasInvalidQualityToken
        ? Loc.Format("Ui.Step.Generated.Validation.UnknownSavedToken", _qualityToken) : string.Empty;

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
                _qualityToken = value.QualityMode.ToString();
                _width = value.Mode?.Width ?? 0;
                _height = value.Mode?.Height ?? 0;
                _framesPerSecond = value.Mode?.FramesPerSecond ?? 0;
                _pixelFormat = value.Mode?.PixelFormat ?? string.Empty;
            }
            OnChanged(nameof(SelectedQuality));
            PropertyChanged?.Invoke(this, new(nameof(HasInvalidQualityToken)));
            PropertyChanged?.Invoke(this, new(nameof(InvalidQualityMessage)));
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
        _qualityToken,
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
                ;
            QualityStatus = Loc.Format("Ui.Step.Camera.QualityFoundCount", modes.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (loadVersion != _qualityLoadVersion) return;
            AddBaseQualities();
            SelectedQuality = Qualities.FirstOrDefault(choice => choice.QualityMode == _qualityMode);
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

public sealed class GeneratedVisualOverlayEditorViewModel : INotifyPropertyChanged, IGeneratedCompositeInputEditor
{
    private readonly IReadOnlyList<SourceStepItem> _sources;
    private readonly StepInputDescriptor _detectionInputContract;
    private readonly StepInputDescriptor _textInputContract;
    private readonly Action<TextOverlayRowViewModel>? _chooseMonitor;
    private readonly IReadOnlyList<JobVariable> _variables;
    private readonly IReadOnlyList<ValueProviderSourceDescriptor> _providerSources;
    private readonly ValueReferencePickerContext? _detectionPickerContext;
    private readonly ValueReferencePickerContext? _textPickerContext;
    private readonly string _inputKeyPrefix;
    private readonly Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? _nestedInputResolver;
    private readonly ValueReferenceSourceCatalog? _sourceCatalog;

    public GeneratedVisualOverlayEditorViewModel(
        JsonNode? value,
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor detectionInputContract,
        StepInputDescriptor textInputContract,
        bool showDesktopOptions,
        Action<TextOverlayRowViewModel>? chooseMonitor = null,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
        ValueReferencePickerContext? detectionPickerContext = null,
        ValueReferencePickerContext? textPickerContext = null,
        string inputKeyPrefix = "overlay",
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel>? nestedInputResolver = null,
        ValueReferenceSourceCatalog? sourceCatalog = null)
    {
        _sources = sources;
        _detectionInputContract = detectionInputContract;
        _textInputContract = textInputContract;
        _chooseMonitor = chooseMonitor;
        _variables = variables ?? [];
        _providerSources = providerSources ?? [];
        _detectionPickerContext = detectionPickerContext;
        _textPickerContext = textPickerContext;
        _inputKeyPrefix = inputKeyPrefix;
        _nestedInputResolver = nestedInputResolver;
        _sourceCatalog = sourceCatalog;
        ShowOverlayDesktopOptions = showDesktopOptions;
        OverlayDetectionRows.CollectionChanged += OnCollectionChanged;
        OverlayTextRows.CollectionChanged += OnCollectionChanged;
        AddOverlayDetectionCommand = new RelayCommand(AddDetection);
        AddOverlayTextCommand = new RelayCommand(AddText);

        var settings = ReadSettings(value);
        foreach (var binding in settings.DetectionResults)
            OverlayDetectionRows.Add(new(OverlayDetectionRows, sources, detectionInputContract,
                _variables, _providerSources, binding, _detectionPickerContext, _sourceCatalog));
        foreach (var text in settings.TextResults)
            AddText(text);
    }

    public IEnumerable<GeneratedStepFieldViewModel> InputFields => OverlayDetectionRows.Select(row => row.SourceField).OfType<GeneratedStepFieldViewModel>()
        .Concat(OverlayTextRows.SelectMany(row => row.InputFields));
    public ObservableCollection<DetectionOverlayRowViewModel> OverlayDetectionRows { get; } = [];
    public ObservableCollection<TextOverlayRowViewModel> OverlayTextRows { get; } = [];
    public ICommand AddOverlayDetectionCommand { get; }
    public ICommand AddOverlayTextCommand { get; }
    public bool ShowOverlayDesktopOptions { get; }
    public IReadOnlyDictionary<string, ResultBinding> InputBindings => OverlayTextRows
        .SelectMany((row, index) => row.InputBindings.Select(pair => new KeyValuePair<string, ResultBinding>(
            $"{_inputKeyPrefix}.text_results.{index}.{pair.Key[(pair.Key.LastIndexOf('.') + 1)..]}", pair.Value)))
        .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    public VisualOverlaySettings ToValue() => new()
    {
        DetectionResults = OverlayDetectionRows.Select(row => row.Source.ToBinding()).ToList(),
        TextResults = OverlayTextRows.Select(row => row.ToSettings()).ToList()
    };

    private void AddDetection() =>
        OverlayDetectionRows.Add(new(OverlayDetectionRows, _sources, _detectionInputContract,
            _variables, _providerSources, pickerContext: _detectionPickerContext,
            sourceCatalog: _sourceCatalog));

    private void AddText() => AddText(null);

    private void AddText(TextResultOverlaySettings? settings)
    {
        var index = OverlayTextRows.Count;
        OverlayTextRows.Add(new(OverlayTextRows, _sources, _textInputContract, _chooseMonitor,
            _variables, _providerSources, settings, _textPickerContext,
            $"{_inputKeyPrefix}.text_results.{index}", _nestedInputResolver, _sourceCatalog));
    }

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
