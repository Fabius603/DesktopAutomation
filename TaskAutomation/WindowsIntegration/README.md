# Windows integration backend

The integration has two public entry points:

- `IWindowsSystemEventHub` for automation events.
- `IWindowsSystemStateService` for point-in-time job queries.

`WindowsCapabilityCatalog` is the authoritative list of supported IDs. Event and query IDs are persisted as stable strings.

## Query parameters

| Query | Parameters |
|---|---|
| `filesystem.path` | `path` (required) |
| `process.running` | `name` (required, with or without `.exe`) |
| `storage.drives` | `name` (optional drive prefix) |
| `device.hardware`, `audio.devices`, `printer.status` | `filter_property`, `filter_value` (optional WMI filter) |

Other built-in queries currently require no parameters. The public provider and service
contracts return the concrete query-specific `WindowsStateQueryResult`. The default
provider may use the internal `WindowsStateSnapshot` only as a private native-data
assembly object before it crosses the provider boundary. Unsupported and denied queries
return a status and error code instead of pretending that the queried state is false.

## Event filters

Native event filters are matched case-insensitively against event data. `filesystem.*` requires `path` and optionally accepts `include_subdirectories`. `input.idle.*` accepts `threshold_ms` (default: 60000). WLAN connection transitions accept `ssid`; WLAN notifications without connection data do not expose that filter. Device event filters are matched against the native device path.

All automation events are push-based. The sources use .NET system notifications, Core Audio callbacks, a Win32 message window, window-event hooks, `FileSystemWatcher`, process traces, spooler change notifications, global input hooks with one-shot threshold timers, and Windows Event Log subscriptions. The event hub contains no state polling loop. All subscriptions share the same hub and support per-subscription debouncing.

Legacy `*.changed` IDs remain available and are emitted together with more specific IDs such as `device.usb.connected`, `filesystem.deleted`, `window.focused`, `audio.volume.muted`, `printer.job.added`, and `windows_update.installed`.

The native WLAN source additionally exposes association, authentication, connect/disconnect, roaming, radio, signal, scan, adapter, network-availability, and profile events. A completed WLAN connection is classified as connected only when its native reason code reports success. Bluetooth exposes the reliable generic device-list change notification; old persisted Bluetooth subevent IDs are mapped to it. Session, power, display, clipboard, printer, storage, system-setting, and lifecycle notifications are classified into concrete event IDs whenever the Windows callback contains enough information. Windows Update listens to both the provider's System and Operational channels. Unclassified provider-specific records still use their category's `*.changed` fallback.

## Adding a capability

1. Add a stable ID to `WindowsCapabilityCatalog`.
2. Add or reuse a focused result record in `WindowsQueryResults.cs` and annotate
   every selectable property with an explicit, stable `ResultProperty` ID.
3. Register the query/result mapping in `WindowsQueryResultRegistry`.
4. Implement the query in an `IWindowsStateProvider` when the capability exposes state.
5. Add an `IWindowsEventSource` for a global push API or an
   `IWindowsSubscriptionEventSource` when native registration depends on automation filters.
6. Register the provider/source in dependency injection.

Do not expose access failures as normal `false` values. Use `Unsupported`, `AccessDenied`, `Timeout`, or `Failed` and a stable `ErrorCode`.
