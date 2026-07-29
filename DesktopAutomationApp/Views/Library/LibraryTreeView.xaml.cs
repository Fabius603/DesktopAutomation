using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels.Library;

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
