using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels;

public sealed record SourceStepItem(string StepId, string DisplayName, ResultTypeDescriptor ResultType);

public sealed class ConditionSelectionNode
{
    public ConditionSelectionNode(string displayName, IReadOnlyList<ConditionSelectionNode>? children = null, ICommand? selectCommand = null)
    {
        DisplayName = displayName;
        Children = children ?? [];
        SelectCommand = selectCommand;
    }

    public string DisplayName { get; }
    public IReadOnlyList<ConditionSelectionNode> Children { get; }
    public ICommand? SelectCommand { get; }
}

public sealed class ConditionRowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChange([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new(p));
    private void NotifyInput()
    {
        OnChange(nameof(ShowComparisonValue));
        OnChange(nameof(ShowNumericValue));
        OnChange(nameof(ShowTextValue));
        OnChange(nameof(ShowDateValue));
        OnChange(nameof(InputHint));
        OnChange(nameof(InputExample));
        NotifyValidation();
    }
    private void NotifyValidation()
    {
        OnChange(nameof(IsComparisonValueValid));
        OnChange(nameof(ComparisonValueValidationError));
        OnChange(nameof(IsValid));
    }

    public ICommand RemoveCommand { get; }
    public IReadOnlyList<ConditionSelectionNode> SelectionTree { get; }
    public ObservableCollection<ConditionOperator> AvailableOperators { get; } = [];

    private SourceStepItem? _selectedSourceStep;
    public SourceStepItem? SelectedSourceStep { get => _selectedSourceStep; private set { _selectedSourceStep = value; OnChange(); OnChange(nameof(SelectedPath)); } }
    private ResultPropertyDescriptor? _selectedProperty;
    public ResultPropertyDescriptor? SelectedProperty { get => _selectedProperty; private set { _selectedProperty = value; OnChange(); RefreshOperators(); OnChange(nameof(SelectedPath)); } }
    private ConditionOperator _selectedOperator;
    public ConditionOperator SelectedOperator { get => _selectedOperator; set { _selectedOperator = value; OnChange(); NotifyInput(); } }

    private string _comparisonValue = "";
    public string ComparisonValue { get => _comparisonValue; set { _comparisonValue = value; OnChange(); NotifyValidation(); } }
    private double? _comparisonNumber;
    public double? ComparisonNumber
    {
        get => _comparisonNumber;
        set { _comparisonNumber = IsIntegerValue && value.HasValue ? Math.Round(value.Value) : value; OnChange(); NotifyValidation(); }
    }
    private DateTime? _comparisonDate;
    public DateTime? ComparisonDate { get => _comparisonDate; set { _comparisonDate = value; OnChange(); NotifyValidation(); } }

    public bool ShowComparisonValue => ConditionRules.RequiresComparisonValue(SelectedOperator);
    public bool ShowNumericValue => ShowComparisonValue && SelectedProperty?.PropertyType is ResultPropertyType.Double or ResultPropertyType.Integer;
    public bool ShowTextValue => ShowComparisonValue && SelectedProperty?.PropertyType == ResultPropertyType.String;
    public bool ShowDateValue => ShowComparisonValue && SelectedProperty?.PropertyType == ResultPropertyType.DateTime;
    public bool IsIntegerValue => SelectedProperty?.PropertyType == ResultPropertyType.Integer;
    public bool CanRemove => _owner.Count > 1;
    public string SelectedPath => SelectedSourceStep is null || SelectedProperty is null
        ? Loc.Get("Ui.Step.IfEditor.SelectValue")
        : $"{SelectedSourceStep.DisplayName}.{SelectedProperty.Name}";
    public string InputHint => SelectedProperty?.Description ?? "";
    public string InputExample => SelectedProperty?.Example ?? (SelectedProperty?.PropertyType switch
    {
        ResultPropertyType.Double => Loc.Get("Ui.Step.IfEditor.ExampleDecimal"),
        ResultPropertyType.Integer => Loc.Get("Ui.Step.IfEditor.ExampleInteger"),
        ResultPropertyType.String => Loc.Get("Ui.Step.IfEditor.ExampleText"),
        ResultPropertyType.DateTime => Loc.Get("Ui.Step.IfEditor.ExampleDate"),
        _ => string.Empty
    });
    public bool IsComparisonValueValid => SelectedProperty is not null &&
        ConditionRules.IsComparisonValueValid(SelectedProperty, SelectedOperator, GetComparisonValue());
    public string ComparisonValueValidationError => IsComparisonValueValid
        ? string.Empty
        : Loc.Get("Ui.Step.IfEditor.InvalidValue");
    public bool IsValid => SelectedSourceStep is not null && SelectedProperty is not null &&
        IsComparisonValueValid;

    private readonly ObservableCollection<ConditionRowViewModel> _owner;
    private readonly IReadOnlyList<SourceStepItem> _availableSourceSteps;

    public ConditionRowViewModel(ObservableCollection<ConditionRowViewModel> owner, IReadOnlyList<SourceStepItem> sources)
    {
        _owner = owner;
        RemoveCommand = new RelayCommand(
            () => { if (owner.Count > 1) owner.Remove(this); },
            () => owner.Count > 1);
        _availableSourceSteps = sources;
        SelectionTree = BuildSelectionTree(sources);
        owner.CollectionChanged += OnOwnerCollectionChanged;
        var firstSource = sources.FirstOrDefault();
        var firstProperty = firstSource?.ResultType.Properties.FirstOrDefault();
        if (firstSource is not null && firstProperty is not null)
            SelectPath(firstSource, firstProperty);
    }

    private void OnOwnerCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnChange(nameof(CanRemove));
        (RemoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        if (e.OldItems?.Contains(this) == true || e.Action == NotifyCollectionChangedAction.Reset && !_owner.Contains(this))
            _owner.CollectionChanged -= OnOwnerCollectionChanged;
    }

    private IReadOnlyList<ConditionSelectionNode> BuildSelectionTree(IReadOnlyList<SourceStepItem> sources)
        => sources.Select(source => new ConditionSelectionNode(
            source.DisplayName,
            source.ResultType.PropertyTree.Select(node => CreateSelectionNode(source, node)).ToArray())).ToArray();

    private ConditionSelectionNode CreateSelectionNode(SourceStepItem source, ResultPropertyNode node) =>
        new(
            node.Property is null ? node.DisplayName : $"{node.DisplayName} ({node.Property.PropertyType})",
            node.Children.Select(child => CreateSelectionNode(source, child)).ToArray(),
            node.Property is null ? null : new RelayCommand(() => SelectPath(source, node.Property)));

    private void SelectPath(SourceStepItem source, ResultPropertyDescriptor property)
    {
        SelectedSourceStep = source;
        SelectedProperty = property;
    }

    public StepCondition ToCondition()
    {
        return new StepCondition
        {
            SourceStepId = SelectedSourceStep?.StepId ?? "",
            PropertyPath = SelectedProperty?.Name ?? "",
            Operator = SelectedOperator,
            ComparisonValue = GetComparisonValue()
        };
    }

    private string? GetComparisonValue()
    {
        if (SelectedProperty is null || !ShowComparisonValue) return null;
        object? editorValue = SelectedProperty.PropertyType switch
        {
            ResultPropertyType.Double or ResultPropertyType.Integer => ComparisonNumber,
            ResultPropertyType.DateTime => ComparisonDate,
            ResultPropertyType.String => ComparisonValue,
            _ => null
        };
        return ConditionRules.FormatComparisonValue(SelectedProperty, editorValue);
    }

    public void LoadFrom(StepCondition condition)
    {
        _selectedSourceStep = _availableSourceSteps.FirstOrDefault(s => s.StepId == condition.SourceStepId);
        _selectedProperty = _selectedSourceStep?.ResultType.Properties.FirstOrDefault(p => p.Name == condition.PropertyPath);
        RefreshOperators();
        if (_selectedProperty is not null && ConditionRules.IsOperatorAllowed(_selectedProperty.PropertyType, condition.Operator))
            _selectedOperator = condition.Operator;
        _comparisonValue = condition.ComparisonValue ?? "";
        if (_selectedProperty is not null && StepResultMetadata.TryParseComparison(_selectedProperty, condition.ComparisonValue, out var parsed))
        {
            if (_selectedProperty.PropertyType is ResultPropertyType.Double or ResultPropertyType.Integer)
                _comparisonNumber = Convert.ToDouble(parsed);
            if (_selectedProperty.PropertyType == ResultPropertyType.DateTime && parsed is DateTime date)
                _comparisonDate = date.ToLocalTime();
        }
        OnChange(nameof(SelectedSourceStep)); OnChange(nameof(SelectedProperty)); OnChange(nameof(SelectedOperator));
        OnChange(nameof(ComparisonValue)); OnChange(nameof(ComparisonNumber)); OnChange(nameof(ComparisonDate)); OnChange(nameof(SelectedPath)); NotifyInput();
    }

    private void RefreshOperators()
    {
        AvailableOperators.Clear();
        if (_selectedProperty is not null)
            foreach (var op in ConditionRules.GetOperators(_selectedProperty.PropertyType)) AvailableOperators.Add(op);
        SelectedOperator = AvailableOperators.FirstOrDefault(); NotifyInput();
    }
}
