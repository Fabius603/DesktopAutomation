using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Markup;

namespace DesktopAutomationApp.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly ResourceManager ResourceManager = new(
        "DesktopAutomationApp.Resources.Strings",
        Assembly.GetExecutingAssembly());

    public static LocalizationService Instance { get; } = new();

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("de-DE");
    public string this[string key] => ResourceManager.GetString(key, CurrentCulture) ?? $"[{key}]";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? CultureChanged;

    private LocalizationService() { }

    public void SetCulture(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var changed = !Equals(CurrentCulture, culture);
        CurrentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        if (Application.Current != null)
        {
            var language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
            foreach (Window window in Application.Current.Windows)
                window.Language = language;
        }
        if (changed)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            CultureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string Format(string key, params object?[] arguments) =>
        string.Format(CurrentCulture, this[key], arguments);
}
