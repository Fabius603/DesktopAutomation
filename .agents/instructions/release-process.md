# Release notes and releases

`DesktopAutomationApp/Resources/ReleaseNotes.json` is the user-facing changelog.

For every user-visible feature, behavior change, performance improvement, or bug fix:

1. Read `<Version>` from `git show HEAD:DesktopAutomationApp/DesktopAutomationApp.csproj`.
2. Increment only the patch component for the unreleased release-note block.
3. Do not change the project version unless the user explicitly requests a release.
4. Write one concise observable-result bullet in German and English.
5. Use only `Added`, `Changed`, or `Fixed` and keep newest versions first.
6. Merge overlapping bullets; omit implementation, tests, refactors, logging, and documentation.

Preserve every older release entry unchanged. A task with no user-visible effect needs no release
note, but the final handoff must say so.

Before an explicit release, compare against the previous release commit or tag and cover all
user-visible committed and relevant uncommitted changes.
