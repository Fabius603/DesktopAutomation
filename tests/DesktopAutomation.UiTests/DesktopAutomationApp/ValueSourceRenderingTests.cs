using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Controls.Jobs.Editors.Generated;
using DesktopAutomationApp.Controls.Jobs.Roi;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class ValueSourceRenderingTests
{
    [Fact]
    public async Task OverlayRows_FitNarrowInspector_AndOfferAllActionsThroughTheSharedMenu()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "overlays");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(ValueSourceRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-overlays");
        start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
    }

    [Fact]
    public async Task SourceActions_RevealOnKeyboardFocus_KeepMenusUsable_WithoutChangingFieldGeometry()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "value-sources");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(ValueSourceRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-value-sources");
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

internal static class ValueSourceRenderHost
{
    public static int RunOverlays(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            foreach (var theme in new[] { "Light", "Dark", "Black" })
                foreach (var width in new[] { 340, 520 })
                {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    { Source = new Uri($"pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/{theme}.xaml") });
                    var source = new OcrStep();
                    var detection = new TemplateMatchingStep();
                    var selected = new ShowOnDesktopStep();
                    using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Steps = [source, detection, selected] });
                    vm.SelectedStep = selected;
                    var overlays = vm.SelectedGeneratedEditor!.Fields.Single().VisualOverlayEditor!;
                    overlays.AddOverlayDetectionCommand.Execute(null);
                    overlays.AddOverlayTextCommand.Execute(null);
                    overlays.AddOverlayTextCommand.Execute(null);
                    var editor = new GeneratedStepEditor { DataContext = new { GeneratedEditor = vm.SelectedGeneratedEditor } };
                    var view = new Border { Padding = new Thickness(12), Background = (Brush)app.FindResource("App.Brush.Surface"), Child = new ScrollViewer { Content = editor, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
                    var window = new Window { Width = width, Height = 1000, Content = view, Left = -10000, Top = -10000, ShowInTaskbar = false };
                    window.Show();
                    Pump();
                    Save(view, Path.Combine(directory, $"{theme}-{width}-compact.png"));
                    var menus = Descendants<Button>(editor).Where(button => AutomationProperties.GetAutomationId(button) == "OverlayRowActions").ToArray();
                    Assert.Equal(3, menus.Length);
                    foreach (var button in menus)
                    {
                        Assert.True(button.TranslatePoint(new Point(), view).X + button.ActualWidth <= view.ActualWidth - 12);
                        Assert.True(button.Focus());
                        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Pump();
                        var row = Ancestor<ListBoxItem>(button);
                        Assert.True(row.ContextMenu!.IsOpen);
                        Assert.Equal(4, row.ContextMenu.Items.OfType<MenuItem>().Count());
                        Assert.True(row.IsSelected);
                        Save(row.ContextMenu, Path.Combine(directory, $"{theme}-{width}-menu.png"));
                        row.ContextMenu.IsOpen = false;
                    }
                    var fields = Descendants<GeneratedValueSourceInput>(editor).Where(field => field.IsVisible).ToArray();
                    Assert.All(fields, field => Assert.True(field.ActualWidth >= 200));
                    foreach (var expander in Descendants<Expander>(editor)) expander.IsExpanded = true;
                    Pump();
                    Save(view, Path.Combine(directory, $"{theme}-{width}-expanded.png"));
                    window.Close();
                }
            app.Shutdown();
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    public static int Run(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            foreach (var theme in new[] { "Light", "Dark", "Black" })
                foreach (var width in new[] { 340, 520 })
                {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/{theme}.xaml")
                    });
                    var source = new OcrStep();
                    var selected = new TemplateMatchingStep();
                    using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Steps = [source, selected] });
                    vm.SelectedStep = selected;
                    var editor = new GeneratedStepEditor { DataContext = new { GeneratedEditor = vm.SelectedGeneratedEditor } };
                    var focusSink = new Button { Width = 80, Height = 30, Content = "Focus sink" };
                    var layout = new StackPanel { Children = { focusSink, editor } };
                    var view = new Border { Padding = new Thickness(12), Background = (Brush)app.FindResource("App.Brush.Surface"), Child = layout };
                    var window = new Window { Width = width, Height = 1000, Content = view, Left = -10000, Top = -10000, ShowInTaskbar = false };
                    window.Show();
                    Pump();
                    foreach (var expander in Descendants<Expander>(view)) expander.IsExpanded = true;
                    Pump();
                    Keyboard.Focus(focusSink);
                    Pump();
                    var actions = Descendants<Button>(view).Where(button => button.IsVisible && AutomationProperties.GetAutomationId(button).EndsWith("ValueSourceAction", StringComparison.Ordinal)).ToArray();
                    Assert.NotEmpty(actions);
                    foreach (var button in actions)
                    {
                        Assert.Equal(0, button.Opacity);
                        Assert.False(button.IsHitTestVisible);
                        Assert.True(button.IsTabStop);
                        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)));
                    }
                    // Every source action must leave all input geometries unchanged, including
                    // dropdowns, path pickers and grouped whole-value editors.
                    var inputs = Descendants<Control>(editor)
                        .Where(control => control.IsVisible && control is TextBox or ComboBox or CheckBox)
                        .Select(control => (Control: control, Bounds: new Rect(control.TranslatePoint(new Point(), editor), control.RenderSize)))
                        .ToArray();
                    foreach (var action in actions)
                    {
                        Assert.True(action.Focus());
                        Pump();
                        Assert.Equal(1, action.Opacity);
                        foreach (var input in inputs)
                            Assert.Equal(input.Bounds, new Rect(input.Control.TranslatePoint(new Point(), editor), input.Control.RenderSize));
                        Keyboard.Focus(focusSink);
                        Pump();
                        Assert.Equal(0, action.Opacity);
                    }
                    var roi = Assert.Single(Descendants<RoiEditor>(view));
                    var wholeAction = Assert.Single(Descendants<Button>(roi), button => AutomationProperties.GetAutomationId(button) == "WholeValueSourceAction");
                    var component = Descendants<GeneratedValueSourceInput>(roi).First();
                    var componentAction = Assert.Single(Descendants<Button>(component), button => AutomationProperties.GetAutomationId(button) == "StepValueSourceAction");
                    Assert.True(wholeAction.TranslatePoint(new Point(), roi).Y + wholeAction.ActualHeight
                    <= component.TranslatePoint(new Point(), roi).Y);
                    var text = Assert.Single(Descendants<TextBox>(component));
                    var idleWidth = text.ActualWidth;
                    var hostWidth = component.ActualWidth;
                    Save(view, Path.Combine(directory, $"{theme}-{width}-idle.png"));
                    Keyboard.Focus(text);
                    Pump();
                    Assert.Equal(1, componentAction.Opacity);
                    Assert.True(componentAction.IsHitTestVisible);
                    Assert.Equal(0, wholeAction.Opacity);
                    Assert.Equal(hostWidth, component.ActualWidth);
                    Assert.True(text.ActualWidth > 60);
                    Assert.Equal(idleWidth, text.ActualWidth);
                    Save(view, Path.Combine(directory, $"{theme}-{width}-field-focus.png"));
                    Assert.True(componentAction.Focus());
                    Pump();
                    Assert.Equal(1, componentAction.Opacity);
                    componentAction.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Pump();
                    Assert.True(componentAction.ContextMenu.IsOpen);
                    Assert.Equal(1, componentAction.Opacity);
                    Assert.Equal(4, componentAction.ContextMenu.Items.Count);
                    Assert.Equal(idleWidth, text.ActualWidth);
                    Assert.Equal(hostWidth, component.ActualWidth);
                    Save(componentAction.ContextMenu, Path.Combine(directory, $"{theme}-{width}-menu.png"));
                    componentAction.ContextMenu.IsOpen = false;
                    Keyboard.Focus(focusSink);
                    Pump();
                    Assert.Equal(0, componentAction.Opacity);
                    Assert.Equal(idleWidth, text.ActualWidth);
                    Assert.Equal(hostWidth, component.ActualWidth);
                    Assert.True(wholeAction.Focus());
                    Pump();
                    Assert.Equal(1, wholeAction.Opacity);
                    Assert.Equal(0, componentAction.Opacity);
                    wholeAction.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Pump();
                    Assert.True(wholeAction.ContextMenu.IsOpen);
                    Assert.Equal(3, wholeAction.ContextMenu.Items.Count);
                    Save(wholeAction.ContextMenu, Path.Combine(directory, $"{theme}-{width}-whole-menu.png"));
                    wholeAction.ContextMenu.IsOpen = false;
                    Keyboard.Focus(focusSink);
                    Pump();
                    Assert.Equal(0, wholeAction.Opacity);
                    var nestedField = (global::DesktopAutomationApp.ViewModels.GeneratedStepFieldViewModel)component.DataContext;
                    var componentBounds = new Rect(component.TranslatePoint(new Point(), roi), component.RenderSize);
                    nestedField.InputReferenceEditor!.Picker.UseJobVariableCommand.Execute(null);
                    Pump();
                    Assert.True(nestedField.Validation.HasError);
                    Assert.Equal(componentBounds, new Rect(component.TranslatePoint(new Point(), roi), component.RenderSize));
                    Assert.Contains(Descendants<Border>(component), border => border.IsVisible && AutomationProperties.GetAutomationId(border) == "NestedStepFieldErrorOutline");
                    window.Close();
                }
            app.Shutdown();
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static T Ancestor<T>(DependencyObject child) where T : DependencyObject
    {
        for (var parent = VisualTreeHelper.GetParent(child); parent is not null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is T match) return match;
        throw new InvalidOperationException($"Missing {typeof(T).Name} ancestor.");
    }

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
        if (surface.ActualWidth < 1 || surface.ActualHeight < 1) throw new InvalidOperationException("Source surface did not render.");
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(surface.ActualWidth * 1.5), (int)Math.Ceiling(surface.ActualHeight * 1.5), 144, 144, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
