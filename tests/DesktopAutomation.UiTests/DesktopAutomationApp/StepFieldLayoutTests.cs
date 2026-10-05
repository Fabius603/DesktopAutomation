using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopAutomationApp.Controls;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class StepFieldLayoutTests
{
    [Theory]
    [InlineData(120)]
    [InlineData(200)]
    [InlineData(500)]
    public void SourceButton_StaysBesideItsInputAtEveryEditorWidth(double width) => RunSta(() =>
    {
        var input = new Border { Height = 34 };
        var button = new Button { Width = 34, Height = 34 };
        var panel = new ResponsiveActionPanel { CanWrap = false, HorizontalSpacing = 5 };
        panel.Children.Add(input);
        panel.Children.Add(button);
        panel.Measure(new Size(width, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));

        Assert.Equal(34, panel.ActualHeight);
        Assert.Equal(VisualTreeHelper.GetOffset(input).Y, VisualTreeHelper.GetOffset(button).Y);
        Assert.Equal(width - 34, VisualTreeHelper.GetOffset(button).X);
        Assert.True(input.ActualWidth + 5 <= VisualTreeHelper.GetOffset(button).X);
    });

    [Theory]
    [InlineData(300, false)]
    [InlineData(450, false)]
    [InlineData(504, true)]
    [InlineData(700, true)]
    public void ConditionFields_AreAllInOneRowOrAllInOneColumn(double width, bool horizontal) => RunSta(() =>
    {
        var panel = new ResponsiveColumnsPanel
        {
            MinItemWidth = 160,
            MaxColumns = 3,
            KeepAllColumnsTogether = true,
            HorizontalSpacing = 12,
            VerticalSpacing = 10
        };
        for (var index = 0; index < 3; index++) panel.Children.Add(new Border { Height = 34 });
        panel.Measure(new Size(width, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));
        var offsets = panel.Children.Cast<UIElement>().Select(VisualTreeHelper.GetOffset).ToArray();

        if (horizontal)
        {
            Assert.All(offsets, offset => Assert.Equal(0, offset.Y));
            Assert.True(offsets[0].X < offsets[1].X && offsets[1].X < offsets[2].X);
        }
        else
        {
            Assert.All(offsets, offset => Assert.Equal(0, offset.X));
            Assert.True(offsets[0].Y < offsets[1].Y && offsets[1].Y < offsets[2].Y);
        }
    });

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Layout did not finish.");
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
