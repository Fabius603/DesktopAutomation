using DesktopAutomationApp.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Views
{
    public partial class ListJobsView : UserControl
    {
        public ListJobsView() => InitializeComponent();

        private void JobGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (DataContext is ListJobsViewModel vm && e.Row?.Item is Job j)
                {
                    // Name eindeutig machen
                    vm.EnsureUniqueNameFor(j);

                    // Sicherstellen, dass Items den aktuellen Stand hat
                    var index = vm.Items.IndexOf(j);
                    if (index >= 0)
                        vm.Items[index] = j;

                    // Änderungen abspeichern
                    await vm.SaveAllAsync();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void JobGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row == null || !row.IsSelected) return;
            JobGrid.SelectedItem = null;
            e.Handled = true;
        }

        private static T? FindAncestor<T>(DependencyObject? obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T t) return t;
                obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
            }
            return null;
        }
    }
}

