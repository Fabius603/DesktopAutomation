using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Controls.Jobs;

public partial class ResultPathPicker : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ResultPathPicker));

    public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(
        nameof(DisplayText), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SecondaryTextProperty = DependencyProperty.Register(
        nameof(SecondaryText), typeof(string), typeof(ResultPathPicker));

    public ResultPathPicker() => InitializeComponent();

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public string? SecondaryText
    {
        get => (string?)GetValue(SecondaryTextProperty);
        set => SetValue(SecondaryTextProperty, value);
    }

    private void TreeNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ConditionSelectionNode node } button) return;
        if (node.IsSelectable)
        {
            SelectionPopup.IsOpen = false;
            return;
        }

        var item = VisualTreeHelperExtensions.GetAncestor<TreeViewItem>(button);
        if (item is not null) item.IsExpanded = !item.IsExpanded;
    }
}
