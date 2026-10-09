---
name: maintain-website-screenshots
description: Add, selectively render, review and maintain real DesktopAutomation screenshots for website and product documentation when their UI, fixtures or source dependencies change.
---

# Maintain website screenshots

Read [website documentation policy](../../instructions/website-documentation.md) and
[screenshot workflow](../../../website/docs/screenshots.md). The canonical catalog is
`website/docs/screenshots.json`; the renderer is `website/tools/Renderer`.

- Run `website/Build.ps1 -ListScreenshots` to identify stale images and their documentation pages.
- Extend the existing renderer fixture for a new function or state. Use fake services and fixed
  sample data; never execute desktop input, recordings or a real user's jobs for a screenshot.
- Add a stable catalog ID, PNG file, dimensions, descriptive caption, documentation URLs and
  relevant source patterns. Watch shared styles, localization, the view, its code-behind, view
  model and fixture dependencies. The generator inserts images on the catalog's `pages` URLs.
- Documentation uses focused `crop` regions and `markLabels`, not complete application windows.
  The capture derives numbered markers from visible, unclipped rendered UI labels and creates
  `.detail.png` plus `.detail.json`. Keep `markNotes` aligned with the meaning of each field.
  Step editor captures use `fieldDetails: true` to compose complete native field subtrees,
  including settings below the viewport. Frame the caption together with its input, populate
  every visible input with a meaningful example through isolated fixture data, and describe
  the composite honestly. A checkbox may be off and a numeric example may be zero.
  Review the cropped view and legend together; reject misplaced markers, missing controls,
  empty margins, wrong editor selections, or targets hidden by a scroll viewport. Structural
  contracts without an application view use the generator's clearly labelled schema diagrams.
- Render affected IDs with `website/Build.ps1 -Screenshots "verlauf,automationen"` or use
  `-ChangedScreenshots`. `-Render` refreshes the complete catalog.
- Inspect every new PNG and its placement in the generated website. Check intended state,
  Lightmode, readable fields, unclipped content and anonymous data. Repair and rerender poor
  captures. Then run `python website/tools/screenshots.py approve --ids "verlauf,automationen"`
  for exactly the images inspected. Never approve a render solely because it succeeded.
- Finish with the focused website/documentation/skills checks. Source fingerprints and PNG
  hashes detect stale or manually changed captures; visual review remains the agent's job.

If a changed function has no matching capture, add an appropriate fixture and association or
explain why no screenshot is affected. Update surrounding UI and JSON explanations in the same
task. This workflow does not schedule background work or publish the website.
