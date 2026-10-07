using DesktopAutomationApp.Controls;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;
using System.Windows;
using DesktopAutomationApp.Behaviors;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels.Library;
using DesktopAutomationApp.Input;
using System.Windows.Controls.Primitives;

namespace DesktopAutomationApp.Views.Library;

public partial class LibraryTreeView : UserControl
{
    private Point _dragStart;

    public LibraryTreeView()
    {
        InitializeComponent();
        PreviewMouseLeftButtonDown += (_, eventArgs) => _dragStart = eventArgs.GetPosition(this);
    }

    private void Folder_Select(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) != null) return;
        if (sender is FrameworkElement { DataContext: LibraryTreeNodeViewModel node } && DataContext is LibraryTreeViewModel vm)
        {
            if (node.IsFolder) vm.OpenNodeCommand.Execute(node);
        }
    }

    private void Folder_Expand(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LibraryTreeNodeViewModel node }) node.IsExpanded = !node.IsExpanded;
        e.Handled = true;
    }

    private void LibraryNodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LibraryTreeViewModel vm && sender is ListBox list)
            vm.SetSelectedNodes(list.SelectedItems.Cast<LibraryTreeNodeViewModel>());
    }

    private void ItemMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not DependencyObject source) return;
        var list = FindAncestor<ListBox>(source);
        if (list is null) return;
        var item = ItemsControl.ContainerFromElement(list, source) as ListBoxItem;
        if (item is null) return;
        ActionMenus.SelectContextItem(list, item);
        OpenMenu(item, item.DataContext as LibraryTreeNodeViewModel);
        e.Handled = true;
    }

    private void Background_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled || FindAncestor<TextBoxBase>(e.OriginalSource as DependencyObject) is not null) return;
        if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is not null) return;
        OpenMenu((FrameworkElement)sender, null);
        e.Handled = true;
    }

    internal ContextMenu CreateMenu(FrameworkElement target, LibraryTreeNodeViewModel? node)
    {
        var menu = ActionMenus.Create(target);
        if (DataContext is not LibraryTreeViewModel vm) return menu;
        void Add(string key, ICommand command, object? parameter = null, int count = 1)
            => ActionMenus.Add(menu, key, command, parameter, count: count);
        if (node is null)
        {
            var create = new MenuItem { Header = vm.NewItemLabel, Command = vm.NewItemCommand };
            ActionMenus.Decorate(create, "Create");
            menu.Items.Add(create);
            Add("Ui.Library.NewFolder", vm.NewFolderCommand);
            return menu;
        }
        if (node.IsFolder)
        {
            Add("Ui.Common.Open", vm.OpenNodeCommand, node);
            var create = new MenuItem { Header = vm.NewItemLabel, Command = vm.NewItemInFolderCommand, CommandParameter = node };
            ActionMenus.Decorate(create, "Create");
            menu.Items.Add(create);
            Add("Ui.Library.NewFolder", vm.NewSubfolderCommand, node);
            Add("Ui.Library.RenameFolder", vm.RenameFolderCommand, node);
            var folderMove = new MenuItem { Header = Loc.Get("Ui.Context.MoveTo") };
            ActionMenus.Decorate(folderMove, "MoveTo");
            ActionMenus.Add(folderMove, "Ui.Context.LibraryRoot", new AsyncRelayCommand(() => vm.MoveNodeAsync(node, null)));
            foreach (var destination in vm.MoveDestinations.Where(folder => folder.Id != node.Id))
            {
                var entry = new MenuItem { Header = destination.Name, Command = new AsyncRelayCommand(() => vm.MoveNodeAsync(node, destination.Id)) };
                ActionMenus.Decorate(entry, "Ui.Context.LibraryRoot");
                folderMove.Items.Add(entry);
            }
            menu.Items.Add(folderMove);
            Add("Ui.Library.MoveUpOneLevel", vm.MoveUpOneLevelCommand, node);
            menu.Items.Add(new Separator()); Add("Ui.Library.Delete", vm.DeleteNodeCommand, node);
            return menu;
        }
        var sourceList = FindAncestor<ListBox>(target);
        var selected = sourceList == FolderList ? new[] { node }
            : vm.SelectedNodes.Any(item => item.Id == node.Id) ? vm.SelectedNodes.ToArray() : [node];
        var count = selected.Length;
        if (count == 1)
        {
            Add("Ui.Common.Open", vm.OpenNodeCommand, node);
            Add(node.IsRunning ? "Ui.Common.Stop" : "Ui.Context.Run", vm.ExecuteNodeCommand, node);
            Add("Ui.Common.Rename", vm.RenameNodeCommand, node);
            if (node.Item?.OpenFile is { } open) Add("Ui.Common.OpenFolderInExplorer", new RelayCommand(open));
        }
        else
        {
            var running = selected.Where(item => item.IsRunning && item.Item?.Stop is not null).ToArray();
            if (running.Length > 0) Add("Ui.Common.Stop", new RelayCommand(() => { foreach (var item in running) item.Item!.Stop!(); }), count: running.Length);
        }
        if (selected.All(item => item.Item?.DuplicateAsync is not null))
            Add("Ui.Context.Duplicate", new AsyncRelayCommand(() => vm.DuplicateSelectionAsync(selected)), count: count);
        if (selected.All(item => item.Item?.SetActiveAsync is not null))
        {
            Add("Ui.Context.Enable", new AsyncRelayCommand(() => vm.SetSelectionActiveAsync(true, selected), () => selected.Any(item => item.Item?.IsActive?.Invoke() != true)), count: count);
            Add("Ui.Context.Disable", new AsyncRelayCommand(() => vm.SetSelectionActiveAsync(false, selected), () => selected.Any(item => item.Item?.IsActive?.Invoke() == true)), count: count);
        }
        var move = new MenuItem { Header = Loc.Get("Ui.Context.MoveTo") };
        ActionMenus.Decorate(move, "MoveTo");
        ActionMenus.Add(move, "Ui.Context.LibraryRoot", new AsyncRelayCommand(() => vm.MoveSelectionAsync(null, selected)));
        foreach (var folder in vm.MoveDestinations)
        {
            var entry = new MenuItem { Header = folder.Name, Command = new AsyncRelayCommand(() => vm.MoveSelectionAsync(folder.Id, selected)) };
            ActionMenus.Decorate(entry, "Ui.Context.LibraryRoot");
            move.Items.Add(entry);
        }
        menu.Items.Add(move);
        if (count == 1) Add("Ui.Library.MoveUpOneLevel", vm.MoveUpOneLevelCommand, node);
        menu.Items.Add(new Separator());
        if (selected.All(item => item.Item?.DeleteConfirmedAsync is not null))
            Add("Ui.Library.Delete", new AsyncRelayCommand(() => vm.DeleteSelectionAsync(selected)), count: count);
        else if (count == 1) Add("Ui.Library.Delete", vm.DeleteNodeCommand, node);
        return menu;
    }

    private void OpenMenu(FrameworkElement target, LibraryTreeNodeViewModel? node)
    {
        var menu = CreateMenu(target, node);
        target.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) != null) return;
        if (sender is not FrameworkElement { DataContext: LibraryTreeNodeViewModel node } ||
            DataContext is not LibraryTreeViewModel viewModel)
            return;
        if (node.IsFolder)
        {
            viewModel.OpenNodeCommand.Execute(node);
            e.Handled = true;
        }
    }

    private void Node_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) != null) return;
        if (sender is not FrameworkElement { DataContext: LibraryTreeNodeViewModel { IsItem: true } node } ||
            DataContext is not LibraryTreeViewModel viewModel)
            return;
        viewModel.OpenNodeCommand.Execute(node);
        e.Handled = true;
    }

    private void Node_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        var list = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
        if (list is null) return;
        ActionMenus.SelectContextItem(list, item);
        OpenMenu(item, item.DataContext as LibraryTreeNodeViewModel);
        e.Handled = true;
    }

    private void Node_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            sender is not FrameworkElement { DataContext: LibraryTreeNodeViewModel node })
            return;
        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) != null) return;
        if (DataContext is not LibraryTreeViewModel viewModel) return;
        viewModel.BeginDrag(node);
        UpdateDragPreviewPosition(position);
        try
        {
            using var scrolling = new DragScrollSession(this, RefreshDragPreviewFromCursor);
            DragDrop.DoDragDrop(this, node, DragDropEffects.Move);
        }
        finally
        {
            viewModel.EndDrag();
        }
    }

    private void LibraryTree_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not LibraryTreeViewModel viewModel) return;
        if (e.Key == Key.Apps || e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift)
        {
            var list = FolderList.IsKeyboardFocusWithin ? FolderList : LibraryNodes;
            if (list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem) is ListBoxItem item)
                OpenMenu(item, item.DataContext as LibraryTreeNodeViewModel);
            else OpenMenu(list, null);
            e.Handled = true;
            return;
        }

        if (AppShortcutGestures.Matches(e, AppShortcutGestures.FocusSearch))
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (Keyboard.FocusedElement is TextBoxBase)
        {
            if (e.Key == Key.Escape && viewModel.HasSearchText)
            {
                viewModel.ClearSearchCommand.Execute(null);
                LibraryNodes.Focus();
                e.Handled = true;
                return;
            }

            // Keep native text editing intact, but do not disable unrelated
            // library shortcuts merely because the search box owns the focus.
            if (AppShortcutGestures.Matches(e, AppShortcutGestures.Copy)
                || AppShortcutGestures.Matches(e, AppShortcutGestures.Paste)
                || AppShortcutGestures.Matches(e, AppShortcutGestures.Undo)
                || AppShortcutGestures.Matches(e, AppShortcutGestures.Redo)
                || AppShortcutGestures.Matches(e, AppShortcutGestures.RedoAlternate)
                || e.Key is Key.Delete or Key.Back or Key.Enter
                || (Keyboard.Modifiers == ModifierKeys.None && e.Key is >= Key.A and <= Key.Z))
                return;
        }

        var selected = (FolderList.IsKeyboardFocusWithin ? FolderList.SelectedItem : LibraryNodes.SelectedItem) as LibraryTreeNodeViewModel;
        if (LibraryNodes.IsKeyboardFocusWithin && viewModel.SelectedNodes.Count > 1)
        {
            if (AppShortcutGestures.Matches(e, AppShortcutGestures.Open)
                || AppShortcutGestures.Matches(e, AppShortcutGestures.Rename)
                || AppShortcutGestures.Matches(e, AppShortcutGestures.Execute)
                || AppShortcutGestures.Matches(e, AppShortcutGestures.MoveUp))
            { e.Handled = true; return; }
            if (AppShortcutGestures.Matches(e, AppShortcutGestures.Stop))
            {
                foreach (var item in viewModel.SelectedNodes.Where(item => item.IsRunning)) item.Item?.Stop?.Invoke();
                e.Handled = true; return;
            }
        }
        if (AppShortcutGestures.Matches(e, AppShortcutGestures.DuplicateStep) && viewModel.SelectedNodes.Count > 0
            && viewModel.SelectedNodes.All(node => node.Item?.DuplicateAsync is not null))
        {
            Execute(new AsyncRelayCommand(viewModel.DuplicateSelectionAsync), null, e);
            return;
        }
        if (AppShortcutGestures.Matches(e, AppShortcutGestures.NewFolder))
            Execute(viewModel.NewFolderCommand, null, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.NewItem))
            Execute(viewModel.NewItemCommand, null, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Open))
            Execute(viewModel.OpenNodeCommand, selected, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Rename))
            Execute(viewModel.RenameNodeCommand, selected, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Delete))
            Execute(viewModel.SelectedNodes.Count > 1 ? new AsyncRelayCommand(viewModel.DeleteSelectionAsync) : viewModel.DeleteNodeCommand, selected, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Execute))
            Execute(viewModel.StartNodeCommand, selected, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Stop))
            Execute(viewModel.StopNodeCommand, selected, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.MoveUp))
            Execute(viewModel.MoveUpOneLevelCommand, selected, e);
    }

    private static void Execute(ICommand command, object? parameter, KeyEventArgs e)
    {
        if (!command.CanExecute(parameter)) return;
        command.Execute(parameter);
        e.Handled = true;
    }

    private void LibraryTree_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(LibraryTreeNodeViewModel)) ||
            DataContext is not LibraryTreeViewModel viewModel)
            return;
        UpdateDragPreviewPosition(e.GetPosition(this));
        var target = FindNode(e.OriginalSource as DependencyObject);
        if (target != null)
            viewModel.SetDropTarget(target);
        else if (FindAncestor<ListBox>(e.OriginalSource as DependencyObject) == LibraryNodes && viewModel.SelectedFolderId.HasValue)
            viewModel.SetDropTarget(viewModel.FolderNodes.FirstOrDefault(node => node.Id == viewModel.SelectedFolderId));
        else
            viewModel.SetRootDropTarget();
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void LibraryTree_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        => RefreshDragPreviewFromCursor();

    private void RefreshDragPreviewFromCursor()
    {
        if (DataContext is LibraryTreeViewModel { IsDragActive: true })
        {
            var cursor = System.Windows.Forms.Cursor.Position;
            UpdateDragPreviewPosition(PointFromScreen(new Point(cursor.X, cursor.Y)));
        }
    }

    private void UpdateDragPreviewPosition(Point position)
    {
        if (!DragPreview.IsMeasureValid)
            DragPreview.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var previewSize = DragPreview.DesiredSize;
        var previewPosition = LibraryDragPreviewPosition.Calculate(position, previewSize, RenderSize);
        Canvas.SetLeft(DragPreview, previewPosition.X);
        Canvas.SetTop(DragPreview, previewPosition.Y);
    }

    private async void LibraryTree_PreviewDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(LibraryTreeNodeViewModel)) ||
            e.Data.GetData(typeof(LibraryTreeNodeViewModel)) is not LibraryTreeNodeViewModel source ||
            DataContext is not LibraryTreeViewModel viewModel)
            return;
        var target = FindNode(e.OriginalSource as DependencyObject);
        var targetFolderId = target?.Folder?.Id ?? target?.FolderId;
        if (target == null && FindAncestor<ListBox>(e.OriginalSource as DependencyObject) == LibraryNodes) targetFolderId = viewModel.SelectedFolderId;
        await viewModel.MoveNodeAsync(source, targetFolderId);
        viewModel.SetDropTarget(null);
        e.Handled = true;
    }

    private async void MoveUpOneLevel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem ||
            FindAncestor<ContextMenu>(menuItem) is not { PlacementTarget: FrameworkElement placementTarget } ||
            placementTarget.DataContext is not LibraryTreeNodeViewModel node ||
            placementTarget.Tag is not LibraryTreeViewModel viewModel)
            return;
        await viewModel.MoveUpOneLevelAsync(node);
        e.Handled = true;
    }

    private static LibraryTreeNodeViewModel? FindNode(DependencyObject? current)
    {
        while (current != null)
        {
            if (current is FrameworkElement { DataContext: LibraryTreeNodeViewModel node })
                return node;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match) return match;
            current = current is FrameworkContentElement contentElement
                ? contentElement.Parent
                : System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
