using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Controls.Jobs;

public partial class ResultPathPicker : UserControl
{
    private ScrollViewer? _ancestorScrollViewer;
    private bool _popupWasOpen;
    private bool _repositionPending;

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ResultPathPicker));

    public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(
        nameof(DisplayText), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SecondaryTextProperty = DependencyProperty.Register(
        nameof(SecondaryText), typeof(string), typeof(ResultPathPicker));

    public static readonly DependencyProperty PreviewTextProperty = DependencyProperty.Register(
        nameof(PreviewText), typeof(string), typeof(ResultPathPicker));

    public static readonly DependencyProperty SourceTextProperty = DependencyProperty.Register(
        nameof(SourceText), typeof(string), typeof(ResultPathPicker));

    public static readonly DependencyProperty IsRichPreviewProperty = DependencyProperty.Register(
        nameof(IsRichPreview), typeof(bool), typeof(ResultPathPicker), new PropertyMetadata(false));

    public static readonly DependencyProperty ContextTextProperty = DependencyProperty.Register(
        nameof(ContextText), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

    public ResultPathPicker()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        PreviewKeyDown += OnPreviewKeyDown;
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

    public string? PreviewText
    {
        get => (string?)GetValue(PreviewTextProperty);
        set => SetValue(PreviewTextProperty, value);
    }

    public string? SourceText
    {
        get => (string?)GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    public bool IsRichPreview
    {
        get => (bool)GetValue(IsRichPreviewProperty);
        set => SetValue(IsRichPreviewProperty, value);
    }

    public string ContextText
    {
        get => (string)GetValue(ContextTextProperty);
        set => SetValue(ContextTextProperty, value);
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

    private void DropDownToggle_Checked(object sender, RoutedEventArgs e)
    {
        _popupWasOpen = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        });
    }

    private void DropDownToggle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!SelectionPopup.IsOpen && !_popupWasOpen) return;
        SelectionPopup.IsOpen = false;
        _popupWasOpen = false;
        DropDownToggle.Focus();
        e.Handled = true;
    }

    private void SelectionPopup_Closed(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () => _popupWasOpen = false);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !SelectionPopup.IsOpen) return;
        SelectionPopup.IsOpen = false;
        DropDownToggle.Focus();
        e.Handled = true;
    }
}
