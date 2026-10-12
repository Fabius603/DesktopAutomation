---
date: 2026-10-12
status: accepted
supersedes: 2026-10-06-support-velopack-and-msix.md
superseded_by:
---

# Use supported Windows APIs with standard MSIX virtualization

## Context

Store certification rejected `unvirtualizedResources`. The user requires its removal, feature
parity across distribution channels, and removal of automatic settings for which no supported
equivalent exists. Manual settings-page replacements were explicitly rejected.

## Decision

- Keep the shared self-contained application payload, installation policy, updater isolation,
  profile compatibility guard and StartupTask architecture from the superseded distribution record.
- Enable standard MSIX virtualization. Do not declare `unvirtualizedResources` or disable registry
  or filesystem write virtualization. New Windows integrations must use supported public contracts.
- `AppPaths` owns a user-managed `%USERPROFILE%\DesktopAutomation` data directory, outside both
  the package and AppData redirection. `Local` and `Roaming` are logical categories, not a promise
  of Windows roaming synchronization. Named profiles live in `Profiles\name` under that root.
  Every channel uses the same data, DPAPI secrets, profile marker and `ProfileLease` file.
- `LegacyDataImport` copies old canonical AppData roots first, then earlier DesktopAutomationApp
  roots. It preserves existing targets and originals, skips reparse points and instance locks,
  publishes complete files, and marks each completed import so deleted data is not resurrected.
  The application acquires the new lease before import and checks imported profile compatibility.
- Remove automatic Windows theme, focus mode and default input/output device settings in every
  channel. Keep saved IDs readable and report a localized validation error and unsupported runtime
  result. `RetiredWindowsSettings` owns this compatibility rule; no manual fallback is substituted.
- Use documented `SetDisplayConfig` topologies for display mode instead of DisplaySwitch arguments.
- MSIX startup does not attempt to delete another installation's HKCU Run entry. The shared lease
  suppresses duplicate execution; users control each installation's startup registration.
- Declare `wiFiControl` for detailed WLAN notifications. User consent and Windows privacy policies
  remain prerequisites; capabilities never override permission denial.
- `FFmpegRuntime` owns an absolute writable tool directory under shared user data. FFmpeg downloads
  never target the package; no setting mutation is delegated to a helper to bypass virtualization.

## Alternatives considered

Keeping the rejected capability cannot satisfy the user's request. Separate MSIX data would break
shared-profile behavior. Hidden registry/COM mutations and scripts do not supply a documented
supported system-setting contract. Manual replacements do not preserve automatic semantics.

## Consequences

Existing installations need updating before concurrent cross-channel use: old versions retain old
paths and leases. Old AppData originals remain as recovery copies, not live synchronization sources.
Shared user-owned data survives MSIX removal; users can remove it explicitly. Legacy jobs containing
removed settings require editing. Runtime downloads still require network access on first video use.
Actual installed tests, not unit tests alone, must establish shared data and crash recovery.

## Verification

Cover absent restricted capabilities, retired saved IDs, validation and runtime rejection,
non-destructive/restartable import, shared paths, lease collision and recovery. Run Full repository
verification, strict website documentation checks and the installed MSIX distribution probe with
an isolated profile. Report any unavailable OS or hardware boundary rather than claiming it passed.

Sources: [MSIX behavior](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes),
[SetDisplayConfig](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setdisplayconfig),
[WLAN notifications](https://learn.microsoft.com/en-us/windows/win32/api/wlanapi/nf-wlanapi-wlanregisternotification),
[Store supported methods](https://learn.microsoft.com/en-us/windows/apps/publish/store-policy-archive/store-policy-7-19).
