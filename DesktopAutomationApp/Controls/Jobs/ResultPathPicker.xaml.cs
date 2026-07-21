using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Controls.Jobs;

public partial class ResultPathPicker : UserControl
{
    private ScrollViewer? _ancestorScrollViewer;
    private bool _repositionPending;

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ResultPathPicker));

    public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(
        nameof(DisplayText), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SecondaryTextProperty = DependencyProperty.Register(
        nameof(SecondaryText), typeof(string), typeof(ResultPathPicker));

    public ResultPathPicker()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ancestorScrollViewer = VisualTreeHelperExtensions.GetAncestor<ScrollViewer>(this);
        if (_ancestorScrollViewer != null)
            _ancestorScrollViewer.ScrollChanged += OnAncestorScrollChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_ancestorScrollViewer != null)
            _ancestorScrollViewer.ScrollChanged -= OnAncestorScrollChanged;
        _ancestorScrollViewer = null;
        SelectionPopup.IsOpen = false;
    }

    private void OnAncestorScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (!SelectionPopup.IsOpen || _repositionPending) return;
        _repositionPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            _repositionPending = false;
            if (!SelectionPopup.IsOpen || _ancestorScrollViewer == null || !IsLoaded) return;

            var position = DropDownToggle.TranslatePoint(new Point(), _ancestorScrollViewer);
            var targetBounds = new Rect(position, DropDownToggle.RenderSize);
            var viewportBounds = new Rect(
                0, 0, _ancestorScrollViewer.ViewportWidth, _ancestorScrollViewer.ViewportHeight);
            if (!targetBounds.IntersectsWith(viewportBounds))
            {
                SelectionPopup.IsOpen = false;
                return;
            }

            // A Popup owns a separate native window. Toggling an offset makes WPF
            // recalculate its placement after the target moved inside a ScrollViewer.
            var offset = SelectionPopup.HorizontalOffset;
            SelectionPopup.HorizontalOffset = offset + 0.1;
            SelectionPopup.HorizontalOffset = offset;
        });
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
