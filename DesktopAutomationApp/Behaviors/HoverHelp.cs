using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace DesktopAutomationApp.Behaviors;

/// <summary>Localized, wrapping help shown exclusively by WPF's hover tooltip service.</summary>
public static class HoverHelp
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(HoverHelp), new PropertyMetadata(null, OnTextChanged));

    public static string? GetText(DependencyObject element) => (string?)element.GetValue(TextProperty);
    public static void SetText(DependencyObject element, string? value) => element.SetValue(TextProperty, value);

    private static void OnTextChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not FrameworkElement target) return;
        var text = args.NewValue as string;
        target.SetCurrentValue(ToolTipService.ToolTipProperty, string.IsNullOrWhiteSpace(text) ? null : new ToolTip
        {
            MaxWidth = 430,
            Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 400 }
        });
        target.SetCurrentValue(AutomationProperties.HelpTextProperty, text ?? string.Empty);
        ToolTipService.SetInitialShowDelay(target, 500);
        ToolTipService.SetShowDuration(target, 30000);
        ToolTipService.SetShowOnDisabled(target, true);
    }
}
