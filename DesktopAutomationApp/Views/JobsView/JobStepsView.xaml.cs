using DesktopAutomationApp.Controls;
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
using System.Windows.Media.Animation;
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
        private GridLength _expandedInspectorWidth = new(1, GridUnitType.Star);
        private bool _inspectorCollapsed;

        public JobStepsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            PreviewKeyDown += OnPreviewKeyDown;
            SizeChanged += (_, _) => _vm?.InspectorPane.UpdateWidth(ActualWidth);
            Loaded += (_, _) =>
            {
                if (_vm is null) return;
                _vm.InspectorPane.PropertyChanged -= OnInspectorStateChanged;
                _vm.InspectorPane.PropertyChanged += OnInspectorStateChanged;
                _vm.InspectorPane.UpdateWidth(ActualWidth);
                ApplyInspectorLayout();
            };
            Unloaded += (_, _) =>
            {
                if (_vm is not null) _vm.InspectorPane.PropertyChanged -= OnInspectorStateChanged;
            };
        }

        private void SelectedJobStepTabs_Loaded(object sender, RoutedEventArgs e) =>
            UpdateInspectorPageSelection(animate: false);

        private void SelectedJobStepTabs_SizeChanged(object sender, SizeChangedEventArgs e) =>
            UpdateInspectorPageSelection(animate: false);

        private void SelectedJobStepTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, sender)) return;
            UpdateInspectorPageSelection(animate: true);
        }

        private void UpdateInspectorPageSelection(bool animate)
        {
            if (!SelectedJobStepTabs.IsLoaded || SelectedJobStepTabs.Items.Count == 0) return;
            if (SelectedJobStepTabs.Template.FindName("PART_InspectorHeaderTrack", SelectedJobStepTabs) is not FrameworkElement track
                || SelectedJobStepTabs.Template.FindName("PART_InspectorSelectionPill", SelectedJobStepTabs) is not Border pill
                || track.ActualWidth <= 0) return;

            var segmentWidth = track.ActualWidth / SelectedJobStepTabs.Items.Count;
            var target = Math.Max(0, SelectedJobStepTabs.SelectedIndex) * segmentWidth;
            var transform = EnsureMutableTranslateTransform(pill.RenderTransform);
            if (!ReferenceEquals(pill.RenderTransform, transform)) pill.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, null);
            if (!animate)
            {
                transform.X = target;
                return;
            }

            var current = transform.X;
            transform.X = target;
            transform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation(current, target, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop
                });
        }

        internal static TranslateTransform EnsureMutableTranslateTransform(Transform? transform)
        {
            if (transform is not TranslateTransform translateTransform) return new TranslateTransform();
            return translateTransform.IsFrozen
                ? translateTransform.CloneCurrentValue()
                : translateTransform;
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

            var focusedList = AllStepLists().First(list => list.IsKeyboardFocusWithin);
            if ((e.Key == Key.Apps || e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift)
                && focusedList.SelectedItem is JobStep selected
                && focusedList.ItemContainerGenerator.ContainerFromItem(selected) is ListBoxItem item)
            {
                ShowStepContextMenu(focusedList, item, selected, _vm);
                e.Handled = true;
                return;
            }
            if (ViewShortcutRouter.IsTextInputFocused || Keyboard.FocusedElement is ButtonBase or ComboBox) return;
            if (AppShortcutGestures.Matches(e, AppShortcutGestures.SelectAll))
            {
                focusedList.SelectAll();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
            {
                focusedList.UnselectAll();
                e.Handled = true;
                return;
            }

            if (AppShortcutGestures.Matches(e, AppShortcutGestures.AddStep)) Execute(_vm.AddStepCommand, null, e);
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
            if (_vm != null)
            {
                _vm.PropertyChanged -= OnVmPropertyChanged;
                _vm.InspectorPane.PropertyChanged -= OnInspectorStateChanged;
            }
            _vm = e.NewValue as JobStepsViewModel;
            if (_vm != null)
            {
                _vm.PropertyChanged += OnVmPropertyChanged;
                _vm.InspectorPane.PropertyChanged += OnInspectorStateChanged;
                _vm.InspectorPane.UpdateWidth(ActualWidth);
            }
            ApplyInspectorLayout();
        }

        private void OnInspectorStateChanged(object? sender, PropertyChangedEventArgs e) => ApplyInspectorLayout();

        private void ApplyInspectorLayout()
        {
            var collapsed = _vm?.InspectorPane.IsCollapsed == true;
            if (collapsed == _inspectorCollapsed) return;
            if (collapsed) _expandedInspectorWidth = DebugInspectorColumn.Width;
            _inspectorCollapsed = collapsed;
            DebugInspectorColumn.MinWidth = collapsed ? CollapsiblePaneState.InspectorPeekWidth : 360;
            DebugInspectorColumn.Width = collapsed ? new GridLength(CollapsiblePaneState.InspectorPeekWidth) : _expandedInspectorWidth;
            // Keep keyboard navigation on the visible opening/closing control.
            if (InspectorSurface.IsKeyboardFocusWithin || ExpandInspectorButton.IsKeyboardFocusWithin)
                Dispatcher.BeginInvoke(() => (collapsed ? ExpandInspectorButton : CollapseInspectorButton).Focus());
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(JobStepsViewModel.SelectedStep)) return;

            // If the item is already among the selected ones, leave multi-selection intact.
            if (_vm!.SelectedStep != null && AllStepLists().FirstOrDefault(list => list.SelectedItems.Contains(_vm.SelectedStep)) is { } selectedList)
            {
                var selected = _vm.SelectedStep;
                selectedList.Dispatcher.BeginInvoke(() => selectedList.ScrollIntoView(selected));
                return;
            }

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
                || e.OriginalSource is not DependencyObject source
                || ItemsControl.ContainerFromElement(list, source) is not ListBoxItem item
                || item.DataContext is not JobStep step)
                return;

            var isInteractiveControl = FindVisualAncestor<ButtonBase>(source, item) is not null;
            if (!ShouldFocusStepInspector(isInteractiveControl, step is not EndIfStep)) return;
            SelectedJobStepTabs.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            e.Handled = true;
        }

        internal static bool ShouldFocusStepInspector(bool isInteractiveControl, bool hasDetails) =>
            !isInteractiveControl && hasDetails;

        private void StepsList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox list
                || ItemsControl.ContainerFromElement(
                    list, e.OriginalSource as DependencyObject) is not ListBoxItem item
                || item.DataContext is not JobStep step
                || DataContext is not JobStepsViewModel vm)
                return;

            ShowStepContextMenu(list, item, step, vm);
            e.Handled = true;
        }

        private static void ShowStepContextMenu(ListBox list, ListBoxItem item, JobStep step, JobStepsViewModel vm)
        {
            if (!item.IsSelected)
            {
                list.SelectedItems.Clear();
                item.IsSelected = true;
            }

            CreateStepContextMenu(item, step, vm).IsOpen = true;
        }

        internal static ContextMenu CreateStepContextMenu(FrameworkElement target, JobStep step, JobStepsViewModel vm)
        {
            var multiple = vm.SelectedSteps.Count > 1 && vm.SelectedSteps.Contains(step);
            var count = multiple ? vm.SelectedSteps.Count : 1;
            var captured = multiple ? vm.SelectedSteps.ToArray() : [step];
            ICommand SelectionCommand(ICommand command) => new RelayCommand(() =>
            {
                vm.SetSelectedSteps(captured);
                command.Execute(null);
            }, () => command.CanExecute(null));

            var menu = ActionMenus.Create(target);
            if (!multiple)
            {
                ActionMenus.Add(menu, "Ui.Common.EditStep", vm.EditStepCommand, step);
                ActionMenus.Add(menu, "Ui.Context.Debug", SelectionCommand(vm.DebugStepCommand));
                if (step is TaskAutomation.Jobs.IfStep or TaskAutomation.Jobs.ElseIfStep or TaskAutomation.Jobs.ElseStep)
                    ActionMenus.Add(menu, "Ui.Context.ExpandCollapse", vm.ToggleBlockCommand, step);
            }
            ActionMenus.Add(menu, "Ui.Context.Paste", SelectionCommand(vm.PasteCommand), gesture: "Shortcut.CtrlV");
            var enabledTargets = multiple ? vm.SelectedSteps.Where(selected => selected.CanBeDisabled).ToArray() : step.CanBeDisabled ? [step] : Array.Empty<JobStep>();
            if (enabledTargets.Length > 0)
            {
                ActionMenus.Add(menu, "Ui.Context.Enable", new AsyncRelayCommand(() => vm.SetSelectionEnabledAsync(step, true), () => !vm.IsDebugActive && !vm.IsMutationBusy), count: enabledTargets.Length);
                ActionMenus.Add(menu, "Ui.Context.Disable", new AsyncRelayCommand(() => vm.SetSelectionEnabledAsync(step, false), () => !vm.IsDebugActive && !vm.IsMutationBusy), count: enabledTargets.Length);
            }
            ActionMenus.Add(menu, "Ui.Context.SetBreakpoints", new AsyncRelayCommand(() => vm.SetSelectionBreakpointsAsync(step, true), () => !vm.IsMutationBusy), count: count);
            ActionMenus.Add(menu, "Ui.Context.RemoveBreakpoints", new AsyncRelayCommand(() => vm.SetSelectionBreakpointsAsync(step, false), () => !vm.IsMutationBusy), count: count);
            menu.Items.Add(new Separator());
            AddMenuItem(menu, multiple ? Loc.Format("Ui.Common.CopySelected", count) : Loc.Get("Ui.Common.Copy"),
                "Ui.Common.Copy", SelectionCommand(vm.CopyCommand), null, Loc.Get("Shortcut.CtrlC"));
            AddMenuItem(menu, multiple ? Loc.Format("Ui.Common.DuplicateSelected", count) : Loc.Get("Ui.Macro.Steps.DuplicateStep"),
                "Ui.Macro.Steps.DuplicateStep", SelectionCommand(vm.DuplicateStepCommand), null, Loc.Get("Shortcut.CtrlD"));
            AddMenuItem(menu, Loc.Get("Ui.Common.MoveStepUp"), "Ui.Common.MoveStepUp", vm.MoveStepUpCommand, step, Loc.Get("Shortcut.AltUp"));
            AddMenuItem(menu, Loc.Get("Ui.Common.MoveStepDown"), "Ui.Common.MoveStepDown", vm.MoveStepDownCommand, step, Loc.Get("Shortcut.AltDown"));
            menu.Items.Add(new Separator());
            AddMenuItem(menu, Loc.Get("Ui.Job.Steps.MoveToStart"), "Ui.Job.Steps.MoveToStart", vm.MoveToStartSectionCommand, step);
            AddMenuItem(menu, Loc.Get("Ui.Job.Steps.MoveToRun"), "Ui.Job.Steps.MoveToRun", vm.MoveToRunSectionCommand, step);
            AddMenuItem(menu, Loc.Get("Ui.Job.Steps.MoveToEnd"), "Ui.Job.Steps.MoveToEnd", vm.MoveToEndSectionCommand, step);
            if (vm.AddElseIfCommand.CanExecute(step) || vm.AddElseCommand.CanExecute(step))
            {
                menu.Items.Add(new Separator());
                AddMenuItem(menu, Loc.Get("Ui.Job.Steps.AddElseIf"), "Ui.Job.Steps.AddElseIf", vm.AddElseIfCommand, step);
                AddMenuItem(menu, Loc.Get("Ui.Job.Steps.AddElse"), "Ui.Job.Steps.AddElse", vm.AddElseCommand, step);
            }
            if (vm.RemoveConditionCommand.CanExecute(step))
                AddMenuItem(menu, Loc.Get("Ui.Job.Steps.RemoveCondition"), "Ui.Job.Steps.RemoveCondition", vm.RemoveConditionCommand, step);
            menu.Items.Add(new Separator());
            AddMenuItem(menu, multiple
                    ? Loc.Format("Ui.Common.DeleteSelected", count)
                    : Loc.Get("Ui.Job.Steps.DeleteStep"),
                "Ui.Job.Steps.DeleteStep",
                multiple ? SelectionCommand(vm.DeleteSelectedCommand) : vm.DeleteStepCommand,
                multiple ? null : step,
                Loc.Get("Shortcut.Delete"));
            return menu;
        }

        private static void AddMenuItem(
            ItemsControl menu,
            string header,
            string identity,
            ICommand command,
            object? parameter = null,
            string? inputGestureText = null)
        {
            var item = new MenuItem
            {
                Header = header,
                Command = command,
                CommandParameter = parameter,
                InputGestureText = inputGestureText
            };
            ActionMenus.Decorate(item, identity);
            menu.Items.Add(item);
        }

        private IEnumerable<ListBox> AllStepLists()
        {
            yield return StartStepsList;
            yield return StepsList;
            yield return EndStepsList;
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

            var valid = IsPointerOverHeader(expander, e)
                ? StepDragDrop.ShowSectionStartTarget(list, e.GetPosition(list))
                : StepDragDrop.ShowSectionTarget(list, e.GetPosition(list));
            e.Effects = valid ? DragDropEffects.Move : DragDropEffects.None;
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
            if (list is not null && StepDragDrop.KeepStationaryPreview(list, e.GetPosition(list))) return;
            StepDragDrop.ClearTargetPreview(list);
        }

        private void StepSection_Drop(object sender, DragEventArgs e)
        {
            if (e.Handled
                || sender is not Expander { Tag: System.Collections.IList target } expander
                || !StepDragDrop.TryGetPayload(e.Data, out var payload)
                || DataContext is not JobStepsViewModel vm)
                return;

            var list = AllStepLists().FirstOrDefault(candidate => ReferenceEquals(candidate.ItemsSource, target));
            if (list is not null && StepDragDrop.DropRetainedTarget(list, e.GetPosition(list), e.Data) is { } retainedEffect)
            {
                e.Effects = retainedEffect;
                e.Handled = true;
                return;
            }
            var request = new StepDragDrop.MoveRequest(
                payload.Source,
                payload.SourceIndex,
                target,
                IsPointerOverHeader(expander, e) ? 0 : target.Count,
                SourceIndices: payload.SourceIndices);
            StepDragDrop.ClearTargetPreview();
            if (vm.PreviewMoveValidator(request) && vm.ReorderStepCommand.CanExecute(request))
                vm.ReorderStepCommand.Execute(request);
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

        private static T? FindVisualAncestor<T>(DependencyObject? child, DependencyObject stopAt)
            where T : DependencyObject
        {
            var current = child;
            while (current is not null && !ReferenceEquals(current, stopAt))
            {
                if (current is T match) return match;
                current = current switch
                {
                    Visual or System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(current),
                    FrameworkContentElement contentElement => contentElement.Parent,
                    _ => LogicalTreeHelper.GetParent(current)
                };
            }
            return null;
        }

    }
}
