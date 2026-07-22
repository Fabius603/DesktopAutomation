namespace TaskAutomation.WindowsIntegration;

public interface IWindowsCapabilityCatalog
{
    IReadOnlyCollection<WindowsCapabilityDescriptor> Capabilities { get; }
    WindowsCapabilityDescriptor? Find(string id);
}

public sealed class WindowsCapabilityCatalog : IWindowsCapabilityCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["network.availability.changed"] = "Netzwerkverfügbarkeit geändert", ["network.address.changed"] = "Netzwerkadresse geändert",
        ["network.connected"] = "Netzwerk verbunden", ["network.disconnected"] = "Netzwerk getrennt", ["network.wifi.changed"] = "WLAN-Ereignis",
        ["network.wifi.connecting"] = "WLAN-Verbindung wird aufgebaut", ["network.wifi.connected"] = "WLAN verbunden",
        ["network.wifi.connection_failed"] = "WLAN-Verbindung fehlgeschlagen", ["network.wifi.disconnecting"] = "WLAN wird getrennt",
        ["network.wifi.disconnected"] = "WLAN-Verbindung getrennt", ["network.wifi.associating"] = "WLAN-Zuordnung wird aufgebaut",
        ["network.wifi.associated"] = "WLAN zugeordnet", ["network.wifi.authenticating"] = "WLAN wird authentifiziert",
        ["network.wifi.roaming_started"] = "WLAN-Roaming gestartet", ["network.wifi.roaming_completed"] = "WLAN-Roaming abgeschlossen",
        ["network.wifi.radio_state_changed"] = "WLAN-Funkstatus geändert", ["network.wifi.signal_quality_changed"] = "WLAN-Signalstärke geändert",
        ["network.wifi.scan_completed"] = "WLAN-Suche abgeschlossen", ["network.wifi.scan_failed"] = "WLAN-Suche fehlgeschlagen",
        ["network.wifi.adapter_added"] = "WLAN-Adapter hinzugefügt", ["network.wifi.adapter_removed"] = "WLAN-Adapter entfernt",
        ["network.wifi.profile_changed"] = "WLAN-Profil geändert", ["network.wifi.network_available"] = "WLAN-Netzwerk verfügbar",
        ["network.wifi.network_unavailable"] = "WLAN-Netzwerk nicht verfügbar", ["network.wifi.autoconfig_enabled"] = "WLAN-Autokonfiguration aktiviert",
        ["network.wifi.autoconfig_disabled"] = "WLAN-Autokonfiguration deaktiviert",
        ["network.connectivity"] = "Aktuelle Netzwerkverbindung",
        ["audio.device.changed"] = "Audiogerät geändert", ["audio.device.added"] = "Audiogerät hinzugefügt",
        ["audio.device.removed"] = "Audiogerät entfernt", ["audio.device.default_changed"] = "Standard-Audiogerät geändert",
        ["audio.device.connected"] = "Audiogerät verbunden", ["audio.device.disconnected"] = "Audiogerät getrennt",
        ["audio.device.state_changed"] = "Audiogerätestatus geändert", ["audio.device.property_changed"] = "Audiogeräteeigenschaft geändert",
        ["audio.volume.changed"] = "Lautstärke geändert", ["audio.volume.level_changed"] = "Lautstärke geändert",
        ["audio.volume.muted"] = "Audio stummgeschaltet", ["audio.volume.unmuted"] = "Stummschaltung aufgehoben", ["audio.devices"] = "Audiogeräte", ["audio.volume"] = "Lautstärke und Stummschaltung",
        ["session.state.changed"] = "Windows-Sitzung geändert", ["session.state"] = "Aktuelle Windows-Sitzung",
        ["session.locked"] = "Sitzung gesperrt", ["session.unlocked"] = "Sitzung entsperrt",
        ["session.logged_on"] = "Benutzer angemeldet", ["session.logged_off"] = "Benutzer abgemeldet",
        ["session.remote_connected"] = "Remotesitzung verbunden", ["session.remote_disconnected"] = "Remotesitzung getrennt",
        ["session.console_connected"] = "Konsolensitzung verbunden", ["session.console_disconnected"] = "Konsolensitzung getrennt",
        ["power.mode.changed"] = "Energiemodus geändert", ["power.status"] = "Akku und Energiequelle",
        ["power.suspended"] = "Standby gestartet", ["power.resumed"] = "Standby beendet", ["power.status.changed"] = "Energiestatus geändert",
        ["power.ac_connected"] = "Netzteil angeschlossen", ["power.ac_disconnected"] = "Netzteil getrennt",
        ["power.charging_started"] = "Laden begonnen", ["power.charging_stopped"] = "Laden beendet",
        ["power.battery_level_changed"] = "Akkustand geändert",
        ["display.settings.changed"] = "Bildschirmeinstellungen geändert", ["display.monitors"] = "Angeschlossene Monitore",
        ["display.connected"] = "Bildschirm verbunden", ["display.disconnected"] = "Bildschirm getrennt",
        ["display.configuration.changed"] = "Bildschirmkonfiguration geändert",
        ["display.orientation_changed"] = "Bildschirmausrichtung geändert", ["display.resolution_changed"] = "Bildschirmauflösung geändert",
        ["display.primary_changed"] = "Hauptbildschirm geändert",
        ["device.hardware.changed"] = "Hardwaregerät geändert", ["device.hardware.connected"] = "Hardwaregerät verbunden",
        ["device.hardware.disconnected"] = "Hardwaregerät getrennt", ["device.usb.changed"] = "USB-Gerät geändert",
        ["device.hardware.updated"] = "Hardwarekonfiguration aktualisiert",
        ["device.usb.connected"] = "USB-Gerät verbunden", ["device.usb.disconnected"] = "USB-Gerät getrennt",
        ["device.hardware"] = "Hardwaregeräte", ["device.usb"] = "USB-Geräte",
        ["bluetooth.state.changed"] = "Bluetooth geändert", ["bluetooth.device.connected"] = "Bluetooth-Gerät verbunden",
        ["bluetooth.device.disconnected"] = "Bluetooth-Gerät getrennt", ["bluetooth.devices"] = "Bluetooth-Geräte",
        ["bluetooth.device.paired"] = "Bluetooth-Gerät gekoppelt", ["bluetooth.device.unpaired"] = "Bluetooth-Kopplung entfernt",
        ["bluetooth.radio.enabled"] = "Bluetooth aktiviert", ["bluetooth.radio.disabled"] = "Bluetooth deaktiviert",
        ["filesystem.changed"] = "Datei oder Ordner geändert", ["filesystem.created"] = "Datei oder Ordner erstellt",
        ["filesystem.deleted"] = "Datei oder Ordner gelöscht", ["filesystem.renamed"] = "Datei oder Ordner umbenannt",
        ["filesystem.path"] = "Datei- oder Ordnerzustand",
        ["process.started"] = "Prozess gestartet", ["process.exited"] = "Prozess beendet", ["process.running"] = "Läuft ein Prozess?",
        ["window.changed"] = "Fenster geändert", ["window.opened"] = "Fenster geöffnet", ["window.closed"] = "Fenster geschlossen",
        ["window.focused"] = "Fenster fokussiert", ["window.minimized"] = "Fenster minimiert", ["window.restored"] = "Fenster wiederhergestellt",
        ["window.shown"] = "Fenster angezeigt", ["window.hidden"] = "Fenster ausgeblendet", ["window.moved_or_resized"] = "Fenster verschoben oder skaliert",
        ["window.foreground"] = "Aktives Fenster",
        ["input.idle.changed"] = "Leerlaufgrenze erreicht oder verlassen", ["input.idle.entered"] = "Leerlaufgrenze erreicht",
        ["input.idle.left"] = "Leerlauf beendet", ["input.idle"] = "Zeit seit letzter Eingabe",
        ["clipboard.changed"] = "Zwischenablage geändert", ["clipboard.content_changed"] = "Zwischenablageninhalt geändert",
        ["clipboard.text_changed"] = "Text in Zwischenablage", ["clipboard.image_changed"] = "Bild in Zwischenablage",
        ["clipboard.files_changed"] = "Dateien in Zwischenablage", ["clipboard.cleared"] = "Zwischenablage geleert",
        ["clipboard.content"] = "Zwischenablageninhalt",
        ["printer.queue.changed"] = "Druckerwarteschlange geändert", ["printer.job.added"] = "Druckauftrag hinzugefügt",
        ["printer.job.changed"] = "Druckauftrag geändert", ["printer.job.deleted"] = "Druckauftrag entfernt",
        ["printer.state.changed"] = "Druckerstatus geändert", ["printer.status"] = "Druckerstatus",
        ["printer.added"] = "Drucker hinzugefügt", ["printer.removed"] = "Drucker entfernt",
        ["printer.connection_failed"] = "Druckerverbindung fehlgeschlagen", ["printer.settings_changed"] = "Druckereinstellung geändert",
        ["storage.drive.changed"] = "Laufwerk geändert", ["storage.drive.mounted"] = "Laufwerk eingebunden",
        ["storage.drive.unmounted"] = "Laufwerk entfernt", ["storage.drives"] = "Laufwerke und freier Speicher",
        ["storage.media.inserted"] = "Wechselmedium eingelegt", ["storage.media.removed"] = "Wechselmedium entfernt",
        ["system.time.changed"] = "Systemzeit geändert", ["system.settings.changed"] = "Systemeinstellung geändert",
        ["system.time.clock_adjusted"] = "Systemuhr angepasst", ["system.settings.locale_changed"] = "Region oder Sprache geändert",
        ["system.time.timezone_changed"] = "Zeitzone geändert",
        ["system.settings.colors_changed"] = "Farben geändert", ["system.settings.desktop_changed"] = "Desktop-Einstellung geändert",
        ["system.settings.general_changed"] = "Allgemeine Systemeinstellung geändert", ["system.settings.icons_changed"] = "Symbole geändert",
        ["system.settings.keyboard_changed"] = "Tastatureinstellung geändert", ["system.settings.menu_changed"] = "Menüeinstellung geändert",
        ["system.settings.mouse_changed"] = "Mauseinstellung geändert", ["system.settings.power_changed"] = "Energieeinstellung geändert",
        ["system.settings.screensaver_changed"] = "Bildschirmschoner geändert", ["system.settings.window_changed"] = "Fenstereinstellung geändert",
        ["system.settings"] = "Zeit, Sprache und Darstellung",
        ["security.state.changed"] = "Windows-Sicherheit geändert", ["security.threat.detected"] = "Bedrohung erkannt",
        ["security.threat.action_taken"] = "Sicherheitsmaßnahme ausgeführt", ["security.settings.changed"] = "Sicherheitseinstellung geändert",
        ["security.status"] = "Windows-Sicherheitsstatus",
        ["windows_update.changed"] = "Windows Update geändert", ["windows_update.download_started"] = "Update-Download gestartet",
        ["windows_update.downloaded"] = "Update heruntergeladen", ["windows_update.installed"] = "Update installiert",
        ["windows_update.failed"] = "Update fehlgeschlagen", ["windows_update.restart_required"] = "Update erfordert Neustart",
        ["windows_update.status"] = "Windows-Update-Status",
        ["system.lifecycle.changed"] = "Start, Herunterfahren oder Sitzung beendet", ["system.lifecycle"] = "Systemlaufzeit",
        ["system.lifecycle.logoff"] = "Benutzer wird abgemeldet", ["system.lifecycle.shutdown"] = "Windows wird heruntergefahren"
    };

    public IReadOnlyCollection<WindowsCapabilityDescriptor> Capabilities { get; } = BuildCapabilities();
    public WindowsCapabilityDescriptor? Find(string id) => Capabilities.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyCollection<WindowsCapabilityDescriptor> BuildCapabilities()
    {
        var result = new List<WindowsCapabilityDescriptor>();
        void E(WindowsEventCategory category, string query, params string[] ids)
        {
            // Events describe a transition over time and are valid automation triggers only.
            // Job steps perform a point-in-time query and therefore expose only the separate
            // query capabilities registered through Q below.
            foreach (var id in ids) result.Add(Create(id, category, true, false, query));
        }
        void Q(string id, WindowsEventCategory category, bool admin = false) => result.Add(Create(id, category, false, true, admin: admin));

        E(WindowsEventCategory.Network, "network.connectivity", "network.availability.changed", "network.address.changed", "network.connected", "network.disconnected",
            "network.wifi.changed", "network.wifi.connecting", "network.wifi.connected", "network.wifi.connection_failed", "network.wifi.disconnecting", "network.wifi.disconnected",
            "network.wifi.associating", "network.wifi.associated", "network.wifi.authenticating", "network.wifi.roaming_started", "network.wifi.roaming_completed",
            "network.wifi.radio_state_changed", "network.wifi.signal_quality_changed", "network.wifi.scan_completed", "network.wifi.scan_failed", "network.wifi.adapter_added",
            "network.wifi.adapter_removed", "network.wifi.profile_changed", "network.wifi.network_available", "network.wifi.network_unavailable",
            "network.wifi.autoconfig_enabled", "network.wifi.autoconfig_disabled"); Q("network.connectivity", WindowsEventCategory.Network);
        E(WindowsEventCategory.Audio, "audio.devices", "audio.device.changed", "audio.device.added", "audio.device.removed", "audio.device.connected", "audio.device.disconnected", "audio.device.default_changed", "audio.device.state_changed", "audio.device.property_changed");
        E(WindowsEventCategory.Audio, "audio.volume", "audio.volume.changed", "audio.volume.level_changed", "audio.volume.muted", "audio.volume.unmuted"); Q("audio.devices", WindowsEventCategory.Audio); Q("audio.volume", WindowsEventCategory.Audio);
        E(WindowsEventCategory.Session, "session.state", "session.state.changed", "session.locked", "session.unlocked", "session.logged_on", "session.logged_off",
            "session.remote_connected", "session.remote_disconnected", "session.console_connected", "session.console_disconnected"); Q("session.state", WindowsEventCategory.Session);
        E(WindowsEventCategory.Power, "power.status", "power.mode.changed", "power.suspended", "power.resumed", "power.status.changed", "power.ac_connected",
            "power.ac_disconnected", "power.charging_started", "power.charging_stopped"); Q("power.status", WindowsEventCategory.Power);
        E(WindowsEventCategory.Power, "power.status", "power.battery_level_changed");
        E(WindowsEventCategory.Display, "display.monitors", "display.settings.changed", "display.connected", "display.disconnected", "display.configuration.changed",
            "display.orientation_changed", "display.resolution_changed", "display.primary_changed"); Q("display.monitors", WindowsEventCategory.Display);
        E(WindowsEventCategory.Device, "device.hardware", "device.hardware.changed", "device.hardware.connected", "device.hardware.disconnected", "device.hardware.updated");
        E(WindowsEventCategory.Device, "device.usb", "device.usb.changed", "device.usb.connected", "device.usb.disconnected"); Q("device.hardware", WindowsEventCategory.Device); Q("device.usb", WindowsEventCategory.Device);
        E(WindowsEventCategory.Bluetooth, "bluetooth.devices", "bluetooth.state.changed", "bluetooth.device.connected", "bluetooth.device.disconnected",
            "bluetooth.device.paired", "bluetooth.device.unpaired", "bluetooth.radio.enabled", "bluetooth.radio.disabled"); Q("bluetooth.devices", WindowsEventCategory.Bluetooth);
        E(WindowsEventCategory.FileSystem, "filesystem.path", "filesystem.changed", "filesystem.created", "filesystem.deleted", "filesystem.renamed"); Q("filesystem.path", WindowsEventCategory.FileSystem);
        E(WindowsEventCategory.Process, "process.running", "process.started", "process.exited"); Q("process.running", WindowsEventCategory.Process);
        E(WindowsEventCategory.Window, "window.foreground", "window.changed", "window.opened", "window.closed", "window.focused", "window.minimized", "window.restored",
            "window.shown", "window.hidden", "window.moved_or_resized"); Q("window.foreground", WindowsEventCategory.Window);
        E(WindowsEventCategory.Input, "input.idle", "input.idle.changed", "input.idle.entered", "input.idle.left"); Q("input.idle", WindowsEventCategory.Input);
        E(WindowsEventCategory.Clipboard, "clipboard.content", "clipboard.changed", "clipboard.content_changed", "clipboard.text_changed", "clipboard.image_changed",
            "clipboard.files_changed", "clipboard.cleared"); Q("clipboard.content", WindowsEventCategory.Clipboard);
        E(WindowsEventCategory.Printer, "printer.status", "printer.queue.changed", "printer.job.added", "printer.job.changed", "printer.job.deleted", "printer.state.changed",
            "printer.added", "printer.removed", "printer.connection_failed", "printer.settings_changed"); Q("printer.status", WindowsEventCategory.Printer);
        E(WindowsEventCategory.Storage, "storage.drives", "storage.drive.changed", "storage.drive.mounted", "storage.drive.unmounted", "storage.media.inserted", "storage.media.removed"); Q("storage.drives", WindowsEventCategory.Storage);
        E(WindowsEventCategory.SystemSettings, "system.settings", "system.time.changed", "system.time.clock_adjusted", "system.time.timezone_changed", "system.settings.changed",
            "system.settings.locale_changed", "system.settings.colors_changed", "system.settings.desktop_changed", "system.settings.general_changed",
            "system.settings.icons_changed", "system.settings.keyboard_changed", "system.settings.menu_changed", "system.settings.mouse_changed",
            "system.settings.power_changed", "system.settings.screensaver_changed", "system.settings.window_changed"); Q("system.settings", WindowsEventCategory.SystemSettings);
        foreach (var id in new[] { "security.state.changed", "security.threat.detected", "security.threat.action_taken", "security.settings.changed" }) result.Add(Create(id, WindowsEventCategory.Security, true, true, "security.status", true));
        Q("security.status", WindowsEventCategory.Security, true);
        E(WindowsEventCategory.WindowsUpdate, "windows_update.status", "windows_update.changed", "windows_update.download_started", "windows_update.downloaded", "windows_update.installed", "windows_update.failed", "windows_update.restart_required"); Q("windows_update.status", WindowsEventCategory.WindowsUpdate);
        E(WindowsEventCategory.SystemLifecycle, "system.lifecycle", "system.lifecycle.changed", "system.lifecycle.logoff", "system.lifecycle.shutdown"); Q("system.lifecycle", WindowsEventCategory.SystemLifecycle);
        return result;
    }

    private static WindowsCapabilityDescriptor Create(string id, WindowsEventCategory category, bool events, bool query,
        string? relatedQuery = null, bool admin = false) => new(id, category, Names.GetValueOrDefault(id, id), events, query, relatedQuery,
        new WindowsCapabilityRequirements(admin), Parameters(id), ResultProperties(id));

    private static IReadOnlyList<WindowsParameterDescriptor> Parameters(string id)
    {
        if (id.StartsWith("network.wifi.", StringComparison.OrdinalIgnoreCase))
            return [new("ssid", "WLAN-Name (SSID)", WindowsParameterType.Text, Placeholder: "Mein WLAN")];
        if (id == "filesystem.path") return [new("path", "Datei oder Ordner", WindowsParameterType.FilePath, true, Placeholder: "C:\\Pfad")];
        if (id.StartsWith("filesystem.", StringComparison.OrdinalIgnoreCase))
            return [new("path", "Datei oder Ordner", WindowsParameterType.FilePath, true, Placeholder: "C:\\Pfad"), new("include_subdirectories", "Unterordner", WindowsParameterType.Boolean, DefaultValue: "false")];
        if (id is "process.started" or "process.exited" or "process.running") return [new("name", "Prozessname", WindowsParameterType.ProcessName, true, Placeholder: "notepad")];
        if (id.StartsWith("storage.drive.", StringComparison.OrdinalIgnoreCase) || id == "storage.drives") return [new("name", "Laufwerk", WindowsParameterType.Drive, Placeholder: "C:")];
        if (id.StartsWith("input.idle.", StringComparison.OrdinalIgnoreCase)) return [new("threshold_ms", "Leerlaufgrenze (ms)", WindowsParameterType.Duration, true, "60000")];
        if (id.StartsWith("device.", StringComparison.OrdinalIgnoreCase) || id.StartsWith("audio.device.", StringComparison.OrdinalIgnoreCase)
            || id.StartsWith("bluetooth.", StringComparison.OrdinalIgnoreCase) || id.StartsWith("printer.", StringComparison.OrdinalIgnoreCase))
            return [new("filter_value", "Filter", WindowsParameterType.Text, Placeholder: "Name enthält …")];
        return [];
    }

    private static IReadOnlyList<string> ResultProperties(string id) => id switch
    {
        "network.connectivity" => Common("IsConnected", "Connectivity", "ConnectionType", "Name", "Count", "Items.Count"),
        "audio.devices" => Common("Exists", "Count", "Name", "DeviceState", "Items.Count"), "audio.volume" => Common("Exists", "IsMuted", "Percentage", "OnOffState", "Id"),
        "session.state" => Common("IsActive", "Name", "Id", "SessionState"), "power.status" => Common("IsConnected", "IsCharging", "Percentage", "PowerSource"),
        "display.monitors" => Common("IsConnected", "Count", "Name", "Items.Count"),
        "device.hardware" or "device.usb" or "bluetooth.devices" => Common("Exists", "IsConnected", "Count", "Name", "DeviceState", "Items.Count"),
        "filesystem.path" => Common("Exists", "IsActive", "Path", "Count", "Value"), "process.running" => Common("Exists", "IsActive", "Count", "Name", "Id"),
        "window.foreground" => Common("Exists", "IsActive", "Name", "Text", "Id"), "input.idle" => Common("Value", "Percentage"),
        "clipboard.content" => Common("Exists", "Name", "Text"), "printer.status" => Common("Exists", "Count", "Name", "DeviceState", "Items.Count"),
        "storage.drives" => Common("Exists", "IsConnected", "Count", "Name", "FreeSpaceGb", "Items.Count"), "system.settings" => Common("Name", "Text", "IsEnabled"),
        "security.status" => Common("Exists", "IsEnabled", "OnOffState"), "windows_update.status" => Common("PendingRestart", "IsActive"),
        "system.lifecycle" => Common("Value", "Percentage", "Text"), _ => Common()
    };
    private static string[] Common(params string[] values) => ["Status", "IsAvailable", "CapturedAt", "ErrorCode", "ErrorMessage", .. values];
}
