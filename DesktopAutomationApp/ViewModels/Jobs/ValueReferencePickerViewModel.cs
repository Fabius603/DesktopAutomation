using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Windows.Input;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services.Jobs;
using MahApps.Metro.IconPacks;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels;

public sealed record ValueReferencePickerContext(
    string StepName,
    string FieldName,
    Func<StepInputDescriptor, JobVariable?>? CreateJobVariable = null,
    Func<Guid, int>? GetVariableUsageCount = null,
    Func<JobVariable, JobVariable?>? DetachStepValue = null,
    Func<JobVariable?>? CreateStepValue = null,
    Func<ValueProviderSourceDescriptor?>? CreateSecret = null);

public enum StepInputSourceKind
{
    Direct,
    JobVariable,
    StepResult,
    Secret,
    ExternalProvider
}
public class ValueReferencePickerViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<SourceStepItem> _sources;
    private readonly List<ValueProviderSourceDescriptor> _providerSources;
    private readonly Dictionary<string, JobVariable> _jobVariables;
    private readonly StepInputDescriptor _contract;
    private readonly ValueReferencePickerContext? _context;
    private readonly IValueReferenceDisplayFormatter _formatter;
    private IReadOnlyList<ConditionSelectionNode> _selectionTree = [];
    private SourceStepItem? _selectedSource;
    private ResultPropertyDescriptor? _selectedProperty;
    private ValueProviderSourceDescriptor? _selectedProviderSource;
    private ResultPropertyDescriptor? _selectedProviderProperty;
    private ResultBinding? _missingReference;
    private string _searchText = string.Empty;
    private bool _showIncompatible;
    private bool _inlineEditEnabled;
    private StepInputSourceKind _activeSourceKind = StepInputSourceKind.Direct;

    public ValueReferencePickerViewModel(
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor contract,
        bool selectDefault = true,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
        ValueReferencePickerContext? context = null,
        IValueReferenceDisplayFormatter? formatter = null)
    {
        var availableVariables = (variables ?? []).Where(variable => variable.Id != Guid.Empty).ToArray();
        _sources = sources.Where(source => !string.IsNullOrWhiteSpace(source.StepId)).ToArray();
        _contract = contract;
        _context = context;
        _formatter = formatter ?? ValueReferenceDisplayFormatter.Instance;
        _jobVariables = availableVariables.ToDictionary(
            variable => variable.Id.ToString("D"), variable => variable, StringComparer.OrdinalIgnoreCase);
        _providerSources = availableVariables.Select(ValueProviderSourceDescriptor.FromVariable)
            .Concat(providerSources ?? [])
            .Where(source => !string.IsNullOrWhiteSpace(source.ProviderId)
                             && !string.IsNullOrWhiteSpace(source.SourceId))
            .DistinctBy(source => (source.ProviderId, source.SourceId))
            .ToList();
        ClearCommand = new RelayCommand(Clear);
        CreateJobVariableCommand = new RelayCommand(CreateJobVariable, () => CanCreateJobVariable);
        ToggleIncompatibleCommand = new RelayCommand(() => ShowIncompatible = !ShowIncompatible);
        EditEverywhereCommand = new RelayCommand(EnableInlineEdit, () => RequiresInlineEditChoice);
        EditOnlyHereCommand = new RelayCommand(DetachStepValue, () => RequiresInlineEditChoice && _context?.DetachStepValue is not null);
        UseDirectValueCommand = new RelayCommand(UseDirectValue, () => CanUseDirectValue);
        UseJobVariableCommand = new RelayCommand(
            () => SelectSourceKind(StepInputSourceKind.JobVariable),
            () => CanUseJobVariables);
        UseStepResultCommand = new RelayCommand(
            () => SelectSourceKind(StepInputSourceKind.StepResult),
            () => CanUseStepResults);
        UseSecretCommand = new RelayCommand(
            () => SelectSourceKind(StepInputSourceKind.Secret),
            () => CanUseSecrets);
        UseExternalProviderCommand = new RelayCommand(
            () => SelectSourceKind(StepInputSourceKind.ExternalProvider),
            () => CanUseExternalProviders);
        CreateSecretCommand = new RelayCommand(CreateSecret, () => CanCreateSecret);
        SetActiveSourceKind(DefaultSourceKind());
        RebuildTree();
        if (selectDefault)
        {
            var source = contract.AllowsProvider(ValueProviderIds.StepResult)
                ? _sources.FirstOrDefault(s =>
                    contract.FindPreferredProperty(s.ResultType.Properties) is not null)
                : null;
            var property = source is null ? null : contract.FindPreferredProperty(source.ResultType.Properties);
            if (source is not null && property is not null) Select(source, property);
            else if (_providerSources.FirstOrDefault(Accepts) is { } providerSource) Select(providerSource);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<ConditionSelectionNode> SelectionTree => _selectionTree;
    public ICommand ClearCommand { get; }
    public ICommand CreateJobVariableCommand { get; }
    public ICommand ToggleIncompatibleCommand { get; }
    public ICommand EditEverywhereCommand { get; }
    public ICommand EditOnlyHereCommand { get; }
    public ICommand UseDirectValueCommand { get; }
    public ICommand UseJobVariableCommand { get; }
    public ICommand UseStepResultCommand { get; }
    public ICommand UseSecretCommand { get; }
    public ICommand UseExternalProviderCommand { get; }
    public ICommand CreateSecretCommand { get; }
    public StepInputSourceKind ActiveSourceKind => _activeSourceKind;
    public bool IsDirectSource => ActiveSourceKind == StepInputSourceKind.Direct;
    public bool IsJobVariableSource => ActiveSourceKind == StepInputSourceKind.JobVariable;
    public bool IsStepResultSource => ActiveSourceKind == StepInputSourceKind.StepResult;
    public bool IsSecretSource => ActiveSourceKind == StepInputSourceKind.Secret;
    public bool IsExternalProviderSource => ActiveSourceKind == StepInputSourceKind.ExternalProvider;
    public bool CanUseJobVariables => _contract.AllowsProvider(ValueProviderIds.JobVariable)
                                      && _contract.AcceptedShapes.Any(shape =>
                                          JobVariableEditorViewModel.SupportedKinds.Contains(shape.ValueKind));
    public bool CanUseStepResults => _contract.AllowsProvider(ValueProviderIds.StepResult);
    public bool CanUseSecrets => _contract.AllowsProvider(ValueProviderIds.Secret)
                                 && _contract.AcceptedShapes.Any(shape => shape.ValueKind == ResultValueKind.Text);
    public bool CanUseExternalProviders => _providerSources.Any(source =>
        source.ProviderId is not ValueProviderIds.LocalValue
            and not ValueProviderIds.JobVariable
            and not ValueProviderIds.StepResult
            and not ValueProviderIds.Secret
        && Accepts(source));
    public bool CanCreateSecret => IsSecretSource && _context?.CreateSecret is not null
                                  && _contract.AllowsProvider(ValueProviderIds.Secret)
                                  && _contract.AcceptedShapes.Any(shape => shape.ValueKind == ResultValueKind.Text);
    public bool CanClear => !_contract.Required;
    public bool CanCreateJobVariable => _context?.CreateJobVariable is not null
                                        && _contract.AllowsProvider(ValueProviderIds.JobVariable)
                                        && _contract.AcceptedShapes.Any(shape =>
                                            JobVariableEditorViewModel.SupportedKinds.Contains(shape.ValueKind));
    public bool CanUseDirectValue => _contract.AllowsDirectValue
                                     && _context?.CreateStepValue is not null;
    public bool CanSwitchSource => new[]
    {
        CanUseDirectValue,
        CanUseJobVariables,
        CanUseStepResults,
        CanUseSecrets,
        CanUseExternalProviders
    }.Count(available => available) > 1;
    public bool IsConfigured => _missingReference is not null
                                || _selectedProviderSource is not null
                                || _selectedSource is not null && _selectedProperty is not null;
    public bool HasMissingReference => _missingReference is not null;
    public JobVariable? SelectedJobVariable =>
        _selectedProviderSource is not null
        && _selectedProviderSource.ProviderId is ValueProviderIds.LocalValue or ValueProviderIds.JobVariable
        && _jobVariables.TryGetValue(_selectedProviderSource.SourceId, out var variable)
            ? variable
            : null;
    public bool IsStepValue => _selectedProviderSource?.ProviderId == ValueProviderIds.LocalValue
                               || SelectedJobVariable?.Scope == JobVariableScope.StepValue;
    public int SelectedVariableUsageCount => SelectedJobVariable is { } variable
        ? Math.Max(0, _context?.GetVariableUsageCount?.Invoke(variable.Id) ?? 1)
        : 0;
    public bool RequiresInlineEditChoice => IsStepValue && SelectedVariableUsageCount > 1 && !_inlineEditEnabled;
    public bool CanEditStepValueInline => IsStepValue && !RequiresInlineEditChoice;
    public string MultipleUsageText => Loc.Format("Ui.Job.Variables.Inline.MultipleUsage", SelectedVariableUsageCount);
    public string PickerContextText => _context is null
        ? string.Empty
        : Loc.Format("Ui.ValueReference.PickerContext", _context.FieldName, _context.StepName);
    public string ExpectedTypeText => Loc.Format(
        "Ui.ValueReference.ExpectedType",
        string.Join(", ", _contract.AcceptedShapes
            .Select(shape => _formatter.Type(
                shape.ValueKind,
                shape.Cardinalities.FirstOrDefault(ResultCardinality.Single)))
            .Distinct()));
    public bool ShowCreateVariableAction => IsJobVariableSource && CanCreateJobVariable;
    public int IncompatibleCount => CountIncompatible();
    public bool HasIncompatible => IncompatibleCount > 0;
    public string IncompatibleText => Loc.Format(
        ShowIncompatible
            ? "Ui.ValueReference.HideIncompatible"
            : "Ui.ValueReference.ShowIncompatible",
        IncompatibleCount);

    public bool ShowIncompatible
    {
        get => _showIncompatible;
        set
        {
            if (_showIncompatible == value) return;
            _showIncompatible = value;
            OnChange();
            OnChange(nameof(IncompatibleText));
            RebuildTree();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value ?? string.Empty;
            OnChange();
            RebuildTree();
        }
    }

    public string SelectedStepName => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Missing")
        : _selectedProviderSource is not null
            ? ProviderLabel(_selectedProviderSource.ProviderId)
            : _selectedSource is null
                ? string.Empty
                : $"{Loc.Get("Ui.ValueReference.ResultVariables")} → {_selectedSource.DisplayName}";
    public string SelectedPropertyName => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Missing")
        : _selectedProviderSource is not null && _selectedProviderProperty is not null
            ? $"{_selectedProviderSource.Name} › {_selectedProviderProperty.DisplayName.Replace(" / ", " › ", StringComparison.Ordinal)}"
        : _selectedProviderSource?.Name ?? _selectedProperty?.DisplayName.Replace(" / ", " › ", StringComparison.Ordinal)
          ?? Loc.Get("Ui.ValueReference.SelectVariable");
    public string SelectedCardinality => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Invalid")
        : _selectedProviderSource is not null
            ? _selectedProviderProperty is null
                ? ProviderSecondaryText(_selectedProviderSource)
                : _formatter.Type(_selectedProviderProperty.DataType, _selectedProviderProperty.Cardinality)
            : _selectedProperty is null ? string.Empty : _formatter.Type(
                _selectedProperty.DataType, _selectedProperty.Cardinality);
    public string SelectedDisplayPath => IsConfigured && _missingReference is null
        ? $"{SelectedStepName}  →  {SelectedPropertyName}"
        : SelectedPropertyName;
    public string SelectedPreviewValue => _missingReference is not null
        ? string.Empty
        : _selectedProviderSource is not null
            ? ProviderPreviewValue(_selectedProviderSource)
            : string.Empty;
    public string SelectedPreviewSource => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Missing")
        : _selectedProviderSource is not null
            ? ProviderLabel(_selectedProviderSource.ProviderId)
            : _selectedSource?.DisplayName ?? string.Empty;
    public string SelectedPreviewType => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Invalid")
        : _selectedProviderSource is not null
            ? _selectedProviderProperty is null
                ? _formatter.Type(_selectedProviderSource.ValueKind, _selectedProviderSource.Cardinality)
                : _formatter.Type(_selectedProviderProperty.DataType, _selectedProviderProperty.Cardinality)
            : _selectedProperty is null
                ? string.Empty
                : _formatter.Type(_selectedProperty.DataType, _selectedProperty.Cardinality);
    public string SelectedInlineText => string.IsNullOrWhiteSpace(SelectedPreviewValue)
        ? SelectedPropertyName
        : $"{SelectedPropertyName} · {SelectedPreviewValue}";
    public string SelectedTooltipValue => _missingReference is not null
        ? string.Empty
        : SelectedJobVariable is { } variable
            ? _formatter.FullValue(variable)
            : _selectedSource is not null && _selectedProperty is not null
                ? SelectedDisplayPath
                : string.Empty;
    public string SelectedTooltipDescription => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Missing.ToolTip")
        : _selectedProviderSource?.Description
          ?? _selectedProperty?.Description
          ?? string.Empty;

    public ResultBinding ToBinding()
    {
        if (_missingReference is not null) return _missingReference;
        if (_selectedProviderSource is not null)
            return new ResultBinding
            {
                ProviderId = _selectedProviderSource.ProviderId,
                SourceId = _selectedProviderSource.SourceId,
                ValuePath = _selectedProviderProperty?.Name
            };
        return _selectedSource is not null && _selectedProperty is not null
            ? ResultBinding.ForStepResult(_selectedSource.StepId, _selectedProperty.StableId)
            : new ResultBinding();
    }

    public void Load(ResultBinding? binding)
    {
        _missingReference = null;
        if (binding?.IsConfigured != true)
        {
            if (!_contract.Required) Clear();
            return;
        }
        if (binding.HasProviderReference
            && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal)
            && _providerSources.FirstOrDefault(source =>
                string.Equals(source.ProviderId, binding.ProviderId, StringComparison.Ordinal)
                && string.Equals(source.SourceId, binding.SourceId, StringComparison.OrdinalIgnoreCase)) is { } providerSource
            && (string.IsNullOrWhiteSpace(binding.ValuePath)
                ? Accepts(providerSource)
                : FindProviderProperty(providerSource, binding.ValuePath) is { } providerProperty
                  && _contract.Accepts(providerProperty)))
        {
            if (string.IsNullOrWhiteSpace(binding.ValuePath)) Select(providerSource);
            else Select(providerSource, FindProviderProperty(providerSource, binding.ValuePath)!);
            return;
        }
        var source = _sources.FirstOrDefault(item => item.StepId == binding.SourceStepId);
        var property = source?.ResultType.Properties.FirstOrDefault(item =>
            ((!string.IsNullOrWhiteSpace(binding.PropertyId)
              && item.StableId.Equals(binding.PropertyId, StringComparison.OrdinalIgnoreCase))
             || item.Name.Equals(binding.PropertyPath, StringComparison.OrdinalIgnoreCase))
            && _contract.AllowsProvider(ValueProviderIds.StepResult)
            && _contract.Accepts(item));
        if (source is not null && property is not null)
        {
            Select(source, property);
            return;
        }
        _selectedSource = null;
        _selectedProperty = null;
        _selectedProviderSource = null;
        _selectedProviderProperty = null;
        _missingReference = binding;
        SetActiveSourceKind(DefaultSourceKind());
        NotifySelection();
    }

    private void RebuildTree()
    {
        _selectionTree = ActiveSourceKind switch
        {
            StepInputSourceKind.JobVariable => CreateProviderEntries(
                    ValueProviderIds.JobVariable,
                    "Ui.ValueReference.Empty.JobVariables",
                    source => _jobVariables.TryGetValue(source.SourceId, out var variable)
                              && variable.Scope == JobVariableScope.Shared),
            StepInputSourceKind.StepResult => CreateResultEntries(),
            StepInputSourceKind.Secret => CreateProviderEntries(
                ValueProviderIds.Secret, "Ui.ValueReference.Empty.Secrets"),
            StepInputSourceKind.ExternalProvider => _providerSources
                .Where(source => source.ProviderId is not ValueProviderIds.LocalValue
                    and not ValueProviderIds.JobVariable
                    and not ValueProviderIds.StepResult
                    and not ValueProviderIds.Secret)
                .Where(AcceptsForNewSelection)
                .Where(source => MatchesSearch(
                    source.Name, source.Description, ProviderPreviewValue(source),
                    ProviderSecondaryText(source), ProviderSourceText(source)))
                .OrderBy(source => source.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(source => CreateProviderNode(source, true))
                .ToArray(),
            _ => []
        };
        OnChange(nameof(SelectionTree));
    }

    private IReadOnlyList<ConditionSelectionNode> CreateProviderEntries(
        string providerId,
        string emptyKey,
        Func<ValueProviderSourceDescriptor, bool>? filter = null)
    {
        var providerAllowed = _contract.AllowsProvider(providerId);
        var entries = _providerSources.Where(source =>
                string.Equals(source.ProviderId, providerId, StringComparison.Ordinal))
            .Where(source => filter?.Invoke(source) ?? true)
            .Where(source => providerAllowed && HasSelectableProviderValue(source))
            .Where(MatchesProviderSearch)
            .OrderBy(source => source.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(source => CreateProviderNode(source, providerAllowed && AcceptsForNewSelection(source)))
            .ToList();
        if (entries.Count == 0)
            entries.Add(EmptyNode(Loc.Get(!string.IsNullOrWhiteSpace(SearchText)
                ? "Ui.ValueReference.Empty.Search"
                : providerAllowed ? emptyKey : "Ui.ValueReference.ProviderNotAllowed")));
        return entries;
    }

    private ConditionSelectionNode? CreateRecommendedGroup()
    {
        if (!string.IsNullOrWhiteSpace(SearchText)) return null;
        var entries = new List<ConditionSelectionNode>();
        entries.AddRange(_providerSources
            .Where(source => source.ProviderId != ValueProviderIds.LocalValue)
            .Where(AcceptsForNewSelection)
            .OrderBy(source => _jobVariables.TryGetValue(source.SourceId, out var variable)
                               && variable.Scope == JobVariableScope.Shared ? 0 : 1)
            .Take(3)
            .Select(source => CreateProviderNode(source, true)));
        foreach (var source in _sources)
        {
            var property = _contract.FindPreferredProperty(source.ResultType.Properties);
            if (property is null || !_contract.AllowsProvider(ValueProviderIds.StepResult)) continue;
            entries.Add(new ConditionSelectionNode(
                $"{source.DisplayName} · {property.DisplayName}",
                selectCommand: new RelayCommand(() => Select(source, property)),
                secondaryText: _formatter.Type(property.DataType, property.Cardinality),
                description: property.Description,
                icon: PackIconMaterialKind.SourceBranch,
                sourceText: Loc.Get("Ui.ValueReference.Result"),
                isSelected: IsSelected(source, property)));
            if (entries.Count >= 5) break;
        }
        return entries.Count == 0 ? null : new ConditionSelectionNode(
            Loc.Get("Ui.ValueReference.Recommended"), entries,
            icon: PackIconMaterialKind.StarOutline, isExpanded: true);
    }

    private IReadOnlyList<ConditionSelectionNode> CreateResultEntries()
    {
        var providerAllowed = _contract.AllowsProvider(ValueProviderIds.StepResult);
        var entries = _sources.Select(source => CreateSourceNode(source, providerAllowed))
            .Where(node => node is not null).Cast<ConditionSelectionNode>().ToList();
        if (entries.Count == 0)
            entries.Add(EmptyNode(Loc.Get(!string.IsNullOrWhiteSpace(SearchText)
                ? "Ui.ValueReference.Empty.Search"
                : providerAllowed
                    ? "Ui.ValueReference.Empty.ResultVariables"
                    : "Ui.ValueReference.ProviderNotAllowed")));
        return entries;
    }

    private ConditionSelectionNode? CreateSourceNode(SourceStepItem source, bool providerAllowed)
    {
        var sourceMatches = MatchesSearch(source.DisplayName);
        var children = source.ResultType.PropertyTree
            .Select(node => CreateResultNode(source, node, providerAllowed, sourceMatches))
            .Where(node => node is not null).Cast<ConditionSelectionNode>().ToArray();
        return children.Length == 0 ? null : new ConditionSelectionNode(
            source.DisplayName, children);
    }

    private ConditionSelectionNode? CreateResultNode(
        SourceStepItem source,
        ResultPropertyNode node,
        bool providerAllowed,
        bool ancestorMatches)
    {
        var nodeMatches = ancestorMatches || MatchesSearch(
            node.DisplayName,
            node.Property?.DisplayName,
            node.Property?.Description,
            node.Property is null ? null : _formatter.Type(node.Property.DataType, node.Property.Cardinality));
        var children = node.Children.Select(child => CreateResultNode(source, child, providerAllowed, nodeMatches))
            .Where(child => child is not null).Cast<ConditionSelectionNode>().ToList();
        var compatible = node.Property is not null && providerAllowed && _contract.Accepts(node.Property);
        if ((!compatible || !nodeMatches) && children.Count == 0) return null;
        if (node.Property is not null && compatible && nodeMatches)
        {
            var current = new ConditionSelectionNode(
                children.Count > 0 ? Loc.Get("Ui.Step.IfEditor.CompleteValue") : node.DisplayName,
                selectCommand: new RelayCommand(() => Select(source, node.Property)),
                secondaryText: _formatter.Type(node.Property.DataType, node.Property.Cardinality),
                description: node.Property.Description,
                icon: TypeIcon(node.Property.DataType),
                sourceText: source.DisplayName,
                isSelected: IsSelected(source, node.Property));
            if (children.Count == 0) return current;
            children.Insert(0, current);
        }
        return new ConditionSelectionNode(node.DisplayName, children);
    }

    private ConditionSelectionNode CreateProviderNode(ValueProviderSourceDescriptor source, bool compatible)
    {
        var properties = ProviderProperties(source);
        var sourceMatches = MatchesSearch(
            source.Name, source.Description, ProviderPreviewValue(source),
            ProviderSecondaryText(source), ProviderSourceText(source));
        IReadOnlyList<ConditionSelectionNode> children = ResultPropertyTree.Create(properties)
            .Select(node => CreateProviderPropertyNode(source, node, sourceMatches))
            .Where(node => node is not null).Cast<ConditionSelectionNode>().ToArray();
        if (children.Count == 0
            && source.ProviderId is ValueProviderIds.LocalValue or ValueProviderIds.JobVariable
            && _jobVariables.TryGetValue(source.SourceId, out var structuredVariable))
            children = CreateValueNodes(structuredVariable.Value);
        compatible = compatible && AcceptsForNewSelection(source);
        var valueText = ProviderPreviewValue(source);
        var fullValueText = source.IsSensitive
            ? valueText
            : _jobVariables.TryGetValue(source.SourceId, out var selectedVariable)
                ? _formatter.FullValue(selectedVariable)
                : valueText;
        return new ConditionSelectionNode(
            source.Name,
            children,
            selectCommand: compatible ? new RelayCommand(() => Select(source)) : null,
            secondaryText: _formatter.Type(source.ValueKind, source.Cardinality),
            description: source.Description,
            icon: TypeIcon(source.ValueKind),
            isEnabled: compatible || children.Count > 0,
            sourceText: ProviderSourceText(source),
            isSelected: IsSelected(source),
            valueText: valueText,
            fullValueText: fullValueText,
            colorPreview: source.ValueKind == ResultValueKind.Color ? valueText : null);
    }

    private ConditionSelectionNode? CreateProviderPropertyNode(
        ValueProviderSourceDescriptor source,
        ResultPropertyNode node,
        bool ancestorMatches)
    {
        var nodeMatches = ancestorMatches || MatchesSearch(
            node.DisplayName, node.Property?.DisplayName, node.Property?.Description,
            node.Property is null ? null : _formatter.Type(node.Property.DataType, node.Property.Cardinality));
        var children = node.Children
            .Select(child => CreateProviderPropertyNode(source, child, nodeMatches))
            .Where(child => child is not null).Cast<ConditionSelectionNode>().ToList();
        var compatible = node.Property is not null
                         && _contract.AllowsProvider(source.ProviderId)
                         && _contract.Accepts(
                             node.Property.DataType, node.Property.Cardinality, includeLegacy: false);
        if ((!compatible || !nodeMatches) && children.Count == 0) return null;
        if (node.Property is not null && compatible && nodeMatches)
        {
            var current = new ConditionSelectionNode(
                children.Count > 0 ? Loc.Get("Ui.Step.IfEditor.CompleteValue") : node.DisplayName,
                selectCommand: new RelayCommand(() => Select(source, node.Property)),
                secondaryText: _formatter.Type(node.Property.DataType, node.Property.Cardinality),
                description: node.Property.Description,
                icon: TypeIcon(node.Property.DataType),
                sourceText: source.Name,
                isSelected: IsSelected(source, node.Property));
            if (children.Count == 0) return current;
            children.Insert(0, current);
        }
        return new ConditionSelectionNode(node.DisplayName, children);
    }

    private IReadOnlyList<ResultPropertyDescriptor> ProviderProperties(ValueProviderSourceDescriptor source) =>
        source.ProviderId is ValueProviderIds.LocalValue or ValueProviderIds.JobVariable
        && _jobVariables.TryGetValue(source.SourceId, out var variable)
            ? JobVariablePropertyMetadata.GetProperties(variable)
            : [];

    private ResultPropertyDescriptor? FindProviderProperty(ValueProviderSourceDescriptor source, string path) =>
        ProviderProperties(source).FirstOrDefault(property =>
            property.Name.Equals(path, StringComparison.OrdinalIgnoreCase));

    private bool HasSelectableProviderValue(ValueProviderSourceDescriptor source) =>
        AcceptsForNewSelection(source)
        || ProviderProperties(source).Any(property => _contract.Accepts(
            property.DataType, property.Cardinality, includeLegacy: false));

    private bool MatchesProviderSearch(ValueProviderSourceDescriptor source) =>
        MatchesSearch(source.Name, source.Description, ProviderPreviewValue(source),
            ProviderSecondaryText(source), ProviderSourceText(source))
        || ProviderProperties(source).Any(property => MatchesSearch(
            property.Name, property.DisplayName, property.Description));

    private static IReadOnlyList<ConditionSelectionNode> CreateValueNodes(JsonNode? value)
    {
        if (value is JsonArray array)
            return array.Select((item, index) => new ConditionSelectionNode(
                DisplayValue(item, index + 1), CreateValueNodes(item))).ToArray();
        if (value is JsonObject objectValue)
            return objectValue.Select(property => new ConditionSelectionNode(
                property.Key,
                property.Value is JsonObject or JsonArray
                    ? CreateValueNodes(property.Value)
                    : [new ConditionSelectionNode(DisplayValue(property.Value))])).ToArray();
        return [];
    }

    private static string DisplayValue(JsonNode? value, int? index = null)
    {
        var text = value switch
        {
            null => Loc.Get("Ui.ValueReference.EmptyValue"),
            JsonValue json when json.TryGetValue<string>(out var stringValue) => stringValue,
            _ => value.ToJsonString()
        };
        return index is null ? text : $"{index}: {text}";
    }

    private static ConditionSelectionNode EmptyNode(string text) => new(
        text, icon: PackIconMaterialKind.InformationOutline, isEnabled: false);

    private string ProviderSecondaryText(ValueProviderSourceDescriptor source)
    {
        if (source.IsSensitive) return Loc.Get("Ui.ValueReference.Sensitive");
        var type = _formatter.Type(source.ValueKind, source.Cardinality);
        return source.ProviderId is ValueProviderIds.LocalValue or ValueProviderIds.JobVariable
               && _jobVariables.TryGetValue(source.SourceId, out var variable)
            ? $"{_formatter.CompactValue(variable)} · {type}"
            : type;
    }

    private string ProviderPreviewValue(ValueProviderSourceDescriptor source)
    {
        if (source.IsSensitive) return "••••••••";
        return source.ProviderId is ValueProviderIds.LocalValue or ValueProviderIds.JobVariable
               && _jobVariables.TryGetValue(source.SourceId, out var variable)
            ? _formatter.CompactValue(variable)
            : source.Description;
    }

    private string ProviderDescription(ValueProviderSourceDescriptor source)
    {
        if (source.IsSensitive) return source.Description;
        if (source.ProviderId is not ValueProviderIds.LocalValue and not ValueProviderIds.JobVariable
            || !_jobVariables.TryGetValue(source.SourceId, out var variable))
            return source.Description;
        var value = _formatter.FullValue(variable);
        return string.IsNullOrWhiteSpace(source.Description)
            ? value
            : $"{source.Description}{Environment.NewLine}{value}";
    }

    private string ProviderSourceText(ValueProviderSourceDescriptor source)
    {
        if (source.ProviderId is not ValueProviderIds.LocalValue and not ValueProviderIds.JobVariable)
            return ProviderLabel(source.ProviderId);
        return source.ProviderId == ValueProviderIds.LocalValue
            ? Loc.Get("Ui.Job.Variables.Scope.StepValues")
            : Loc.Get("Ui.Job.Variables.Scope.Shared");
    }

    private bool IsSelected(ValueProviderSourceDescriptor source) =>
        _selectedProviderSource is not null
        && _selectedProviderProperty is null
        && string.Equals(_selectedProviderSource.ProviderId, source.ProviderId, StringComparison.Ordinal)
        && string.Equals(_selectedProviderSource.SourceId, source.SourceId, StringComparison.OrdinalIgnoreCase);

    private bool IsSelected(ValueProviderSourceDescriptor source, ResultPropertyDescriptor property) =>
        _selectedProviderSource is not null
        && _selectedProviderProperty is not null
        && string.Equals(_selectedProviderSource.ProviderId, source.ProviderId, StringComparison.Ordinal)
        && string.Equals(_selectedProviderSource.SourceId, source.SourceId, StringComparison.OrdinalIgnoreCase)
        && string.Equals(_selectedProviderProperty.Name, property.Name, StringComparison.OrdinalIgnoreCase);

    private bool IsSelected(SourceStepItem source, ResultPropertyDescriptor property) =>
        _selectedSource?.StepId == source.StepId
        && string.Equals(_selectedProperty?.StableId, property.StableId, StringComparison.OrdinalIgnoreCase);

    private void CreateJobVariable()
    {
        var variable = _context?.CreateJobVariable?.Invoke(_contract);
        if (variable is null || variable.Id == Guid.Empty) return;
        variable.Scope = JobVariableScope.Shared;
        var sourceId = variable.Id.ToString("D");
        _jobVariables[sourceId] = variable;
        _providerSources.RemoveAll(source => string.Equals(source.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
                                             && string.Equals(source.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));
        var descriptor = ValueProviderSourceDescriptor.FromVariable(variable);
        _providerSources.Add(descriptor);
        RebuildTree();
        Select(descriptor);
    }

    private void CreateSecret()
    {
        var source = _context?.CreateSecret?.Invoke();
        if (source is null || !string.Equals(source.ProviderId, ValueProviderIds.Secret, StringComparison.Ordinal)) return;
        _providerSources.RemoveAll(candidate =>
            string.Equals(candidate.ProviderId, source.ProviderId, StringComparison.Ordinal)
            && string.Equals(candidate.SourceId, source.SourceId, StringComparison.OrdinalIgnoreCase));
        _providerSources.Add(source);
        Select(source);
    }

    private void Select(SourceStepItem source, ResultPropertyDescriptor property)
    {
        SetActiveSourceKind(StepInputSourceKind.StepResult);
        _inlineEditEnabled = false;
        _missingReference = null;
        _selectedProviderSource = null;
        _selectedProviderProperty = null;
        _selectedSource = source;
        _selectedProperty = property;
        NotifySelection();
    }

    private void Select(ValueProviderSourceDescriptor source)
    {
        SetActiveSourceKind(SourceKind(source));
        _inlineEditEnabled = false;
        _missingReference = null;
        _selectedSource = null;
        _selectedProperty = null;
        _selectedProviderSource = source;
        _selectedProviderProperty = null;
        NotifySelection();
    }

    private void Select(ValueProviderSourceDescriptor source, ResultPropertyDescriptor property)
    {
        SetActiveSourceKind(SourceKind(source));
        _inlineEditEnabled = false;
        _missingReference = null;
        _selectedSource = null;
        _selectedProperty = null;
        _selectedProviderSource = source;
        _selectedProviderProperty = property;
        NotifySelection();
    }

    private void Clear()
    {
        _inlineEditEnabled = false;
        _missingReference = null;
        _selectedSource = null;
        _selectedProperty = null;
        _selectedProviderSource = null;
        _selectedProviderProperty = null;
        NotifySelection();
    }

    private void NotifySelection()
    {
        RebuildTree();
        OnChange(nameof(SelectedStepName));
        OnChange(nameof(SelectedDisplayPath));
        OnChange(nameof(SelectedPropertyName));
        OnChange(nameof(SelectedCardinality));
        OnChange(nameof(SelectedPreviewValue));
        OnChange(nameof(SelectedPreviewSource));
        OnChange(nameof(SelectedPreviewType));
        OnChange(nameof(SelectedInlineText));
        OnChange(nameof(SelectedTooltipValue));
        OnChange(nameof(SelectedTooltipDescription));
        OnChange(nameof(IsConfigured));
        OnChange(nameof(HasMissingReference));
        OnChange(nameof(SelectedJobVariable));
        OnChange(nameof(IsStepValue));
        OnChange(nameof(SelectedVariableUsageCount));
        OnChange(nameof(RequiresInlineEditChoice));
        OnChange(nameof(CanEditStepValueInline));
        OnChange(nameof(MultipleUsageText));
        (EditEverywhereCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (EditOnlyHereCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void EnableInlineEdit()
    {
        _inlineEditEnabled = true;
        NotifySelection();
    }

    private void DetachStepValue()
    {
        if (SelectedJobVariable is not { } current) return;
        var detached = _context?.DetachStepValue?.Invoke(current);
        if (detached is null || detached.Id == Guid.Empty) return;
        var sourceId = detached.Id.ToString("D");
        _jobVariables[sourceId] = detached;
        _providerSources.Add(ValueProviderSourceDescriptor.FromVariable(detached));
        _inlineEditEnabled = true;
        RebuildTree();
        Select(ValueProviderSourceDescriptor.FromVariable(detached));
        _inlineEditEnabled = true;
        NotifySelection();
    }

    private void UseDirectValue()
    {
        if (!CanUseDirectValue) return;
        SetActiveSourceKind(StepInputSourceKind.Direct);
        var variable = _context?.CreateStepValue?.Invoke();
        if (variable is null || variable.Id == Guid.Empty) return;
        variable.Scope = JobVariableScope.StepValue;
        var sourceId = variable.Id.ToString("D");
        _jobVariables[sourceId] = variable;
        _providerSources.RemoveAll(source =>
            source.ProviderId == ValueProviderIds.LocalValue
            && string.Equals(source.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));
        var descriptor = ValueProviderSourceDescriptor.FromVariable(variable);
        _providerSources.Add(descriptor);
        Select(descriptor);
    }

    public void SelectSourceKind(StepInputSourceKind kind)
    {
        if (!AllowsSourceKind(kind)) return;
        if (kind == StepInputSourceKind.Direct)
        {
            UseDirectValue();
            return;
        }

        SetActiveSourceKind(kind);
        if (!SelectionMatches(kind))
        {
            _missingReference = null;
            _selectedSource = null;
            _selectedProperty = null;
            _selectedProviderSource = null;
            _selectedProviderProperty = null;
        }
        NotifySelection();
    }

    private bool SelectionMatches(StepInputSourceKind kind) => kind switch
    {
        StepInputSourceKind.StepResult => _selectedSource is not null && _selectedProperty is not null,
        StepInputSourceKind.JobVariable => _selectedProviderSource is not null
            && SourceKind(_selectedProviderSource) == StepInputSourceKind.JobVariable,
        StepInputSourceKind.Secret => _selectedProviderSource is not null
            && SourceKind(_selectedProviderSource) == StepInputSourceKind.Secret,
        StepInputSourceKind.ExternalProvider => _selectedProviderSource is not null
            && SourceKind(_selectedProviderSource) == StepInputSourceKind.ExternalProvider,
        StepInputSourceKind.Direct => IsStepValue,
        _ => false
    };

    private StepInputSourceKind SourceKind(ValueProviderSourceDescriptor source)
    {
        if (string.Equals(source.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal))
            return StepInputSourceKind.Direct;
        if (string.Equals(source.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal))
            return _jobVariables.TryGetValue(source.SourceId, out var variable)
                   && variable.Scope == JobVariableScope.StepValue
                ? StepInputSourceKind.Direct
                : StepInputSourceKind.JobVariable;
        if (string.Equals(source.ProviderId, ValueProviderIds.Secret, StringComparison.Ordinal))
            return StepInputSourceKind.Secret;
        if (string.Equals(source.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
            return StepInputSourceKind.StepResult;
        return StepInputSourceKind.ExternalProvider;
    }

    private void SetActiveSourceKind(StepInputSourceKind kind)
    {
        if (_activeSourceKind == kind) return;
        _activeSourceKind = kind;
        OnChange(nameof(ActiveSourceKind));
        OnChange(nameof(IsDirectSource));
        OnChange(nameof(IsJobVariableSource));
        OnChange(nameof(IsStepResultSource));
        OnChange(nameof(IsSecretSource));
        OnChange(nameof(IsExternalProviderSource));
        OnChange(nameof(CanCreateSecret));
        OnChange(nameof(ShowCreateVariableAction));
        (CreateSecretCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public void RefreshSelectedValue()
    {
        RebuildTree();
        NotifySelection();
    }

    private int CountIncompatible()
    {
        var providerValues = _providerSources.Count(source =>
            !_contract.AllowsProvider(source.ProviderId) || !Accepts(source));
        var stepValues = _sources.Sum(source => source.ResultType.Properties.Count(property =>
            !_contract.AllowsProvider(ValueProviderIds.StepResult) || !_contract.Accepts(property)));
        return providerValues + stepValues;
    }

    private bool Accepts(ValueProviderSourceDescriptor source) =>
        IsDirectStepValue(source)
            ? _contract.AllowsDirectValue
              && _contract.Accepts(source.ValueKind, source.Cardinality)
            : _contract.AllowsProvider(source.ProviderId)
              && _contract.Accepts(source.ValueKind, source.Cardinality);

    private bool AcceptsForNewSelection(ValueProviderSourceDescriptor source) =>
        Accepts(source)
        && (source.ProviderId != ValueProviderIds.JobVariable
            || !_jobVariables.TryGetValue(source.SourceId, out var variable)
            || JobVariableEditorViewModel.SupportedKinds.Contains(variable.ValueKind))
        && _contract.Accepts(source.ValueKind, source.Cardinality, includeLegacy: false);

    private bool IsDirectStepValue(ValueProviderSourceDescriptor source) =>
        string.Equals(source.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal)
        && _jobVariables.ContainsKey(source.SourceId)
        || string.Equals(source.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
        && _jobVariables.TryGetValue(source.SourceId, out var variable)
        && variable.Scope == JobVariableScope.StepValue;

    private bool AllowsSourceKind(StepInputSourceKind kind) => kind switch
    {
        StepInputSourceKind.Direct => CanUseDirectValue,
        StepInputSourceKind.JobVariable => CanUseJobVariables,
        StepInputSourceKind.StepResult => _contract.AllowsProvider(ValueProviderIds.StepResult),
        StepInputSourceKind.Secret => CanUseSecrets,
        StepInputSourceKind.ExternalProvider => CanUseExternalProviders,
        _ => false
    };

    private StepInputSourceKind DefaultSourceKind()
    {
        if (_contract.AllowsDirectValue) return StepInputSourceKind.Direct;
        if (_contract.AllowsProvider(ValueProviderIds.StepResult)) return StepInputSourceKind.StepResult;
        if (_contract.AllowsProvider(ValueProviderIds.JobVariable)) return StepInputSourceKind.JobVariable;
        if (_contract.AllowsProvider(ValueProviderIds.Secret)) return StepInputSourceKind.Secret;
        return StepInputSourceKind.ExternalProvider;
    }

    private bool MatchesSearch(params string?[] values) => string.IsNullOrWhiteSpace(SearchText)
        || values.Any(value => value?.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) == true);

    private string IncompatibleReason(ResultValueKind actual) => Loc.Format(
        "Ui.ValueReference.Incompatible",
        _formatter.Type(actual, ResultCardinality.Single),
        string.Join(", ", _contract.AcceptedShapes.Select(shape =>
            _formatter.Type(shape.ValueKind, shape.Cardinalities.FirstOrDefault(ResultCardinality.Single))).Distinct()));

    private static PackIconMaterialKind TypeIcon(ResultValueKind kind) => kind switch
    {
        ResultValueKind.Text or ResultValueKind.Enum => PackIconMaterialKind.FormatText,
        ResultValueKind.Boolean => PackIconMaterialKind.CheckboxMarkedOutline,
        ResultValueKind.Integer or ResultValueKind.Number => PackIconMaterialKind.Numeric,
        ResultValueKind.DateTime => PackIconMaterialKind.CalendarClock,
        ResultValueKind.Point => PackIconMaterialKind.CrosshairsGps,
        ResultValueKind.Rectangle => PackIconMaterialKind.RectangleOutline,
        ResultValueKind.Color => PackIconMaterialKind.PaletteOutline,
        ResultValueKind.FilePath => PackIconMaterialKind.FileOutline,
        ResultValueKind.Image => PackIconMaterialKind.ImageOutline,
        _ => PackIconMaterialKind.Variable
    };

    private static string ProviderLabel(string providerId) => providerId switch
    {
        ValueProviderIds.LocalValue => Loc.Get("Ui.ValueReference.DirectValue"),
        ValueProviderIds.JobVariable => Loc.Get("Ui.ValueReference.JobVariables"),
        ValueProviderIds.StepResult => Loc.Get("Ui.ValueReference.ResultVariables"),
        ValueProviderIds.Secret => Loc.Get("Ui.ValueReference.Secrets"),
        _ => providerId
    };

    private void OnChange([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
