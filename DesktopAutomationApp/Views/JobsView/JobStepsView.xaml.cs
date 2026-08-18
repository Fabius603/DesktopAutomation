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
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using DesktopAutomationApp.Input;

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
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;
            if (_vm is null) return;

            // View-wide commands must be handled during the preview phase. Relying on
            // InputBindings alone is unreliable because editors and nested controls can
            // consume the normal KeyDown event first.
            if (ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Save, _vm.SaveCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.NewItem, _vm.AddStepCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Back, _vm.BackCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Rename, _vm.RenameCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.OpenFile, _vm.OpenFileCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Execute, _vm.StartJobCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Stop, _vm.StopJobCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.DebugJob, _vm.DebugJobCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.DebugStep, _vm.DebugStepCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.DebugContinue, _vm.DebugContinueCommand))
                return;

            if (!AllStepLists().Any(list => list.IsKeyboardFocusWithin)) return;

            if (ViewShortcutRouter.TryExecute(e, AppShortcutGestures.ToggleBreakpoint, _vm.ToggleBreakpointCommand, _vm.SelectedStep))
                return;

            if (ViewShortcutRouter.IsTextInputFocused || Keyboard.FocusedElement is ButtonBase or ComboBox) return;

            if (AppShortcutGestures.Matches(e, AppShortcutGestures.AddStep)) Execute(_vm.AddStepCommand, null, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.EditStep)) Execute(_vm.EditStepCommand, _vm.SelectedStep, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.DuplicateStep)) Execute(_vm.DuplicateStepCommand, null, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.MoveUp)) Execute(_vm.MoveStepUpCommand, _vm.SelectedStep, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.MoveDown)) Execute(_vm.MoveStepDownCommand, _vm.SelectedStep, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.ToggleEnabled)) Execute(_vm.ToggleStepEnabledCommand, _vm.SelectedStep, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Delete)) Execute(_vm.DeleteSelectedCommand, null, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Copy)) Execute(_vm.CopyCommand, null, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Paste)) Execute(_vm.PasteCommand, null, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Undo)) Execute(_vm.UndoCommand, null, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Redo)) Execute(_vm.RedoCommand, null, e);
            else if (AppShortcutGestures.Matches(e, AppShortcutGestures.RedoAlternate)) Execute(_vm.RedoCommand, null, e);
        }

        private static void Execute(ICommand command, object? parameter, KeyEventArgs e)
        {
            if (!command.CanExecute(parameter)) return;
            command.Execute(parameter);
            e.Handled = true;
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

        private void DebugInspector_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DebugInspectorColumn == null) return;
            DebugInspectorColumn.Width = e.NewValue is true
                ? new GridLength(360)
                : new GridLength(0);
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

        private void StepsList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox list
                || ItemsControl.ContainerFromElement(
                    list, e.OriginalSource as DependencyObject) is not ListBoxItem item
                || item.DataContext is not JobStep step
                || DataContext is not JobStepsViewModel vm)
                return;

            if (!item.IsSelected)
            {
                list.SelectedItems.Clear();
                item.IsSelected = true;
            }

            var menu = new ContextMenu { PlacementTarget = item };
            AddMenuItem(menu, Loc.Get("Ui.Common.Edit"), vm.EditStepCommand, step, Loc.Get("Shortcut.Enter"));
            if (step.CanBeDisabled)
                AddMenuItem(menu, Loc.Get(step.IsEnabled
                    ? "Ui.Job.Steps.DisableStep"
                    : "Ui.Job.Steps.EnableStep"), vm.ToggleStepEnabledCommand, step, Loc.Get("Shortcut.Space"));
            AddMenuItem(menu, Loc.Get("Ui.Job.Debug.ToggleBreakpoint"), vm.ToggleBreakpointCommand, step, Loc.Get("Shortcut.CtrlB"));
            menu.Items.Add(new Separator());
            AddMenuItem(menu, Loc.Get("Ui.Common.Copy"), vm.CopyCommand, null, Loc.Get("Shortcut.CtrlC"));
            AddMenuItem(menu, Loc.Get("Ui.Macro.Steps.DuplicateStep"), vm.DuplicateStepCommand, null, Loc.Get("Shortcut.CtrlD"));
            AddMenuItem(menu, Loc.Get("Ui.Common.MoveStepUp"), vm.MoveStepUpCommand, step, Loc.Get("Shortcut.AltUp"));
            AddMenuItem(menu, Loc.Get("Ui.Common.MoveStepDown"), vm.MoveStepDownCommand, step, Loc.Get("Shortcut.AltDown"));
            menu.Items.Add(new Separator());
            AddMenuItem(menu, Loc.Get("Ui.Job.Steps.MoveToStart"), vm.MoveToStartSectionCommand, step);
            AddMenuItem(menu, Loc.Get("Ui.Job.Steps.MoveToRun"), vm.MoveToRunSectionCommand, step);
            AddMenuItem(menu, Loc.Get("Ui.Job.Steps.MoveToEnd"), vm.MoveToEndSectionCommand, step);
            if (vm.AddElseIfCommand.CanExecute(step) || vm.AddElseCommand.CanExecute(step))
            {
                menu.Items.Add(new Separator());
                AddMenuItem(menu, Loc.Get("Ui.Job.Steps.AddElseIf"), vm.AddElseIfCommand, step);
                AddMenuItem(menu, Loc.Get("Ui.Job.Steps.AddElse"), vm.AddElseCommand, step);
            }
            menu.Items.Add(new Separator());
            AddMenuItem(menu, Loc.Get("Ui.Job.Steps.DeleteStep"), vm.DeleteStepCommand, step, Loc.Get("Shortcut.Delete"));
            menu.IsOpen = true;
            e.Handled = true;
        }

        private static void AddMenuItem(
            ItemsControl menu,
            string header,
            ICommand command,
            object? parameter = null,
            string? inputGestureText = null)
        {
            menu.Items.Add(new MenuItem
            {
                Header = header,
                Command = command,
                CommandParameter = parameter,
                InputGestureText = inputGestureText
            });
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

        private void EndSettingsButton_Click(object sender, RoutedEventArgs e)
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
                || sender is not Expander { Tag: System.Collections.IList target } expander
                || !StepDragDrop.TryGetPayload(e.Data, out _))
                return;

            var list = AllStepLists().FirstOrDefault(candidate => ReferenceEquals(candidate.ItemsSource, target));
            if (list == null)
                return;

            if (IsPointerOverHeader(expander, e))
                StepDragDrop.ShowSectionStartTarget(list);
            else
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
                || sender is not Expander { Tag: System.Collections.IList target } expander
                || !StepDragDrop.TryGetPayload(e.Data, out var payload)
                || DataContext is not JobStepsViewModel vm)
                return;

            var request = new StepDragDrop.MoveRequest(
                payload.Source,
                payload.SourceIndex,
                target,
                IsPointerOverHeader(expander, e) ? 0 : target.Count);
            if (vm.ReorderStepCommand.CanExecute(request))
                vm.ReorderStepCommand.Execute(request);
            StepDragDrop.ClearTargetPreview();
            e.Handled = true;
        }

        private static bool IsPointerOverHeader(Expander expander, DragEventArgs e)
        {
            if (expander.Header is not FrameworkElement header)
                return false;

            var point = e.GetPosition(header);
            return point.X >= 0 && point.X <= header.ActualWidth
                && point.Y >= 0 && point.Y <= header.ActualHeight;
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
