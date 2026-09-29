using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.WindowsIntegration;

public sealed class WindowsEventRegressionTests
{
    [Theory]
    [InlineData("printer.queue.changed")]
    [InlineData("printer.job.added")]
    [InlineData("printer.job.changed")]
    [InlineData("printer.job.deleted")]
    public void PrinterEvents_DoNotOfferUnsupportedNameFilter(string eventType)
    {
        var descriptor = Assert.IsType<WindowsCapabilityDescriptor>(new WindowsCapabilityCatalog().Find(eventType));

        Assert.DoesNotContain(descriptor.Parameters ?? [], parameter =>
            string.Equals(parameter.Name, "filter_value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Subscribe_IgnoresLegacyFiltersThatCapabilityNoLongerSupports()
    {
        var source = new ManualWindowsEventSource();
        var hub = CreateHub(source);
        var received = 0;

        using var subscription = hub.Subscribe(new WindowsEventSubscription
        {
            EventType = "printer.job.added",
            Filters = new(StringComparer.OrdinalIgnoreCase) { ["filter_value"] = "Office" }
        }, _ => received++);

        source.Fire(new WindowsSystemEvent("printer.job.added", WindowsEventCategory.Printer,
            DateTimeOffset.UtcNow, Data: new Dictionary<string, string?> { ["change"] = "added" }));

        Assert.Equal(1, received);
    }

    [Fact]
    public void Subscribe_ContinuesToApplySupportedFilters()
    {
        var source = new ManualWindowsEventSource();
        var hub = CreateHub(source);
        var received = 0;

        using var subscription = hub.Subscribe(new WindowsEventSubscription
        {
            EventType = "process.started",
            Filters = new(StringComparer.OrdinalIgnoreCase) { ["name"] = "notepad" }
        }, _ => received++);

        source.Fire(new WindowsSystemEvent("process.started", WindowsEventCategory.Process,
            DateTimeOffset.UtcNow, Data: new Dictionary<string, string?> { ["name"] = "calc.exe" }));
        source.Fire(new WindowsSystemEvent("process.started", WindowsEventCategory.Process,
            DateTimeOffset.UtcNow, Data: new Dictionary<string, string?> { ["name"] = "notepad.exe" }));

        Assert.Equal(1, received);
    }

    [Fact]
    public void EventLogFallback_EmitsLegacyEventOnlyOnce()
    {
        var eventTypes = EventLogWindowsEventSource.GetEventTypes(
            WindowsEventCategory.WindowsUpdate, "windows_update.changed");

        Assert.Equal(["windows_update.changed"], eventTypes);
    }

    [Fact]
    public void EventLogConcreteEvent_AlsoEmitsLegacyCompatibilityEvent()
    {
        var eventTypes = EventLogWindowsEventSource.GetEventTypes(
            WindowsEventCategory.WindowsUpdate, "windows_update.installed");

        Assert.Equal(["windows_update.changed", "windows_update.installed"], eventTypes);
    }

    [Theory]
    [InlineData(19, "windows_update.installed")]
    [InlineData(20, "windows_update.failed")]
    [InlineData(21, "windows_update.restart_required")]
    [InlineData(25, "windows_update.failed")]
    [InlineData(26, "windows_update.changed")]
    [InlineData(31, "windows_update.failed")]
    [InlineData(41, "windows_update.downloaded")]
    [InlineData(43, "windows_update.changed")]
    [InlineData(44, "windows_update.download_started")]
    public void WindowsUpdateEventIds_AreClassifiedByProviderMeaning(int eventId, string expected)
    {
        Assert.Equal(expected, EventLogWindowsEventSource.OnWindowsUpdate(eventId));
    }

    [Fact]
    public void WindowsUpdate_WatchesSystemAndOperationalLogs()
    {
        var sources = EventLogWindowsEventSource.SourceDefinitions
            .Where(source => source.Category == WindowsEventCategory.WindowsUpdate)
            .ToArray();

        Assert.Contains(sources, source => source.LogName == "System"
            && source.Query.Contains("Microsoft-Windows-WindowsUpdateClient", StringComparison.Ordinal));
        Assert.Contains(sources, source => source.LogName == "Microsoft-Windows-WindowsUpdateClient/Operational");
    }

    [Theory]
    [InlineData(1006, "security.threat.detected")]
    [InlineData(1117, "security.threat.action_taken")]
    [InlineData(2000, "security.state.changed")]
    [InlineData(2001, "security.state.changed")]
    [InlineData(5004, "security.settings.changed")]
    [InlineData(5007, "security.settings.changed")]
    public void DefenderEventIds_AreClassifiedByProviderMeaning(int eventId, string expected)
    {
        Assert.Equal(expected, EventLogWindowsEventSource.OnDefenderSecurity(eventId));
    }

    [Theory]
    [InlineData(2002, "security.settings.changed")]
    [InlineData(2006, "security.settings.changed")]
    [InlineData(2008, "security.settings.changed")]
    [InlineData(2010, "security.state.changed")]
    public void FirewallEventIds_AreClassifiedByProviderMeaning(int eventId, string expected)
    {
        Assert.Equal(expected, EventLogWindowsEventSource.OnFirewallSecurity(eventId));
    }

    [Fact]
    public void WlanConnectionComplete_UsesReasonCode()
    {
        Assert.Equal("network.wifi.connected", WlanWindowsEventSource.AcmType(10, 0));
        Assert.Equal("network.wifi.connection_failed", WlanWindowsEventSource.AcmType(10, 0x10001));
        Assert.Equal("network.wifi.connection_failed", WlanWindowsEventSource.AcmType(10));
    }

    [Fact]
    public void WlanSsidFilter_IsOnlyOfferedForEventsThatProvideConnectionData()
    {
        var catalog = new WindowsCapabilityCatalog();

        Assert.Contains(catalog.Find("network.wifi.connected")!.Parameters ?? [], parameter => parameter.Name == "ssid");
        Assert.DoesNotContain(catalog.Find("network.wifi.scan_completed")!.Parameters ?? [], parameter => parameter.Name == "ssid");
        Assert.DoesNotContain(catalog.Find("network.wifi.radio_state_changed")!.Parameters ?? [], parameter => parameter.Name == "ssid");
    }

    [Fact]
    public void LegacyBluetoothSubevents_AreHiddenAndMapToGenericEvent()
    {
        var catalog = new WindowsCapabilityCatalog();

        Assert.DoesNotContain(catalog.Capabilities, capability => capability.Id == "bluetooth.device.paired");
        Assert.Equal("bluetooth.state.changed", WindowsCapabilityCatalog.NormalizeEventId("bluetooth.device.paired"));
        Assert.Equal("bluetooth.state.changed", catalog.Find("bluetooth.device.paired")?.Id);
    }

    [Fact]
    public void LegacyBluetoothSubscription_ReceivesGenericDeviceListChange()
    {
        var source = new ManualWindowsEventSource();
        var hub = CreateHub(source);
        var received = 0;

        using var subscription = hub.Subscribe(new WindowsEventSubscription
        {
            EventType = "bluetooth.device.paired"
        }, _ => received++);
        source.Fire(new WindowsSystemEvent("bluetooth.state.changed", WindowsEventCategory.Bluetooth,
            DateTimeOffset.UtcNow));

        Assert.Equal(1, received);
    }

    [Fact]
    public void ClipboardWithUnsupportedFormat_IsContentChangedNotCleared()
    {
        Assert.Equal("clipboard.content_changed",
            Win32MessageWindowsEventSource.ClassifyClipboard(true, false, false, false));
        Assert.Equal("clipboard.cleared",
            Win32MessageWindowsEventSource.ClassifyClipboard(false, false, false, false));
    }

    [Fact]
    public void PrinterChangeBitmask_EmitsEveryMatchingSubevent()
    {
        var eventTypes = PrinterWindowsEventSource.GetEventTypes(0x00000100 | 0x00000002);

        Assert.Equal(["printer.job.added", "printer.settings_changed"], eventTypes);
    }

    [Theory]
    [InlineData(0x00000001, "audio.device.connected")]
    [InlineData(0x00000004, "audio.device.disconnected")]
    [InlineData(0x00000008, "audio.device.disconnected")]
    public void AudioDeviceState_EmitsAccurateConnectionEvent(uint state, string expected)
    {
        var source = new CoreAudioWindowsEventSource();
        var eventTypes = new List<string>();
        source.EventReceived += systemEvent => eventTypes.Add(systemEvent.EventType);

        source.OnDeviceStateChanged("device", state);

        Assert.Contains(expected, eventTypes);
    }

    private static WindowsSystemEventHub CreateHub(IWindowsEventSource source) =>
        new([source], [], new WindowsCapabilityCatalog(), NullLogger<WindowsSystemEventHub>.Instance);

    private sealed class ManualWindowsEventSource : IWindowsEventSource
    {
        public event Action<WindowsSystemEvent>? EventReceived;
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Fire(WindowsSystemEvent systemEvent) => EventReceived?.Invoke(systemEvent);
    }
}
