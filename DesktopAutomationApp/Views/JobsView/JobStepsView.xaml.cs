using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Behaviors;

namespace DesktopAutomationApp.Views
{
    /// <summary>
    /// Interaktionslogik für JobStepsView.xaml
    /// </summary>
    public partial class JobStepsView : UserControl
    {
        private JobStepsViewModel? _vm;
        private bool _syncingSelection;

        public JobStepsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        // ── VM → View: react when SelectedStep changes programmatically (delete, paste, undo …) ──
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm = e.NewValue as JobStepsViewModel;
            if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(JobStepsViewModel.SelectedStep)) return;

            // If the item is already among the selected ones, leave multi-selection intact.
            if (_vm!.SelectedStep != null && AllStepLists().Any(list => list.SelectedItems.Contains(_vm.SelectedStep)))
                return;

            _syncingSelection = true;
            try
            {
                if (_vm.SelectedStep is null)
                {
                    foreach (var list in AllStepLists()) list.SelectedItems.Clear();
                }
                else
                {
                    var target = AllStepLists().FirstOrDefault(list => list.Items.Contains(_vm.SelectedStep));
                    foreach (var list in AllStepLists())
                        if (!ReferenceEquals(list, target)) list.SelectedItems.Clear();
                    if (target != null)
                    {
                        target.SelectedItem = _vm.SelectedStep;
                        target.Dispatcher.BeginInvoke(() => target.ScrollIntoView(_vm.SelectedStep));
                    }
                }
            }
            finally { _syncingSelection = false; }
        }

        // ── View → VM: sync multi-selection to VM ──
        private void StepsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection || sender is not ListBox lb) return;

            _syncingSelection = true;
            try
            {
                foreach (var other in AllStepLists())
                    if (!ReferenceEquals(other, lb)) other.SelectedItems.Clear();
            }
            finally { _syncingSelection = false; }

            // Scroll last selected item into view.
            if (lb.SelectedItem != null)
                lb.Dispatcher.BeginInvoke(() => lb.ScrollIntoView(lb.SelectedItem));

            _vm?.SetSelectedSteps(lb.SelectedItems.Cast<object>(), lb.ItemsSource as System.Collections.IList);
        }

        private void StepsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox list
                || ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) is not ListBoxItem item)
                return;
            var toggle = FindVisualChild<ToggleButton>(item, "DetailsToggle");
            if (toggle is not null) toggle.IsChecked = toggle.IsChecked != true;
            e.Handled = true;
        }

        private IEnumerable<ListBox> AllStepLists()
        {
            yield return StartStepsList;
            yield return StepsList;
            yield return EndStepsList;
        }

        private void StepMoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { ContextMenu: { } menu } button)
            {
                menu.PlacementTarget = button;
                menu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void EndSettingsButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EndSettingsPopup.IsOpen = !EndSettingsPopup.IsOpen;
            e.Handled = true;
        }

        private void StepSection_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Expander expander && e.Data.GetDataPresent(StepDragDrop.DataFormat))
                expander.IsExpanded = true;
        }

        private void StepSection_DragOver(object sender, DragEventArgs e)
        {
            if (e.Handled
                || sender is not Expander { Tag: System.Collections.IList target }
                || !StepDragDrop.TryGetPayload(e.Data, out _))
                return;

            var list = AllStepLists().FirstOrDefault(candidate => ReferenceEquals(candidate.ItemsSource, target));
            if (list == null)
                return;

            StepDragDrop.ShowSectionTarget(list);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void StepSection_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is not Expander expander)
                return;

            var point = e.GetPosition(expander);
            if (point.X >= 0 && point.X <= expander.ActualWidth
                && point.Y >= 0 && point.Y <= expander.ActualHeight)
                return;

            var list = AllStepLists().FirstOrDefault(candidate => ReferenceEquals(candidate.ItemsSource, expander.Tag));
            StepDragDrop.ClearTargetPreview(list);
        }

        private void StepSection_Drop(object sender, DragEventArgs e)
        {
            if (e.Handled
                || sender is not Expander { Tag: System.Collections.IList target }
                || !StepDragDrop.TryGetPayload(e.Data, out var payload)
                || DataContext is not JobStepsViewModel vm)
                return;

            var request = new StepDragDrop.MoveRequest(
                payload.Source,
                payload.SourceIndex,
                target,
                target.Count);
            if (vm.ReorderStepCommand.CanExecute(request))
                vm.ReorderStepCommand.Execute(request);
            StepDragDrop.ClearTargetPreview();
            e.Handled = true;
        }

        private static T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match && match.Name == name) return match;
                var nested = FindVisualChild<T>(child, name);
                if (nested is not null) return nested;
            }
            return null;
        }
    }
}
