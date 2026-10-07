using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DesktopAutomationApp.Localization;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DesktopAutomationApp.Controls;

/// <summary>Presentation-only menu projection. Commands remain owned by their use case.</summary>
public static class ActionMenus
{
    private const double IconSize = 16;
    private static readonly IReadOnlyDictionary<string, string> IconKinds = CreateIconKinds();

    private static IReadOnlyDictionary<string, string> CreateIconKinds()
    {
        var kinds = new Dictionary<string, string>(StringComparer.Ordinal);
        void Map(string kind, params string[] keys) { foreach (var key in keys) kinds.Add(key, kind); }
        Map("Edit", "Ui.Common.Edit", "Ui.Common.EditStep", "Ui.Context.EditStep", "Ui.Common.Rename", "Ui.Library.RenameFolder", "Ui.Macro.Group.Rename");
        Map("Copy", "Ui.Common.Copy", "Ui.Context.CopyName", "Ui.Context.CopyIdentifier", "Ui.Context.CopyPath", "Ui.Context.CopyImage");
        Map("Duplicate", "Ui.Context.Duplicate", "Ui.Macro.Steps.DuplicateStep", "Ui.Job.Variables.Duplicate");
        Map("Paste", "Ui.Context.Paste");
        Map("Delete", "Ui.Context.Remove", "Ui.Library.Delete", "Ui.Job.Steps.DeleteStep", "Ui.Macro.Steps.DeleteStep", "Ui.Job.Variables.Delete.Action", "Settings.Credentials.Delete", "Ui.Yolo.Uninstall");
        Map("Up", "Ui.Common.MoveStepUp", "Ui.Context.MoveUp", "Ui.Library.MoveUpOneLevel");
        Map("Down", "Ui.Common.MoveStepDown", "Ui.Context.MoveDown");
        Map("Move", "Ui.Context.MoveTo", "MoveTo", "Ui.Job.Steps.MoveToStart", "Ui.Job.Steps.MoveToRun", "Ui.Job.Steps.MoveToEnd");
        Map("Folder", "Ui.Context.LibraryRoot", "Ui.Common.OpenFolderInExplorer");
        Map("New", "Create", "Ui.Library.NewFolder", "Ui.Job.List.NewJob", "Ui.Macro.List.NewMacro", "Ui.Automation.List.NewAutomation");
        Map("Run", "Ui.Context.Run", "Ui.Context.Enable", "Tray.ResumeAutomations");
        Map("Pause", "Ui.Context.Disable", "Tray.PauseAutomations");
        Map("Stop", "Ui.Common.Stop", "Ui.Context.StopInstances", "Tray.StopAllJobs", "Tray.StopAllMakros");
        Map("Group", "Ui.Context.Group", "Ui.Macro.Group.Create");
        Map("Ungroup", "Ui.Macro.Group.Dissolve", "Ui.Macro.Group.RemoveSelected", "Ui.Job.Steps.RemoveCondition");
        Map("Expand", "Ui.Context.ExpandCollapse", "Ui.Macro.Group.Toggle");
        Map("Breakpoint", "Ui.Context.SetBreakpoints");
        Map("RemoveBreakpoint", "Ui.Context.RemoveBreakpoints");
        Map("Debug", "Ui.Context.Debug");
        Map("Save", "Ui.Common.Save", "Ui.Context.SaveImage");
        Map("Undo", "Ui.Common.Discard", "Logs.Ui.ReopenProblem");
        Map("Check", "Ui.Job.Variables.Draft.ApplySelected", "Ui.Job.Variables.Usage.OnlyThis", "Logs.Ui.ResolveProblem");
        Map("Download", "Ui.Yolo.Download");
        Map("Export", "Logs.Ui.Export");
        Map("Seen", "Logs.Ui.MarkSeen");
        Map("Replace", "Settings.Credentials.Replace");
        Map("Open", "Ui.Common.Open", "Ui.Context.OpenDefinition", "Ui.Context.OpenStep", "Ui.Context.OpenRelatedRun", "Ui.Job.Variables.Usage.Open", "Ui.Logs.OpenFile", "Tray.Open");
        Map("Logs", "Ui.Context.OpenLogs");
        Map("Cause", "Logs.Ui.OpenCause");
        Map("Branch", "Ui.Job.Steps.AddElseIf", "Ui.Job.Steps.AddElse");
        Map("Direct", "Ui.Job.StepInput.Source.Direct", "Ui.Job.StepInput.Source.IndividualValues");
        Map("Variable", "Ui.Job.StepInput.Source.JobVariable");
        Map("Result", "Ui.Job.StepInput.Source.StepResult");
        Map("Secret", "Ui.Job.StepInput.Source.Secret");
        Map("Exit", "Tray.Exit");
        return kinds;
    }
    private static string LabelKey(string key) => key switch
    {
        "Ui.Common.EditStep" => "Ui.Context.EditStep",
        "Ui.Macro.Steps.DuplicateStep" => "Ui.Context.Duplicate",
        "Ui.Common.MoveStepUp" => "Ui.Context.MoveUp",
        "Ui.Common.MoveStepDown" => "Ui.Context.MoveDown",
        "Ui.Job.Steps.DeleteStep" or "Ui.Macro.Steps.DeleteStep" => "Ui.Context.DeleteStep",
        _ => key
    };
    public static ContextMenu Create(FrameworkElement target)
    {
        var menu = new ContextMenu { PlacementTarget = target, DataContext = target.DataContext };
        menu.SetResourceReference(FrameworkElement.StyleProperty, "App.ContextMenuStyle");
        return menu;
    }

