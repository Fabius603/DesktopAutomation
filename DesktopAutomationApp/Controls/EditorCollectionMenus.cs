using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace DesktopAutomationApp.Controls;

internal static class EditorCollectionMenus
{
    public static ContextMenu Create(ListBox list, FrameworkElement target, string profile, IReadOnlyList<object> selection)
    {
        var menu = ActionMenus.Create(target);
        if (list.ItemsSource is not IList items || selection.Count == 0) return menu;
        var rows = items.Cast<object>().Where(selection.Contains).ToArray();
        var minimum = profile == "Answers" ? UserChoiceStepDefinition.MinimumOptions
            : profile == "Conditions" ? TaskAutomation.Jobs.ConditionRules.MinimumConditions : 0;
        int? maximum = profile == "Answers" ? UserChoiceStepDefinition.MaximumOptions : null;
        ActionMenus.Add(menu, "Ui.Context.Duplicate", new RelayCommand(() =>
        {
            var originals = items.Cast<object>().ToHashSet();
            foreach (var row in rows) Command(row, true)!.Execute(null);
            list.UnselectAll();
            foreach (var copy in items.Cast<object>().Where(item => !originals.Contains(item))) list.SelectedItems.Add(copy);
        }, () => CollectionSelectionRules.CanDuplicate(items.Count, rows.Length, maximum)
            && rows.All(row => Command(row, true)?.CanExecute(null) == true)), count: rows.Length);
        if (profile is "Answers" or "OverlayRows")
        {
            void Move(int direction)
            {
                var order = CollectionSelectionRules.MoveOrder(items.Count, rows.Select(items.IndexOf).ToArray(), direction);
                if (order is null) return;
                var snapshot = items.Cast<object>().ToArray();
                for (var index = 0; index < order.Count; index++)
                {
                    var item = snapshot[order[index]];
                    var current = items.IndexOf(item);
                    if (current == index) continue;
                    items.RemoveAt(current); items.Insert(index, item);
                }
                list.UnselectAll();
                foreach (var row in rows) list.SelectedItems.Add(row);
            }
            bool CanMove(int direction) => CollectionSelectionRules.MoveOrder(items.Count, rows.Select(items.IndexOf).ToArray(), direction) is not null;
            ActionMenus.Add(menu, "Ui.Common.MoveStepUp", new RelayCommand(() => Move(-1), () => CanMove(-1)), count: rows.Length);
            ActionMenus.Add(menu, "Ui.Common.MoveStepDown", new RelayCommand(() => Move(1), () => CanMove(1)), count: rows.Length);
        }
        menu.Items.Add(new Separator());
        ActionMenus.Add(menu, "Ui.Context.Remove", new RelayCommand(() =>
        {
            foreach (var row in rows.Reverse()) Command(row, false)!.Execute(null);
        }, () => CollectionSelectionRules.CanRemove(items.Count, rows.Length, minimum)
            && rows.All(row => Command(row, false)?.CanExecute(null) == true)), count: rows.Length);
        return menu;
    }

    private static ICommand? Command(object row, bool duplicate) => row switch
    {
        UserChoiceOptionEditorViewModel item => duplicate ? item.DuplicateCommand : item.RemoveCommand,
        PointEntryViewModel item => duplicate ? item.DuplicateCommand : item.RemoveCommand,
        AxisExpressionViewModel item => duplicate ? item.DuplicateCommand : item.RemoveCommand,
        ConditionRowViewModel item => duplicate ? item.DuplicateCommand : item.RemoveCommand,
        DetectionOverlayRowViewModel item => duplicate ? item.DuplicateCommand : item.RemoveCommand,
        TextOverlayRowViewModel item => duplicate ? item.DuplicateCommand : item.RemoveCommand,
        _ => null
    };
}
