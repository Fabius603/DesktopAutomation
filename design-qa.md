# Log workspace design QA — 2026-10-06

## Findings and result

No actionable P0/P1/P2 findings remain in the reviewed desktop states. This is a native WPF implementation of the four supplied mockups, using the existing application's theme, control templates and step icon catalog. It is not an assertion of pixel identity with raster mockups or their invented example messages.

final result: passed

## Source and capture

Source truth: the four user attachments in `C:/Users/fjsch/AppData/Local/Temp/`:

| View | Source file | Rendered implementation | Combined comparison |
|---|---|---|---|
| Overview | codex-clipboard-6d9034c3-bde7-4171-9418-4f80da9a52ae.png | artifacts/logging-ui/design-qa/overview.png | artifacts/logging-ui/design-qa/overview-comparison.png |
| Run and steps | codex-clipboard-8f72aba6-e71e-447b-a500-d021c33ef8ed.png | artifacts/logging-ui/design-qa/run.png | artifacts/logging-ui/design-qa/run-comparison.png |
| Automation history | codex-clipboard-37d307c9-2717-4737-b099-06a7d10f2a3d.png | artifacts/logging-ui/design-qa/automation.png | artifacts/logging-ui/design-qa/automation-comparison.png |
| Application diagnostics | codex-clipboard-ebc80d62-9784-4a44-a29f-2a9704606d08.png | artifacts/logging-ui/design-qa/application.png | artifacts/logging-ui/design-qa/application-comparison.png |

The complete source images are 1487 × 1058 px. Captures crop the unchanged navigation shell: source content crops are saved as `*-source.png`. Real Release WPF views are rendered in a 1200 × 1010 DIP window at 96 DPI (1× density); PNGs are 1200 × 1010 px. Source content crops are resampled to that client size only for side-by-side comparisons. No browser/CSS viewport applies. The native computer-use API is disabled, so evidence comes from the compiled WPF rendering host rather than a manually operated production window. The host uses the actual application dictionaries, view classes, query services and an isolated v2 repository.

States: German, dark theme/cyan accent; five representative recent runs, failed step 4, skipped step 5, four automation observations, selected diagnostic with expanded technical details. The diagnostics capture precedes the live-pause interaction; the test subsequently exercises pause and catch-up. Dynamic dates, IDs, run names and truthful backend diagnostics differ from mock example values. Additional captures cover 1000 × 720 DIP, light theme and empty history.

## Comparison history

1. P2: step rows and phase/iteration labels were too dense and the fifth step fell below the initial list viewport. Reduced row padding and placed phase/iteration metadata beside the result. Revised run capture shows all five rows and count footer.
2. P2: application category labels wrapped awkwardly. Increased the area column to 150 DIP and adjusted list text to 16 px. Revised application capture keeps category labels readable.
3. P1: light-theme rendering retained a dark window backdrop. Bound the rendering window to the application's dynamic background resource. Revised `light.png` shows consistent light surfaces and readable text.
4. P2: technical details reserved empty vertical space before the diagnostic text; extra path actions displaced the report. Moved technical text first, removed empty duplicate fields, and kept safe actions below it.
5. P2: long expanded reports could hide the copy action. Anchored copy/privacy controls outside the scrolling detail body; reduced detail spacing and simplified the application severity/summary surface. Revised application capture exposes code/time/source and leaves copying reachable; longer reports scroll.
6. P2: supplementary category/filter controls displaced the primary diagnostic table. Consolidated category and source controls under the existing advanced expander. Revised application composition retains the three primary filters, count, table and detail panel.

Post-fix full-view and focused-region evidence: `artifacts/logging-ui/design-qa/{overview,run,automation,application}-{comparison,detail-comparison}.png`. Focused crops compare the table/timeline rows, status treatments, descriptions, action buttons and technical section below y=350. Both artifacts were opened together before judging. Supplementary evidence: `compact.png`, `light.png`, `empty.png`.

