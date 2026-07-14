using Microsoft.Win32;
using System.IO;
using Velopack.Locators;

namespace DesktopAutomationApp.Settings;

public interface IWindowsStartupRegistrationService
{
    void Apply(bool enabled, bool startInBackground);
}

public sealed class WindowsStartupRegistrationService : IWindowsStartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopAutomation";

    public void Apply(bool enabled, bool startInBackground)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Der Windows-Autostart konnte nicht geöffnet werden.");

        if (!enabled)
        {
            runKey.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = ResolveUpdateSafeExecutablePath();
        var arguments = startInBackground ? " --background" : string.Empty;
        runKey.SetValue(ValueName, $"\"{executablePath}\"{arguments}", RegistryValueKind.String);
    }

    private static string ResolveUpdateSafeExecutablePath()
    {
        var processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Der Pfad der Anwendung konnte nicht ermittelt werden.");

        var locator = VelopackLocator.Current;
        if (locator.CurrentlyInstalledVersion is not null && !string.IsNullOrWhiteSpace(locator.RootAppDir))
        {
            var stableLauncher = Path.Combine(locator.RootAppDir, Path.GetFileName(processPath));
            if (File.Exists(stableLauncher))
                return stableLauncher;
        }

        return processPath;
    }
}
