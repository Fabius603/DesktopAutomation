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
- Maintain website/product documentation in the same task whenever steps, values, automations,
  macros or logs change; follow `.agents/instructions/website-documentation.md`.
- Do not commit, stage, create branches, or push unless the user explicitly asks.

## Validation

Follow `.agents/instructions/testing.md` for risk-based validation. Agent turns and intermediate
iterations do not automatically require tests or a full repository run. Before completing a code
change, build affected projects and run checks covering the changed behavior and affected contracts.
Full verification through `eng/verify.ps1 -Mode Full` remains mandatory in CI and before releases;
run it locally when explicitly requested or when broad changes make focused validation insufficient.
Report checks performed, failures, and material validation gaps. Required checks must exit with code
`0`; an unavailable or failing required check is a blocker for the affected completion claim.

## Core instructions

- Architecture and persistence: `.agents/instructions/architecture.md`
- Architecture-boundary or deduplication work: `.agents/skills/maintain-architecture/SKILL.md`
- Testing: `.agents/instructions/testing.md`
- WPF and Windows desktop UI: `.agents/instructions/ui-desktop.md`
- Visual changes and application theme: `.agents/instructions/visual-design.md`
- Localization: `.agents/instructions/localization.md`
- Release notes and releases: `.agents/instructions/release-process.md`

- Website and product documentation: `.agents/instructions/website-documentation.md`.
  Every affected agent change must update both UI guidance and JSON/agent references for steps,
  values, jobs, macros, automations and logs in the same task; regenerate and pass the strict
  documentation check. Technical export alone does not replace reviewing behavior explanations.

Website screenshot maintenance: use `.agents/skills/maintain-website-screenshots/SKILL.md` for affected UI captures. Keep `website/docs/screenshots.json` dependencies and page mappings current; selectively render and visually inspect changed images before marking them reviewed. The strict website check verifies source/image hashes, review status and Doku placement.