## Required fidelity surfaces

- Typography: existing application font, 40 px page title, 28 px card titles, readable 18 px run/step text and 16 px diagnostic rows. Technical reports retain a selectable monospace field. Native text antialiasing differs from raster source rendering.
- Spacing/layout: four tabs, attention banner, five-column run table, recent automation card and two-column detail pages preserve source hierarchy. All five steps and four automation rows remain visible in the standard viewport. Extra backend options are collapsible; long reports and compact views scroll without hiding persistent copy controls.
- Colors/tokens: dynamic application surfaces, borders and foregrounds; cyan active accents; red failures, amber warnings, green success and muted unavailable states. Existing category colors for step icons are retained rather than recoloring every step orange.
- Image/icon quality: native MahApps vector icons and the existing canonical step catalog; no decorative raster assets or approximated illustrations are introduced. The unchanged shell is excluded from comparisons.
- Copy/content: all new interface text has German/English resources. Step summaries, effects and reasons use canonical backend facts and presentation keys. Missing context remains unknown. The mock's specific folder diagnosis is shown only when supported by captured facts. Search covers jobs, macros, automations and events; the table therefore labels its source column Job/Makro.

## Interactions and remaining scope

`LogScreenRenderingTests` renders real WPF controls, checks binding errors, and exercises step/problem selection, exact related-run navigation, skipped-step editor navigation, category filtering, live pause/catch-up, locale changes, ZIP export, 55-run pagination with a frozen export checkpoint, and unavailable current definitions. Lifecycle detaches view callbacks on unload. Canonical rule owners are `LogQueryService`, `LogPresentation`, `LogTimeline` and `LogDiagnostics`; the view model coordinates presentation state.

There is no live-browser console: the rendering test captures WPF binding diagnostics. Native save-dialog selection, Explorer integration and a manual multi-monitor/high-DPI production-window walk-through are not automated by this fixture. The existing shell and general DPI styles are retained. These are residual manual coverage limits, not evidence of a rendered defect. No P3 visual follow-up is required for the requested desktop layouts.


## Follow-up corrections — user feedback

The user's correction supersedes the original application's mockup header order: application
heading and subtitle now precede the tabs, consistently with the other log pages. The real Release
WPF capture `artifacts/logging-ui/design-qa/application-followup.png` was opened and inspected;
its heading, filters and two-column body are readable at the same viewport. No additional layout
regression is visible. `LogScreenRenderingTests` verifies the actual rendered heading/tab vertical
order, default execution filters after attention navigation, and no loading transitions across
multiple idle timer ticks after editor deactivation/reactivation. The subsequently authorized attention acknowledgement concept is implemented by the shared
application service and documented separately.

Follow-up final result: passed


## Attention implementation follow-up

The user explicitly requested implementing the attention concept. Overview adds a deliberate
bulk acknowledgement; details expose persistent Seen/Resolved/New and exact next-unseen-event
navigation. Status is placed beside Copy to keep the action area compact. Missing-evidence
summary acknowledgement is a bounded expander. The new actions are intentional additions to
the source mockups. Release rendering captures include `attention-summary.png`; interaction
coverage includes explicit summary acknowledgement, resolving/reopening, preserving reopening
through refresh, bulk confirmation, and two warnings from the same step after a deleted definition.
Persistent storage, severity changes and checkpoint scope have integration coverage.


## Macro steps editor — 2026-10-06

Selected source: `docs/concepts/macro-steps-reference.png`, the first selected Product Design
variant. Implementation: real native `MakroStepsView` in the existing application shell.
Evidence: `artifacts/macro-editor/design-qa/comparison.png` and `detail-comparison.png`, opened
alongside each other; supplementary `light.png`, `combination.png`, `multiple.png`, `invalid.png`,
`invalid-number.png`, `compact.png`, `dark.png` and `english.png`. The Release WPF rendering test
recreates these captures under `artifacts/verify/macro-ui-render`.

