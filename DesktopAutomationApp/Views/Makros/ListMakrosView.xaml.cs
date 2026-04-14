using DesktopAutomationApp.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.Views
{
    public partial class ListMakrosView : UserControl
    {
        public ListMakrosView() => InitializeComponent();

        private void MakroGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (DataContext is ListMakrosViewModel vm && e.Row?.Item is Makro m)
                {
                    await vm.EnsureUniqueNameFor(m);
                    CollectionViewSource.GetDefaultView(vm.Items)?.Refresh();
                    await vm.SaveSingleAsync(m);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void MakroGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row == null || !row.IsSelected) return;
            MakroGrid.SelectedItem = null;
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

