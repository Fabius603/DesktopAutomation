# Architecture and persistence

## Ownership boundaries

`DesktopAutomationApp` is a presentation layer. It may own XAML, transient view state, focus,
commands, visual formatting, localized labels, and mapping shared diagnostics to localized
messages. It must not own rules that determine what a job step or automation means, which
defaults apply, whether data is valid, how data is normalized or stored, which transitions are
legal, or how behavior executes.

- Put models, defaults, contracts, validation, reference materialization, migrations, and
  execution behavior in `TaskAutomation` or a lower platform-neutral project.
- Put transportable descriptors in `TaskAutomation.Contracts`.
- Keep shared and contract projects independent from WPF and Windows Forms types.
- Put control-flow matching, nesting, legal insertion and move rules, and runtime transitions
  in shared code consumed by every frontend.
- Keep view models, converters, controls, and code-behind as adapters over shared results. They
  may transform shared output for display but must not independently derive the same decision.

## One rule, one canonical implementation

Every business rule, default, validation, normalization, mapping, capability description, and
state transition must have one named canonical owner. All UI surfaces, execution paths, importers,
and tests consume that owner. Two implementations that currently return the same result are still
duplication and are not an acceptable fallback design.

- Search the repository for related models, services, validators, descriptors, converters, and
  tests before adding behavior. Prefer extending an existing abstraction over adding a parallel
  helper.
- When behavior differs by caller, model the variation explicitly as data, policy, strategy, or a
  shared interface. Do not copy the rule and edit each copy separately.
- Constants, identifiers, option catalogs, defaults, and mappings that must agree belong in one
  shared source. Presentation projects may contain only localized display projections.
- Do not keep an old and a new implementation active as an undocumented migration shortcut.
  Compatibility adapters may translate old input into the canonical model, but may not implement
  a second set of semantics.
- In touched code, remove or redirect competing implementations when it is safe and within scope.
  If immediate consolidation would require a separate migration, record the remaining duplicate,
  its canonical target, and the reason instead of silently adding another copy.

## Required architecture workflow

For every feature, bug fix, or refactor that changes behavior:

1. Trace the complete path from persistence or input through shared rules and execution to every
   affected frontend. Do not stop at the first visible symptom.
2. State the invariant and name the layer and type that will own it before editing.
3. Search for all existing implementations and consumers of that invariant.
4. Change the canonical shared implementation first. Keep frontend changes limited to binding,
   interaction, localization, and rendering.
5. Test the invariant at its owning layer. Add UI tests only for presentation behavior and add
   integration or end-to-end coverage only for distinct boundaries.
6. Re-run the search after editing and confirm that no conflicting implementation remains in the
   touched scope.

A task that introduces business decisions into presentation code or leaves newly discovered
conflicting implementations without an explicit migration record is not complete.

## Automated enforcement

Use architecture tests for dependency direction and other mechanically enforceable boundaries.
When a recurring violation can be detected deterministically, add or extend an architecture test
instead of relying only on prose. Do not use brittle source-text assertions for rules that need a
real dependency or behavior test.

Before handoff, report the canonical owner chosen for each changed rule and any known duplicate
that could not safely be consolidated. Durable exceptions or boundary changes require an accepted
decision record.

## Persistence

Persisted jobs, macros, automations, settings, identifiers, and paths are compatibility
contracts. New properties need safe defaults. Migrations must be best-effort, non-overwriting,
and tolerant of partially migrated state. `Common.JsonRepository/AppPaths.cs` is authoritative
for application data paths. Velopack installation data and user data stay separate.

Record decisions that constrain future designs in `docs/decisions/`; do not hide them only in
code comments or a dated specification.
