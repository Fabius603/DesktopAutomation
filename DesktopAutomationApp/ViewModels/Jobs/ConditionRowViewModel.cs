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
public sealed record EnumConditionOption(string Value, string DisplayName);

public sealed class ConditionSelectionNode
{
    public ConditionSelectionNode(string displayName, IReadOnlyList<ConditionSelectionNode>? children = null,
        ICommand? selectCommand = null, string? secondaryText = null)
    {
        DisplayName = displayName;
        Children = children ?? [];
        SelectCommand = selectCommand;
        SecondaryText = secondaryText;
    }

    public string DisplayName { get; }
    public IReadOnlyList<ConditionSelectionNode> Children { get; }
    public ICommand? SelectCommand { get; }
    public string? SecondaryText { get; }
    public bool IsSelectable => SelectCommand is not null;
}

public sealed class ConditionRowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChange([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new(p));
    private void NotifyInput()
    {
        OnChange(nameof(ShowComparisonValue));
        OnChange(nameof(ShowLiteralComparisonValue));
        OnChange(nameof(ShowJobResultComparisonValue));
        OnChange(nameof(ShowNumericValue));
        OnChange(nameof(ShowTextValue));
        OnChange(nameof(ShowDateValue));
        OnChange(nameof(ShowBooleanValue));
        OnChange(nameof(ShowEnumValue));
        OnChange(nameof(EnumValues));
        OnChange(nameof(EnumOptions));
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
    public IReadOnlyList<ConditionSelectionNode> ComparisonSelectionTree { get; private set; } = [];
    public ObservableCollection<ConditionOperator> AvailableOperators { get; } = [];

    private SourceStepItem? _selectedSourceStep;
    public SourceStepItem? SelectedSourceStep { get => _selectedSourceStep; private set { _selectedSourceStep = value; OnChange(); OnChange(nameof(SelectedPath)); } }
    private ResultPropertyDescriptor? _selectedProperty;
    public ResultPropertyDescriptor? SelectedProperty
    {
        get => _selectedProperty;
        private set
        {
            _selectedProperty = value;
            OnChange();
            RefreshOperators();
            RefreshComparisonChoices();
            OnChange(nameof(SelectedPath));
        }
    }
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
    private bool? _comparisonBoolean;
    public bool? ComparisonBoolean { get => _comparisonBoolean; set { _comparisonBoolean = value; OnChange(); NotifyValidation(); } }
    public IReadOnlyList<bool> BooleanValues { get; } = [true, false];
    private string? _comparisonEnum;
    public string? ComparisonEnum { get => _comparisonEnum; set { _comparisonEnum = value; OnChange(); NotifyValidation(); } }
    public IReadOnlyList<string> EnumValues => SelectedProperty?.EnumValues ?? [];
    public IReadOnlyList<EnumConditionOption> EnumOptions => (SelectedProperty?.EnumValues ?? [])
        .Select(value =>
        {
            var typeName = SelectedProperty?.EnumTypeName?.Split('.').LastOrDefault() ?? "Enum";
            var key = $"Enum.{typeName}.{value}";
            var localized = Loc.Get(key);
            return new EnumConditionOption(value, localized == $"[{key}]" ? value : localized);
        }).ToArray();

    private ComparisonOperandKind _comparisonKind = ComparisonOperandKind.Literal;
    public ComparisonOperandKind ComparisonKind
    {
        get => _comparisonKind;
        set
        {
            if (_comparisonKind == value) return;
            _comparisonKind = value;
            OnChange(); OnChange(nameof(ComparisonIsLiteral)); OnChange(nameof(ComparisonIsJobResult));
            NotifyInput();
        }
    }
    public bool ComparisonIsLiteral
    {
        get => ComparisonKind == ComparisonOperandKind.Literal;
        set { if (value) ComparisonKind = ComparisonOperandKind.Literal; }
    }
    public bool ComparisonIsJobResult
    {
        get => ComparisonKind == ComparisonOperandKind.JobResult;
        set { if (value) ComparisonKind = ComparisonOperandKind.JobResult; }
    }

    private SourceStepItem? _selectedComparisonSourceStep;
    public SourceStepItem? SelectedComparisonSourceStep
    {
        get => _selectedComparisonSourceStep;
        private set { _selectedComparisonSourceStep = value; OnChange(); OnChange(nameof(ComparisonPath)); NotifyValidation(); }
    }
    private ResultPropertyDescriptor? _selectedComparisonProperty;
    public ResultPropertyDescriptor? SelectedComparisonProperty
    {
        get => _selectedComparisonProperty;
        private set { _selectedComparisonProperty = value; OnChange(); OnChange(nameof(ComparisonPath)); NotifyValidation(); }
    }

    public bool ShowComparisonValue => ConditionRules.RequiresComparisonValue(SelectedOperator);
    public bool ShowLiteralComparisonValue => ShowComparisonValue && ComparisonIsLiteral;
    public bool ShowJobResultComparisonValue => ShowComparisonValue && ComparisonIsJobResult;
    public bool ShowNumericValue => ShowLiteralComparisonValue && SelectedProperty?.DataType is ResultValueKind.Number or ResultValueKind.Integer;
    public bool ShowTextValue => ShowLiteralComparisonValue && SelectedProperty?.DataType == ResultValueKind.Text;
    public bool ShowDateValue => ShowLiteralComparisonValue && SelectedProperty?.DataType == ResultValueKind.DateTime;
    public bool ShowBooleanValue => ShowLiteralComparisonValue && SelectedProperty?.DataType == ResultValueKind.Boolean;
    public bool ShowEnumValue => ShowLiteralComparisonValue && SelectedProperty?.DataType == ResultValueKind.Enum;
    public bool IsIntegerValue => SelectedProperty?.DataType == ResultValueKind.Integer;
    public bool CanRemove => _owner.Count > 1;
    public string SelectedPath => SelectedSourceStep is null || SelectedProperty is null
        ? Loc.Get("Ui.Step.IfEditor.SelectValue")
        : $"{SelectedSourceStep.DisplayName}  →  {SelectedProperty.DisplayName}";
    public string ComparisonPath => SelectedComparisonSourceStep is null || SelectedComparisonProperty is null
        ? Loc.Get("Ui.Step.IfEditor.SelectValue")
        : $"{SelectedComparisonSourceStep.DisplayName}  →  {SelectedComparisonProperty.DisplayName}";
    public string InputHint => SelectedProperty?.Description ?? "";
    public string InputExample => SelectedProperty?.Example ?? (SelectedProperty?.DataType switch
    {
        ResultValueKind.Number => Loc.Get("Ui.Step.IfEditor.ExampleDecimal"),
        ResultValueKind.Integer => Loc.Get("Ui.Step.IfEditor.ExampleInteger"),
        ResultValueKind.Text => Loc.Get("Ui.Step.IfEditor.ExampleText"),
        ResultValueKind.DateTime => Loc.Get("Ui.Step.IfEditor.ExampleDate"),
        _ => string.Empty
    });
    public bool IsComparisonValueValid => SelectedProperty is not null &&
        (!ShowComparisonValue || (ComparisonIsLiteral
            ? ConditionRules.IsComparisonValueValid(SelectedProperty, SelectedOperator, GetLiteralComparisonValue())
            : SelectedComparisonSourceStep is not null && SelectedComparisonProperty is not null
                && StepResultMetadata.AreComparable(SelectedProperty, SelectedComparisonProperty)));
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

    private IReadOnlyList<ConditionSelectionNode> BuildSelectionTree(
        IReadOnlyList<SourceStepItem> sources,
        Func<ResultPropertyDescriptor, bool>? filter = null,
        Action<SourceStepItem, ResultPropertyDescriptor>? selection = null)
        => sources.Select(source => new ConditionSelectionNode(
            source.DisplayName,
            source.ResultType.PropertyTree
                .Select(node => CreateSelectionNode(source, node, filter, selection))
                .Where(node => node is not null)
                .Cast<ConditionSelectionNode>()
                .ToArray()))
            .Where(node => node.Children.Count > 0)
            .ToArray();

    private ConditionSelectionNode? CreateSelectionNode(
        SourceStepItem source,
        ResultPropertyNode node,
        Func<ResultPropertyDescriptor, bool>? filter,
        Action<SourceStepItem, ResultPropertyDescriptor>? selection)
    {
        var children = node.Children
            .Select(child => CreateSelectionNode(source, child, filter, selection))
            .Where(child => child is not null)
            .Cast<ConditionSelectionNode>()
            .ToArray();
        if (node.Property is null)
            return children.Length == 0 ? null : new ConditionSelectionNode(node.DisplayName, children);
        if (filter is not null && !filter(node.Property)) return null;
        var select = selection ?? SelectPath;
        if (children.Length > 0)
        {
            var completeValue = new ConditionSelectionNode(
                Loc.Get("Ui.Step.IfEditor.CompleteValue"),
                selectCommand: new RelayCommand(() => select(source, node.Property)),
                secondaryText: StepLocalization.ResultValueType(node.Property));
            return new ConditionSelectionNode(node.DisplayName, new[] { completeValue }.Concat(children).ToArray());
        }
        return new ConditionSelectionNode(
            node.DisplayName,
            children,
            new RelayCommand(() => select(source, node.Property)),
            StepLocalization.ResultValueType(node.Property));
    }

    private void SelectPath(SourceStepItem source, ResultPropertyDescriptor property)
    {
        SelectedSourceStep = source;
        SelectedProperty = property;
    }

    private void SelectComparisonPath(SourceStepItem source, ResultPropertyDescriptor property)
    {
        SelectedComparisonSourceStep = source;
        SelectedComparisonProperty = property;
    }

    private void RefreshComparisonChoices()
    {
        ComparisonSelectionTree = SelectedProperty is null
            ? []
            : BuildSelectionTree(_availableSourceSteps,
                property => StepResultMetadata.AreComparable(SelectedProperty, property),
                SelectComparisonPath);
        OnChange(nameof(ComparisonSelectionTree));

        if (SelectedProperty is null || SelectedComparisonProperty is not null
            && !StepResultMetadata.AreComparable(SelectedProperty, SelectedComparisonProperty))
        {
            SelectedComparisonSourceStep = null;
            SelectedComparisonProperty = null;
        }
        NotifyValidation();
    }

    public StepCondition ToCondition()
    {
        return new StepCondition
        {
            SourceStepId = SelectedSourceStep?.StepId ?? "",
            PropertyId = SelectedProperty?.StableId,
            PropertyPath = SelectedProperty?.Name ?? "",
            Operator = SelectedOperator,
            Comparison = ShowComparisonValue ? new ComparisonOperand
            {
                Kind = ComparisonKind,
                Value = ComparisonIsLiteral ? GetLiteralComparisonValue() : null,
                SourceStepId = ComparisonIsJobResult ? SelectedComparisonSourceStep?.StepId : null,
                PropertyId = ComparisonIsJobResult ? SelectedComparisonProperty?.StableId : null,
                PropertyPath = ComparisonIsJobResult ? SelectedComparisonProperty?.Name : null
            } : null
        };
    }

    private string? GetLiteralComparisonValue()
    {
        if (SelectedProperty is null || !ShowComparisonValue || !ComparisonIsLiteral) return null;
        object? editorValue = SelectedProperty.DataType switch
        {
            ResultValueKind.Number or ResultValueKind.Integer => ComparisonNumber,
            ResultValueKind.DateTime => ComparisonDate,
            ResultValueKind.Boolean => ComparisonBoolean,
            ResultValueKind.Text => ComparisonValue,
            ResultValueKind.Enum => ComparisonEnum,
            _ => null
        };
        return ConditionRules.FormatComparisonValue(SelectedProperty, editorValue);
    }

    public void LoadFrom(StepCondition condition)
    {
        _selectedSourceStep = _availableSourceSteps.FirstOrDefault(s => s.StepId == condition.SourceStepId);
        _selectedProperty = _selectedSourceStep?.ResultType.Properties.FirstOrDefault(p =>
            (!string.IsNullOrWhiteSpace(condition.PropertyId)
             && p.StableId.Equals(condition.PropertyId, StringComparison.OrdinalIgnoreCase))
            || p.Name.Equals(condition.PropertyPath, StringComparison.OrdinalIgnoreCase));
        RefreshOperators();
        var comparison = condition.EffectiveComparison;
        var editorOperator = condition.Operator;
        switch (condition.Operator)
        {
            case ConditionOperator.IsTrue:
                editorOperator = ConditionOperator.Equals;
                comparison = new ComparisonOperand { Kind = ComparisonOperandKind.Literal, Value = bool.TrueString };
                break;
            case ConditionOperator.IsFalse:
                editorOperator = ConditionOperator.Equals;
                comparison = new ComparisonOperand { Kind = ComparisonOperandKind.Literal, Value = bool.FalseString };
                break;
            case ConditionOperator.IsEmpty:
                editorOperator = ConditionOperator.Equals;
                comparison = new ComparisonOperand { Kind = ComparisonOperandKind.Literal, Value = string.Empty };
                break;
            case ConditionOperator.IsNotEmpty:
                editorOperator = ConditionOperator.NotEquals;
                comparison = new ComparisonOperand { Kind = ComparisonOperandKind.Literal, Value = string.Empty };
                break;
        }
        if (AvailableOperators.Contains(editorOperator))
            _selectedOperator = editorOperator;
        _comparisonKind = comparison.Kind;
        _comparisonValue = comparison.Value ?? "";
        if (_selectedProperty is not null && StepResultMetadata.TryParseComparison(_selectedProperty, comparison.Value, out var parsed))
        {
            if (_selectedProperty.DataType is ResultValueKind.Number or ResultValueKind.Integer)
                _comparisonNumber = Convert.ToDouble(parsed);
            if (_selectedProperty.DataType == ResultValueKind.DateTime && parsed is DateTime date)
                _comparisonDate = date.ToLocalTime();
            if (_selectedProperty.DataType == ResultValueKind.Boolean && parsed is bool boolean)
                _comparisonBoolean = boolean;
            if (_selectedProperty.DataType == ResultValueKind.Enum)
                _comparisonEnum = parsed?.ToString();
        }
        RefreshComparisonChoices();
        if (comparison.Kind == ComparisonOperandKind.JobResult)
        {
            _selectedComparisonSourceStep = _availableSourceSteps.FirstOrDefault(s => s.StepId == comparison.SourceStepId);
            _selectedComparisonProperty = _selectedComparisonSourceStep?.ResultType.Properties
                .FirstOrDefault(p =>
                    ((!string.IsNullOrWhiteSpace(comparison.PropertyId)
                      && p.StableId.Equals(comparison.PropertyId, StringComparison.OrdinalIgnoreCase))
                     || p.Name.Equals(comparison.PropertyPath, StringComparison.OrdinalIgnoreCase))
                    && _selectedProperty is not null
                    && StepResultMetadata.AreComparable(_selectedProperty, p));
        }
        OnChange(nameof(SelectedSourceStep)); OnChange(nameof(SelectedProperty)); OnChange(nameof(SelectedOperator));
        OnChange(nameof(ComparisonValue)); OnChange(nameof(ComparisonNumber)); OnChange(nameof(ComparisonDate)); OnChange(nameof(ComparisonBoolean)); OnChange(nameof(ComparisonEnum)); OnChange(nameof(EnumValues)); OnChange(nameof(EnumOptions)); OnChange(nameof(SelectedPath));
        OnChange(nameof(ComparisonKind)); OnChange(nameof(ComparisonIsLiteral)); OnChange(nameof(ComparisonIsJobResult));
        OnChange(nameof(SelectedComparisonSourceStep)); OnChange(nameof(SelectedComparisonProperty)); OnChange(nameof(ComparisonPath)); NotifyInput();
    }

    private void RefreshOperators()
    {
        AvailableOperators.Clear();
        if (_selectedProperty is not null)
            foreach (var op in ConditionRules.GetOperators(_selectedProperty.DataType)) AvailableOperators.Add(op);
        SelectedOperator = AvailableOperators.FirstOrDefault(); NotifyInput();
    }

}
