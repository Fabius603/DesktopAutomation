---
date: 2026-10-06
status: accepted
supersedes:
superseded_by:
---

# Support Velopack and MSIX from one application payload

## Context

The user authorized implementing the dual-distribution plan: Microsoft Store MSIX, the existing
Velopack direct download and local source builds must coexist without duplicate automation execution.
The previous single-file payload failed native Tesseract loading in a real deployment probe.

## Decision

- `InstallationContext` in the application layer owns distribution capabilities. Package identity
  takes precedence over installer metadata. Windows detection and StartupTask are shell adapters.
- Publish one self-contained, untrimmed, multi-file win-x64 payload for both packagers. Store identity
  is supplied from Partner Center; a distinct development identity is never claimed to be store-ready.
- Keep canonical AppData paths in `AppPaths`. Disable MSIX filesystem and registry write
  virtualization using `unvirtualizedResources`: jobs and user secrets must survive uninstall and
  be shared with the unpackaged app; Windows setting changes must reach the actual user registry.
- Acquire `ProfileLease` before migration, logging and automation initialization. The exclusive
  file handle protects across distribution channels and Windows sessions and recovers after crashes.
  Foreground second launches signal activation; background duplicates exit without executing jobs.
- Explicit `--profile name` / `DESKTOPAUTOMATION_PROFILE` isolates all application data. Non-default
  profiles never register autostart or migrate legacy files. Default paths remain unchanged.
- `ProfileCompatibility` rejects an incompatible profile format without modifying user files.
  Future incompatible persistence changes must advance this format before their first write.
- Velopack alone installs in-app updates. MSIX updates belong to package deployment; local builds
  require a rebuild or installer. A local MSIX is not assumed to originate from the Store.
- MSIX uses a manifest StartupTask; Velopack uses its stable launcher with `--startup`. Shared
  preferences are honored on startup. Startup registration never re-enables user-disabled tasks
  on normal launch, and local builds never write a development path into Windows startup.
- Release versions are immutable. Do not delete and recreate an existing release/tag.

## Alternatives considered

Replacing Velopack would remove a working direct-download channel. Converting the Velopack
installer with the Packaging Tool would entangle two update systems. Separate data directories
for official installations would require migration and make channel switches lose apparent data.

## Consequences

Windows 10 build 19041 is the application minimum (WinRT projections and manifest parameters).
Restricted capabilities need explanation during Store certification. Older releases that predate
this lease cannot be made to participate retroactively; update those installations before testing
coexistence. Administrator privileges and secure-desktop input remain Windows permission boundaries,
not capabilities granted by MSIX. Installed-data format compatibility is independent of app version.

## Verification

Capability matrices, isolated profile names, shared leases, crash recovery, profile format checks,
update isolation and settings projections are covered by automated tests. `eng/test-distribution.ps1`
tests actual deployed MSIX and optional installed Velopack against the local payload. The mandatory
repository gate remains `eng/verify.ps1 -Mode Full`. Store-origin updates require a reserved product
and a Store flight; local packaging tests do not claim that boundary is verified.
