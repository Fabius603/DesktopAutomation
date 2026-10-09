using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Views;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class ValidationLayoutRenderingTests
{
    [Fact]
    public async Task ErrorsAndSourceChanges_KeepFieldAndFlowGeometryStable()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "validation-layout");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(ValidationLayoutRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-validation-layout");
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

internal static class ValidationLayoutRenderHost
{
    public static int Run(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            foreach (var theme in new[] { "Light", "Dark", "Black" })
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                { Source = new Uri($"pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/{theme}.xaml") });
                var steps = Enumerable.Range(0, 30).Select(_ => new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 250 } }).ToArray();
                using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Name = "Validation preview", Steps = [.. steps] });
                var view = new JobStepsView { DataContext = vm };
                var window = new Window { Width = 1360, Height = 840, Content = view, Background = (Brush)app.FindResource("App.Brush.Surface"), Left = -10000, Top = -10000, ShowInTaskbar = false };
                window.Show();
                vm.SetSelectedSteps([steps[0]], vm.Steps);
                Pump();
                var flow = (ScrollViewer)view.FindName("JobFlowScrollViewer");
                var inspector = (ScrollViewer)view.FindName("StepInspectorScrollViewer");
                flow.ScrollToVerticalOffset(200);
                Pump();
                var originalFlow = Bounds(flow, view);
                var originalInspector = Bounds(inspector, view);
                var originalOffset = flow.VerticalOffset;
                var delay = vm.SelectedGeneratedEditor!.Fields.Single(field => field.Descriptor.Id == "delay_ms");
                var input = Descendants<TextBox>(view).First(control => control.IsVisible && ReferenceEquals(control.DataContext, delay));
                var originalInput = Bounds(input, view);
                Save(view, Path.Combine(directory, $"{theme}-valid.png"));
                delay.InputText = "invalid";
                Pump();
                Assert.True(vm.HasInlineEditorError);
                Assert.True(delay.Validation.HasError);
                Assert.False(string.IsNullOrWhiteSpace(delay.Validation.Message));
                Assert.Equal(originalInput, Bounds(input, view));
                Assert.Equal(originalFlow, Bounds(flow, view));
                Assert.Equal(originalInspector, Bounds(inspector, view));
                Assert.Equal(originalOffset, flow.VerticalOffset);
                var outline = Assert.Single(Descendants<Border>(view), border => border.IsVisible && AutomationProperties.GetAutomationId(border) == "StepFieldErrorOutline");
                Assert.Equal(originalInput, Bounds(outline, view));
                var sourceAction = Descendants<Button>(view).Single(button => button.IsVisible && ReferenceEquals(button.DataContext, delay) && AutomationProperties.GetAutomationId(button) == "StepValueSourceAction");
                sourceAction.Focus();
                Pump();
                Assert.Equal(1, sourceAction.Opacity);
                Assert.True(Bounds(outline, view).Right <= Bounds(sourceAction, view).Left);
                Assert.Equal(originalInput, Bounds(outline, view));
                var message = Descendants<TextBlock>(view).Single(block => AutomationProperties.GetAutomationId(block) == "SelectedJobStepValidationError");
                Assert.True(message.IsVisible);
                Assert.True(message.TranslatePoint(new Point(), inspector).Y > inspector.ActualHeight / 2);
                Save(view, Path.Combine(directory, $"{theme}-invalid.png"));
                delay.InputText = "500";
                Pump();
                Assert.False(delay.Validation.HasError);
                Assert.False(vm.HasInlineEditorError);
                Assert.Equal(originalOffset, flow.VerticalOffset);
                delay.InputReferenceEditor!.Picker.UseJobVariableCommand.Execute(null);
                Pump();
                Assert.True(delay.Validation.HasError);
                Assert.Equal(originalFlow, Bounds(flow, view));
                Assert.Equal(originalInspector, Bounds(inspector, view));
                Assert.Equal(originalOffset, flow.VerticalOffset);
                Assert.True(Bounds(outline, view).Right <= Bounds(sourceAction, view).Left);
                Save(view, Path.Combine(directory, $"{theme}-variable.png"));
                window.Close();
            }
            app.Shutdown();
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static Rect Bounds(FrameworkElement element, Visual relativeTo) => new(element.TranslatePoint(new Point(), (UIElement)relativeTo), element.RenderSize);
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var item in Descendants<T>(child)) yield return item;
        }
    }
    private static void Save(FrameworkElement surface, string path)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(surface.ActualWidth * 1.5), (int)Math.Ceiling(surface.ActualHeight * 1.5), 144, 144, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
