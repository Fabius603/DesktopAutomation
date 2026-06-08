using System.Collections.Specialized;
using System.Windows.Controls;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Views
{
    public partial class ExecutionLogsView : UserControl
    {
        private INotifyCollectionChanged? _currentEntries;

        public ExecutionLogsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (_, _) => AttachToEntries();
            Unloaded += (_, _) => DetachFromEntries();
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
            if (LogEntriesList.Items.Count == 0)
                return;

            var last = LogEntriesList.Items[LogEntriesList.Items.Count - 1];
            Dispatcher.BeginInvoke(new System.Action(() => LogEntriesList.ScrollIntoView(last)));
        }
    }
}
