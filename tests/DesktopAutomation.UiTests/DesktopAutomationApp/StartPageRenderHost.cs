using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Views;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class StartPageRenderHost
{
    public static int Run(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Light.xaml") });
            foreach (var culture in new[] { "de-DE", "en-US" })
            {
                LocalizationService.Instance.SetCulture(culture);
                var view = new StartView { DataContext = new Dashboard() };
                var window = new global::DesktopAutomationApp.MainWindow
                {
                    DataContext = new Shell(view),
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -20000,
                    Top = -20000
                };
                window.Show();
                var deadline = DateTime.UtcNow.AddSeconds(3);
                while (DateTime.UtcNow < deadline && !Descendants<DataGridRow>(view).Any())
                {
                    Pump();
                    Thread.Sleep(10);
                }
                Pump();
                Ensure(window.ActualWidth == window.MinWidth && window.ActualHeight == window.MinHeight, "Startup must use the minimum window size.");
                var outerScroll = Descendants<ScrollViewer>(view).First();
                Ensure(outerScroll.ScrollableHeight < 1 && outerScroll.ScrollableWidth < 1, "All dashboard sections must fit without outer scrolling.");
                Ensure(Descendants<DataGridRow>(view).Any(), "The automation table must render populated rows.");
                foreach (var header in Descendants<DataGridColumnHeader>(view).Where(header => header.Content is string))
                {
                    var presenter = Descendants<ContentPresenter>(header).First();
                    presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Ensure(presenter.DesiredSize.Width <= header.ActualWidth + 1, "Dashboard column headers must be fully readable.");
                }
                window.UpdateLayout();
                var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var stream = File.Create(Path.Combine(directory, culture + ".png"))) encoder.Save(stream);
                window.Close();
            }
            app.Shutdown();
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Shell(StartView view)
    {
        public object CurrentContent => view;
        public string WindowTitle => "Dashboard preview";
        public bool HasUpdate => false;
    }

    private sealed class Dashboard
    {
        public int ActiveAutomationCount => 8;
        public int TotalAutomationCount => 8;
        public int TotalJobCount => 12;
        public int TotalMakroCount => 6;
        public int RunningTotalCount => 1;
        public object[] ActiveAutomations => Enumerable.Range(1, 8).Select(index => (object)new { Name = $"Automation {index}", Trigger = "Zeitplan", Action = "Job ausführen", LastRun = "Heute", NextRun = "Morgen" }).ToArray();
        public RunningItem[] RunningItems { get; } = [new()];
    }

    private sealed class RunningItem
    {
        public string Name { get; set; } = "Beispieljob";
        public string PhaseText { get; set; } = "Ausführung";
        public int InstanceCount { get; set; } = 1;
        public bool IsMakro { get; set; }
    }
}
