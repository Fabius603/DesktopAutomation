namespace DesktopAutomationApp.Localization;

public static class Loc
{
    public static string Get(string key) => LocalizationService.Instance[key];
    public static string Format(string key, params object?[] arguments) => LocalizationService.Instance.Format(key, arguments);
}

