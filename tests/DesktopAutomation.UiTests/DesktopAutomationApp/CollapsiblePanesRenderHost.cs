using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Views;
using MahApps.Metro.IconPacks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class CollapsiblePanesRenderHost
{
    public static int Run(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            foreach (var culture in new[] { "de-DE", "en-US" })
            {
                LocalizationService.Instance.SetCulture(culture);
                foreach (var theme in new[] { "Black", "Light" })
                {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    { Source = new Uri($"pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/{theme}.xaml") });
                    var first = new TimeoutStep();
                    var second = new TimeoutStep();
                    using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Name = "Pane preview", Steps = [first, second] });
                    vm.SelectedStep = first;
                    var view = new JobStepsView { DataContext = vm };
                    var shell = new Shell(view);
                    var window = new global::DesktopAutomationApp.MainWindow
                    {
                        DataContext = shell,
                        Width = 1520,
                        ShowInTaskbar = false,
                        WindowTransitionsEnabled = false,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Left = -20000,
                        Top = -20000
                    };
                    window.Show();
                    Pump();
                    var transition = Descendants<global::DesktopAutomationApp.Controls.PageTransitionContentControl>(window).Single();
                    var deadline = DateTime.UtcNow.AddSeconds(2);
                    while (transition.Opacity < 0.999 && DateTime.UtcNow < deadline) { Thread.Sleep(10); Pump(); }
                    Assert.True(transition.Opacity >= 0.999);
                    var pane = window.NavigationPane;
                    Assert.True(pane.IsExpanded);
                    Assert.True(vm.InspectorPane.IsExpanded);
                    var brand = (TextBlock)window.FindName("NavigationBrandTitle");
                    var navigationToggle = (Button)window.FindName("NavigationPaneToggle");
                    Assert.True(brand.IsVisible);
                    var titleEnd = brand.TranslatePoint(new Point(brand.ActualWidth, 0), window);
                    var toggleStart = navigationToggle.TranslatePoint(new Point(), window);
                    Assert.True(toggleStart.X >= titleEnd.X);
                    Assert.InRange(Math.Abs(toggleStart.Y - titleEnd.Y), 0, 10);
                    VerifyToggleIcon(navigationToggle);
                    var navigation = Descendants<Button>(window).Where(button => AutomationProperties.GetAutomationId(button).StartsWith("Navigation.")
                        && AutomationProperties.GetAutomationId(button) is not "Navigation.Toggle" and not "Navigation.Update").ToArray();
                    Assert.Equal(6, navigation.Length);
                    foreach (var button in navigation)
                    {
                        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)));
                        Assert.False(string.IsNullOrWhiteSpace(button.ToolTip?.ToString()));
                        Assert.Single(Descendants<PackIconMaterial>(button));
                        Assert.Contains(Descendants<TextBlock>(button), label => label.IsVisible);
                    }
                    Save(window, Path.Combine(directory, $"{culture}-{theme}-expanded.png"));
                    Click(Descendants<Button>(window).Single(button => AutomationProperties.GetAutomationId(button) == "Navigation.Toggle"));
                    Pump();
                    Assert.True(pane.IsCollapsed);
                    Assert.False(brand.IsVisible);
                    VerifyToggleIcon(navigationToggle);
                    foreach (var button in navigation)
                    {
                        Assert.DoesNotContain(Descendants<TextBlock>(button), label => label.IsVisible);
                        var icon = Assert.Single(Descendants<PackIconMaterial>(button));
                        Assert.True(icon.IsVisible);
                        var center = icon.TranslatePoint(new Point(icon.ActualWidth / 2, 0), button).X;
                        Assert.InRange(center, button.ActualWidth / 2 - 2, button.ActualWidth / 2 + 2);
                        Click(button);
                    }
                    Assert.Equal(6, shell.Navigations);
                    window.Width = 1600;
                    Pump();
                    Assert.True(pane.IsCollapsed);
                    pane.ToggleCommand.Execute(null);
                    Pump();
                    Assert.True(pane.IsExpanded);

                    var column = (ColumnDefinition)view.FindName("DebugInspectorColumn");
                    column.Width = new GridLength(420);
                    Pump();
                    var list = (ListBox)view.FindName("StepsList");
                    VerifyToggleIcon((Button)view.FindName("CollapseInspectorButton"));
                    var expandedListWidth = list.ActualWidth;
                    Click((Button)view.FindName("CollapseInspectorButton"));
                    Pump();
                    Assert.True(vm.InspectorPane.IsCollapsed);
                    Assert.True(list.ActualWidth > expandedListWidth + 250);
                    Assert.False(((Border)view.FindName("InspectorSurface")).IsVisible);
                    var peek = (Border)view.FindName("InspectorPeekSurface");
                    Assert.True(peek.IsVisible);
                    Assert.True(peek.ActualWidth > 32 && peek.ActualWidth < 60);
                    Assert.True(peek.ActualHeight > view.ActualHeight / 2);
                    var listRight = list.TranslatePoint(new Point(list.ActualWidth, 0), view).X;
                    var peekLeft = peek.TranslatePoint(new Point(), view).X;
                    Assert.True(peekLeft - listRight >= 12, "The collapsed panel must keep a visible gap from the steps.");
                    VerifyToggleIcon((Button)view.FindName("ExpandInspectorButton"));
                    vm.SelectedStep = second;
                    Pump();
                    Assert.True(vm.InspectorPane.IsCollapsed);
                    Click((Button)view.FindName("ExpandInspectorButton"));
                    Pump();
                    Assert.True(vm.InspectorPane.IsExpanded);
                    Assert.Equal(420, column.ActualWidth, 1);
                    Assert.Same(second, vm.SelectedStep);

                    window.Width = window.MinWidth;
                    Pump();
                    Assert.True(pane.IsCollapsed);
                    pane.ToggleCommand.Execute(null); // Explicitly open the navigation in the narrow window.
                    Pump();
                    Assert.True(pane.IsExpanded);
                    Assert.True(vm.InspectorPane.IsCollapsed);
                    window.Width = 1520;
                    Pump();
                    Assert.True(pane.IsExpanded);
                    Assert.True(vm.InspectorPane.IsExpanded);
                    pane.ToggleCommand.Execute(null);
                    vm.InspectorPane.ToggleCommand.Execute(null);
                    window.Width = window.MinWidth;
                    Pump();
                    Assert.True(pane.IsCollapsed);
                    Assert.True(vm.InspectorPane.IsCollapsed);
                    Save(window, Path.Combine(directory, $"{culture}-{theme}-collapsed.png"));
                    window.Close();
                }
            }
            app.Shutdown();
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Click(Button button)
    {
        Assert.True(button.IsVisible && button.IsEnabled);
        typeof(ButtonBase).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(button, null);
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static void VerifyToggleIcon(Button button)
    {
        var icon = Assert.Single(Descendants<global::DesktopAutomationApp.Controls.PaneToggleIcon>(button));
        var point = icon.TranslatePoint(new Point(), button);
        Assert.True(icon.IsVisible);
        Assert.InRange(point.X, 0, button.ActualWidth - icon.ActualWidth);
        Assert.InRange(point.Y, 0, button.ActualHeight - icon.ActualHeight);
    }

    private static void Save(FrameworkElement surface, string path)
    {
        var bitmap = new RenderTargetBitmap((int)surface.ActualWidth, (int)surface.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typed) yield return typed;
            foreach (var item in Descendants<T>(child)) yield return item;
        }
    }

    private sealed class Shell(JobStepsView view)
    {
        public object CurrentContent => view;
        public string WindowTitle => "Pane preview";
        public string ActiveNavigationKey => "ListJobsViewModel";
        public bool HasUpdate => false;
        public int Navigations { get; private set; }
        public RelayCommand ShowStart => new(() => Navigations++);
        public RelayCommand ShowListAutomations => new(() => Navigations++);
        public RelayCommand ShowListMakros => new(() => Navigations++);
        public RelayCommand ShowListJobs => new(() => Navigations++);
        public RelayCommand ShowExecutionLogs => new(() => Navigations++);
        public RelayCommand ShowSettings => new(() => Navigations++);
    }
}
