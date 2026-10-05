using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.ViewModels
{
    /// <summary>
    /// ViewModel für eine einzelne Achsen-Ausdruck-Zeile im PointComparisonStep-Dialog.
    /// </summary>
    public sealed class AxisExpressionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChange([CallerMemberName] string? p = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public string[] AvailableAxes { get; } = { "X", "Y" };

        public PointAxisOperator[] AvailableOperators { get; } =
        {
            PointAxisOperator.LessThan,
            PointAxisOperator.LessThanOrEqual,
            PointAxisOperator.GreaterThan,
            PointAxisOperator.GreaterThanOrEqual,
            PointAxisOperator.Equal,
            PointAxisOperator.NotEqual
        };

        private string _axis = "X";
        public string Axis
        {
            get => _axis;
            set { _axis = value; OnChange(); }
        }

        private string? _unknownOperator;
        private PointAxisOperator? _operator = PointAxisOperator.LessThan;
        public PointAxisOperator? Operator
        {
            get => _operator;
            set
            {
                if (value is null) return;
                _operator = value; _unknownOperator = null;
                OnChange(); OnChange(nameof(HasInvalidToken)); OnChange(nameof(InvalidTokenMessage));
            }
        }

        public string OperatorToken => _unknownOperator ?? _operator?.ToString() ?? string.Empty;
        public bool HasInvalidToken => _unknownOperator is not null;
        public string InvalidTokenMessage => HasInvalidToken
            ? Loc.Format("Ui.Step.Generated.Validation.UnknownSavedToken", _unknownOperator) : string.Empty;
        public void LoadOperatorToken(string token)
        {
            if (StepEnumRules.TryRead<PointAxisOperator>(token, out var value)) Operator = value;
            else { _operator = null; _unknownOperator = token; OnChange(nameof(Operator)); }
            OnChange(nameof(HasInvalidToken)); OnChange(nameof(InvalidTokenMessage));
        }

        private int _value;
        public int Value
        {
            get => _value;
            set { _value = value; OnChange(); }
        }

        public ICommand RemoveCommand { get; }

        public AxisExpressionViewModel(ObservableCollection<AxisExpressionViewModel> owner)
        {
            RemoveCommand = new RelayCommand(() => owner.Remove(this));
        }

        public AxisExpression ToAxisExpression() => new AxisExpression
        {
            Axis = _axis,
            Operator = _operator ?? throw new InvalidOperationException(InvalidTokenMessage),
            Value = _value
        };

        public void LoadFrom(AxisExpression e)
        {
            Axis = e.Axis;
            Operator = e.Operator;
            Value = e.Value;
        }
    }
}