    public static MenuItem Add(ItemsControl menu, string key, ICommand command,
        object? parameter = null, string? gesture = null, int count = 1)
    {
        var item = new MenuItem
        {
            Header = count > 1 ? Loc.Format("Ui.Context.SelectedAction", Loc.Get(LabelKey(key)), count) : Loc.Get(LabelKey(key)),
            Command = command,
            CommandParameter = parameter,
            InputGestureText = gesture is null ? null : Loc.Get(gesture)
        };
        Decorate(item, key);
        menu.Items.Add(item);
        return item;
    }

    public static MenuItem Bind(ItemsControl menu, object owner, string key, string command,
        object? parameter = null)
    {
        var item = new MenuItem { Header = Loc.Get(LabelKey(key)), CommandParameter = parameter };
        item.SetBinding(MenuItem.CommandProperty, new Binding(command) { Source = owner });
        if (owner.GetType().GetProperty(command)?.GetValue(owner) is not ICommand) item.IsEnabled = false;
        Decorate(item, key);
        menu.Items.Add(item);
        return item;
    }

    public static void Decorate(MenuItem item, string identity)
    {
        item.SetResourceReference(FrameworkElement.StyleProperty, "App.MenuItemStyle");
        if (!IconKinds.ContainsKey(identity))
        {
            var label = item.Header?.ToString() ?? identity;
            foreach (var key in IconKinds.Keys)
                if (label.Equals(Loc.Get(key), StringComparison.CurrentCulture)
                    || label.Equals(Loc.Get(LabelKey(key)), StringComparison.CurrentCulture))
                {
                    identity = key;
                    item.Header = label.Replace(Loc.Get(key), Loc.Get(LabelKey(key)), StringComparison.CurrentCultureIgnoreCase);
                    break;
                }
        }
        var kind = IconKinds.GetValueOrDefault(identity, "More");
        if (LabelKey(identity) != identity && item.Header is string header
            && BindingOperations.GetBinding(item, MenuItem.HeaderProperty) is null)
            item.Header = header.Replace(Loc.Get(identity), Loc.Get(LabelKey(identity)), StringComparison.CurrentCulture);
        var danger = kind == "Delete";
        // Rounded, unfilled menu glyphs match the reference's line icons.
        var geometry = kind switch
        {
            "Delete" => "M3,6 H21 M9,6 V3 H15 V6 M5,6 L6,21 H18 L19,6 M10,10 V17 M14,10 V17",
            "Copy" => "M8,8 H20 V21 H8 Z M16,8 V3 H3 V16 H8",
            "Duplicate" => "M8,8 H20 V21 H8 Z M16,8 V3 H3 V16 H8 M11,14 H17 M14,11 V17",
            "Paste" => "M8,5 H4 V21 H20 V5 H16 M8,3 H16 V7 H8 Z",
            "Group" => "M2,2 H22 V22 H2 Z M6,6 H11 V11 H6 Z M13,13 H18 V18 H13 Z",
            "Ungroup" => "M2,8 V2 H8 M16,2 H22 V8 M22,16 V22 H16 M8,22 H2 V16 M6,6 H11 V11 H6 Z M13,13 H18 V18 H13 Z",
            "Up" => "M12,21 V3 M5,10 L12,3 L19,10",
            "Down" => "M12,3 V21 M5,14 L12,21 L19,14",
            "Move" => "M3,12 H21 M15,6 L21,12 L15,18",
            "Stop" => "M5,5 H19 V19 H5 Z",
            "Run" => "M6,3 L21,12 L6,21 Z",
            "Pause" => "M5,3 H9 V21 H5 Z M15,3 H19 V21 H15 Z",
            "Edit" => "M3,21 L4,15 L17,2 L22,7 L9,20 Z M14,5 L19,10",
            "New" => "M12,3 V21 M3,12 H21",
            "Download" => "M12,3 V16 M6,10 L12,16 L18,10 M3,17 V21 H21 V17",
            "Export" => "M12,16 V3 M6,9 L12,3 L18,9 M3,17 V21 H21 V17",
            "Folder" => "M2,7 V21 H22 V7 Z M2,7 V3 H9 L12,7",
            "Breakpoint" => "M12,3 A9,9 0 1 1 12,21 A9,9 0 1 1 12,3",
            "RemoveBreakpoint" => "M12,3 A9,9 0 1 1 12,21 A9,9 0 1 1 12,3 M6,18 L18,6",
            "Debug" => "M7,8 H17 V15 A5,5 0 0 1 7,15 Z M9,8 V5 H15 V8 M3,9 H7 M17,9 H21 M3,14 H7 M17,14 H21 M5,21 L8,18 M16,18 L19,21 M9,5 L7,2 M15,5 L17,2",
            "Save" => "M3,3 H18 L21,6 V21 H3 Z M7,3 V9 H16 V3 M7,21 V14 H17 V21",
            "Undo" => "M9,4 L3,10 L9,16 M3,10 H15 A6,6 0 0 1 15,22",
            "Check" => "M4,12 L9,17 L21,5",
            "Seen" => "M2,12 Q12,-2 22,12 Q12,26 2,12 M12,8 A4,4 0 1 1 12,16 A4,4 0 1 1 12,8",
            "Replace" => "M3,7 H21 M16,2 L21,7 L16,12 M21,17 H3 M8,12 L3,17 L8,22",
            "Expand" => "M6,9 L12,3 L18,9 M6,15 L12,21 L18,15",
            "Logs" => "M4,2 H16 L20,6 V22 H4 Z M8,10 H16 M8,14 H16 M8,18 H14",
            "Cause" => "M4,4 H9 V9 H4 Z M15,15 H20 V20 H15 Z M7,9 V17 H15",
            "Branch" => "M6,3 V21 M6,10 H15 Q19,10 19,6 V3 M15,17 L19,21 L23,17",
            "Direct" => "M3,4 H21 V20 H3 Z M7,9 H17 M7,15 H13",
            "Variable" => "M8,3 H5 V9 L2,12 L5,15 V21 H8 M16,3 H19 V9 L22,12 L19,15 V21 H16 M10,9 L14,15 M14,9 L10,15",
            "Result" => "M3,3 H15 V21 H3 Z M7,8 H11 M7,12 H11 M11,17 H22 M18,13 L22,17 L18,21",
            "Secret" => "M6,10 H18 V22 H6 Z M8,10 V6 A4,4 0 0 1 16,6 V10 M12,15 V18",
            "Exit" => "M11,3 H3 V21 H11 M9,12 H22 M17,7 L22,12 L17,17",
            "Open" => "M4,2 H15 L20,7 V22 H4 Z M15,2 V7 H20 M8,12 H16 M8,16 H14",
            _ => "M4,11 A1,1 0 1 1 4,13 A1,1 0 1 1 4,11 M12,11 A1,1 0 1 1 12,13 A1,1 0 1 1 12,11 M20,11 A1,1 0 1 1 20,13 A1,1 0 1 1 20,11"
        };
        var icon = new Path
        {
            Data = Geometry.Parse(geometry),
            Width = IconSize,
            Height = IconSize,
            Stretch = Stretch.Uniform,
            StrokeThickness = 1.6,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        icon.SetBinding(Shape.StrokeProperty, new Binding(nameof(Control.Foreground)) { Source = item });
        item.Icon = icon;
        if (danger) item.SetResourceReference(Control.ForegroundProperty, "App.Brush.MenuDanger");
        AutomationProperties.SetAutomationId(item, "Context." + identity);
        AutomationProperties.SetName(item, item.Header?.ToString() ?? identity);
    }

    public static void Prepare(ContextMenu menu)
    {
        menu.SetResourceReference(FrameworkElement.StyleProperty, "App.ContextMenuStyle");
        PrepareItems(menu);
    }

    private static void PrepareItems(ItemsControl menu)
    {
        foreach (var entry in menu.Items)
        {
            if (entry is Separator separator)
            {
                separator.SetResourceReference(FrameworkElement.StyleProperty, "App.MenuSeparatorStyle");
                continue;
            }
            if (entry is not MenuItem item) continue;
            item.SetResourceReference(FrameworkElement.StyleProperty, "App.MenuItemStyle");
            if (item.Icon is null)
                Decorate(item, BindingOperations.GetBinding(item, MenuItem.HeaderProperty)?.Path?.Path?.Trim('[', ']') ?? item.Header?.ToString() ?? "");
            if (item.Icon is FrameworkElement icon) { icon.Width = IconSize; icon.Height = IconSize; }
            PrepareItems(item);
        }
    }

    public static void SelectContextItem(ListBox list, ListBoxItem item)
    {
        if (!item.IsSelected)
        {
            list.UnselectAll();
            item.IsSelected = true;
        }
        item.Focus();
    }
}
