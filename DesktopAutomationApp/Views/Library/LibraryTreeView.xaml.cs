using System.Windows;
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
        if (sender is ListBoxItem item)
            item.IsSelected = true;
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

        var selected = LibraryNodes.SelectedItem as LibraryTreeNodeViewModel;
        if (AppShortcutGestures.Matches(e, AppShortcutGestures.NewFolder))
            Execute(viewModel.NewFolderCommand, null, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.NewItem))
            Execute(viewModel.NewItemCommand, null, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Open))
            Execute(viewModel.OpenNodeCommand, selected, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Rename))
            Execute(viewModel.RenameNodeCommand, selected, e);
        else if (AppShortcutGestures.Matches(e, AppShortcutGestures.Delete))
            Execute(viewModel.DeleteNodeCommand, selected, e);
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
        else
            viewModel.SetRootDropTarget();
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void LibraryTree_GiveFeedback(object sender, GiveFeedbackEventArgs e)
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
