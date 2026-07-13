using System.ComponentModel;
using System.Globalization;

namespace DesktopAutomationApp.Localization;

public interface ILocalizationService : INotifyPropertyChanged
{
    CultureInfo CurrentCulture { get; }
    string this[string key] { get; }
    event EventHandler? CultureChanged;
    void SetCulture(string cultureName);
    string Format(string key, params object?[] arguments);
}

