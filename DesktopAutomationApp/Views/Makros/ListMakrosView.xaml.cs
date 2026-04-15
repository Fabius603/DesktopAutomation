using DesktopAutomationApp.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.Views
{
    public partial class ListMakrosView : UserControl
    {
        public ListMakrosView() => InitializeComponent();

        private void MakroGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ListMakrosViewModel vm)
                vm.SetSelectedItems(MakroGrid.SelectedItems.Cast<Makro>());
        }

        private void MakroGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0) return;
            var src = e.OriginalSource as DependencyObject;
            if (FindAncestor<System.Windows.Controls.Primitives.ButtonBase>(src) != null) return;
            var cell = FindAncestor<DataGridCell>(src);
            if (cell != null && !cell.IsReadOnly) return;
            var row = FindAncestor<DataGridRow>(src);
            if (row == null || !row.IsSelected || MakroGrid.SelectedItems.Count != 1) return;
            MakroGrid.SelectedItem = null;
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

