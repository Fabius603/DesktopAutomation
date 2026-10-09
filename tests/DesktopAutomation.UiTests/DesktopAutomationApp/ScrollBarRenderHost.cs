using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class ScrollBarRenderHost
{
    internal static int Run(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            foreach (var theme in new[] { "Dark", "Black", "Light" })
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/{theme}.xaml")
                });
                var viewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new Border { Width = 900, Height = 900, Background = (Brush)app.FindResource("App.Brush.SurfaceRaised") }
                };
                var window = new Window
                {
                    Width = 320,
                    Height = 220,
                    Left = -10000,
                    Top = -10000,
                    ShowInTaskbar = false,
                    Content = viewer,
                    Background = (Brush)app.FindResource("App.Brush.WindowBackground")
                };
                window.Show();
                Pump();
                viewer.UpdateLayout();
                var bars = Descendants<ScrollBar>(viewer).Where(bar => bar.IsVisible).ToArray();
                Assert.Equal(2, bars.Length);
                foreach (var bar in bars)
                    bar.Style = (Style)app.Resources.MergedDictionaries[0]["MahApps.Styles.ScrollBar"];
                viewer.UpdateLayout();
                Save(viewer, Path.Combine(directory, $"{theme}-before.png"));
                foreach (var bar in bars) bar.ClearValue(FrameworkElement.StyleProperty);
                viewer.UpdateLayout();
                var viewport = new Size(viewer.ViewportWidth, viewer.ViewportHeight);
                Save(viewer, Path.Combine(directory, $"{theme}-idle.png"));
                foreach (var bar in bars)
                {
                    var track = (Track)bar.Template.FindName("PART_Track", bar);
                    var thumb = track.Thumb;
                    var grip = (Border)thumb.Template.FindName("Grip", thumb);
                    double Thickness() => bar.Orientation == Orientation.Vertical ? grip.ActualWidth : grip.ActualHeight;
                    Assert.Equal(3, Thickness());
                    var muted = grip.Opacity;
                    // Explicit keyboard access remains available even though scrollbars are not tab stops.
                    Keyboard.Focus(bar);
                    Pump();
                    viewer.UpdateLayout();
                    Assert.True(bar.IsKeyboardFocusWithin);
                    Assert.Equal(7, Thickness());
                    Assert.True(grip.Opacity > muted);
                    Assert.Equal(viewport, new Size(viewer.ViewportWidth, viewer.ViewportHeight));
                    Save(viewer, Path.Combine(directory, $"{theme}-{bar.Orientation}-focus.png"));
                    Keyboard.Focus(viewer);
                    Pump();
                    viewer.UpdateLayout();
                    Assert.Equal(3, Thickness());
                    Assert.True(Mouse.Capture(thumb));
                    Pump();
                    viewer.UpdateLayout();
                    Assert.Equal(7, Thickness());
                    Mouse.Capture(null);
                    Pump();
                    viewer.UpdateLayout();
                    Assert.Equal(3, Thickness());
                    Assert.Equal(viewport, new Size(viewer.ViewportWidth, viewer.ViewportHeight));
                    var previous = bar.Orientation == Orientation.Vertical ? viewer.VerticalOffset : viewer.HorizontalOffset;
                    var command = bar.Orientation == Orientation.Vertical ? ScrollBar.PageDownCommand : ScrollBar.PageRightCommand;
                    command.Execute(null, track.IncreaseRepeatButton);
                    Pump();
                    viewer.UpdateLayout();
                    Assert.True((bar.Orientation == Orientation.Vertical ? viewer.VerticalOffset : viewer.HorizontalOffset) > previous);
                }
                window.Close();
            }
            app.Shutdown();
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

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
}
