using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Views
{
    public partial class MakroStepsView : UserControl
    {
        private MakroStepsViewModel? _vm;
        private bool _syncingSelection;
        private MacroListItem? _contextItem;

        public MakroStepsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm = e.NewValue as MakroStepsViewModel;
            if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MakroStepsViewModel.SelectedStep)) return;

            var visibleItem = _vm!.SelectedStep is null ? null : _vm.GetVisibleItem(_vm.SelectedStep);
            if (visibleItem != null && StepsList.SelectedItems.Contains(visibleItem))
                return;

            _syncingSelection = true;
            try
            {
                if (_vm.SelectedStep is null)
                    StepsList.SelectedItems.Clear();
                else
                    StepsList.SelectedItem = visibleItem;
            }
            finally { _syncingSelection = false; }
        }

        private void StepsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection || sender is not ListBox lb) return;

            if (lb.SelectedItem != null)
                lb.Dispatcher.BeginInvoke(() => lb.ScrollIntoView(lb.SelectedItem));

            _syncingSelection = true;
            try { _vm?.SetSelectedSteps(lb.SelectedItems.Cast<object>()); }
            finally { _syncingSelection = false; }
        }

        private void StepsList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(StepsList, e.OriginalSource as DependencyObject) is not ListBoxItem item)
                return;

            _contextItem = item.DataContext as MacroListItem;
            if (_contextItem is MacroGroupListItem)
            {
                StepsList.SelectedItems.Clear();
                item.IsSelected = true;
                return;
            }

            if (item.IsSelected) return;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                StepsList.SelectedItems.Clear();
            item.IsSelected = true;
        }

        private void StepsContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var groupContext = _contextItem is MacroGroupListItem;
            var groupVisibility = groupContext ? Visibility.Visible : Visibility.Collapsed;
            var stepVisibility = groupContext ? Visibility.Collapsed : Visibility.Visible;

            GroupToggleMenuItem.Visibility = groupVisibility;
            GroupRenameMenuItem.Visibility = groupVisibility;
            GroupDissolveMenuItem.Visibility = groupVisibility;
            StepCreateGroupMenuItem.Visibility = stepVisibility;
            StepRemoveGroupMenuItem.Visibility = stepVisibility;
            StepContextSeparator.Visibility = stepVisibility;
            StepCopyMenuItem.Visibility = stepVisibility;
            StepDuplicateMenuItem.Visibility = stepVisibility;
            StepDeleteMenuItem.Visibility = stepVisibility;

            if (_contextItem is MacroGroupListItem group)
            {
                GroupToggleMenuItem.CommandParameter = group.GroupId;
                GroupRenameMenuItem.CommandParameter = group.GroupId;
                GroupDissolveMenuItem.CommandParameter = group.GroupId;
            }
        }
    }
}
