using DesktopAutomationApp.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Views
{
    public partial class ListJobsView : UserControl
    {
        public ListJobsView() => InitializeComponent();

        private void JobGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ListJobsViewModel vm)
                vm.SetSelectedItems(JobGrid.SelectedItems.Cast<Job>());
        }

        private void JobGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0) return;
            var src = e.OriginalSource as DependencyObject;
            if (FindAncestor<System.Windows.Controls.Primitives.ButtonBase>(src) != null) return;
            var cell = FindAncestor<DataGridCell>(src);
            if (cell != null && !cell.IsReadOnly) return;
            var row = FindAncestor<DataGridRow>(src);
            if (row == null || !row.IsSelected || JobGrid.SelectedItems.Count != 1) return;
            JobGrid.SelectedItem = null;
            e.Handled = true;
        }

        private static T? FindAncestor<T>(DependencyObject? obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T t) return t;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }
    }
}

