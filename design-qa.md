# Step-list design QA — 2026-10-05

The latest user request supersedes the earlier grip-based drag surface, inspector activation,
dense arrow routes and permanent Add step cards. The reference remains the selected DesktopAutomation mockup; this report
covers the revised native step-list surface.

Evidence: the real WPF `JobStepsView` rendered off-screen at 1195 × 1060 DIP using Black/Cyan
resources. Images are saved in `artifacts/verify/ui-render/`: `branches.png`, `nested.png`,
`collapsed.png`, `drag-snapped.png`, `drag-free.png`, and `invalid-nested.png`. The isolated process uses synthetic jobs
and fake services, never shows a desktop window and never loads user jobs. A hidden native
presentation source verifies screen-to-DIP cursor coordinates without moving the user’s cursor. No Computer Use is used.

- Cards contain one colored library icon, a localized name and a short single-line summary.
  Important numeric settings have labels or units; enum answers resolve to their visible labels,
  lists show counts and missing sources or secrets do not expose identifiers. Source numbers agree
  with gutter positions even after branch markers, and collapsed drag counts include hidden steps.
- Additional Add step cards are absent in both empty phases and branch bodies; natural empty
  branch boundaries remain valid drop positions.
- Only the colored icon starts dragging; the actual icon descendant is a handle and the actual
  card text is not. Number and breakpoint targets occupy separate measured columns.
- The inspector exposes neither activation nor breakpoint switches. These remain accessible
  through list actions and keyboard shortcuts.
- The always-visible collapse button has a 32 × 32 DIP hit region, state-specific localized name
  and tooltip, a circled chevron, raised background and subtle border.
- Nested surfaces have distinct elevation colors, shadows, left indentation and right insets.
  Surfaces are drawn before paths. Unchosen branches bypass their bodies in the indentation gutter;
  branch returns converge in the same narrow corridor at their own closure. The merge aligns with
  the next ordinary step icon, without rectangular returns across card widths. Collapsed descendants
  cannot supply stale route coordinates. Render tests bound routes to the measured icon corridor.
- The nested empty-condition scenario from the user’s screenshot has a red card border and an
  error entry with its list position, step name and translated message. Clicking it reveals and
  selects the step even after collapsing its phase and parent block. The inspector repeats the
  canonical validation message. Navigation leaves job data and dirty state unchanged.
  Validation remains owned by JobValidation; the editor and list share issue localization.
- Inspected snapped and floating renderings show the source ghost, target outline and cursor card.
  Native screen coordinates and drag feedback keep floating previews aligned with the cursor.
  Snapped previews reserve their own gap. Repeated snapping and rejected targets leave collection
  order intact; disposal restores opacity, transforms, displacement and original minimum height.
  Preview widths are measured again when the reserved gap reveals a viewport scrollbar.
- Existing structural coverage includes whole block moves, captured selections, empty branches,
  phase changes, collapsed groups, nested outdenting and undo. Hover expansion remains non-mutating.
- Edge scrolling is driven by elapsed time with a quadratic ramp and bounded speed. Tests cover
  both edges, the middle, invalid viewport positions and acquisition/retention snap thresholds.

The native operating-system drag gesture was not exercised through desktop automation, following
the user's instruction. Actual WPF templates, visual preview states and structural mutations are
covered by deterministic tests and saved renderings. The final repository gate checks all layers.

No outstanding P0/P1/P2 visual findings within the revised step-list scope.

Additional verification for the internal-marker and draft changes:

- Nine off-screen WPF renderings now cover branch and nested surfaces, collapsed blocks, both drag
  previews, canonical nested errors, an invalid draft after selecting another step, three consecutive
  block closures and an empty If block. Every EndIf row is collapsed with zero height.
- Inspected saved images show rounded closure joins without merge dots or selected footer strips.
  Consecutive closings have separate footer lanes; drop-placement tests address each nesting depth.
- The full-width problem entries use a quiet raised surface, a red left rule, visible step number,
  name, explanatory message and navigation chevron. Cached invalid drafts retain red card borders.
- Isolated picker interactions apply leaf selections and close, toggle closed on a second field
  click, leave group expansion open, and dismiss on Escape, outside input and editor hiding.
- Nested enum and shared-variable comparisons can be reopened without changing stored locals.
  Invalid local drafts leave persisted values intact; a valid replacement commits and undo restores
  the original value. Canonical input and structure failures coexist instead of overwriting one another.

final result: passed


# Step catalog and variable manager design QA — 2026-10-05

This section covers the first selected Product Design image, implemented in the existing WPF
project. It updates the earlier step-list observation about action-free cards: conditional headers
now intentionally contain the two branch buttons requested by the user. Ordinary steps retain the
single main Add step entrypoint.

## Visual target and evidence

Reference: first displayed generated design,
`C:/Users/fjsch/.codex/generated_images/01a10bd8-ca81-7ff2-80f9-88c819aa0099/exec-607b01d9-31dc-41d4-b6c9-984cbb0a7886.png`.
Reference board: 1448 × 1086 pixels with two native dialogs. Compare dialog-owned content rather
than the board padding or illustrative chrome. Actual WPF dialog contents are rendered at 96 DPI
(device scale factor 1): catalog 1120 × 610 and 940 × 480 DIP; variable manager 1140 × 710 and
980 × 580 DIP. The native title bars add their standard 36 DIP in the running application and
are excluded from the content capture. Both Dark/Cyan and Black/Cyan are covered.

