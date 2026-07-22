using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Blocks physical mouse and keyboard input while still allowing injected automation input.</summary>
public static class WindowsInputBlockController
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const uint WmQuit = 0x0012;
    private const uint LlkhfInjected = 0x10;
    private const uint LlmhfInjected = 0x01;
    private const uint VkF10 = 0x79;
    private static readonly object Sync = new();
    private static Thread? _ownerThread;
    private static uint _ownerThreadId;

    public static void Block(TimeSpan safetyTimeout)
    {
        if (safetyTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(safetyTimeout));

        lock (Sync)
        {
            ReleaseCore();
            var started = new ManualResetEventSlim(false);
            Exception? startError = null;
            var owner = new Thread(() => RunHookLoop(safetyTimeout, started, ex => startError = ex))
            {
                IsBackground = true,
                Name = "DesktopAutomation.PhysicalInputBlocker"
            };
            _ownerThread = owner;
            owner.Start();
            if (!started.Wait(TimeSpan.FromSeconds(2)))
            {
                ReleaseCore();
                throw new TimeoutException("Das Blockieren der Eingaben hat nicht rechtzeitig geantwortet.");
            }
            if (startError is not null)
            {
                ReleaseCore();
                throw startError;
            }
        }
    }

    public static void Unblock()
    {
        lock (Sync)
            ReleaseCore();
    }

    private static void RunHookLoop(TimeSpan safetyTimeout, ManualResetEventSlim started, Action<Exception> reportError)
    {
        HookProc keyboardProc = KeyboardHook;
        HookProc mouseProc = MouseHook;
        IntPtr keyboardHook = IntPtr.Zero;
        IntPtr mouseHook = IntPtr.Zero;
        Timer? safetyTimer = null;
        try
        {
            _ownerThreadId = GetCurrentThreadId();
            PeekMessage(out _, IntPtr.Zero, 0, 0, 0); // Ensure the thread message queue exists.
            keyboardHook = SetWindowsHookEx(WhKeyboardLl, keyboardProc, IntPtr.Zero, 0);
            mouseHook = SetWindowsHookEx(WhMouseLl, mouseProc, IntPtr.Zero, 0);
            if (keyboardHook == IntPtr.Zero || mouseHook == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Maus- und Tastatureingaben konnten nicht blockiert werden.");

            safetyTimer = new Timer(_ => PostThreadMessage(_ownerThreadId, WmQuit, UIntPtr.Zero, IntPtr.Zero), null, safetyTimeout, Timeout.InfiniteTimeSpan);
            started.Set();
            while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
        catch (Exception ex)
        {
            reportError(ex);
            started.Set();
        }
        finally
        {
            safetyTimer?.Dispose();
            if (keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(keyboardHook);
            if (mouseHook != IntPtr.Zero) UnhookWindowsHookEx(mouseHook);
            _ownerThreadId = 0;
        }

        GC.KeepAlive(keyboardProc);
        GC.KeepAlive(mouseProc);
    }

    private static void ReleaseCore()
    {
        var owner = _ownerThread;
        _ownerThread = null;
        var threadId = _ownerThreadId;
        if (threadId != 0) PostThreadMessage(threadId, WmQuit, UIntPtr.Zero, IntPtr.Zero);
        if (owner is { IsAlive: true } && owner != Thread.CurrentThread)
            owner.Join(TimeSpan.FromSeconds(2));
    }

    private static IntPtr KeyboardHook(int code, UIntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var input = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if ((input.Flags & LlkhfInjected) == 0 && input.VkCode != VkF10)
                return new IntPtr(1);
        }
        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    private static IntPtr MouseHook(int code, UIntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && (Marshal.PtrToStructure<MsLlHookStruct>(lParam).Flags & LlmhfInjected) == 0)
            return new IntPtr(1);
        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    private delegate IntPtr HookProc(int code, UIntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct KbdLlHookStruct { public uint VkCode, ScanCode, Flags, Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct MsLlHookStruct { public Point Point; public uint MouseData, Flags, Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct Message { public IntPtr HWnd; public uint Value; public UIntPtr WParam; public IntPtr LParam; public uint Time; public Point Point; public uint Private; }

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern int GetMessage(out Message message, IntPtr window, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll")] private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
