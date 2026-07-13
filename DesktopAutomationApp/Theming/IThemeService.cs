using DesktopAutomationApp.Settings;

namespace DesktopAutomationApp.Theming;

public interface IThemeService
{
    AppThemeMode EffectiveMode { get; }
    event EventHandler? ThemeChanged;
    void Apply(AppThemeMode mode, string accent);
}

