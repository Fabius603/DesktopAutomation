using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

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
            ic.DragOver -= OnDragOver;
            ic.DragLeave -= OnDragLeave;
            ic.GiveFeedback -= OnGiveFeedback;
            ic.QueryContinueDrag -= OnQueryContinueDrag;
            ic.AllowDrop = false;

            if (e.NewValue is ICommand)
            {
                ic.AllowDrop = true;
                ic.PreviewMouseLeftButtonDown += OnMouseDown;
                ic.PreviewMouseMove += OnMouseMove;
                ic.Drop += OnDrop;
                ic.DragOver += OnDragOver;
                ic.DragLeave += OnDragLeave;
                ic.GiveFeedback += OnGiveFeedback;
                ic.QueryContinueDrag += OnQueryContinueDrag;
            }
        }

        private static Point _dragStart;
        private static int _fromIndex = -1;
        private static FrameworkElement? _draggedContainer;
        private static DragAdorner? _dragAdorner;
        private static InsertionAdorner? _insertionAdorner;
        private static int _currentInsertIndex = -1;
        private static bool _isDragging;

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ItemsControl ic) return;
            _dragStart = e.GetPosition(ic);
            _fromIndex = ContainerIndexAt(ic, e.OriginalSource as DependencyObject);

            if (_fromIndex >= 0)
            {
                _draggedContainer = ic.ItemContainerGenerator.ContainerFromIndex(_fromIndex) as FrameworkElement;
            }
            else
            {
                _draggedContainer = null;
            }
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ItemsControl ic || e.LeftButton != MouseButtonState.Pressed) return;
            if (_fromIndex < 0 || _draggedContainer == null || _isDragging) return;

            var pos = e.GetPosition(ic);
            if ((pos - _dragStart).Length <= 6) return;

            _isDragging = true;

            // Adorner erstellen
            var adornerLayer = AdornerLayer.GetAdornerLayer(ic);
            if (adornerLayer != null)
            {
                _dragAdorner = new DragAdorner(ic, _draggedContainer);
                adornerLayer.Add(_dragAdorner);

                _insertionAdorner = new InsertionAdorner(ic);
                adornerLayer.Add(_insertionAdorner);
            }

            // Gezogenes Item visuell verstecken
            _draggedContainer.Opacity = 0.3;

            try
            {
                DragDrop.DoDragDrop(ic, new DataObject("reorder", _fromIndex), DragDropEffects.Move);
            }
            finally
            {
                CleanupAdorners(ic);
            }
        }

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            if (sender is not ItemsControl ic) return;
            if (!e.Data.GetDataPresent("reorder")) return;

            e.Effects = DragDropEffects.Move;
            e.Handled = true;

            var pos = e.GetPosition(ic);

            // DragAdorner Position aktualisieren
            _dragAdorner?.UpdatePosition(pos);

            // Zielindex und Insertion-Y berechnen
            UpdateInsertionPosition(ic, pos);
        }

        private static void OnDragLeave(object sender, DragEventArgs e)
        {
            _insertionAdorner?.Hide();
            _currentInsertIndex = -1;
        }

        private static void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = true;
            e.Handled = true;
        }

        private static void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (e.EscapePressed)
            {
                e.Action = DragAction.Cancel;
                e.Handled = true;
            }
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is not ItemsControl ic) return;
            if (!e.Data.GetDataPresent("reorder")) return;
            var from = (int)e.Data.GetData("reorder")!;

            // Endgültigen Index berechnen
            var to = _currentInsertIndex >= 0
                ? _currentInsertIndex
                : ContainerIndexAt(ic, e.OriginalSource as DependencyObject);

            // Korrektur: wenn wir nach unten verschieben, muss der Index angepasst werden
            // da _currentInsertIndex den Slot-Index vor dem Entfernen darstellt
            if (to > from) to--;

            if (to < 0 || from == to) return;

            var cmd = GetMoveCommand(ic);
            if (cmd?.CanExecute((from, to)) == true)
                cmd.Execute((from, to));
        }

        private static void UpdateInsertionPosition(ItemsControl ic, Point mousePos)
        {
            int insertIndex = -1;
            double insertionY = 0;

            for (int i = 0; i < ic.Items.Count; i++)
            {
                if (ic.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                    continue;

                var transform = container.TransformToAncestor(ic);
                var topLeft = transform.Transform(new Point(0, 0));
                var bottomLeft = transform.Transform(new Point(0, container.ActualHeight));
                double midY = (topLeft.Y + bottomLeft.Y) / 2;

                if (mousePos.Y < midY)
                {
                    insertIndex = i;
                    insertionY = topLeft.Y;
                    break;
                }

                // Nach dem letzten Element
                if (i == ic.Items.Count - 1)
                {
                    insertIndex = i + 1;
                    insertionY = bottomLeft.Y;
                }
            }

            if (insertIndex < 0)
            {
                _insertionAdorner?.Hide();
                _currentInsertIndex = -1;
                return;
            }

            // Nicht an der Position des gezogenen Elements anzeigen
            if (insertIndex == _fromIndex || insertIndex == _fromIndex + 1)
            {
                _insertionAdorner?.Hide();
                _currentInsertIndex = -1;
                return;
            }

            _currentInsertIndex = insertIndex;
            _insertionAdorner?.UpdatePosition(insertionY);
            _insertionAdorner?.Show();
        }

        private static void CleanupAdorners(ItemsControl ic)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(ic);
            if (adornerLayer != null)
            {
                if (_dragAdorner != null) adornerLayer.Remove(_dragAdorner);
                if (_insertionAdorner != null) adornerLayer.Remove(_insertionAdorner);
            }

            if (_draggedContainer != null)
                _draggedContainer.Opacity = 1.0;

            _dragAdorner = null;
            _insertionAdorner = null;
            _draggedContainer = null;
            _currentInsertIndex = -1;
            _isDragging = false;
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

        // ═══════════════════════════════════════════════════════════════
        //  DragAdorner — halbtransparente Kopie des gezogenen Elements
        // ═══════════════════════════════════════════════════════════════
        private sealed class DragAdorner : Adorner
        {
            private readonly Rectangle _child;
            private readonly TranslateTransform _transform;
            private readonly double _offsetX;
            private readonly double _offsetY;

            public DragAdorner(UIElement adornedElement, FrameworkElement draggedItem)
                : base(adornedElement)
            {
                IsHitTestVisible = false;

                _transform = new TranslateTransform();

                _child = new Rectangle
                {
                    Width = draggedItem.ActualWidth,
                    Height = draggedItem.ActualHeight,
                    Fill = new VisualBrush(draggedItem)
                    {
                        Opacity = 0.8,
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top
                    },
                    Effect = new DropShadowEffect
                    {
                        ShadowDepth = 4,
                        Opacity = 0.4,
                        BlurRadius = 8
                    },
                    RenderTransform = _transform,
                    IsHitTestVisible = false
                };

                // Anfangsoffset: Mitte des gezogenen Elements
                _offsetX = -draggedItem.ActualWidth / 2;
                _offsetY = -draggedItem.ActualHeight / 2;

                AddVisualChild(_child);
            }

            public void UpdatePosition(Point pos)
            {
                _transform.X = pos.X + _offsetX;
                _transform.Y = pos.Y + _offsetY;
            }

            protected override int VisualChildrenCount => 1;
            protected override Visual GetVisualChild(int index) => _child;

            protected override Size MeasureOverride(Size constraint)
            {
                _child.Measure(constraint);
                return _child.DesiredSize;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                _child.Arrange(new Rect(finalSize));
                return finalSize;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  InsertionAdorner — horizontale Einfügelinie
        // ═══════════════════════════════════════════════════════════════
        private sealed class InsertionAdorner : Adorner
        {
            private double _insertionY;
            private bool _isVisible;
            private readonly Pen _pen;

            public InsertionAdorner(UIElement adornedElement) : base(adornedElement)
            {
                IsHitTestVisible = false;

                var accentBrush = TryFindAccentBrush(adornedElement) ?? Brushes.DodgerBlue;
                _pen = new Pen(accentBrush, 3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                _pen.Freeze();
            }

            public void UpdatePosition(double y)
            {
                _insertionY = y;
                InvalidateVisual();
            }

            public void Show()
            {
                if (_isVisible) return;
                _isVisible = true;
                InvalidateVisual();
            }

            public void Hide()
            {
                if (!_isVisible) return;
                _isVisible = false;
                InvalidateVisual();
            }

            protected override void OnRender(DrawingContext dc)
            {
                if (!_isVisible) return;

                double width = ((FrameworkElement)AdornedElement).ActualWidth;
                double margin = 8;
                double y = _insertionY;

                // Linie zeichnen
                dc.DrawLine(_pen, new Point(margin, y), new Point(width - margin, y));

                // Kleine Kreise an den Enden
                var brush = _pen.Brush;
                dc.DrawEllipse(brush, null, new Point(margin, y), 4, 4);
                dc.DrawEllipse(brush, null, new Point(width - margin, y), 4, 4);
            }

            private static Brush? TryFindAccentBrush(DependencyObject element)
            {
                if (element is FrameworkElement fe)
                {
                    var brush = fe.TryFindResource("MahApps.Brushes.Accent") as Brush
                             ?? fe.TryFindResource("App.Brush.Accent") as Brush;
                    return brush;
                }
                return null;
            }
        }
    }
}
