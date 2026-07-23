using System.Runtime.InteropServices;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Waits on the Windows spooler's change-notification handle; no interval polling is used.</summary>
public sealed class PrinterWindowsEventSource : IWindowsEventSource
{
    private const uint PrinterChangeAddJob = 0x00000100;
    private const uint PrinterChangeSetJob = 0x00000200;
    private const uint PrinterChangeDeleteJob = 0x00000400;
    private const uint PrinterChangeAddPrinter = 0x00000001;
    private const uint PrinterChangeSetPrinter = 0x00000002;
    private const uint PrinterChangeDeletePrinter = 0x00000004;
    private const uint PrinterChangeFailedConnectionPrinter = 0x00000008;
    private const uint PrinterChangeAll = 0x7777FFFF;
    private IntPtr _printer;
    private IntPtr _notification;
    private Thread? _thread;
    private volatile bool _stopping;
    public event Action<WindowsSystemEvent>? EventReceived;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_thread is not null) return Task.CompletedTask;
        if (!OpenPrinter(null, out _printer, IntPtr.Zero)) throw new InvalidOperationException("Der lokale Druckserver konnte nicht geöffnet werden.");
        _notification = FindFirstPrinterChangeNotification(_printer, PrinterChangeAll, 0, IntPtr.Zero);
        if (_notification == new IntPtr(-1))
        {
            ClosePrinter(_printer); _printer = IntPtr.Zero;
            throw new InvalidOperationException("Druckerereignisse konnten nicht registriert werden.");
        }
        _stopping = false;
        _thread = new Thread(WaitLoop) { IsBackground = true, Name = "Windows printer event source" };
        _thread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping = true;
        if (_notification != IntPtr.Zero && _notification != new IntPtr(-1)) FindClosePrinterChangeNotification(_notification);
        _notification = IntPtr.Zero;
        if (_printer != IntPtr.Zero) ClosePrinter(_printer);
        _printer = IntPtr.Zero;
        _thread?.Join(TimeSpan.FromSeconds(2)); _thread = null;
        return Task.CompletedTask;
    }

    private void WaitLoop()
    {
        while (!_stopping && _notification != IntPtr.Zero)
        {
            if (WaitForSingleObject(_notification, 0xFFFFFFFF) != 0 || _stopping) break;
            if (!FindNextPrinterChangeNotification(_notification, out var change, IntPtr.Zero, IntPtr.Zero)) break;
            var eventTypes = GetEventTypes(change);
            var data = new Dictionary<string, string?> { ["change"] = string.Join(",", eventTypes), ["flags"] = change.ToString() };
            EventReceived?.Invoke(new WindowsSystemEvent("printer.queue.changed", WindowsEventCategory.Printer, DateTimeOffset.Now, Data: data));
            foreach (var eventType in eventTypes)
                EventReceived?.Invoke(new WindowsSystemEvent(eventType, WindowsEventCategory.Printer, DateTimeOffset.Now, Data: data));
        }
    }

    internal static IReadOnlyList<string> GetEventTypes(uint change)
    {
        var result = new List<string>();
        if ((change & PrinterChangeAddJob) != 0) result.Add("printer.job.added");
        if ((change & PrinterChangeDeleteJob) != 0) result.Add("printer.job.deleted");
        if ((change & PrinterChangeSetJob) != 0) result.Add("printer.job.changed");
        if ((change & PrinterChangeAddPrinter) != 0) result.Add("printer.added");
        if ((change & PrinterChangeDeletePrinter) != 0) result.Add("printer.removed");
        if ((change & PrinterChangeFailedConnectionPrinter) != 0) result.Add("printer.connection_failed");
        if ((change & PrinterChangeSetPrinter) != 0) result.Add("printer.settings_changed");
        return result.Count == 0 ? ["printer.state.changed"] : result;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool OpenPrinter(string? name, out IntPtr printer, IntPtr defaults);
    [DllImport("winspool.drv")] private static extern bool ClosePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern IntPtr FindFirstPrinterChangeNotification(IntPtr printer, uint filter, uint options, IntPtr notifyOptions);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool FindNextPrinterChangeNotification(IntPtr notification, out uint change, IntPtr options, IntPtr info);
    [DllImport("winspool.drv")] private static extern bool FindClosePrinterChangeNotification(IntPtr notification);
    [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
}
