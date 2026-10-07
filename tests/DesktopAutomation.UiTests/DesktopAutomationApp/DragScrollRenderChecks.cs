using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using DesktopAutomationApp.Services.Jobs;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using DesktopAutomationApp.Behaviors;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class DragScrollRenderChecks
{
    internal static void Verify(string directory)
    {
        var content = new Border { Width = 900, Height = 1500, Background = Brushes.Transparent };
        var viewer = new ScrollViewer
        {
            Content = content,
            CanContentScroll = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var root = new UserControl { Content = viewer, Width = 400, Height = 300 };
        using var surface = new HwndSource(new HwndSourceParameters("Drag scrolling test")
        { PositionX = 170, PositionY = 130, Width = 400, Height = 300, WindowStyle = unchecked((int)0x80000000) });
        surface.RootVisual = root;
        root.Measure(new Size(400, 300));
        root.Arrange(new Rect(0, 0, 400, 300));
        root.UpdateLayout();
        viewer.ScrollToVerticalOffset(300);
        root.UpdateLayout();
        var center = new Point(150, 150);
        var edge = new Point(150, 280);
        var notifications = 0;
        using (var scrolling = new DragScrollSession(root, () => notifications++, start: false))
        {
            var original = viewer.VerticalOffset;
            if (!scrolling.WheelAt(center, -120) || viewer.VerticalOffset != original + 30)
                throw new InvalidOperationException("Drag wheel must use the ordinary global scroll distance.");
            var manual = viewer.VerticalOffset;
            for (var index = 0; index < 10; index++) scrolling.Advance(edge, 0.05);
            if (viewer.VerticalOffset != manual) throw new InvalidOperationException("Wheel input must take priority over edge scrolling.");
            for (var index = 0; index < 10; index++) scrolling.Advance(edge, 0.05);
            if (viewer.VerticalOffset <= manual) throw new InvalidOperationException("A stationary pointer must scroll at the edge after the wheel pause.");
            var stopped = viewer.VerticalOffset;
            scrolling.Advance(center, 0.05);
            scrolling.Advance(new Point(-1, 280), 0.05);
            if (viewer.VerticalOffset != stopped) throw new InvalidOperationException("Auto-scroll must stop in the center and outside the view.");
            if (!scrolling.WheelAt(center, -120, horizontal: true) || viewer.HorizontalOffset != 30)
                throw new InvalidOperationException("Shift/wheel must scroll horizontally during a drag.");
            scrolling.Dispose();
            var disposed = viewer.VerticalOffset;
            if (scrolling.WheelAt(center, -120)) throw new InvalidOperationException("Disposed drag must not consume wheel input.");
            scrolling.Advance(edge, 0.05);
            if (viewer.VerticalOffset != disposed) throw new InvalidOperationException("Disposed drag must stop scrolling.");
        }
        viewer.ScrollToHorizontalOffset(0);
        root.UpdateLayout();
        var screen = root.PointToScreen(center);
        var coordinates = (nint)((int)screen.X & 0xffff | ((int)screen.Y & 0xffff) << 16);
        using (var native = new DragScrollSession(root, () => notifications++))
        {
            var before = viewer.VerticalOffset;
            if (!PostMessage(surface.Handle, 0x020A, unchecked((nuint)(-120 << 16)), coordinates))
                throw new InvalidOperationException("Could not queue native wheel test.");
            if (!PeekMessage(out var peeked, surface.Handle, 0x020A, 0x020A, 0) || peeked.Message != 0x020A || viewer.VerticalOffset != before)
                throw new InvalidOperationException("Peeking at a wheel message must not scroll or consume it.");
            if (!PeekMessage(out var message, surface.Handle, 0x020A, 0x020A, 1) || message.Message != 0)
                throw new InvalidOperationException("Native drag must consume the wheel exactly once in the message pump.");
            root.UpdateLayout();
            if (viewer.VerticalOffset != before + 30)
                throw new InvalidOperationException("Native wheel must scroll the rendered surface during the drag session.");
            PostMessage(surface.Handle, 0x020E, (nuint)(120 << 16), coordinates);
            if (!PeekMessage(out var horizontal, surface.Handle, 0x020E, 0x020E, 1) || horizontal.Message != 0 || viewer.HorizontalOffset != 30)
                throw new InvalidOperationException("Native horizontal wheel must scroll in the correct direction.");
            PostMessage(surface.Handle, 0x020A, unchecked((nuint)((-120 << 16) | 4)), coordinates);
            if (!PeekMessage(out var shifted, surface.Handle, 0x020A, 0x020A, 1) || shifted.Message != 0 || viewer.HorizontalOffset != 60)
                throw new InvalidOperationException("Native Shift/wheel must honor the modifiers captured in its message.");
        }
        PostMessage(surface.Handle, 0x020A, unchecked((nuint)(-120 << 16)), coordinates);
        if (!PeekMessage(out var released, surface.Handle, 0x020A, 0x020A, 1) || released.Message != 0x020A)
            throw new InvalidOperationException("Ending the drag must release the native message hook.");
        if (notifications < 3) throw new InvalidOperationException("Scrolling must refresh the drag preview.");
        VerifyStepPreviewTransitions(directory);
        Console.WriteLine("Drag scrolling interactions verified");
    }

    private static void VerifyStepPreviewTransitions(string directory)
    {
        var steps = new ObservableCollection<JobStep>(Enumerable.Range(0, 20).Select(_ => new TimeoutStep()));
        var order = steps.Select(step => step.Id).ToArray();
        var list = new ListBox { ItemsSource = steps, BorderThickness = new Thickness(0) };
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        list.ItemContainerStyle = (Style)XamlReader.Parse("""
            <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ListBoxItem">
              <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ListBoxItem">
                <Border x:Name="Card" Height="64" Background="DimGray"><TextBlock Text="Step" Foreground="White"/></Border>
              </ControlTemplate></Setter.Value></Setter>
            </Style>
            """);
        StepDragDrop.MoveRequest? committedMove = null;
        var commits = 0;
        StepDragDrop.SetMoveCommand(list, new RelayCommand<StepDragDrop.MoveRequest>(request => { committedMove = request; commits++; }));
        StepDragDrop.SetIsGhostPreviewEnabled(list, true);
        StepDragDrop.SetPlacementResolver(list, JobStepDropPlacement.Resolve);
        var spacer = new Border { Height = 0 };
        var stack = new StackPanel();
        stack.Children.Add(spacer);
        stack.Children.Add(list);
        var left = new ScrollViewer { Content = stack, CanContentScroll = false };
        var right = new ScrollViewer { Content = new Border { Height = 1500, Background = Brushes.DarkSlateGray }, CanContentScroll = false };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        var root = new UserControl { Content = grid, Width = 600, Height = 300 };
        var decorator = new AdornerDecorator { Child = root };
        using var surface = new HwndSource(new HwndSourceParameters("Step preview lifecycle test")
        { PositionX = 170, PositionY = 130, Width = 600, Height = 300, WindowStyle = unchecked((int)0x80000000) });
        surface.RootVisual = decorator;
        decorator.Measure(new Size(600, 300));
        decorator.Arrange(new Rect(0, 0, 600, 300));
        root.UpdateLayout();
        var source = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(1);
        var originalOpacity = source.Opacity;
        var originalMinimum = list.MinHeight;
        var payload = new StepDragDrop.DragPayload((IList)steps, 1, [1]);
        StepDragDrop.BeginDragSession(list, payload, startTimers: false);
        try
        {
            var preview = StepDragDrop.ActivePreview!;
            var scrolling = StepDragDrop.ActiveScrolling!;
            Point TargetPoint() => ((ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(3))
                .TranslatePoint(new Point(100, -4), list) - new Vector(0, StepDragDrop.GetPreviewDisplacement((ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(3)));
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var point = TargetPoint();
                StepDragDrop.UpdateDragTarget(list, payload, point);
                if (!preview.IsSnapped) throw new InvalidOperationException("Re-entering the step list must reacquire the drag preview.");
                var bounds = preview.PreviewBounds!.Value;
                left.ScrollToVerticalOffset(left.VerticalOffset + 20);
                root.UpdateLayout();
                preview.UpdatePointer(root.TranslatePoint(new Point(100, 150), root));
                if (Math.Abs(preview.PreviewBounds!.Value.Y - (bounds.Y - 20)) > 0.5)
                    throw new InvalidOperationException("A snapped preview must follow its target when the viewport scrolls.");
                // The inspector scrolls while the previous target still belongs to the step list.
                // This used to pass a null hit into ContainerFromElement and crash on the UI timer.
                if (!scrolling.WheelAt(new Point(500, 150), -120)) throw new InvalidOperationException("Fixture inspector must scroll.");
                if (preview.IsSnapped || list.MinHeight != originalMinimum)
                    throw new InvalidOperationException("Leaving the step area must release the target and its gap.");
                if (Math.Abs(source.Opacity - 0.22) > 0.01) throw new InvalidOperationException("Leaving the target must retain the source ghost.");
                if (attempt == 0) Save(decorator, Path.Combine(directory, "drag-scroll-step-exit.png"));
                if (StepDragDrop.UpdateDragTarget(list, payload, new Point(-10, 100)) != DragDropEffects.None)
                    throw new InvalidOperationException("An outside pointer must never become an insertion target.");
                StepDragDrop.UpdateDragTarget(list, payload, TargetPoint());
                if (!preview.IsSnapped) throw new InvalidOperationException("The target preview must recover after an outside scroll.");
                if (attempt == 0) Save(decorator, Path.Combine(directory, "drag-scroll-step-reentry.png"));
                StepDragDrop.ClearTargetPreview(list);
            }
            left.ScrollToVerticalOffset(100);
            root.UpdateLayout();
            var gapPoint = TargetPoint();
            StepDragDrop.UpdateDragTarget(list, payload, gapPoint);
            var gapIndex = preview.TargetIndex;
            // Traverse the entire actual gap, including its lower half, then stop there.
            // The pointer moves much farther than stationary-pointer hysteresis allows.
            foreach (var offset in Enumerable.Range(0, 77).Concat(Enumerable.Repeat(70, 60)))
            {
                StepDragDrop.UpdateDragTarget(list, payload, gapPoint + new Vector(0, offset));
                root.UpdateLayout();
                if (!preview.IsSnapped || preview.TargetIndex != gapIndex)
                    throw new InvalidOperationException("Moving through the preview gap must keep the insertion target stable.");
            }
            Save(decorator, Path.Combine(directory, "drag-pointer-inside-gap.png"));
            // Cross the shifted next card and keep moving down, then reverse direction.
            // Resolving repeatedly at each position must never bounce back after layout.
            var previousIndex = gapIndex;
            foreach (var offset in Enumerable.Range(77, 220).Concat(Enumerable.Range(0, 297).Reverse()))
            {
                var pointer = gapPoint + new Vector(0, offset);
                StepDragDrop.UpdateDragTarget(list, payload, pointer);
                var expectedIndex = preview.TargetIndex;
                if (!preview.IsSnapped)
                    throw new InvalidOperationException("Moving between adjacent targets must keep the preview snapped.");
                for (var frame = 0; frame < 5; frame++)
                {
                    root.UpdateLayout();
                    StepDragDrop.UpdateDragTarget(list, payload, pointer);
                    if (!preview.IsSnapped || preview.TargetIndex != expectedIndex)
                        throw new InvalidOperationException("Shifted cards must not change the insertion target again at the same pointer position.");
                }
                previousIndex = Math.Max(previousIndex, expectedIndex);
            }
            if (previousIndex <= gapIndex)
                throw new InvalidOperationException("Leaving the preview gap must allow moving to subsequent steps.");
            StepDragDrop.ClearTargetPreview(list);
            left.ScrollToVerticalOffset(0);
            root.UpdateLayout();
            var targetPoint = TargetPoint();
            var heldPointer = list.TranslatePoint(targetPoint, root);
            StepDragDrop.UpdateDragTarget(list, payload, targetPoint);
            var heldIndex = preview.TargetIndex;
            for (var frame = 0; frame < 60; frame++)
            {
                // Model the feedback: changing insertion targets changes the surrounding layout.
                // Input remains at the same physical position, with a small hand tremor.
                spacer.Height = preview.TargetIndex == heldIndex ? 60 : 0;
                root.UpdateLayout();
                var jitter = heldPointer + new Vector(frame % 2 == 0 ? 2 : -2, 0);
                StepDragDrop.UpdateDragTarget(list, payload, root.TranslatePoint(jitter, list));
                if (!preview.IsSnapped || preview.TargetIndex != heldIndex)
                    throw new InvalidOperationException("Preview layout must not repeatedly change the target under a stationary pointer.");
            }
            Save(decorator, Path.Combine(directory, "drag-stable-target.png"));
            StepDragDrop.UpdateDragTarget(list, payload, root.TranslatePoint(heldPointer - new Vector(0, 100), list));
            if (!preview.IsSnapped || preview.TargetIndex == heldIndex)
                throw new InvalidOperationException("A deliberate pointer movement must still select another insertion target.");
            StepDragDrop.ClearTargetPreview(list);
            spacer.Height = 0;
            root.UpdateLayout();
            targetPoint = TargetPoint();
            heldPointer = list.TranslatePoint(targetPoint, root);
            StepDragDrop.UpdateDragTarget(list, payload, targetPoint);
            heldIndex = preview.TargetIndex;
            if (!scrolling.WheelAt(heldPointer, -240) || preview.TargetIndex == heldIndex)
                throw new InvalidOperationException("Intentional wheel scrolling must release pointer retention and resolve the new target.");
            left.ScrollToVerticalOffset(left.ScrollableHeight);
            root.UpdateLayout();
            var bottomOffset = left.VerticalOffset;
            foreach (var targetRow in new[] { 18, 17, 19, 18 })
            {
                var container = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(targetRow);
                var point = container.TranslatePoint(new Point(100, -4), list);
                StepDragDrop.UpdateDragTarget(list, payload, point);
                if (!preview.IsSnapped || preview.TargetIndex != targetRow || Math.Abs(left.VerticalOffset - bottomOffset) > 0.5)
                    throw new InvalidOperationException("Moving the preview near the bottom must not shrink the scroll extent and jump the viewport.");
            }
            var pendingIndex = preview.TargetIndex;
            var pendingPointer = list.TranslatePoint(preview.PointerRelativeTo(list), root);
            // A section header exposed by preview layout must not override the retained slot,
            // including when its alternative boundary would reject the move.
            StepDragDrop.SetPreviewMoveValidator(list, request => request.TargetIndex != 0);
            if (!StepDragDrop.ShowSectionStartTarget(list, root.TranslatePoint(pendingPointer, list)) || preview.TargetIndex != pendingIndex)
                throw new InvalidOperationException("A layout-generated section hover must keep the stable insertion slot.");
            var data = new DataObject(StepDragDrop.DataFormat, payload);
            if (StepDragDrop.DropRetainedTarget(list, root.TranslatePoint(pendingPointer, list), data) != DragDropEffects.Move
                || commits != 1 || committedMove is not { } move || move.TargetIndex != pendingIndex || !ReferenceEquals(move.Target, steps))
                throw new InvalidOperationException("Dropping must commit the retained preview position exactly once.");
            if (!order.SequenceEqual(steps.Select(step => step.Id))) throw new InvalidOperationException("Preview transitions must not move persisted steps.");
        }
        finally { StepDragDrop.CleanupDrag(); }
        if (list.MinHeight != originalMinimum || source.Opacity != originalOpacity)
            throw new InvalidOperationException("Ending the drag must restore source visuals and target layout.");
        Console.WriteLine("Step drag exit and re-entry verified");
        Console.WriteLine("Stationary drag target retention verified");
    }

    private static void Save(FrameworkElement root, string path)
    {
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(600, 300, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(path);
        encoder.Save(output);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public nint Hwnd;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint hwnd, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out NativeMessage message, nint hwnd, uint min, uint max, uint remove);
}
