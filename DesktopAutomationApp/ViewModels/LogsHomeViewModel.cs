using System.Diagnostics;
using System.IO;
using Common.ApplicationData;
using System.Windows.Input;

namespace DesktopAutomationApp.ViewModels;

public enum LogPageKind
{
    Jobs,
    Automations,
    Application
}

public sealed class LogsHomeViewModel : ViewModelBase
{
    public event Action<LogPageKind>? RequestOpen;

    public LogsHomeViewModel()
    {
        OpenJobLogsCommand = new RelayCommand(() => RequestOpen?.Invoke(LogPageKind.Jobs));
        OpenAutomationLogsCommand = new RelayCommand(() => RequestOpen?.Invoke(LogPageKind.Automations));
        OpenApplicationLogsCommand = new RelayCommand(() => RequestOpen?.Invoke(LogPageKind.Application));
        OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
    }

    public ICommand OpenJobLogsCommand { get; }
    public ICommand OpenAutomationLogsCommand { get; }
    public ICommand OpenApplicationLogsCommand { get; }
    public ICommand OpenLogFolderCommand { get; }

    private static void OpenLogFolder()
    {
        var directory = AppPaths.LogsDirectory;
        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
    }
}
