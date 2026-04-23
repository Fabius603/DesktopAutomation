using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels
{
    /// <summary>
    /// Represents a preceding step that produces a result usable in a condition.
    /// </summary>
    public sealed record SourceStepItem(string StepId, string DisplayName, ResultTypeDescriptor ResultType);

    public sealed class ConditionRowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChange([CallerMemberName] string? p = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public ICommand RemoveCommand { get; }

        // Source step selection (which preceding step's result to evaluate)
        public IReadOnlyList<SourceStepItem> AvailableSourceSteps { get; }

        private SourceStepItem? _selectedSourceStep;
        public SourceStepItem? SelectedSourceStep
        {
            get => _selectedSourceStep;
            set { _selectedSourceStep = value; OnChange(); RefreshProperties(); }
        }

        // Property selection
        public ObservableCollection<ResultPropertyDescriptor> AvailableProperties { get; } = new();

        private ResultPropertyDescriptor? _selectedProperty;
        public ResultPropertyDescriptor? SelectedProperty
        {
            get => _selectedProperty;
            set { _selectedProperty = value; OnChange(); RefreshOperators(); }
        }

        // Operator selection
        public ObservableCollection<ConditionOperator> AvailableOperators { get; } = new();

        private ConditionOperator _selectedOperator = ConditionOperator.IsTrue;
        public ConditionOperator SelectedOperator
        {
            get => _selectedOperator;
            set { _selectedOperator = value; OnChange(); OnChange(nameof(ShowComparisonValue)); }
        }

        // Comparison value
        public bool ShowComparisonValue =>
            _selectedOperator is not (ConditionOperator.IsTrue or ConditionOperator.IsFalse);

        private string _comparisonValue = "";
        public string ComparisonValue
        {
            get => _comparisonValue;
            set { _comparisonValue = value; OnChange(); }
        }

        // Constructor
        public ConditionRowViewModel(
            ObservableCollection<ConditionRowViewModel> ownerCollection,
            IReadOnlyList<SourceStepItem> sourceSteps)
        {
            RemoveCommand      = new RelayCommand(() => ownerCollection.Remove(this));
            AvailableSourceSteps = sourceSteps;
            SelectedSourceStep   = AvailableSourceSteps.FirstOrDefault();
        }

        // Serialization
        public StepCondition ToCondition() => new()
        {
            SourceStepId          = _selectedSourceStep?.StepId ?? "",
            SourceStepDisplayName = _selectedSourceStep?.DisplayName ?? "",
            Property              = _selectedProperty?.Name ?? "",
            PropertyDisplayName   = _selectedProperty?.DisplayName ?? "",
            Operator              = _selectedOperator,
            ComparisonValue       = ShowComparisonValue ? _comparisonValue : null
        };

        public void LoadFrom(StepCondition condition)
        {
            _selectedSourceStep = AvailableSourceSteps.FirstOrDefault(s => s.StepId == condition.SourceStepId);
            OnChange(nameof(SelectedSourceStep));
            RefreshProperties();

            _selectedProperty = AvailableProperties.FirstOrDefault(p => p.Name == condition.Property);
            OnChange(nameof(SelectedProperty));
            RefreshOperators();

            if (AvailableOperators.Contains(condition.Operator))
                _selectedOperator = condition.Operator;
            OnChange(nameof(SelectedOperator));
            OnChange(nameof(ShowComparisonValue));

            _comparisonValue = condition.ComparisonValue ?? "";
            OnChange(nameof(ComparisonValue));
        }

        // Private helpers
        private void RefreshProperties()
        {
            AvailableProperties.Clear();
            _selectedProperty = null;
            OnChange(nameof(SelectedProperty));

            if (_selectedSourceStep is null) return;

            foreach (var p in _selectedSourceStep.ResultType.Properties)
                AvailableProperties.Add(p);

            SelectedProperty = AvailableProperties.FirstOrDefault();
        }

        private void RefreshOperators()
        {
            AvailableOperators.Clear();

            if (_selectedProperty is null) return;

            var ops = _selectedProperty.PropertyType == ResultPropertyType.Bool
                ? new[] { ConditionOperator.IsTrue, ConditionOperator.IsFalse }
                : new[] { ConditionOperator.Equals, ConditionOperator.NotEquals,
                          ConditionOperator.GreaterThan, ConditionOperator.LessThan,
                          ConditionOperator.GreaterThanOrEqual, ConditionOperator.LessThanOrEqual };

            foreach (var op in ops)
                AvailableOperators.Add(op);

            SelectedOperator = AvailableOperators.FirstOrDefault();
        }
    }
}