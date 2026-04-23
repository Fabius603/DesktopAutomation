using System;
using System.Windows.Input;

namespace DesktopAutomationApp.ViewModels
{
    // Parameterlos
    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();

        // Eigenes Event statt CommandManager.RequerySuggested:
        // CommandManager.RequerySuggested feuert bei JEDER Mausbewegung und
        // würde alle CanExecute-Prüfungen im gesamten Fenster auslösen → O(n²) bei vielen Steps.
        private EventHandler? _canExecuteChanged;
        public event EventHandler? CanExecuteChanged
        {
            add    => _canExecuteChanged += value;
            remove => _canExecuteChanged -= value;
        }

        public void RaiseCanExecuteChanged()
            => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    // Mit Parameter
    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            // Sichere Konvertierung: null oder falscher Typ -> default
            var value = ConvertParameter(parameter);
            return _canExecute?.Invoke(value) ?? true;
        }

        public void Execute(object? parameter)
        {
            var value = ConvertParameter(parameter);
            _execute(value);
        }

        private static T? ConvertParameter(object? parameter)
        {
            if (parameter is null) return default;
            if (parameter is T t) return t;

            // Versuch: ChangeType für einfache Typen (optional)
            try
            {
                return (T?)System.Convert.ChangeType(parameter, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        private EventHandler? _canExecuteChanged;
        public event EventHandler? CanExecuteChanged
        {
            add    => _canExecuteChanged += value;
            remove => _canExecuteChanged -= value;
        }

        public void RaiseCanExecuteChanged()
            => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
