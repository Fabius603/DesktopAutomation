using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Text.Json;
using System.Globalization;

internal sealed record ScreenshotSpec(string File, int Width, int Height, int[]? Crop, string[] Labels, bool FieldDetails)
{
    public static ScreenshotSpec FromJson(JsonElement e) => new(e.GetProperty("file").GetString()!,
        e.GetProperty("width").GetInt32(), e.GetProperty("height").GetInt32(),
        e.TryGetProperty("crop", out var crop) ? crop.EnumerateArray().Select(v => v.GetInt32()).ToArray() : null,
        e.TryGetProperty("markLabels", out var labels) ? labels.EnumerateArray().Select(v => v.GetString()!).ToArray() : [],
        e.TryGetProperty("fieldDetails", out var fields) && fields.GetBoolean());
}

internal static class ScreenshotCapture
{
    private static readonly HashSet<string> Captured = [];
    public static IReadOnlySet<string> RenderedIds => Captured;
    public static void Render(object view, string id, IReadOnlyDictionary<string, ScreenshotSpec> specs,
        string output, Func<bool>? ready = null)
    {
        var spec = specs[id];
        if (Path.GetFileName(spec.File) != spec.File || spec.Width < 1 || spec.Height < 1)
            throw new InvalidOperationException("Invalid screenshot target: " + id);
        var window = new DesktopAutomationApp.MainWindow
        {
            DataContext = new Shell(view), Width = spec.Width, Height = spec.Height,
            ShowInTaskbar = false, WindowTransitionsEnabled = false,
            WindowStartupLocation = WindowStartupLocation.Manual, Left = -20000, Top = -20000
        };
        try
        {
            window.Show();
            window.UpdateLayout();
            if (spec.Crop is not null)
                foreach (var expander in Elements(window).OfType<Expander>().Where(e => e.IsVisible))
                    expander.IsExpanded = true;
            for (var i = 0; i < 100; i++)
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                Thread.Sleep(20);
                if (i > 30 && (ready?.Invoke() ?? true)) break;
            }
            if (ready is not null && !ready()) throw new InvalidOperationException("Screenshot did not settle: " + id);
            window.UpdateLayout();
            if (spec.FieldDetails)
            {
                foreach (var box in Elements(window).OfType<TextBox>().Where(b => b.IsVisible && !b.IsReadOnly && string.IsNullOrWhiteSpace(b.Text)).ToArray())
                {
                    if (Ancestors(box).Any(p => p is ComboBox)) continue;
                    if (!Ancestors(box).OfType<FrameworkElement>().Any(p => p.Name == "GeneratedFieldHost" || p.DataContext?.GetType().Name is "GeneratedStepPointFieldPairViewModel" or "GeneratedStepChoiceGroupViewModel")) continue;
                    var binding = box.GetBindingExpression(TextBox.TextProperty);
                    if (binding is null) continue;
                    var name = box.DataContext is DesktopAutomationApp.ViewModels.GeneratedStepFieldViewModel f ? f.Descriptor.Id : binding.ParentBinding.Path?.Path ?? "";
                    var value = ExampleText(name);
                    box.SetCurrentValue(TextBox.TextProperty, value);
                    binding.UpdateSource();
                }
                foreach (var box in Elements(window).OfType<ComboBox>().Where(b => b.IsVisible && b.IsEditable && string.IsNullOrWhiteSpace(b.Text)).ToArray())
                {
                    var binding = box.GetBindingExpression(ComboBox.TextProperty);
                    if (binding is null || box.DataContext is not DesktopAutomationApp.ViewModels.GeneratedStepFieldViewModel field) continue;
                    box.SetCurrentValue(ComboBox.TextProperty, ExampleText(field.Descriptor.Id));
                    binding.UpdateSource();
                }
                for (var i = 0; i < 60; i++)
                {
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    Thread.Sleep(20);
                }
                foreach (var camera in Elements(window).Select(e => e.DataContext).OfType<DesktopAutomationApp.ViewModels.GeneratedStepFieldViewModel>()
                    .Select(f => f.CameraEditor).Where(c => c is not null).Distinct())
                {
                    if (camera!.Qualities.Count == 0)
                        camera.Qualities.Add(new(TaskAutomation.Jobs.CameraQualityMode.Automatic, null, "Automatisch (Beispiel)"));
                    camera.SelectedQuality ??= camera.Qualities[0];
                }
                for (var i = 0; i < 60; i++)
                {
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    Thread.Sleep(20);
                }
                window.UpdateLayout();
            }
            var bitmap = new RenderTargetBitmap(spec.Width, spec.Height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(output, spec.File));
            encoder.Save(stream);
            if (spec.FieldDetails) FieldDetails(window, spec, output);
            else if (spec.Crop is not null) Detail(window, bitmap, spec, output);
            Captured.Add(id);
            Console.WriteLine(id + ": rendered");
        }
        finally { window.Close(); }
    }

    private static IEnumerable<FrameworkElement> Elements(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement element) yield return element;
            foreach (var nested in Elements(child)) yield return nested;
        }
    }

    private static IEnumerable<DependencyObject> Ancestors(DependencyObject element)
    {
        for (var p = VisualTreeHelper.GetParent(element); p is not null; p = VisualTreeHelper.GetParent(p)) yield return p;
    }

    private static string ExampleText(string name) => name.ToLowerInvariant() switch
    {
        var n when n.Contains("path") || n.Contains("directory") => @"C:\Beispiele\Dokumente",
        var n when n.Contains("argument") => @"C:\Beispiele\Notiz.txt",
        var n when n.Contains("filter") => "*.txt",
        var n when n.Contains("window_title") => "Editor",
        var n when n.Contains("process") => "notepad",
        var n when n.Contains("key") => "Tab",
        "script" => "return 42;",
        "description" => "Wähle den nächsten Arbeitsschritt.",
        "monitor_device_name" => @"\\.\DISPLAY1",
        var n when n.Contains("color") => "#2563EB",
        var n when n.Contains("comparison") || n.Contains("value") => "yes",
        var n when n.Contains("name") => "Beispiel",
        _ => "Beispieltext"
    };

    // Render complete native field subtrees: also retain fields below the viewport.
    // The result is an explicitly documented composite of real editor crops.
    private static void FieldDetails(Window window, ScreenshotSpec spec, string output)
    {
        var candidates = Elements(window).Where(e => e.IsVisible && e.ActualWidth > 20 && e.ActualHeight > 20 &&
            (e.Name == "GeneratedFieldHost" || e is Grid &&
                e.DataContext?.GetType().Name is "GeneratedStepPointFieldPairViewModel" or "GeneratedStepChoiceGroupViewModel"))
            .GroupBy(e => e.DataContext).Select(g => g.First()).ToHashSet();
        var hosts = Elements(window).Where(candidates.Contains)
            .Where(e => !Ancestors(e).OfType<FrameworkElement>().Any(candidates.Contains)).ToArray();
        if (hosts.Length == 0) { Detail(window, RenderWindow(window, spec), spec, output); return; }
        var width = spec.Crop![2] + 44;
        var tiles = hosts.Select(e => {
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(e.ActualWidth), (int)Math.Ceiling(e.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(e);
            var label = e.DataContext is DesktopAutomationApp.ViewModels.GeneratedStepFieldViewModel field ? field.Label :
                Elements(e).OfType<TextBlock>().FirstOrDefault(t => t.IsVisible && !string.IsNullOrWhiteSpace(t.Text))?.Text ?? "Einstellung";
            var scale = Math.Min(1d, (width - 60d) / bitmap.PixelWidth);
            return (label, bitmap, scale, height: (int)Math.Ceiling(bitmap.PixelHeight * scale));
        }).ToArray();
        var height = tiles.Sum(t => t.height + 20) + 10;
        var drawing = new DrawingVisual();
        var markers = new List<object>();
        using (var dc = drawing.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            var y = 10d;
            for (var i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                var bounds = new Rect(48, y, t.bitmap.PixelWidth * t.scale, t.height);
                dc.DrawImage(t.bitmap, bounds);
                var pen = new Pen(new SolidColorBrush(Color.FromRgb(20, 108, 177)), 2);
                dc.DrawRoundedRectangle(null, pen, bounds, 4, 4);
                dc.DrawEllipse(pen.Brush, null, new Point(17, y+17), 13, 13);
                var number = new FormattedText((i+1).ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 15, Brushes.White, 1);
                dc.DrawText(number, new Point(17-number.Width/2, y+17-number.Height/2));
                var inputs = Elements(hosts[i]).Where(e => e.IsVisible).Select(e => e switch {
                    DesktopAutomationApp.Controls.Jobs.ValueReferencePicker p when p.DataContext is DesktopAutomationApp.ViewModels.ValueReferencePickerViewModel vm =>
                        new { kind="reference", value=vm.IsConfigured ? vm.SelectedDisplayPath : "" },
                    TextBox b when !Ancestors(b).Any(p => p is ComboBox) => new { kind="text", value=b.Text },
                    ComboBox b => new { kind="choice", value=string.IsNullOrWhiteSpace(b.Text)
                        ? Elements(b).OfType<TextBlock>().FirstOrDefault(v => v.IsVisible && !string.IsNullOrWhiteSpace(v.Text))?.Text ?? "" : b.Text },
                    CheckBox b => new { kind="boolean", value=b.IsChecked == true ? "An" : "Aus" },
                    Slider b => new { kind="number", value=b.Value.ToString(CultureInfo.InvariantCulture) },
                    _ => null }).Where(v => v is not null).ToArray();
                markers.Add(new { number=i+1, label=t.label, bounds=new[] { bounds.X, bounds.Y, bounds.Width, bounds.Height }, inputs });
                y += t.height + 20;
            }
        }
        var detail = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); detail.Render(drawing);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(detail));
        var basename = Path.GetFileNameWithoutExtension(spec.File);
        using (var stream = File.Create(Path.Combine(output, basename+".detail.png"))) encoder.Save(stream);
        File.WriteAllText(Path.Combine(output, basename+".detail.json"), JsonSerializer.Serialize(new { width, height, mode="fields", markers }));
    }

    private static RenderTargetBitmap RenderWindow(Window window, ScreenshotSpec spec)
    {
        var bitmap = new RenderTargetBitmap(spec.Width, spec.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window); return bitmap;
    }

    private static void Detail(Window window, BitmapSource original, ScreenshotSpec spec, string output)
    {
        var c = spec.Crop!;
        var crop = new Rect(c[0], c[1], c[2], c[3]);
        string Caption(FrameworkElement e) => e switch { TextBlock t => t.Text, AccessText t => t.Text, Label l => l.Content as string ?? "", _ => "" };
        Rect VisibleRect(FrameworkElement e)
        {
            var r=e.TransformToAncestor(window).TransformBounds(new Rect(0,0,e.ActualWidth,e.ActualHeight));
            var full = r;
            r.Intersect(new Rect(0,0,window.ActualWidth,window.ActualHeight));
            for (var parent=VisualTreeHelper.GetParent(e); parent is not null && parent!=window; parent=VisualTreeHelper.GetParent(parent))
                if (parent is FrameworkElement ancestor && (ancestor.ClipToBounds || ancestor is ScrollContentPresenter))
                    r.Intersect(ancestor.TransformToAncestor(window).TransformBounds(new Rect(0,0,ancestor.ActualWidth,ancestor.ActualHeight)));
            return r.Width < full.Width - 1 || r.Height < full.Height - 1 ? Rect.Empty : r;
        }
        Rect TargetRect(FrameworkElement caption)
        {
            // A form annotation includes the input next to/below its label, not just the label.
            if (spec.Crop![2] < 700 && !Ancestors(caption).Any(p => p is ComboBox))
                foreach (var parent in Ancestors(caption).OfType<FrameworkElement>())
                {
                    var controls = Elements(parent).Where(e => e.IsVisible && e is TextBox or ComboBox or CheckBox or Slider).ToArray();
                    if (controls.Length == 0) continue;
                    var rect = VisibleRect(parent);
                    if (crop.Contains(rect) && rect.Width <= crop.Width && rect.Height < 500) return rect;
                    break;
                }
            return VisibleRect(caption);
        }
        var candidates = Elements(window).Where(e => e.IsVisible && !string.IsNullOrWhiteSpace(Caption(e)) && !Ancestors(e).Any(p => p is ComboBox))
            .Select(e => (label: Caption(e).Trim().TrimEnd(':'), rect: TargetRect(e)))
            .Where(e => crop.Contains(e.rect) && e.rect.Width > 10 && e.rect.Height > 10).ToList();
        var targets = spec.Labels.Select(label => candidates.FirstOrDefault(e => e.label == label || e.label.StartsWith(label+" (")))
            .Where(e => e.label is not null).DistinctBy(e => e.label).ToList();
        if (targets.Count == 0 && candidates.Count > 0) targets.Add(candidates[0]);
        var maximumHeight=Math.Min(c[3],(int)window.ActualHeight-c[1]);
        var badgeY = new double[targets.Count];
        var previousY = -15d;
        foreach (var index in Enumerable.Range(0, targets.Count).OrderBy(i => targets[i].rect.Y))
        {
            badgeY[index] = Math.Max(targets[index].rect.Y + targets[index].rect.Height / 2 - c[1], previousY + 30);
            previousY = badgeY[index];
        }
        var height = targets.Count > 0 ? Math.Clamp((int)targets.Max(t => t.rect.Bottom)-c[1]+200, Math.Min(260,maximumHeight), maximumHeight) : maximumHeight;
        var drawing = new DrawingVisual();
        using (var dc = drawing.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, c[2] + 44, height));
            dc.PushClip(new RectangleGeometry(new Rect(44, 0, c[2], height)));
            dc.DrawImage(original, new Rect(44-c[0], -c[1], spec.Width, spec.Height));
            dc.Pop();
            for (var i = 0; i < targets.Count; i++)
            {
                var r = targets[i].rect; r.Offset(44-c[0], -c[1]); r.Inflate(3, 3);
                var pen = new Pen(new SolidColorBrush(Color.FromRgb(20, 108, 177)), 2);
                dc.DrawRoundedRectangle(null, pen, r, 3, 3);
                dc.DrawLine(pen, new Point(30, badgeY[i]), new Point(r.X, r.Y+r.Height/2));
                dc.DrawEllipse(pen.Brush, null, new Point(16, badgeY[i]), 13, 13);
                var number = new FormattedText((i+1).ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 15, Brushes.White, 1);
                dc.DrawText(number, new Point(16-number.Width/2, badgeY[i]-number.Height/2));
            }
        }
        var detail = new RenderTargetBitmap(c[2]+44, height, 96, 96, PixelFormats.Pbgra32); detail.Render(drawing);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(detail));
        var basename = Path.GetFileNameWithoutExtension(spec.File);
        using (var stream = File.Create(Path.Combine(output, basename+".detail.png"))) encoder.Save(stream);
        File.WriteAllText(Path.Combine(output, basename+".detail.json"), JsonSerializer.Serialize(new {
            width=c[2]+44, height, markers=targets.Select((t,i) => new { number=i+1, label=t.label })
        }));
    }
}
