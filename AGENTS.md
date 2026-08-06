# Repository instructions for Codex

## Release notes are mandatory

`DesktopAutomationApp/Resources/ReleaseNotes.json` is the user-facing changelog and must
always be maintained together with the implementation.

For every user-facing feature, behavior change, performance improvement, or bug fix:

1. Determine the current released version from Git `HEAD`, not from modified files in
   the working tree. Read `<Version>` from
   `git show HEAD:DesktopAutomationApp/DesktopAutomationApp.csproj`, increment its patch
   component by exactly one, and use that version only for new release-note entries.
   For example, if Git `HEAD` contains `1.5.6`, new release notes must use `1.5.7`.
   Never append new changes to the version currently stored in Git `HEAD`, even if it
   is also the newest entry in `ReleaseNotes.json`.
2. Do not change `<Version>` in `DesktopAutomationApp/DesktopAutomationApp.csproj` as
   part of normal feature or bug-fix work. The project version remains at the version
   stored in Git `HEAD`. Increase it only when the user explicitly asks to prepare or
   perform a release.
3. Write both German (`de`) and English (`en`) text. Every change must be a short,
   bullet-style statement that describes only the observable result for users.
   Prefer one concise sentence. Before adding a bullet, inspect the complete current
   unreleased version for an existing entry about the same user-facing outcome. Extend
   or rewrite that entry instead of adding another one. Closely related features, fixes,
   UI controls, and follow-up adjustments must be represented by one combined bullet,
   even when they were implemented across different tasks or files.
4. Use only the existing categories `Added`, `Changed`, and `Fixed`.
5. Keep the newest version first. Preserve every older release entry, including its
   date and sections, unchanged.
6. During normal development, the newest release-note version must be exactly one patch
   version higher than `<Version>` in Git `HEAD`. A mismatch is expected until an
   explicit release task updates the project version.
7. Before preparing a release, compare the repository against the previous release
   commit or tag and ensure every user-facing change is represented. Include committed
   changes and relevant uncommitted changes.
8. Include only changes that users notice and are likely to care about. Do not mention
   implementation details, architecture, internal contracts, migrations, logging
   internals, tests, refactoring, or documentation. Omit minor technical corrections
   that have no meaningful effect on normal use; do not add internal quality sections.
   After every edit, review the entire unreleased block and merge or remove redundant,
   overlapping, overly specific, or low-value entries. Release notes are a curated
   summary for users, not a chronological record of completed development tasks.
9. Validate the JSON and run the normal DesktopAutomationApp Release build. The build's
   localization and embedded-resource checks must pass.

When a task truly has no user-facing effect, explicitly state in the final handoff that
no release-note update was required.

## Localization

Never add user-visible text directly in XAML or view models.

Every new or changed UI text must be added to both:

- `DesktopAutomationApp/Resources/Strings.resx`
- `DesktopAutomationApp/Resources/Strings.en.resx`

German and English resource files must contain matching keys.

## Working tree safety

The working tree may contain unrelated user changes.

- Never reset, revert, overwrite, or reformat unrelated changes.
- Inspect `git status --short` before editing.
- When a target file already contains changes, modify only the required sections.
- Do not create commits, branches, or stage files unless explicitly requested.

## Tests

Use risk-based regression coverage.

- `TaskAutomation` tests belong in `tests/TaskAutomation.Tests`.
- Runtime behavior changes and bug fixes require a regression test for the
  observable behavior or the concrete failure mode.
- Do not add a separate test for every modified method, property, XAML element,
  or implementation detail.
- Prefer extending an existing scenario or parameterized contract test over
  creating a new test. Do not duplicate behavior already covered by an
  equivalent higher-level or generic contract test.
- Pure refactoring, styling, spacing, resource-key-preserving text changes, and
  other changes without meaningful behavioral risk normally do not require a
  new test.
- Avoid tests that assert exact source-code or XAML fragments. Use them only
  when no practical behavioral or structural test is available and the asserted
  detail represents a deliberate, stable regression contract.
- Cover success, failure, missing input, cancellation, skipped execution, and
  backward compatibility only where they are relevant to the changed behavior.
- A successful build alone is not sufficient for material runtime changes.

## Persistence and backward compatibility

Changes to serialized jobs, macros, automations, settings, or paths must remain backward
compatible unless a migration is explicitly introduced.

- Existing JSON files must continue to load.
- New serialized properties require safe defaults.
- Migrations must be best-effort, non-overwriting, and tolerate partial old state.
- `Common.JsonRepository/AppPaths.cs` is the source of truth for application data paths.
- Keep Velopack installation data separate from user data.

## Job steps and result contracts

New or changed job steps must follow the repository contracts:

- Register the step in the pipeline registry.
- Define a typed result contract when the step produces output.
- Every selectable result property requires a stable property ID.
- Keep legacy property paths readable for backward compatibility.
- Update validation, localization, editor UI, details display, and tests together.
- Follow:
  - `TaskAutomation/Steps/ADDING_A_JOB_STEP.md`
  - `TaskAutomation/Steps/RESULT_CONTRACTS.md`

## Definition of done

A change is complete only when:

- applicable risk-based regression coverage is present; when no test was added,
  the final handoff explains why existing coverage or build validation is sufficient;
- German and English localization are synchronized;
- release notes are updated when behavior is user-visible;
- backward compatibility was considered;
- applicable tests and the Release build pass;
- unrelated working-tree changes remain untouched.
