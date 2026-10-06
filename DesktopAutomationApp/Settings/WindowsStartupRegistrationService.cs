using Common.ApplicationData;
using DesktopAutomation.Application.Deployment;
using Microsoft.Win32;
using System.IO;
using Velopack.Locators;
using Windows.ApplicationModel;

namespace DesktopAutomationApp.Settings;

public enum StartupRegistrationStatus { Enabled, Disabled, DisabledByUser, Unavailable }

public interface IWindowsStartupRegistrationService
{
    bool IsSupported { get; }
    StartupRegistrationStatus Status { get; }
    Task ApplyAsync(bool enabled, bool startInBackground, bool userInitiated = false);
}

public sealed class WindowsStartupRegistrationService(InstallationContext installation) : IWindowsStartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopAutomation";
    public const string StartupTaskId = "DesktopAutomationStartup";
    public bool IsSupported => installation.CanRegisterStartup && AppPaths.IsDefaultProfile;
    public StartupRegistrationStatus Status { get; private set; } = StartupRegistrationStatus.Unavailable;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task ApplyAsync(bool enabled, bool startInBackground, bool userInitiated = false)
    {
        if (!IsSupported) return;
        await _gate.WaitAsync();
        try { await ApplyCoreAsync(enabled, userInitiated); }
        finally { _gate.Release(); }
    }

    private async Task ApplyCoreAsync(bool enabled, bool userInitiated)
    {
        if (installation.Kind == InstallationKind.Msix)
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (!enabled) task.Disable();
            else if (userInitiated && task.State == StartupTaskState.Disabled)
                await task.RequestEnableAsync();
            Status = task.State switch
            {
                StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy => StartupRegistrationStatus.Enabled,
                StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy => StartupRegistrationStatus.DisabledByUser,
                _ => StartupRegistrationStatus.Disabled
            };
            if (Status == StartupRegistrationStatus.Enabled) RemoveLegacyRegistration();
            return;
        }

        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new IOException("Cannot open the Windows startup registration.");
        if (!enabled)
        {
            RemoveLegacyRegistration();
            Status = StartupRegistrationStatus.Disabled;
            return;
        }
        // A normal launch must not re-create an entry removed by the user.
        if (userInitiated)
        {
            var locator = VelopackLocator.Current;
            var processPath = Environment.ProcessPath ?? throw new IOException("Application path unavailable.");
            var launcher = Path.Combine(locator.RootAppDir!, Path.GetFileName(processPath));
            if (!File.Exists(launcher)) throw new FileNotFoundException("Stable Velopack launcher unavailable.", launcher);
            runKey.SetValue(ValueName, $"\"{launcher}\" --startup", RegistryValueKind.String);
        }
        Status = runKey.GetValue(ValueName) is string ? StartupRegistrationStatus.Enabled : StartupRegistrationStatus.Disabled;
    }

    private static void RemoveLegacyRegistration()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (runKey?.GetValue(ValueName) is string command && command.StartsWith('"'))
        {
            var closingQuote = command.IndexOf('"', 1);
            if (closingQuote > 1 && string.Equals(Path.GetFileName(command[1..closingQuote]), "DesktopAutomationApp.exe", StringComparison.OrdinalIgnoreCase))
                runKey.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