Typography uses the application font, a 32 px title, readable compact rows and a clear inspector
hierarchy. Layout retains the mock's header actions, grouped table, teal selection, right inspector
and bottom preview toolbar. Native vector mouse, keyboard, clock and folder icons replace raster
icons. Dynamic theme brushes preserve contrast in light and dark. The standard window shows all
eight sample commands; compact windows scroll table and inspector independently while keeping
Apply and preview actions visible. German and English controls refresh without losing drafts.

Corrected during review: white text on the light table; faded initial captures; uppercase action
labels; oversized title; malformed Unicode sample labels; lost selection after applying a draft;
stale validation after correcting a number; stale inspector/filter/recording labels after changing
language. Full and focused comparisons were inspected after these corrections.

Intentional functional adaptations: the application's existing navigation, branding and viewport
padding remain; an explicit Apply action validates drafts before changing commands; Run is disabled
until saved and while recording or previewing; native controls use existing styles. All step actions,
grouping, filtering, drag reordering, shortcuts, undo/redo, recording options and file access remain.
Duration summaries are calculated from actual command timing rather than copied from inconsistent
sample numbers in the source. Text input and key combinations are real persisted commands.

`MacroEditorRenderingTests` exercises actual TextBox/Button bindings, applying/undoing edits,
selection retention, multiple selection, invalid text/numbers, locale changes and the actual preview
overlay's start, seek, speed, stop and automatic completion. Backend tests cover Unicode, key
ownership, cancellation/failure cleanup, timing, conservative recording conversion, polymorphic
contracts, old defaults and repository round trips. Native computer automation is unavailable in
this session; evidence comes from the compiled WPF window and actual overlay in an isolated test
process, without sending desktop input. Manual hardware-hook/IME/multi-monitor input remains outside
the automated fixture.

Macro editor visual result: passed for the requested native composition, themes and viewports.


## Recording settings — selected second design, 2026-10-06

Source: `docs/concepts/recording-settings-reference.png`, the second displayed generated image
from the latest three-option set. Native implementation is the existing WPF RecordingSettingsDialog,
960 x 820 pixels. Source is normalized from its generated 1356 x 1158 viewport to 960 x 820.
Full and focused combined evidence was opened together:
`artifacts/recording-settings/design-qa/comparison.png` and `detail-comparison.png`.
Supplementary captures show the Inputs and Precision pages and dark compact 820 x 650 window.

The selected composition is implemented: quiet 270 px left navigation, turquoise active indicator,
large page heading, flat recording-mode rows, cyan selected mode, a separate hotkey area and fixed
Apply/Cancel footer. Typography uses the application's Segoe UI and compact native control styles;
headings, explanatory text, dividers, spacing and vector icons follow the source hierarchy. Window
chrome and button sizes remain native application conventions. All five input options, the two
precision controls, the three modes and hotkey capture remain available through the three sections.

Earlier visual findings corrected: nested card layout was replaced by flat pages; unreadable light
checkbox labels received theme foregrounds; mode descriptions wrap; mode selection updates every
radio and selection treatment; sidebar width, active accent and vertical spacing were aligned;
render fixtures use canonical hotkey formatting, showing F9 rather than its numeric key code.
The revised full and focused comparisons show no remaining actionable layout or contrast defect.
Compact content scrolls independently while footer actions and left navigation remain accessible.

Actual WPF rendering tests navigate all sections without losing edits, edit the precise interval,
check live navigation summaries, and exercise modal Apply and Cancel buttons. Draft settings are
isolated; Cancel preserves the original object, Apply returns an edited independent snapshot.
The macro editor regression also checks title-row Back placement and a rendered 9999 step number
against its measured text width. Additional captures cover live English labels and dark compact
layout. The native hardware hook remains represented by a fake; no real desktop input is sent.

Recording settings visual result: passed.
