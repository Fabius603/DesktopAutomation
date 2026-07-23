using System.Windows.Input;

namespace DesktopAutomationApp.ViewModels;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool IsExecuting => _isExecuting;
    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    private EventHandler? _canExecuteChanged;
    public event EventHandler? CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }

    public void RaiseCanExecuteChanged()
        => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool IsExecuting => _isExecuting;

    public bool CanExecute(object? parameter)
    {
        var value = ConvertParameter(parameter);
        return !_isExecuting && (_canExecute?.Invoke(value) ?? true);
    }

    public async void Execute(object? parameter)
    {
        var value = ConvertParameter(parameter);
        if (!CanExecute(parameter)) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute(value);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    private static T? ConvertParameter(object? parameter)
    {
        if (parameter is null) return default;
        if (parameter is T value) return value;
        try { return (T?)Convert.ChangeType(parameter, typeof(T)); }
        catch { return default; }
    }

    private EventHandler? _canExecuteChanged;
    public event EventHandler? CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }

    public void RaiseCanExecuteChanged()
        => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
}
