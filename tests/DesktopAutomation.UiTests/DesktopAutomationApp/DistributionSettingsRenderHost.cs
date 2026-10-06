using DesktopAutomation.Application.Deployment;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging.Abstractions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class DistributionSettingsRenderHost
{
    public static int Run(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var app = StepListRenderHost.LoadResources(directory);
            foreach (var culture in new[] { "de-DE", "en-US" })
            {
                LocalizationService.Instance.SetCulture(culture);
                foreach (var kind in new[] { InstallationKind.Msix, InstallationKind.Unpackaged })
                {
                    var updates = new UpdateService(NullLogger<UpdateService>.Instance, new InstallationContext(kind, "1.6.0"));
                    var view = new UpdateSettingsView
                    {
                        DataContext = new UpdateSettingsViewModel(new Notes(), updates, NullLogger<UpdateSettingsViewModel>.Instance)
                    };
                    Render(view, Path.Combine(directory, culture + "-" + kind + ".png"), () =>
                    {
                        var check = Descendants<Button>(view).Single(button => AutomationProperties.GetAutomationId(button) == "CheckForUpdates");
                        if (check.IsEnabled) throw new InvalidOperationException("Externally managed update button must be disabled.");
                        if (!Descendants<TextBlock>(view).Any(text => text.Text == ((UpdateSettingsViewModel)view.DataContext).UpdateCheckStatus))
                            throw new InvalidOperationException("Distribution status must be rendered.");
                    });
                }
                var general = new GeneralSettingsView { DataContext = new GeneralProjection() };
                Render(general, Path.Combine(directory, culture + "-startup.png"), () =>
                {
                    foreach (var check in Descendants<CheckBox>(general))
                        if (check.IsEnabled) throw new InvalidOperationException("Local builds must not configure startup.");
                });
            }
            app.Shutdown();
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void Render(UserControl view, string path, Action validate)
    {
        var window = new Window { Width = 900, Height = 700, Content = view, ShowInTaskbar = false, Left = -20000, Top = -20000 };
        window.SetResourceReference(Control.ForegroundProperty, "App.Brush.TextPrimary");
        window.SetResourceReference(Control.BackgroundProperty, "App.Brush.WindowBackground");
        window.Show();
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
        window.UpdateLayout();
        validate();
        var bitmap = new RenderTargetBitmap((int)view.ActualWidth, (int)view.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(path)) encoder.Save(stream);
        window.Close();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private sealed class Notes : IReleaseNotesService
    {
        public Task ShowIfNewAsync() => Task.CompletedTask;
        public Task ShowAllAsync() => Task.CompletedTask;
    }

    private sealed class GeneralProjection
    {
        public bool StartWithWindows { get; set; } = true;
        public bool StartInBackgroundAtWindowsStartup { get; set; }
        public LanguageOption[] Languages { get; } = [new("de-DE", "Deutsch"), new("en-US", "English")];
        public ThemeOption[] Themes { get; } = [];
        public AccentOption[] Accents { get; } = [];
        public LanguageOption? SelectedLanguage { get; set; }
        public ThemeOption? SelectedTheme { get; set; }
        public AccentOption? SelectedAccent { get; set; }
        public string ForceStopKeyDisplay => "F12";
        public string ForceStopKeyButtonText => Loc.Get("Settings.ForceStopKey.Change");
        public RelayCommand CaptureForceStopKeyCommand { get; } = new(() => { });
        public bool CanConfigureStartup => false;
        public bool CanConfigureBackgroundStartup => false;
        public string StartupStatus => Loc.Get("Settings.Startup.Unavailable");
    }
}
