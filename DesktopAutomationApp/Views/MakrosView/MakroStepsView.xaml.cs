using DesktopAutomationApp.Controls;
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

        public MakroStepsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            PreviewKeyDown += OnPreviewKeyDown;
            Loaded += (_, _) => OnVmPropertyChanged(_vm, new PropertyChangedEventArgs(nameof(MakroStepsViewModel.SelectedStep)));
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || _vm is null) return;
            if ((e.Key == Key.Apps || e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift) && StepsList.IsKeyboardFocusWithin)
            {
                if (StepsList.ItemContainerGenerator.ContainerFromItem(StepsList.SelectedItem) is ListBoxItem item) OpenStepMenu(item);
                e.Handled = true;
                return;
            }


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
            if (_syncingSelection || _vm is null || e.PropertyName != nameof(MakroStepsViewModel.SelectedStep)) return;

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
            if (ItemsControl.ContainerFromElement(StepsList, e.OriginalSource as DependencyObject) is not ListBoxItem item) return;
            ActionMenus.SelectContextItem(StepsList, item);
            OpenStepMenu(item);
            e.Handled = true;
        }

        internal ContextMenu CreateStepMenu(FrameworkElement target, MacroListItem? context)
        {
            var menu = ActionMenus.Create(target);
            if (_vm is not { } vm) return menu;
            var count = Math.Max(1, vm.SelectedStepCount);
            var multiple = count > 1;
            if (context is MacroGroupListItem group && StepsList.SelectedItems.Count <= 1)
            {
                ActionMenus.Add(menu, "Ui.Macro.Group.Toggle", vm.ToggleGroupCommand, group.GroupId);
                ActionMenus.Add(menu, "Ui.Macro.Group.Rename", vm.RenameGroupCommand, group.GroupId);
                ActionMenus.Add(menu, "Ui.Macro.Group.Dissolve", vm.DissolveGroupCommand, group.GroupId);
                menu.Items.Add(new Separator());
            }
            else if (!multiple)
            {
                ActionMenus.Add(menu, "Ui.Common.EditStep", vm.EditStepCommand, vm.SelectedStep);
                menu.Items.Add(new Separator());
            }
            ActionMenus.Add(menu, "Ui.Common.Copy", vm.CopyCommand, gesture: "Shortcut.CtrlC", count: count);
            ActionMenus.Add(menu, "Ui.Context.Paste", vm.PasteCommand, gesture: "Shortcut.CtrlV");
            ActionMenus.Add(menu, "Ui.Macro.Steps.DuplicateStep", vm.DuplicateStepCommand, vm.SelectedStep, "Shortcut.CtrlD", count);
            menu.Items.Add(new Separator());
            ActionMenus.Add(menu, "Ui.Common.MoveStepUp", vm.MoveStepUpCommand, vm.SelectedStep, "Shortcut.AltUp", count);
            ActionMenus.Add(menu, "Ui.Common.MoveStepDown", vm.MoveStepDownCommand, vm.SelectedStep, "Shortcut.AltDown", count);
            var grouping = new MenuItem { Header = Loc.Get("Ui.Context.Group") };
            ActionMenus.Decorate(grouping, "Ui.Context.Group");
            ActionMenus.Add(grouping, "Ui.Macro.Group.Create", vm.CreateGroupCommand, gesture: "Shortcut.CtrlG");
            ActionMenus.Add(grouping, "Ui.Macro.Group.RemoveSelected", vm.RemoveFromGroupCommand, gesture: "Shortcut.CtrlShiftG", count: count);
            menu.Items.Add(grouping);
            menu.Items.Add(new Separator());
            ActionMenus.Add(menu, "Ui.Macro.Steps.DeleteStep", vm.DeleteSelectedCommand, gesture: "Shortcut.Delete", count: count);
            return menu;
        }

        private void OpenStepMenu(ListBoxItem item)
        {
            var menu = CreateStepMenu(item, item.DataContext as MacroListItem);
            item.ContextMenu = menu;
            menu.IsOpen = true;
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not DependencyObject source || ItemsControl.ContainerFromElement(StepsList, source) is not ListBoxItem item) return;
            ActionMenus.SelectContextItem(StepsList, item);
            OpenStepMenu(item);
            e.Handled = true;
        }
    }
}
