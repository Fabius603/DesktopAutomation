using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopAutomationApp.Behaviors;

internal static class LogTailFollowBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(LogTailFollowBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(FollowState), typeof(LogTailFollowBehavior));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not ListBox listBox) return;
        if ((bool)args.NewValue)
        {
            var state = new FollowState(listBox);
            listBox.SetValue(StateProperty, state);
            state.Attach();
        }
        else if (listBox.GetValue(StateProperty) is FollowState state)
        {
            state.Detach();
            listBox.ClearValue(StateProperty);
        }
    }

    private sealed class FollowState(ListBox listBox)
    {
        private INotifyCollectionChanged? _entries;
        private ScrollViewer? _scrollViewer;
        private bool _followTail = true;

        public void Attach()
        {
            listBox.Loaded += OnLoaded;
            listBox.Unloaded += OnUnloaded;
            if (listBox.IsLoaded) OnLoaded(listBox, new RoutedEventArgs());
        }

        public void Detach()
        {
            listBox.Loaded -= OnLoaded;
            listBox.Unloaded -= OnUnloaded;
            DetachLoadedState();
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            DetachLoadedState();
            _followTail = true;
            _entries = listBox.ItemsSource as INotifyCollectionChanged;
            if (_entries != null) _entries.CollectionChanged += OnEntriesChanged;
            _scrollViewer = FindVisualChild<ScrollViewer>(listBox);
            if (_scrollViewer != null) _scrollViewer.ScrollChanged += OnScrollChanged;
            ScrollToTail();
        }

        private void OnUnloaded(object sender, RoutedEventArgs args) => DetachLoadedState();

        private void DetachLoadedState()
        {
            if (_entries != null) _entries.CollectionChanged -= OnEntriesChanged;
            if (_scrollViewer != null) _scrollViewer.ScrollChanged -= OnScrollChanged;
            _entries = null;
            _scrollViewer = null;
        }

        private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (_followTail) ScrollToTail();
        }

        private void ScrollToTail() => listBox.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_followTail && listBox.Items.Count > 0)
                listBox.ScrollIntoView(listBox.Items[^1]);
        }));

        private void OnScrollChanged(object sender, ScrollChangedEventArgs args)
        {
            if (args.ExtentHeightChange != 0 || args.ViewportHeightChange != 0) return;
            _followTail = args.VerticalOffset >= args.ExtentHeight - args.ViewportHeight - 1;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T match) return match;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null) return descendant;
            }
            return null;
        }
    }
}
