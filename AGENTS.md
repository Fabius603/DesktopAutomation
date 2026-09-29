# Agent instructions

DesktopAutomation is a Windows desktop application built with .NET 8. The main UI is WPF;
Windows Forms is enabled only for specific interoperability needs. Business rules belong in
`TaskAutomation` or a lower platform-neutral project, not in `DesktopAutomationApp`.

## Authority and documentation

Apply guidance in this order:

1. The user's current request.
2. This file and the applicable files in `.agents/instructions/`.
3. Accepted, non-superseded records in `docs/decisions/`.
4. Current descriptions in `docs/architecture/`.
5. Dated snapshots in `docs/specs/`.

Specifications are historical snapshots, not standing instructions. Before relying on one,
ask the user whether it still applies. Do not silently rewrite an old specification; add a new
dated specification instead. Decisions remain authoritative until a newer decision supersedes
them.

Start at `.agents/README.md`. Load only instructions and repository skills relevant to the task.

## Required workflow

- Inspect `git status --short` before editing and preserve unrelated changes.
- Before adding behavior, search for existing implementations and identify the single canonical
  owner. Extend or refactor that owner instead of creating a second rule in a view model,
  converter, code-behind file, handler, or service.
- Define observable behavior and invariants before changing production code.
- Add risk-based tests at the appropriate level; test behavior, not the current code path.
- Keep persisted jobs, macros, automations, settings, and paths backward compatible unless the
  user explicitly approves a migration.
- Put all new or changed visible UI text in both localization resource files.
- Update release notes only for user-visible outcomes; follow `.agents/instructions/release-process.md`.
- Do not commit, stage, create branches, or push unless the user explicitly asks.

## Mandatory completion gate

After any repository change, run from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full
```

Do not report completion unless this command exits with code `0`. A skipped, unavailable, or
failing required check is a blocker and must be reported. CI must call the same entrypoint.

## Core instructions

- Architecture and persistence: `.agents/instructions/architecture.md`
- Architecture-boundary or deduplication work: `.agents/skills/maintain-architecture/SKILL.md`
- Testing: `.agents/instructions/testing.md`
- WPF and Windows desktop UI: `.agents/instructions/ui-desktop.md`
- Localization: `.agents/instructions/localization.md`
- Release notes and releases: `.agents/instructions/release-process.md`
