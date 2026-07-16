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
            if (_vm!.SelectedStep != null && StepsList.SelectedItems.Contains(_vm.SelectedStep))
                return;

            _syncingSelection = true;
            try
            {
                if (_vm.SelectedStep is null)
                    StepsList.SelectedItems.Clear();
                else
                    StepsList.SelectedItem = _vm.SelectedStep;   // clears others — intentional for programmatic nav
            }
            finally { _syncingSelection = false; }
        }

        // ── View → VM: sync multi-selection to VM ──
        private void StepsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection || sender is not ListBox lb) return;

            // Scroll last selected item into view.
            if (lb.SelectedItem != null)
                lb.Dispatcher.BeginInvoke(() => lb.ScrollIntoView(lb.SelectedItem));

            _vm?.SetSelectedSteps(lb.SelectedItems.Cast<object>());
        }

        private void StepsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(StepsList, e.OriginalSource as DependencyObject) is not ListBoxItem item)
                return;
            var toggle = FindVisualChild<ToggleButton>(item, "DetailsToggle");
            if (toggle is not null) toggle.IsChecked = toggle.IsChecked != true;
            e.Handled = true;
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
