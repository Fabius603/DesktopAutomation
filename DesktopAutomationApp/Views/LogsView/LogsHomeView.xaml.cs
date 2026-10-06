using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;
using Microsoft.Win32;

namespace DesktopAutomationApp.Views;

public partial class LogsHomeView : UserControl
{
    public LogsHomeView() => InitializeComponent();
    private LogsHomeViewModel? _vm;
    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (DataContext is not LogsHomeViewModel vm) return;
        _vm = vm;
        vm.RequestCopy += Copy;
        vm.RequestCheckPath += CheckPath;
        vm.ChooseExportPath = () =>
        {
            var dialog = new SaveFileDialog { Filter = Loc.Get("Logs.Ui.ZipFilter"), DefaultExt = ".zip", FileName = "Logs-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip", OverwritePrompt = true };
            return Task.FromResult(dialog.ShowDialog(Window.GetWindow(this)) == true ? dialog.FileName : null);
        };
        vm.Activate(true);
    }
    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (_vm is null) return;
        _vm.RequestCopy -= Copy; _vm.RequestCheckPath -= CheckPath; _vm.ChooseExportPath = null; _vm.Activate(false); _vm = null;
    }
    private static void Copy(string text)
    {
        try { Clipboard.SetText(text); }
        catch (ExternalException) { AppDialog.Show(Loc.Get("Logs.Ui.CopyFailed"), Loc.Get("Logs.Title"), MessageBoxButton.OK, MessageBoxImage.Information); }
    }
    private static void CheckPath(string path)
    {
        try
        {
            if (Directory.Exists(path)) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            else if (File.Exists(path)) Process.Start(new ProcessStartInfo("explorer.exe") { Arguments = "/select,\"" + path + "\"", UseShellExecute = true });
            else AppDialog.Show(Loc.Format("Logs.Ui.PathUnavailable", path), Loc.Get("Logs.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException) { AppDialog.Show(Loc.Get("Logs.Ui.PathOpenFailed"), Loc.Get("Logs.Title"), MessageBoxButton.OK, MessageBoxImage.Information); }
    }
}
