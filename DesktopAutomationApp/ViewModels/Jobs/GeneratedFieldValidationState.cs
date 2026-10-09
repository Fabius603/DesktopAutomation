using System.ComponentModel;

namespace DesktopAutomationApp.ViewModels;

/// <summary>Transient projection of editor diagnostics; changing it never changes an editor value.</summary>
public sealed class GeneratedFieldValidationState : INotifyPropertyChanged
{
    public string? Message { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(Message);
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetMessage(string? message)
    {
        if (Message == message) return;
        Message = message;
        PropertyChanged?.Invoke(this, new(nameof(Message)));
        PropertyChanged?.Invoke(this, new(nameof(HasError)));
    }
}
