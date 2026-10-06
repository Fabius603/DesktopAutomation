# Windows integration backend

The integration has three public entry points:

- `IWindowsSystemEventHub` for automation events.
- `IWindowsSystemStateService` for point-in-time job queries.
- `IWindowsSystemSettingService` for validated Windows setting changes.

`WindowsCapabilityCatalog` is the authoritative list of supported IDs. Event and query IDs are persisted as stable strings.
Setting IDs and their parameter values follow the same stable persistence contract.

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

`ProcessEventWatcher` owns WMI process registration for the event hub source and the legacy
process automation provider. Prefer `Win32_ProcessStartTrace`/`StopTrace`; on denied access or
an unavailable trace class, subscribe to intrinsic creation/deletion events for `Win32_Process`
with a one-second WMI sampling interval. This fallback requires no privilege elevation, but may
miss processes that start and exit between samples. Consumers still receive pushed WMI events;
the application introduces no polling loop. A successful fallback is informational. If both
registrations fail, report unavailable process monitoring as a warning, preserving other app
features. Dispose failed and partially registered watchers before returning. Both consumers
retain separate subscriptions because the legacy provider additionally resolves window titles;
query selection and native event normalization have only this shared owner.

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

Setting changes are separate capabilities. Add a stable setting ID with
`SupportsSettingChange`, define all accepted parameters, implement the change in
`IWindowsSettingProvider`, and return `WindowsSettingChangeResult` with the previous and
applied value whenever Windows exposes both. Never persist passwords or other secrets in
the step parameters. Parameters backed by connected displays, installed devices, or saved
Windows profiles use `DynamicOptionSource`; `IWindowsSettingOptionProvider` resolves their
current display options while the stable device ID or profile name remains the serialized value.

Do not expose access failures as normal `false` values. Use `Unsupported`, `AccessDenied`, `Timeout`, or `Failed` and a stable `ErrorCode`.
