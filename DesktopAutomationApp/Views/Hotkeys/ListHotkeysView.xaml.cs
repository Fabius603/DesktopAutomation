using DesktopAutomationApp.Models;
using DesktopAutomationApp.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopAutomationApp.Views
{
    public partial class ListHotkeysView : UserControl
    {
        public ListHotkeysView() => InitializeComponent();

        private void HotkeyGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ListHotkeysViewModel vm)
                vm.SetSelectedItems(HotkeyGrid.SelectedItems.Cast<EditableHotkey>());
        }

        private void HotkeyGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0) return;
            var src = e.OriginalSource as DependencyObject;
            if (FindAncestor<System.Windows.Controls.Primitives.ButtonBase>(src) != null) return;
            var cell = FindAncestor<DataGridCell>(src);
            if (cell != null && !cell.IsReadOnly) return;
            var row = FindAncestor<DataGridRow>(src);
            if (row == null || !row.IsSelected || HotkeyGrid.SelectedItems.Count != 1) return;
            HotkeyGrid.SelectedItem = null;
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
