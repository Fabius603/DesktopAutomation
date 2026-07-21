using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels;

public sealed class ResultBindingPickerViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<SourceStepItem> _sources;
    private readonly StepInputDescriptor _contract;
    private SourceStepItem? _selectedSource;
    private ResultPropertyDescriptor? _selectedProperty;

    public ResultBindingPickerViewModel(
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor contract,
        bool selectDefault = true)
    {
        _sources = sources.Where(source => !string.IsNullOrWhiteSpace(source.StepId)).ToArray();
        _contract = contract;
        SelectionTree = BuildTree();
        ClearCommand = new RelayCommand(Clear);
        if (selectDefault)
        {
            var source = _sources.FirstOrDefault(s => s.ResultType.Properties.Any(contract.Accepts));
            var property = source?.ResultType.Properties.FirstOrDefault(contract.Accepts);
            if (source is not null && property is not null) Select(source, property);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<ConditionSelectionNode> SelectionTree { get; }
    public ICommand ClearCommand { get; }
    public bool CanClear => !_contract.Required;
    public bool IsConfigured => _selectedSource is not null && _selectedProperty is not null;
    public string SelectedStepName => _selectedSource?.DisplayName
        ?? Loc.Get("Ui.Job.Steps.NoSourceSelected");
    public string SelectedPropertyName => _selectedProperty?.DisplayName
        ?? Loc.Get("Ui.Step.IfEditor.SelectValue");
    public string SelectedCardinality => _selectedProperty is null ? string.Empty : StepLocalization.ResultValueType(_selectedProperty);
    public string EmptyTitle => Loc.Get("Ui.ResultPicker.EmptyTitle");
    public string EmptyHint => Loc.Get("Ui.ResultPicker.EmptyHint");
    public string SelectedPath => IsConfigured
        ? $"{SelectedStepName} → {SelectedPropertyName}"
        : SelectedPropertyName;

    public string SelectedDisplayPath => IsConfigured
        ? $"{SelectedStepName}  →  {SelectedPropertyName}"
        : SelectedPropertyName;

    public ResultBinding ToBinding() => new()
    {
        SourceStepId = _selectedSource?.StepId ?? string.Empty,
        PropertyPath = _selectedProperty?.Name ?? string.Empty
    };

    public void Load(ResultBinding? binding)
    {
        var source = _sources.FirstOrDefault(item => item.StepId == binding?.SourceStepId);
        var property = source?.ResultType.Properties.FirstOrDefault(item =>
            item.Name.Equals(binding?.PropertyPath, StringComparison.OrdinalIgnoreCase) && _contract.Accepts(item));
        if (source is not null && property is not null) Select(source, property);
        else if (!_contract.Required) Clear();
    }

    private IReadOnlyList<ConditionSelectionNode> BuildTree() => _sources
        .Select(CreateSourceNode)
        .Where(node => node is not null).Cast<ConditionSelectionNode>().ToArray();

    private ConditionSelectionNode? CreateSourceNode(SourceStepItem source)
    {
        var acceptedProperties = source.ResultType.Properties.Where(_contract.Accepts).ToArray();
        if (acceptedProperties.Length == 0) return null;
        return new ConditionSelectionNode(source.DisplayName,
            source.ResultType.PropertyTree.Select(node => CreateNode(source, node))
                .Where(node => node is not null).Cast<ConditionSelectionNode>().ToArray());
    }

    private ConditionSelectionNode? CreateNode(SourceStepItem source, ResultPropertyNode node)
    {
        var children = node.Children.Select(child => CreateNode(source, child))
            .Where(child => child is not null).Cast<ConditionSelectionNode>().ToArray();
        var selectable = node.Property is not null && _contract.Accepts(node.Property);
        if (!selectable && children.Length == 0) return null;
        if (selectable && children.Length > 0)
        {
            var selectCurrent = new ConditionSelectionNode(
                Loc.Get("Ui.Step.IfEditor.CompleteValue"),
                selectCommand: new RelayCommand(() => Select(source, node.Property!)),
                secondaryText: StepLocalization.ResultValueType(node.Property!));
            return new ConditionSelectionNode(node.DisplayName, new[] { selectCurrent }.Concat(children).ToArray());
        }
        return new ConditionSelectionNode(
            node.DisplayName,
            children,
            selectable ? new RelayCommand(() => Select(source, node.Property!)) : null,
            selectable ? StepLocalization.ResultValueType(node.Property!) : null);
    }

    private void Select(SourceStepItem source, ResultPropertyDescriptor property)
    {
        _selectedSource = source;
        _selectedProperty = property;
        Notify();
    }

    private void Clear()
    {
        _selectedSource = null;
        _selectedProperty = null;
        Notify();
    }

    private void Notify()
    {
        OnChange(nameof(SelectedPath));
        OnChange(nameof(SelectedStepName));
        OnChange(nameof(SelectedDisplayPath));
        OnChange(nameof(SelectedPropertyName));
        OnChange(nameof(SelectedCardinality));
        OnChange(nameof(IsConfigured));
    }

    private void OnChange([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
