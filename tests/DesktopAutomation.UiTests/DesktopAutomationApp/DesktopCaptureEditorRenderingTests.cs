using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Controls.Jobs.Editors.Generated;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class DesktopCaptureEditorRenderingTests
{
    [Fact]
    public async Task CaptureOptions_RenderAndKeepTheirValuesAtNarrowInspectorWidth()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "capture-editor");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(DesktopCaptureEditorRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-desktop-capture");
        start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        Assert.True(File.Exists(Path.Combine(directory, "desktop-capture.png")));
    }
}

internal static class DesktopCaptureEditorRenderHost
{
    public static int Run(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            var model = new GeneratedStepEditorViewModel(new DesktopDuplicationStepDefinition());
            var monitor = model.Fields.Single(field => field.Descriptor.Id == DesktopDuplicationStepDefinition.DesktopIndexFieldId);
            model.ApplyMonitorSelection(monitor, 2, "DISPLAY-A");
            model.Fields.Single(field => field.Descriptor.Id == DesktopDuplicationStepDefinition.WaitForNewFrameFieldId).BooleanValue = false;
            model.Fields.Single(field => field.Descriptor.Id == DesktopDuplicationStepDefinition.TimeoutFieldId).IntegerValue = 1234;
            var view = new Border
            {
                Width = 380,
                Height = 820,
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 24)),
                Padding = new Thickness(12),
                Child = new GeneratedStepEditor { DataContext = new { GeneratedEditor = model } }
            };
            using var surface = new HwndSource(new HwndSourceParameters("Desktop capture editor verification")
            {
                Width = (int)view.Width,
                Height = (int)view.Height,
                PositionX = -32000,
                PositionY = -32000,
                WindowStyle = unchecked((int)0x80000000)
            });
            surface.RootVisual = view;
            void Layout()
            {
                view.Measure(new Size(view.Width, view.Height));
                view.Arrange(new Rect(0, 0, view.Width, view.Height));
                view.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
            }
            Layout();
            foreach (var expander in Descendants(view).OfType<Expander>()) expander.IsExpanded = true;
            Layout();
            var bitmap = new RenderTargetBitmap((int)view.Width, (int)view.Height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(view);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var output = File.Create(Path.Combine(directory, "desktop-capture.png"))) encoder.Save(output);
            var controls = Descendants(view).OfType<Control>().Where(control => control.IsVisible).ToArray();
            if (!controls.OfType<TextBox>().Any(text => text.Text == "1234")
                || !controls.OfType<TextBox>().Any(text => text.Text == "DISPLAY-A"))
                throw new InvalidOperationException("Capture timeout and selected monitor identity must render in the generated editor. Textboxes: "
                    + string.Join(", ", Descendants(view).OfType<TextBox>().Select(text => $"{text.Text} (visible={text.IsVisible})")));
            if (!model.TryCreateStep(out var created) || created is not TaskAutomation.Jobs.DesktopDuplicationStep capture
                || capture.Settings.WaitForNewFrame || capture.Settings.TimeoutMilliseconds != 1234 || capture.Settings.MonitorDeviceName != "DISPLAY-A")
                throw new InvalidOperationException("Rendered capture options must survive editing.");
            app.Shutdown();
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
