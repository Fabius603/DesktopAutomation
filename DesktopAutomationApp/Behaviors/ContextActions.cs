using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DesktopAutomationApp.Controls;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Behaviors;

/// <summary>Explicit, opt-in action profiles. Text editing keeps its native menu.</summary>
public static class ContextActions
{
    public static readonly DependencyProperty IsMenuButtonProperty = DependencyProperty.RegisterAttached(
        "IsMenuButton", typeof(bool), typeof(ContextActions), new PropertyMetadata(false, MenuButtonChanged));
    public static void SetIsMenuButton(DependencyObject target, bool value) => target.SetValue(IsMenuButtonProperty, value);
    public static bool GetIsMenuButton(DependencyObject target) => (bool)target.GetValue(IsMenuButtonProperty);
    private static void MenuButtonChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not Button button) return;
        button.Click -= MenuButtonClick;
        if (args.NewValue is true) button.Click += MenuButtonClick;
    }
    private static void MenuButtonClick(object sender, RoutedEventArgs args)
    {
        for (DependencyObject? element = (DependencyObject)sender; element is not null; element = Parent(element))
            if (element is FrameworkElement host && GetProfile(host) is not null)
            {
                Open(host, (DependencyObject)sender);
                args.Handled = true;
                return;
            }
    }
    public static readonly DependencyProperty ProfileProperty = DependencyProperty.RegisterAttached(
        "Profile", typeof(string), typeof(ContextActions), new PropertyMetadata(null, Changed));
    public static void SetProfile(DependencyObject target, string value) => target.SetValue(ProfileProperty, value);
    public static string? GetProfile(DependencyObject target) => (string?)target.GetValue(ProfileProperty);

    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not FrameworkElement element) return;
        element.PreviewMouseRightButtonDown -= RightClick;
        element.PreviewKeyDown -= KeyDown;
        if (args.NewValue is not null)
        {
            element.PreviewMouseRightButtonDown += RightClick;
            element.PreviewKeyDown += KeyDown;
        }
    }

    private static void RightClick(object sender, MouseButtonEventArgs args)
    {
        if (sender is not FrameworkElement element || IsTextInput(args.OriginalSource as DependencyObject)) return;
        if (NearestProfile(args.OriginalSource as DependencyObject) is { } nested && !ReferenceEquals(nested, element)) return;
        Open(element, args.OriginalSource as DependencyObject);
        args.Handled = true;
    }

    private static void KeyDown(object sender, KeyEventArgs args)
    {
        if (sender is not FrameworkElement element || IsTextInput(args.OriginalSource as DependencyObject)) return;
        if (NearestProfile(args.OriginalSource as DependencyObject) is { } nested && !ReferenceEquals(nested, element)) return;
        if (args.Key != Key.Apps && !(args.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift)) return;
        Open(element, Keyboard.FocusedElement as DependencyObject);
        args.Handled = true;
    }

    public static void Open(FrameworkElement element, DependencyObject? source = null)
    {
        FrameworkElement target = element;
        object? row = element.DataContext;
        if (element is ListBox list)
        {
            var item = source is null ? null : ItemsControl.ContainerFromElement(list, source) as ListBoxItem;
            if (source is null || source == list) item ??= list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem) as ListBoxItem;
            if (item is null) return;
            if (item is not null) { ActionMenus.SelectContextItem(list, item); target = item; row = item.DataContext; }
        }
        else if (element is DataGrid grid)
        {
            var item = source is null ? null : ItemsControl.ContainerFromElement(grid, source) as DataGridRow;
            if (item is null && grid.SelectedItem is not null) item = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
            if (item is null) return;
            if (!item.IsSelected) { grid.UnselectAll(); item.IsSelected = true; }
            target = item; row = item.Item;
        }
        else if (element is ItemsControl items && source is not null && ItemsControl.ContainerFromElement(items, source) is FrameworkElement container)
        {
            target = container;
            row = container.DataContext;
        }
        var profile = GetProfile(element) ?? "";
        var selected = element is DataGrid selectedGrid ? selectedGrid.SelectedItems.Cast<object>().ToArray() : element is ListBox selectedList ? selectedList.SelectedItems.Cast<object>().ToArray() : row is null ? [] : new[] { row };
        var menu = element is ListBox editorList && profile is "Answers" or "Points" or "Expressions" or "Conditions" or "OverlayRows"
            ? EditorCollectionMenus.Create(editorList, target, profile, selected)
            : Create(profile, target, element.DataContext, row, selected);
        if (menu.Items.Count > 0) { target.ContextMenu = menu; menu.IsOpen = true; }
    }

    public static ContextMenu Create(string profile, FrameworkElement target, object? owner, object? row, IReadOnlyList<object>? selected = null)
    {
        var menu = ActionMenus.Create(target);
        if (owner is null) return menu;
        if (owner is JobStepsViewModel job && profile == "Variables")
        {
            var rows = (selected ?? (row is null ? [] : new[] { row })).OfType<JobVariableEditorViewModel>().ToArray();
            if (rows.Length == 0) return menu;
            ActionMenus.Add(menu, "Ui.Context.CopyName", new RelayCommand(() => Clipboard.SetText(string.Join(Environment.NewLine, rows.Select(item => item.Name)))), count: rows.Length);
            ActionMenus.Add(menu, "Ui.Job.Variables.Duplicate", new RelayCommand(() => job.DuplicateVariableSelection(rows), () => rows.All(item => job.DuplicateVariableCommand.CanExecute(item))), count: rows.Length);
            ActionMenus.Add(menu, "Ui.Job.Variables.Draft.ApplySelected", new RelayCommand(() => job.ApplyVariableSelection(rows), () => rows.Any(item => item.IsDirty)), count: rows.Length);
            menu.Items.Add(new Separator());
            ActionMenus.Add(menu, "Ui.Job.Variables.Delete.Action", new AsyncRelayCommand(() => job.DeleteVariableSelectionAsync(rows), () => !job.IsDebugActive && !job.IsMutationBusy), count: rows.Length);
            return menu;
        }
        if (owner is YoloDownloadsViewModel models && profile == "Models")
        {
            var rows = (selected ?? (row is null ? [] : new[] { row })).OfType<YoloModelEntry>().ToArray();
            var downloads = rows.Where(item => models.DownloadCommand.CanExecute(item)).ToArray();
            var installed = rows.Where(item => models.UninstallCommand.CanExecute(item)).ToArray();
            if (downloads.Length > 0) ActionMenus.Add(menu, "Ui.Yolo.Download", new AsyncRelayCommand(() => models.DownloadSelectionAsync(downloads)), count: downloads.Length);
            ActionMenus.Add(menu, "Ui.Context.CopyIdentifier", new RelayCommand(() => Clipboard.SetText(string.Join(Environment.NewLine, rows.Select(item => item.ModelKey)))), count: rows.Length);
            ActionMenus.Add(menu, "Ui.Common.OpenFolderInExplorer", models.OpenFolderCommand);
            if (installed.Length > 0) ActionMenus.Add(menu, "Ui.Yolo.Uninstall", new AsyncRelayCommand(() => models.UninstallSelectionAsync(installed)), count: installed.Length);
            return menu;
        }
        if (owner is StartViewModel dashboard)
        {
            var rows = selected ?? (row is null ? [] : new[] { row });
            if (profile == "Running")
            {
                if (rows.Count == 1)
                {
                    ActionMenus.Add(menu, "Ui.Context.OpenDefinition", dashboard.OpenDefinitionCommand, row);
                    ActionMenus.Add(menu, "Ui.Context.OpenLogs", dashboard.OpenLogsCommand, row);
                }
                var stoppable = rows.Where(item => dashboard.CancelItemCommand.CanExecute(item)).ToArray();
                if (stoppable.Length > 0) ActionMenus.Add(menu, "Ui.Context.StopInstances", new RelayCommand(() => { foreach (var item in stoppable) dashboard.CancelItemCommand.Execute(item); }), count: stoppable.Length);
                return menu;
            }
            if (profile == "DashboardAutomations")
            {
                if (rows.Count == 1) ActionMenus.Add(menu, "Ui.Common.Open", dashboard.OpenAutomationCommand, row);
                ActionMenus.Add(menu, "Ui.Context.Disable", new AsyncRelayCommand(async () => { foreach (var item in rows.OfType<AutomationDashboardInfo>()) await dashboard.DisableAutomationAsync(item); }), count: rows.Count);
                return menu;
            }
        }
        if (owner is LogsHomeViewModel logs && profile is "LogRuns" or "LogSteps" or "LogEvents" or "LogTriggers")
        {
            var rows = selected ?? (row is null ? [] : new[] { row });
            var count = rows.Count;
            if (count == 1)
            {
                if (row is LogRunRow) ActionMenus.Add(menu, "Ui.Common.Open", logs.OpenRunCommand, row);
                if (row is LogStepRow step)
                {
                    ActionMenus.Add(menu, "Ui.Context.OpenStep", new AsyncRelayCommand(() => logs.OpenContextStepAsync(step)));
                    ActionMenus.Add(menu, "Logs.Ui.OpenCause", new RelayCommand(() => logs.OpenContextCause(step), () => step.Display.Execution.Cause is not null));
                }
                if (row is LogEventRow or LogAutomationRow) ActionMenus.Add(menu, "Ui.Context.OpenRelatedRun", new AsyncRelayCommand(() => logs.OpenContextRunAsync(row)));
            }
            ActionMenus.Add(menu, "Ui.Common.Copy", new RelayCommand(() => logs.CopySelection(rows)), count: count);
            ActionMenus.Add(menu, "Logs.Ui.Export", new AsyncRelayCommand(() => logs.ExportSelectionAsync(rows), () => !logs.IsExporting), count: count);
            menu.Items.Add(new Separator());
            ActionMenus.Add(menu, "Logs.Ui.MarkSeen", new AsyncRelayCommand(() => logs.ChangeSelectionAttentionAsync(rows, DesktopAutomation.Application.Logging.LogAttentionState.Seen)), count: count);
            ActionMenus.Add(menu, "Logs.Ui.ResolveProblem", new AsyncRelayCommand(() => logs.ChangeSelectionAttentionAsync(rows, DesktopAutomation.Application.Logging.LogAttentionState.Resolved)), count: count);
            ActionMenus.Add(menu, "Logs.Ui.ReopenProblem", new AsyncRelayCommand(() => logs.ChangeSelectionAttentionAsync(rows, DesktopAutomation.Application.Logging.LogAttentionState.New)), count: count);
            return menu;
        }

        void Add(string key, string command, bool parameter = false)
            => ActionMenus.Bind(menu, owner, key, command, parameter ? row : null);
        switch (profile)
        {
            case "Credentials":
                Add("Ui.Common.Edit", "EditCommand"); Add("Settings.Credentials.Replace", "ReplaceCommand");
                if (row is not null) Add("Ui.Context.CopyName", "CopyNameCommand");
                menu.Items.Add(new Separator()); Add("Settings.Credentials.Delete", "DeleteCommand"); break;
            case "VariableUsages":
                Add("Ui.Job.Variables.Usage.Open", "OpenVariableUsageCommand", true);
                Add("Ui.Job.Variables.Usage.OnlyThis", "ApplyVariableToSelectedUsageCommand", true); break;
            case "LogGroups":
                Add("Ui.Common.Open", "OpenGroupCommand", true); break;
            case "LogDetails":
                Add("Ui.Common.Copy", "CopyCommand"); Add("Logs.Ui.Export", "ExportCommand");
                Add("Ui.Context.OpenDefinition", "OpenOriginCommand");
                Add("Logs.Ui.ResolveProblem", "ResolveProblemCommand"); Add("Logs.Ui.ReopenProblem", "ReopenProblemCommand"); break;
            case "LogLinks":
                Add("Ui.Common.Open", "OpenLinkCommand", true); break;
            case "LogPaths":
                Add("Ui.Common.Open", "CheckPathCommand", true); break;
            case "Header":
                // The title's More button and the title surface share one menu instance.
                var button = Descendants<Button>(target).FirstOrDefault(candidate => candidate.ContextMenu is not null);
                if (button?.ContextMenu is { } existing) { existing.PlacementTarget = button; return existing; }
                break;
        }
        return menu;
    }

    private static bool IsTextInput(DependencyObject? element)
    {
        for (; element is not null; element = Parent(element))
            if (element is TextBoxBase or PasswordBox or ComboBox) return true;
        return false;
    }
    private static FrameworkElement? NearestProfile(DependencyObject? element)
    {
        for (; element is not null; element = Parent(element))
            if (element is FrameworkElement host && GetProfile(host) is not null) return host;
        return null;
    }
    private static DependencyObject? Parent(DependencyObject element)
        => element is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
    private static IEnumerable<T> Descendants<T>(DependencyObject element) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
