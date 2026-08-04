using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls.Jobs.Shared;

public partial class ChoiceGroupEditor : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ChoiceGroupEditor));

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(ChoiceGroupEditor),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(ChoiceGroupEditor));

    public static readonly DependencyProperty SelectedContentTemplateProperty = DependencyProperty.Register(
        nameof(SelectedContentTemplate), typeof(DataTemplate), typeof(ChoiceGroupEditor));

    public static readonly DependencyProperty SelectedContentProperty = DependencyProperty.Register(
        nameof(SelectedContent), typeof(object), typeof(ChoiceGroupEditor));

    public ChoiceGroupEditor() => InitializeComponent();

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public DataTemplate? SelectedContentTemplate
    {
        get => (DataTemplate?)GetValue(SelectedContentTemplateProperty);
        set => SetValue(SelectedContentTemplateProperty, value);
    }

    public object? SelectedContent
    {
        get => GetValue(SelectedContentProperty);
        set => SetValue(SelectedContentProperty, value);
    }
}
