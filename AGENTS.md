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
3. Write both German (`de`) and English (`en`) text. Describe the observable result for
   users rather than internal implementation details.
4. Use only the existing categories `Added`, `Changed`, and `Fixed`.
5. Keep the newest version first. Preserve every older release entry, including its
   date and sections, unchanged.
6. During normal development, the newest release-note version must be exactly one patch
   version higher than `<Version>` in Git `HEAD`. A mismatch is expected until an
   explicit release task updates the project version.
7. Before preparing a release, compare the repository against the previous release
   commit or tag and ensure every user-facing change is represented. Include committed
   changes and relevant uncommitted changes.
8. Do not add release-note entries for tests, refactoring, or documentation alone unless
   they change user-visible behavior. They may be summarized under a quality section
   when they are part of a larger release.
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

Every runtime behavior change and bug fix requires a regression test.

- `TaskAutomation` tests belong in `tests/TaskAutomation.Tests`.
- Prefer scenario coverage over smoke tests.
- Cover success, failure, missing input, skipped execution, and backward compatibility
  where applicable.
- A successful build alone is not sufficient for runtime changes.

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

- implementation and regression tests are present;
- German and English localization are synchronized;
- release notes are updated when behavior is user-visible;
- backward compatibility was considered;
- applicable tests and the Release build pass;
- unrelated working-tree changes remain untouched.
