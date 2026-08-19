using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    Func<JobVariable?>? CreateStepValue = null);

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
    private ResultBinding? _missingReference;
    private string _searchText = string.Empty;
    private bool _showIncompatible;
    private bool _inlineEditEnabled;

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
        UseDirectValueCommand = new RelayCommand(UseDirectValue, () => _context?.CreateStepValue is not null);
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
    public bool CanClear => !_contract.Required;
    public bool CanCreateJobVariable => _context?.CreateJobVariable is not null
                                        && _contract.AllowsProvider(ValueProviderIds.JobVariable)
                                        && _contract.AcceptedShapes.Any(shape =>
                                            JobVariableEditorViewModel.SupportedKinds.Contains(shape.ValueKind));
    public bool IsConfigured => _missingReference is not null
                                || _selectedProviderSource is not null
                                || _selectedSource is not null && _selectedProperty is not null;
    public bool HasMissingReference => _missingReference is not null;
    public JobVariable? SelectedJobVariable =>
        _selectedProviderSource is not null
        && string.Equals(_selectedProviderSource.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
        && _jobVariables.TryGetValue(_selectedProviderSource.SourceId, out var variable)
            ? variable
            : null;
    public bool IsStepValue => SelectedJobVariable?.Scope == JobVariableScope.StepValue;
    public int SelectedVariableUsageCount => SelectedJobVariable is { } variable
        ? Math.Max(0, _context?.GetVariableUsageCount?.Invoke(variable.Id) ?? 1)
        : 0;
    public bool RequiresInlineEditChoice => IsStepValue && SelectedVariableUsageCount > 1 && !_inlineEditEnabled;
    public bool CanEditStepValueInline => IsStepValue && !RequiresInlineEditChoice;
    public string MultipleUsageText => Loc.Format("Ui.Job.Variables.Inline.MultipleUsage", SelectedVariableUsageCount);
    public string PickerContextText => _context is null
        ? string.Empty
        : Loc.Format("Ui.ValueReference.PickerContext", _context.FieldName, _context.StepName);
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
        : _selectedProviderSource?.Name ?? _selectedProperty?.DisplayName
          ?? Loc.Get("Ui.ValueReference.SelectVariable");
    public string SelectedCardinality => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Invalid")
        : _selectedProviderSource is not null
            ? ProviderSecondaryText(_selectedProviderSource)
            : _selectedProperty is null ? string.Empty : _formatter.Type(
                _selectedProperty.DataType, _selectedProperty.Cardinality);
    public string SelectedDisplayPath => IsConfigured && _missingReference is null
        ? $"{SelectedStepName}  →  {SelectedPropertyName}"
        : SelectedPropertyName;
    public string SelectedPreviewValue => _missingReference is not null
        ? string.Empty
        : _selectedProviderSource is not null
            ? ProviderPreviewValue(_selectedProviderSource)
            : _selectedProperty?.Description ?? string.Empty;
    public string SelectedPreviewSource => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Missing")
        : _selectedProviderSource is not null
            ? ProviderLabel(_selectedProviderSource.ProviderId)
            : _selectedSource?.DisplayName ?? string.Empty;
    public string SelectedPreviewType => _missingReference is not null
        ? Loc.Get("Ui.ValueReference.Invalid")
        : _selectedProviderSource is not null
            ? _formatter.Type(_selectedProviderSource.ValueKind, _selectedProviderSource.Cardinality)
            : _selectedProperty is null
                ? string.Empty
                : _formatter.Type(_selectedProperty.DataType, _selectedProperty.Cardinality);

    public ResultBinding ToBinding()
    {
        if (_missingReference is not null) return _missingReference;
        if (_selectedProviderSource is not null)
            return new ResultBinding
            {
                ProviderId = _selectedProviderSource.ProviderId,
                SourceId = _selectedProviderSource.SourceId
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
            && Accepts(providerSource))
        {
            Select(providerSource);
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
        _missingReference = binding;
        NotifySelection();
    }

    private void RebuildTree()
    {
        var nodes = new List<ConditionSelectionNode>();
        if (CreateRecommendedGroup() is { } recommended) nodes.Add(recommended);
        nodes.Add(CreateProviderGroup(ValueProviderIds.JobVariable, PackIconMaterialKind.CodeBraces,
            "Ui.ValueReference.Empty.JobVariables", includeCreateAction: true,
            source => _jobVariables.TryGetValue(source.SourceId, out var variable)
                      && variable.Scope == JobVariableScope.Shared,
            "Ui.Job.Variables.Scope.Shared"));
        nodes.Add(CreateProviderGroup(ValueProviderIds.JobVariable, PackIconMaterialKind.FormTextbox,
            "Ui.ValueReference.Empty.JobVariables", includeCreateAction: false,
            source => _jobVariables.TryGetValue(source.SourceId, out var variable)
                      && variable.Scope == JobVariableScope.StepValue,
            "Ui.Job.Variables.Scope.StepValues"));
        nodes.Add(CreateResultGroup());
        nodes.Add(CreateProviderGroup(ValueProviderIds.Secret, PackIconMaterialKind.LockOutline,
            "Ui.ValueReference.Empty.Secrets"));
        foreach (var providerId in _providerSources.Select(source => source.ProviderId)
                     .Where(id => id is not ValueProviderIds.JobVariable and not ValueProviderIds.Secret)
                     .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.CurrentCultureIgnoreCase))
            nodes.Add(CreateProviderGroup(providerId, PackIconMaterialKind.DatabaseOutline,
                "Ui.ValueReference.Empty.Provider"));
        _selectionTree = nodes;
        OnChange(nameof(SelectionTree));
        OnChange(nameof(IncompatibleCount));
        OnChange(nameof(HasIncompatible));
        OnChange(nameof(IncompatibleText));
    }

    private ConditionSelectionNode CreateProviderGroup(
        string providerId,
        PackIconMaterialKind icon,
        string emptyKey,
        bool includeCreateAction = false,
        Func<ValueProviderSourceDescriptor, bool>? filter = null,
        string? labelKey = null)
    {
        var providerAllowed = _contract.AllowsProvider(providerId);
        var entries = _providerSources.Where(source =>
                string.Equals(source.ProviderId, providerId, StringComparison.Ordinal))
            .Where(source => filter?.Invoke(source) ?? true)
            .Where(source => MatchesSearch(source.Name, source.Description, ProviderLabel(providerId)))
            .Where(source => providerAllowed && Accepts(source) || ShowIncompatible)
            .OrderBy(source => source.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(source => CreateProviderNode(source, providerAllowed && Accepts(source)))
            .ToList();
        if (entries.Count == 0)
            entries.Add(EmptyNode(providerAllowed
                ? Loc.Get(string.IsNullOrWhiteSpace(SearchText) ? emptyKey : "Ui.ValueReference.Empty.Search")
                : Loc.Get("Ui.ValueReference.ProviderNotAllowed")));
        if (includeCreateAction)
            entries.Add(new ConditionSelectionNode(
                Loc.Get("Ui.ValueReference.CreateVariable"),
                selectCommand: CanCreateJobVariable ? CreateJobVariableCommand : null,
                description: CanCreateJobVariable
                    ? Loc.Get("Ui.ValueReference.CreateVariable.Hint")
                    : Loc.Get("Ui.ValueReference.CreateVariable.Unsupported"),
                icon: PackIconMaterialKind.Plus,
                isEnabled: CanCreateJobVariable));
        return new ConditionSelectionNode(
            labelKey is null ? ProviderLabel(providerId) : Loc.Get(labelKey), entries, icon: icon,
            isExpanded: !string.IsNullOrWhiteSpace(SearchText));
    }

    private ConditionSelectionNode? CreateRecommendedGroup()
    {
        if (!string.IsNullOrWhiteSpace(SearchText)) return null;
        var entries = new List<ConditionSelectionNode>();
        entries.AddRange(_providerSources
            .Where(Accepts)
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

    private ConditionSelectionNode CreateResultGroup()
    {
        var providerAllowed = _contract.AllowsProvider(ValueProviderIds.StepResult);
        var entries = _sources.Select(source => CreateSourceNode(source, providerAllowed))
            .Where(node => node is not null).Cast<ConditionSelectionNode>().ToList();
        if (entries.Count == 0)
            entries.Add(EmptyNode(providerAllowed
                ? Loc.Get(string.IsNullOrWhiteSpace(SearchText)
                    ? "Ui.ValueReference.Empty.ResultVariables"
                    : "Ui.ValueReference.Empty.Search")
                : Loc.Get("Ui.ValueReference.ProviderNotAllowed")));
        return new ConditionSelectionNode(
            Loc.Get("Ui.ValueReference.ResultVariables"), entries,
            icon: PackIconMaterialKind.SourceBranch,
            isExpanded: !string.IsNullOrWhiteSpace(SearchText));
    }

    private ConditionSelectionNode? CreateSourceNode(SourceStepItem source, bool providerAllowed)
    {
        var children = source.ResultType.PropertyTree
            .Select(node => CreateResultNode(source, node, providerAllowed))
            .Where(node => node is not null).Cast<ConditionSelectionNode>().ToArray();
        if (children.Length == 0 && !MatchesSearch(source.DisplayName)) return null;
        return children.Length == 0 ? null : new ConditionSelectionNode(
            source.DisplayName, children, description: source.ResultType.DisplayName,
            icon: PackIconMaterialKind.FunctionVariant);
    }

    private ConditionSelectionNode? CreateResultNode(
        SourceStepItem source,
        ResultPropertyNode node,
        bool providerAllowed)
    {
        var children = node.Children.Select(child => CreateResultNode(source, child, providerAllowed))
            .Where(child => child is not null).Cast<ConditionSelectionNode>().ToList();
        var compatible = node.Property is not null && providerAllowed && _contract.Accepts(node.Property);
        var visible = compatible || ShowIncompatible;
        if (node.Property is not null && !MatchesSearch(
                source.DisplayName, node.DisplayName, node.Property.Description,
                _formatter.Type(node.Property.DataType, node.Property.Cardinality)))
            visible = false;
        if (!visible && children.Count == 0) return null;
        if (node.Property is not null && visible)
        {
            var reason = compatible ? node.Property.Description : IncompatibleReason(node.Property.DataType);
            var current = new ConditionSelectionNode(
                children.Count > 0 ? Loc.Get("Ui.Step.IfEditor.CompleteValue") : node.DisplayName,
                selectCommand: compatible ? new RelayCommand(() => Select(source, node.Property)) : null,
                secondaryText: _formatter.Type(node.Property.DataType, node.Property.Cardinality),
                description: reason,
                icon: TypeIcon(node.Property.DataType),
                isEnabled: compatible,
                sourceText: source.DisplayName,
                isSelected: IsSelected(source, node.Property));
            if (children.Count == 0) return current;
            children.Insert(0, current);
        }
        return new ConditionSelectionNode(node.DisplayName, children, icon: PackIconMaterialKind.FolderOutline);
    }

    private ConditionSelectionNode CreateProviderNode(ValueProviderSourceDescriptor source, bool compatible)
    {
        var secondary = compatible
            ? ProviderSecondaryText(source)
            : $"{_formatter.Type(source.ValueKind, source.Cardinality)} · {IncompatibleReason(source.ValueKind)}";
        return new ConditionSelectionNode(
            source.Name,
            selectCommand: compatible ? new RelayCommand(() => Select(source)) : null,
            secondaryText: secondary,
            description: ProviderDescription(source),
            icon: source.IsSensitive ? PackIconMaterialKind.LockOutline : TypeIcon(source.ValueKind),
            isEnabled: compatible,
            sourceText: ProviderSourceText(source),
            isSelected: IsSelected(source));
    }

    private static ConditionSelectionNode EmptyNode(string text) => new(
        text, icon: PackIconMaterialKind.InformationOutline, isEnabled: false);

    private string ProviderSecondaryText(ValueProviderSourceDescriptor source)
    {
        if (source.IsSensitive) return Loc.Get("Ui.ValueReference.Sensitive");
        var type = _formatter.Type(source.ValueKind, source.Cardinality);
        return string.Equals(source.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
               && _jobVariables.TryGetValue(source.SourceId, out var variable)
            ? $"{_formatter.CompactValue(variable)} · {type}"
            : type;
    }

    private string ProviderPreviewValue(ValueProviderSourceDescriptor source)
    {
        if (source.IsSensitive) return Loc.Get("Ui.ValueReference.Sensitive");
        return string.Equals(source.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
               && _jobVariables.TryGetValue(source.SourceId, out var variable)
            ? _formatter.CompactValue(variable)
            : source.Description;
    }

    private string ProviderDescription(ValueProviderSourceDescriptor source)
    {
        if (source.IsSensitive) return source.Description;
        if (!string.Equals(source.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
            || !_jobVariables.TryGetValue(source.SourceId, out var variable))
            return source.Description;
        var value = _formatter.FullValue(variable);
        return string.IsNullOrWhiteSpace(source.Description)
            ? value
            : $"{source.Description}{Environment.NewLine}{value}";
    }

    private string ProviderSourceText(ValueProviderSourceDescriptor source)
    {
        if (!string.Equals(source.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal))
            return ProviderLabel(source.ProviderId);
        return _jobVariables.TryGetValue(source.SourceId, out var variable)
               && variable.Scope == JobVariableScope.StepValue
            ? Loc.Get("Ui.Job.Variables.Scope.StepValues")
            : Loc.Get("Ui.Job.Variables.Scope.Shared");
    }

    private bool IsSelected(ValueProviderSourceDescriptor source) =>
        _selectedProviderSource is not null
        && string.Equals(_selectedProviderSource.ProviderId, source.ProviderId, StringComparison.Ordinal)
        && string.Equals(_selectedProviderSource.SourceId, source.SourceId, StringComparison.OrdinalIgnoreCase);

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

    private void Select(SourceStepItem source, ResultPropertyDescriptor property)
    {
        _inlineEditEnabled = false;
        _missingReference = null;
        _selectedProviderSource = null;
        _selectedSource = source;
        _selectedProperty = property;
        NotifySelection();
    }

    private void Select(ValueProviderSourceDescriptor source)
    {
        _inlineEditEnabled = false;
        _missingReference = null;
        _selectedSource = null;
        _selectedProperty = null;
        _selectedProviderSource = source;
        NotifySelection();
    }

    private void Clear()
    {
        _inlineEditEnabled = false;
        _missingReference = null;
        _selectedSource = null;
        _selectedProperty = null;
        _selectedProviderSource = null;
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
        var variable = _context?.CreateStepValue?.Invoke();
        if (variable is null || variable.Id == Guid.Empty) return;
        variable.Scope = JobVariableScope.StepValue;
        var sourceId = variable.Id.ToString("D");
        _jobVariables[sourceId] = variable;
        _providerSources.RemoveAll(source =>
            string.Equals(source.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
            && string.Equals(source.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));
        var descriptor = ValueProviderSourceDescriptor.FromVariable(variable);
        _providerSources.Add(descriptor);
        Select(descriptor);
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
        _contract.AllowsProvider(source.ProviderId)
        && _contract.AcceptedShapes.Any(shape => shape.Accepts(source.ValueKind, source.Cardinality));

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
        ResultValueKind.Image => PackIconMaterialKind.ImageOutline,
        _ => PackIconMaterialKind.Variable
    };

    private static string ProviderLabel(string providerId) => providerId switch
    {
        ValueProviderIds.JobVariable => Loc.Get("Ui.ValueReference.JobVariables"),
        ValueProviderIds.StepResult => Loc.Get("Ui.ValueReference.ResultVariables"),
        ValueProviderIds.Secret => Loc.Get("Ui.ValueReference.Secrets"),
        _ => providerId
    };

    private void OnChange([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Compatibility name for extensions compiled against the former picker API.</summary>
[Obsolete("Use ValueReferencePickerViewModel.")]
public sealed class ResultBindingPickerViewModel : ValueReferencePickerViewModel
{
    public ResultBindingPickerViewModel(
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor contract,
        bool selectDefault = true,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null)
        : base(sources, contract, selectDefault, variables, providerSources) { }
}
