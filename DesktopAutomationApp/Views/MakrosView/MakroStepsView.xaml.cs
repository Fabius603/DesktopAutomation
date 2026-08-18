using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Input;
using DesktopAutomationApp.Localization;
using System.Windows.Controls.Primitives;

namespace DesktopAutomationApp.Views
{
    public partial class MakroStepsView : UserControl
    {
        private MakroStepsViewModel? _vm;
        private bool _syncingSelection;
        private MacroListItem? _contextItem;
        private bool _contextUsesStepSelection;

        public MakroStepsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || _vm is null) return;

            if (ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Save, _vm.SaveCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.NewItem, _vm.AddStepCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Back, _vm.BackCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Rename, _vm.RenameCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.OpenFile, _vm.OpenFileCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Execute, _vm.StartMakroCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Stop, _vm.StopMakroCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.PreviewMacro, _vm.PreviewPlaybackCommand))
                return;

            if (!StepsList.IsKeyboardFocusWithin || ViewShortcutRouter.IsTextInputFocused
                || Keyboard.FocusedElement is ButtonBase or ComboBox)
                return;

            if (AppShortcutGestures.Matches(e, AppShortcutGestures.SelectAll))
            {
                StepsList.SelectAll();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
            {
                StepsList.UnselectAll();
                e.Handled = true;
                return;
            }

            if (ViewShortcutRouter.TryExecute(e, AppShortcutGestures.AddStep, _vm.AddStepCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.EditStep, _vm.EditStepCommand, _vm.SelectedStep)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.DuplicateStep, _vm.DuplicateStepCommand, _vm.SelectedStep)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.MoveUp, _vm.MoveStepUpCommand, _vm.SelectedStep)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.MoveDown, _vm.MoveStepDownCommand, _vm.SelectedStep)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.CreateGroup, _vm.CreateGroupCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.RemoveFromGroup, _vm.RemoveFromGroupCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Delete, _vm.DeleteSelectedCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Copy, _vm.CopyCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Paste, _vm.PasteCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Undo, _vm.UndoCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Redo, _vm.RedoCommand))
                return;

            ViewShortcutRouter.TryExecute(e, AppShortcutGestures.RedoAlternate, _vm.RedoCommand);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm = e.NewValue as MakroStepsViewModel;
            if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_syncingSelection || e.PropertyName != nameof(MakroStepsViewModel.SelectedStep)) return;

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
            _contextUsesStepSelection = false;
            if (_contextItem is MacroGroupListItem)
            {
                if (StepsList.SelectedItems.Count > 1)
                {
                    _contextUsesStepSelection = true;
                    return;
                }
                StepsList.SelectedItems.Clear();
                item.IsSelected = true;
                return;
            }

            if (item.IsSelected) return;
            StepsList.SelectedItems.Clear();
            item.IsSelected = true;
        }

        private void StepsContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var groupContext = _contextItem is MacroGroupListItem && !_contextUsesStepSelection;
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

            if (!groupContext && _vm is { } viewModel)
            {
                var count = Math.Max(1, viewModel.SelectedStepCount);
                var multiple = count > 1;
                StepCopyMenuItem.Header = multiple
                    ? Loc.Format("Ui.Common.CopySelected", count)
                    : Loc.Get("Ui.Common.Copy");
                StepDuplicateMenuItem.Header = multiple
                    ? Loc.Format("Ui.Common.DuplicateSelected", count)
                    : Loc.Get("Ui.Macro.Steps.DuplicateStep");
                StepDeleteMenuItem.Header = multiple
                    ? Loc.Format("Ui.Common.DeleteSelected", count)
                    : Loc.Get("Ui.Macro.Steps.DeleteStep");
            }

            if (_contextItem is MacroGroupListItem group)
            {
                GroupToggleMenuItem.CommandParameter = group.GroupId;
                GroupRenameMenuItem.CommandParameter = group.GroupId;
                GroupDissolveMenuItem.CommandParameter = group.GroupId;
            }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { ContextMenu: { } menu } button)
            {
                menu.PlacementTarget = button;
                menu.IsOpen = true;
                e.Handled = true;
            }
        }
    }
}
