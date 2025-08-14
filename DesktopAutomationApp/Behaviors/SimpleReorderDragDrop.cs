using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopAutomationApp.Behaviors
{
    public static class SimpleReorderDragDrop
    {
        // Bindbar: ICommand<(int from, int to)>
        public static readonly DependencyProperty MoveCommandProperty =
            DependencyProperty.RegisterAttached("MoveCommand", typeof(ICommand),
                typeof(SimpleReorderDragDrop), new PropertyMetadata(null, OnAttach));

        public static void SetMoveCommand(DependencyObject d, ICommand value) => d.SetValue(MoveCommandProperty, value);
        public static ICommand GetMoveCommand(DependencyObject d) => (ICommand)d.GetValue(MoveCommandProperty);

        private static void OnAttach(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ItemsControl ic) return;

            ic.PreviewMouseLeftButtonDown -= OnMouseDown;
            ic.PreviewMouseMove -= OnMouseMove;
            ic.Drop -= OnDrop;
            ic.AllowDrop = false;

            if (e.NewValue is ICommand)
            {
                ic.AllowDrop = true;
                ic.PreviewMouseLeftButtonDown += OnMouseDown;
                ic.PreviewMouseMove += OnMouseMove;
                ic.Drop += OnDrop;
            }
        }

        private static Point _dragStart;
        private static int _fromIndex = -1;

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ItemsControl ic) return;
            _dragStart = e.GetPosition(ic);
            _fromIndex = ContainerIndexAt(ic, e.OriginalSource as DependencyObject);
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ItemsControl ic || e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(ic);
            if ((pos - _dragStart).Length > 6 && _fromIndex >= 0)
            {
                DragDrop.DoDragDrop(ic, new DataObject("reorder", _fromIndex), DragDropEffects.Move);
            }
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is not ItemsControl ic) return;
            if (!e.Data.GetDataPresent("reorder")) return;
            var from = (int)e.Data.GetData("reorder")!;
            var to = ContainerIndexAt(ic, e.OriginalSource as DependencyObject);
            if (to < 0 || from == to) return;

            var cmd = GetMoveCommand(ic);
            if (cmd?.CanExecute((from, to)) == true)
                cmd.Execute((from, to));
        }

        private static int ContainerIndexAt(ItemsControl ic, DependencyObject? origin)
        {
            if (origin == null) return -1;

            // Suche einen geeigneten Container (ListBoxItem oder ContentPresenter)
            DependencyObject? container =
                System.Windows.Media.VisualTreeHelperExtensions.GetAncestor<ListBoxItem>(origin)
                ?? System.Windows.Media.VisualTreeHelperExtensions.GetAncestor<ContentPresenter>(origin)
                ?? origin;

            // Falls wir noch nicht auf einem Item-Container sind: im Baum nach oben laufen
            while (container != null && container is not ListBoxItem && container is not ContentPresenter)
            {
                container = System.Windows.Media.VisualTreeHelper.GetParent(container);
            }

            if (container == null)
                return -1;

            // Item aus dem Container ermitteln und Index bestimmen
            var item = ic.ItemContainerGenerator.ItemFromContainer(container);
            return ic.Items.IndexOf(item);
        }
    }
}
