using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Views
{
    public partial class ExecutionLogsView : UserControl
    {
        private INotifyCollectionChanged? _currentEntries;
        private ScrollViewer? _scrollViewer;
        private bool _followTail = true;

        public ExecutionLogsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachToEntries();
            _scrollViewer = FindVisualChild<ScrollViewer>(LogEntriesList);
            if (_scrollViewer != null)
                _scrollViewer.ScrollChanged += OnScrollChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachFromEntries();
            if (_scrollViewer != null)
                _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer = null;
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            DetachFromEntries();
            AttachToEntries();
        }

        private void AttachToEntries()
        {
            DetachFromEntries();

            if (DataContext is not ExecutionLogsViewModel vm)
                return;

            _currentEntries = vm.Entries as INotifyCollectionChanged;
            if (_currentEntries != null)
                _currentEntries.CollectionChanged += OnEntriesChanged;
        }

        private void DetachFromEntries()
        {
            if (_currentEntries != null)
                _currentEntries.CollectionChanged -= OnEntriesChanged;

            _currentEntries = null;
        }

        private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_followTail || LogEntriesList.Items.Count == 0)
                return;

            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (_followTail && LogEntriesList.Items.Count > 0)
                    LogEntriesList.ScrollIntoView(LogEntriesList.Items[LogEntriesList.Items.Count - 1]);
            }));
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Extent changes are caused by new log entries. Only user scrolling should change follow mode.
            if (e.ExtentHeightChange != 0 || e.ViewportHeightChange != 0)
                return;

            _followTail = e.VerticalOffset >= e.ExtentHeight - e.ViewportHeight - 1;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T match)
                    return match;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }
    }
}
