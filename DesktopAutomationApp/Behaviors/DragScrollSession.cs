using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesktopAutomationApp.Behaviors;

/// <summary>Scrolling owned only for the lifetime of a native drag, including its OLE message loop.</summary>
internal sealed class DragScrollSession : IDisposable
{
    private readonly FrameworkElement _root;
    private readonly Action _scrolled;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly HookProc _hookProc;
    private nint _hook;
    private double _lastTick;
    private double _wheelPause;
    private double _verticalVelocity;
    private double _horizontalVelocity;
    private ScrollViewer? _vertical;
    private ScrollViewer? _horizontal;
    private bool _disposed;
    private Point _pointer;

    internal Point PointerRelativeTo(FrameworkElement element) => _root.TranslatePoint(_pointer, element);

    internal DragScrollSession(FrameworkElement root, Action scrolled, bool start = true)
    {
        _root = root;
        _scrolled = scrolled;
        _hookProc = OnMessage;
        _timer = new DispatcherTimer(DispatcherPriority.Render, root.Dispatcher) { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
        _root.Unloaded += OnUnloaded;
        if (!start) return;
        // A thread-local message hook also runs while DoDragDrop pumps Windows messages.
        _hook = SetWindowsHookEx(3 /* WH_GETMESSAGE */, _hookProc, 0, GetCurrentThreadId());
        if (_hook == 0)
        {
            Dispose();
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        _timer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args) => Dispose();

    private nint OnMessage(int code, nint wParam, nint lParam)
    {
        if (!_disposed && code == 0 && wParam == 1 /* PM_REMOVE */)
        {
            var message = Marshal.PtrToStructure<NativeMessage>(lParam);
            if (message.Message is 0x020A or 0x020E && PresentationSource.FromVisual(_root) is not null)
            {
                var coordinates = (long)message.LParam;
                var screen = new Point(unchecked((short)coordinates), unchecked((short)(coordinates >> 16)));
                var delta = unchecked((short)((long)message.WParam >> 16));
                var horizontal = message.Message == 0x020E || (message.WParam & 4 /* MK_SHIFT */) != 0;
                // Horizontal wheel messages use the opposite sign from vertical/Shift+wheel.
                if (WheelAt(_root.PointFromScreen(screen), message.Message == 0x020E ? -delta : delta, horizontal))
                {
                    message.Message = 0; // WM_NULL: prevent a second WPF wheel delivery.
                    Marshal.StructureToPtr(message, lParam, false);
                }
            }
        }
        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    internal bool WheelAt(Point point, int delta, bool horizontal = false)
    {
        _pointer = point;
        if (_disposed || delta == 0 || Hit(point) is not { } source) return false;
        _wheelPause = 0.65;
        _verticalVelocity = _horizontalVelocity = 0;
        if (!GlobalScrollBehavior.TryScroll(source, horizontal, delta)) return false;
        _root.UpdateLayout();
        _scrolled();
        return true;
    }

    private void OnTick(object? sender, EventArgs args)
    {
        var now = _clock.Elapsed.TotalSeconds;
        var elapsed = Math.Clamp(now - _lastTick, 0, 0.05);
        _lastTick = now;
        if (PresentationSource.FromVisual(_root) is not null && GetCursorPos(out var screen))
            Advance(_root.PointFromScreen(new Point(screen.X, screen.Y)), elapsed);
    }

    internal void Advance(Point point, double elapsed)
    {
        _pointer = point;
        if (_disposed) return;
        elapsed = Math.Clamp(elapsed, 0, 0.05);
        _wheelPause = Math.Max(0, _wheelPause - elapsed);
        if (_wheelPause > 0) return;
        var ancestors = Ancestors(Hit(point)).OfType<ScrollViewer>().ToArray();
        var vertical = ancestors.FirstOrDefault(viewer => viewer.ScrollableHeight > 0.1);
        var horizontal = ancestors.FirstOrDefault(viewer => viewer.ScrollableWidth > 0.1);
        if (!ReferenceEquals(vertical, _vertical)) _verticalVelocity = 0;
        if (!ReferenceEquals(horizontal, _horizontal)) _horizontalVelocity = 0;
        _vertical = vertical;
        _horizontal = horizontal;
        var moved = Scroll(vertical, point, elapsed, false, ref _verticalVelocity);
        moved |= Scroll(horizontal, point, elapsed, true, ref _horizontalVelocity);
        if (!moved) return;
        _root.UpdateLayout();
        _scrolled();
    }

    private bool Scroll(ScrollViewer? viewer, Point point, double elapsed, bool horizontal, ref double velocity)
    {
        if (viewer is null) { velocity = 0; return false; }
        var local = _root.TranslatePoint(point, viewer);
        // Actual dimensions are DIPs even when an ItemsControl uses logical scrolling.
        var requested = StepDragPreviewPolicy.ScrollVelocity(horizontal ? local.X : local.Y,
            horizontal ? viewer.ActualWidth : viewer.ActualHeight);
        velocity = StepDragPreviewPolicy.EaseScrollVelocity(velocity, requested, elapsed);
        var offset = horizontal ? viewer.HorizontalOffset : viewer.VerticalOffset;
        var extent = horizontal ? viewer.ScrollableWidth : viewer.ScrollableHeight;
        var next = Math.Clamp(offset + velocity * elapsed, 0, extent);
        if (Math.Abs(next - offset) < 0.001) return false;
        if (horizontal) viewer.ScrollToHorizontalOffset(next);
        else viewer.ScrollToVerticalOffset(next);
        return true;
    }

    private DependencyObject? Hit(Point point) => point.X >= 0 && point.Y >= 0 && point.X <= _root.ActualWidth && point.Y <= _root.ActualHeight
        ? _root.InputHitTest(point) as DependencyObject : null;

    private static IEnumerable<DependencyObject> Ancestors(DependencyObject? source)
    {
        for (var current = source; current is not null; current = current is Visual or System.Windows.Media.Media3D.Visual3D
                 ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current))
            yield return current;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _root.Unloaded -= OnUnloaded;
        if (_hook != 0) UnhookWindowsHookEx(_hook);
        _hook = 0;
        GC.KeepAlive(_hookProc);
    }

    private delegate nint HookProc(int code, nint wParam, nint lParam);
    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenPoint { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public nint Hwnd;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public ScreenPoint Point;
        public uint Private;
    }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int hook, HookProc callback, nint module, uint thread);
    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out ScreenPoint point);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
