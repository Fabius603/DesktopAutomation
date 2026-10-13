using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Views;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using DesktopAutomationApp.Behaviors;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class ResultHelpRenderingTests
{
    [Fact]
    public async Task OutputHelp_WrapsInBothLanguagesAndAllThemesAndKeepsDiagnosticsCollapsed()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "result-help-rendering");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(ResultHelpRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-result-help");
        start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
    }
}

internal static class ResultHelpRenderHost
{
    public static int Run(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            foreach (var theme in new[] { "Light", "Dark", "Black" })
                foreach (var culture in new[] { "de-DE", "en-US" })
                    foreach (var scale in new[] { 1d, 1.5d, 2d })
                    {
                        ControlzEx.Theming.ThemeManager.Current.ChangeTheme(app, $"{(theme == "Light" ? "Light" : "Dark")}.Blue");
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary
                        { Source = new Uri($"pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/{theme}.xaml") });
                        LocalizationService.Instance.SetCulture(culture);
                        var step = new OcrStep();
                        using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Name = "OCR", Steps = [step] });
                        vm.SelectedStep = vm.Steps[0];
                        var view = new JobStepsView { DataContext = vm, LayoutTransform = new ScaleTransform(scale, scale) };
                        var window = new Window
                        {
                            Width = 1360 * scale,
                            Height = 900 * scale,
                            Content = view,
                            Background = (Brush)app.FindResource("App.Brush.Surface"),
                            Left = -10000,
                            Top = -10000,
                            ShowInTaskbar = false
                        };
                        try
                        {
                            RenderWindowSizing.Resize(window, 1360 * scale, 900 * scale);
                            window.Show();
                            ((TabControl)view.FindName("SelectedJobStepTabs")).SelectedIndex = 1;
                            Pump();
                            Assert.Equal(1360 * scale, window.ActualWidth, 1);
                            Assert.Equal(900 * scale, window.ActualHeight, 1);
                            var tree = Assert.Single(Descendants<TreeView>(view), item => item.IsVisible);
                            var descriptions = Descendants<TextBlock>(tree).Where(block => block.IsVisible
                                && block.GetBindingExpression(TextBlock.TextProperty)?.ParentBinding.Path?.Path == "Description").ToArray();
                            Assert.Empty(descriptions);
                            var helpTargets = Descendants<Grid>(tree).Where(grid =>
                                grid.IsVisible && !string.IsNullOrWhiteSpace(HoverHelp.GetText(grid))).ToArray();
                            Assert.NotEmpty(helpTargets);
                            Assert.All(helpTargets, target => Assert.False(Assert.IsType<ToolTip>(target.ToolTip).IsOpen));
                            var helpTarget = helpTargets.First(target => HoverHelp.GetText(target)!.Contains(culture == "de-DE" ? "0,92" : "0.92"));
                            var tooltip = Assert.IsType<ToolTip>(helpTarget.ToolTip);
                            Assert.True(ToolTipService.GetShowOnDisabled(helpTarget));
                            tooltip.PlacementTarget = helpTarget;
                            tooltip.IsOpen = true;
                            Pump();
                            var helpText = Assert.IsType<TextBlock>(tooltip.Content);
                            Assert.Equal(TextWrapping.Wrap, helpText.TextWrapping);
                            Assert.Equal(HoverHelp.GetText(helpTarget), helpText.Text);
                            Assert.True(helpText.ActualWidth > 100 && helpText.ActualWidth <= 400);
                            Assert.True(helpText.ActualHeight > 0);
                            Save(tooltip, Path.Combine(directory, $"output-tooltip-{theme}-{culture}-{scale:0.0}.png"));
                            tooltip.IsOpen = false;
                            var diagnostic = tree.Items.Cast<object>().Single(item =>
                                item is global::DesktopAutomationApp.Converters.StepResultPropertyDetails details
                                && details.Name == Loc.Get("Ui.ValueReference.ResultDiagnostics"));
                            var container = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(diagnostic);
                            Assert.NotNull(container);
                            Assert.False(container.IsExpanded);
                            Save(window, Path.Combine(directory, $"{theme}-{culture}-{scale:0.0}.png"));
                            container.IsExpanded = true;
                            container.BringIntoView();
                            Pump();
                            Assert.True(container.IsExpanded);
                            Assert.Equal(2, container.Items.Count);
                            ((TabControl)view.FindName("SelectedJobStepTabs")).SelectedIndex = 0;
                            Pump();
                            var fieldHosts = Descendants<Grid>(view).Where(grid => grid.Name == "GeneratedFieldHost" && grid.IsVisible).ToArray();
                            Assert.NotEmpty(fieldHosts);
                            Assert.All(fieldHosts, field =>
                            {
                                var help = Assert.IsType<ToolTip>(field.ToolTip);
                                Assert.False(help.IsOpen);
                                Assert.False(string.IsNullOrWhiteSpace(HoverHelp.GetText(field)));
                                Assert.DoesNotContain("[Ui.", HoverHelp.GetText(field));
                            });
                            var title = Descendants<TextBlock>(view).First(block => block.IsVisible
                                && block.GetBindingExpression(TextBlock.TextProperty)?.ParentBinding.Path?.Path == "SelectedStepDisplayName");
                            Assert.False(Assert.IsType<ToolTip>(title.ToolTip).IsOpen);
                            Assert.Equal(vm.SelectedStepDescription, HoverHelp.GetText(title));
                            Save(window, Path.Combine(directory, $"inputs-{theme}-{culture}-{scale:0.0}.png"));
                        }
                        finally { window.Close(); }
                    }
            RenderCapturedImageTooltips(app, directory);
            app.Shutdown();
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void RenderCapturedImageTooltips(Application app, string directory)
    {
        foreach (var culture in new[] { "de-DE", "en-US" })
        {
            LocalizationService.Instance.SetCulture(culture);
            var source = new SourceStepItem("capture", "Capture desktop",
                StepLocalization.ResultType(StepResultMetadata.GetResultType(nameof(DesktopDuplicationResult))!));
            var picker = new ValueReferencePickerViewModel([source], InputContractRegistry.Find(typeof(SaveImageStep), "image")!);
            picker.UseStepResultCommand.Execute(null);
            var control = new global::DesktopAutomationApp.Controls.Jobs.ValueReferencePicker
            { DataContext = picker, Width = 340, HorizontalAlignment = HorizontalAlignment.Left };
            var window = new Window { Width = 440, Height = 140, Content = control, Left = -10000, Top = -10000, ShowInTaskbar = false };
            try
            {
                window.Show();
                Pump();
                var target = Assert.Single(Descendants<System.Windows.Controls.Primitives.ToggleButton>(control));
                var tooltip = Assert.IsType<ToolTip>(target.ToolTip);
                tooltip.PlacementTarget = target;
                tooltip.IsOpen = true;
                try
                {
                    Pump();
                    var text = string.Join("\n", Descendants<TextBlock>(tooltip).Select(block => block.Text));
                    Assert.Contains(picker.SelectedPropertyName, text);
                    Assert.Contains("Capture desktop \u2192 ", text);
                    Assert.DoesNotContain("\u00e2\u2020\u2019", text);
                    Save(tooltip, Path.Combine(directory, $"image-tooltip-{culture}.png"));
                }
                finally { tooltip.IsOpen = false; }
            }
            finally { window.Close(); }
        }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T typed) yield return typed;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Pump()
    {
        for (var index = 0; index < 12; index++)
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Thread.Sleep(15);
        }
    }
    private static void Save(FrameworkElement view, string path)
    {
        var bitmap = new RenderTargetBitmap((int)view.ActualWidth, (int)view.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
