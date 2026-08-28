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
    private bool _repositionPending;
    public event EventHandler? DropDownOpened;

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ResultPathPicker));

    public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(
        nameof(DisplayText), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty InlineTextProperty = DependencyProperty.Register(
        nameof(InlineText), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

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

    public static readonly DependencyProperty ToolTipValueProperty = DependencyProperty.Register(
        nameof(ToolTipValue), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ToolTipDescriptionProperty = DependencyProperty.Register(
        nameof(ToolTipDescription), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
        nameof(SearchText), typeof(string), typeof(ResultPathPicker),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ExpectedTypeTextProperty = DependencyProperty.Register(
        nameof(ExpectedTypeText), typeof(string), typeof(ResultPathPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CreateCommandProperty = DependencyProperty.Register(
        nameof(CreateCommand), typeof(ICommand), typeof(ResultPathPicker));

    public static readonly DependencyProperty ShowCreateActionProperty = DependencyProperty.Register(
        nameof(ShowCreateAction), typeof(bool), typeof(ResultPathPicker), new PropertyMetadata(false));

    public static readonly DependencyProperty PreviewDensityProperty = DependencyProperty.Register(
        nameof(PreviewDensity), typeof(string), typeof(ResultPathPicker), new PropertyMetadata("Wide"));

    public ResultPathPicker()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        SizeChanged += OnSizeChanged;
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

    public string InlineText
    {
        get => (string)GetValue(InlineTextProperty);
        set => SetValue(InlineTextProperty, value);
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

    public string ToolTipValue
    {
        get => (string)GetValue(ToolTipValueProperty);
        set => SetValue(ToolTipValueProperty, value);
    }

    public string ToolTipDescription
    {
        get => (string)GetValue(ToolTipDescriptionProperty);
        set => SetValue(ToolTipDescriptionProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public string ExpectedTypeText
    {
        get => (string)GetValue(ExpectedTypeTextProperty);
        set => SetValue(ExpectedTypeTextProperty, value);
    }

    public ICommand? CreateCommand
    {
        get => (ICommand?)GetValue(CreateCommandProperty);
        set => SetValue(CreateCommandProperty, value);
    }

    public bool ShowCreateAction
    {
        get => (bool)GetValue(ShowCreateActionProperty);
        set => SetValue(ShowCreateActionProperty, value);
    }

    public string PreviewDensity
    {
        get => (string)GetValue(PreviewDensityProperty);
        private set => SetValue(PreviewDensityProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_ancestorScrollViewer != null)
            _ancestorScrollViewer.ScrollChanged -= OnAncestorScrollChanged;
        _ancestorScrollViewer = VisualTreeHelperExtensions.GetAncestor<ScrollViewer>(this);
        if (_ancestorScrollViewer != null)
            _ancestorScrollViewer.ScrollChanged += OnAncestorScrollChanged;
        UpdatePreviewDensity(ActualWidth);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdatePreviewDensity(e.NewSize.Width);

    private void UpdatePreviewDensity(double width) => PreviewDensity = width switch
    {
        < 170 => "VeryNarrow",
        < 280 => "Compact",
        < 400 => "Normal",
        _ => "Wide"
    };

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_ancestorScrollViewer != null)
            _ancestorScrollViewer.ScrollChanged -= OnAncestorScrollChanged;
        _ancestorScrollViewer = null;
        SelectionPopup.IsOpen = false;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is false) SelectionPopup.IsOpen = false;
    }

    private void OnAncestorScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (!SelectionPopup.IsOpen || _repositionPending) return;
        _repositionPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            _repositionPending = false;
            if (!SelectionPopup.IsOpen || _ancestorScrollViewer == null || !IsLoaded) return;
            if (!_ancestorScrollViewer.IsAncestorOf(DropDownToggle)) return;

            var position = DropDownToggle.TranslatePoint(new Point(), _ancestorScrollViewer);
            if (!IsTargetInsideViewport(
                    position, DropDownToggle.RenderSize, _ancestorScrollViewer.RenderSize))
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
        if (node.IsSelectable || !node.HasChildren) return;

        var item = VisualTreeHelperExtensions.GetAncestor<TreeViewItem>(button);
        if (item is not null) item.IsExpanded = !item.IsExpanded;
    }

    private void DropDownToggle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!SelectionPopup.IsOpen) return;
        SelectionPopup.IsOpen = false;
        DropDownToggle.Focus();
        e.Handled = true;
    }

    private void DropDownToggle_Click(object sender, RoutedEventArgs e)
    {
        if (!SelectionPopup.IsOpen) SelectionPopup.IsOpen = true;
    }

    private void SelectionPopup_Closed(object? sender, EventArgs e)
    {
        DropDownToggle.IsChecked = false;
    }

    private void SelectionPopup_Opened(object? sender, EventArgs e)
    {
        DropDownOpened?.Invoke(this, EventArgs.Empty);
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (SelectionPopup.IsOpen)
                SearchBox.Focus();
        });
    }

    internal static bool IsTargetInsideViewport(
        Point targetPosition,
        Size targetSize,
        Size viewportSize)
    {
        // ScrollViewer.ViewportWidth/ViewportHeight use item units when logical
        // scrolling is enabled (for example inside the virtualized overlay lists).
        // Positions and RenderSize are always device-independent pixels.
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0) return true;
        return new Rect(targetPosition, targetSize)
            .IntersectsWith(new Rect(new Point(), viewportSize));
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !SelectionPopup.IsOpen) return;
        SelectionPopup.IsOpen = false;
        DropDownToggle.Focus();
        e.Handled = true;
    }
}
