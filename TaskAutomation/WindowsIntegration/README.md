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

Other built-in queries currently require no parameters. Every query returns a `WindowsStateSnapshot`; unsupported and denied queries return a status and error code instead of pretending that the queried state is false.

## Event filters

Native event filters are matched case-insensitively against event data. `filesystem.*` requires `path` and optionally accepts `include_subdirectories`. `input.idle.*` accepts `threshold_ms` (default: 60000). Device event filters are matched against the native device path.

All automation events are push-based. The sources use .NET system notifications, Core Audio callbacks, a Win32 message window, window-event hooks, `FileSystemWatcher`, process traces, spooler change notifications, global input hooks with one-shot threshold timers, and Windows Event Log subscriptions. The event hub contains no state polling loop. All subscriptions share the same hub and support per-subscription debouncing.

Legacy `*.changed` IDs remain available and are emitted together with more specific IDs such as `device.usb.connected`, `filesystem.deleted`, `window.focused`, `audio.volume.muted`, `printer.job.added`, and `windows_update.installed`.

The native WLAN source additionally exposes association, authentication, connect/disconnect, roaming, radio, signal, scan, adapter, network-availability, and profile events. Session, power, display, Bluetooth, clipboard, printer, storage, system-setting, and lifecycle notifications are likewise classified into concrete event IDs whenever the Windows callback contains enough information. Unclassified provider-specific records still use their category's `*.changed` fallback.

## Adding a capability

1. Add a stable ID to `WindowsCapabilityCatalog`.
2. Implement the query in an `IWindowsStateProvider` when the capability exposes state.
3. Add an `IWindowsEventSource` for a global push API or an `IWindowsSubscriptionEventSource` when native registration depends on automation filters.
4. Register the provider/source in dependency injection.

Do not expose access failures as normal `false` values. Use `Unsupported`, `AccessDenied`, `Timeout`, or `Failed` and a stable `ErrorCode`.