Evidence in `artifacts/verify/ui-render/`: `add-step-dialog.png`, `add-step-compact.png`,
`add-step-empty.png`, `add-step-black.png`, `job-variables-dialog.png`,
`job-variables-compact.png`, `job-variables-black.png`, and the existing `nested.png` scene.
The reference and actual screenshots were inspected together, followed by individual normal and
minimum-width frames. The same selected If type and a shared integer variable with two usages are
used; values are deliberately shown as a pending draft to verify protected editing.

## Findings and disposition

- Fixed: selected categories previously inherited a bright theme fill; explicit theme surfaces,
  accent borders, and readable foregrounds now provide a clear selection.
- Fixed: the used variable type previously looked like a grey disabled control. A quiet locked
  value field and a visible explanation now communicate the restriction.
- Fixed: search/category transitions could lose the list marker while retaining the description.
  The picker binds the actual selected item, restores it after filtering, disables confirmation
  for an empty result, and reveals selection after layout and resizing.
- Fixed: long action labels and forced uppercase crowded the headers. Dialog buttons use sentence
  case; branch buttons use the user's compact Add else / Add else if labels.
- Expected product constraints: retain the actual eight canonical categories and all step types,
  not the mockup's illustrative sample category names. Use existing native icon descriptors and
  semantic theme colors. Retain the type filter and legacy variable editors. Numeric values retain
  the existing plain input controls without stepper buttons. Do not invent example content for step
  types; show their canonical description and explain where settings are edited.

## Required fidelity surfaces

- Fonts/typography: native Segoe UI system typography, 14–15 DIP primary rows, 19–21 DIP headings,
  and 12–13 DIP descriptions. Sentence case and text wrapping are legible at minimum size.
- Spacing/layout rhythm: three-column catalog and master/detail variable editor follow the selected
  hierarchy; consistent 16–24 DIP outer spacing and fixed action footers. Smaller windows scroll
  their content while actions remain within bounds. Description labels fit without awkward wrapping.
- Colors/tokens: existing theme surfaces, subtle borders, turquoise accents, yellow If icons,
  and readable selected/disabled states. Black is intentionally deeper than the reference's dark
  palette because the user's chosen application theme remains authoritative.
- Image quality/assets: real MahApps Material icons and descriptor mappings. The target contains no
  photographic or bespoke raster assets. No fabricated illustrations or custom-drawn icons.
- Copy/content: localized German and English text; explicit destination and no-focus fallback;
  clear draft retention, shared-change impact, protected types, and per-usage actions.

## Interaction and accessibility

Isolated native controls verify selection, categories, no-result confirmation, creation of the
chosen default type, direct per-usage action availability, retained drafts, protected types, and
minimum-width action bounds. Domain/UI tests cover branch placement, malformed structures,
phase/focus fallback, direct branch selection, per-usage detachment, and undo. Controls have stable
automation IDs, normal keyboard inputs, visible focus borders, and localized names/tooltips.
Hidden presentation sources never show or activate a desktop window and do not load user data.

## Implementation checklist

- Selected visual structure implemented in the two existing WPF dialogs.
- Conditional branch actions share ControlFlowEditRules with the context menu.
- Existing drafts, themes, input editors, persistence, and localization retained.
- Normal and compact native surfaces inspected; no remaining P0/P1/P2 visual findings.
- Full repository verification remains the independent mandatory completion gate.

final result: passed

## 2026-10-05 — Branch deletion and step group appearance

The shared control-flow editing rules now distinguish deleting an alternative branch from moving
its owning block. Deletion retains the If header, its other branches, and its closing marker;
nested content within the removed branch is included. Both delete commands retain undo support.

Eight catalog groups have distinct icon backgrounds. The canonical descriptors identify each
step's semantic icon; one WPF adapter resolves these icons and category resources for the picker,
step overview, inspector, and variable usage rows. Flow guides keep their structural meaning.

Inspected actual Release WPF renders in `artifacts/step-appearance-preview`: the eight-group
step overview, the add-step catalog, and existing nested branch scenarios. The palette preserves
white glyph legibility, leaves labels readable, and adds no layout changes. The render harness
checks every built-in step for a supported icon, distinct icons within groups, distinct group
colors, and identical colors between catalog and overview.

Visual result: passed. Full repository verification is the separate completion gate.

## 2026-10-05 — Plain left fold arrows and empty branch information

Inspected actual Release WPF renders in `artifacts/branch-fold-preview`, including all three
empty branch types and a folded Else-if with visible sibling branches. Fold arrows are left of
the drag symbols, show only chevrons, and keep 32-DIP keyboard-accessible hit targets. Empty
branches show a small muted localized line within their header row. The line is passive, adds
no list item, and does not create an additional drop index. Existing contour measurement places
the scope exit below that line. Collapsed branch guides are omitted.

Visual result: passed. Full repository verification is the independent completion gate.

## 2026-10-05 — Position inputs, block ends and keyboard focus

The real generated detail editor is rendered for all 38 editable built-in step types with expanded
advanced sections. The binding audit finds no errors, including switching coordinate pairs between
individual values and a shared reference. Position coordinates are shown at both
400 and 260 DIP widths; the narrow version stacks X and Y and keeps the source buttons aligned.
Process placement shows its localized position selection. Plain coordinate pairs have no empty
whole-source action. Related ROI, screen-point, reference-point and input templates are included.

The gray decorative closing chevron is removed while sequence routes still connect real steps.
The application resource for the standard WPF keyboard-focus adorner is empty; themed focus
markers and keyboard navigation remain. The isolated renderer verifies that this default adorner
contains no drawing shape and checks absence of the former block-end glyph.

Inspected `artifacts/detail-audit-preview/position-details.png`, `position-details-compact.png`,
`process-position-details.png` and `empty-branches.png`. Visual result: passed.
Full repository verification is the independent completion gate.
