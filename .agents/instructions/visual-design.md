# Visual changes and application theme

Apply these instructions whenever a task changes visible layout, colors, typography, icons,
spacing, control appearance, or interaction states, including small visual fixes.

## Required skills and visual target

- Before implementation, load `product-design:index` and use the applicable Product Design
  workflow. Skill selection must be explicit; visual changes must not bypass Product Design
  as ordinary implementation work.
- Also load the repository's `wpf-development` skill for WPF work or `winforms-development`
  for changes within an existing Windows Forms interop boundary.
- Use the existing application surface and its code as the visual target for incremental
  changes. Identify the intended visible outcome and the appearance that must be preserved
  before editing. Use design exploration when the task calls for a new design direction.
- Product Design guides visual analysis and review; framework-specific implementation must
  follow the repository's desktop UI instructions. Keep the implementation in the application's
  existing framework and project.
- If a required skill is unavailable, report the missing skill as a blocker before implementing
  visual changes.

## Application theme is authoritative

- Inspect the relevant resource dictionaries, styles, and comparable existing controls before
  changing appearance. Existing application resources are the canonical design source.
- Reuse resources from `DesktopAutomationApp/Styles/`, including `Typography.xaml`,
  `Themes/`, `Accents/`, and the applicable control dictionaries.
- Use dynamic theme resources for theme-dependent properties. Preserve support for Light,
  Dark, and Black themes and the configured accent.
- Reuse existing styles and templates before adding new ones. Extend the canonical shared
  resource when a new reusable appearance is necessary; do not duplicate it in individual views.
- Do not hard-code theme-dependent colors, brushes, or typography where appropriate shared
  resources exist. Put new theme-dependent resources in the canonical dictionaries and provide
  values for every supported theme.
- Preserve the application's compact, value-first, visually quiet appearance, including its
  spacing, typography, icon conventions, and hover, selected, disabled, and focus states.
- Departures from the application theme require an explicit user instruction. Product Design
  suggestions or generated mockups do not authorize such departures.

## Visual verification

- Inspect the actual rendered desktop surface before and after the change. Compare it with the
  intended outcome and surrounding application controls; source inspection alone is insufficient.
- Verify relevant interaction states, keyboard focus, text readability, clipping, and supported
  window sizes and DPI scaling. Check Light, Dark, and Black themes; check affected accent
  behavior when accent resources change.
- Fix visual regressions before handoff. If rendering or required visual checks are unavailable,
  report the exact missing verification as a blocker and do not claim visual completion.
- In the handoff, briefly state the Product Design workflow used and the visual checks performed.
  Select repository checks according to `testing.md`.
