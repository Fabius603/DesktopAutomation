using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaskAutomation.Jobs;

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

        private PointAxisOperator _operator = PointAxisOperator.LessThan;
        public PointAxisOperator Operator
        {
            get => _operator;
            set { _operator = value; OnChange(); }
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
            Axis     = _axis,
            Operator = _operator,
            Value    = _value
        };

        public void LoadFrom(AxisExpression e)
        {
            Axis     = e.Axis;
            Operator = e.Operator;
            Value    = e.Value;
        }
    }
}
